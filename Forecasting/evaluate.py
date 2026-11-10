import os
import numpy as np
import pandas as pd
from sklearn.metrics import mean_absolute_error, mean_squared_error
from xgboost import XGBRegressor
from sktime.forecasting.compose import make_reduction

from main import (
    CSV_PATH, FREQ, WINDOW_LENGTH,
    load_energy_report_csv, prepare_series_from_csv, 
    create_calender_features
)

def evaluate_model():
    if not os.path.exists(CSV_PATH):
        print(f"Fejl: {CSV_PATH} blev ikke fundet.")
        return

    print("Indlæser data til evaluering...")
    df = load_energy_report_csv(CSV_PATH)
    
    area = "DK1"
    y_full = prepare_series_from_csv(df, area)
    
    test_days = 7
    steps_per_day = 96
    total_test_size = test_days * steps_per_day
    
    if len(y_full) <= total_test_size + WINDOW_LENGTH:
        print("Ikke nok data i CSV til at lave et split.")
        return

    y_train = y_full.iloc[:-total_test_size]
    y_test = y_full.iloc[-total_test_size:]

    print(f"Træner model på data indtil: {y_train.index[-1]} ...")
    X_train = create_calender_features(pd.DatetimeIndex(y_train.index))

    regressor = XGBRegressor(
        n_estimators=300,
        learning_rate=0.05,
        max_depth=6,
        subsample=0.8,
        colsample_bytree=0.8,
        random_state=42,
        n_jobs=-1,
    )

    forecaster = make_reduction(regressor, window_length=WINDOW_LENGTH, strategy="recursive")
    forecaster.fit(y_train, X=X_train)

    # Hent den underliggende XGBoost-model ud (præcis som i main.py)
    xgb_model = getattr(forecaster, "estimator_", getattr(forecaster, "_estimator", None))
    assert xgb_model is not None

    print("Kører robust rullende 24-timers evaluering over de sidste 7 dage...")
    
    model_preds = []
    baseline_preds = []
    actuals = []

    for day in range(test_days):
        start_idx = day * steps_per_day
        end_idx = start_idx + steps_per_day
        
        y_target_chunk = y_test.iloc[start_idx:end_idx]
        
        if start_idx == 0:
            y_window = y_train.iloc[-WINDOW_LENGTH:].copy()
        else:
            y_window = pd.concat([y_train, y_test.iloc[:start_idx]]).iloc[-WINDOW_LENGTH:].copy()

        # Byg fremtidige kalender-features for dette døgn
        X_future = create_calender_features(pd.DatetimeIndex(y_target_chunk.index))
        X_future_np = X_future.to_numpy(dtype=np.float64)
        
        history_vals = list(np.nan_to_num(y_window.to_numpy(dtype=np.float64), nan=0.0))
        n_expected = int(xgb_model.n_features_in_)
        n_x_cols = X_future_np.shape[1]

        day_preds = []
        for i in range(steps_per_day):
            lags = np.asarray(history_vals[-WINDOW_LENGTH:], dtype=np.float64)
            x_feat = X_future_np[i]

            # Match feature-størrelse sikkert
            if n_expected == WINDOW_LENGTH + n_x_cols:
                features = np.concatenate((lags, x_feat)).reshape(1, -1)
            else:
                # Sikker fallback hvis formen kræver flere elementer
                dummy_x = np.tile(x_feat, WINDOW_LENGTH)[:n_expected - WINDOW_LENGTH]
                features = np.concatenate((lags, dummy_x)).reshape(1, -1)

            pred_val = float(xgb_model.predict(features)[0])
            if np.isnan(pred_val):
                pred_val = float(history_vals[-1])

            day_preds.append(pred_val)
            history_vals.append(pred_val)

        model_preds.extend(day_preds)
        actuals.extend(y_target_chunk.values)

        # Naive Baseline (Prisen på samme tidspunkt for 24 timer siden)
        baseline_chunk = y_window.iloc[-steps_per_day:].values
        baseline_preds.extend(baseline_chunk)

    y_pred = pd.Series(model_preds)
    y_base = pd.Series(baseline_preds)
    y_true = pd.Series(actuals)

    mae_model = mean_absolute_error(y_true, y_pred)
    rmse_model = np.sqrt(mean_squared_error(y_true, y_pred))

    mae_base = mean_absolute_error(y_true, y_base)
    rmse_base = np.sqrt(mean_squared_error(y_true, y_base))
    
    print("\n================ RESULTATER (24-timers horisont) ================")
    print(f"Prisområde: {area}")
    print(f"--- XGBoost Model ---")
    print(f"  MAE:  {mae_model:.4f} DKK ({mae_model * 100:.2f} øre)")
    print(f"  RMSE: {rmse_model:.4f} DKK")
    print(f"--- Naive Baseline (Pris for 24 timer siden) ---")
    print(f"  MAE:  {mae_base:.4f} DKK ({mae_base * 100:.2f} øre)")
    print(f"  RMSE: {rmse_base:.4f} DKK")
    print("=================================================================\n")

if __name__ == "__main__":
    evaluate_model()