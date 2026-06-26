import sqlite3
conn = sqlite3.connect(r'D:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\FactoryProductDB.db')
print("product_id=12 的数据:")
rows = conn.execute("SELECT id, product_id, is_composite, group_code, item_name, material_name, material_type_name FROM ProductPartMaterials WHERE product_id=12 ORDER BY id").fetchall()
for r in rows:
    print(r)
conn.close()
