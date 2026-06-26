import sqlite3
import sys
sys.stdout.reconfigure(encoding='utf-8')

conn = sqlite3.connect(r'D:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\FactoryProductDB.db')
cursor = conn.cursor()

# 检查 FactoryProducts 表结构
print("=== FactoryProducts 表结构 ===")
cursor.execute("PRAGMA table_info(FactoryProducts)")
for col in cursor.fetchall():
    print(f"  {col[1]} ({col[2]})")

# 物料资料数量
cursor.execute("SELECT COUNT(*) FROM FactoryProducts")
factory_count = cursor.fetchone()[0]
print(f"\n=== 物料资料 ===")
print(f"总数量: {factory_count}")

# 按分类统计
cursor.execute("""
    SELECT category, COUNT(*) as cnt 
    FROM FactoryProducts 
    WHERE category IS NOT NULL 
    GROUP BY category 
    ORDER BY cnt DESC
""")
print("\n按分类统计:")
for row in cursor.fetchall():
    print(f"  {row[0]}: {row[1]}")

# 显示所有物料
print("\n所有物料资料:")
cursor.execute("""
    SELECT id, factory_material_code, my_material_code, material_name, specification, unit, unit_price, category
    FROM FactoryProducts 
""")
for row in cursor.fetchall():
    print(f"  ID={row[0]}, 物料编码={row[1]}, 我的编码={row[2]}, 名称={row[3]}, 规格={row[4]}, 单位={row[5]}, 单价={row[6]}, 分类={row[7]}")

# 检查 Factories 表
print("\n=== 工厂 (Factories) ===")
cursor.execute("SELECT COUNT(*) FROM Factories")
factory_org_count = cursor.fetchone()[0]
print(f"总数量: {factory_org_count}")

cursor.execute("SELECT id, factory_name, factory_code FROM Factories")
for row in cursor.fetchall():
    print(f"  ID={row[0]}, 名称={row[1]}, 编码={row[2]}")

conn.close()
