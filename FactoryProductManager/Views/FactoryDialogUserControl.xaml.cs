using FactoryProductManager.Models;
using System.Windows.Controls;

namespace FactoryProductManager.Views
{
    public partial class FactoryDialogUserControl : UserControl
    {
        public Factory Factory { get; set; }
        public bool IsSaved { get; private set; }
        public string Title { get; set; }

        public FactoryDialogUserControl(Factory? factory = null)
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

        public event System.EventHandler? OkClicked;
        public event System.EventHandler? CancelClicked;

        private void OkButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Factory.FactoryCode))
            {
                System.Windows.MessageBox.Show("请输入工厂编码");
                return;
            }
            if (string.IsNullOrEmpty(Factory.FactoryName))
            {
                System.Windows.MessageBox.Show("请输入工厂名称");
                return;
            }
            IsSaved = true;
            OkClicked?.Invoke(this, e);
        }

        private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            IsSaved = false;
            CancelClicked?.Invoke(this, e);
        }
    }
}
