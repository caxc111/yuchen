using FactoryProductManager.Models;
using FactoryProductManager.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace FactoryProductManager.ViewModels
{
    public class FactoryViewModel : ViewModelBase
    {
        private readonly DbService _dbService;
        private string _currentSearchKeyword;

        public ObservableCollection<Factory> Factories { get; set; }

        public FactoryViewModel()
        {
            LogService.LogViewModelCreation(nameof(FactoryViewModel));
            try
            {
                LogService.Info("初始化FactoryViewModel...");
                _dbService = new DbService();
                Factories = new ObservableCollection<Factory>();
                LogService.Info("开始加载工厂数据...");
                LoadFactories();
                LogService.Info($"FactoryViewModel初始化完成，共加载 {Factories.Count} 条工厂数据");
            }
            catch (Exception ex)
            {
                LogService.Error("FactoryViewModel初始化失败", ex);
                throw;
            }
        }

        private void LoadFactories(string searchKeyword = null)
        {
            try
            {
                LogService.Debug("进入LoadFactories方法");
                Factories.Clear();
                var factories = _dbService.GetFactories();
                
                // 如果有搜索关键词，进行过滤
                if (!string.IsNullOrEmpty(searchKeyword))
                {
                    var keyword = searchKeyword.ToLower().Trim();
                    factories = factories.Where(f => 
                        (f.FactoryCode?.ToLower().Contains(keyword) ?? false) ||
                        (f.FactoryName?.ToLower().Contains(keyword) ?? false) ||
                        (f.FactoryType?.ToLower().Contains(keyword) ?? false) ||
                        (f.Address?.ToLower().Contains(keyword) ?? false) ||
                        (f.Certifications?.ToLower().Contains(keyword) ?? false) ||
                        (f.Description?.ToLower().Contains(keyword) ?? false) ||
                        (f.Scale?.ToLower().Contains(keyword) ?? false) ||
                        (f.ProductionCapacity?.ToLower().Contains(keyword) ?? false) ||
                        (f.ControllingPerson?.ToLower().Contains(keyword) ?? false) ||
                        (f.ContactPerson?.ToLower().Contains(keyword) ?? false) ||
                        (f.ContactInfo?.ToLower().Contains(keyword) ?? false) ||
                        (f.EmployeeCount?.ToString().Contains(keyword) ?? false)
                    ).ToList();
                    LogService.Debug($"搜索关键词: {searchKeyword}，筛选出 {factories.Count} 条记录");
                }
                
                foreach (var factory in factories)
                {
                    Factories.Add(factory);
                }
                LogService.Debug($"LoadFactories方法完成，加载了 {Factories.Count} 条记录");
            }
            catch (Exception ex)
            {
                LogService.Error("加载工厂数据失败", ex);
                throw;
            }
        }
        
        public void Search(string keyword)
        {
            LogService.Info($"执行搜索: {keyword}");
            _currentSearchKeyword = keyword;
            LoadFactories(keyword);
        }

        public void AddFactory(Factory factory)
        {
            try
            {
                LogService.Info("开始添加工厂: " + factory.FactoryName);
                
                // 检查工厂编码是否已存在
                var existingFactory = Factories.FirstOrDefault(f => f.FactoryCode == factory.FactoryCode);
                if (existingFactory != null)
                {
                    LogService.Warning($"工厂编码 '{factory.FactoryCode}' 已存在");
                    System.Windows.MessageBox.Show($"工厂编码 '{factory.FactoryCode}' 已存在，请使用其他编码！", "添加失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                factory.CreatedAt = DateTime.Now;
                factory.UpdatedAt = DateTime.Now;
                var id = _dbService.AddFactory(factory);
                factory.Id = id;
                
                // 根据当前搜索关键词刷新列表
                LoadFactories(_currentSearchKeyword);
                
                LogService.Info("工厂添加成功，ID: " + id);
                System.Windows.MessageBox.Show("工厂添加成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                LogService.Error("添加工厂失败: " + factory.FactoryName, ex);
                System.Windows.MessageBox.Show($"添加工厂失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void UpdateFactory(Factory factory)
        {
            try
            {
                LogService.Info("开始更新工厂: " + factory.FactoryName);
                factory.UpdatedAt = DateTime.Now;
                _dbService.UpdateFactory(factory);
                var index = Factories.IndexOf(Factories.First(f => f.Id == factory.Id));
                if (index >= 0)
                {
                    Factories[index] = factory;
                }
                LogService.Info("工厂更新成功，ID: " + factory.Id);
            }
            catch (Exception ex)
            {
                LogService.Error("更新工厂失败: " + factory.FactoryName, ex);
                throw;
            }
        }

        public void DeleteFactory(int id)
        {
            try
            {
                var factory = Factories.FirstOrDefault(f => f.Id == id);
                LogService.Info("开始删除工厂: " + (factory?.FactoryName ?? "未知"));
                _dbService.DeleteFactory(id);
                if (factory != null)
                {
                    Factories.Remove(factory);
                }
                LogService.Info("工厂删除成功，ID: " + id);
            }
            catch (Exception ex)
            {
                LogService.Error("删除工厂失败，ID: " + id, ex);
                throw;
            }
        }

        public void Refresh()
        {
            try
            {
                LogService.Info("刷新工厂数据...");
                LoadFactories();
                LogService.Info("工厂数据刷新完成");
            }
            catch (Exception ex)
            {
                LogService.Error("刷新工厂数据失败", ex);
                throw;
            }
        }
    }
}