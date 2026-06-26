import sqlite3
conn = sqlite3.connect(r"d:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\FactoryProductDB.db")
cur = conn.cursor()

# 直接查所有material_type_name和material_type列的唯一值
for table, col in [
    ("ProductMaterialLibrary", "material_type_name"),
    ("ProductCompositeMaterials", "material_type_name"),
    ("MaterialGroupItems", "material_type"),
    ("ProductCompositeMaterialItems", "material_type"),
]:
    cur.execute(f"SELECT DISTINCT {col} FROM {table}")
    print(f"\n=== {table}.{col} ===")
    for r in cur.fetchall():
        print(f"  [{r[0]}]")

conn.close()
