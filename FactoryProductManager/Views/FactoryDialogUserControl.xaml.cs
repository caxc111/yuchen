using FactoryProductManager.Models;
using FactoryProductManager.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FactoryProductManager.Views
{
    public partial class FactoryDialogUserControl : UserControl
    {
        private readonly DbService _dbService = new DbService(DatabaseType.GlobalMaterial);
        public Factory Factory { get; set; }
        public bool IsSaved { get; private set; }
        public string Title { get; set; }
        
        // 复选框与类别的映射
        private readonly Dictionary<CheckBox, string> _checkBoxToType = new();

        public FactoryDialogUserControl(Factory? factory = null)
        {
            InitializeComponent();
            if (factory == null)
            {
                Factory = new Factory();
                Factory.FactoryCode = _dbService.GetNextFactoryCode();
                Title = "添加工厂";
            }
            else
            {
                Factory = factory;
                Title = "编辑工厂";
            }
            DataContext = this;

            Loaded += FactoryDialogUserControl_Loaded;
        }

        private void FactoryDialogUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= FactoryDialogUserControl_Loaded;
            BuildFactoryTypeCheckboxes();
        }

        private void BuildFactoryTypeCheckboxes()
        {
            _checkBoxToType.Clear();
            FactoryTypesPanel.Items.Clear();

            var allTypes = ProductCategoryData.GetFactoryTypes();
            var selectedTypes = Factory.FactoryTypes;

            foreach (var type in allTypes)
            {
                var checkBox = new CheckBox
                {
                    Content = type,
                    Style = (Style)FindResource("FactoryTypeCheckBoxStyle"),
                    Tag = type
                };

                // 设置选中状态
                if (selectedTypes.Contains(type))
                {
                    checkBox.IsChecked = true;
                }

                _checkBoxToType[checkBox] = type;
                FactoryTypesPanel.Items.Add(checkBox);
            }
        }

        private List<string> GetSelectedTypes()
        {
            return _checkBoxToType
                .Where(kvp => kvp.Key.IsChecked == true)
                .Select(kvp => kvp.Value)
                .ToList();
        }

        public event System.EventHandler? OkClicked;
        public event System.EventHandler? CancelClicked;

        private void OkButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Factory.FactoryCode))
            {
                MessageBox.Show("请输入工厂编码", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(Factory.FactoryName))
            {
                MessageBox.Show("请输入工厂名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 验证至少选择一个类别
            var selectedTypes = GetSelectedTypes();
            if (selectedTypes.Count == 0)
            {
                MessageBox.Show("请至少选择一个工厂类型", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 保存选中的类别到 Factory.FactoryType
            Factory.FactoryTypes = selectedTypes;

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
