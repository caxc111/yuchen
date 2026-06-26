with open('remote_7d081c6.db', 'rb') as f:
    content = f.read(500)
    # 尝试用 UTF-16 解码
    try:
        text = content.decode('utf-16-le')
        print("内容 (前500字节):")
        print(text[:500])
    except:
        print("无法解码为UTF-16")
