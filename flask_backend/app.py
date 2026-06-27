import os
from flask import Flask, request, jsonify
from models_pipeline import PredictionPipeline
from openai_service import generate_dtc_description

app = Flask(__name__)
pipeline = PredictionPipeline()

# ─── Required feature keys and their valid ranges ────────────────────────────
FEATURE_SPECS = {
    "engine_power":        (0.0,   2000.0),
    "engine_coolant_temp": (-40.0, 215.0),
    "engine_load":         (0.0,   100.0),
    "engine_rpm":          (0.0,   10000.0),
    "air_intake_temp":     (-40.0, 215.0),
    "speed":               (0.0,   400.0),
    "short_term_fuel_trim":(-100.0, 100.0),
    "throttle_pos":        (0.0,   100.0),
    "timing_advance":      (-64.0, 63.5),
}


def _validate_features(data: dict) -> tuple[list | None, str | None]:
    """
    Validates and extracts feature values from the request body.
    Returns (features_list, None) on success or (None, error_message) on failure.
    """
    features = []
    for key, (lo, hi) in FEATURE_SPECS.items():
        raw = data.get(key)
        if raw is None:
            # Missing key — use 0.0 as safe default
            features.append(0.0)
            continue
        try:
            val = float(raw)
        except (TypeError, ValueError):
            return None, f"Invalid value for '{key}': expected a number, got '{raw}'"
        # Clamp to valid range instead of rejecting — OBD adapters sometimes overshoot
        val = max(lo, min(hi, val))
        features.append(val)
    return features, None


# ─── Health check ──────────────────────────────────────────────────────────────
@app.route('/health', methods=['GET'])
def health():
    """Quick liveness probe for monitoring."""
    models_loaded = {
        "binary":     pipeline.binary_model is not None,
        "multiclass": pipeline.multiclass_model is not None,
        "regression": pipeline.regression_model is not None,
    }
    all_loaded = all(models_loaded.values())
    return jsonify({
        "status": "ok" if all_loaded else "degraded",
        "models": models_loaded,
        "version": "2.0.0"
    }), 200 if all_loaded else 207


# ─── Prediction ────────────────────────────────────────────────────────────────
@app.route('/predict', methods=['POST'])
def predict():
    """
    POST /predict
    Body (JSON):
      engine_power, engine_coolant_temp, engine_load, engine_rpm,
      air_intake_temp, speed, short_term_fuel_trim, throttle_pos, timing_advance
    """
    data = request.get_json(silent=True)
    if not data:
        return jsonify({"error": "Request body must be valid JSON"}), 400

    features, err = _validate_features(data)
    if err:
        return jsonify({"error": err}), 400

    try:
        result = pipeline.predict(features)

        # Attach AI description only when a problem is detected
        if result["prediction"] == "Problem Detected":
            dtc = result["trouble_code"]
            result["openai_response"] = generate_dtc_description(dtc)
        else:
            result["openai_response"] = None

        return jsonify(result), 200

    except Exception as e:
        return jsonify({"error": f"Prediction failed: {str(e)}"}), 500


# ─── DTC Code lookup ───────────────────────────────────────────────────────────
@app.route('/dtc_code/<string:dtc_code>', methods=['GET'])
def get_dtc_code(dtc_code):
    """
    GET /dtc_code/<CODE>
    Returns structured JSON description for any OBD2 DTC code.
    """
    dtc_code = dtc_code.upper().strip()
    if not dtc_code or len(dtc_code) > 10:
        return jsonify({"error": "Invalid DTC code format"}), 400

    try:
        result = generate_dtc_description(dtc_code)
        if "error" in result:
            return jsonify(result), 500
        return jsonify(result), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500


# ─── Entry point ───────────────────────────────────────────────────────────────
if __name__ == '__main__':
    debug_mode = os.getenv("FLASK_DEBUG", "false").lower() == "true"
    app.run(host='0.0.0.0', port=7860, debug=debug_mode)
