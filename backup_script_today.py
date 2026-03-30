import os
import shutil
import datetime
from pathlib import Path

# Paths
SOURCE_DIR = r"d:\منظومة انجاز 2026"
DEST_ROOT = r"D:\نسخة احتياطبة منظومة انجاز"
DB_SOURCE = os.path.expandvars(r"%LOCALAPPDATA%\CertificateSystem\certificates.db")

# Exclusions
IGNORE_PATTERNS = shutil.ignore_patterns(
    'bin', 'obj', '.git', '.vs', '.gemini', 'tmp', 'temp_decompile',
    'PerformanceTests', 'QA_Simulation', '__pycache__', '*.log'
)

def perform_backup():
    print("--- البدء في عملية النسخ الاحتياطي ---")
    today = datetime.datetime.now().strftime("%Y-%m-%d")
    backup_name = f"{today}_كامل_منظومة_انجاز"
    backup_path = os.path.join(DEST_ROOT, backup_name)
    
    # Create main backup folder
    if not os.path.exists(backup_path):
        os.makedirs(backup_path)
    
    # 1. Source Code Backup
    source_dest = os.path.join(backup_path, "SourceCode")
    print(f"جاري نسخ الكود المصدري إلى: {source_dest}")
    shutil.copytree(SOURCE_DIR, source_dest, ignore=IGNORE_PATTERNS, dirs_exist_ok=True)
    
    # 2. Database Backup
    db_dest_folder = os.path.join(backup_path, "Database")
    if not os.path.exists(db_dest_folder):
        os.makedirs(db_dest_folder)
        
    if os.path.exists(DB_SOURCE):
        print(f"جاري نسخ قاعدة البيانات...")
        shutil.copy2(DB_SOURCE, os.path.join(db_dest_folder, "certificates.db"))
    else:
        print("تحذير: لم يتم العثور على قاعدة البيانات في المسار المعتاد.")
        
    # 3. Final Check
    print(f"\n✅ تمت عملية النسخ الاحتياطي بنجاح في: {backup_path}")

if __name__ == "__main__":
    perform_backup()
