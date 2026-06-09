using FactoryProductManager.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace FactoryProductManager.Services
{
    public class DbService
    {
        private readonly string _connectionString;

        public DbService(string databasePath = null)
        {
            if (string.IsNullOrEmpty(databasePath))
            {
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string projectDirectory = Path.GetFullPath(Path.Combine(appDirectory, "..", "..", "..", ".."));
                databasePath = Path.Combine(projectDirectory, "FactoryProductManager", "FactoryProductDB.db");
            }
            _connectionString = $"Data Source={databasePath};Version=3;";
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
                
                string createProductsTable = @"
                    CREATE TABLE IF NOT EXISTS FactoryProducts (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        factory_product_code TEXT NOT NULL,
                        my_product_code TEXT,
                        product_name TEXT NOT NULL,
                        brand TEXT,
                        specification TEXT,
                        texture TEXT,
                        process TEXT,
                        usage_scenario TEXT,
                        certifications TEXT,
                        category TEXT,
                        image_url TEXT,
                        factory_id INTEGER,
                        created_at TEXT,
                        updated_at TEXT,
                        FOREIGN KEY (factory_id) REFERENCES Factories(id)
                    )";
                
                using (var cmd = new SQLiteCommand(createProductsTable, conn))
                {
                    cmd.ExecuteNonQuery();
                }
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

        public List<Factory> GetFactories()
        {
            LogService.Info("开始查询工厂列表");
            var factories = new List<Factory>();
            try
            {
                using (var conn = GetConnection())
                {
                    LogService.Info($"数据库连接字符串: {_connectionString}");
                    conn.Open();
                    LogService.Info("数据库连接成功");
                    
                    var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Factories", conn);
                    var count = Convert.ToInt32(cmd.ExecuteScalar());
                    LogService.Info($"Factories表记录数: {count}");
                    
                    cmd = new SQLiteCommand("SELECT * FROM Factories ORDER BY factory_code", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        LogService.Info("开始读取数据...");
                        while (reader.Read())
                        {
                            try
                            {
                                factories.Add(new Factory
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                                    FactoryCode = reader.GetString(reader.GetOrdinal("factory_code")),
                                    FactoryName = reader.GetString(reader.GetOrdinal("factory_name")),
                                    FactoryType = reader.IsDBNull(reader.GetOrdinal("factory_type")) ? null : reader.GetString(reader.GetOrdinal("factory_type")),
                                    Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                                    Certifications = reader.IsDBNull(reader.GetOrdinal("certifications")) ? null : reader.GetString(reader.GetOrdinal("certifications")),
                                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                                    Scale = reader.IsDBNull(reader.GetOrdinal("scale")) ? null : reader.GetString(reader.GetOrdinal("scale")),
                                    EmployeeCount = reader.IsDBNull(reader.GetOrdinal("employee_count")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("employee_count")),
                                    ProductionCapacity = reader.IsDBNull(reader.GetOrdinal("production_capacity")) ? null : reader.GetString(reader.GetOrdinal("production_capacity")),
                                    ControllingPerson = reader.IsDBNull(reader.GetOrdinal("controlling_person")) ? null : reader.GetString(reader.GetOrdinal("controlling_person")),
                                    ContactPerson = reader.IsDBNull(reader.GetOrdinal("contact_person")) ? null : reader.GetString(reader.GetOrdinal("contact_person")),
                                    ContactInfo = reader.IsDBNull(reader.GetOrdinal("contact_info")) ? null : reader.GetString(reader.GetOrdinal("contact_info")),
                                    CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at")) ? DateTime.Now : DateTime.Parse(reader.GetString(reader.GetOrdinal("created_at"))),
                                    UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? DateTime.Now : DateTime.Parse(reader.GetString(reader.GetOrdinal("updated_at")))
                                });
                            }
                            catch (Exception ex)
                            {
                                LogService.Error($"读取单条记录失败: {ex.Message}");
                            }
                        }
                        LogService.Info($"数据读取完成，共读取 {factories.Count} 条记录");
                    }
                }
                LogService.Info($"查询工厂列表完成，共 {factories.Count} 条记录");
            }
            catch (Exception ex)
            {
                LogService.Error($"查询工厂列表失败: {ex.Message}");
                LogService.Error($"异常堆栈: {ex.StackTrace}");
                throw;
            }
            return factories;
        }

        public List<FactoryProduct> GetFactoryProducts()
        {
            var products = new List<FactoryProduct>();
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
                        products.Add(new FactoryProduct
                        {
                            Id = reader.GetInt32(0),
                            FactoryProductCode = reader.GetString(1),
                            MyProductCode = reader.IsDBNull(2) ? null : reader.GetString(2),
                            ProductName = reader.GetString(3),
                            Brand = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Specification = reader.IsDBNull(5) ? null : reader.GetString(5),
                            Texture = reader.IsDBNull(6) ? null : reader.GetString(6),
                            Process = reader.IsDBNull(7) ? null : reader.GetString(7),
                            UsageScenario = reader.IsDBNull(8) ? null : reader.GetString(8),
                            Certifications = reader.IsDBNull(9) ? null : reader.GetString(9),
                            Category = reader.IsDBNull(10) ? null : reader.GetString(10),
                            ImageUrl = reader.IsDBNull(11) ? null : reader.GetString(11),
                            FactoryId = reader.IsDBNull(12) ? null : reader.GetInt32(12),
                            FactoryName = reader.IsDBNull(13) ? null : reader.GetString(13),
                            CreatedAt = reader.IsDBNull(14) ? DateTime.Now : DateTime.Parse(reader.GetString(14)),
                            UpdatedAt = reader.IsDBNull(15) ? DateTime.Now : DateTime.Parse(reader.GetString(15))
                        });
                    }
                }
            }
            return products;
        }

        public int AddFactory(Factory factory)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SQLiteCommand(@"
                    INSERT INTO Factories (factory_code, factory_name, factory_type, address, 
                        certifications, description, scale, employee_count, production_capacity,
                        controlling_person, contact_person, contact_info)
                    VALUES (@code, @name, @type, @address, @cert, @desc, @scale, @empCount, 
                        @capacity, @controller, @contact, @contactInfo);
                    SELECT last_insert_rowid();", conn);
                cmd.Parameters.AddWithValue("@code", factory.FactoryCode);
                cmd.Parameters.AddWithValue("@name", factory.FactoryName);
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
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public int AddFactoryProduct(FactoryProduct product)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SQLiteCommand(@"
                    INSERT INTO FactoryProducts (factory_product_code, my_product_code, product_name, 
                        brand, specification, texture, process, usage_scenario, certifications, 
                        category, image_url, factory_id)
                    VALUES (@factoryCode, @myCode, @name, @brand, @spec, @texture, @process, 
                        @scenario, @cert, @category, @image, @factoryId);
                    SELECT last_insert_rowid();", conn);
                cmd.Parameters.AddWithValue("@factoryCode", product.FactoryProductCode);
                cmd.Parameters.AddWithValue("@myCode", ToDbValue(product.MyProductCode));
                cmd.Parameters.AddWithValue("@name", product.ProductName);
                cmd.Parameters.AddWithValue("@brand", ToDbValue(product.Brand));
                cmd.Parameters.AddWithValue("@spec", ToDbValue(product.Specification));
                cmd.Parameters.AddWithValue("@texture", ToDbValue(product.Texture));
                cmd.Parameters.AddWithValue("@process", ToDbValue(product.Process));
                cmd.Parameters.AddWithValue("@scenario", ToDbValue(product.UsageScenario));
                cmd.Parameters.AddWithValue("@cert", ToDbValue(product.Certifications));
                cmd.Parameters.AddWithValue("@category", ToDbValue(product.Category));
                cmd.Parameters.AddWithValue("@image", ToDbValue(product.ImageUrl));
                cmd.Parameters.AddWithValue("@factoryId", ToDbValue(product.FactoryId));
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void UpdateFactory(Factory factory)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SQLiteCommand(@"
                    UPDATE Factories SET 
                        factory_code = @code, 
                        factory_name = @name, 
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
        }

        public void DeleteFactory(int id)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SQLiteCommand("DELETE FROM Factories WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateFactoryProduct(FactoryProduct product)
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
                        usage_scenario = @scenario, 
                        certifications = @cert, 
                        category = @category, 
                        image_url = @image, 
                        factory_id = @factoryId,
                        updated_at = @updatedAt
                    WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@factoryCode", product.FactoryProductCode);
                cmd.Parameters.AddWithValue("@myCode", ToDbValue(product.MyProductCode));
                cmd.Parameters.AddWithValue("@name", product.ProductName);
                cmd.Parameters.AddWithValue("@brand", ToDbValue(product.Brand));
                cmd.Parameters.AddWithValue("@spec", ToDbValue(product.Specification));
                cmd.Parameters.AddWithValue("@texture", ToDbValue(product.Texture));
                cmd.Parameters.AddWithValue("@process", ToDbValue(product.Process));
                cmd.Parameters.AddWithValue("@scenario", ToDbValue(product.UsageScenario));
                cmd.Parameters.AddWithValue("@cert", ToDbValue(product.Certifications));
                cmd.Parameters.AddWithValue("@category", ToDbValue(product.Category));
                cmd.Parameters.AddWithValue("@image", ToDbValue(product.ImageUrl));
                cmd.Parameters.AddWithValue("@factoryId", ToDbValue(product.FactoryId));
                cmd.Parameters.AddWithValue("@updatedAt", product.UpdatedAt);
                cmd.Parameters.AddWithValue("@id", product.Id);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteFactoryProduct(int id)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                var cmd = new SQLiteCommand("DELETE FROM FactoryProducts WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }
    }
}