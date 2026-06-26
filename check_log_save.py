import sys
sys.stdout.reconfigure(encoding='utf-8')

# 读取日志文件，查找昨天（6月21日）的保存记录
with open(r'D:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\Logs\Log_20260621.txt', 'r', encoding='utf-8') as f:
    lines = f.readlines()

# 查找工厂和物料相关的日志
save_keywords = ['新增工厂成功', '新增工厂物料成功', '添加物料', '添加工厂', 'Save', 'Commit', '事务', 'transaction']

print("=== 昨天(6月21日)保存相关日志 ===\n")
for i, line in enumerate(lines):
    # 只看最后3000行（约下午的日志）
    if i < len(lines) - 3000:
        continue
    for kw in save_keywords:
        if kw in line:
            print(line.strip())
            break

print("\n\n=== 最后100行日志 ===\n")
for line in lines[-100:]:
    print(line.strip())
