import sqlite3

db_path = 'git_commit_f898406.db'

try:
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
    tables = [t[0] for t in cursor.fetchall()]
    print(f"表 ({len(tables)}):")
    for t in tables:
        print(f"  - {t}")
    
    if 'ProductCompositeMaterials' in tables:
        cursor.execute("SELECT COUNT(*) FROM ProductCompositeMaterials")
        count = cursor.fetchone()[0]
        print(f"\nProductCompositeMaterials: {count} 条")
    else:
        print("\nProductCompositeMaterials: 不存在")
    
    for table in ['Products', 'FactoryProducts', 'MaterialGroups', 'MaterialGroupItems']:
        if table in tables:
            cursor.execute(f"SELECT COUNT(*) FROM {table}")
            count = cursor.fetchone()[0]
            print(f"{table}: {count} 条")
    
    conn.close()
except Exception as e:
    print(f"错误: {e}")
