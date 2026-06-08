using System.Windows.Input;
using FactoryProductManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace FactoryProductManager.Views
{
    public partial class ProductDialog : Window
    {
        private const string CategoryPlaceholder = "请选择";

        private List<ProductCategory> _allCategories;
        private ProductCategory _selectedLevel1;
        private ProductCategory _selectedLevel2;
        private ProductCategory _selectedLevel3;

        public FactoryProduct Product { get; set; }
        public bool IsSaved { get; private set; }

        public ProductDialog(FactoryProduct product = null)
        {
            InitializeComponent();
            if (product == null)
            {
                Product = new FactoryProduct();
                Title = "添加产品";
            }
            else
            {
                Product = product;
                Title = "编辑产品";
            }
            DataContext = this;

            InitializeCategories();

            if (product != null && !string.IsNullOrEmpty(product.Category))
            {
                SetCurrentCategory(product.Category);
            }
        }

        private void InitializeCategories()
        {
            _allCategories = ProductCategoryData.GetCategories();

            ResetComboBoxItems(CategoryLevel1, _allCategories.Select(category => category.Name));
            ResetComboBoxItems(CategoryLevel2, Enumerable.Empty<string>());
            ResetComboBoxItems(CategoryLevel3, Enumerable.Empty<string>());
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
                return;
            }

            _selectedLevel1 = _allCategories.Find(c => c.Name == selectedName);

            ResetComboBoxItems(CategoryLevel2, _selectedLevel1?.Children.Select(child => child.Name) ?? Enumerable.Empty<string>());
            ResetComboBoxItems(CategoryLevel3, Enumerable.Empty<string>());
            Level3Border.Visibility = Visibility.Collapsed;

            _selectedLevel2 = null;
            _selectedLevel3 = null;
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
                return;
            }

            _selectedLevel2 = _selectedLevel1.Children.Find(c => c.Name == selectedName);

            bool hasLevel3 = _selectedLevel2?.Children != null && _selectedLevel2.Children.Count > 0;

            ResetComboBoxItems(CategoryLevel3, hasLevel3
                ? _selectedLevel2.Children.Select(child => child.Name)
                : Enumerable.Empty<string>());
            Level3Border.Visibility = hasLevel3 ? Visibility.Visible : Visibility.Collapsed;

            _selectedLevel3 = null;
        }

        // ==================== 编辑模式回填 ====================

        private void SetCurrentCategory(string categoryPath)
        {
            var parts = categoryPath.Split(new string[] { " > " }, StringSplitOptions.None);
            if (parts.Length < 2) return;

            _selectedLevel1 = null;
            _selectedLevel2 = null;
            _selectedLevel3 = null;

            foreach (var cat in _allCategories)
            {
                if (cat.Name == parts[0])
                {
                    _selectedLevel1 = cat;

                    if (parts.Length >= 2)
                    {
                        foreach (var child in cat.Children)
                        {
                            if (child.Name == parts[1])
                            {
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
                        }
                    }
                    break;
                }
            }

            CategoryLevel1.SelectedIndex = FindItemIndex(CategoryLevel1, parts[0]);

            ResetComboBoxItems(CategoryLevel2, _selectedLevel1?.Children.Select(child => child.Name) ?? Enumerable.Empty<string>());
            CategoryLevel2.SelectedIndex = FindItemIndex(CategoryLevel2, parts[1]);

            bool hasLevel3 = _selectedLevel2?.Children != null && _selectedLevel2.Children.Count > 0;
            Level3Border.Visibility = hasLevel3 ? Visibility.Visible : Visibility.Collapsed;

            if (hasLevel3 && _selectedLevel3 != null)
            {
                ResetComboBoxItems(CategoryLevel3, _selectedLevel2.Children.Select(child => child.Name));
                CategoryLevel3.SelectedIndex = FindItemIndex(CategoryLevel3, parts[2]);
            }
            else
            {
                ResetComboBoxItems(CategoryLevel3, Enumerable.Empty<string>());
            }
        }

        private int FindItemIndex(ComboBox comboBox, string text)
        {
            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (string.Equals(comboBox.Items[i]?.ToString(), text, StringComparison.Ordinal))
                    return i;
            }
            return 0;
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
            if (string.IsNullOrEmpty(Product.FactoryProductCode))
            {
                MessageBox.Show("请输入工厂物料编码");
                return;
            }
            if (string.IsNullOrEmpty(Product.ProductName))
            {
                MessageBox.Show("请输入产品名称");
                return;
            }
            
            // 保存类别路径
            Product.Category = GetFullCategoryPath();
            
            IsSaved = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsSaved = false;
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
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
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
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
                // 将图片复制到程序目录下的Images文件夹
                string imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                if (!Directory.Exists(imagesDir))
                {
                    Directory.CreateDirectory(imagesDir);
                }

                // 生成唯一文件名
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(filePath);
                string destPath = Path.Combine(imagesDir, fileName);

                // 复制文件
                File.Copy(filePath, destPath, true);

                // 设置图片预览
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(destPath);
                bitmap.EndInit();
                PreviewImage.Source = bitmap;
                ImageHintText.Visibility = Visibility.Collapsed;

                // 保存图片路径到产品对象
                Product.ImageUrl = destPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"图片加载失败: {ex.Message}");
            }
        }
    }
}