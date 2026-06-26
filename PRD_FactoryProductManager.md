# 工厂产品管理系统 (FactoryProductManager) - 项目规格说明书

## 1. 问题陈述

宇程科技需要一个桌面应用程序来管理智能家居产品的工厂物料、生产产品、部件配置等核心业务数据。系统需要支持多工厂管理、物料选择组合、产品配置等复杂业务场景。

## 2. 解决方案

基于 WPF 的 Windows 桌面应用程序，使用 SQLite 数据库存储，采用 MVVM 架构模式，提供工厂管理、物料管理、产品配置等核心功能模块。

## 3. 系统架构

### 3.1 技术栈
- **框架**: WPF (.NET)
- **数据库**: SQLite
- **UI 主题**: MaterialDesignInXaml
- **架构模式**: MVVM
- **日志服务**: 自定义 LogService，支持文件日志和内存缓存

### 3.2 核心模型

| 模型类 | 描述 | 主要属性 |
|--------|------|----------|
| `Factory` | 工厂 | Id, FactoryCode, FactoryName, ContactPerson, Phone, Address |
| `FactoryMaterial` | 物料 | Id, MaterialName, Brand, Specification, Unit, CostPrice, Category |
| `Product` | 产品 | Id, ProductCode, ProductName, BusinessType, Area, CostTotalPrice |
| `ProductPart` | 产品部件 | Id, ProductId, PartName, PartType |
| `ProductPartMaterial` | 部件物料关联 | ProductPartMaterialId, ProductPartId, FactoryMaterialId, Quantity |
| `CustomPart` | 自定义部件 | Id, PartName, IsCabinetType |
| `MaterialGroup` | 物料组合 | Id, GroupName, MaterialName, GroupType |
| `MaterialGroupItem` | 物料组合项 | Id, MaterialGroupId, ItemName, FactoryMaterialId |
| `CompositeMaterial` | 复合物料 | Id, MaterialName, GroupType |
| `CompositeMaterialItem` | 复合物料子项 | Id, CompositeMaterialId, ItemName, FactoryMaterialId, Quantity |

### 3.3 服务层

| 服务类 | 职责 |
|--------|------|
| `DbService` | 数据库 CRUD 操作，数据验证 |
| `LogService` | 日志记录，支持 INFO/DEBUG/ERROR 级别 |
| `WindowPositionService` | 窗口位置持久化 |
| `MaterialCodeGenerator` | 物料编码自动生成 |
| `CompositeMaterialService` | 复合物料管理 |

### 3.4 视图层 (XAML 窗口)

#### 无边框窗口（需配置 WindowDragBehavior）
- `FactoryDialogWindow.xaml` - 工厂对话框
- `MaterialDialogWindow.xaml` - 物料对话框
- `MaterialSelectorDialog.xaml` - 物料选择器
- `AddProductMaterialWindow.xaml` - 添加产品物料
- `MaterialGroupEditorDialog.xaml` - 物料组合编辑器
- `MaterialDetailsWindow.xaml` - 物料详情
- `ProductDetailsWindow.xaml` - 产品详情
- `FactoryDetailsWindow.xaml` - 工厂详情
- `MaterialTypePickerDialog.xaml` - 物料类型选择器
- `ImageViewerWindow.xaml` - 图片查看器

#### 标准窗口（使用原生标题栏）
- `ProductManagementDialog.xaml` - 产品管理
- `CustomPartEditorDialog.xaml` - 自定义部件编辑
- `PartEditorDialog.xaml` - 部件编辑
- `PartManagementDialog.xaml` - 部件管理

### 3.5 视图模型

- `FactoryViewModel` - 工厂数据管理
- `MaterialViewModel` - 物料数据管理
- `ProductManagementViewModel` - 产品管理业务逻辑
- `MainViewModel` - 主窗口状态
- `ViewModelBase` - MVVM 基类（INotifyPropertyChanged）

## 4. 用户故事

### 4.1 工厂管理
1. 作为管理员，我想要添加、编辑、删除工厂信息，以便维护供应商数据
2. 作为用户，我想要查看工厂详细信息，包括联系方式和地址

### 4.2 物料管理
1. 作为管理员，我想要管理物料库，包括新增、编辑、删除物料
2. 作为用户，我想要按分类浏览和搜索物料
3. 作为用户，我想要查看物料详情和图片

### 4.3 产品配置
1. 作为用户，我想要创建和管理产品，记录产品代码、名称、面积等信息
2. 作为用户，我想要为产品分配部件（柜体、门板、台面、五金等）
3. 作为用户，我想要为每个部件选择具体物料及其用量
4. 作为用户，我想要使用物料组合模板快速配置
5. 作为用户，我想要自定义部件类型

### 4.4 UI/UX
1. 作为用户，我想要所有弹窗都可以通过鼠标拖动，以便调整窗口位置
2. 作为用户，我想要统一的视觉风格和交互体验

## 5. 实现决策

### 5.1 窗口拖动实现
- 使用附加属性 `WindowDragBehavior.Enable` 实现
- 在窗口的 Border 元素上设置该属性
- 已在以下窗口实现：`MaterialSelectorDialog`, `MaterialDialogWindow`, `FactoryDialogWindow`
- 2026-06-23 补充实现：`AddProductMaterialWindow`, `MaterialGroupEditorDialog`, `MaterialDetailsWindow`, `ProductDetailsWindow`, `FactoryDetailsWindow`, `MaterialTypePickerDialog`

### 5.2 数据库设计
- 使用 SQLite，单文件数据库
- 主键使用自增整数 ID
- 包含 CreatedAt/UpdatedAt 时间戳字段
- 支持软删除（IsActive 标志）

### 5.3 日志系统
- 每日生成独立日志文件：`Logs/Log_YYYYMMDD.txt`
- 支持日志级别：INFO, DEBUG, ERROR, BOOT
- 保留最近 30 天日志
- SessionID + PID + TID 追踪

## 6. 测试策略

### 6.1 窗口功能测试
- 验证所有无边框窗口可以正常拖动
- 验证关闭按钮功能正常
- 验证窗口位置和大小的保存与恢复

### 6.2 数据操作测试
- 验证 CRUD 操作的正确性
- 验证数据库事务的回滚机制
- 验证外键约束的有效性

## 7. 超出范围

- 多用户并发支持
- 数据导出/导入功能
- 云端同步
- 移动端支持

## 8. 后续工作记录

### 2026-06-23
- **修复**: 统一所有无边框窗口的拖动行为
  - 为 6 个缺失 `WindowDragBehavior.Enable="True"` 的窗口添加了拖动支持
  - 涉及窗口：
    - `AddProductMaterialWindow.xaml`
    - `MaterialGroupEditorDialog.xaml`
    - `MaterialDetailsWindow.xaml`
    - `ProductDetailsWindow.xaml`
    - `FactoryDetailsWindow.xaml`
    - `MaterialTypePickerDialog.xaml`
  - 状态：已完成

- **修复**: 物料数量小数位无法保存问题
  - 问题：`ProductMaterialLibrary` 表缺少 `quantity` 列
  - 解决：添加 `ALTER TABLE ProductMaterialLibrary ADD COLUMN quantity REAL` 自动创建逻辑
  - 修改文件：`Services/DbService.cs`

- **优化**: 计算式显示格式
  - 问题：物料数量显示被四舍五入（0.8 显示为 1）
  - 解决：整数显示整数，小数保留两位
  - 修改文件：`Views/MaterialGroupEditorDialog.xaml.cs`

- **健康度检查**: 项目编译状态
  - 构建结果：0 个警告，0 个错误
  - 构建命令：`dotnet build ... --no-restore`
  - 注意：Cursor 内置终端存在卡顿问题，建议使用外部 PowerShell

### 2026-06-22
- 新增 `CompositeMaterial` 和 `CompositeMaterialItem` 模型
- 新增 `CompositeMaterialService` 服务
- 物料组合功能开发

### 2026-06-21
- 修复 WindowDragBehavior 焦点问题
- 支持 ScrollBar/Slider 交互元素

---

*文档生成时间: 2026-06-23*
