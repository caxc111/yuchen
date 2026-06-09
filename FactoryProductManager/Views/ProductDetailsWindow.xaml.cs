using FactoryProductManager.Models;
using System.Windows;

namespace FactoryProductManager.Views
{
    public partial class ProductDetailsWindow : Window
    {
        public ProductDetailsWindow(Product product)
        {
            InitializeComponent();
            DataContext = product;
        }
    }
}
