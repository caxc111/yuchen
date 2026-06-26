import sqlite3
from pathlib import Path

base_dir = Path(r'D:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager')
paths = {
    'current': base_dir / 'FactoryProductDB.db',
    'backup': base_dir / 'FactoryProductDB_backup.db',
}

for name, path in paths.items():
    print(f'== {name}: {path}')
    if not path.exists():
        print('missing')
        continue
    con = sqlite3.connect(path)
    cur = con.cursor()
    cur.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;")
    tables = [r[0] for r in cur.fetchall()]
    print('tables:', tables)
    for table in tables:
        try:
            cur.execute(f'SELECT COUNT(*) FROM {table};')
            count = cur.fetchone()[0]
        except Exception as e:
            count = f'ERR: {e}'
        print(f'{table}: {count}')
        if table == 'Factories':
            cur.execute("PRAGMA table_info(Factories);")
            cols = cur.fetchall()
            print('  columns:', cols)
            cols_names = [c[1] for c in cols]
            if set(['Id','FactoryName','ContactPerson','ContactPhone','CreatedAt']).issubset(set(cols_names)):
                cur.execute('SELECT Id,FactoryName,ContactPerson,ContactPhone,CreatedAt FROM Factories ORDER BY Id;')
            else:
                cur.execute(f'SELECT * FROM Factories ORDER BY Id;')
            for row in cur.fetchall():
                print('  FACTORY', row)
    con.close()
    print()
