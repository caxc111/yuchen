using System;
using System.Collections.Generic;
using System.Linq;
using FactoryProductManager.Models;

namespace FactoryProductManager.Services
{
    /// <summary>
    /// 复合物料服务：统一管理复合物料的加载和保存
    /// </summary>
    public class CompositeMaterialService
    {
        private readonly DbService _dbService;

        public CompositeMaterialService(DbService dbService)
        {
            _dbService = dbService;
        }

        /// <summary>
        /// 加载产品的所有复合物料
        /// </summary>
        public List<CompositeMaterial> LoadFromDatabase(int productId)
        {
            var result = new List<CompositeMaterial>();

            // 获取该产品的所有物料记录（包括主行和子行）
            var records = _dbService.GetProductPartMaterials(productId)
                .Where(m => !string.IsNullOrEmpty(m.GroupCode))  // 有 GroupCode 的才是复合物料相关
                .ToList();

            if (records.Count == 0) return result;

            // 主行：GroupCode 有值, MaterialId 为 null
            var mainRows = records.Where(r => r.MaterialId == null).ToList();

            foreach (var mainRow in mainRows)
            {
                var composite = new CompositeMaterial
                {
                    Id = mainRow.Id,
                    PartName = mainRow.PartName ?? "",
                    ComponentName = mainRow.ComponentName ?? "",
                    MaterialTypeName = mainRow.MaterialTypeName ?? "",
                    GroupCode = mainRow.GroupCode ?? "",
                    CabinetName = mainRow.MaterialName ?? "",
                    DrawingNumber = mainRow.DrawingNumber ?? "",
                    Quantity = (int)(mainRow.Quantity > 0 ? mainRow.Quantity : 1)
                };

                // 子行：ParentId 指向主行，或 GroupCode + MaterialName 相同
                var childRows = records
                    .Where(r => r.MaterialId != null &&
                                r.GroupCode == mainRow.GroupCode &&
                                r.MaterialName == mainRow.MaterialName)
                    .ToList();

                foreach (var childRow in childRows)
                {
                    var item = new CompositeMaterialItem
                    {
                        Id = childRow.Id,
                        FactoryMaterialId = childRow.MaterialId ?? 0,
                        ItemName = childRow.ItemName ?? "",
                        MaterialName = childRow.MaterialName ?? "",
                        Specification = childRow.Specification ?? "",
                        Unit = childRow.Unit ?? "",
                        UnitPrice = childRow.UnitPrice,
                        Quantity = childRow.Quantity > 0 ? (double)childRow.Quantity : 1,
                        FactoryMaterialCode = childRow.FactoryMaterialCode ?? "",
                        MyMaterialCode = childRow.MyMaterialCode ?? "",
                        Brand = childRow.Brand ?? "",
                        ImageUrl = childRow.ImageUrl ?? ""
                    };
                    composite.Items.Add(item);
                }

                result.Add(composite);
            }

            return result;
        }

        /// <summary>
        /// 保存复合物料到数据库（批量写入）
        /// </summary>
        public void SaveToDatabase(int productId, IEnumerable<CompositeMaterial> composites, Dictionary<string, int> partMap)
        {
            var records = new List<ProductPartMaterial>();

            foreach (var composite in composites)
            {
                // 主行
                var mainRow = new ProductPartMaterial
                {
                    ProductId = productId,
                    PartId = partMap.TryGetValue(composite.PartName, out var pid) && pid > 0 ? (int?)pid : null,
                    PartName = composite.PartName,
                    ComponentName = composite.ComponentName,
                    MaterialTypeName = composite.MaterialTypeName,
                    MaterialId = null,
                    MaterialName = composite.CabinetName,
                    FactoryMaterialCode = "",
                    MyMaterialCode = "",
                    Brand = "",
                    Specification = "",
                    Unit = "",
                    UnitPrice = 0,
                    Quantity = composite.Quantity,
                    IsComposite = true,
                    GroupCode = composite.GroupCode,
                    DrawingNumber = composite.DrawingNumber,
                    ItemName = "",
                    ParentId = null,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                mainRow.TotalPrice = composite.TotalPrice;
                records.Add(mainRow);

                // 子行
                foreach (var item in composite.Items)
                {
                    records.Add(new ProductPartMaterial
                    {
                        ProductId = productId,
                        PartId = partMap.TryGetValue(composite.PartName, out var pid2) && pid2 > 0 ? (int?)pid2 : null,
                        PartName = composite.PartName,
                        ComponentName = composite.ComponentName,
                        MaterialTypeName = composite.MaterialTypeName,
                        MaterialId = item.FactoryMaterialId > 0 ? (int?)item.FactoryMaterialId : null,
                        MaterialName = item.MaterialName,
                        FactoryMaterialCode = item.FactoryMaterialCode,
                        MyMaterialCode = item.MyMaterialCode,
                        Brand = item.Brand,
                        Specification = item.Specification,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Quantity = (decimal)item.Quantity,
                        IsComposite = false,
                        GroupCode = composite.GroupCode,
                        ItemName = item.ItemName,
                        ParentId = 0,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        TotalPrice = item.TotalPrice
                    });
                }
            }

            _dbService.AddProductPartMaterials(productId, records);
        }

        /// <summary>
        /// 将 SelectedMaterial 列表中的复合物料转换为 CompositeMaterial 列表
        /// </summary>
        public List<CompositeMaterial> ConvertFromSelectedMaterials(IEnumerable<SelectedMaterial> selectedMaterials)
        {
            var result = new List<CompositeMaterial>();

            foreach (var sm in selectedMaterials.Where(m => m.IsComposite))
            {
                var composite = new CompositeMaterial
                {
                    Id = sm.Id,
                    PartName = sm.PartName,
                    ComponentName = sm.ComponentName,
                    MaterialTypeName = sm.MaterialTypeName,
                    GroupCode = sm.GroupCode,
                    CabinetName = sm.MaterialName,
                    DrawingNumber = sm.DrawingNumber,
                    Quantity = (int)sm.Quantity
                };

                foreach (var child in sm.Children)
                {
                    var item = new CompositeMaterialItem
                    {
                        Id = child.Id,
                        FactoryMaterialId = child.FactoryMaterialId,
                        ItemName = child.ItemName,
                        MaterialName = child.MaterialName,
                        Specification = child.Specification,
                        Unit = child.Unit,
                        UnitPrice = child.UnitPrice,
                        Quantity = (double)child.Quantity,
                        FactoryMaterialCode = child.FactoryMaterialCode,
                        MyMaterialCode = child.MyMaterialCode,
                        Brand = child.Brand,
                        ImageUrl = child.ImageUrl
                    };
                    composite.Items.Add(item);
                }

                result.Add(composite);
            }

            return result;
        }
    }
}
