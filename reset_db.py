import sqlite3
import os
import shutil

db_path = os.path.join(os.environ['LOCALAPPDATA'], 'CertificateSystem', 'certificates.db')
settings_path = os.path.join(os.environ['LOCALAPPDATA'], 'CertificateSystem', 'settings.json')
logs_dir = os.path.join(os.environ['LOCALAPPDATA'], 'CertificateSystem', 'Logs')

# Database Reset
if os.path.exists(db_path):
    try:
        conn = sqlite3.connect(db_path)
        cursor = conn.cursor()
        
        tables = ['Samples', 'Certificates', 'AuditLogs', 'ReferralLetters']
        print("Zeroing tables...")
        for table in tables:
            try:
                cursor.execute(f"DELETE FROM {table}")
                print(f"- {table}: Cleared")
            except sqlite3.OperationalError:
                print(f"- {table}: Table missing (Skipped)")

        cursor.execute("DELETE FROM sqlite_sequence")
        cursor.execute("DELETE FROM Users WHERE Id != (SELECT MIN(Id) FROM Users)")
        print("- Users: Reset to primary Admin only.")
        
        conn.commit()
        print("Running VACUUM...")
        cursor.execute("VACUUM")
        conn.close()
        print("✅ Database Reset Complete!")
    except Exception as e:
        print(f"❌ Error during database reset: {e}")
else:
    print(f"ℹ️ Database not found at {db_path} (Skipping)")

# Clear Settings
if os.path.exists(settings_path):
    os.remove(settings_path)
    print("✅ settings.json: Deleted")

# Clear Logs
if os.path.exists(logs_dir):
    shutil.rmtree(logs_dir)
    print("✅ Logs: Directory cleared")

print("\n🏁 FULL FACTORY RESET COMPLETE!")
