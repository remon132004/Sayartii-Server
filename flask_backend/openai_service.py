import os
import openai
from dotenv import load_dotenv

# Load environment variables
load_dotenv()

# Pre-defined DTC dictionary for fallback or context
DTC_DESCRIPTIONS = {
    "C0300": "Rear Speed Sensor Malfunction",
    # Other DTCs can be added here
}

def generate_dtc_description(dtc_code: str) -> str:
    """
    Connects to OpenAI GPT-4o model to fetch meaning, 
    reasons for appearance, and treatments for the specified DTC Code.
    """
    api_key = os.getenv("OPENAI_API_KEY")
    if not api_key:
        return "OpenAI API Key not found. Please set OPENAI_API_KEY in .env file."
        
    client = openai.OpenAI(api_key=api_key)

    prompt = f"""
You are an expert automotive mechanic diagnostic system. 
A vehicle has reported the following Diagnostic Trouble Code (DTC): {dtc_code}.

Please provide a detailed report in plain text, structured exactly like this:
{dtc_code} Trouble Code: Detailed Information
Meaning: [detailed meaning here]
Reasons for Appearance: [reasons here]
Recommended Treatments: [treatments here]
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
        return response.choices[0].message.content.strip()
    except Exception as e:
        return f"Failed to retrieve data from OpenAI: {str(e)}"
