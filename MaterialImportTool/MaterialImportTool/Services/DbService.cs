using MaterialImportTool.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace MaterialImportTool.Services
{
    public class DbService
    {
        private readonly string _dbPath;

        public DbService(string dbPath = null)
        {
            _dbPath = dbPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FactoryProductDB.db");
            LogService.Info($"数据库路径: {_dbPath}", "DbService");
            try
            {
                EnsureDatabaseExists();
                LogService.Info("数据库初始化成功", "DbService");
            }
            catch (Exception ex)
            {
                LogService.Error(ex, "DbService");
                throw;
            }
        }

        private void EnsureDatabaseExists()
        {
            if (!File.Exists(_dbPath))
            {
                SQLiteConnection.CreateFile(_dbPath);
                InitializeDatabase();
            }
        }

        private void InitializeDatabase()
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                var cmd = new SQLiteCommand(conn);
                
                cmd.CommandText = @"
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
                    );
                    
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
                        sub_category TEXT,
                        image_url TEXT,
                        factory_id INTEGER,
                        factory_code TEXT,
                        created_at TEXT,
                        updated_at TEXT,
                        FOREIGN KEY (factory_id) REFERENCES Factories(id)
                    );
                    
                    CREATE INDEX IF NOT EXISTS idx_factories_code ON Factories(factory_code);
                    CREATE INDEX IF NOT EXISTS idx_products_factory ON FactoryProducts(factory_id);
                    CREATE INDEX IF NOT EXISTS idx_products_code ON FactoryProducts(factory_product_code);
                ";
                cmd.ExecuteNonQuery();
            }
        }

        private object ToDbValue(object value)
        {
            return value ?? DBNull.Value;
        }

        public List<Factory> GetFactories()
        {
            var factories = new List<Factory>();
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                var cmd = new SQLiteCommand("SELECT * FROM Factories ORDER BY factory_code", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        factories.Add(new Factory
                        {
                            Id = reader.GetInt32(0),
                            FactoryCode = reader.GetString(1),
                            FactoryName = reader.GetString(2),
                            FactoryType = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Address = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Certifications = reader.IsDBNull(5) ? null : reader.GetString(5),
                            Description = reader.IsDBNull(6) ? null : reader.GetString(6),
                            Scale = reader.IsDBNull(7) ? null : reader.GetString(7),
                            EmployeeCount = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                            ProductionCapacity = reader.IsDBNull(9) ? null : reader.GetString(9),
                            ControllingPerson = reader.IsDBNull(10) ? null : reader.GetString(10),
                            ContactPerson = reader.IsDBNull(11) ? null : reader.GetString(11),
                            ContactInfo = reader.IsDBNull(12) ? null : reader.GetString(12),
                            ContactMethod = reader.IsDBNull(13) ? null : reader.GetString(13),
                            CreatedAt = reader.IsDBNull(14) ? null : DateTime.Parse(reader.GetString(14)),
                            UpdatedAt = reader.IsDBNull(15) ? null : DateTime.Parse(reader.GetString(15))
                        });
                    }
                }
            }
            return factories;
        }

        public Factory GetFactoryById(int id)
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                var cmd = new SQLiteCommand("SELECT * FROM Factories WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Factory
                        {
                            Id = reader.GetInt32(0),
                            FactoryCode = reader.GetString(1),
                            FactoryName = reader.GetString(2),
                            FactoryType = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Address = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Certifications = reader.IsDBNull(5) ? null : reader.GetString(5),
                            Description = reader.IsDBNull(6) ? null : reader.GetString(6),
                            Scale = reader.IsDBNull(7) ? null : reader.GetString(7),
                            EmployeeCount = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                            ProductionCapacity = reader.IsDBNull(9) ? null : reader.GetString(9),
                            ControllingPerson = reader.IsDBNull(10) ? null : reader.GetString(10),
                            ContactPerson = reader.IsDBNull(11) ? null : reader.GetString(11),
                            ContactInfo = reader.IsDBNull(12) ? null : reader.GetString(12),
                            ContactMethod = reader.IsDBNull(13) ? null : reader.GetString(13),
                            CreatedAt = reader.IsDBNull(14) ? null : DateTime.Parse(reader.GetString(14)),
                            UpdatedAt = reader.IsDBNull(15) ? null : DateTime.Parse(reader.GetString(15))
                        };
                    }
                }
            }
            return null;
        }

        public Factory GetFactoryByCode(string code)
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                var cmd = new SQLiteCommand("SELECT * FROM Factories WHERE factory_code = @code", conn);
                cmd.Parameters.AddWithValue("@code", code);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Factory
                        {
                            Id = reader.GetInt32(0),
                            FactoryCode = reader.GetString(1),
                            FactoryName = reader.GetString(2),
                            FactoryType = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Address = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Certifications = reader.IsDBNull(5) ? null : reader.GetString(5),
                            Description = reader.IsDBNull(6) ? null : reader.GetString(6),
                            Scale = reader.IsDBNull(7) ? null : reader.GetString(7),
                            EmployeeCount = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                            ProductionCapacity = reader.IsDBNull(9) ? null : reader.GetString(9),
                            ControllingPerson = reader.IsDBNull(10) ? null : reader.GetString(10),
                            ContactPerson = reader.IsDBNull(11) ? null : reader.GetString(11),
                            ContactInfo = reader.IsDBNull(12) ? null : reader.GetString(12),
                            ContactMethod = reader.IsDBNull(13) ? null : reader.GetString(13),
                            CreatedAt = reader.IsDBNull(14) ? null : DateTime.Parse(reader.GetString(14)),
                            UpdatedAt = reader.IsDBNull(15) ? null : DateTime.Parse(reader.GetString(15))
                        };
                    }
                }
            }
            return null;
        }

        public void SaveFactory(Factory factory)
        {
            try
            {
                LogService.LogMethodEntry("SaveFactory", "DbService");
                LogService.Info($"保存工厂: [{factory.FactoryCode}] {factory.FactoryName}", "DbService");

                using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
                {
                    conn.Open();
                    var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    if (factory.Id == 0)
                    {
                        var cmd = new SQLiteCommand(@"
                            INSERT INTO Factories (factory_code, factory_name, factory_type, address, certifications, 
                                description, scale, employee_count, production_capacity, controlling_person, 
                                contact_person, contact_info, contact_method, created_at, updated_at)
                            VALUES (@code, @name, @type, @address, @certifications, @description, 
                                @scale, @employeeCount, @productionCapacity, @controllingPerson, 
                                @contactPerson, @contactInfo, @contactMethod, @createdAt, @updatedAt)
                        ", conn);
                        cmd.Parameters.AddWithValue("@code", ToDbValue(factory.FactoryCode));
                        cmd.Parameters.AddWithValue("@name", ToDbValue(factory.FactoryName));
                        cmd.Parameters.AddWithValue("@type", ToDbValue(factory.FactoryType));
                        cmd.Parameters.AddWithValue("@address", ToDbValue(factory.Address));
                        cmd.Parameters.AddWithValue("@certifications", ToDbValue(factory.Certifications));
                        cmd.Parameters.AddWithValue("@description", ToDbValue(factory.Description));
                        cmd.Parameters.AddWithValue("@scale", ToDbValue(factory.Scale));
                        cmd.Parameters.AddWithValue("@employeeCount", ToDbValue(factory.EmployeeCount));
                        cmd.Parameters.AddWithValue("@productionCapacity", ToDbValue(factory.ProductionCapacity));
                        cmd.Parameters.AddWithValue("@controllingPerson", ToDbValue(factory.ControllingPerson));
                        cmd.Parameters.AddWithValue("@contactPerson", ToDbValue(factory.ContactPerson));
                        cmd.Parameters.AddWithValue("@contactInfo", ToDbValue(factory.ContactInfo));
                        cmd.Parameters.AddWithValue("@contactMethod", ToDbValue(factory.ContactMethod));
                        cmd.Parameters.AddWithValue("@createdAt", now);
                        cmd.Parameters.AddWithValue("@updatedAt", now);
                        cmd.ExecuteNonQuery();
                        LogService.LogDatabaseOperation("INSERT", "Factories", 1, "DbService");
                    }
                    else
                    {
                        var cmd = new SQLiteCommand(@"
                            UPDATE Factories SET factory_code = @code, factory_name = @name, factory_type = @type, 
                                address = @address, certifications = @certifications, description = @description, 
                                scale = @scale, employee_count = @employeeCount, production_capacity = @productionCapacity, 
                                controlling_person = @controllingPerson, contact_person = @contactPerson, 
                                contact_info = @contactInfo, contact_method = @contactMethod, updated_at = @updatedAt
                            WHERE id = @id
                        ", conn);
                        cmd.Parameters.AddWithValue("@id", factory.Id);
                        cmd.Parameters.AddWithValue("@code", ToDbValue(factory.FactoryCode));
                        cmd.Parameters.AddWithValue("@name", ToDbValue(factory.FactoryName));
                        cmd.Parameters.AddWithValue("@type", ToDbValue(factory.FactoryType));
                        cmd.Parameters.AddWithValue("@address", ToDbValue(factory.Address));
                        cmd.Parameters.AddWithValue("@certifications", ToDbValue(factory.Certifications));
                        cmd.Parameters.AddWithValue("@description", ToDbValue(factory.Description));
                        cmd.Parameters.AddWithValue("@scale", ToDbValue(factory.Scale));
                        cmd.Parameters.AddWithValue("@employeeCount", ToDbValue(factory.EmployeeCount));
                        cmd.Parameters.AddWithValue("@productionCapacity", ToDbValue(factory.ProductionCapacity));
                        cmd.Parameters.AddWithValue("@controllingPerson", ToDbValue(factory.ControllingPerson));
                        cmd.Parameters.AddWithValue("@contactPerson", ToDbValue(factory.ContactPerson));
                        cmd.Parameters.AddWithValue("@contactInfo", ToDbValue(factory.ContactInfo));
                        cmd.Parameters.AddWithValue("@contactMethod", ToDbValue(factory.ContactMethod));
                        cmd.Parameters.AddWithValue("@updatedAt", now);
                        cmd.ExecuteNonQuery();
                        LogService.LogDatabaseOperation("UPDATE", "Factories", 1, "DbService");
                    }
                }
                LogService.LogMethodExit("SaveFactory", "DbService");
            }
            catch (Exception ex)
            {
                LogService.Error(ex, "DbService");
                throw;
            }
        }

        public void DeleteFactory(int id)
        {
            try
            {
                LogService.LogMethodEntry("DeleteFactory", "DbService");
                LogService.Info($"删除工厂: ID={id}", "DbService");

                using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
                {
                    conn.Open();
                    var cmd = new SQLiteCommand("DELETE FROM Factories WHERE id = @id", conn);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                LogService.LogDatabaseOperation("DELETE", "Factories", 1, "DbService");
                LogService.LogMethodExit("DeleteFactory", "DbService");
            }
            catch (Exception ex)
            {
                LogService.Error(ex, "DbService");
                throw;
            }
        }

        public List<Product> GetProducts()
        {
            var products = new List<Product>();
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                var cmd = new SQLiteCommand(@"
                    SELECT p.*, f.factory_name 
                    FROM FactoryProducts p 
                    LEFT JOIN Factories f ON p.factory_id = f.id 
                    ORDER BY p.my_product_code
                ", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(new Product
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
                            SubCategory = reader.IsDBNull(11) ? null : reader.GetString(11),
                            ImageUrl = reader.IsDBNull(12) ? null : reader.GetString(12),
                            FactoryId = reader.IsDBNull(13) ? null : reader.GetInt32(13),
                            FactoryCode = reader.IsDBNull(14) ? null : reader.GetString(14),
                            CreatedAt = reader.IsDBNull(15) ? null : DateTime.Parse(reader.GetString(15)),
                            UpdatedAt = reader.IsDBNull(16) ? null : DateTime.Parse(reader.GetString(16)),
                            FactoryName = reader.IsDBNull(17) ? null : reader.GetString(17)
                        });
                    }
                }
            }
            return products;
        }

        public void SaveProduct(Product product)
        {
            try
            {
                LogService.LogMethodEntry("SaveProduct", "DbService");
                LogService.Info($"保存产品: [{product.FactoryProductCode}] {product.ProductName}", "DbService");

                using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
                {
                    conn.Open();
                    var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    if (product.Id == 0)
                    {
                        var cmd = new SQLiteCommand(@"
                            INSERT INTO FactoryProducts (factory_product_code, my_product_code, product_name, brand, 
                                specification, texture, process, usage_scenario, certifications, category, 
                                sub_category, image_url, factory_id, factory_code, created_at, updated_at)
                            VALUES (@factoryCode, @myCode, @name, @brand, @specification, @texture, @process, 
                                @usageScenario, @certifications, @category, @subCategory, @imageUrl, 
                                @factoryId, @factoryCodeStr, @createdAt, @updatedAt)
                        ", conn);
                        cmd.Parameters.AddWithValue("@factoryCode", ToDbValue(product.FactoryProductCode));
                        cmd.Parameters.AddWithValue("@myCode", ToDbValue(product.MyProductCode));
                        cmd.Parameters.AddWithValue("@name", ToDbValue(product.ProductName));
                        cmd.Parameters.AddWithValue("@brand", ToDbValue(product.Brand));
                        cmd.Parameters.AddWithValue("@specification", ToDbValue(product.Specification));
                        cmd.Parameters.AddWithValue("@texture", ToDbValue(product.Texture));
                        cmd.Parameters.AddWithValue("@process", ToDbValue(product.Process));
                        cmd.Parameters.AddWithValue("@usageScenario", ToDbValue(product.UsageScenario));
                        cmd.Parameters.AddWithValue("@certifications", ToDbValue(product.Certifications));
                        cmd.Parameters.AddWithValue("@category", ToDbValue(product.Category));
                        cmd.Parameters.AddWithValue("@subCategory", ToDbValue(product.SubCategory));
                        cmd.Parameters.AddWithValue("@imageUrl", ToDbValue(product.ImageUrl));
                        cmd.Parameters.AddWithValue("@factoryId", ToDbValue(product.FactoryId));
                        cmd.Parameters.AddWithValue("@factoryCodeStr", ToDbValue(product.FactoryCode));
                        cmd.Parameters.AddWithValue("@createdAt", now);
                        cmd.Parameters.AddWithValue("@updatedAt", now);
                        cmd.ExecuteNonQuery();
                        LogService.LogDatabaseOperation("INSERT", "FactoryProducts", 1, "DbService");
                    }
                    else
                    {
                        var cmd = new SQLiteCommand(@"
                            UPDATE FactoryProducts SET factory_product_code = @factoryCode, my_product_code = @myCode, 
                                product_name = @name, brand = @brand, specification = @specification, 
                                texture = @texture, process = @process, usage_scenario = @usageScenario, 
                                certifications = @certifications, category = @category, sub_category = @subCategory, 
                                image_url = @imageUrl, factory_id = @factoryId, factory_code = @factoryCodeStr, 
                                updated_at = @updatedAt WHERE id = @id
                        ", conn);
                        cmd.Parameters.AddWithValue("@id", product.Id);
                        cmd.Parameters.AddWithValue("@factoryCode", ToDbValue(product.FactoryProductCode));
                        cmd.Parameters.AddWithValue("@myCode", ToDbValue(product.MyProductCode));
                        cmd.Parameters.AddWithValue("@name", ToDbValue(product.ProductName));
                        cmd.Parameters.AddWithValue("@brand", ToDbValue(product.Brand));
                        cmd.Parameters.AddWithValue("@specification", ToDbValue(product.Specification));
                        cmd.Parameters.AddWithValue("@texture", ToDbValue(product.Texture));
                        cmd.Parameters.AddWithValue("@process", ToDbValue(product.Process));
                        cmd.Parameters.AddWithValue("@usageScenario", ToDbValue(product.UsageScenario));
                        cmd.Parameters.AddWithValue("@certifications", ToDbValue(product.Certifications));
                        cmd.Parameters.AddWithValue("@category", ToDbValue(product.Category));
                        cmd.Parameters.AddWithValue("@subCategory", ToDbValue(product.SubCategory));
                        cmd.Parameters.AddWithValue("@imageUrl", ToDbValue(product.ImageUrl));
                        cmd.Parameters.AddWithValue("@factoryId", ToDbValue(product.FactoryId));
                        cmd.Parameters.AddWithValue("@factoryCodeStr", ToDbValue(product.FactoryCode));
                        cmd.Parameters.AddWithValue("@updatedAt", now);
                        cmd.ExecuteNonQuery();
                        LogService.LogDatabaseOperation("UPDATE", "FactoryProducts", 1, "DbService");
                    }
                }
                LogService.LogMethodExit("SaveProduct", "DbService");
            }
            catch (Exception ex)
            {
                LogService.Error(ex, "DbService");
                throw;
            }
        }

        public void DeleteProduct(int id)
        {
            try
            {
                LogService.LogMethodEntry("DeleteProduct", "DbService");
                LogService.Info($"删除产品: ID={id}", "DbService");

                using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
                {
                    conn.Open();
                    var cmd = new SQLiteCommand("DELETE FROM FactoryProducts WHERE id = @id", conn);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
                LogService.LogDatabaseOperation("DELETE", "FactoryProducts", 1, "DbService");
                LogService.LogMethodExit("DeleteProduct", "DbService");
            }
            catch (Exception ex)
            {
                LogService.Error(ex, "DbService");
                throw;
            }
        }

        public int GetNextProductSequence()
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                var cmd = new SQLiteCommand("SELECT COUNT(*) FROM FactoryProducts", conn);
                var count = (long)cmd.ExecuteScalar();
                return (int)count + 1;
            }
        }

        public bool IsProductCodeExists(string code)
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                var cmd = new SQLiteCommand("SELECT COUNT(*) FROM FactoryProducts WHERE my_product_code = @code", conn);
                cmd.Parameters.AddWithValue("@code", code);
                var count = (long)cmd.ExecuteScalar();
                return count > 0;
            }
        }
    }
}