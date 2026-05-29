using ExcelImporter.Models;
using ExcelImporter.Services;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.IO;

namespace ExcelImporter
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== 工厂数据导入工具 ===");
            Console.WriteLine();

            string excelPath = @"d:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\工厂信息.xls";
            string dbPath = @"d:\BaiduSyncdisk\宇程科技智能家居\编程\宇辰信息中心\FactoryProductManager\FactoryProductManager\FactoryProductDB.db";

            Console.WriteLine($"Excel文件: {excelPath}");
            Console.WriteLine($"数据库文件: {dbPath}");
            Console.WriteLine();

            if (!File.Exists(excelPath))
            {
                Console.WriteLine($"错误: Excel文件不存在 - {excelPath}");
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
                return;
            }

            if (!File.Exists(dbPath))
            {
                Console.WriteLine($"警告: 数据库文件不存在，将自动创建 - {dbPath}");
            }

            Console.WriteLine("自动确认导入数据...");
            string input = "Y";

            if (input?.Trim().ToUpper() == "Y")
            {
                try
                {
                    List<Factory> factories = ReadExcelFile(excelPath);
                    Console.WriteLine($"\n读取到 {factories.Count} 条工厂数据");

                    var dbService = new DbService(dbPath);
                    int importedCount = 0;

                    foreach (var factory in factories)
                    {
                        try
                        {
                            dbService.AddFactory(factory);
                            importedCount++;
                            Console.WriteLine($"成功导入: {factory.FactoryCode} - {factory.FactoryName}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"失败: {factory.FactoryCode} - {ex.Message}");
                        }
                    }

                    Console.WriteLine();
                    Console.WriteLine($"导入完成！成功导入 {importedCount} 条工厂数据");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"导入失败: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            }
            else
            {
                Console.WriteLine("操作已取消");
            }

            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        private static List<Factory> ReadExcelFile(string excelPath)
        {
            List<Factory> factories = new List<Factory>();

            using (var stream = new FileStream(excelPath, FileMode.Open, FileAccess.Read))
            {
                HSSFWorkbook workbook = new HSSFWorkbook(stream);
                ISheet sheet = workbook.GetSheetAt(0);

                int rowCount = sheet.LastRowNum;
                Console.WriteLine($"发现 {rowCount} 行数据");

                for (int row = 1; row <= rowCount; row++)
                {
                    IRow excelRow = sheet.GetRow(row);
                    if (excelRow == null) continue;

                    try
                    {
                        string factoryCode = GetCellStringValue(excelRow.GetCell(0));
                        if (string.IsNullOrWhiteSpace(factoryCode))
                            continue;

                        var factory = new Factory
                        {
                            FactoryCode = factoryCode,
                            FactoryName = GetCellStringValue(excelRow.GetCell(1)),
                            FactoryType = GetCellStringValue(excelRow.GetCell(2)),
                            Certifications = GetCellStringValue(excelRow.GetCell(3)),
                            Description = GetCellStringValue(excelRow.GetCell(4)),
                            Scale = GetCellStringValue(excelRow.GetCell(5)),
                            EmployeeCount = GetCellIntValue(excelRow.GetCell(6)),
                            ProductionCapacity = GetCellStringValue(excelRow.GetCell(7)),
                            ContactPerson = GetCellStringValue(excelRow.GetCell(8)),
                            ContactInfo = GetCellStringValue(excelRow.GetCell(9)),
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };

                        factories.Add(factory);
                        Console.WriteLine($"读取第{row}行: {factory.FactoryCode} - {factory.FactoryName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"警告: 读取第{row}行失败 - {ex.Message}");
                    }
                }
            }

            return factories;
        }

        private static string GetCellStringValue(ICell cell)
        {
            if (cell == null) return string.Empty;

            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue.Trim();
                case CellType.Numeric:
                    return cell.NumericCellValue.ToString();
                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();
                case CellType.Formula:
                    try
                    {
                        return cell.NumericCellValue.ToString();
                    }
                    catch
                    {
                        return cell.StringCellValue.Trim();
                    }
                default:
                    return string.Empty;
            }
        }

        private static int? GetCellIntValue(ICell cell)
        {
            if (cell == null) return null;

            switch (cell.CellType)
            {
                case CellType.Numeric:
                    return (int)cell.NumericCellValue;
                case CellType.String:
                    if (int.TryParse(cell.StringCellValue.Trim(), out int result))
                        return result;
                    return null;
                default:
                    return null;
            }
        }
    }
}