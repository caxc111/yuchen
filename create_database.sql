-- =============================================
-- 工厂产品管理系统 - 数据库建表脚本
-- 版本: 1.0
-- 创建日期: 2026-05-27
-- =============================================

-- 创建数据库
CREATE DATABASE IF NOT EXISTS FactoryProductDB 
DEFAULT CHARACTER SET utf8mb4 
DEFAULT COLLATE utf8mb4_unicode_ci;

USE FactoryProductDB;

-- =============================================
-- 1. 用户表（Users）- 权限管理
-- =============================================
CREATE TABLE IF NOT EXISTS Users (
    id              INT PRIMARY KEY AUTO_INCREMENT COMMENT '用户ID',
    username        VARCHAR(50) NOT NULL UNIQUE COMMENT '登录账号',
    password        VARCHAR(255) NOT NULL COMMENT '加密密码',
    real_name       VARCHAR(50) COMMENT '真实姓名',
    role            VARCHAR(20) DEFAULT 'User' COMMENT '角色：Admin/User/ViewOnly',
    is_active       TINYINT(1) DEFAULT 1 COMMENT '是否启用',
    created_at      DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    updated_at      DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='用户表';

-- =============================================
-- 2. 工厂表（Factories）
-- =============================================
CREATE TABLE IF NOT EXISTS Factories (
    id                      INT PRIMARY KEY AUTO_INCREMENT COMMENT '工厂ID',
    factory_code            VARCHAR(20) NOT NULL UNIQUE COMMENT '工厂编码（如 S001）',
    factory_name            VARCHAR(100) NOT NULL COMMENT '工厂名称',
    factory_type            VARCHAR(50) COMMENT '工厂类别（木制品厂等）',
    address                 VARCHAR(200) COMMENT '工厂地址',
    certifications          TEXT COMMENT '认证信息（ISO等）',
    description             TEXT COMMENT '工厂简介',
    scale                   VARCHAR(100) COMMENT '工厂规模',
    employee_count          INT COMMENT '工厂人数',
    production_capacity     VARCHAR(100) COMMENT '生产量/产能',
    controlling_person      VARCHAR(50) COMMENT '实际控制人',
    contact_person          VARCHAR(50) COMMENT '业务对接人',
    contact_info            VARCHAR(100) COMMENT '联系方式',
    created_at              DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    updated_at              DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='工厂表';

-- =============================================
-- 3. 工厂产品表（FactoryProducts）
-- =============================================
CREATE TABLE IF NOT EXISTS FactoryProducts (
    id                      INT PRIMARY KEY AUTO_INCREMENT COMMENT '产品ID',
    factory_product_code    VARCHAR(50) NOT NULL UNIQUE COMMENT '工厂产品编码',
    my_product_code         VARCHAR(50) COMMENT '我的产品编码（如 S001-0001）',
    product_name            VARCHAR(100) NOT NULL COMMENT '产品名称',
    brand                   VARCHAR(50) COMMENT '品牌商',
    specification           TEXT COMMENT '规格参数',
    texture                 VARCHAR(50) COMMENT '纹理',
    process                 VARCHAR(50) COMMENT '工艺',
    usage_scenario          VARCHAR(100) COMMENT '使用场景',
    certifications          TEXT COMMENT '产品认证信息',
    category                VARCHAR(50) COMMENT '产品类别',
    image_url               VARCHAR(255) COMMENT '图片路径',
    factory_id              INT COMMENT '所属工厂ID',
    created_at              DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    updated_at              DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    
    FOREIGN KEY (factory_id) REFERENCES Factories(id) ON DELETE SET NULL COMMENT '关联工厂'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='工厂产品表';

-- =============================================
-- 4. 成品表（Products）- 组合后的新产品
-- =============================================
CREATE TABLE IF NOT EXISTS Products (
    id                  INT PRIMARY KEY AUTO_INCREMENT COMMENT '成品ID',
    product_code        VARCHAR(50) NOT NULL UNIQUE COMMENT '成品编码',
    product_name        VARCHAR(100) NOT NULL COMMENT '成品名称',
    specification       VARCHAR(100) COMMENT '规格型号',
    unit                VARCHAR(20) COMMENT '单位',
    total_cost          DECIMAL(10,2) DEFAULT 0 COMMENT '总成本',
    selling_price       DECIMAL(10,2) COMMENT '售价',
    is_active           TINYINT(1) DEFAULT 1 COMMENT '是否启用',
    created_at          DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    updated_at          DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='成品表';

-- =============================================
-- 5. 物料清单表（ProductBOM）- 成品组成
-- =============================================
CREATE TABLE IF NOT EXISTS ProductBOM (
    id              INT PRIMARY KEY AUTO_INCREMENT COMMENT 'BOM记录ID',
    product_id      INT NOT NULL COMMENT '成品ID',
    component_id    INT NOT NULL COMMENT '组件ID（工厂产品）',
    quantity        DECIMAL(10,2) NOT NULL COMMENT '组件数量',
    
    FOREIGN KEY (product_id) REFERENCES Products(id) ON DELETE CASCADE COMMENT '关联成品',
    FOREIGN KEY (component_id) REFERENCES FactoryProducts(id) COMMENT '关联组件',
    UNIQUE KEY (product_id, component_id) COMMENT '防止重复添加'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='物料清单表';

-- =============================================
-- 6. 采购订单表（PurchaseOrders）
-- =============================================
CREATE TABLE IF NOT EXISTS PurchaseOrders (
    id              INT PRIMARY KEY AUTO_INCREMENT COMMENT '采购单ID',
    purchase_no     VARCHAR(50) NOT NULL UNIQUE COMMENT '采购单号',
    supplier        VARCHAR(100) NOT NULL COMMENT '供应商',
    total_amount    DECIMAL(12,2) DEFAULT 0 COMMENT '总金额',
    status          VARCHAR(20) DEFAULT '待入库' COMMENT '状态：待入库/已入库/已取消',
    purchaser_id    INT COMMENT '采购员ID',
    purchase_date   DATE COMMENT '采购日期',
    remark          TEXT COMMENT '备注',
    created_at      DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    updated_at      DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    
    FOREIGN KEY (purchaser_id) REFERENCES Users(id) COMMENT '关联采购员'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='采购订单表';

-- =============================================
-- 7. 采购明细表（PurchaseItems）
-- =============================================
CREATE TABLE IF NOT EXISTS PurchaseItems (
    id                  INT PRIMARY KEY AUTO_INCREMENT COMMENT '采购明细ID',
    purchase_id         INT NOT NULL COMMENT '采购单ID',
    component_id        INT NOT NULL COMMENT '组件ID',
    quantity            DECIMAL(10,2) NOT NULL COMMENT '采购数量',
    unit_price          DECIMAL(10,2) COMMENT '采购单价',
    received_quantity   DECIMAL(10,2) DEFAULT 0 COMMENT '已入库数量',
    
    FOREIGN KEY (purchase_id) REFERENCES PurchaseOrders(id) ON DELETE CASCADE COMMENT '关联采购单',
    FOREIGN KEY (component_id) REFERENCES FactoryProducts(id) COMMENT '关联组件'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='采购明细表';

-- =============================================
-- 8. 库存流水表（InventoryLogs）
-- =============================================
CREATE TABLE IF NOT EXISTS InventoryLogs (
    id              INT PRIMARY KEY AUTO_INCREMENT COMMENT '流水ID',
    component_id    INT NOT NULL COMMENT '组件ID',
    change_type     VARCHAR(20) NOT NULL COMMENT '变动类型：采购入库/生产领料/库存盘点/销售出库/退货',
    change_quantity DECIMAL(10,2) NOT NULL COMMENT '变动数量',
    stock_before    DECIMAL(10,2) COMMENT '变动前库存',
    stock_after     DECIMAL(10,2) COMMENT '变动后库存',
    reference_no    VARCHAR(50) COMMENT '单据编号',
    operator_id     INT COMMENT '操作人ID',
    remark          VARCHAR(200) COMMENT '备注',
    created_at      DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '操作时间',
    
    FOREIGN KEY (component_id) REFERENCES FactoryProducts(id) COMMENT '关联组件',
    FOREIGN KEY (operator_id) REFERENCES Users(id) COMMENT '关联操作人'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='库存流水表';

-- =============================================
-- 9. 合同表（Contracts）
-- =============================================
CREATE TABLE IF NOT EXISTS Contracts (
    id              INT PRIMARY KEY AUTO_INCREMENT COMMENT '合同ID',
    contract_no     VARCHAR(50) NOT NULL UNIQUE COMMENT '合同编号',
    customer_name   VARCHAR(100) NOT NULL COMMENT '客户名称',
    total_amount    DECIMAL(12,2) DEFAULT 0 COMMENT '合同总金额',
    status          VARCHAR(20) DEFAULT '待签' COMMENT '状态：待签/已签/执行中/完成/终止',
    sign_date       DATE COMMENT '签订日期',
    delivery_date   DATE COMMENT '交货日期',
    remark          TEXT COMMENT '备注',
    creator_id      INT COMMENT '创建人ID',
    created_at      DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    updated_at      DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    
    FOREIGN KEY (creator_id) REFERENCES Users(id) COMMENT '关联创建人'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='合同表';

-- =============================================
-- 10. 合同明细表（ContractItems）
-- =============================================
CREATE TABLE IF NOT EXISTS ContractItems (
    id              INT PRIMARY KEY AUTO_INCREMENT COMMENT '合同明细ID',
    contract_id     INT NOT NULL COMMENT '合同ID',
    product_id      INT NOT NULL COMMENT '成品ID',
    quantity        DECIMAL(10,2) NOT NULL COMMENT '数量',
    unit_price      DECIMAL(10,2) COMMENT '单价',
    amount          DECIMAL(10,2) COMMENT '金额',
    
    FOREIGN KEY (contract_id) REFERENCES Contracts(id) ON DELETE CASCADE COMMENT '关联合同',
    FOREIGN KEY (product_id) REFERENCES Products(id) COMMENT '关联成品'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='合同明细表';

-- =============================================
-- 创建索引
-- =============================================
CREATE INDEX idx_FactoryProducts_factory_id ON FactoryProducts(factory_id);
CREATE INDEX idx_ProductBOM_product_id ON ProductBOM(product_id);
CREATE INDEX idx_PurchaseItems_purchase_id ON PurchaseItems(purchase_id);
CREATE INDEX idx_InventoryLogs_component_id ON InventoryLogs(component_id);
CREATE INDEX idx_ContractItems_contract_id ON ContractItems(contract_id);

-- =============================================
-- 插入初始数据（可选）
-- =============================================
-- INSERT INTO Users (username, password, real_name, role) VALUES 
-- ('admin', '加密密码', '管理员', 'Admin');

-- INSERT INTO Factories (factory_code, factory_name) VALUES 
-- ('S001', '默认工厂');

SELECT '数据库创建完成！' AS message;
