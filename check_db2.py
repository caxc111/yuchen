import sqlite3

db_path = r"d:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\FactoryProductDB.db"
conn = sqlite3.connect(db_path)
cur = conn.cursor()

# 检查是否有 Projects 表
print("=== 查找项目相关表 ===")
cur.execute("SELECT name FROM sqlite_master WHERE type='table' AND (name LIKE '%Project%' OR name LIKE '%project%') ORDER BY name")
rows = cur.fetchall()
if rows:
    for row in rows:
        print(row[0])
else:
    print("没有找到 Project 相关表")

# 查看 Products 表的数据
print("\n=== Products 表的数据示例 ===")
cur.execute("SELECT id, product_code, project_name FROM Products LIMIT 10")
for row in cur.fetchall():
    print(row)

# 查看 ProductMaterials 表的数据示例
print("\n=== ProductMaterials 表的数据示例 ===")
cur.execute("SELECT id, product_id, material_name, drawing_number FROM ProductMaterials LIMIT 10")
for row in cur.fetchall():
    print(row)

# 检查是否有 product_code 和 drawing_number 的关联
print("\n=== Products 和 ProductMaterials 关联检查 ===")
cur.execute("""
    SELECT p.id, p.product_code, p.project_name, pm.material_name, pm.drawing_number
    FROM Products p
    LEFT JOIN ProductMaterials pm ON p.id = pm.product_id
    WHERE pm.drawing_number IS NOT NULL
    LIMIT 10
""")
for row in cur.fetchall():
    print(row)

conn.close()
