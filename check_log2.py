import sys
sys.stdout.reconfigure(encoding='utf-8')

# 读取日志文件
with open(r'D:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\Logs\Log_20260621.txt', 'r', encoding='utf-8') as f:
    lines = f.readlines()

# 搜索工厂物料相关
print("=== 搜索'新增工厂物料成功' ===")
for line in lines:
    if '新增工厂物料成功' in line:
        print(line.strip())

print("\n=== 搜索'新增工厂成功' ===")
for line in lines:
    if '新增工厂成功' in line:
        print(line.strip())

print("\n=== 搜索'添加工厂' ===")
for line in lines:
    if '添加工厂' in line or '添加物料' in line:
        print(line.strip())

print("\n=== 搜索'工厂资料' ===")
for line in lines:
    if '工厂资料' in line:
        print(line.strip())

print("\n=== 搜索'FactoryMaterial' ===")
for line in lines:
    if 'FactoryMaterial' in line or '工厂物料' in line:
        print(line.strip())

# 查看上午的日志
print("\n\n=== 上午日志 (10:00-12:00) ===")
for line in lines:
    if '10:' in line or '11:' in line:
        if any(kw in line for kw in ['工厂', '物料', 'Factory', 'Material', 'Product', '产品', '新增', '保存', 'Commit', 'Save']):
            print(line.strip())
