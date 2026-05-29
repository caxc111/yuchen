using FactoryProductManager.Models;
using FactoryProductManager.Services;
using System.Windows;

namespace FactoryProductManager.Views
{
    public partial class FactoryDialog : Window
    {
        public Factory Factory { get; set; }
        public bool IsSaved { get; private set; }
        private bool _isEditMode;

        public FactoryDialog(Factory factory = null)
        {
            InitializeComponent();
            if (factory == null)
            {
                Factory = new Factory();
                Title = "添加工厂";
                _isEditMode = false;
            }
            else
            {
                Factory = factory;
                Title = "编辑工厂";
                _isEditMode = true;
            }
            DataContext = this;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Factory.FactoryCode))
            {
                MessageBox.Show("请输入工厂编码");
                return;
            }
            if (string.IsNullOrEmpty(Factory.FactoryName))
            {
                MessageBox.Show("请输入工厂名称");
                return;
            }

            if (!_isEditMode)
            {
                var dbService = new DbService();
                var existingFactory = dbService.GetFactoryByCode(Factory.FactoryCode);
                
                if (existingFactory != null)
                {
                    var result = MessageBox.Show($"该编码 '{Factory.FactoryCode}' 已经存在，是否需要重新编辑？", "编码重复", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        Factory = existingFactory;
                        _isEditMode = true;
                        Title = "编辑工厂";
                        return;
                    }
                    else
                    {
                        return;
                    }
                }
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
