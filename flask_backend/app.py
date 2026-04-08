from flask import Flask, request, jsonify
from models_pipeline import PredictionPipeline
from openai_service import generate_dtc_description

app = Flask(__name__)
pipeline = PredictionPipeline()

@app.route('/predict', methods=['POST'])
def predict():
    """
    Endpoint: POST /predict
    Expected JSON Body:
    {
       "engine_power": 120.0,
       "engine_coolant_temp": 95.0,
       "engine_load": 40.0,
       ...
    }
    """
    try:
        data = request.get_json()
        if not data:
            return jsonify({"error": "No JSON provided"}), 400

        # Extract features in exact order
        features = [
            float(data.get('engine_power', 0)),
            float(data.get('engine_coolant_temp', 0)),
            float(data.get('engine_load', 0)),
            float(data.get('engine_rpm', 0)),
            float(data.get('air_intake_temp', 0)),
            float(data.get('speed', 0)),
            float(data.get('short_term_fuel_trim', 0)),
            float(data.get('throttle_pos', 0)),
            float(data.get('timing_advance', 0))
        ]

        # Machine Learning Pipeline Inference
        prediction_result = pipeline.predict(features)

        # Include OpenAI context if there's a problem
        if prediction_result["prediction"] == "Problem Detected":
            dtc = prediction_result["trouble_code"]
            ai_description = generate_dtc_description(dtc)
            prediction_result["openai_response"] = ai_description
        else:
            prediction_result["openai_response"] = "Vehicle is running normally. No issues detected."

        return jsonify(prediction_result), 200

    except Exception as e:
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    # Start the Flask API on port 5000
    app.run(host='0.0.0.0', port=5000, debug=True)
