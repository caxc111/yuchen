import sqlite3

OLD_DB = "FactoryProductDB_old.db"
NEW_DB = "FactoryProductDB.db"

old_conn = sqlite3.connect(OLD_DB)
new_conn = sqlite3.connect(NEW_DB)

old_cur = old_conn.cursor()
new_cur = new_conn.cursor()

# Get all user tables from old DB
old_cur.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name")
old_tables = [r[0] for r in old_cur.fetchall()]

print(f"Old DB tables: {old_tables}")

new_cur.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name")
new_tables = [r[0] for r in new_cur.fetchall()]
print(f"New DB tables: {new_tables}")

for table in old_tables:
    if table == "sqlite_sequence":
        continue
    if table not in new_tables:
        print(f"[SKIP] {table}: not in new DB")
        continue

    old_cur.execute(f"PRAGMA table_info({table})")
    old_cols = [r[1] for r in old_cur.fetchall()]

    new_cur.execute(f"PRAGMA table_info({table})")
    new_cols = [r[1] for r in new_cur.fetchall()]

    common_cols = [c for c in old_cols if c in new_cols]
    if not common_cols:
        print(f"[SKIP] {table}: no common columns")
        continue

    cols_str = ", ".join(common_cols)
    placeholders = ", ".join(["?"] * len(common_cols))

    old_cur.execute(f"SELECT {cols_str} FROM {table}")
    rows = old_cur.fetchall()

    if not rows:
        print(f"[OK] {table}: 0 rows to restore")
        continue

    new_cur.execute(f"DELETE FROM {table}")
    insert_sql = f"INSERT INTO {table} ({cols_str}) VALUES ({placeholders})"
    new_cur.executemany(insert_sql, rows)

    print(f"[OK] {table}: restored {len(rows)} rows")

new_conn.commit()
print("Restore complete.")

old_conn.close()
new_conn.close()
