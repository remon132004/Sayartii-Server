import os
import openai
import json
from dotenv import load_dotenv

# Load environment variables
load_dotenv()

# Pre-defined DTC dictionary for fallback or context
DTC_DESCRIPTIONS = {
    "C0300": "Rear Speed Sensor Malfunction",
    # Other DTCs can be added here
}

def generate_dtc_description(dtc_code: str) -> dict:
    """
    Connects to OpenAI GPT-4o model to fetch meaning, 
    reasons for appearance, and treatments for the specified DTC Code.
    Returns a dictionary mapping to Flutter's DtcCodeModel.
    """
    api_key = os.getenv("OPENAI_API_KEY")
    if not api_key:
        return {"error": "OpenAI API Key not found. Please set OPENAI_API_KEY in .env file."}
        
    client = openai.OpenAI(api_key=api_key)

    prompt = f"""
You are an expert automotive mechanic diagnostic system. 
A vehicle has reported the following Diagnostic Trouble Code (DTC): {dtc_code}.

Please provide a detailed report structured EXACTLY as a valid JSON object with the following keys. Do NOT include Markdown formatting like ```json. Just raw JSON.
{{
    "dtc_code": "{dtc_code}",
    "critical_level": "High/Medium/Low",
    "description": "Short description of the trouble code",
    "long_description": "Detailed explanation of the problem meaning",
    "reason": ["Reason 1", "Reason 2"],
    "repair": ["Treatment step 1", "Treatment step 2"]
}}
    """

    try:
        response = client.chat.completions.create(
            model="gpt-4o",
            messages=[
                {"role": "system", "content": "You are a specialized car mechanic AI."},
                {"role": "user", "content": prompt}
            ],
            max_tokens=600,
            temperature=0.3
        )
        content = response.choices[0].message.content.strip()
        if content.startswith("```json"):
            content = content[7:-3].strip()
        elif content.startswith("```"):
            content = content[3:-3].strip()
            
        return json.loads(content)
    except Exception as e:
        return {"error": f"Failed to retrieve data from OpenAI: {str(e)}"}
