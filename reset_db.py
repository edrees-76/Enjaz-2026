import sqlite3
import os
import shutil

db_paths = [
    os.path.join(os.environ['LOCALAPPDATA'], 'Enjaz', 'certificates.db'),
    os.path.join(os.getcwd(), 'bin', 'Debug', 'net8.0-windows', 'enjaz.db'),
    os.path.join(os.getcwd(), 'enjaz.db')
]
settings_path = os.path.join(os.environ['LOCALAPPDATA'], 'Enjaz', 'settings.json')
logs_dir = os.path.join(os.environ['LOCALAPPDATA'], 'Enjaz', 'Logs')

# Database Reset
for db_path in db_paths:
    if os.path.exists(db_path):
        try:
            print(f"\nProcessing database: {db_path}")
            conn = sqlite3.connect(db_path)
            cursor = conn.cursor()
            
            tables = ['Samples', 'Certificates', 'AuditLogs', 'ReferralLetters', 'SampleReceptions', 'ReceptionSamples']
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
            print(f"✅ Reset Complete for: {db_path}")
        except Exception as e:
            print(f"❌ Error during database reset for {db_path}: {e}")
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
