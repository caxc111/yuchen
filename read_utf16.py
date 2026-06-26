import sys
sys.stdout.reconfigure(encoding='utf-8')

with open('remote_7d081c6.db', 'rb') as f:
    content = f.read()

# UTF-16 LE 特征: FF FE
if content[0] == 0xFF and content[1] == 0xFE:
    text = content.decode('utf-16-le')
    print("UTF-16 LE 文件内容预览:")
    print(text[:2000])
