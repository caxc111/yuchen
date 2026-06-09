using FactoryProductManager.Models;
using System.Windows;

namespace FactoryProductManager.Views
{
    public partial class FactoryDialog : Window
    {
        public Factory Factory { get; set; }
        public bool IsSaved { get; private set; }

        public FactoryDialog(Factory? factory = null)
        {
            InitializeComponent();
            if (factory == null)
            {
                Factory = new Factory();
                Title = "添加工厂";
            }
            else
            {
                Factory = factory;
                Title = "编辑工厂";
            }
            DataContext = this;
            
            // 使用统一的工厂类型数据源，确保与物料管理页面一致
            foreach (var type in ProductCategoryData.GetFactoryTypes())
            {
                FactoryTypeComboBox.Items.Add(type);
            }
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
