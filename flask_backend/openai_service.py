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
        print(f"Warning: OPENAI_API_KEY not found. Returning mock response for {dtc_code}.")
        return {
            "dtc_code": dtc_code,
            "critical_level": "High",
            "description": DTC_DESCRIPTIONS.get(dtc_code, "Unknown Error Code"),
            "long_description": "This is a mock diagnostic description generated because the OpenAI API Key is missing. The vehicle has reported a fault code that requires inspection.",
            "driving_advice": "Do not drive until inspected.",
            "reason": ["Sensor malfunction", "Wiring issue", "Missing OpenAI Key"],
            "repair": ["Inspect sensor", "Check wiring connections", "Add OPENAI_API_KEY to environment"]
        }
        
    client = openai.OpenAI(api_key=api_key)

    prompt = f"""
You are an expert automotive mechanic diagnostic system. 
A vehicle has reported the following Diagnostic Trouble Code (DTC): {dtc_code}.

IMPORTANT: You MUST respond entirely in Arabic. 
Do NOT use literal or direct translations. You MUST use the common professional automotive terminology used by mechanics and car experts in the Arab world (e.g., using terms like "بوابة الهواء", "حساس الشكمان", "ثلاجة المحرك", "موبينة", "بوجيهات", etc., where applicable).

Please provide a detailed report structured EXACTLY as a valid JSON object with the following keys. Do NOT include Markdown formatting like ```json. Just raw JSON.
{{
    "dtc_code": "{dtc_code}",
    "critical_level": "High/Medium/Low",
    "description": "Short description of the trouble code in professional Arabic",
    "long_description": "Detailed explanation of the problem meaning in professional Arabic",
    "driving_advice": "Clear, professional advice on whether the driver should stop immediately, drive to the nearest mechanic, or if it can wait. Must be in Arabic.",
    "reason": ["Reason 1 in Arabic", "Reason 2 in Arabic"],
    "repair": ["Treatment step 1 in Arabic", "Treatment step 2 in Arabic"]
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
