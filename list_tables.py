import sqlite3
import os

db_path = r'D:\منظومة انجاز 2026\bin\Debug\net8.0-windows\enjaz.db'

if os.path.exists(db_path):
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    cursor.execute("SELECT name FROM sqlite_master WHERE type='table';")
    tables = cursor.fetchall()
    print("Tables in Database:")
    for table in tables:
        print(f"- {table[0]}")
    conn.close()
else:
    print(f"Database not found at {db_path}")
