import os
import joblib
import numpy as np

# We assume models are stored in a 'saved_models' directory
BASE_DIR = os.path.dirname(os.path.abspath(__file__))
MODELS_DIR = os.path.join(BASE_DIR, 'saved_models')

# Paths
BINARY_MODEL_PATH     = os.path.join(MODELS_DIR, 'binary_classifier.pkl')
MULTICLASS_MODEL_PATH = os.path.join(MODELS_DIR, 'multiclass_classifier.pkl')
REGRESSION_MODEL_PATH = os.path.join(MODELS_DIR, 'extra_trees_regressor.pkl')


class PredictionPipeline:
    def __init__(self):
        self.binary_model     = None
        self.multiclass_model = None
        self.regression_model = None
        self._load_models()

    def _load_models(self):
        """Loads models if they exist. Logs missing models for visibility."""
        if os.path.exists(BINARY_MODEL_PATH):
            self.binary_model = joblib.load(BINARY_MODEL_PATH)
            print(f"[Pipeline] Binary classifier loaded.")
        else:
            print(f"[Pipeline] WARNING: Binary classifier not found at {BINARY_MODEL_PATH}")

        if os.path.exists(MULTICLASS_MODEL_PATH):
            self.multiclass_model = joblib.load(MULTICLASS_MODEL_PATH)
            print(f"[Pipeline] Multiclass classifier loaded.")
        else:
            print(f"[Pipeline] WARNING: Multiclass classifier not found at {MULTICLASS_MODEL_PATH}")

        if os.path.exists(REGRESSION_MODEL_PATH):
            self.regression_model = joblib.load(REGRESSION_MODEL_PATH)
            print(f"[Pipeline] Regression model loaded.")
        else:
            print(f"[Pipeline] WARNING: Regression model not found at {REGRESSION_MODEL_PATH}")

    def predict(self, features: list) -> dict:
        """
        Runs the 3-stage pipeline:
        1. Binary Classification  → Normal / Problem Detected
        2. Multi-class            → DTC Code identification
        3. Regression             → Estimated hours until failure

        features: [engine_power, coolant_temp, engine_load, rpm,
                   intake_temp, speed, fuel_trim, throttle, timing]

        Returns a dict with:
          prediction, confidence, trouble_code, estimated_time_remaining
        """
        X = np.array(features).reshape(1, -1)

        result = {
            "prediction": "Normal",
            "confidence": 1.0,
            "trouble_code": "None",
            "estimated_time_remaining": 0.0,
        }

        # ── Stage 1: Binary Classification ────────────────────────────────────
        if self.binary_model is not None:
            is_problem = int(self.binary_model.predict(X)[0])
            # Get probability of the "problem" class (index 1)
            try:
                proba = self.binary_model.predict_proba(X)[0]
                # confidence = probability of the predicted class
                confidence = float(proba[is_problem])
            except AttributeError:
                confidence = 1.0  # Model doesn't support predict_proba
        else:
            # Fallback mock: high coolant temp → problem
            is_problem = 1 if features[1] > 90 else 0
            confidence = 0.75  # lower confidence for mock

        result["confidence"] = round(confidence, 3)

        if is_problem == 0:
            return result

        result["prediction"] = "Problem Detected"

        # ── Stage 2: Multi-class Classification (DTC Code) ────────────────────
        if self.multiclass_model is not None:
            dtc = str(self.multiclass_model.predict(X)[0])
        else:
            dtc = "C0300"  # Fallback mock
        result["trouble_code"] = dtc

        # ── Stage 3: Regression (Estimated Hours Remaining) ───────────────────
        if self.regression_model is not None:
            hours = float(self.regression_model.predict(X)[0])
            # Clamp to a sensible range — model can overshoot on edge cases
            hours = max(0.0, min(168.0, hours))  # max 1 week
        else:
            hours = 7.27  # Fallback mock
        result["estimated_time_remaining"] = round(hours, 2)

        return result
