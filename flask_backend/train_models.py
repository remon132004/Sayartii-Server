import os
import pandas as pd
import numpy as np
import joblib
from sklearn.ensemble import RandomForestClassifier, ExtraTreesRegressor
from sklearn.model_selection import train_test_split
from sklearn.metrics import accuracy_score, mean_squared_error

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
MODELS_DIR = os.path.join(BASE_DIR, 'saved_models')
os.makedirs(MODELS_DIR, exist_ok=True)

def preprocess_value(val):
    if pd.isna(val) or val == "": 
        return 0.0
    val_str = str(val).replace(',', '.')
    val_str = val_str.replace('%', '')
    try:
        return float(val_str)
    except:
        return 0.0

def train_pipeline(data_path: str):
    print(f"Loading data from {data_path}...")
    df = pd.read_csv(data_path)

    # Clean percentages and decimal commas
    df['engine_load'] = df['ENGINE_LOAD'].apply(preprocess_value)
    df['throttle_pos'] = df['THROTTLE_POS'].apply(preprocess_value)
    df['engine_power'] = df['ENGINE_POWER'].apply(preprocess_value)
    df['timing_advance'] = df['TIMING_ADVANCE'].apply(preprocess_value)
    df['short_term_fuel_trim'] = df['SHORT TERM FUEL TRIM BANK 1'].apply(preprocess_value)
    
    # Simple casts for clean numeric features
    df['engine_coolant_temp'] = df['ENGINE_COOLANT_TEMP'].fillna(0).astype(float)
    df['engine_rpm'] = df['ENGINE_RPM'].fillna(0).astype(float)
    df['air_intake_temp'] = df['AIR_INTAKE_TEMP'].fillna(0).astype(float)
    df['speed'] = df['SPEED'].fillna(0).astype(float)

    # Troubles/Targets preprocessing
    # Target: normal -> 0, any DTC -> 1
    # df['TROUBLE_CODES'] is empty/NaN if normal, else has DTC strings
    df['status'] = df['TROUBLE_CODES'].apply(lambda x: 0 if pd.isna(x) or str(x).strip() == "" else 1)
    
    # Fill DTC Codes explicitly
    df['dtc_code'] = df['TROUBLE_CODES'].fillna('None')

    # Since the Korean dataset doesn't have an "Estimated Hours" label explicitly documented,
    # we simulate the estimated hours for vehicles breaking down to satisfy the Model 3 training label.
    # In a real environment, this is collected via ground-truth survival analysis data.
    np.random.seed(42)
    df['estimated_hours'] = np.where(df['status'] == 1, 
                                     np.random.uniform(0.5, 48.0, size=len(df)), 
                                     0.0)

    # 1. Preprocessing (Interpolation)
    feature_cols = ['engine_power', 'engine_coolant_temp', 'engine_load', 'engine_rpm', 
                    'air_intake_temp', 'speed', 'short_term_fuel_trim', 'throttle_pos', 'timing_advance']
    
    X = df[feature_cols]
    y_binary = df['status']

    # --- Phase 1: Binary Classification ---
    print("Training Binary Classifier (Random Forest)...")
    X_train_bin, X_test_bin, y_train_bin, y_test_bin = train_test_split(X, y_binary, test_size=0.2, random_state=42)
    binary_clf = RandomForestClassifier(n_estimators=100, random_state=42)
    binary_clf.fit(X_train_bin, y_train_bin)
    
    bin_preds = binary_clf.predict(X_test_bin)
    print(f"Binary Classifier Accuracy: {accuracy_score(y_test_bin, bin_preds):.4f}")
    joblib.dump(binary_clf, os.path.join(MODELS_DIR, 'binary_classifier.pkl'))

    # Filter out Normal vehicles for next models
    problem_df = df[df['status'] == 1].copy()

    # --- Phase 2: Multi-Class ---
    if len(problem_df) > 0:
        print("Training Multi-class Classifier (Random Forest) on failing units...")
        X_problem = problem_df[feature_cols]
        y_dtc = problem_df['dtc_code']
        
        # We need at least 2 samples per class to cross-val or test split reliably. 
        # So we stratify selectively or just use simple split.
        X_train_dtc, X_test_dtc, y_train_dtc, y_test_dtc = train_test_split(X_problem, y_dtc, test_size=0.2, random_state=42)
        dtc_clf = RandomForestClassifier(n_estimators=100, random_state=42)
        dtc_clf.fit(X_train_dtc, y_train_dtc)
        
        dtc_preds = dtc_clf.predict(X_test_dtc)
        print(f"DTC Multi-Class Accuracy: {accuracy_score(y_test_dtc, dtc_preds):.4f}")
        joblib.dump(dtc_clf, os.path.join(MODELS_DIR, 'multiclass_classifier.pkl'))

        # --- Phase 3: Remaining Hours Regression (ExtraTreesRegressor) ---
        print("Training Estimated Time Regressor (ExtraTreesRegressor)...")
        y_hours = problem_df['estimated_hours']
        
        X_train_reg, X_test_reg, y_train_reg, y_test_reg = train_test_split(X_problem, y_hours, test_size=0.2, random_state=42)
        reg_model = ExtraTreesRegressor(n_estimators=100, random_state=42)
        reg_model.fit(X_train_reg, y_train_reg)
        
        reg_preds = reg_model.predict(X_test_reg)
        print(f"Regressor Mean Squared Error: {mean_squared_error(y_test_reg, reg_preds):.4f}")
        joblib.dump(reg_model, os.path.join(MODELS_DIR, 'extra_trees_regressor.pkl'))
    else:
        print("No trouble codes found in dataset to train Multiclass/Regressor fallback!")

    print("All models successfully trained and exported!")

if __name__ == "__main__":
    train_pipeline('dataset.csv')
