import sqlite3

conn = sqlite3.connect('git_recovered.db')
cursor = conn.cursor()

cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
tables = [t[0] for t in cursor.fetchall()]
print(f"Git中的表 ({len(tables)}):")
for t in tables:
    print(f"  - {t}")

if 'ProductCompositeMaterials' in tables:
    cursor.execute("SELECT COUNT(*) FROM ProductCompositeMaterials")
    count = cursor.fetchone()[0]
    print(f"\nProductCompositeMaterials: {count} 条记录")
else:
    print("\nProductCompositeMaterials: 不存在")

# 检查其他关键表
for table in ['Products', 'FactoryProducts', 'MaterialGroups', 'MaterialGroupItems']:
    if table in tables:
        cursor.execute(f"SELECT COUNT(*) FROM {table}")
        count = cursor.fetchone()[0]
        print(f"{table}: {count} 条")

conn.close()
