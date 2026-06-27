import os
import json
import openai
from functools import lru_cache
from dotenv import load_dotenv

# Load environment variables
load_dotenv()

# ─── Fallback descriptions for common DTCs (Arabic + English) ────────────────
DTC_FALLBACK: dict[str, dict] = {
    "P0300": {"ar": "اضطراب عشوائي في الاشتعال",           "en": "Random/Multiple Cylinder Misfire"},
    "P0301": {"ar": "اضطراب في اشتعال الأسطوانة الأولى",   "en": "Cylinder 1 Misfire"},
    "P0171": {"ar": "خليط وقود رقيق جداً — بنك 1",         "en": "Fuel System Too Lean (Bank 1)"},
    "P0420": {"ar": "كفاءة منخفضة لمحفز العادم",            "en": "Catalyst System Efficiency Below Threshold"},
    "P0505": {"ar": "عطل في نظام التحكم بسرعة الخمول",      "en": "Idle Air Control System Malfunction"},
    "C0300": {"ar": "عطل في حساس سرعة العجلة الخلفية",     "en": "Rear Wheel Speed Sensor Malfunction"},
    "P0113": {"ar": "إشارة عالية من حساس درجة هواء المدخل", "en": "IAT Sensor Circuit High Input"},
    "P0128": {"ar": "حرارة التبريد أقل من الطبيعي",         "en": "Coolant Temp Below Thermostat Regulating Temperature"},
}


@lru_cache(maxsize=128)
def generate_dtc_description(dtc_code: str) -> dict:
    """
    Returns a bilingual (Arabic + English) structured diagnostic report for a given DTC code.
    Every text field is duplicated with _ar and _en suffixes so the Flutter client
    can display the correct language based on device locale.

    Results are cached in memory (LRU, max 128 entries) to avoid redundant API calls.
    """
    api_key = os.getenv("OPENAI_API_KEY")

    if not api_key:
        print(f"[OpenAI] WARNING: OPENAI_API_KEY not found. Returning fallback for {dtc_code}.")
        return _fallback_response(dtc_code)

    client = openai.OpenAI(api_key=api_key)

    prompt = f"""You are an expert automotive diagnostic system for a bilingual Arabic/English mobile app called Sayartii.
A vehicle has reported DTC code: {dtc_code}

Generate a bilingual JSON diagnostic report. Every text field must appear TWICE — once in Arabic (_ar) and once in English (_en).
Use professional Arabic automotive terminology (e.g., بوجيهات، حساس الشكمان، ثلاجة المحرك، بوابة الهواء، موبينة).
Do NOT include markdown code blocks. Return raw JSON only.

{{
    "dtc_code": "{dtc_code}",
    "critical_level": "High" | "Medium" | "Low",
    "description_ar": "وصف قصير للعطل باللغة العربية (15-30 كلمة)",
    "description_en": "Short English description of the fault (15-30 words)",
    "long_description_ar": "شرح تفصيلي للعطل الميكانيكي باللغة العربية (50-100 كلمة)",
    "long_description_en": "Detailed English explanation of the mechanical fault (50-100 words)",
    "driving_advice_ar": "نصيحة قيادة احترافية بالعربية: هل يجب إيقاف السيارة فوراً / التوجه للميكانيكي / الانتظار (30-50 كلمة)",
    "driving_advice_en": "Professional English driving advice: whether to stop immediately / drive to mechanic / can wait (30-50 words)",
    "reason_ar": ["السبب الأول بالعربية", "السبب الثاني بالعربية", "السبب الثالث بالعربية"],
    "reason_en": ["First reason in English", "Second reason in English", "Third reason in English"],
    "repair_ar": ["خطوة الإصلاح الأولى بالعربية", "خطوة الإصلاح الثانية بالعربية", "خطوة الإصلاح الثالثة بالعربية"],
    "repair_en": ["First repair step in English", "Second repair step in English", "Third repair step in English"]
}}"""

    try:
        response = client.chat.completions.create(
            model="gpt-4o-mini",       # Faster + cheaper than gpt-4o, same quality for this task
            messages=[
                {
                    "role": "system",
                    "content": (
                        "You are a specialized automotive diagnostic AI for a bilingual app. "
                        "Always respond with raw JSON only — no markdown, no code fences. "
                        "Every descriptive field must have both _ar (Arabic) and _en (English) variants."
                    ),
                },
                {"role": "user", "content": prompt},
            ],
            max_tokens=1100,
            temperature=0.2,           # Low temp = consistent, deterministic output
            response_format={"type": "json_object"},  # Force JSON (gpt-4o-mini supports this)
        )

        content = response.choices[0].message.content.strip()

        # Strip markdown fences if present (defensive)
        if content.startswith("```json"):
            content = content[7:].rstrip("```").strip()
        elif content.startswith("```"):
            content = content[3:].rstrip("```").strip()

        parsed = json.loads(content)

        # Validate required keys are present
        required_keys = {
            "dtc_code", "critical_level",
            "description_ar", "description_en",
            "long_description_ar", "long_description_en",
            "reason_ar", "reason_en",
            "repair_ar", "repair_en",
        }
        missing = required_keys - set(parsed.keys())
        if missing:
            print(f"[OpenAI] Response missing keys: {missing}. Using fallback.")
            return _fallback_response(dtc_code)

        return parsed

    except json.JSONDecodeError as e:
        print(f"[OpenAI] JSON decode error for {dtc_code}: {e}")
        return _fallback_response(dtc_code)
    except openai.RateLimitError:
        print(f"[OpenAI] Rate limit hit for {dtc_code}. Using fallback.")
        return _fallback_response(dtc_code)
    except openai.APIError as e:
        print(f"[OpenAI] API error for {dtc_code}: {e}")
        return {"error": f"OpenAI API error: {str(e)}"}
    except Exception as e:
        print(f"[OpenAI] Unexpected error for {dtc_code}: {e}")
        return {"error": f"Failed to retrieve data: {str(e)}"}


def _fallback_response(dtc_code: str) -> dict:
    """Returns a bilingual pre-written fallback response when OpenAI is unavailable."""
    fallback = DTC_FALLBACK.get(dtc_code.upper(), {})
    ar_desc = fallback.get("ar", "كود عطل غير معروف — يرجى مراجعة الميكانيكي")
    en_desc = fallback.get("en", "Unknown fault code — please consult a mechanic")

    return {
        "dtc_code": dtc_code,
        "critical_level": "Medium",
        "description_ar": ar_desc,
        "description_en": en_desc,
        "long_description_ar": (
            f"الكود {dtc_code} يشير إلى عطل في إحدى وحدات التحكم بالسيارة. "
            "يُنصح بفحص السيارة في أقرب وقت لدى متخصص."
        ),
        "long_description_en": (
            f"Code {dtc_code} indicates a fault in one of the vehicle's control modules. "
            "It is recommended to have the vehicle inspected by a specialist as soon as possible."
        ),
        "driving_advice_ar": (
            "لا تتجاهل هذا التحذير. "
            "توجه لأقرب ميكانيكي لفحص السيارة في أسرع وقت ممكن."
        ),
        "driving_advice_en": (
            "Do not ignore this warning. "
            "Head to the nearest mechanic to have the vehicle inspected as soon as possible."
        ),
        "reason_ar": [
            "تلف في أحد الحساسات الإلكترونية",
            "خلل في وحدة التحكم الإلكترونية (ECU)",
            "مشكلة في الأسلاك الكهربائية",
        ],
        "reason_en": [
            "Damage to one of the electronic sensors",
            "Malfunction in the Electronic Control Unit (ECU)",
            "Electrical wiring issue",
        ],
        "repair_ar": [
            "فحص الكود بجهاز OBD2 لتأكيد التشخيص",
            "فحص حالة الحساسات المرتبطة بالكود",
            "مراجعة الميكانيكي المختص للإصلاح النهائي",
        ],
        "repair_en": [
            "Scan the code with an OBD2 device to confirm the diagnosis",
            "Inspect the condition of sensors related to the code",
            "Consult a specialist mechanic for the final repair",
        ],
    }
