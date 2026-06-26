import sqlite3

db_path = 'remote_master.db'

try:
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()
    
    cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
    tables = [t[0] for t in cursor.fetchall()]
    print(f"远程master的表 ({len(tables)}):")
    for t in tables:
        print(f"  - {t}")
    
    if 'ProductCompositeMaterials' in tables:
        cursor.execute("SELECT COUNT(*) FROM ProductCompositeMaterials")
        print(f"\nProductCompositeMaterials: {cursor.fetchone()[0]} 条")
        
        cursor.execute("SELECT * FROM ProductCompositeMaterials")
        rows = cursor.fetchall()
        print("数据:")
        for r in rows:
            print(f"  {r}")
    else:
        print("\nProductCompositeMaterials: 不存在")
    
    conn.close()
except Exception as e:
    print(f"错误: {e}")
