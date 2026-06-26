import sqlite3
conn = sqlite3.connect(r"d:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\FactoryProductDB.db")
cursor = conn.cursor()

# MaterialGroups 的列
cursor.execute("PRAGMA table_info(MaterialGroups)")
print("=== MaterialGroups columns ===")
for row in cursor.fetchall():
    print(f"{row[1]} ({row[2]})")

# MaterialGroupItems 的列
cursor.execute("PRAGMA table_info(MaterialGroupItems)")
print("\n=== MaterialGroupItems columns ===")
for row in cursor.fetchall():
    print(f"{row[1]} ({row[2]})")

conn.close()
