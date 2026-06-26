import sqlite3
conn = sqlite3.connect(r"d:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\FactoryProductDB.db")
cursor = conn.cursor()

# 查询 MaterialGroups
cursor.execute("SELECT Id, GroupCode, GroupName, GroupType, SelectedMaterialsJson FROM MaterialGroups LIMIT 10")
print("=== MaterialGroups ===")
for row in cursor.fetchall():
    print(f"ID={row[0]}, Code={row[1]}, Name={row[2]}, Type={row[3]}, SelectedMaterialsJson={row[4][:100] if row[4] else 'NULL'}...")

# 查询 MaterialGroupItems
cursor.execute("SELECT Id, GroupId, ItemName, MaterialType, IsRequired FROM MaterialGroupItems LIMIT 20")
print("\n=== MaterialGroupItems ===")
for row in cursor.fetchall():
    print(f"ID={row[0]}, GroupId={row[1]}, ItemName={row[2]}, MaterialType={row[3]}, IsRequired={row[4]}")

conn.close()
