using FactoryProductManager.Models;
using FactoryProductManager.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace FactoryProductManager.ViewModels
{
    public class ProductViewModel : ViewModelBase
    {
        private readonly DbService _dbService;

        public ObservableCollection<FactoryProduct> Products { get; set; }

        public ProductViewModel()
        {
            LogService.LogViewModelCreation(nameof(ProductViewModel));
            try
            {
                LogService.Info("初始化ProductViewModel...");
                _dbService = new DbService();
                Products = new ObservableCollection<FactoryProduct>();
                LogService.Info("开始加载产品数据...");
                LoadProducts();
                LogService.Info($"ProductViewModel初始化完成，共加载 {Products.Count} 条产品数据");
            }
            catch (Exception ex)
            {
                LogService.Error("ProductViewModel初始化失败", ex);
                throw;
            }
        }

        private void LoadProducts()
        {
            try
            {
                LogService.Debug("进入LoadProducts方法");
                Products.Clear();
                var products = _dbService.GetFactoryProducts();
                foreach (var product in products)
                {
                    Products.Add(product);
                }
                LogService.Debug($"LoadProducts方法完成，加载了 {Products.Count} 条记录");
            }
            catch (Exception ex)
            {
                LogService.Error("加载产品数据失败", ex);
                throw;
            }
        }

        public void AddProduct(FactoryProduct product)
        {
            try
            {
                LogService.Info("开始添加产品: " + product.ProductName);
                product.CreatedAt = DateTime.Now;
                product.UpdatedAt = DateTime.Now;
                var id = _dbService.AddFactoryProduct(product);
                product.Id = id;
                Products.Add(product);
                LogService.Info("产品添加成功，ID: " + id);
            }
            catch (Exception ex)
            {
                LogService.Error("添加产品失败: " + product.ProductName, ex);
                throw;
            }
        }

        public void UpdateProduct(FactoryProduct product)
        {
            try
            {
                LogService.Info("开始更新产品: " + product.ProductName);
                product.UpdatedAt = DateTime.Now;
                _dbService.UpdateFactoryProduct(product);
                var index = Products.IndexOf(Products.First(p => p.Id == product.Id));
                if (index >= 0)
                {
                    Products[index] = product;
                }
                LogService.Info("产品更新成功，ID: " + product.Id);
            }
            catch (Exception ex)
            {
                LogService.Error("更新产品失败: " + product.ProductName, ex);
                throw;
            }
        }

        public void DeleteProduct(int id)
        {
            try
            {
                var product = Products.FirstOrDefault(p => p.Id == id);
                LogService.Info("开始删除产品: " + (product?.ProductName ?? "未知"));
                _dbService.DeleteFactoryProduct(id);
                if (product != null)
                {
                    Products.Remove(product);
                }
                LogService.Info("产品删除成功，ID: " + id);
            }
            catch (Exception ex)
            {
                LogService.Error("删除产品失败，ID: " + id, ex);
                throw;
            }
        }

        public void Refresh()
        {
            try
            {
                LogService.Info("刷新产品数据...");
                LoadProducts();
                LogService.Info("产品数据刷新完成");
            }
            catch (Exception ex)
            {
                LogService.Error("刷新产品数据失败", ex);
                throw;
            }
        }
    }
}