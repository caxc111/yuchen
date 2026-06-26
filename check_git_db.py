import sqlite3
import sys
sys.stdout.reconfigure(encoding='utf-8')

# 先从 git 中恢复数据库看看
import subprocess
import os

# 获取 git 中数据库的临时路径
temp_db_path = r'D:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\temp_git_db.db'

# 从 git 恢复数据库
result = subprocess.run([
    'git', '-C', r'D:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心',
    'show', 'HEAD:FactoryProductManager/FactoryProductDB.db'
], capture_output=True)

with open(temp_db_path, 'wb') as f:
    f.write(result.stdout)

# 检查 git 中的数据库
conn = sqlite3.connect(temp_db_path)
cursor = conn.cursor()

print("=== Git中的数据库 - 工厂资料 ===")
cursor.execute("SELECT COUNT(*) FROM FactoryProducts")
print(f"物料数量: {cursor.fetchone()[0]}")

cursor.execute("""
    SELECT id, factory_product_code, my_product_code, product_name, brand, category
    FROM FactoryProducts
""")
for row in cursor.fetchall():
    print(f"  ID={row[0]}, 编码={row[1]}, 我的编码={row[2]}, 名称={row[3]}, 品牌={row[4]}, 分类={row[5]}")

print("\n=== Git中的数据库 - 工厂 ===")
cursor.execute("SELECT COUNT(*) FROM Factories")
print(f"工厂数量: {cursor.fetchone()[0]}")

cursor.execute("SELECT id, factory_code, factory_name FROM Factories")
for row in cursor.fetchall():
    print(f"  ID={row[0]}, 编码={row[1]}, 名称={row[2]}")

conn.close()

# 删除临时文件
os.remove(temp_db_path)
