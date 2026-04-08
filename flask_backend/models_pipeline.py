import os
import joblib
import numpy as np

# We assume models are stored in a 'saved_models' directory
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
MODELS_DIR = os.path.join(BASE_DIR, 'saved_models')

# Paths
BINARY_MODEL_PATH = os.path.join(MODELS_DIR, 'binary_classifier.pkl')
MULTICLASS_MODEL_PATH = os.path.join(MODELS_DIR, 'multiclass_classifier.pkl')
REGRESSION_MODEL_PATH = os.path.join(MODELS_DIR, 'extra_trees_regressor.pkl')

class PredictionPipeline:
    def __init__(self):
        self.binary_model = None
        self.multiclass_model = None
        self.regression_model = None
        self._load_models()

    def _load_models(self):
        """Loads models if they exist. Silently skips if they don't, 
           falling back to dummy predictions for testing."""
        if os.path.exists(BINARY_MODEL_PATH):
            self.binary_model = joblib.load(BINARY_MODEL_PATH)
        if os.path.exists(MULTICLASS_MODEL_PATH):
            self.multiclass_model = joblib.load(MULTICLASS_MODEL_PATH)
        if os.path.exists(REGRESSION_MODEL_PATH):
            self.regression_model = joblib.load(REGRESSION_MODEL_PATH)

    def predict(self, features: list) -> dict:
        """
        Runs the exact pipeline specified in the documentation.
        features: [engine_power, coolant_temp, engine_load, rpm, 
                   intake_temp, speed, fuel_trim, throttle, timing]
        """
        X = np.array(features).reshape(1, -1)

        result = {
            "prediction": "Normal",
            "trouble_code": "None",
            "estimated_time_remaining": 0.0,
        }

        # 1. Binary Classification
        if self.binary_model is not None:
            is_problem = self.binary_model.predict(X)[0]
        else:
            # Fallback mock logic for development
            is_problem = 1 if features[1] > 90 else 0

        if is_problem == 0:
            return result

        result["prediction"] = "Problem Detected"

        # 2. Multi-class Classification (DTC Code)
        if self.multiclass_model is not None:
            dtc = self.multiclass_model.predict(X)[0]
        else:
            # Fallback mock logic
            dtc = "C0300" 
        result["trouble_code"] = dtc

        # 3. Regression (Estimated Hours)
        if self.regression_model is not None:
            hours = self.regression_model.predict(X)[0]
        else:
            # Fallback mock logic
            hours = 7.27
        result["estimated_time_remaining"] = round(float(hours), 2)

        return result
