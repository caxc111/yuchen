using FactoryProductManager.Models;
using FactoryProductManager.Services;
using System.Collections.ObjectModel;

namespace FactoryProductManager.ViewModels
{
    public class ProductViewModel : ViewModelBase
    {
        private readonly DbService _dbService;

        public ObservableCollection<FactoryProduct> Products { get; set; }

        public ProductViewModel()
        {
            _dbService = new DbService();
            Products = new ObservableCollection<FactoryProduct>();
            LoadProducts();
        }

        private void LoadProducts()
        {
            var products = _dbService.GetFactoryProducts();
            foreach (var product in products)
            {
                Products.Add(product);
            }
        }
    }
}
