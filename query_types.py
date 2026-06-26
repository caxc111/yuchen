import sqlite3
conn = sqlite3.connect(r"d:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\FactoryProductDB.db")
cur = conn.cursor()
# 查看FactoryMaterial表的所有类型
cur.execute("SELECT DISTINCT MaterialType FROM FactoryMaterial ORDER BY MaterialType")
types = [r[0] for r in cur.fetchall()]
print("=== 物料类型列表 ===")
for i, t in enumerate(types, 1):
    print(f"{i}. {t}")
conn.close()
