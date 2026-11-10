import os

import numpy as np
import pandas as pd
from sklearn.metrics import mean_absolute_error, mean_squared_error
from xgboost import XGBRegressor
from sktime.forecasting.base import ForecastingHorizon
from sktime.forecasting.compose import make_reduction

from fake import (
    CSV_PATH,
    FREQ,
    WINDOW_LENGTH,
    load_energy_report_csv,
    prepare_series_from_csv,
    create_calender_features,
)


def calculate_metrics(actuals: pd.Series, predictions: pd.Series) -> tuple[float, float]:
    """Return MAE and RMSE for a set of predictions."""
    mae = mean_absolute_error(actuals, predictions)
    rmse = np.sqrt(mean_squared_error(actuals, predictions))
    return mae, rmse


def print_metrics(name: str, actuals: pd.Series, predictions: pd.Series) -> None:
    mae, rmse = calculate_metrics(actuals, predictions)
    print(f"--- {name} ---")
    print(f"  MAE:  {mae:.4f} DKK/kWh ({mae * 100:.2f} øre/kWh)")
    print(f"  RMSE: {rmse:.4f} DKK/kWh")


def evaluate_model() -> None:
    if not os.path.exists(CSV_PATH):
        print(f"Error: {CSV_PATH} was not found.")
        return

    print("Loading data for evaluation...")
    df = load_energy_report_csv(CSV_PATH)

    area = "DK1"
    y_full = prepare_series_from_csv(df, area)

    test_days = 7
    steps_per_day = int(pd.Timedelta("1 day") / pd.Timedelta(FREQ))
    total_test_size = test_days * steps_per_day
    seasonal_lookback = 7 * steps_per_day

    # The 7-day average baseline needs at least seven complete days
    # of observations before the first test forecast.
    min_training_size = max(WINDOW_LENGTH, seasonal_lookback)
    if len(y_full) <= total_test_size + min_training_size:
        print("Not enough data in the CSV for this train/test split.")
        return

    y_train = y_full.iloc[:-total_test_size]
    y_test = y_full.iloc[-total_test_size:]

    print(f"Training data ends at: {y_train.index[-1]}")
    print(f"Test period: {y_test.index[0]} to {y_test.index[-1]}")

    X_train = create_calender_features(pd.DatetimeIndex(y_train.index, freq=FREQ))

    # Keep these hyperparameters in sync with train_models_from_csv()
    # in main_sktime.py.
    regressor = XGBRegressor(
        n_estimators=300,
        learning_rate=0.05,
        max_depth=6,
        subsample=0.8,
        colsample_bytree=0.8,
        random_state=42,
        n_jobs=-1,
    )

    forecaster = make_reduction(
        regressor,
        window_length=WINDOW_LENGTH,
        strategy="recursive",
    )
    # update() must retain the observed history so subsequent forecasts
    # can use the newly observed prices without refitting the XGBoost model.
    forecaster.set_config(remember_data=True)
    forecaster.fit(y_train, X=X_train)

    # A relative horizon of 1..96 means the next 24 hours at 15-minute
    # frequency. sktime handles the recursive predictions internally.
    fh = ForecastingHorizon(
        np.arange(1, steps_per_day + 1),
        is_relative=True,
    )

    model_predictions: list[float] = []
    naive_24h_predictions: list[float] = []
    seasonal_7d_average_predictions: list[float] = []
    actual_values: list[float] = []

    print("\nRunning rolling 24-hour forecasts across the final 7 days...")

    for day in range(test_days):
        start_idx = day * steps_per_day
        end_idx = start_idx + steps_per_day
        y_target_chunk = y_test.iloc[start_idx:end_idx]

        # For day 1 and onwards, ingest the previous day's actual prices.
        # This mimics a real daily forecast: we do not feed predictions
        # from yesterday into today's forecast when yesterday's actual prices
        # would by then be available.
        if start_idx > 0:
            y_new = y_test.iloc[start_idx - steps_per_day:start_idx]
            X_new = create_calender_features(
                pd.DatetimeIndex(y_new.index, freq=FREQ)
            )
            forecaster.update(y=y_new, X=X_new, update_params=False)

        X_future = create_calender_features(
            pd.DatetimeIndex(y_target_chunk.index, freq=FREQ)
        )
        y_pred = forecaster.predict(fh=fh, X=X_future)

        # Fail loudly if the predictions do not line up with the target period.
        if not y_pred.index.equals(y_target_chunk.index):
            raise RuntimeError(
                "sktime returned a prediction index that does not match "
                f"the test period for day {day + 1}. "
                f"Predicted: {y_pred.index[0]}..{y_pred.index[-1]}, "
                f"expected: {y_target_chunk.index[0]}..{y_target_chunk.index[-1]}"
            )

        day_predictions = y_pred.to_numpy(dtype=np.float64)
        if not np.isfinite(day_predictions).all():
            raise ValueError(f"The model produced a non-finite value on test day {day + 1}.")

        model_predictions.extend(day_predictions.tolist())
        actual_values.extend(y_target_chunk.to_numpy(dtype=np.float64).tolist())

        # Both baselines use only prices available before this forecast day.
        observed_history = pd.concat([y_train, y_test.iloc[:start_idx]])

        # Baseline 1: same price as exactly 24 hours earlier.
        naive_24h = observed_history.iloc[-steps_per_day:]
        naive_24h_predictions.extend(naive_24h.to_numpy(dtype=np.float64).tolist())

        # Baseline 2: average of the previous seven days at the same
        # 15-minute position within the day.
        previous_seven_days = observed_history.iloc[-seasonal_lookback:]
        seven_day_matrix = previous_seven_days.to_numpy(dtype=np.float64).reshape(
            test_days, steps_per_day
        )
        seasonal_average = seven_day_matrix.mean(axis=0)
        seasonal_7d_average_predictions.extend(seasonal_average.tolist())

        day_actuals = y_target_chunk.to_numpy(dtype=np.float64)
        day_mae = mean_absolute_error(day_actuals, day_predictions)
        print(f"  Day {day + 1}: XGBoost MAE = {day_mae:.4f} DKK/kWh")

    y_true = pd.Series(actual_values, name="actual")
    y_model = pd.Series(model_predictions, name="xgboost")
    y_naive_24h = pd.Series(naive_24h_predictions, name="seasonal_naive_24h")
    y_seasonal_7d = pd.Series(
        seasonal_7d_average_predictions,
        name="seasonal_average_7d",
    )

    print("\n================ RESULTS (rolling 24-hour forecasts) ================")
    print(f"Price area: {area}")
    print(f"Test observations: {len(y_true)}")
    print(f"Forecast frequency: {FREQ}")
    print()
    print_metrics("XGBoost + sktime", y_true, y_model)
    print()
    print_metrics("Seasonal naive (same time yesterday)", y_true, y_naive_24h)
    print()
    print_metrics("Seasonal average (same time, previous 7 days)", y_true, y_seasonal_7d)
    print("=======================================================================\n")


if __name__ == "__main__":
    evaluate_model()
