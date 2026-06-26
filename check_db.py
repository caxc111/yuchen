import sqlite3
import sys

db_path = "D:/BaiduSyncdisk/宇程科技智能家居/编程/宇辰信息中心/FactoryProductManager/FactoryProductDB.db"

try:
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()

    # 查看所有表
    print("=== 数据库表 ===")
    cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
    tables = cursor.fetchall()
    table_names = [t[0] for t in tables]
    for t in table_names:
        print(f"  - {t}")

    # 检查 ProductCompositeMaterials 表
    print("\n=== ProductCompositeMaterials 数据 ===")
    if 'ProductCompositeMaterials' in table_names:
        cursor.execute("SELECT * FROM ProductCompositeMaterials ORDER BY id")
        rows = cursor.fetchall()
        print(f"共 {len(rows)} 条记录:")
        for r in rows:
            print(f"  {r}")
    else:
        print("表不存在!")

    # 检查 ProductCompositeMaterialItems 表
    print("\n=== ProductCompositeMaterialItems 数据 ===")
    if 'ProductCompositeMaterialItems' in table_names:
        cursor.execute("SELECT * FROM ProductCompositeMaterialItems ORDER BY id")
        rows = cursor.fetchall()
        print(f"共 {len(rows)} 条记录:")
        for r in rows:
            print(f"  {r}")
    else:
        print("表不存在!")

    # 检查 Products 表
    print("\n=== Products 数据 ===")
    cursor.execute("SELECT id, product_code, house_type FROM Products ORDER BY id")
    rows = cursor.fetchall()
    print(f"共 {len(rows)} 条记录:")
    for r in rows:
        print(f"  id={r[0]}, code={r[1]}, house_type={r[2]}")

    # 检查 MaterialGroups 表
    print("\n=== MaterialGroups 数据 ===")
    cursor.execute("SELECT * FROM MaterialGroups ORDER BY id")
    rows = cursor.fetchall()
    print(f"共 {len(rows)} 条记录:")
    for r in rows:
        print(f"  {r}")

    # 检查 MaterialGroupItems 表
    print("\n=== MaterialGroupItems 数据 ===")
    cursor.execute("SELECT * FROM MaterialGroupItems ORDER BY id")
    rows = cursor.fetchall()
    print(f"共 {len(rows)} 条记录:")
    for r in rows:
        print(f"  {r}")

    conn.close()
except Exception as e:
    print(f"错误: {e}")
    import traceback
    traceback.print_exc()
