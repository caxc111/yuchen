import sqlite3
import sys
import io

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

# 创建数据库连接
conn = sqlite3.connect('FactoryProductDB.db')
cursor = conn.cursor()

# =============================================
# 1. 用户表（Users）- 权限管理
# =============================================
cursor.execute('''
CREATE TABLE IF NOT EXISTS Users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    username VARCHAR(50) NOT NULL UNIQUE,
    password VARCHAR(255) NOT NULL,
    real_name VARCHAR(50),
    role VARCHAR(20) DEFAULT 'User',
    is_active INTEGER DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
)
''')

# =============================================
# 2. 工厂表（Factories）
# =============================================
cursor.execute('''
CREATE TABLE IF NOT EXISTS Factories (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    factory_code VARCHAR(20) NOT NULL UNIQUE,
    factory_name VARCHAR(100) NOT NULL,
    factory_type VARCHAR(50),
    address VARCHAR(200),
    certifications TEXT,
    description TEXT,
    scale VARCHAR(100),
    employee_count INTEGER,
    production_capacity VARCHAR(100),
    controlling_person VARCHAR(50),
    contact_person VARCHAR(50),
    contact_info VARCHAR(100),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
)
''')

# =============================================
# 3. 工厂产品表（FactoryProducts）
# =============================================
cursor.execute('''
CREATE TABLE IF NOT EXISTS FactoryProducts (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    factory_product_code VARCHAR(50) NOT NULL UNIQUE,
    my_product_code VARCHAR(50),
    product_name VARCHAR(100) NOT NULL,
    brand VARCHAR(50),
    specification TEXT,
    texture VARCHAR(50),
    process VARCHAR(50),
    usage_scenario VARCHAR(100),
    certifications TEXT,
    category VARCHAR(50),
    image_url VARCHAR(255),
    factory_id INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (factory_id) REFERENCES Factories(id) ON DELETE SET NULL
)
''')

# =============================================
# 4. 成品表（Products）- 组合后的新产品
# =============================================
cursor.execute('''
CREATE TABLE IF NOT EXISTS Products (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    product_code VARCHAR(50) NOT NULL UNIQUE,
    product_name VARCHAR(100) NOT NULL,
    specification VARCHAR(100),
    unit VARCHAR(20),
    total_cost DECIMAL(10,2) DEFAULT 0,
    selling_price DECIMAL(10,2),
    is_active INTEGER DEFAULT 1,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
)
''')

# =============================================
# 5. 物料清单表（ProductBOM）- 成品组成
# =============================================
cursor.execute('''
CREATE TABLE IF NOT EXISTS ProductBOM (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    product_id INTEGER NOT NULL,
    component_id INTEGER NOT NULL,
    quantity DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (product_id) REFERENCES Products(id) ON DELETE CASCADE,
    FOREIGN KEY (component_id) REFERENCES FactoryProducts(id),
    UNIQUE(product_id, component_id)
)
''')

# =============================================
# 6. 采购订单表（PurchaseOrders）
# =============================================
cursor.execute('''
CREATE TABLE IF NOT EXISTS PurchaseOrders (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    purchase_no VARCHAR(50) NOT NULL UNIQUE,
    supplier VARCHAR(100) NOT NULL,
    total_amount DECIMAL(12,2) DEFAULT 0,
    status VARCHAR(20) DEFAULT '待入库',
    purchaser_id INTEGER,
    purchase_date DATE,
    remark TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (purchaser_id) REFERENCES Users(id)
)
''')

# =============================================
# 7. 采购明细表（PurchaseItems）
# =============================================
cursor.execute('''
CREATE TABLE IF NOT EXISTS PurchaseItems (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    purchase_id INTEGER NOT NULL,
    component_id INTEGER NOT NULL,
    quantity DECIMAL(10,2) NOT NULL,
    unit_price DECIMAL(10,2),
    received_quantity DECIMAL(10,2) DEFAULT 0,
    FOREIGN KEY (purchase_id) REFERENCES PurchaseOrders(id) ON DELETE CASCADE,
    FOREIGN KEY (component_id) REFERENCES FactoryProducts(id)
)
''')

# =============================================
# 8. 库存流水表（InventoryLogs）
# =============================================
cursor.execute('''
CREATE TABLE IF NOT EXISTS InventoryLogs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    component_id INTEGER NOT NULL,
    change_type VARCHAR(20) NOT NULL,
    change_quantity DECIMAL(10,2) NOT NULL,
    stock_before DECIMAL(10,2),
    stock_after DECIMAL(10,2),
    reference_no VARCHAR(50),
    operator_id INTEGER,
    remark VARCHAR(200),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (component_id) REFERENCES FactoryProducts(id),
    FOREIGN KEY (operator_id) REFERENCES Users(id)
)
''')

# =============================================
# 9. 合同表（Contracts）
# =============================================
cursor.execute('''
CREATE TABLE IF NOT EXISTS Contracts (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    contract_no VARCHAR(50) NOT NULL UNIQUE,
    customer_name VARCHAR(100) NOT NULL,
    total_amount DECIMAL(12,2) DEFAULT 0,
    status VARCHAR(20) DEFAULT '待签',
    sign_date DATE,
    delivery_date DATE,
    remark TEXT,
    creator_id INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (creator_id) REFERENCES Users(id)
)
''')

# =============================================
# 10. 合同明细表（ContractItems）
# =============================================
cursor.execute('''
CREATE TABLE IF NOT EXISTS ContractItems (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    contract_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    quantity DECIMAL(10,2) NOT NULL,
    unit_price DECIMAL(10,2),
    amount DECIMAL(10,2),
    FOREIGN KEY (contract_id) REFERENCES Contracts(id) ON DELETE CASCADE,
    FOREIGN KEY (product_id) REFERENCES Products(id)
)
''')

# 创建索引
cursor.execute('CREATE INDEX IF NOT EXISTS idx_FactoryProducts_factory_id ON FactoryProducts(factory_id)')
cursor.execute('CREATE INDEX IF NOT EXISTS idx_ProductBOM_product_id ON ProductBOM(product_id)')
cursor.execute('CREATE INDEX IF NOT EXISTS idx_PurchaseItems_purchase_id ON PurchaseItems(purchase_id)')
cursor.execute('CREATE INDEX IF NOT EXISTS idx_InventoryLogs_component_id ON InventoryLogs(component_id)')
cursor.execute('CREATE INDEX IF NOT EXISTS idx_ContractItems_contract_id ON ContractItems(contract_id)')

# 提交并关闭
conn.commit()
conn.close()

print("数据库创建成功！")
print("数据库文件: FactoryProductDB.db")