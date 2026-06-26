import sqlite3
import subprocess
import os
import sys
sys.stdout.reconfigure(encoding='utf-8')

# 检查 git 中 6月21日提交的数据库状态
repo = r'D:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心'

# 6月21日15:21的提交
result = subprocess.run([
    'git', '-C', repo,
    'show', '7d081c6:FactoryProductManager/FactoryProductDB.db'
], capture_output=True)

if result.returncode == 0 and len(result.stdout) > 0:
    temp_path = os.path.join(os.path.dirname(__file__), 'temp_0621.db')
    with open(temp_path, 'wb') as f:
        f.write(result.stdout)

    conn = sqlite3.connect(temp_path)
    cursor = conn.cursor()

    print("=== 6月21日15:21提交中的数据库 ===")
    cursor.execute("SELECT COUNT(*) FROM FactoryProducts")
    mat_count = cursor.fetchone()[0]
    print(f"物料数量: {mat_count}")

    cursor.execute("SELECT COUNT(*) FROM Factories")
    fac_count = cursor.fetchone()[0]
    print(f"工厂数量: {fac_count}")

    # 显示所有物料
    if mat_count > 0:
        cursor.execute("SELECT id, product_name, brand FROM FactoryProducts")
        for row in cursor.fetchall():
            print(f"  ID={row[0]}, 名称={row[1]}, 品牌={row[2]}")

    conn.close()
    os.remove(temp_path)
else:
    print("无法获取6月21日的数据库")

# 检查更早的提交
print("\n=== 6月21日之前的提交 ===")
result = subprocess.run([
    'git', '-C', repo,
    'log', '--oneline', '--before=2026-06-21', '-5'
], capture_output=True, text=True)
print(result.stdout)
