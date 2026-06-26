import sqlite3
import sys
sys.stdout.reconfigure(encoding='utf-8')

conn = sqlite3.connect(r'D:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\FactoryProductDB.db')
cursor = conn.cursor()

# 工厂资料数量
cursor.execute("SELECT COUNT(*) FROM FactoryProducts")
factory_count = cursor.fetchone()[0]
print(f"=== 工厂资料 (FactoryProducts) ===")
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

# 显示前20条物料
print("\n前20条物料资料:")
cursor.execute("""
    SELECT id, material_code, material_name, specification, unit, unit_price, category
    FROM FactoryProducts 
    LIMIT 20
""")
for row in cursor.fetchall():
    print(f"  ID={row[0]}, Code={row[1]}, Name={row[2]}, Spec={row[3]}, Unit={row[4]}, Price={row[5]}, Cat={row[6]}")

# 检查 Factories 表
print("\n=== 工厂 (Factories) ===")
cursor.execute("SELECT COUNT(*) FROM Factories")
factory_org_count = cursor.fetchone()[0]
print(f"总数量: {factory_org_count}")

cursor.execute("SELECT id, factory_name, factory_code FROM Factories")
for row in cursor.fetchall():
    print(f"  ID={row[0]}, Name={row[1]}, Code={row[2]}")

conn.close()
