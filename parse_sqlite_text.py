import re

with open('remote_7d081c6.db', 'rb') as f:
    content = f.read()

# 去掉 UTF-16 BOM
if content[0] == 0xFF and content[1] == 0xFE:
    text = content.decode('utf-16-le')
    # 去掉 BOM
    text = text.lstrip('\ufeff')
    
# 查找所有 CREATE TABLE 语句
tables = re.findall(r'CREATE TABLE (\w+)', text)
print(f"找到 {len(tables)} 个表:")
for t in tables:
    print(f"  - {t}")

# 检查 ProductCompositeMaterials
if 'ProductCompositeMaterials' in text:
    print("\n包含 ProductCompositeMaterials 的定义")
else:
    print("\n不包含 ProductCompositeMaterials")
