import sqlite3
import os

# Path to one of the backups
db_path = r"C:\استعادة منظومة اصدار الشهادات\certificates_backup_20260113_230053.db"

print(f"Inspecting database: {db_path}")

try:
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()

    # List all tables
    print("\n[TABLES]")
    cursor.execute("SELECT name FROM sqlite_master WHERE type='table';")
    tables = cursor.fetchall()
    found_shipments = False
    for t in tables:
        print(f"- {t[0]}")
        if t[0] == 'Shipments':
            found_shipments = True

    if not found_shipments:
        print("WARNING: Table 'Shipments' NOT found.")

    # List all triggers
    print("\n[TRIGGERS]")
    cursor.execute("SELECT name, tbl_name, sql FROM sqlite_master WHERE type='trigger';")
    triggers = cursor.fetchall()
    for t in triggers:
        name, tbl_name, sql = t
        print(f"Trigger: {name} on {tbl_name}")
        if "Shipments" in sql:
            print(f"!!! FOUND 'Shipments' in Trigger SQL: {sql}")

    # Search inside Views as well
    print("\n[VIEWS]")
    cursor.execute("SELECT name, sql FROM sqlite_master WHERE type='view';")
    views = cursor.fetchall()
    for v in views:
        name, sql = v
        print(f"View: {name}")
        if "Shipments" in sql:
             print(f"!!! FOUND 'Shipments' in View SQL: {sql}")

    conn.close()

except Exception as e:
    print(f"Error: {e}")
