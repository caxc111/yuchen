using FactoryProductManager.Models;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.IO;

namespace FactoryProductManager.Tools
{
    public class ExcelImporter
    {
        public static int ImportFromExcel(string excelPath, string dbPath = "FactoryProductDB.db")
        {
            if (!File.Exists(excelPath))
            {
                Console.WriteLine($"错误: 文件不存在 - {excelPath}");
                return 0;
            }

            Console.WriteLine($"开始导入Excel文件: {excelPath}");

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

            Console.WriteLine($"\n准备导入 {factories.Count} 条工厂数据...");

            var dbService = new Services.DbService(dbPath);
            int importedCount = 0;

            foreach (var factory in factories)
            {
                try
                {
                    dbService.AddFactory(factory);
                    importedCount++;
                    Console.WriteLine($"成功导入: {factory.FactoryCode}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"失败: {factory.FactoryCode} - {ex.Message}");
                }
            }

            Console.WriteLine($"\n导入完成！成功导入 {importedCount} 条数据");
            return importedCount;
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