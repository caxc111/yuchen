using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FactoryProductManager.Models;
using FactoryProductManager.Services;
using Microsoft.Win32;

namespace FactoryProductManager.Views
{
    public partial class MaterialDialogUserControl : UserControl
    {
        private const string CategoryPlaceholder = "请选择";
        private const string FactoryPlaceholder = "请选择工厂";
        private const string FactoryEmptyPlaceholder = "暂无匹配工厂";
        private const string BrandPlaceholder = "请选择品牌";
        private const string BrandEmptyPlaceholder = "暂无品牌";

        private readonly DbService _dbService = new DbService();
        private readonly Brush _factoryDefaultBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC"));
        private readonly Brush _factoryWarningBorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F2B366"));
        private List<Factory> _allFactories = new();
        private Factory? _selectedFactory;

        private List<ProductCategory> _allCategories = new();
        private ProductCategory? _selectedLevel1;
        private ProductCategory? _selectedLevel2;
        private ProductCategory? _selectedLevel3;

        public FactoryMaterial Material { get; }
        public bool IsSaved { get; private set; }
        public string Title { get; }

        public event EventHandler? OkClicked;
        public event EventHandler? CancelClicked;

        public MaterialDialogUserControl(FactoryMaterial? material = null)
        {
            InitializeComponent();

            if (material == null)
            {
                Material = new FactoryMaterial();
                Title = "添加物料";
            }
            else
            {
                Material = material;
                Title = "编辑物料";
            }

            DataContext = this;

            InitializeFactories();
            InitializeCategories();

            if (!string.IsNullOrWhiteSpace(Material.Brand))
            {
                SelectBrand(Material.Brand);
            }

            if (!string.IsNullOrEmpty(Material.ImageUrl) && File.Exists(Material.ImageUrl))
            {
                SetPreviewImage(Material.ImageUrl);
            }

            if (!string.IsNullOrEmpty(Material.Category))
            {
                SetCurrentCategory(Material.Category);
            }
        }

        private void InitializeFactories()
        {
            _allFactories = _dbService.GetFactories();
            ResetFactoryComboBoxItems(Enumerable.Empty<Factory>());
        }

        private void InitializeCategories()
        {
            _allCategories = ProductCategoryData.GetCategories();

            ResetComboBoxItems(CategoryLevel1, _allCategories.Select(category => category.Name));
            ResetComboBoxItems(CategoryLevel2, Enumerable.Empty<string>());
            ResetComboBoxItems(CategoryLevel3, Enumerable.Empty<string>());
            Level3Border.Visibility = Visibility.Collapsed;
        }

        private void ResetFactoryComboBoxItems(IEnumerable<Factory> factories)
        {
            var factoryList = factories.ToList();
            var factoryOptions = new List<FactoryOption>
            {
                new FactoryOption
                {
                    DisplayName = factoryList.Count == 0 ? FactoryEmptyPlaceholder : FactoryPlaceholder,
                    IsSelectable = false
                }
            };

            factoryOptions.AddRange(factoryList.Select(factory => new FactoryOption
            {
                Id = factory.Id,
                FactoryCode = factory.FactoryCode,
                DisplayName = factory.FactoryName,
                IsSelectable = true
            }));

            FactoryNameComboBox.ItemsSource = factoryOptions;
            FactoryNameComboBox.SelectedIndex = 0;
            FactoryNameBorder.BorderBrush = factoryList.Count == 0 ? _factoryWarningBorderBrush : _factoryDefaultBorderBrush;
            Material.FactoryId = null;
            Material.FactoryName = string.Empty;

            ResetBrandComboBoxItems(factoryList);
        }

        private void ResetBrandComboBoxItems(IEnumerable<Factory> factories)
        {
            var brandOptions = factories
                .SelectMany(factory => (factory.Brand ?? string.Empty)
                    .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(brand => brand.Trim()))
                .Where(brand => !string.IsNullOrWhiteSpace(brand))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(brand => brand, StringComparer.Ordinal)
                .ToList();

            BrandComboBox.ItemsSource = new[]
            {
                brandOptions.Count == 0 ? BrandEmptyPlaceholder : BrandPlaceholder
            }.Concat(brandOptions).ToList();
            BrandComboBox.SelectedIndex = 0;
            Material.Brand = string.Empty;
        }

        private void UpdateFactoryOptionsByLevel1()
        {
            if (_selectedLevel1 == null)
            {
                ResetFactoryComboBoxItems(Enumerable.Empty<Factory>());
                return;
            }

            var filteredFactories = _allFactories
                .Where(factory => string.Equals(factory.FactoryType, _selectedLevel1.Name, StringComparison.Ordinal))
                .OrderBy(factory => factory.FactoryCode)
                .ToList();

            ResetFactoryComboBoxItems(filteredFactories);

            if (!string.IsNullOrWhiteSpace(Material.FactoryName))
            {
                SelectFactoryByName(Material.FactoryName);
            }
        }

        private void SelectFactoryByName(string factoryName)
        {
            if (string.IsNullOrWhiteSpace(factoryName)) return;

            for (int i = 0; i < FactoryNameComboBox.Items.Count; i++)
            {
                if (FactoryNameComboBox.Items[i] is FactoryOption option &&
                    string.Equals(option.DisplayName, factoryName, StringComparison.Ordinal))
                {
                    _selectedFactory = _allFactories.FirstOrDefault(factory => factory.Id == option.Id);
                    FactoryNameComboBox.SelectedIndex = i;
                    return;
                }
            }
        }

        private void SelectBrand(string brand)
        {
            if (string.IsNullOrWhiteSpace(brand) || BrandComboBox.Items == null)
            {
                return;
            }

            int index = FindItemIndex(BrandComboBox, brand);
            if (index > 0)
            {
                BrandComboBox.SelectedIndex = index;
            }
        }

        private void ResetComboBoxItems(ComboBox comboBox, IEnumerable<string> items)
        {
            comboBox.ItemsSource = new[] { CategoryPlaceholder }.Concat(items).ToList();
            comboBox.SelectedIndex = 0;
        }

        private void CategoryLevel1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryLevel1.SelectedItem is not string selectedName) return;

            if (selectedName == CategoryPlaceholder)
            {
                ResetComboBoxItems(CategoryLevel2, Enumerable.Empty<string>());
                ResetComboBoxItems(CategoryLevel3, Enumerable.Empty<string>());
                Level3Border.Visibility = Visibility.Collapsed;

                _selectedLevel1 = null;
                _selectedLevel2 = null;
                _selectedLevel3 = null;
                SetMaterialNameFromCategory(string.Empty);
                UpdateFactoryOptionsByLevel1();
                return;
            }

            _selectedLevel1 = _allCategories.Find(c => c.Name == selectedName);

            ResetComboBoxItems(CategoryLevel2, _selectedLevel1?.Children.Select(child => child.Name) ?? Enumerable.Empty<string>());
            ResetComboBoxItems(CategoryLevel3, Enumerable.Empty<string>());
            Level3Border.Visibility = Visibility.Collapsed;

            _selectedLevel2 = null;
            _selectedLevel3 = null;
            SetMaterialNameFromCategory(string.Empty);
            UpdateFactoryOptionsByLevel1();
        }

        private void CategoryLevel2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryLevel2.SelectedItem is not string selectedName) return;

            if (selectedName == CategoryPlaceholder || _selectedLevel1 == null)
            {
                ResetComboBoxItems(CategoryLevel3, Enumerable.Empty<string>());
                Level3Border.Visibility = Visibility.Collapsed;

                _selectedLevel2 = null;
                _selectedLevel3 = null;
                SetMaterialNameFromCategory(string.Empty);
                return;
            }

            _selectedLevel2 = _selectedLevel1.Children.Find(c => c.Name == selectedName);

            bool hasLevel3 = _selectedLevel2 is { Children.Count: > 0 };

            ResetComboBoxItems(CategoryLevel3, hasLevel3 && _selectedLevel2 != null
                ? _selectedLevel2.Children.Select(child => child.Name)
                : Enumerable.Empty<string>());
            Level3Border.Visibility = hasLevel3 ? Visibility.Visible : Visibility.Collapsed;

            _selectedLevel3 = null;
            SetMaterialNameFromCategory(hasLevel3 ? string.Empty : selectedName);
        }

        private void CategoryLevel3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryLevel3.SelectedItem is not string selectedName)
            {
                return;
            }

            if (selectedName == CategoryPlaceholder)
            {
                _selectedLevel3 = null;
                SetMaterialNameFromCategory(string.Empty);
                return;
            }

            _selectedLevel3 = _selectedLevel2?.Children.Find(c => c.Name == selectedName);
            SetMaterialNameFromCategory(selectedName);
        }

        private void FactoryNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FactoryNameComboBox.SelectedItem is not FactoryOption option || !option.IsSelectable || option.Id == null)
            {
                _selectedFactory = null;
                Material.FactoryId = null;
                Material.FactoryName = string.Empty;
                ResetBrandComboBoxItems(Enumerable.Empty<Factory>());
                return;
            }

            _selectedFactory = _allFactories.FirstOrDefault(factory => factory.Id == option.Id);
            Material.FactoryId = option.Id;
            Material.FactoryName = option.DisplayName;

            if (_selectedFactory != null)
            {
                ResetBrandComboBoxItems(new[] { _selectedFactory });
                SelectBrand(Material.Brand);
            }
        }

        private void BrandComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BrandComboBox.SelectedItem is not string selectedBrand ||
                selectedBrand == BrandPlaceholder ||
                selectedBrand == BrandEmptyPlaceholder)
            {
                Material.Brand = string.Empty;
                return;
            }

            Material.Brand = selectedBrand;
        }

        private void SetCurrentCategory(string categoryPath)
        {
            var parts = categoryPath.Split(new[] { " > " }, StringSplitOptions.None);
            if (parts.Length < 2) return;

            _selectedLevel1 = null;
            _selectedLevel2 = null;
            _selectedLevel3 = null;

            foreach (var category in _allCategories)
            {
                if (category.Name != parts[0]) continue;

                _selectedLevel1 = category;

                foreach (var child in category.Children)
                {
                    if (child.Name != parts[1]) continue;

                    _selectedLevel2 = child;

                    if (parts.Length >= 3)
                    {
                        foreach (var grandchild in child.Children)
                        {
                            if (grandchild.Name == parts[2])
                            {
                                _selectedLevel3 = grandchild;
                                break;
                            }
                        }
                    }

                    break;
                }

                break;
            }

            CategoryLevel1.SelectedIndex = FindItemIndex(CategoryLevel1, parts[0]);
            ResetComboBoxItems(CategoryLevel2, _selectedLevel1?.Children.Select(child => child.Name) ?? Enumerable.Empty<string>());
            CategoryLevel2.SelectedIndex = FindItemIndex(CategoryLevel2, parts[1]);
            UpdateFactoryOptionsByLevel1();

            bool hasLevel3 = _selectedLevel2?.Children != null && _selectedLevel2.Children.Count > 0;
            Level3Border.Visibility = hasLevel3 ? Visibility.Visible : Visibility.Collapsed;

            if (hasLevel3 && _selectedLevel2 != null && parts.Length >= 3)
            {
                ResetComboBoxItems(CategoryLevel3, _selectedLevel2.Children.Select(child => child.Name));
                CategoryLevel3.SelectedIndex = FindItemIndex(CategoryLevel3, parts[2]);
            }
            else
            {
                ResetComboBoxItems(CategoryLevel3, Enumerable.Empty<string>());
            }

            if (!string.IsNullOrWhiteSpace(Material.MaterialName))
            {
                return;
            }

            if (hasLevel3 && _selectedLevel3 != null)
            {
                SetMaterialNameFromCategory(_selectedLevel3.Name);
            }
            else if (_selectedLevel2 != null)
            {
                SetMaterialNameFromCategory(_selectedLevel2.Name);
            }
        }

        private int FindItemIndex(ComboBox comboBox, string text)
        {
            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (string.Equals(comboBox.Items[i]?.ToString(), text, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return 0;
        }

        private void SetMaterialNameFromCategory(string materialName)
        {
            Material.MaterialName = materialName;
            if (MaterialNameTextBox != null)
            {
                MaterialNameTextBox.Text = materialName;
            }
        }

        private string GetFullCategoryPath()
        {
            var parts = new List<string>();
            if (_selectedLevel1 != null) parts.Add(_selectedLevel1.Name);
            if (_selectedLevel2 != null) parts.Add(_selectedLevel2.Name);
            if (_selectedLevel3 != null && CategoryLevel3.SelectedItem is string selectedName && selectedName != CategoryPlaceholder)
            {
                parts.Add(selectedName);
            }

            return string.Join(" > ", parts);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Material.FactoryMaterialCode))
            {
                MessageBox.Show("请输入工厂物料编码");
                return;
            }

            if (string.IsNullOrEmpty(Material.MaterialName))
            {
                MessageBox.Show("请输入物料名称");
                return;
            }

            Material.Category = GetFullCategoryPath();
            IsSaved = true;
            OkClicked?.Invoke(this, EventArgs.Empty);
        }

        private void GenerateMyMaterialCodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFactory == null)
            {
                MessageBox.Show("请先选择工厂后再生成编码", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!MaterialCodeGenerator.TryGenerate(_dbService, Material, _selectedFactory, _selectedLevel1, _selectedLevel2, _selectedLevel3, out string code, out string errorMessage))
            {
                MessageBox.Show(errorMessage, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Material.MyMaterialCode = code;
            if (MyMaterialCodeTextBox != null)
            {
                MyMaterialCodeTextBox.Text = code;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsSaved = false;
            CancelClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ImageDropZone_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void ImageDropZone_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 0) return;

            string filePath = files[0];
            if (IsValidImageFile(filePath))
            {
                LoadImage(filePath);
            }
            else
            {
                MessageBox.Show("请选择有效的图片文件（支持：jpg, jpeg, png, gif, bmp）");
            }
        }

        private void ImageDropZone_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "图片文件 (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp",
                Title = "选择图片"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadImage(openFileDialog.FileName);
            }
        }

        private bool IsValidImageFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp";
        }

        private void LoadImage(string filePath)
        {
            try
            {
                string imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                if (!Directory.Exists(imagesDir))
                {
                    Directory.CreateDirectory(imagesDir);
                }

                string fileName = Guid.NewGuid() + Path.GetExtension(filePath);
                string destPath = Path.Combine(imagesDir, fileName);

                File.Copy(filePath, destPath, true);
                SetPreviewImage(destPath);
                Material.ImageUrl = destPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"图片加载失败: {ex.Message}");
            }
        }

        private class FactoryOption
        {
            public int? Id { get; set; }
            public string FactoryCode { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public bool IsSelectable { get; set; }
        }

        private void SetPreviewImage(string imagePath)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            PreviewImage.Source = bitmap;
            ImageHintText.Visibility = Visibility.Collapsed;
            ImagePickerButton.Visibility = Visibility.Collapsed;
        }
    }
}
