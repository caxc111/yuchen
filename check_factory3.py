import sqlite3
import sys
sys.stdout.reconfigure(encoding='utf-8')

conn = sqlite3.connect(r'D:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\FactoryProductDB.db')
cursor = conn.cursor()

# 物料资料数量
cursor.execute("SELECT COUNT(*) FROM FactoryProducts")
factory_count = cursor.fetchone()[0]
print(f"=== 物料资料 (FactoryProducts) ===")
print(f"总数量: {factory_count}")

# 显示所有物料
print("\n所有物料资料:")
cursor.execute("""
    SELECT id, factory_product_code, my_product_code, product_name, brand, specification, unit, cost_price, category
    FROM FactoryProducts 
""")
for row in cursor.fetchall():
    print(f"  ID={row[0]}, 物料编码={row[1]}, 我的编码={row[2]}, 名称={row[3]}, 品牌={row[4]}, 规格={row[5]}, 单位={row[6]}, 成本价={row[7]}, 分类={row[8]}")

# 检查 Factories 表
print("\n=== 工厂 (Factories) ===")
cursor.execute("SELECT COUNT(*) FROM Factories")
factory_org_count = cursor.fetchone()[0]
print(f"总数量: {factory_org_count}")

cursor.execute("SELECT id, factory_name, factory_code FROM Factories")
for row in cursor.fetchall():
    print(f"  ID={row[0]}, 名称={row[1]}, 编码={row[2]}")

conn.close()
