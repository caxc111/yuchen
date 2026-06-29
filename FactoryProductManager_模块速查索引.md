# FactoryProductManager 模块速查索引

## 目录

- [1. 入口与导航](#1-入口与导航)
- [2. 工厂管理](#2-工厂管理)
- [3. 物料管理](#3-物料管理)
- [4. 产品管理](#4-产品管理)
- [5. 物料选择与部件编辑](#5-物料选择与部件编辑)
- [6. BOM / 采购 / 合同 / 报表](#6-bom--采购--合同--报表)
- [7. 支撑服务](#7-支撑服务)
- [8. 数据层与表结构](#8-数据层与表结构)
- [9. 视图与弹窗清单](#9-视图与弹窗清单)

---

## 1. 入口与导航

- `MainWindow.xaml.cs`
  - 应用启动入口
  - `DataContext = new MainViewModel()`
- `MainViewModel`
  - 当前页面：`CurrentView`
  - 当前页面标识：`CurrentPage`
  - 导航命令：`NavigateCommand`
  - 支持页面：
    - `Factory`
    - `Material`
    - `ProductManagement`
    - `BOM`
    - `Purchase`
    - `Contract`
    - `Report`

## 2. 工厂管理

### 视图
- `FactoryView.xaml`
- `FactoryDetailsWindow.xaml`

### 视图模型
- `FactoryViewModel`
  - `Factories`：工厂列表
  - `LoadFactories()`
  - `Search()`
  - `AddFactory()`
  - `UpdateFactory()`

### 模型
- `Models/Factory.cs`

### 数据操作
- `DbService.GetFactories()`
- `DbService.AddFactory()`
- `DbService.UpdateFactory()`

## 3. 物料管理

### 视图
- `MaterialView.xaml`
- `MaterialDetailsWindow.xaml`
- `MaterialGroupEditorDialog.xaml`
- `MaterialTypePickerDialog.xaml`

### 视图模型
- `MaterialViewModel`
  - `Materials`：物料列表
  - `LoadMaterials()`
  - `Search()`
  - `AddMaterial()`
  - `UpdateMaterial()`
  - `DeleteMaterial()`

### 模型
- `Models/FactoryMaterial.cs`
- `Models/MaterialGroup.cs`
- `Models/MaterialGroupItem.cs`
- `Models/SelectedMaterial.cs`

### 数据操作
- `DbService.GetFactoryMaterials()`
- `DbService.GetFactoryMaterialsByType()`
- `DbService.AddFactoryMaterial()`
- `DbService.UpdateFactoryMaterial()`
- `DbService.DeleteFactoryMaterial()`

## 4. 产品管理

### 视图
- `ProductManagementView.xaml`
- `ProductManagementView.xaml.cs`
- `ProductManagementDialog.xaml`
- `ProductManagementDialog.xaml.cs`
- `ProductDetailsWindow.xaml`

### 视图模型
- `ProductManagementViewModel`
  - `Products`：启用产品
  - `InactiveProducts`：停用产品
  - `DisplayProducts`：界面展示列表
  - `SearchKeyword`
  - `ShowInactiveProducts`
  - `RefreshDisplayProducts()`
  - `AddProduct()`
  - `UpdateProduct()`
  - `EnableProduct()`

### 模型
- `Models/Product.cs`
- `Models/ProductPart.cs`
- `Models/ProductPartMaterial.cs`
- `Models/ProductCategory.cs`

### 数据操作
- `DbService.GetProducts()`
- `DbService.AddProduct()`
- `DbService.UpdateProduct()`
- `DbService.GetProductParts()`
- `DbService.GetProductPartMaterials()`

### ProductManagement 数据流全景图

#### 1. 列表加载
- `ProductManagementView` 构造 `ProductManagementViewModel`
- `ProductManagementViewModel` 调用 `DbService.GetProducts()`
- 数据按 `IsActive` 分流到 `Products` / `InactiveProducts`
- `RefreshDisplayProducts()` 负责生成 `DisplayProducts` 供 UI 绑定

#### 2. 搜索
- `SearchKeyword` 变化触发 `Refresh()`
- `Refresh()` -> `LoadProducts(keyword)`
- 关键过滤字段：`ProductCode / ProjectCode / HouseType / BusinessType`

#### 3. 添加产品
- `ProductManagementView.AddButton_Click`
- 打开 `ProductManagementDialog(product: null)`
- 用户填写基础信息、生成产品编码、编辑部件、选择物料
- 点击 `OkButton` 设置 `IsSaved = true`
- 外部取回 `dialog.PendingParts / dialog.PendingMaterials`
- `ProductManagementViewModel.AddProduct()` 落库

#### 4. 编辑产品
- `ProductManagementView.EditButton_Click`
- 打开 `ProductManagementDialog(product)`
- 构造函数从 DB 回填 `Product` 基础信息
- `ContentRendered` 异步加载已有部件与物料
- 编辑完成后同样由 `UpdateProduct()` 落库

#### 5. 部件编辑
- `ProductManagementDialog.EditPartsButton_Click`
- 打开 `PartManagementDialog(productId, isNewProduct, _pendingParts)`
- 默认部件：`门厅 / 客餐厨 / 主卧室 / 主卫生间 / 次卧室 / 次卫生间 / 洗衣房 / 书房 / 阳台`
- 自定义部件通过 `CustomPartEditorDialog -> PartEditorDialog` 维护
- `PartManagementDialog` 内部维护 `_pendingPresetParts / _pendingCustomParts`
- 返回后调用 `UpdatePartsSummary()` 刷新摘要并计算户型

#### 6. 物料选择
- `ProductManagementDialog.AddMaterialButton_Click`
- 打开 `AddProductMaterialWindow`
- 组织方式：`部件 -> 部品 -> 物料类型 -> 物料列表`
- 物料选择通过 `MaterialSelectorDialog` 完成
- 支持预选已有物料、多选、数量录入
- 编辑态选择完成后立即保存到 `ProductMaterialLibrary`
- 新建态暂存在 `_pendingMaterials`

#### 7. 详情查看
- `ProductManagementView.DetailsButton_Click`
- 打开 `ProductDetailsWindow`
- 重新查询 `DbService.GetProductParts()` 和 `DbService.LoadProductMaterialsFromLibrary()`
- 展示部件摘要、物料明细、成本汇总、平面图

#### 8. 状态切换
- 点击状态标签 -> `StatusToggle_Click`
- 确认后调用 `EnableProduct() / DisableProduct()`
- 修改 `IsActive` 后写库并刷新 `DisplayProducts`

#### 9. 删除
- `DeleteButton_Click` -> 确认后调用 `_viewModel.DeleteProduct()`
- `ProductManagementViewModel` 调用 `DbService.DeleteProduct()`
- 从集合移除并刷新展示列表

#### 10. 导出
- `ExportToExcel()` 使用 `OfficeOpenXml`
- 逐产品创建工作表
- 每个工作表列：图纸、部件、部品、物料、单位、品牌、工厂名称、工厂物料编码、宇辰物料编码、规格、数量、单价、成本总价、供货周期

## 5. 物料选择与部件编辑

### 视图
- `AddProductMaterialWindow.xaml`
- `MaterialSelectorDialog.xaml`
- `MaterialSelectorDialog.xaml.cs`
- `CustomPartEditorDialog.xaml`
- `CustomPartEditorDialog.xaml.cs`
- `PartEditorDialog.xaml`
- `PartEditorDialog.xaml.cs`

### 关键逻辑点
- `AddProductMaterialWindow`
  - 按“房间 + 分类”组织物料选择
  - `_componentMaterials` 配置决定各分类显示物料类型
  - 支持子类型多选（如“灶具 -> 燃气灶 / 电磁灶”）
- `MaterialSelectorDialog`
  - 按 `category LIKE '%类型%'` 检索物料
  - 支持传入单个或多个物料类型
  - 支持传入项目数据库和项目代码，查询项目内已有图纸编号
  - 选中物料或输入数量时，如果物料没有图纸编号则弹出输入框
  - 显示该项目内该物料之前用过的图纸编号
- `CustomPartEditorDialog`
  - 自定义部件编辑
  - 维护默认部品列表
- `PartEditorDialog`
  - 部件基础信息编辑

## 6. BOM / 采购 / 合同 / 报表

### 视图
- `BOMView.xaml`
- `PurchaseView.xaml`
- `ContractView.xaml`
- `ReportView.xaml`

### 视图模型
- `BOMView.xaml.cs`
- `PurchaseView.xaml.cs`
- `ContractView.xaml.cs`
- `ReportView.xaml.cs`

### 依赖
- `OfficeOpenXml`
- 页面相对独立，各自承载业务报表与导出逻辑

## 7. 支撑服务

- `Services/DbService.cs`
  - SQLite 数据访问统一入口
  - 创建并维护所有表结构
  - 支持两种构造方式：
    - `DbService(string? databasePath)` - 按路径
    - `DbService(DatabaseType)` - 按类型（Project/GlobalMaterial）
- `Services/LogService.cs`
  - 应用日志服务
  - 文件日志、会话标识、启动/退出日志、DEBUG 开关
- `Services/MaterialCodeGenerator.cs`
  - 物料编码生成规则
  - 按 `一级/二级/三级分类` 生成编码
- `Services/WindowPositionService.cs`
  - 窗口位置保护，防止弹窗超出屏幕
- `Services/DatabaseManager.cs`
  - 数据库类型枚举：`DatabaseType`（Project/GlobalMaterial）
  - 全局物料库：`GlobalMaterialDB.db`
  - 项目数据库：`FactoryProductDB.db`

### DbService 关键方法补充

#### 产品编码相关
- `GetAllProjectCodes()` - 获取所有项目编码
- `CheckProductCodeExists(string productCode)` - 检查产品编码是否存在
- `CheckProductCodeExists(string projectCode, string productCode)` - 按项目检查产品编码
- `CheckProductCodeExistsForEdit(int excludeProductId, string productCode)` - 编辑时检查（排除自身）
- `UpdateProductCode(int productId, string newCode)` - 更新产品编码

#### 物料检查相关
- `ExistsFactoryMaterialCode(string factoryCode, int? excludeId)` - 检查工厂物料编码是否存在
- `CheckDrawingNumberExistsInProject(...)` - 检查图纸编号是否存在（返回元组含部品信息）

## 8. 数据层与表结构

### 数据库类型
- `FactoryProductDB.db`（Project）- 项目产品数据库
- `GlobalMaterialDB.db`（GlobalMaterial）- 全局物料库

### FactoryProductDB.db 表结构
- `Factories`
  - 工厂主数据
- `FactoryMaterials`
  - 工厂物料主数据（factory_code, material_code, material_name...）
- `Products`
  - 产品主数据（project_code, product_code, house_type...）
- `ProductParts`
  - 产品部件（part_name, component_name...）
- `ProductPartMaterials`
  - 部件选用物料（drawing_number, quantity...）
- `CustomParts`
  - 自定义部件
- `ProductMaterialLibrary`
  - 产品物料统一库
- `MaterialGroups`
  - 物料分组
- `MaterialGroupItems`
  - 物料分组子项

### GlobalMaterialDB.db 表结构
- `Factories` / `FactoryMaterials` / `MaterialGroups` / `MaterialGroupItems`
  - 与项目库共享表结构，但数据独立

> 注意：部分视图使用 `DatabaseType` 参数决定使用哪个数据库，通过 `DbService(DatabaseType)` 构造

## 9. 视图与弹窗清单

### 9.1 主页面

| 页面 | 类型 | 主要职责 |
| --- | --- | --- |
| `FactoryView` | UserControl | 工厂列表与操作 |
| `MaterialView` | UserControl | 物料列表与操作 |
| `ProductManagementView` | UserControl | 产品列表与操作 |
| `BOMView` | UserControl | BOM 报表 |
| `PurchaseView` | UserControl | 采购相关 |
| `ContractView` | UserControl | 合同相关 |
| `ReportView` | UserControl | 综合报表 |

### 9.2 弹窗

| 弹窗 | 类型 | 主要职责 |
| --- | --- | --- |
| `FactoryDetailsWindow` | Window | 工厂详情 |
| `FactoryDialog` | Window | 工厂对话框 |
| `FactoryDialogWindow` | Window | 工厂对话框窗口 |
| `MaterialDetailsWindow` | Window | 物料详情 |
| `MaterialDialogWindow` | Window | 物料对话框窗口 |
| `EditProductDialog` | Window | 产品编辑 |
| `AddProductDialog` | Window | 新增产品 |
| `ProductDetailsWindow` | Window | 产品详情 |
| `AddProductMaterialWindow` | Window | 产品物料选择 |
| `MaterialSelectorDialog` | Window | 物料筛选选择 |
| `MaterialTypePickerDialog` | Window | 物料类型选择 |
| `MaterialGroupEditorDialog` | Window | 复合物料编辑 |
| `PartEditorDialog` | Window | 部件编辑 |
| `PartManagementDialog` | Window | 部件管理 |
| `CustomPartEditorDialog` | Window | 自定义部件编辑 |
| `DrawingNumberInputDialog` | Window | 图号输入 |
| `ImageViewerWindow` | Window | 图片查看 |

---

## 使用建议

- 遇到问题先看 **视图 -> 视图模型 -> DbService** 这个顺序。
- 若问题是“物料类型/分类筛选/编码”，优先查：
  - `AddProductMaterialWindow`
  - `MaterialSelectorDialog`
  - `MaterialCodeGenerator`
- 若问题是“产品部件/户型/物料回填”，优先查：
  - `ProductManagementDialog`
  - `ProductManagementViewModel`
  - `CustomPartEditorDialog`
