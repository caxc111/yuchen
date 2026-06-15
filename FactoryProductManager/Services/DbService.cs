using FactoryProductManager.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FactoryProductManager.Services
{
    public class DbService
    {
        private readonly string _connectionString;

        public DbService(string? databasePath = null)
        {
            if (string.IsNullOrEmpty(databasePath))
            {
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string projectDirectory = Path.GetFullPath(Path.Combine(appDirectory, "..", "..", "..", ".."));
                databasePath = Path.Combine(projectDirectory, "FactoryProductManager", "FactoryProductDB.db");
            }
            _connectionString = $"Data Source={databasePath};Version=3;BusyTimeout=3000;";
            CreateTables();
        }

        private void CreateTables()
        {
            using (var conn = GetConnection())
            {
                conn.Open();

                string createFactoriesTable = @"
                    CREATE TABLE IF NOT EXISTS Factories (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        factory_code TEXT NOT NULL UNIQUE,
                        factory_name TEXT NOT NULL,
                        brand TEXT,
                        factory_type TEXT,
                        address TEXT,
                        certifications TEXT,
                        description TEXT,
                        scale TEXT,
                        employee_count INTEGER,
                        production_capacity TEXT,
                        controlling_person TEXT,
                        contact_person TEXT,
                        contact_info TEXT,
                        contact_method TEXT,
                        created_at TEXT,
                        updated_at TEXT
                    )";

                using (var cmd = new SQLiteCommand(createFactoriesTable, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                EnsureFactoriesSchema(conn);

                string createMaterialsTable = @"
                    CREATE TABLE IF NOT EXISTS FactoryProducts (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        factory_product_code TEXT NOT NULL,
                        my_product_code TEXT,
                        product_name TEXT NOT NULL,
                        brand TEXT,
                        specification TEXT,
                        texture TEXT,
                        process TEXT,
                        unit TEXT,
                        cost_price REAL,
                        usage_scenario TEXT,
                        certifications TEXT,
                        category TEXT,
                        image_url TEXT,
                        factory_id INTEGER,
                        created_at TEXT,
                        updated_at TEXT,
                        FOREIGN KEY (factory_id) REFERENCES Factories(id)
                    )";

                using (var cmd = new SQLiteCommand(createMaterialsTable, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                EnsureFactoryProductsSchema(conn);

                string createManagedProductsTable = @"
                    CREATE TABLE IF NOT EXISTS Products (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        business_type TEXT,
                        product_code TEXT NOT NULL UNIQUE,
                        project_name TEXT,
                        house_type TEXT,
                        area REAL DEFAULT 0,
                        cost_total_price REAL DEFAULT 0,
                        selling_total_price REAL,
                        floor_plan TEXT,
                        is_active INTEGER DEFAULT 1,
                        created_at TEXT,
                        updated_at TEXT
                    )";

                using (var cmd = new SQLiteCommand(createManagedProductsTable, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                EnsureProductsSchema(conn);
                EnsureProductPartsSchema(conn);
                EnsureCustomPartsSchema(conn);
                EnsureMaterialGroupsSchema(conn);
            }
        }

        public SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(_connectionString);
        }

        private object ToDbValue(string value)
        {
            return string.IsNullOrEmpty(value) ? DBNull.Value : (object)value;
        }

        private object ToDbValue(int? value)
        {
            return value.HasValue ? (object)value.Value : DBNull.Value;
        }

        private void EnsureFactoriesSchema(SQLiteConnection conn)
        {
            if (!ColumnExists(conn, "Factories", "brand"))
            {
                using var cmd = new SQLiteCommand("ALTER TABLE Factories ADD COLUMN brand TEXT", conn);
                cmd.ExecuteNonQuery();
            }
        }

        private void EnsureFactoryProductsSchema(SQLiteConnection conn)
        {
            if (!ColumnExists(conn, "FactoryProducts", "unit"))
            {
                using var cmd = new SQLiteCommand("ALTER TABLE FactoryProducts ADD COLUMN unit TEXT", conn);
                cmd.ExecuteNonQuery();
            }

            if (!ColumnExists(conn, "FactoryProducts", "cost_price"))
            {
                using var cmd = new SQLiteCommand("ALTER TABLE FactoryProducts ADD COLUMN cost_price REAL", conn);
                cmd.ExecuteNonQuery();
            }
        }

        private void EnsureProductsSchema(SQLiteConnection conn)
        {
            if (!ColumnExists(conn, "Products", "business_type"))
            {
                using var cmd = new SQLiteCommand("ALTER TABLE Products ADD COLUMN business_type TEXT", conn);
                cmd.ExecuteNonQuery();
            }

            if (!ColumnExists(conn, "Products", "house_type"))
            {
                using var cmd = new SQLiteCommand("ALTER TABLE Products ADD COLUMN house_type TEXT", conn);
                cmd.ExecuteNonQuery();
            }

            if (!ColumnExists(conn, "Products", "area"))
            {
                using var cmd = new SQLiteCommand("ALTER TABLE Products ADD COLUMN area REAL DEFAULT 0", conn);
                cmd.ExecuteNonQuery();
            }

            if (!ColumnExists(conn, "Products", "cost_total_price"))
            {
                using var cmd = new SQLiteCommand("ALTER TABLE Products ADD COLUMN cost_total_price REAL DEFAULT 0", conn);
                cmd.ExecuteNonQuery();
            }

            if (!ColumnExists(conn, "Products", "selling_total_price"))
            {
                using var cmd = new SQLiteCommand("ALTER TABLE Products ADD COLUMN selling_total_price REAL", conn);
                cmd.ExecuteNonQuery();
            }

            if (!ColumnExists(conn, "Products", "floor_plan"))
            {
                using var cmd = new SQLiteCommand("ALTER TABLE Products ADD COLUMN floor_plan TEXT", conn);
                cmd.ExecuteNonQuery();
            }

            if (!ColumnExists(conn, "Products", "product_name"))
            {
                using var cmd = new SQLiteCommand("ALTER TABLE Products ADD COLUMN product_name TEXT", conn);
                cmd.ExecuteNonQuery();
            }
            else
            {
                // 检查列是否有 NOT NULL 约束，如果有则需要重建表
                using var checkCmd = new SQLiteCommand("PRAGMA table_info(Products)", conn);
                using var reader = checkCmd.ExecuteReader();
                bool hasNotNull = false;
                while (reader.Read())
                {
                    // 列结构: cid, name, type, notnull, dflt_value, pk
                    if (reader.GetString(1).Equals("product_name", StringComparison.OrdinalIgnoreCase))
                    {
                        hasNotNull = reader.GetInt32(3) == 1;
                        break;
                    }
                }
                reader.Close();

                if (hasNotNull)
                {
                    // 需要重建表：重命名旧表，创建新表，迁移数据
                    using var dropCmd = new SQLiteCommand(@"
                        ALTER TABLE Products RENAME TO Products_old;
                        CREATE TABLE Products (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            business_type TEXT,
                            product_code TEXT NOT NULL UNIQUE,
                            product_name TEXT,
                            project_name TEXT,
                            house_type TEXT,
                            area REAL DEFAULT 0,
                            cost_total_price REAL DEFAULT 0,
                            selling_total_price REAL,
                            floor_plan TEXT,
                            is_active INTEGER DEFAULT 1,
                            created_at TEXT,
                            updated_at TEXT
                        );
                        INSERT INTO Products (id, business_type, product_code, product_name, project_name, house_type, area, cost_total_price, selling_total_price, floor_plan, is_active, created_at, updated_at)
                        SELECT id, business_type, product_code, product_name, project_name, house_type, area, cost_total_price, selling_total_price, floor_plan, is_active, created_at, updated_at FROM Products_old;
                        DROP TABLE Products_old;", conn);
                    dropCmd.ExecuteNonQuery();
                    LogService.Info("Products表已重建，移除了product_name的NOT NULL约束");
                }
            }

            if (!ColumnExists(conn, "Products", "project_name"))
            {
                using var cmd = new SQLiteCommand("ALTER TABLE Products ADD COLUMN project_name TEXT", conn);
                cmd.ExecuteNonQuery();
            }
        }

        private void EnsureProductPartsSchema(SQLiteConnection conn)
        {
            string createProductPartsTable = @"
                CREATE TABLE IF NOT EXISTS ProductParts (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    product_id INTEGER NOT NULL,
                    part_name TEXT NOT NULL,
                    part_code TEXT,
                    part_type TEXT,
                    material TEXT,
                    specification TEXT,
                    quantity REAL DEFAULT 0,
                    unit TEXT,
                    unit_price REAL DEFAULT 0,
                    total_price REAL DEFAULT 0,
                    remarks TEXT,
                    is_active INTEGER DEFAULT 1,
                    created_at TEXT,
                    updated_at TEXT,
                    FOREIGN KEY (product_id) REFERENCES Products(id)
                )";

            using (var cmd = new SQLiteCommand(createProductPartsTable, conn))
            {
                cmd.ExecuteNonQuery();
            }

            EnsureProductPartMaterialsSchema(conn);
        }

        private void EnsureProductPartMaterialsSchema(SQLiteConnection conn)
        {
            string createTable = @"
                CREATE TABLE IF NOT EXISTS ProductPartMaterials (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    product_id INTEGER NOT NULL,
                    part_id INTEGER,
                    part_name TEXT NOT NULL,
                    component_name TEXT NOT NULL,
                    material_type_name TEXT,
                    material_id INTEGER,
                    material_name TEXT NOT NULL,
                    factory_material_code TEXT,
                    my_material_code TEXT,
                    brand TEXT,
                    specification TEXT,
                    unit TEXT,
                    unit_price REAL DEFAULT 0,
                    quantity REAL DEFAULT 0,
                    total_price REAL DEFAULT 0,
                    remarks TEXT,
                    created_at TEXT,
                    updated_at TEXT,
                    FOREIGN KEY (product_id) REFERENCES Products(id),
                    FOREIGN KEY (part_id) REFERENCES ProductParts(id),
                    FOREIGN KEY (material_id) REFERENCES FactoryProducts(id)
                )";

            using (var cmd = new SQLiteCommand(createTable, conn))
            {
                cmd.ExecuteNonQuery();
            }

            if (!IndexExists(conn, "ProductPartMaterials", "idx_ppm_product_id"))
            {
                using var idxCmd = new SQLiteCommand("CREATE INDEX idx_ppm_product_id ON ProductPartMaterials(product_id)", conn);
                idxCmd.ExecuteNonQuery();
            }
            if (!IndexExists(conn, "ProductPartMaterials", "idx_ppm_part_id"))
            {
                using var idxCmd = new SQLiteCommand("CREATE INDEX idx_ppm_part_id ON ProductPartMaterials(part_id)", conn);
                idxCmd.ExecuteNonQuery();
            }

            // 复合物料相关列（方案 B：MaterialGroup）
            if (!ColumnExists(conn, "ProductPartMaterials", "is_composite"))
            {
                using var cmd = new SQLiteCommand("ALTER TABLE ProductPartMaterials ADD COLUMN is_composite INTEGER DEFAULT 0", conn);
                cmd.ExecuteNonQuery();
            }
            if (!ColumnExists(conn, "ProductPartMaterials", "group_code"))
            {
                using var cmd = new SQLiteCommand("ALTER TABLE ProductPartMaterials ADD COLUMN group_code TEXT", conn);
                cmd.ExecuteNonQuery();
            }
            if (!ColumnExists(conn, "ProductPartMaterials", "item_name"))
            {
                using var cmd = new SQLiteCommand("ALTER TABLE ProductPartMaterials ADD COLUMN item_name TEXT", conn);
                cmd.ExecuteNonQuery();
            }
            if (!ColumnExists(conn, "ProductPartMaterials", "parent_id"))
            {
                using var cmd = new SQLiteCommand("ALTER TABLE ProductPartMaterials ADD COLUMN parent_id INTEGER", conn);
                cmd.ExecuteNonQuery();
            }
            if (!IndexExists(conn, "ProductPartMaterials", "idx_ppm_parent_id"))
            {
                using var idxCmd = new SQLiteCommand("CREATE INDEX idx_ppm_parent_id ON ProductPartMaterials(parent_id)", conn);
                idxCmd.ExecuteNonQuery();
            }
        }

        // ===== MaterialGroup / MaterialGroupItem =====

        private void EnsureMaterialGroupsSchema(SQLiteConnection conn)
        {
            string createGroupsTable = @"
                CREATE TABLE IF NOT EXISTS MaterialGroups (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    group_code TEXT NOT NULL UNIQUE,
                    group_name TEXT NOT NULL,
                    category TEXT,
                    description TEXT,
                    is_active INTEGER DEFAULT 1,
                    created_at TEXT,
                    updated_at TEXT
                )";

            using (var cmd = new SQLiteCommand(createGroupsTable, conn))
            {
                cmd.ExecuteNonQuery();
            }

            string createItemsTable = @"
                CREATE TABLE IF NOT EXISTS MaterialGroupItems (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    group_id INTEGER NOT NULL,
                    item_name TEXT NOT NULL,
                    item_order INTEGER DEFAULT 0,
                    material_type TEXT,
                    selection_rule TEXT DEFAULT 'Single',
                    is_required INTEGER DEFAULT 1,
                    prompt TEXT,
                    FOREIGN KEY (group_id) REFERENCES MaterialGroups(id) ON DELETE CASCADE
                )";

            using (var cmd = new SQLiteCommand(createItemsTable, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public List<MaterialGroup> GetMaterialGroups()
        {
            var list = new List<MaterialGroup>();
            try
            {
                using var conn = GetConnection();
                conn.Open();
                var cmd = new SQLiteCommand(
                    "SELECT id, group_code, group_name, category, description, is_active, created_at, updated_at FROM MaterialGroups WHERE is_active = 1 ORDER BY id", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new MaterialGroup
                    {
                        Id = reader.GetInt32(0),
                        GroupCode = reader.GetString(1),
                        GroupName = reader.GetString(2),
                        Category = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        Description = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        IsActive = !reader.IsDBNull(5) && reader.GetInt32(5) == 1,
                        CreatedAt = reader.IsDBNull(6) ? DateTime.Now : DateTime.Parse(reader.GetString(6)),
                        UpdatedAt = reader.IsDBNull(7) ? DateTime.Now : DateTime.Parse(reader.GetString(7))
                    });
                }

                // 批量加载子项
                if (list.Count > 0)
                {
                    var itemCmd = new SQLiteCommand(
                        "SELECT id, group_id, item_name, item_order, material_type, selection_rule, is_required, prompt FROM MaterialGroupItems ORDER BY group_id, item_order", conn);
                    using var itemReader = itemCmd.ExecuteReader();
                    var byGroup = list.ToDictionary(g => g.Id);
                    while (itemReader.Read())
                    {
                        int gid = itemReader.GetInt32(1);
                        if (byGroup.TryGetValue(gid, out var g))
                        {
                            g.Items.Add(new MaterialGroupItem
                            {
                                Id = itemReader.GetInt32(0),
                                GroupId = gid,
                                ItemName = itemReader.GetString(2),
                                ItemOrder = itemReader.GetInt32(3),
                                MaterialType = itemReader.IsDBNull(4) ? string.Empty : itemReader.GetString(4),
                                SelectionRule = itemReader.IsDBNull(5) ? SelectionRuleType.Single : itemReader.GetString(5),
                                IsRequired = !itemReader.IsDBNull(6) && itemReader.GetInt32(6) == 1,
                                Prompt = itemReader.IsDBNull(7) ? string.Empty : itemReader.GetString(7)
                            });
                        }
                    }
                }

                LogService.Info($"查询物料组合模板完成，共 {list.Count} 条");
                return list;
            }
            catch (Exception ex)
            {
                LogService.Error("查询物料组合模板失败", ex);
                throw;
            }
        }

        public MaterialGroup? GetMaterialGroupByCode(string groupCode)
        {
            if (string.IsNullOrWhiteSpace(groupCode)) return null;
            try
            {
                using var conn = GetConnection();
                conn.Open();
                var cmd = new SQLiteCommand(
                    "SELECT id, group_code, group_name, category, description, is_active, created_at, updated_at FROM MaterialGroups WHERE group_code = @code LIMIT 1", conn);
                cmd.Parameters.AddWithValue("@code", groupCode.Trim());
                using var reader = cmd.ExecuteReader();
                if (!reader.Read()) return null;
                var g = new MaterialGroup
                {
                    Id = reader.GetInt32(0),
                    GroupCode = reader.GetString(1),
                    GroupName = reader.GetString(2),
                    Category = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Description = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    IsActive = !reader.IsDBNull(5) && reader.GetInt32(5) == 1,
                    CreatedAt = reader.IsDBNull(6) ? DateTime.Now : DateTime.Parse(reader.GetString(6)),
                    UpdatedAt = reader.IsDBNull(7) ? DateTime.Now : DateTime.Parse(reader.GetString(7))
                };
                reader.Close();

                var itemCmd = new SQLiteCommand(
                    "SELECT id, group_id, item_name, item_order, material_type, selection_rule, is_required, prompt FROM MaterialGroupItems WHERE group_id = @gid ORDER BY item_order", conn);
                itemCmd.Parameters.AddWithValue("@gid", g.Id);
                using var itemReader = itemCmd.ExecuteReader();
                while (itemReader.Read())
                {
                    g.Items.Add(new MaterialGroupItem
                    {
                        Id = itemReader.GetInt32(0),
                        GroupId = g.Id,
                        ItemName = itemReader.GetString(2),
                        ItemOrder = itemReader.GetInt32(3),
                        MaterialType = itemReader.IsDBNull(4) ? string.Empty : itemReader.GetString(4),
                        SelectionRule = itemReader.IsDBNull(5) ? SelectionRuleType.Single : itemReader.GetString(5),
                        IsRequired = !itemReader.IsDBNull(6) && itemReader.GetInt32(6) == 1,
                        Prompt = itemReader.IsDBNull(7) ? string.Empty : itemReader.GetString(7)
                    });
                }
                return g;
            }
            catch (Exception ex)
            {
                LogService.Error($"按编码查询物料组合失败: code={groupCode}", ex);
                throw;
            }
        }

        public int AddMaterialGroup(MaterialGroup group)
        {
            if (group == null || string.IsNullOrWhiteSpace(group.GroupCode) || string.IsNullOrWhiteSpace(group.GroupName))
            {
                throw new ArgumentException("组合编码和名称不能为空");
            }
            try
            {
                using var conn = GetConnection();
                conn.Open();
                using var tx = conn.BeginTransaction();

                var cmd = new SQLiteCommand(@"
                    INSERT INTO MaterialGroups (group_code, group_name, category, description, is_active, created_at, updated_at)
                    VALUES (@code, @name, @category, @desc, @active, @createdAt, @updatedAt);
                    SELECT last_insert_rowid();", conn, tx);
                cmd.Parameters.AddWithValue("@code", group.GroupCode.Trim());
                cmd.Parameters.AddWithValue("@name", group.GroupName.Trim());
                cmd.Parameters.AddWithValue("@category", ToDbValue(group.Category));
                cmd.Parameters.AddWithValue("@desc", ToDbValue(group.Description));
                cmd.Parameters.AddWithValue("@active", group.IsActive ? 1 : 0);
                cmd.Parameters.AddWithValue("@createdAt", group.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@updatedAt", group.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                int newId = Convert.ToInt32(cmd.ExecuteScalar());

                foreach (var item in group.Items)
                {
                    var itemCmd = new SQLiteCommand(@"
                        INSERT INTO MaterialGroupItems (group_id, item_name, item_order, material_type, selection_rule, is_required, prompt)
                        VALUES (@gid, @name, @order, @mtype, @rule, @req, @prompt)", conn, tx);
                    itemCmd.Parameters.AddWithValue("@gid", newId);
                    itemCmd.Parameters.AddWithValue("@name", item.ItemName);
                    itemCmd.Parameters.AddWithValue("@order", item.ItemOrder);
                    itemCmd.Parameters.AddWithValue("@mtype", ToDbValue(item.MaterialType));
                    itemCmd.Parameters.AddWithValue("@rule", item.SelectionRule);
                    itemCmd.Parameters.AddWithValue("@req", item.IsRequired ? 1 : 0);
                    itemCmd.Parameters.AddWithValue("@prompt", ToDbValue(item.Prompt));
                    itemCmd.ExecuteNonQuery();
                }

                tx.Commit();
                LogService.Info($"新增物料组合成功: ID={newId}, code={group.GroupCode}, 子项 {group.Items.Count} 个");
                return newId;
            }
            catch (Exception ex)
            {
                LogService.Error($"新增物料组合失败: code={group.GroupCode}", ex);
                throw;
            }
        }

        /// <summary>
        /// 种子数据：3 个柜类模板（橱柜/洗衣柜/中岛台），仅在数据库无任何 MaterialGroups 记录时插入。
        /// </summary>
        public void SeedDefaultMaterialGroups()
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();

                var checkCmd = new SQLiteCommand("SELECT COUNT(*) FROM MaterialGroups", conn);
                long count = (long)checkCmd.ExecuteScalar();
                if (count > 0) return; // 已有数据，跳过 seed

                var seeds = new List<MaterialGroup>
                {
                    new MaterialGroup
                    {
                        GroupCode = "CB-001", GroupName = "定制橱柜", Category = "柜类",
                        Description = "厨房定制橱柜：柜体+门板+台面+五金+拉手",
                        Items = new List<MaterialGroupItem>
                        {
                            new MaterialGroupItem { ItemName = "柜体", ItemOrder = 1, MaterialType = "板材", IsRequired = true },
                            new MaterialGroupItem { ItemName = "门板", ItemOrder = 2, MaterialType = "门板", IsRequired = true },
                            new MaterialGroupItem { ItemName = "台面", ItemOrder = 3, MaterialType = "石英石,大理石,岩板", IsRequired = true, Prompt = "请选择台面材质（石英石/大理石/岩板 三选一）" },
                            new MaterialGroupItem { ItemName = "五金", ItemOrder = 4, MaterialType = "五金", IsRequired = true },
                            new MaterialGroupItem { ItemName = "拉手", ItemOrder = 5, MaterialType = "拉手", IsRequired = false, SelectionRule = SelectionRuleType.SingleOrNone }
                        }
                    },
                    new MaterialGroup
                    {
                        GroupCode = "XYG-001", GroupName = "洗衣柜", Category = "柜类",
                        Description = "洗衣房洗衣柜：柜体+门板+台面+五金",
                        Items = new List<MaterialGroupItem>
                        {
                            new MaterialGroupItem { ItemName = "柜体", ItemOrder = 1, MaterialType = "板材", IsRequired = true },
                            new MaterialGroupItem { ItemName = "门板", ItemOrder = 2, MaterialType = "门板", IsRequired = true },
                            new MaterialGroupItem { ItemName = "台面", ItemOrder = 3, MaterialType = "石英石,大理石,岩板", IsRequired = true, Prompt = "请选择台面材质（石英石/大理石/岩板 三选一）" },
                            new MaterialGroupItem { ItemName = "五金", ItemOrder = 4, MaterialType = "五金", IsRequired = true }
                        }
                    },
                    new MaterialGroup
                    {
                        GroupCode = "ZDT-001", GroupName = "餐厨中岛台", Category = "柜类",
                        Description = "餐厨中岛台：柜体+门板+台面+五金",
                        Items = new List<MaterialGroupItem>
                        {
                            new MaterialGroupItem { ItemName = "柜体", ItemOrder = 1, MaterialType = "板材", IsRequired = true },
                            new MaterialGroupItem { ItemName = "门板", ItemOrder = 2, MaterialType = "门板", IsRequired = true },
                            new MaterialGroupItem { ItemName = "台面", ItemOrder = 3, MaterialType = "石英石,大理石,岩板", IsRequired = true, Prompt = "请选择台面材质（石英石/大理石/岩板 三选一）" },
                            new MaterialGroupItem { ItemName = "五金", ItemOrder = 4, MaterialType = "五金", IsRequired = true }
                        }
                    }
                };

                foreach (var g in seeds)
                {
                    AddMaterialGroup(g);
                }
                LogService.Info($"种子数据插入完成，共 {seeds.Count} 个柜类模板");
            }
            catch (Exception ex)
            {
                LogService.Error("种子物料组合模板失败", ex);
            }
        }

        private bool IndexExists(SQLiteConnection conn, string tableName, string indexName)
        {
            using var cmd = new SQLiteCommand($"PRAGMA index_list({tableName})", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader["name"]?.ToString(), indexName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private bool ColumnExists(SQLiteConnection conn, string tableName, string columnName)
        {
            using var cmd = new SQLiteCommand($"PRAGMA table_info({tableName})", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader[1]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public List<Factory> GetFactories()
        {
            LogService.Info("开始查询工厂列表");
            var factories = new List<Factory>();
            try
            {
                using (var conn = GetConnection())
                {
                    LogService.LogDatabaseConnection(_connectionString);
                    conn.Open();

                    var cmd = new SQLiteCommand("SELECT * FROM Factories ORDER BY factory_code", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                factories.Add(new Factory
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                                    FactoryCode = reader.GetString(reader.GetOrdinal("factory_code")),
                                    FactoryName = reader.GetString(reader.GetOrdinal("factory_name")),
                                    Brand = reader.IsDBNull(reader.GetOrdinal("brand")) ? string.Empty : reader.GetString(reader.GetOrdinal("brand")),
                                    FactoryType = reader.IsDBNull(reader.GetOrdinal("factory_type")) ? string.Empty : reader.GetString(reader.GetOrdinal("factory_type")),
                                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? string.Empty : reader.GetString(reader.GetOrdinal("address")),
                                    Certifications = reader.IsDBNull(reader.GetOrdinal("certifications")) ? string.Empty : reader.GetString(reader.GetOrdinal("certifications")),
                                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? string.Empty : reader.GetString(reader.GetOrdinal("description")),
                                    Scale = reader.IsDBNull(reader.GetOrdinal("scale")) ? string.Empty : reader.GetString(reader.GetOrdinal("scale")),
                                    EmployeeCount = reader.IsDBNull(reader.GetOrdinal("employee_count")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("employee_count")),
                                    ProductionCapacity = reader.IsDBNull(reader.GetOrdinal("production_capacity")) ? string.Empty : reader.GetString(reader.GetOrdinal("production_capacity")),
                                    ControllingPerson = reader.IsDBNull(reader.GetOrdinal("controlling_person")) ? string.Empty : reader.GetString(reader.GetOrdinal("controlling_person")),
                                    ContactPerson = reader.IsDBNull(reader.GetOrdinal("contact_person")) ? string.Empty : reader.GetString(reader.GetOrdinal("contact_person")),
                                    ContactInfo = reader.IsDBNull(reader.GetOrdinal("contact_info")) ? string.Empty : reader.GetString(reader.GetOrdinal("contact_info")),
                                    CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at")) ? DateTime.Now : DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at"))),
                                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? DateTime.Now : DateTime.Parse(reader.GetString(reader.GetOrdinal("updated_at")))
                                });
                            }
                            catch (Exception ex)
                            {
                                LogService.Error("读取工厂记录失败", ex);
                            }
                        }
                    }
                }

                LogService.Info($"查询工厂列表完成，共 {factories.Count} 条记录");
            }
            catch (Exception ex)
            {
                LogService.Error($"查询工厂列表失败: {ex.Message}", ex);
                throw;
            }
            return factories;
        }

        public string GetNextFactoryCode()
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                var cmd = new SQLiteCommand(@"
                    SELECT factory_code FROM Factories
                    WHERE factory_code LIKE 'S%'
                    ORDER BY factory_code", conn);
                using var reader = cmd.ExecuteReader();
                var existingCodes = new List<int>();
                while (reader.Read())
                {
                    var code = reader.GetString(0);
                    if (code.Length > 1 && int.TryParse(code.Substring(1), out int num))
                    {
                        existingCodes.Add(num);
                    }
                }
                int next = 1;
                while (existingCodes.Contains(next))
                {
                    next++;
                }
                return $"S{next:D3}";
            }
            catch (Exception ex)
            {
                LogService.Error("获取下一个工厂编码失败", ex);
                throw;
            }
        }

        public List<FactoryMaterial> GetFactoryMaterials()
        {
            var materials = new List<FactoryMaterial>();
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SQLiteCommand(@"
                    SELECT p.*, f.factory_name 
                    FROM FactoryProducts p 
                    LEFT JOIN Factories f ON p.factory_id = f.id 
                    ORDER BY p.factory_product_code", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            materials.Add(new FactoryMaterial
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                FactoryMaterialCode = reader.GetString(reader.GetOrdinal("factory_product_code")),
                                MyMaterialCode = reader.IsDBNull(reader.GetOrdinal("my_product_code")) ? string.Empty : reader.GetString(reader.GetOrdinal("my_product_code")),
                                MaterialName = reader.GetString(reader.GetOrdinal("product_name")),
                                Brand = reader.IsDBNull(reader.GetOrdinal("brand")) ? string.Empty : reader.GetString(reader.GetOrdinal("brand")),
                                Specification = reader.IsDBNull(reader.GetOrdinal("specification")) ? string.Empty : reader.GetString(reader.GetOrdinal("specification")),
                                Texture = reader.IsDBNull(reader.GetOrdinal("texture")) ? string.Empty : reader.GetString(reader.GetOrdinal("texture")),
                                Process = reader.IsDBNull(reader.GetOrdinal("process")) ? string.Empty : reader.GetString(reader.GetOrdinal("process")),
                                Unit = reader.IsDBNull(reader.GetOrdinal("unit")) ? string.Empty : reader.GetString(reader.GetOrdinal("unit")),
                                CostPrice = reader.IsDBNull(reader.GetOrdinal("cost_price")) ? null : Convert.ToDecimal(reader.GetValue(reader.GetOrdinal("cost_price"))),
                                UsageScenario = reader.IsDBNull(reader.GetOrdinal("usage_scenario")) ? string.Empty : reader.GetString(reader.GetOrdinal("usage_scenario")),
                                Certifications = reader.IsDBNull(reader.GetOrdinal("certifications")) ? string.Empty : reader.GetString(reader.GetOrdinal("certifications")),
                                Category = reader.IsDBNull(reader.GetOrdinal("category")) ? string.Empty : reader.GetString(reader.GetOrdinal("category")),
                                ImageUrl = reader.IsDBNull(reader.GetOrdinal("image_url")) ? string.Empty : reader.GetString(reader.GetOrdinal("image_url")),
                                FactoryId = reader.IsDBNull(reader.GetOrdinal("factory_id")) ? null : reader.GetInt32(reader.GetOrdinal("factory_id")),
                                FactoryName = reader.IsDBNull(reader.GetOrdinal("factory_name")) ? string.Empty : reader.GetString(reader.GetOrdinal("factory_name")),
                                CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at")) ? DateTime.Now : DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at"))),
                                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? DateTime.Now : DateTime.Parse(reader.GetString(reader.GetOrdinal("updated_at")))
                            });
                        }
                    }
                }

                LogService.Info($"查询工厂物料列表完成，共 {materials.Count} 条记录");
                return materials;
            }
            catch (Exception ex)
            {
                LogService.Error($"查询工厂物料列表失败: {ex.Message}", ex);
                throw;
            }
        }

        public List<FactoryMaterial> GetFactoryMaterialsByType(string materialType)
        {
            var materials = new List<FactoryMaterial>();
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SQLiteCommand(@"
                    SELECT p.*, f.factory_name 
                    FROM FactoryProducts p 
                    LEFT JOIN Factories f ON p.factory_id = f.id 
                    WHERE p.product_name = @materialType
                       OR p.category LIKE @materialTypePattern
                       OR p.texture = @materialType
                    ORDER BY p.factory_product_code", conn);
                    cmd.Parameters.AddWithValue("@materialType", materialType);
                    cmd.Parameters.AddWithValue("@materialTypePattern", $"%{materialType}%");
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            materials.Add(new FactoryMaterial
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                FactoryMaterialCode = reader.GetString(reader.GetOrdinal("factory_product_code")),
                                MyMaterialCode = reader.IsDBNull(reader.GetOrdinal("my_product_code")) ? string.Empty : reader.GetString(reader.GetOrdinal("my_product_code")),
                                MaterialName = reader.GetString(reader.GetOrdinal("product_name")),
                                Brand = reader.IsDBNull(reader.GetOrdinal("brand")) ? string.Empty : reader.GetString(reader.GetOrdinal("brand")),
                                Specification = reader.IsDBNull(reader.GetOrdinal("specification")) ? string.Empty : reader.GetString(reader.GetOrdinal("specification")),
                                Texture = reader.IsDBNull(reader.GetOrdinal("texture")) ? string.Empty : reader.GetString(reader.GetOrdinal("texture")),
                                Process = reader.IsDBNull(reader.GetOrdinal("process")) ? string.Empty : reader.GetString(reader.GetOrdinal("process")),
                                Unit = reader.IsDBNull(reader.GetOrdinal("unit")) ? string.Empty : reader.GetString(reader.GetOrdinal("unit")),
                                CostPrice = reader.IsDBNull(reader.GetOrdinal("cost_price")) ? null : Convert.ToDecimal(reader.GetValue(reader.GetOrdinal("cost_price"))),
                                UsageScenario = reader.IsDBNull(reader.GetOrdinal("usage_scenario")) ? string.Empty : reader.GetString(reader.GetOrdinal("usage_scenario")),
                                Certifications = reader.IsDBNull(reader.GetOrdinal("certifications")) ? string.Empty : reader.GetString(reader.GetOrdinal("certifications")),
                                Category = reader.IsDBNull(reader.GetOrdinal("category")) ? string.Empty : reader.GetString(reader.GetOrdinal("category")),
                                ImageUrl = reader.IsDBNull(reader.GetOrdinal("image_url")) ? string.Empty : reader.GetString(reader.GetOrdinal("image_url")),
                                FactoryId = reader.IsDBNull(reader.GetOrdinal("factory_id")) ? null : reader.GetInt32(reader.GetOrdinal("factory_id")),
                                FactoryName = reader.IsDBNull(reader.GetOrdinal("factory_name")) ? string.Empty : reader.GetString(reader.GetOrdinal("factory_name")),
                                CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at")) ? DateTime.Now : DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at"))),
                                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? DateTime.Now : DateTime.Parse(reader.GetString(reader.GetOrdinal("updated_at")))
                            });
                        }
                    }
                }

                LogService.Info($"按类型查询工厂物料完成，类型={materialType}，共 {materials.Count} 条记录");
                return materials;
            }
            catch (Exception ex)
            {
                LogService.Error($"按类型查询工厂物料列表失败: {ex.Message}", ex);
                throw;
            }
        }

        public List<Product> GetProducts(string? keyword = null)
        {
            var products = new List<Product>();
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string sql = @"
                    SELECT id, business_type, product_code, product_name, project_name, house_type, area, cost_total_price, selling_total_price, floor_plan, is_active, created_at, updated_at
                    FROM Products";

                    bool hasKeyword = !string.IsNullOrWhiteSpace(keyword);
                    if (hasKeyword)
                    {
                        sql += " WHERE product_code LIKE @keyword OR business_type LIKE @keyword OR product_name LIKE @keyword OR project_name LIKE @keyword OR house_type LIKE @keyword";
                    }

                    sql += " ORDER BY product_code";

                    var cmd = new SQLiteCommand(sql, conn);
                    if (hasKeyword && keyword != null)
                    {
                        cmd.Parameters.AddWithValue("@keyword", $"%{keyword.Trim()}%");
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new Product
                            {
                                Id = reader.GetInt32(0),
                                BusinessType = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                                ProductCode = reader.GetString(2),
                                ProductName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                ProjectCode = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                HouseType = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                                Area = reader.IsDBNull(6) ? 0 : Convert.ToDecimal(reader.GetValue(6)),
                                CostTotalPrice = reader.IsDBNull(7) ? 0 : Convert.ToDecimal(reader.GetValue(7)),
                                SellingTotalPrice = reader.IsDBNull(8) ? null : Convert.ToDecimal(reader.GetValue(8)),
                                FloorPlan = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                                IsActive = !reader.IsDBNull(10) && reader.GetInt32(10) == 1,
                                CreatedAt = reader.IsDBNull(11) ? DateTime.Now : DateTime.Parse(reader.GetString(11)),
                                UpdatedAt = reader.IsDBNull(12) ? DateTime.Now : DateTime.Parse(reader.GetString(12))
                            });
                        }
                    }
                }

                LogService.Info($"查询产品列表完成，共 {products.Count} 条记录");
                return products;
            }
            catch (Exception ex)
            {
                LogService.Error($"查询产品列表失败: {ex.Message}", ex);
                throw;
            }
        }

        public int GetNextProductCodeSequence(string codePrefix, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(codePrefix))
            {
                return 1;
            }

            int maxSequence = 0;
            using var conn = GetConnection();
            conn.Open();

            string sql = @"
                SELECT product_code
                FROM Products
                WHERE product_code LIKE @prefixPattern";

            if (excludeId.HasValue)
            {
                sql += " AND id <> @excludeId";
            }

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@prefixPattern", codePrefix.Trim() + "-%");
            if (excludeId.HasValue)
            {
                cmd.Parameters.AddWithValue("@excludeId", excludeId.Value);
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (reader.IsDBNull(0))
                {
                    continue;
                }

                string existingCode = reader.GetString(0);
                if (string.IsNullOrWhiteSpace(existingCode) || !existingCode.StartsWith(codePrefix + "-", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string[] parts = existingCode.Split('-');
                if (parts.Length == 4 && int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out int sequence))
                {
                    maxSequence = Math.Max(maxSequence, sequence);
                }
            }

            return maxSequence + 1;
        }

        public int AddFactory(Factory factory)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SQLiteCommand(@"
                    INSERT INTO Factories (factory_code, factory_name, brand, factory_type, address, 
                        certifications, description, scale, employee_count, production_capacity,
                        controlling_person, contact_person, contact_info)
                    VALUES (@code, @name, @brand, @type, @address, @cert, @desc, @scale, @empCount, 
                        @capacity, @controller, @contact, @contactInfo);
                    SELECT last_insert_rowid();", conn);
                    cmd.Parameters.AddWithValue("@code", factory.FactoryCode);
                    cmd.Parameters.AddWithValue("@name", factory.FactoryName);
                    cmd.Parameters.AddWithValue("@brand", ToDbValue(factory.Brand));
                    cmd.Parameters.AddWithValue("@type", ToDbValue(factory.FactoryType));
                    cmd.Parameters.AddWithValue("@address", ToDbValue(factory.Address));
                    cmd.Parameters.AddWithValue("@cert", ToDbValue(factory.Certifications));
                    cmd.Parameters.AddWithValue("@desc", ToDbValue(factory.Description));
                    cmd.Parameters.AddWithValue("@scale", ToDbValue(factory.Scale));
                    cmd.Parameters.AddWithValue("@empCount", ToDbValue(factory.EmployeeCount));
                    cmd.Parameters.AddWithValue("@capacity", ToDbValue(factory.ProductionCapacity));
                    cmd.Parameters.AddWithValue("@controller", ToDbValue(factory.ControllingPerson));
                    cmd.Parameters.AddWithValue("@contact", ToDbValue(factory.ContactPerson));
                    cmd.Parameters.AddWithValue("@contactInfo", ToDbValue(factory.ContactInfo));
                    int factoryId = Convert.ToInt32(cmd.ExecuteScalar());
                    LogService.Info($"新增工厂成功: ID={factoryId}, 编码={factory.FactoryCode}");
                    return factoryId;
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"新增工厂失败: 编码={factory.FactoryCode}", ex);
                throw;
            }
        }

        public int AddFactoryMaterial(FactoryMaterial material)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SQLiteCommand(@"
                    INSERT INTO FactoryProducts (factory_product_code, my_product_code, product_name, 
                        brand, specification, texture, process, unit, cost_price, usage_scenario, certifications, 
                        category, image_url, factory_id)
                    VALUES (@factoryCode, @myCode, @name, @brand, @spec, @texture, @process, @unit, @costPrice,
                        @scenario, @cert, @category, @image, @factoryId);
                    SELECT last_insert_rowid();", conn);
                    cmd.Parameters.AddWithValue("@factoryCode", material.FactoryMaterialCode);
                    cmd.Parameters.AddWithValue("@myCode", ToDbValue(material.MyMaterialCode));
                    cmd.Parameters.AddWithValue("@name", material.MaterialName);
                    cmd.Parameters.AddWithValue("@brand", ToDbValue(material.Brand));
                    cmd.Parameters.AddWithValue("@spec", ToDbValue(material.Specification));
                    cmd.Parameters.AddWithValue("@texture", ToDbValue(material.Texture));
                    cmd.Parameters.AddWithValue("@process", ToDbValue(material.Process));
                    cmd.Parameters.AddWithValue("@unit", ToDbValue(material.Unit));
                    cmd.Parameters.AddWithValue("@costPrice", material.CostPrice.HasValue ? (object)material.CostPrice.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@scenario", ToDbValue(material.UsageScenario));
                    cmd.Parameters.AddWithValue("@cert", ToDbValue(material.Certifications));
                    cmd.Parameters.AddWithValue("@category", ToDbValue(material.Category));
                    cmd.Parameters.AddWithValue("@image", ToDbValue(material.ImageUrl));
                    cmd.Parameters.AddWithValue("@factoryId", ToDbValue(material.FactoryId));
                    int materialId = Convert.ToInt32(cmd.ExecuteScalar());
                    LogService.Info($"新增工厂物料成功: ID={materialId}, 编码={material.FactoryMaterialCode}");
                    return materialId;
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"新增工厂物料失败: 编码={material.FactoryMaterialCode}", ex);
                throw;
            }
        }

        public int AddProduct(Product product, IReadOnlyList<ProductPart>? parts = null)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        var cmd = new SQLiteCommand(@"
                        INSERT INTO Products (business_type, product_code, product_name, project_name, house_type, area, cost_total_price, selling_total_price, floor_plan, is_active, created_at, updated_at)
                        VALUES (@businessType, @code, @productName, @projectName, @houseType, @area, @costTotalPrice, @sellingTotalPrice, @floorPlan, @isActive, @createdAt, @updatedAt);
                        SELECT last_insert_rowid();", conn, tx);
                        cmd.Parameters.AddWithValue("@businessType", ToDbValue(product.BusinessType));
                        cmd.Parameters.AddWithValue("@code", product.ProductCode);
                        cmd.Parameters.AddWithValue("@productName", ToDbValue(product.ProductName));
                        cmd.Parameters.AddWithValue("@projectName", ToDbValue(product.ProjectCode));
                        cmd.Parameters.AddWithValue("@houseType", ToDbValue(product.HouseType));
                        cmd.Parameters.AddWithValue("@area", product.Area);
                        cmd.Parameters.AddWithValue("@costTotalPrice", product.CostTotalPrice);
                        cmd.Parameters.AddWithValue("@sellingTotalPrice", product.SellingTotalPrice.HasValue ? (object)product.SellingTotalPrice.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@floorPlan", ToDbValue(product.FloorPlan));
                        cmd.Parameters.AddWithValue("@isActive", product.IsActive ? 1 : 0);
                        cmd.Parameters.AddWithValue("@createdAt", product.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@updatedAt", product.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                        int productId = Convert.ToInt32(cmd.ExecuteScalar());
                        LogService.Info($"新增产品成功: ID={productId}, 编码={product.ProductCode}");

                        if (parts != null && parts.Count > 0)
                        {
                            InsertProductPartsInternal(conn, tx, productId, parts);
                        }

                        tx.Commit();
                        return productId;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"新增产品失败: 编码={product.ProductCode}", ex);
                throw;
            }
        }

        public string? GetMyMaterialCodeByFactoryMaterialCode(string factoryMaterialCode, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(factoryMaterialCode))
            {
                return null;
            }

            using var conn = GetConnection();
            conn.Open();
            string sql = @"
                SELECT my_product_code
                FROM FactoryProducts
                WHERE factory_product_code = @factoryCode
                  AND my_product_code IS NOT NULL
                  AND TRIM(my_product_code) <> ''";

            if (excludeId.HasValue)
            {
                sql += " AND id <> @excludeId";
            }

            sql += " ORDER BY id DESC LIMIT 1";

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@factoryCode", factoryMaterialCode.Trim());
            if (excludeId.HasValue)
            {
                cmd.Parameters.AddWithValue("@excludeId", excludeId.Value);
            }

            var result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value ? null : result.ToString();
        }

        public int GetNextMyMaterialCodeSequence(string codePrefix, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(codePrefix))
            {
                return 1;
            }

            int maxSequence = 0;
            using var conn = GetConnection();
            conn.Open();
            string sql = @"
                SELECT my_product_code
                FROM FactoryProducts
                WHERE my_product_code LIKE @prefixPattern";

            if (excludeId.HasValue)
            {
                sql += " AND id <> @excludeId";
            }

            using var cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@prefixPattern", codePrefix.Trim() + "-%");
            if (excludeId.HasValue)
            {
                cmd.Parameters.AddWithValue("@excludeId", excludeId.Value);
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                if (reader.IsDBNull(0))
                {
                    continue;
                }

                string existingCode = reader.GetString(0);
                if (string.IsNullOrWhiteSpace(existingCode) || !existingCode.StartsWith(codePrefix + "-", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string[] parts = existingCode.Split('-');
                if (parts.Length == 4 && int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out int sequence))
                {
                    maxSequence = Math.Max(maxSequence, sequence);
                }
            }

            return maxSequence + 1;
        }

        public void UpdateFactory(Factory factory)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SQLiteCommand(@"
                    UPDATE Factories SET 
                        factory_code = @code, 
                        factory_name = @name,
                        brand = @brand,
                        factory_type = @type, 
                        address = @address, 
                        certifications = @cert, 
                        description = @desc, 
                        scale = @scale, 
                        employee_count = @empCount, 
                        production_capacity = @capacity,
                        controlling_person = @controller, 
                        contact_person = @contact, 
                        contact_info = @contactInfo,
                        updated_at = @updatedAt
                    WHERE id = @id", conn);
                    cmd.Parameters.AddWithValue("@code", factory.FactoryCode);
                    cmd.Parameters.AddWithValue("@name", factory.FactoryName);
                    cmd.Parameters.AddWithValue("@brand", ToDbValue(factory.Brand));
                    cmd.Parameters.AddWithValue("@type", ToDbValue(factory.FactoryType));
                    cmd.Parameters.AddWithValue("@address", ToDbValue(factory.Address));
                    cmd.Parameters.AddWithValue("@cert", ToDbValue(factory.Certifications));
                    cmd.Parameters.AddWithValue("@desc", ToDbValue(factory.Description));
                    cmd.Parameters.AddWithValue("@scale", ToDbValue(factory.Scale));
                    cmd.Parameters.AddWithValue("@empCount", ToDbValue(factory.EmployeeCount));
                    cmd.Parameters.AddWithValue("@capacity", ToDbValue(factory.ProductionCapacity));
                    cmd.Parameters.AddWithValue("@controller", ToDbValue(factory.ControllingPerson));
                    cmd.Parameters.AddWithValue("@contact", ToDbValue(factory.ContactPerson));
                    cmd.Parameters.AddWithValue("@contactInfo", ToDbValue(factory.ContactInfo));
                    cmd.Parameters.AddWithValue("@updatedAt", factory.UpdatedAt);
                    cmd.Parameters.AddWithValue("@id", factory.Id);
                    cmd.ExecuteNonQuery();
                }

                LogService.Info($"更新工厂成功: ID={factory.Id}, 编码={factory.FactoryCode}");
            }
            catch (Exception ex)
            {
                LogService.Error($"更新工厂失败: ID={factory.Id}, 编码={factory.FactoryCode}", ex);
                throw;
            }
        }

        public void DeleteFactory(int id)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SQLiteCommand("DELETE FROM Factories WHERE id = @id", conn);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }

                LogService.Info($"删除工厂成功: ID={id}");
            }
            catch (Exception ex)
            {
                LogService.Error($"删除工厂失败: ID={id}", ex);
                throw;
            }
        }

        public void UpdateFactoryMaterial(FactoryMaterial material)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SQLiteCommand(@"
                    UPDATE FactoryProducts SET 
                        factory_product_code = @factoryCode, 
                        my_product_code = @myCode, 
                        product_name = @name, 
                        brand = @brand, 
                        specification = @spec, 
                        texture = @texture, 
                        process = @process, 
                        unit = @unit,
                        cost_price = @costPrice,
                        usage_scenario = @scenario, 
                        certifications = @cert, 
                        category = @category, 
                        image_url = @image, 
                        factory_id = @factoryId,
                        updated_at = @updatedAt
                    WHERE id = @id", conn);
                    cmd.Parameters.AddWithValue("@factoryCode", material.FactoryMaterialCode);
                    cmd.Parameters.AddWithValue("@myCode", ToDbValue(material.MyMaterialCode));
                    cmd.Parameters.AddWithValue("@name", material.MaterialName);
                    cmd.Parameters.AddWithValue("@brand", ToDbValue(material.Brand));
                    cmd.Parameters.AddWithValue("@spec", ToDbValue(material.Specification));
                    cmd.Parameters.AddWithValue("@texture", ToDbValue(material.Texture));
                    cmd.Parameters.AddWithValue("@process", ToDbValue(material.Process));
                    cmd.Parameters.AddWithValue("@unit", ToDbValue(material.Unit));
                    cmd.Parameters.AddWithValue("@costPrice", material.CostPrice.HasValue ? (object)material.CostPrice.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@scenario", ToDbValue(material.UsageScenario));
                    cmd.Parameters.AddWithValue("@cert", ToDbValue(material.Certifications));
                    cmd.Parameters.AddWithValue("@category", ToDbValue(material.Category));
                    cmd.Parameters.AddWithValue("@image", ToDbValue(material.ImageUrl));
                    cmd.Parameters.AddWithValue("@factoryId", ToDbValue(material.FactoryId));
                    cmd.Parameters.AddWithValue("@updatedAt", material.UpdatedAt);
                    cmd.Parameters.AddWithValue("@id", material.Id);
                    cmd.ExecuteNonQuery();
                }

                LogService.Info($"更新工厂物料成功: ID={material.Id}, 编码={material.FactoryMaterialCode}");
            }
            catch (Exception ex)
            {
                LogService.Error($"更新工厂物料失败: ID={material.Id}, 编码={material.FactoryMaterialCode}", ex);
                throw;
            }
        }

        public void UpdateProduct(Product product, IReadOnlyList<ProductPart>? parts = null)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        var cmd = new SQLiteCommand(@"
                        UPDATE Products SET
                            business_type = @businessType,
                            product_code = @code,
                            product_name = @productName,
                            project_name = @projectName,
                            house_type = @houseType,
                            area = @area,
                            cost_total_price = @costTotalPrice,
                            selling_total_price = @sellingTotalPrice,
                            floor_plan = @floorPlan,
                            is_active = @isActive,
                            updated_at = @updatedAt
                        WHERE id = @id", conn, tx);
                        cmd.Parameters.AddWithValue("@businessType", ToDbValue(product.BusinessType));
                        cmd.Parameters.AddWithValue("@code", product.ProductCode);
                        cmd.Parameters.AddWithValue("@productName", ToDbValue(product.ProductName));
                        cmd.Parameters.AddWithValue("@projectName", ToDbValue(product.ProjectCode));
                        cmd.Parameters.AddWithValue("@houseType", ToDbValue(product.HouseType));
                        cmd.Parameters.AddWithValue("@area", product.Area);
                        cmd.Parameters.AddWithValue("@costTotalPrice", product.CostTotalPrice);
                        cmd.Parameters.AddWithValue("@sellingTotalPrice", product.SellingTotalPrice.HasValue ? (object)product.SellingTotalPrice.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@floorPlan", ToDbValue(product.FloorPlan));
                        cmd.Parameters.AddWithValue("@isActive", product.IsActive ? 1 : 0);
                        cmd.Parameters.AddWithValue("@updatedAt", product.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@id", product.Id);
                        cmd.ExecuteNonQuery();

                        if (parts != null)
                        {
                            var del = new SQLiteCommand("DELETE FROM ProductParts WHERE product_id = @id", conn, tx);
                            del.Parameters.AddWithValue("@id", product.Id);
                            del.ExecuteNonQuery();

                            if (parts.Count > 0)
                            {
                                InsertProductPartsInternal(conn, tx, product.Id, parts);
                            }
                        }

                        tx.Commit();
                    }
                }

                LogService.Info($"更新产品成功: ID={product.Id}, 编码={product.ProductCode}");
            }
            catch (Exception ex)
            {
                LogService.Error($"更新产品失败: ID={product.Id}, 编码={product.ProductCode}", ex);
                throw;
            }
        }

        private void InsertProductPartsInternal(SQLiteConnection conn, SQLiteTransaction tx, int productId, IReadOnlyList<ProductPart> parts)
        {
            var insertCmd = new SQLiteCommand(@"
                INSERT INTO ProductParts (product_id, part_name, part_code, part_type, material, specification,
                    quantity, unit, unit_price, total_price, remarks, is_active, created_at, updated_at)
                VALUES (@productId, @partName, @partCode, @partType, @material, @specification,
                    @quantity, @unit, @unitPrice, @totalPrice, @remarks, @isActive, @createdAt, @updatedAt);
                SELECT last_insert_rowid();", conn, tx);

            insertCmd.Parameters.Add("@productId", System.Data.DbType.Int32);
            insertCmd.Parameters.Add("@partName", System.Data.DbType.String);
            insertCmd.Parameters.Add("@partCode", System.Data.DbType.String);
            insertCmd.Parameters.Add("@partType", System.Data.DbType.String);
            insertCmd.Parameters.Add("@material", System.Data.DbType.String);
            insertCmd.Parameters.Add("@specification", System.Data.DbType.String);
            insertCmd.Parameters.Add("@quantity", System.Data.DbType.Decimal);
            insertCmd.Parameters.Add("@unit", System.Data.DbType.String);
            insertCmd.Parameters.Add("@unitPrice", System.Data.DbType.Decimal);
            insertCmd.Parameters.Add("@totalPrice", System.Data.DbType.Decimal);
            insertCmd.Parameters.Add("@remarks", System.Data.DbType.String);
            insertCmd.Parameters.Add("@isActive", System.Data.DbType.Int32);
            insertCmd.Parameters.Add("@createdAt", System.Data.DbType.String);
            insertCmd.Parameters.Add("@updatedAt", System.Data.DbType.String);

            foreach (var p in parts)
            {
                insertCmd.Parameters["@productId"].Value = productId;
                insertCmd.Parameters["@partName"].Value = p.PartName ?? string.Empty;
                insertCmd.Parameters["@partCode"].Value = ToDbValue(p.PartCode);
                insertCmd.Parameters["@partType"].Value = ToDbValue(p.PartType);
                insertCmd.Parameters["@material"].Value = ToDbValue(p.Material);
                insertCmd.Parameters["@specification"].Value = ToDbValue(p.Specification);
                insertCmd.Parameters["@quantity"].Value = p.Quantity;
                insertCmd.Parameters["@unit"].Value = ToDbValue(p.Unit);
                insertCmd.Parameters["@unitPrice"].Value = p.UnitPrice;
                insertCmd.Parameters["@totalPrice"].Value = p.TotalPrice;
                insertCmd.Parameters["@remarks"].Value = ToDbValue(p.Remarks);
                insertCmd.Parameters["@isActive"].Value = p.IsActive ? 1 : 0;
                insertCmd.Parameters["@createdAt"].Value = p.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                insertCmd.Parameters["@updatedAt"].Value = p.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                int newId = Convert.ToInt32(insertCmd.ExecuteScalar());
                LogService.Info($"新增产品部位成功: ID={newId}, 名称={p.PartName}");
            }
        }

        public void DeleteProduct(int id)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SQLiteCommand("DELETE FROM Products WHERE id = @id", conn);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }

                LogService.Info($"删除产品成功: ID={id}");
            }
            catch (Exception ex)
            {
                LogService.Error($"删除产品失败: ID={id}", ex);
                throw;
            }
        }

        public void DeleteFactoryMaterial(int id)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SQLiteCommand("DELETE FROM FactoryProducts WHERE id = @id", conn);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }

                LogService.Info($"删除工厂物料成功: ID={id}");
            }
            catch (Exception ex)
            {
                LogService.Error($"删除工厂物料失败: ID={id}", ex);
                throw;
            }
        }

        public List<ProductPart> GetProductParts(int productId)
        {
            var parts = new List<ProductPart>();
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SQLiteCommand(@"
                    SELECT id, product_id, part_name, part_code, part_type, material, specification,
                           quantity, unit, unit_price, total_price, remarks, is_active, created_at, updated_at
                    FROM ProductParts
                    WHERE product_id = @productId
                    ORDER BY id", conn);
                    cmd.Parameters.AddWithValue("@productId", productId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            parts.Add(new ProductPart
                            {
                                Id = reader.GetInt32(0),
                                ProductId = reader.GetInt32(1),
                                PartName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                PartCode = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                PartType = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                Material = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                                Specification = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                                Quantity = reader.IsDBNull(7) ? 0 : Convert.ToDecimal(reader.GetValue(7)),
                                Unit = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                                UnitPrice = reader.IsDBNull(9) ? 0 : Convert.ToDecimal(reader.GetValue(9)),
                                TotalPrice = reader.IsDBNull(10) ? 0 : Convert.ToDecimal(reader.GetValue(10)),
                                Remarks = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                                IsActive = !reader.IsDBNull(12) && reader.GetInt32(12) == 1,
                                CreatedAt = reader.IsDBNull(13) ? DateTime.Now : DateTime.Parse(reader.GetString(13)),
                                UpdatedAt = reader.IsDBNull(14) ? DateTime.Now : DateTime.Parse(reader.GetString(14))
                            });
                        }
                    }
                }

                LogService.Info($"查询产品部位列表完成，共 {parts.Count} 条记录");
                return parts;
            }
            catch (Exception ex)
            {
                LogService.Error($"查询产品部位列表失败: productId={productId}", ex);
                throw;
            }
        }

        public int AddProductPart(ProductPart part)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SQLiteCommand(@"
                    INSERT INTO ProductParts (product_id, part_name, part_code, part_type, material, specification,
                        quantity, unit, unit_price, total_price, remarks, is_active, created_at, updated_at)
                    VALUES (@productId, @partName, @partCode, @partType, @material, @specification,
                        @quantity, @unit, @unitPrice, @totalPrice, @remarks, @isActive, @createdAt, @updatedAt);
                    SELECT last_insert_rowid();", conn);
                    cmd.Parameters.AddWithValue("@productId", part.ProductId);
                    cmd.Parameters.AddWithValue("@partName", part.PartName);
                    cmd.Parameters.AddWithValue("@partCode", ToDbValue(part.PartCode));
                    cmd.Parameters.AddWithValue("@partType", ToDbValue(part.PartType));
                    cmd.Parameters.AddWithValue("@material", ToDbValue(part.Material));
                    cmd.Parameters.AddWithValue("@specification", ToDbValue(part.Specification));
                    cmd.Parameters.AddWithValue("@quantity", part.Quantity);
                    cmd.Parameters.AddWithValue("@unit", ToDbValue(part.Unit));
                    cmd.Parameters.AddWithValue("@unitPrice", part.UnitPrice);
                    cmd.Parameters.AddWithValue("@totalPrice", part.TotalPrice);
                    cmd.Parameters.AddWithValue("@remarks", ToDbValue(part.Remarks));
                    cmd.Parameters.AddWithValue("@isActive", part.IsActive ? 1 : 0);
                    cmd.Parameters.AddWithValue("@createdAt", part.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@updatedAt", part.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                    int partId = Convert.ToInt32(cmd.ExecuteScalar());
                    LogService.Info($"新增产品部位成功: ID={partId}, 名称={part.PartName}");
                    return partId;
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"新增产品部位失败: 名称={part.PartName}", ex);
                throw;
            }
        }

        public void UpdateProductPart(ProductPart part)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SQLiteCommand(@"
                    UPDATE ProductParts SET
                        part_name = @partName,
                        part_code = @partCode,
                        part_type = @partType,
                        material = @material,
                        specification = @specification,
                        quantity = @quantity,
                        unit = @unit,
                        unit_price = @unitPrice,
                        total_price = @totalPrice,
                        remarks = @remarks,
                        is_active = @isActive,
                        updated_at = @updatedAt
                    WHERE id = @id", conn);
                    cmd.Parameters.AddWithValue("@partName", part.PartName);
                    cmd.Parameters.AddWithValue("@partCode", ToDbValue(part.PartCode));
                    cmd.Parameters.AddWithValue("@partType", ToDbValue(part.PartType));
                    cmd.Parameters.AddWithValue("@material", ToDbValue(part.Material));
                    cmd.Parameters.AddWithValue("@specification", ToDbValue(part.Specification));
                    cmd.Parameters.AddWithValue("@quantity", part.Quantity);
                    cmd.Parameters.AddWithValue("@unit", ToDbValue(part.Unit));
                    cmd.Parameters.AddWithValue("@unitPrice", part.UnitPrice);
                    cmd.Parameters.AddWithValue("@totalPrice", part.TotalPrice);
                    cmd.Parameters.AddWithValue("@remarks", ToDbValue(part.Remarks));
                    cmd.Parameters.AddWithValue("@isActive", part.IsActive ? 1 : 0);
                    cmd.Parameters.AddWithValue("@updatedAt", part.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@id", part.Id);
                    cmd.ExecuteNonQuery();
                }

                LogService.Info($"更新产品部位成功: ID={part.Id}, 名称={part.PartName}");
            }
            catch (Exception ex)
            {
                LogService.Error($"更新产品部位失败: ID={part.Id}", ex);
                throw;
            }
        }

        public void DeleteProductPart(int id)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SQLiteCommand("DELETE FROM ProductParts WHERE id = @id", conn);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }

                LogService.Info($"删除产品部位成功: ID={id}");
            }
            catch (Exception ex)
            {
                LogService.Error($"删除产品部位失败: ID={id}", ex);
                throw;
            }
        }

        public int AddProductPartMaterials(int productId, IReadOnlyList<ProductPartMaterial> materials)
        {
            if (materials == null || materials.Count == 0) return 0;
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        var cmd = new SQLiteCommand(@"
                            INSERT INTO ProductPartMaterials
                                (product_id, part_id, part_name, component_name, material_type_name,
                                 material_id, material_name, factory_material_code, my_material_code,
                                 brand, specification, unit, unit_price, quantity, total_price,
                                 remarks, is_composite, group_code, item_name, parent_id,
                                 created_at, updated_at)
                            VALUES
                                (@productId, @partId, @partName, @componentName, @materialTypeName,
                                 @materialId, @materialName, @factoryMaterialCode, @myMaterialCode,
                                 @brand, @specification, @unit, @unitPrice, @quantity, @totalPrice,
                                 @remarks, @isComposite, @groupCode, @itemName, @parentId,
                                 @createdAt, @updatedAt);
                            SELECT last_insert_rowid();", conn, tx);

                        cmd.Parameters.Add("@productId", System.Data.DbType.Int32);
                        cmd.Parameters.Add("@partId", System.Data.DbType.Int32);
                        cmd.Parameters.Add("@partName", System.Data.DbType.String);
                        cmd.Parameters.Add("@componentName", System.Data.DbType.String);
                        cmd.Parameters.Add("@materialTypeName", System.Data.DbType.String);
                        cmd.Parameters.Add("@materialId", System.Data.DbType.Int32);
                        cmd.Parameters.Add("@materialName", System.Data.DbType.String);
                        cmd.Parameters.Add("@factoryMaterialCode", System.Data.DbType.String);
                        cmd.Parameters.Add("@myMaterialCode", System.Data.DbType.String);
                        cmd.Parameters.Add("@brand", System.Data.DbType.String);
                        cmd.Parameters.Add("@specification", System.Data.DbType.String);
                        cmd.Parameters.Add("@unit", System.Data.DbType.String);
                        cmd.Parameters.Add("@unitPrice", System.Data.DbType.Decimal);
                        cmd.Parameters.Add("@quantity", System.Data.DbType.Decimal);
                        cmd.Parameters.Add("@totalPrice", System.Data.DbType.Decimal);
                        cmd.Parameters.Add("@remarks", System.Data.DbType.String);
                        cmd.Parameters.Add("@isComposite", System.Data.DbType.Int32);
                        cmd.Parameters.Add("@groupCode", System.Data.DbType.String);
                        cmd.Parameters.Add("@itemName", System.Data.DbType.String);
                        cmd.Parameters.Add("@parentId", System.Data.DbType.Int32);
                        cmd.Parameters.Add("@createdAt", System.Data.DbType.String);
                        cmd.Parameters.Add("@updatedAt", System.Data.DbType.String);

                        int count = 0;
                        foreach (var m in materials)
                        {
                            cmd.Parameters["@productId"].Value = productId;
                            cmd.Parameters["@partId"].Value = m.PartId.HasValue ? (object)m.PartId.Value : DBNull.Value;
                            cmd.Parameters["@partName"].Value = m.PartName ?? string.Empty;
                            cmd.Parameters["@componentName"].Value = m.ComponentName ?? string.Empty;
                            cmd.Parameters["@materialTypeName"].Value = ToDbValue(m.MaterialTypeName);
                            cmd.Parameters["@materialId"].Value = m.MaterialId.HasValue ? (object)m.MaterialId.Value : DBNull.Value;
                            cmd.Parameters["@materialName"].Value = m.MaterialName ?? string.Empty;
                            cmd.Parameters["@factoryMaterialCode"].Value = ToDbValue(m.FactoryMaterialCode);
                            cmd.Parameters["@myMaterialCode"].Value = ToDbValue(m.MyMaterialCode);
                            cmd.Parameters["@brand"].Value = ToDbValue(m.Brand);
                            cmd.Parameters["@specification"].Value = ToDbValue(m.Specification);
                            cmd.Parameters["@unit"].Value = ToDbValue(m.Unit);
                            cmd.Parameters["@unitPrice"].Value = m.UnitPrice;
                            cmd.Parameters["@quantity"].Value = m.Quantity;
                            cmd.Parameters["@totalPrice"].Value = m.TotalPrice;
                            cmd.Parameters["@remarks"].Value = ToDbValue(m.Remarks);
                            cmd.Parameters["@isComposite"].Value = m.IsComposite ? 1 : 0;
                            cmd.Parameters["@groupCode"].Value = ToDbValue(m.GroupCode);
                            cmd.Parameters["@itemName"].Value = ToDbValue(m.ItemName);
                            cmd.Parameters["@parentId"].Value = m.ParentId.HasValue ? (object)m.ParentId.Value : DBNull.Value;
                            cmd.Parameters["@createdAt"].Value = m.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                            cmd.Parameters["@updatedAt"].Value = m.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                            Convert.ToInt32(cmd.ExecuteScalar());
                            count++;
                        }

                        tx.Commit();
                        LogService.Info($"新增产品部位物料成功: productId={productId}, 共 {count} 条");
                        return count;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"新增产品部位物料失败: productId={productId}", ex);
                throw;
            }
        }

        public List<ProductPartMaterial> GetProductPartMaterials(int productId, int? partId = null)
        {
            var list = new List<ProductPartMaterial>();
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SQLiteCommand(@"
                        SELECT id, product_id, part_id, part_name, component_name, material_type_name,
                               material_id, material_name, factory_material_code, my_material_code,
                               brand, specification, unit, unit_price, quantity, total_price,
                               remarks, is_composite, group_code, item_name, parent_id,
                               created_at, updated_at
                        FROM ProductPartMaterials
                        WHERE product_id = @productId" + (partId.HasValue ? " AND part_id = @partId" : "") + @"
                        ORDER BY part_name, component_name, id", conn);
                    cmd.Parameters.AddWithValue("@productId", productId);
                    if (partId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@partId", partId.Value);
                    }
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        list.Add(new ProductPartMaterial
                        {
                            Id = reader.GetInt32(0),
                            ProductId = reader.GetInt32(1),
                            PartId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                            PartName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            ComponentName = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            MaterialTypeName = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            MaterialId = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                            MaterialName = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                            FactoryMaterialCode = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                            MyMaterialCode = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                            Brand = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                            Specification = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                            Unit = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                            UnitPrice = reader.IsDBNull(13) ? 0 : Convert.ToDecimal(reader.GetValue(13)),
                            Quantity = reader.IsDBNull(14) ? 0 : Convert.ToDecimal(reader.GetValue(14)),
                            Remarks = reader.IsDBNull(15) ? string.Empty : reader.GetString(15),
                            IsComposite = !reader.IsDBNull(16) && reader.GetInt32(16) == 1,
                            GroupCode = reader.IsDBNull(17) ? string.Empty : reader.GetString(17),
                            ItemName = reader.IsDBNull(18) ? string.Empty : reader.GetString(18),
                            ParentId = reader.IsDBNull(19) ? null : reader.GetInt32(19),
                            CreatedAt = reader.IsDBNull(20) ? DateTime.Now : DateTime.Parse(reader.GetString(20)),
                            UpdatedAt = reader.IsDBNull(21) ? DateTime.Now : DateTime.Parse(reader.GetString(21))
                        });
                    }
                }
                LogService.Info($"查询产品部位物料完成: productId={productId}, partId={partId}, 共 {list.Count} 条");
                return list;
            }
            catch (Exception ex)
            {
                LogService.Error($"查询产品部位物料失败: productId={productId}", ex);
                throw;
            }
        }

        public int DeleteProductPartMaterialsByProduct(int productId)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SQLiteCommand("DELETE FROM ProductPartMaterials WHERE product_id = @productId", conn);
                    cmd.Parameters.AddWithValue("@productId", productId);
                    int n = cmd.ExecuteNonQuery();
                    LogService.Info($"清空产品部位物料: productId={productId}, 删除 {n} 条");
                    return n;
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"清空产品部位物料失败: productId={productId}", ex);
                throw;
            }
        }

        // ===== CustomPart 自定义部位 =====

        private void EnsureCustomPartsSchema(SQLiteConnection conn)
        {
            string createTable = @"
                CREATE TABLE IF NOT EXISTS CustomParts (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    part_name TEXT NOT NULL UNIQUE,
                    components TEXT,
                    created_at TEXT,
                    updated_at TEXT
                )";

            using (var cmd = new SQLiteCommand(createTable, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public List<CustomPart> GetCustomParts()
        {
            var list = new List<CustomPart>();
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SQLiteCommand("SELECT id, part_name, components, created_at, updated_at FROM CustomParts ORDER BY id", conn);
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        list.Add(new CustomPart
                        {
                            Id = reader.GetInt32(0),
                            PartName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            Components = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            CreatedAt = reader.IsDBNull(3) ? DateTime.Now : DateTime.Parse(reader.GetString(3)),
                            UpdatedAt = reader.IsDBNull(4) ? DateTime.Now : DateTime.Parse(reader.GetString(4))
                        });
                    }
                }
                LogService.Info($"查询自定义部位列表完成，共 {list.Count} 条记录");
                return list;
            }
            catch (Exception ex)
            {
                LogService.Error("查询自定义部位列表失败", ex);
                throw;
            }
        }

        public int AddCustomPart(CustomPart part)
        {
            if (part == null || string.IsNullOrWhiteSpace(part.PartName))
            {
                throw new ArgumentException("部位名称不能为空");
            }

            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    var checkCmd = new SQLiteCommand("SELECT id FROM CustomParts WHERE part_name = @name", conn);
                    checkCmd.Parameters.AddWithValue("@name", part.PartName.Trim());
                    var existing = checkCmd.ExecuteScalar();
                    if (existing != null && existing != DBNull.Value)
                    {
                        throw new InvalidOperationException($"自定义部位\"{part.PartName}\"已存在");
                    }

                    var cmd = new SQLiteCommand(@"
                        INSERT INTO CustomParts (part_name, components, created_at, updated_at)
                        VALUES (@name, @components, @createdAt, @updatedAt);
                        SELECT last_insert_rowid();", conn);
                    cmd.Parameters.AddWithValue("@name", part.PartName.Trim());
                    cmd.Parameters.AddWithValue("@components", ToDbValue(part.Components));
                    cmd.Parameters.AddWithValue("@createdAt", part.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@updatedAt", part.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                    int newId = Convert.ToInt32(cmd.ExecuteScalar());
                    part.Id = newId;
                    LogService.Info($"新增自定义部位成功: ID={newId}, 名称={part.PartName}");
                    return newId;
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"新增自定义部位失败: 名称={part.PartName}", ex);
                throw;
            }
        }

        public void UpdateCustomPart(CustomPart part)
        {
            if (part == null || part.Id <= 0)
            {
                throw new ArgumentException("自定义部位 ID 无效");
            }

            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SQLiteCommand(@"
                        UPDATE CustomParts SET
                            part_name = @name,
                            components = @components,
                            updated_at = @updatedAt
                        WHERE id = @id", conn);
                    cmd.Parameters.AddWithValue("@name", part.PartName.Trim());
                    cmd.Parameters.AddWithValue("@components", ToDbValue(part.Components));
                    cmd.Parameters.AddWithValue("@updatedAt", part.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@id", part.Id);
                    cmd.ExecuteNonQuery();
                }
                LogService.Info($"更新自定义部位成功: ID={part.Id}, 名称={part.PartName}");
            }
            catch (Exception ex)
            {
                LogService.Error($"更新自定义部位失败: ID={part.Id}", ex);
                throw;
            }
        }

        public void DeleteCustomPart(int id)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SQLiteCommand("DELETE FROM CustomParts WHERE id = @id", conn);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                LogService.Info($"删除自定义部位成功: ID={id}");
            }
            catch (Exception ex)
            {
                LogService.Error($"删除自定义部位失败: ID={id}", ex);
                throw;
            }
        }

        public bool CustomPartExists(string partName, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(partName)) return false;
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT COUNT(*) FROM CustomParts WHERE part_name = @name";
                    if (excludeId.HasValue)
                    {
                        sql += " AND id <> @excludeId";
                    }
                    var cmd = new SQLiteCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@name", partName.Trim());
                    if (excludeId.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@excludeId", excludeId.Value);
                    }
                    long count = (long)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                LogService.Error($"检查自定义部位是否存在失败: name={partName}", ex);
                return false;
            }
        }
    }
}
