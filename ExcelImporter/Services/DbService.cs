using ExcelImporter.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace ExcelImporter.Services
{
    public class DbService
    {
        private readonly string _connectionString;

        public DbService(string databasePath = "FactoryProductDB.db")
        {
            _connectionString = $"Data Source={databasePath};Version=3;";
            CreateTables();
        }

        private void CreateTables()
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                
                string createTableSql = @"
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
                
                using (var cmd = new SQLiteCommand(createTableSql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
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
            using (var conn = new SQLiteConnection(_connectionString))
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
                            EmployeeCount = reader.IsDBNull(8) ? null : (int?)reader.GetInt32(8),
                            ProductionCapacity = reader.IsDBNull(9) ? null : reader.GetString(9),
                            ControllingPerson = reader.IsDBNull(10) ? null : reader.GetString(10),
                            ContactPerson = reader.IsDBNull(11) ? null : reader.GetString(11),
                            ContactInfo = reader.IsDBNull(12) ? null : reader.GetString(12),
                            ContactMethod = reader.IsDBNull(13) ? null : reader.GetString(13),
                            CreatedAt = reader.IsDBNull(14) ? DateTime.Now : DateTime.Parse(reader.GetString(14)),
                            UpdatedAt = reader.IsDBNull(15) ? DateTime.Now : DateTime.Parse(reader.GetString(15))
                        });
                    }
                }
            }
            return factories;
        }

        public void AddFactory(Factory factory)
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string sql = @"
                    INSERT INTO Factories (factory_code, factory_name, factory_type, address,
                        certifications, description, scale, employee_count, production_capacity,
                        controlling_person, contact_person, contact_info,
                        created_at, updated_at)
                    VALUES (@code, @name, @type, @address, @cert, @desc, @scale, @empCount,
                        @capacity, @controller, @contact, @contactInfo,
                        @createdAt, @updatedAt)";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
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
                    cmd.Parameters.AddWithValue("@createdAt", factory.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@updatedAt", factory.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}