import sqlite3

db_path = r"d:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\FactoryProductDB.db"
conn = sqlite3.connect(db_path)
cur = conn.cursor()

# 列出所有表
print("=== 所有表 ===")
cur.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name")
for row in cur.fetchall():
    print(row[0])

# 检查 ProductMaterials 表结构
print("\n=== ProductMaterials 表结构 ===")
cur.execute("PRAGMA table_info(ProductMaterials)")
for row in cur.fetchall():
    print(row)

# 检查 Products 表结构
print("\n=== Products 表结构 ===")
cur.execute("PRAGMA table_info(Products)")
for row in cur.fetchall():
    print(row)

# 检查是否有 Products 表之外的产品相关表
print("\n=== 查找产品相关表 ===")
cur.execute("SELECT name FROM sqlite_master WHERE type='table' AND name LIKE '%Product%' ORDER BY name")
for row in cur.fetchall():
    print(row[0])

conn.close()
