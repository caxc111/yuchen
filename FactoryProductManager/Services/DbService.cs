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
                            CreatedAt = reader.GetDateTime(13),
                            UpdatedAt = reader.GetDateTime(14)
                        });
                    }
                }
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
                            CreatedAt = reader.GetDateTime(14),
                            UpdatedAt = reader.GetDateTime(15)
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
    }
}