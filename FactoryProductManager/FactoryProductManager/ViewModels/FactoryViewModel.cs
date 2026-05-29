using FactoryProductManager.Models;
using FactoryProductManager.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace FactoryProductManager.ViewModels
{
    public class FactoryViewModel : ViewModelBase
    {
        private readonly DbService _dbService;

        public ObservableCollection<Factory> Factories { get; set; }

        public FactoryViewModel()
        {
            _dbService = new DbService();
            Factories = new ObservableCollection<Factory>();
            LoadFactories();
        }

        private void LoadFactories()
        {
            Factories.Clear();
            var factories = _dbService.GetFactories();
            foreach (var factory in factories)
            {
                Factories.Add(factory);
            }
        }

        public void AddFactory(Factory factory)
        {
            factory.CreatedAt = DateTime.Now;
            factory.UpdatedAt = DateTime.Now;
            var id = _dbService.AddFactory(factory);
            factory.Id = id;
            Factories.Add(factory);
        }

        public void UpdateFactory(Factory factory)
        {
            factory.UpdatedAt = DateTime.Now;
            _dbService.UpdateFactory(factory);
            var index = Factories.IndexOf(Factories.First(f => f.Id == factory.Id));
            if (index >= 0)
            {
                Factories[index] = factory;
            }
        }

        public void DeleteFactory(int id)
        {
            _dbService.DeleteFactory(id);
            var factory = Factories.FirstOrDefault(f => f.Id == id);
            if (factory != null)
            {
                Factories.Remove(factory);
            }
        }

        public void Refresh()
        {
            LoadFactories();
        }
    }
}
