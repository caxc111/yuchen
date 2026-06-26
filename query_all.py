import sqlite3
conn = sqlite3.connect(r"d:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\FactoryProductDB.db")
cursor = conn.cursor()

# 查询 MaterialGroups
cursor.execute("SELECT id, group_code, group_name, category FROM MaterialGroups")
print("=== MaterialGroups ===")
for row in cursor.fetchall():
    print(f"ID={row[0]}, Code={row[1]}, Name={row[2]}, Category={row[3]}")

# 查询 MaterialGroupItems
cursor.execute("SELECT id, group_id, item_name, material_type, is_required FROM MaterialGroupItems")
print("\n=== MaterialGroupItems ===")
for row in cursor.fetchall():
    print(f"ID={row[0]}, GroupId={row[1]}, ItemName={row[2]}, MaterialType={row[3]}, IsRequired={row[4]}")

# 查询 ProductCompositeMaterials 表
cursor.execute("SELECT id, product_id, part_id, component_name, group_code FROM ProductCompositeMaterials LIMIT 5")
print("\n=== ProductCompositeMaterials ===")
for row in cursor.fetchall():
    print(f"ID={row[0]}, ProductId={row[1]}, PartId={row[2]}, ComponentName={row[3]}, GroupCode={row[4]}")

# 查询 ProductCompositeMaterialItems 表
cursor.execute("SELECT id, composite_id, item_name, material_name, material_type FROM ProductCompositeMaterialItems LIMIT 20")
print("\n=== ProductCompositeMaterialItems ===")
for row in cursor.fetchall():
    print(f"ID={row[0]}, CompositeId={row[1]}, ItemName={row[2]}, MaterialName={row[3]}, MaterialType={row[4]}")

conn.close()
