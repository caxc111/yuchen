with open('remote_7d081c6.db', 'rb') as f:
    header = f.read(16)
    print("文件头 (hex):", header.hex())
    print("文件头 (bytes):", list(header))
