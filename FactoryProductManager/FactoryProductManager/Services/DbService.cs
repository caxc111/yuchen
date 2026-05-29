using FactoryProductManager.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace FactoryProductManager.Services
{
    public class DbService
    {
        private readonly string _connectionString;

        public DbService(string databasePath = "FactoryProductDB.db")
        {
            _connectionString = $"Data Source={databasePath};Version=3;";
            LogService.LogInfo($"数据库连接初始化: {databasePath}");
            CreateTables();
        }

        private void CreateTables()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    string createFactoriesTable = @"
                        CREATE TABLE IF NOT EXISTS Factories (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            factory_code TEXT UNIQUE NOT NULL,
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
                    LogService.LogInfo("工厂表创建成功");
                }
            }
            catch (Exception ex)
            {
                LogService.LogError($"创建表失败: {ex.Message}");
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
            var factories = new List<Factory>();
            try
            {
                using (var conn = GetConnection())
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
                                CreatedAt = reader.IsDBNull(14) ? DateTime.Now : reader.GetDateTime(14),
                                UpdatedAt = reader.IsDBNull(15) ? DateTime.Now : reader.GetDateTime(15)
                            });
                        }
                    }
                }
                LogService.LogInfo($"查询工厂列表成功，共 {factories.Count} 条记录");
            }
            catch (Exception ex)
            {
                LogService.LogError("查询工厂列表失败", ex);
                throw;
            }
            return factories;
        }

        public List<FactoryProduct> GetFactoryProducts()
        {
            var products = new List<FactoryProduct>();
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
                                CreatedAt = reader.GetDateTime(14),
                                UpdatedAt = reader.GetDateTime(15)
                            });
                        }
                    }
                }
                LogService.LogInfo($"查询工厂产品列表成功，共 {products.Count} 条记录");
            }
            catch (Exception ex)
            {
                LogService.LogError("查询工厂产品列表失败", ex);
                throw;
            }
            return products;
        }

        public int AddFactory(Factory factory)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SQLiteCommand(@"
                        INSERT INTO Factories (factory_code, factory_name, factory_type, address, 
                            certifications, description, scale, employee_count, production_capacity,
                            controlling_person, contact_person, contact_info, contact_method)
                        VALUES (@code, @name, @type, @address, @cert, @desc, @scale, @empCount, 
                            @capacity, @controller, @contact, @contactInfo, @contactMethod);
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
                    cmd.Parameters.AddWithValue("@contactMethod", ToDbValue(factory.ContactMethod));
                    var id = Convert.ToInt32(cmd.ExecuteScalar());
                    LogService.LogInfo($"添加工厂成功: ID={id}, 编码={factory.FactoryCode}, 名称={factory.FactoryName}");
                    return id;
                }
            }
            catch (Exception ex)
            {
                LogService.LogError($"添加工厂失败: {factory.FactoryCode}", ex);
                throw;
            }
        }

        public int AddFactoryProduct(FactoryProduct product)
        {
            try
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
                    var id = Convert.ToInt32(cmd.ExecuteScalar());
                    LogService.LogInfo($"添加工厂产品成功: ID={id}, 编码={product.FactoryProductCode}, 名称={product.ProductName}");
                    return id;
                }
            }
            catch (Exception ex)
            {
                LogService.LogError($"添加工厂产品失败: {product.FactoryProductCode}", ex);
                throw;
            }
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
                            contact_method = @contactMethod,
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
                    cmd.Parameters.AddWithValue("@contactMethod", ToDbValue(factory.ContactMethod));
                    cmd.Parameters.AddWithValue("@updatedAt", factory.UpdatedAt);
                    cmd.Parameters.AddWithValue("@id", factory.Id);
                    cmd.ExecuteNonQuery();
                    LogService.LogInfo($"更新工厂成功: ID={factory.Id}, 编码={factory.FactoryCode}, 名称={factory.FactoryName}");
                }
            }
            catch (Exception ex)
            {
                LogService.LogError($"更新工厂失败: ID={factory.Id}", ex);
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
                    LogService.LogInfo($"删除工厂成功: ID={id}");
                }
            }
            catch (Exception ex)
            {
                LogService.LogError($"删除工厂失败: ID={id}", ex);
                throw;
            }
        }

        public Factory GetFactoryByCode(string factoryCode)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var cmd = new SQLiteCommand("SELECT * FROM Factories WHERE factory_code = @code", conn);
                    cmd.Parameters.AddWithValue("@code", factoryCode);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var factory = new Factory
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
                                CreatedAt = reader.IsDBNull(14) ? DateTime.Now : reader.GetDateTime(14),
                                UpdatedAt = reader.IsDBNull(15) ? DateTime.Now : reader.GetDateTime(15)
                            };
                            LogService.LogInfo($"按编码查询工厂成功: 编码={factoryCode}, ID={factory.Id}, 名称={factory.FactoryName}");
                            return factory;
                        }
                    }
                }
                LogService.LogInfo($"按编码查询工厂未找到: 编码={factoryCode}");
            }
            catch (Exception ex)
            {
                LogService.LogError($"按编码查询工厂失败: {factoryCode}", ex);
                throw;
            }
            return null;
        }
    }
}
