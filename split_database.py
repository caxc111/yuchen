# -*- coding: utf-8 -*-
"""
数据库分库脚本 v2
直接复制原始数据库，然后按库分离表
"""
import sys
import io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

import sqlite3
import os
import shutil
from datetime import datetime

# 路径配置
BASE_DIR = r"d:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager"
SOURCE_DB = os.path.join(BASE_DIR, "FactoryProductDB.db")
GLOBAL_DB = os.path.join(BASE_DIR, "GlobalMaterialDB.db")
PROJECT_DB = os.path.join(BASE_DIR, "ProjectDB.db")

# 全局物料库需要的表
GLOBAL_TABLES = [
    'Factories',
    'FactoryProducts',
    'MaterialGroups',
    'MaterialGroupItems',
    'CustomParts'
]

# 项目库需要的表
PROJECT_TABLES = [
    'Projects',  # 新增
    'Products',  # 项目库保留 Products（关联项目和全局物料）
    'ProductParts',
    'ProductPartMaterials',
    'ProductMaterials',
    'ProductMaterialLibrary',
    'ProductBOM',
    'ProductCompositeMaterials',
    'ProductCompositeMaterialItems',
    'Contracts',
    'ContractItems',
    'PurchaseOrders',
    'PurchaseItems',
    'InventoryLogs',
    'Users'
]


def backup_database():
    """备份原始数据库"""
    backup_path = SOURCE_DB + f".backup_{datetime.now().strftime('%Y%m%d_%H%M%S')}"
    shutil.copy2(SOURCE_DB, backup_path)
    print(f"[OK] 备份完成: {backup_path}")
    return backup_path


def create_databases():
    """创建分库后的数据库"""
    print("\n[1] 创建分库...")
    
    # 复制原始数据库作为基础
    if os.path.exists(GLOBAL_DB):
        os.remove(GLOBAL_DB)
    if os.path.exists(PROJECT_DB):
        os.remove(PROJECT_DB)
    
    # 复制源数据库
    shutil.copy2(SOURCE_DB, GLOBAL_DB)
    shutil.copy2(SOURCE_DB, PROJECT_DB)
    
    global_conn = sqlite3.connect(GLOBAL_DB)
    project_conn = sqlite3.connect(PROJECT_DB)
    
    # 在 GlobalDB 中删除项目相关的表
    tables_to_delete_global = PROJECT_TABLES.copy()
    if 'Products' in tables_to_delete_global:
        tables_to_delete_global.remove('Products')  # Products 在全局库也要保留（用于关联）
    for table in tables_to_delete_global:
        try:
            global_conn.execute(f"DROP TABLE IF EXISTS {table}")
            print(f"    [GlobalDB] 删除表: {table}")
        except Exception as e:
            print(f"    [GlobalDB] 删除 {table} 失败: {e}")
    
    # 在 ProjectDB 中删除全局物料相关的表
    tables_to_delete_project = GLOBAL_TABLES.copy()
    if 'Products' in tables_to_delete_project:
        tables_to_delete_project.remove('Products')  # Products 在项目库也要保留
    for table in tables_to_delete_project:
        try:
            project_conn.execute(f"DROP TABLE IF EXISTS {table}")
            print(f"    [ProjectDB] 删除表: {table}")
        except Exception as e:
            print(f"    [ProjectDB] 删除 {table} 失败: {e}")
    
    global_conn.commit()
    project_conn.commit()
    global_conn.close()
    project_conn.close()
    
    print("    [OK] 分库创建完成")


def create_projects_table():
    """在项目库中创建 Projects 表并迁移数据"""
    print("\n[2] 创建 Projects 表...")
    
    project_conn = sqlite3.connect(PROJECT_DB)
    cursor = project_conn.cursor()
    
    # 创建 Projects 表
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS Projects (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            project_name TEXT NOT NULL UNIQUE,
            project_code TEXT,
            status TEXT DEFAULT '进行中',
            description TEXT,
            start_date TEXT,
            end_date TEXT,
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL
        )
    """)
    
    # 创建 Products.project_id 列（如果不存在）
    cursor.execute("PRAGMA table_info(Products)")
    columns = [col[1] for col in cursor.fetchall()]
    if 'project_id' not in columns:
        cursor.execute("ALTER TABLE Products ADD COLUMN project_id INTEGER")
        print("    [OK] 添加 Products.project_id 列")
    
    # 获取所有不同的 project_name，创建 Projects 记录
    cursor.execute("""
        SELECT DISTINCT project_name 
        FROM Products 
        WHERE project_name IS NOT NULL AND project_name != ''
    """)
    project_names = cursor.fetchall()
    
    now = datetime.now().strftime('%Y-%m-%d %H:%M:%S')
    
    for (name,) in project_names:
        code = generate_project_code(name)
        try:
            cursor.execute("""
                INSERT INTO Projects (project_name, project_code, status, created_at, updated_at)
                VALUES (?, ?, '进行中', ?, ?)
            """, (name, code, now, now))
        except:
            pass  # 已存在则忽略
    
    # 更新 Products.project_id
    cursor.execute("SELECT id, project_name FROM Projects")
    project_map = {name: pid for pid, name in cursor.fetchall()}
    
    for name, pid in project_map.items():
        cursor.execute("UPDATE Products SET project_id = ? WHERE project_name = ?", (pid, name))
    
    project_conn.commit()
    project_conn.close()
    
    print(f"    [OK] 创建了 {len(project_names)} 个项目")


def generate_project_code(project_name):
    """生成项目编码"""
    if not project_name:
        return f"PJ{datetime.now().timestamp():.0f}"
    code = "PJ"
    for c in project_name:
        if c.isalnum():
            code += c
    return code[:10] if len(code) > 10 else code


def add_foreign_keys():
    """在项目库中添加外键约束"""
    print("\n[3] 添加外键约束...")
    
    project_conn = sqlite3.connect(PROJECT_DB)
    cursor = project_conn.cursor()
    
    # 为 Products 表添加外键
    try:
        cursor.execute("""
            CREATE INDEX IF NOT EXISTS idx_products_project 
            ON Products(project_id)
        """)
        print("    [OK] Products.project_id 索引创建完成")
    except Exception as e:
        print(f"    [WARN] {e}")
    
    # 为其他表添加外键
    foreign_keys = [
        ("ProductParts", "product_id", "Products"),
        ("ProductMaterials", "product_id", "Products"),
        ("ProductMaterialLibrary", "product_id", "Products"),
        ("ProductBOM", "product_id", "Products"),
        ("ProductCompositeMaterials", "product_id", "Products"),
        ("Contracts", "project_id", "Projects"),
        ("PurchaseOrders", "project_id", "Projects"),
    ]
    
    for table, column, ref_table in foreign_keys:
        try:
            cursor.execute(f"""
                CREATE INDEX IF NOT EXISTS idx_{table.lower()}_{column.lower()} 
                ON {table}({column})
            """)
            print(f"    [OK] {table}.{column} 索引创建完成")
        except Exception as e:
            print(f"    [WARN] {table}: {e}")
    
    project_conn.commit()
    project_conn.close()


def verify_split():
    """验证分库结果"""
    print("\n[4] 验证分库结果...")
    
    # 验证全局物料库
    global_conn = sqlite3.connect(GLOBAL_DB)
    global_cursor = global_conn.cursor()
    
    global_cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
    global_tables = [row[0] for row in global_cursor.fetchall() if row[0] != 'sqlite_sequence']
    
    print(f"\n  GlobalMaterialDB ({len(global_tables)} 个表):")
    for t in sorted(global_tables):
        global_cursor.execute(f"SELECT COUNT(*) FROM {t}")
        count = global_cursor.fetchone()[0]
        print(f"    - {t}: {count} 条")
    
    global_conn.close()
    
    # 验证项目库
    project_conn = sqlite3.connect(PROJECT_DB)
    project_cursor = project_conn.cursor()
    
    project_cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
    project_tables = [row[0] for row in project_cursor.fetchall() if row[0] != 'sqlite_sequence']
    
    print(f"\n  ProjectDB ({len(project_tables)} 个表):")
    for t in sorted(project_tables):
        project_cursor.execute(f"SELECT COUNT(*) FROM {t}")
        count = project_cursor.fetchone()[0]
        print(f"    - {t}: {count} 条")
    
    # 验证关联
    project_cursor.execute("""
        SELECT p.project_name, pr.product_code
        FROM Projects p
        LEFT JOIN Products pr ON p.id = pr.project_id
        LIMIT 10
    """)
    
    print(f"\n  项目-产品关联:")
    for row in project_cursor.fetchall():
        print(f"    {row[0]} -> {row[1]}")
    
    project_conn.close()


def main():
    print("=" * 50)
    print("数据库分库脚本 v2")
    print("=" * 50)
    
    if not os.path.exists(SOURCE_DB):
        print(f"[ERROR] 找不到源数据库 {SOURCE_DB}")
        return
    
    # 1. 备份
    backup_database()
    
    # 2. 创建分库
    create_databases()
    
    # 3. 创建 Projects 表
    create_projects_table()
    
    # 4. 添加外键索引
    add_foreign_keys()
    
    # 5. 验证
    verify_split()
    
    print("\n" + "=" * 50)
    print("分库完成!")
    print(f"  全局物料库: {GLOBAL_DB}")
    print(f"  项目数据库: {PROJECT_DB}")
    print("=" * 50)
    
    # 打印下一步操作建议
    print("\n[下一步]")
    print("1. 测试应用程序是否正常")
    print("2. 修改 DbService.cs 支持双数据库连接")
    print("3. 提交代码到 git")


if __name__ == "__main__":
    main()
