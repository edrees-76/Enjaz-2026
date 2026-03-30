import os
import shutil
import sqlite3
import datetime
import subprocess
import platform
import sys

# Configuration
SOURCE_DIR = r"D:\منظومة شهادات جديدة 2026 جديدة"
BACKUP_ROOT = r"D:\منظومة انجاز"
DB_PATH = os.path.expandvars(r"%LOCALAPPDATA%\CertificateSystem\certificates.db")
IGNORE_PATTERNS = shutil.ignore_patterns('bin', 'obj', '.git', '.vs', '.gemini', 'tmp', 'TestSprite', '__pycache__', '*.lock', '*.log')

def create_backup_structure():
    timestamp = datetime.datetime.now().strftime("%Y-%m-%d_%H-%M-%S")
    backup_dir = os.path.join(BACKUP_ROOT, f"Backup_{timestamp}")
    
    dirs = {
        "root": backup_dir,
        "source": os.path.join(backup_dir, "SourceCode"),
        "db": os.path.join(backup_dir, "Database"),
        "docs": os.path.join(backup_dir, "Documentation"),
        "env": os.path.join(backup_dir, "Environment")
    }
    
    for d in dirs.values():
        os.makedirs(d, exist_ok=True)
        
    return dirs

def backup_source_code(dirs):
    print("Backing up source code...")
    try:
        # shutil.copytree requires destination to not exist usually, 
        # but here we created 'source' dir. 
        # Let's copy contents of SOURCE_DIR to dirs['source']
        
        # We need to copy recursively but skip ignored patterns.
        # Since shutil.copytree creates the dir, we might want to iterate or use copytree on the parent if possible.
        # Better approach: Use copytree for the whole folder to a new path inside SourceCode
        
        # Actually, let's remove the empty directory created by makedirs if we want to use copytree directly,
        # OR use copytree with dirs_exist_ok=True (Python 3.8+)
        
        shutil.copytree(SOURCE_DIR, 
                       files_dest := os.path.join(dirs["source"], "ItaqanSystem"), 
                       ignore=IGNORE_PATTERNS, 
                       dirs_exist_ok=True)
        print(f"Source code backed up to: {files_dest}")
    except Exception as e:
        print(f"Error backing up source code: {e}")

def backup_database(dirs):
    print("Backing up database...")
    
    # 1. File Copy
    if os.path.exists(DB_PATH):
        try:
            dest_file = os.path.join(dirs["db"], "certificates_backup.db")
            shutil.copy2(DB_PATH, dest_file)
            print(f"Database file copied to: {dest_file}")
            
            # 2. SQL Dump
            dump_file = os.path.join(dirs["db"], "database_dump.sql")
            conn = sqlite3.connect(dest_file) # Connect to the backup copy to avoid locking live DB
            with open(dump_file, 'w', encoding='utf-8') as f:
                for line in conn.iterdump():
                    f.write(f'{line}\n')
            conn.close()
            print(f"Database dump created at: {dump_file}")
            
        except Exception as e:
            print(f"Error backing up database: {e}")
    else:
        print(f"WARNING: Database file not found at {DB_PATH}")

def save_environment_info(dirs):
    print("جاري حفظ معلومات البيئة...")
    info_file = os.path.join(dirs["env"], "System_Info.txt")
    
    with open(info_file, 'w', encoding='utf-8') as f:
        f.write("=== معلومات النظام ===\n")
        f.write(f"نظام التشغيل: {platform.system()} {platform.release()} ({platform.version()})\n")
        f.write(f"الجهاز: {platform.machine()}\n")
        f.write(f"تاريخ النسخ الاحتياطي: {datetime.datetime.now()}\n\n")
        
        f.write("=== إصدار Dotnet ===\n")
        try:
            dotnet_ver = subprocess.check_output(["dotnet", "--version"], encoding='utf-8').strip()
            f.write(f"Dotnet SDK: {dotnet_ver}\n")
        except:
            f.write("Dotnet SDK: غير موجود أو تعذر الحصول على الإصدار\n")
            
        f.write("\n=== إصدار Python ===\n")
        f.write(f"Python: {sys.version}\n")

def generate_restore_instruction(dirs):
    print("جاري إنشاء تعليمات الاستعادة...")
    readme_path = os.path.join(dirs["root"], "Restore_Guide.md")
    
    content = """# دليل استعادة منظومة انجاز

## 1. المتطلبات الأساسية
- **نظام التشغيل**: Windows 10/11
- **بيئة التشغيل**: .NET Desktop Runtime 6.0 أو أحدث.
- **قاعدة البيانات**: تستخدم المنظومة SQLite، ولا تتطلب تثبيت خادم خارجي.

## 2. استعادة الكود المصدري
1. انسخ المجلد `SourceCode/ItaqanSystem` إلى موقع التطوير المطلوب (مثلاً: `D:\\Projects\\Itaqan`).
2. افتح ملف الحل `CertificateSystem.sln` باستخدام Visual Studio.
3. قم باستعادة حزم NuGet (Restore Packages) وقم ببناء الحل (Build).

## 3. استعادة قاعدة البيانات
يبحث التطبيق عن قاعدة البيانات في المسار `%LOCALAPPDATA%\\CertificateSystem\\certificates.db`.

### الخيار أ: الاستعادة اليدوية
1. انتقل إلى المسار `C:\\Users\\[اسم_المستخدم]\\AppData\\Local\\CertificateSystem`.
   - إذا لم يكن المجلد موجوداً، قم بإنشائه.
2. انسخ الملف `Database/certificates_backup.db` من هذه النسخة الاحتياطية.
3. الصقه في المجلد المذكور أعلاه وأعد تسميته إلى `certificates.db`.

### الخيار ب: استخدام التفريغ النصي (SQL Dump)
1. إذا كنت بحاجة إلى إعادة بناء قاعدة البيانات من الصفر:
2. قم بإنشاء ملف قاعدة بيانات SQLite جديد.
3. نفذ أوامر SQL الموجودة في الملف `Database/database_dump.sql`.

## 4. واجهات المستخدم والإعدادات
- جميع أصول البرمجية (Assets) مضمنة أو موجودة داخل مجلد `SourceCode`.
- ملفات الإعداد (`appsettings.json` أو `App.config`) موجودة ضمن الكود المصدري.

"""
    with open(readme_path, 'w', encoding='utf-8') as f:
        f.write(content)
    print(f"تم إنشاء دليل الاستعادة في: {readme_path}")

def main():
    print("Starting System Backup...")
    
    # Check source existence
    if not os.path.exists(SOURCE_DIR):
        print(f"CRITICAL ERROR: Source directory not found: {SOURCE_DIR}")
        return

    dirs = create_backup_structure()
    
    backup_source_code(dirs)
    backup_database(dirs)
    save_environment_info(dirs)
    generate_restore_instruction(dirs)
    
    print("\nBackup Completed Successfully!")
    print(f"Location: {dirs['root']}")

if __name__ == "__main__":
    main()
