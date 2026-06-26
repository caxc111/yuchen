with open('remote_7d081c6.db', 'rb') as f:
    content = f.read(1000)
    
# UTF-16 LE 特征: FF FE 开头
if content[0] == 0xFF and content[1] == 0xFE:
    print("检测到 UTF-16 LE 编码")
    text = content.decode('utf-16-le')
    print("内容预览:")
    print(text[:500])
elif content[0] == 0x53 and content[1] == 0x51:  # 'SQ'
    print("检测到 SQLCipher 加密数据库")
else:
    print(f"文件开头: {content[:20]}")
    # 尝试找SQLite特征
    print("查找 'SQLite' 字符串位置:")
    pos = content.find(b'SQLite')
    if pos >= 0:
        print(f"  在位置 {pos} 找到")
    else:
        print("  未找到")
