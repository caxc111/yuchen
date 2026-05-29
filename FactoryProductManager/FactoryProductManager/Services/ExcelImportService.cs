using FactoryProductManager.Models;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.IO;

namespace FactoryProductManager.Services
{
    public class ExcelImportService
    {
        private readonly DbService _dbService;

        public ExcelImportService(DbService dbService)
        {
            _dbService = dbService;
        }

        public int ImportFactoriesFromExcel(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Excel文件不存在", filePath);
            }

            List<Factory> factories = new List<Factory>();

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                HSSFWorkbook workbook = new HSSFWorkbook(stream);
                ISheet sheet = workbook.GetSheetAt(0);

                int rowCount = sheet.LastRowNum;

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
                    }
                    catch (Exception ex)
                    {
                        LogService.LogError($"导入第{row}行数据失败", ex);
                    }
                }
            }

            int importedCount = 0;
            foreach (var factory in factories)
            {
                try
                {
                    _dbService.AddFactory(factory);
                    importedCount++;
                    LogService.LogInfo($"成功导入工厂: {factory.FactoryCode} - {factory.FactoryName}");
                }
                catch (Exception ex)
                {
                    LogService.LogError($"导入工厂 {factory.FactoryCode} 失败", ex);
                }
            }

            return importedCount;
        }

        private string GetCellStringValue(ICell cell)
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

        private int? GetCellIntValue(ICell cell)
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