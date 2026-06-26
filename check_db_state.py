import sqlite3, os, datetime

db = r'D:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\FactoryProductDB.db'

print('=== 文件信息 ===')
print('大小:', os.path.getsize(db), 'bytes')
print('修改时间:', datetime.datetime.fromtimestamp(os.path.getmtime(db)))

con = sqlite3.connect(db)
cur = con.cursor()

print()
print('=== 所有表的记录数 ===')
cur.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name")
for (tbl,) in cur.fetchall():
    try:
        cur.execute(f'SELECT COUNT(*) FROM {tbl}')
        print(f'  {tbl}: {cur.fetchone()[0]}')
    except Exception as e:
        print(f'  {tbl}: 无法读取 - {e}')

print()
print('=== 最新工厂记录（按ID倒序，前5条）===')
cur.execute('SELECT id, factory_code, factory_name, created_at FROM Factories ORDER BY id DESC LIMIT 5')
for r in cur.fetchall(): print(' ', r)

print()
print('=== 最新物料记录（按ID倒序，前5条）===')
cur.execute('SELECT id, product_name, factory_product_code, factory_id FROM FactoryProducts ORDER BY id DESC LIMIT 5')
for r in cur.fetchall(): print(' ', r)

con.close()
