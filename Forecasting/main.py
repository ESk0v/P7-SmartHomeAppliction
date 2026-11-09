import os
import copy
import joblib
import numpy as np
import pandas as pd
from datetime import datetime
from typing import List, Literal, cast
from contextlib import asynccontextmanager

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field
from xgboost import XGBRegressor
from sktime.forecasting.base import ForecastingHorizon
from sktime.forecasting.compose import make_reduction

FREQ = "15min"
# 96 length, since it's 15 minutes intervals for 24 hours
WINDOW_LENGTH = 96
CSV_PATH = "Dataset/EnergyReport.csv"
CSV_TIME_COL = "TimeUTC"

MODEL_PATHS = {
    "DK1": "model_DK1.joblib",
    "DK2": "model_DK2.joblib",
}

models = {}

# Helpers for Calender features
def create_calender_features(index: pd.DatetimeIndex):
    # gets time of day and day of week features
    dk_time = index.tz_localize("UTC").tz_convert("Europe/Copenhagen")

    df_feat = pd.DataFrame(index=index)
    df_feat["hour"] = dk_time.hour
    df_feat["minute"] = dk_time.minute
    df_feat["dayofweek"] = dk_time.dayofweek
    df_feat["is_weekend"] = (dk_time.dayofweek >= 5).astype(int)
    df_feat["month"] = dk_time.month

    seconds_in_day = 24*60*60
    time_sec = dk_time.hour * 3600 + dk_time.minute * 60
    df_feat["sin_time"] = np.sin(2 *np.pi * time_sec / seconds_in_day)
    df_feat["cos_time"] = np.cos(2 * np.pi * time_sec / seconds_in_day)
    return df_feat

def enforce_15min_grid(df_time_price: pd.DataFrame):
    # sorts, removes duplicates and uses 15-minute freq (needed by sktime)
    df_clean = df_time_price.copy()
    df_clean["time"] = pd.to_datetime(df_clean["time"], utc=True).dt.tz_localize(None)
    df_clean = df_clean.sort_values("time").drop_duplicates(subset=["time"], keep="last")
    df_clean = df_clean.set_index("time")

    full_idx = pd.date_range(start=df_clean.index.min(), end=df_clean.index.max(), freq=FREQ)
    series = df_clean["price_per_kwh"].reindex(full_idx).interpolate(method="time").bfill()
    series.index = pd.DatetimeIndex(series.index, freq=FREQ)
    return series

def load_energy_report_csv(csv_path: str):
    #loads the csv
    with open(csv_path, "r", encoding="utf-8-sig") as file:
        first_line = file.readline()

    skip = 0 if CSV_TIME_COL in first_line else 1
    df = pd.read_csv(csv_path, sep=None, engine="python", skiprows=skip, encoding="utf-8-sig")
    df.columns = df.columns.str.strip()
    return df

def prepare_series_from_csv(df: pd.DataFrame, area: str):
    # finds either DK1 or DK2 and gets mwh to kwh
    col_name = f'{area}_DKK/MWh'
    if col_name not in df.columns:
        raise ValueError(f'Column "{col_name}" is not in the csv. Instead: {list(df.columns)}')
    price_mwh = (df[col_name].astype(str)
                 .str.replace(",", ".", regex=False)
                 .str.strip().astype(float))
    df_area = pd.DataFrame({
        "time": df[CSV_TIME_COL], 
        "price_per_kwh": price_mwh / 1000.0,
    })
    return enforce_15min_grid(df_area)

def train_models_from_csv(csv_path: str):
    # trains the sk_time XGBRressor model
    if not os.path.exists(csv_path):
        raise FileNotFoundError(f'{csv_path} not found. Place in correct folder')

    print('loading training data')
    df = load_energy_report_csv(csv_path)

    for area in ["DK1", "DK2"]:
        print(f'training for area: {area}')
        y_train = prepare_series_from_csv(df, area)
        X_train = create_calender_features(pd.DatetimeIndex(y_train.index, freq=FREQ))

        regressor = XGBRegressor(
            n_estimators = 300,
            learning_rate = 0.05,
            max_depth = 6,
            subsample = 0.8,
            colsample_bytree = 0.8,
            random_state = 42,
            n_jobs = -1,
        )

        forecaster = make_reduction(regressor, window_length=WINDOW_LENGTH, strategy="recursive")
        forecaster.fit(y_train, X=X_train)
        joblib.dump(forecaster, MODEL_PATHS[area])
        models[area] = forecaster
        print(f'Finished model saved to: {MODEL_PATHS[area]}')


# FastAPI
@asynccontextmanager
async def lifespan(app: FastAPI):
    for area, path in MODEL_PATHS.items():
        if os.path.exists(path):
            print(f"Loading pre-trained model for {area} from {path}")
            models[area] = joblib.load(path)
    if len(models) < 2:
        train_models_from_csv(CSV_PATH)
    yield

app = FastAPI(lifespan=lifespan)

# DTO's which match the PostgreSQL schema
class HistoricalPricePoint(BaseModel):
    time: datetime
    price_per_kwh: float

class ForecastingRequest(BaseModel):
    price_area: Literal["DK1", "DK2"]
    steps_ahead: int = Field(default=96, ge=1, le=288)
    recent_prices: List[HistoricalPricePoint] = Field(..., description="At least the last 24 hours from database")

class PredictedPricePoint(BaseModel):
    time: datetime
    price_per_kwh: float
    price_area: str
    is_predicted: bool = True

class ForecastResponse(BaseModel):
    price_area: str
    predictions: List[PredictedPricePoint]


# FastAPI endoints called by our C# backend
@app.post("/forecast", response_model=ForecastResponse)
def generate_forecast(req: ForecastingRequest):
    area = req.price_area
    if area not in models:
        raise HTTPException(status_code=503, detail=f'Model for {area} not trained')

    # Convert to 15 minute grid
    df_recent = pd.DataFrame([p.model_dump() for p in req.recent_prices])
    y_recent = enforce_15min_grid(df_recent)

    if len(y_recent) < WINDOW_LENGTH:
        raise HTTPException(status_code=400, detail=f'Need minimum {WINDOW_LENGTH} previous prices')

    y_window = y_recent.iloc[-WINDOW_LENGTH:]
    X_window = create_calender_features(pd.DatetimeIndex(y_window.index, freq=FREQ))

    forecaster = models[area]

    xgb_model = getattr(forecaster, "estimator_", getattr(forecaster, "_estimator", None))
    assert xgb_model is not None

    future_start = y_window.index[-1] + pd.Timedelta(minutes=15)
    future_idx = pd.date_range(start=future_start, periods=req.steps_ahead, freq=FREQ)
    X_future = create_calender_features(future_idx)

    X_all_np = pd.concat([X_window, X_future]).to_numpy(dtype=np.float64)
    history_y = list(np.nan_to_num(y_window.to_numpy(dtype=np.float64), nan=0.0))
    
    n_expected = int(xgb_model.n_features_in_)
    n_x_cols = X_all_np.shape[1]
    preds: list[float] = []

    for i in range(req.steps_ahead):
        # De seneste 96 y-værdier
        y_lags = np.asarray(history_y[-WINDOW_LENGTH:], dtype=np.float64)

        # sktime's make_reduction inkluderer enten kun aktuel X (96 + 7 = 103 features)
        # eller hele vinduet af X (96 + 96*7 = 768 features). Vi matcher automatisk:
        if n_expected == WINDOW_LENGTH + n_x_cols:
            x_feat = X_all_np[WINDOW_LENGTH + i]
        elif n_expected == WINDOW_LENGTH + (WINDOW_LENGTH * n_x_cols):
            x_feat = X_all_np[i + 1 : i + 1 + WINDOW_LENGTH].ravel()
        else:
            x_feat = X_all_np[i : i + WINDOW_LENGTH + 1].ravel()[: n_expected - WINDOW_LENGTH]

        features = np.concatenate((y_lags, x_feat)).reshape(1, -1)
        pred_val = float(xgb_model.predict(features)[0])
        
        # Sikr at vi aldrig sender NaN tilbage til C#
        if np.isnan(pred_val):
            pred_val = float(history_y[-1])

        preds.append(pred_val)
        history_y.append(pred_val)

    predictions = [
        PredictedPricePoint(
            time=pd.Timestamp(ts).tz_localize("UTC").to_pydatetime(),
            price_per_kwh=round(val, 5),
            price_area=area,
            is_predicted=True,
        )
        for ts, val in zip(future_idx, preds)
    ]

    return ForecastResponse(price_area=area, predictions=predictions)


@app.post("/retrain")
def retrain_models():
    # Retrains the models for both areas
    train_models_from_csv(CSV_PATH)
    return {"message": "Models retrained successfully"}

