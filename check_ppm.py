import sqlite3
import sys
sys.stdout.reconfigure(encoding='utf-8')

conn = sqlite3.connect(r'D:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\FactoryProductDB.db')
cursor = conn.cursor()

# 检查 ProductPartMaterials 表结构
print("=== ProductPartMaterials 表结构 ===")
cursor.execute("PRAGMA table_info(ProductPartMaterials)")
for col in cursor.fetchall():
    print(f"  {col[1]} ({col[2]})")

# 检查有多少记录
cursor.execute("SELECT COUNT(*) FROM ProductPartMaterials")
print(f"\n总记录数: {cursor.fetchone()[0]}")

# 检查有 group_code 的记录（复合物料）
cursor.execute("SELECT COUNT(*) FROM ProductPartMaterials WHERE group_code IS NOT NULL AND group_code != ''")
print(f"有 group_code 的记录: {cursor.fetchone()[0]}")

# 列出所有 group_code
cursor.execute("SELECT DISTINCT group_code FROM ProductPartMaterials WHERE group_code IS NOT NULL AND group_code != ''")
groups = cursor.fetchall()
print(f"\nGroup codes: {[g[0] for g in groups]}")

# 查看前10条记录
print("\n前10条记录:")
cursor.execute("""
    SELECT id, product_id, material_name, group_code, item_name, is_composite 
    FROM ProductPartMaterials 
    LIMIT 10
""")
for row in cursor.fetchall():
    print(f"  {row}")

# 检查 Products 表
print("\n=== Products 表 ===")
cursor.execute("SELECT COUNT(*) FROM Products")
print(f"Products 记录数: {cursor.fetchone()[0]}")

cursor.execute("SELECT id, product_code, product_name FROM Products")
for row in cursor.fetchall():
    print(f"  {row}")

conn.close()
