import sqlite3
conn = sqlite3.connect(r"d:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\FactoryProductDB.db")
cursor = conn.cursor()

# FactoryProducts 的列
cursor.execute("PRAGMA table_info(FactoryProducts)")
print("=== FactoryProducts columns ===")
for row in cursor.fetchall():
    print(f"{row[1]} ({row[2]})")

# 查询不同 category
cursor.execute("SELECT DISTINCT category FROM FactoryProducts WHERE category IS NOT NULL AND category != ''")
print("\n=== Distinct categories ===")
for row in cursor.fetchall():
    print(f"'{row[0]}'")

# 查询不同 texture
cursor.execute("SELECT DISTINCT texture FROM FactoryProducts WHERE texture IS NOT NULL AND texture != ''")
print("\n=== Distinct textures ===")
for row in cursor.fetchall():
    print(f"'{row[0]}'")

# 查询不同 product_name
cursor.execute("SELECT DISTINCT product_name FROM FactoryProducts WHERE product_name IS NOT NULL AND product_name != '' LIMIT 30")
print("\n=== Distinct product_names (first 30) ===")
for row in cursor.fetchall():
    print(f"'{row[0]}'")

conn.close()
