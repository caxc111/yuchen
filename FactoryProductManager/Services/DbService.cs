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
                                ProjectName = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
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

        public int AddProduct(Product product)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SQLiteCommand(@"
                    INSERT INTO Products (business_type, product_code, product_name, project_name, house_type, area, cost_total_price, selling_total_price, floor_plan, is_active, created_at, updated_at)
                    VALUES (@businessType, @code, @productName, @projectName, @houseType, @area, @costTotalPrice, @sellingTotalPrice, @floorPlan, @isActive, @createdAt, @updatedAt);
                    SELECT last_insert_rowid();", conn);
                    cmd.Parameters.AddWithValue("@businessType", ToDbValue(product.BusinessType));
                    cmd.Parameters.AddWithValue("@code", product.ProductCode);
                    cmd.Parameters.AddWithValue("@productName", ToDbValue(product.ProductName));
                    cmd.Parameters.AddWithValue("@projectName", ToDbValue(product.ProjectName));
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
                    return productId;
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

        public void UpdateProduct(Product product)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
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
                    WHERE id = @id", conn);
                    cmd.Parameters.AddWithValue("@businessType", ToDbValue(product.BusinessType));
                    cmd.Parameters.AddWithValue("@code", product.ProductCode);
                    cmd.Parameters.AddWithValue("@productName", ToDbValue(product.ProductName));
                    cmd.Parameters.AddWithValue("@projectName", ToDbValue(product.ProjectName));
                    cmd.Parameters.AddWithValue("@houseType", ToDbValue(product.HouseType));
                    cmd.Parameters.AddWithValue("@area", product.Area);
                    cmd.Parameters.AddWithValue("@costTotalPrice", product.CostTotalPrice);
                    cmd.Parameters.AddWithValue("@sellingTotalPrice", product.SellingTotalPrice.HasValue ? (object)product.SellingTotalPrice.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@floorPlan", ToDbValue(product.FloorPlan));
                    cmd.Parameters.AddWithValue("@isActive", product.IsActive ? 1 : 0);
                    cmd.Parameters.AddWithValue("@updatedAt", product.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@id", product.Id);
                    cmd.ExecuteNonQuery();
                }

                LogService.Info($"更新产品成功: ID={product.Id}, 编码={product.ProductCode}");
            }
            catch (Exception ex)
            {
                LogService.Error($"更新产品失败: ID={product.Id}, 编码={product.ProductCode}", ex);
                throw;
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
    }
}
