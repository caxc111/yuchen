using FactoryProductManager.Models;
using System.Windows;

namespace FactoryProductManager.Views
{
    public partial class ProductManagementDialog : Window
    {
        public Product Product { get; set; }
        public bool IsSaved { get; private set; }

        public ProductManagementDialog(Product? product = null)
        {
            InitializeComponent();
            if (product == null)
            {
                Product = new Product
                {
                    IsActive = true
                };
                Title = "添加产品";
            }
            else
            {
                Product = new Product
                {
                    Id = product.Id,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    Specification = product.Specification,
                    Unit = product.Unit,
                    TotalCost = product.TotalCost,
                    SellingPrice = product.SellingPrice,
                    IsActive = product.IsActive,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt
                };
                Title = "编辑产品";
            }

            DataContext = this;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Product.ProductCode))
            {
                MessageBox.Show("请输入产品编码");
                return;
            }

            if (string.IsNullOrWhiteSpace(Product.ProductName))
            {
                MessageBox.Show("请输入产品名称");
                return;
            }

            IsSaved = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsSaved = false;
            Close();
        }
    }
}
