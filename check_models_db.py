import sqlite3
import sys

db_path = r"D:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\Models\FactoryProductDB.db"

print(f"检查数据库: {db_path}")

try:
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()

    # 查看所有表
    cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
    tables = cursor.fetchall()
    table_names = [t[0] for t in tables]
    print(f"\n所有表 ({len(table_names)}):")
    for t in table_names:
        print(f"  - {t}")

    # 检查 ProductCompositeMaterials
    if 'ProductCompositeMaterials' in table_names:
        cursor.execute("SELECT COUNT(*) FROM ProductCompositeMaterials")
        count = cursor.fetchone()[0]
        print(f"\nProductCompositeMaterials: {count} 条记录")
    else:
        print("\nProductCompositeMaterials: 表不存在")

    conn.close()
    print("\n数据库文件正常")
except Exception as e:
    print(f"错误: {e}")
