import os
import sqlite3
import hashlib

# Update this path to the actual backup created
BACKUP_PATH = r"D:\منظومة انجاز"

def find_latest_backup():
    if not os.path.exists(BACKUP_PATH):
        return None
    subdirs = [os.path.join(BACKUP_PATH, d) for d in os.listdir(BACKUP_PATH) if d.startswith("Backup_")]
    if not subdirs:
        return None
    return max(subdirs, key=os.path.getmtime)

def verify_structure(backup_dir):
    required_dirs = ["SourceCode", "Database", "Documentation", "Environment"]
    print(f"Verifying structure in: {backup_dir}")
    all_ok = True
    for d in required_dirs:
        path = os.path.join(backup_dir, d)
        if os.path.isdir(path):
            print(f"[OK] Directory exists: {d}")
        else:
            print(f"[FAIL] Missing directory: {d}")
            all_ok = False
    return all_ok

def verify_database(backup_dir):
    db_path = os.path.join(backup_dir, "Database", "certificates_backup.db")
    dump_path = os.path.join(backup_dir, "Database", "database_dump.sql")
    
    if not os.path.exists(db_path):
        print(f"[FAIL] Database file missing: {db_path}")
        return False
        
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        cursor.execute("PRAGMA integrity_check;")
        result = cursor.fetchone()[0]
        conn.close()
        
        if result == "ok":
            print(f"[OK] Database integrity check passed.")
        else:
            print(f"[FAIL] Database integrity check failed: {result}")
            return False
            
    except Exception as e:
        print(f"[FAIL] Database check threw exception: {e}")
        return False

    if os.path.exists(dump_path) and os.path.getsize(dump_path) > 0:
        print(f"[OK] SQL Dump exists and is not empty.")
    else:
        print(f"[FAIL] SQL Dump missing or empty.")
        return False
        
    return True

def main():
    latest = find_latest_backup()
    if not latest:
        print("No backup found to verify.")
        return

    print(f"Verifying Backup: {latest}")
    print("-" * 30)
    
    struct_ok = verify_structure(latest)
    db_ok = verify_database(latest)
    
    print("-" * 30)
    if struct_ok and db_ok:
        print("VERIFICATION SUCCESSFUL: Backup is complete and valid.")
    else:
        print("VERIFICATION FAILED: Issues found in backup.")

if __name__ == "__main__":
    main()
