import sqlite3

# 检查主数据库
print("=== 主数据库 FactoryProductDB.db ===")
try:
    conn = sqlite3.connect(r'D:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\FactoryProductDB.db')
    cursor = conn.cursor()
    
    cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
    tables = [t[0] for t in cursor.fetchall()]
    print(f"表 ({len(tables)}):")
    for t in tables:
        print(f"  - {t}")
    
    if 'ProductCompositeMaterials' in tables:
        cursor.execute("SELECT COUNT(*) FROM ProductCompositeMaterials")
        print(f"\nProductCompositeMaterials: {cursor.fetchone()[0]} 条")
    else:
        print("\nProductCompositeMaterials: 不存在")
    
    conn.close()
except Exception as e:
    print(f"错误: {e}")

# 检查备份数据库
print("\n=== 备份数据库 FactoryProductDB_backup_restored.db ===")
try:
    conn2 = sqlite3.connect(r'D:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\FactoryProductDB_backup_restored.db')
    cursor2 = conn2.cursor()
    
    cursor2.execute("SELECT name FROM sqlite_master WHERE type='table'")
    tables2 = [t[0] for t in cursor2.fetchall()]
    print(f"表 ({len(tables2)}):")
    for t in tables2:
        print(f"  - {t}")
    
    conn2.close()
except Exception as e:
    print(f"错误: {e}")
