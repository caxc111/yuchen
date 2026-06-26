using FactoryProductManager.Models;
using FactoryProductManager.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FactoryProductManager.Views
{
    public partial class AddProductDialog : Window, INotifyPropertyChanged
    {
        private readonly DbService _dbService = new();
        private static readonly HashSet<string> ResidentialBusinessTypes = new(StringComparer.Ordinal)
        {
            "公寓",
            "House"
        };

        private string _businessType = string.Empty;

        public Product Product { get; set; }
        public IReadOnlyList<string> BusinessTypeOptions { get; } = new[] { "公寓", "House", "公区", "酒店", "商业" };
        public string BusinessType
        {
            get => _businessType;
            set
            {
                if (_businessType == value) return;
                _businessType = value;
                Product.BusinessType = value;
                if (!RequiresResidentialHouseType)
                {
                    Product.HouseType = string.Empty;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsResidential));
                OnPropertyChanged(nameof(RequiresResidentialHouseType));
            }
        }
        public bool IsResidential => RequiresResidentialHouseType;
        public bool RequiresResidentialHouseType => ResidentialBusinessTypes.Contains(BusinessType);
        public bool IsSaved { get; private set; }
        public event PropertyChangedEventHandler? PropertyChanged;

        private List<string> _projectCodeSuggestions = new();
        public List<string> ProjectCodeSuggestions
        {
            get => _projectCodeSuggestions;
            set
            {
                _projectCodeSuggestions = value;
                OnPropertyChanged();
            }
        }

        public decimal AveragePrice => Product.Area > 0 ? Product.CostTotalPrice / Product.Area : 0;

        public string HouseTypeDisplay
        {
            get => Product.HouseType;
            set
            {
                if (Product.HouseType != value)
                {
                    Product.HouseType = value;
                    OnPropertyChanged();
                }
            }
        }

        public AddProductDialog()
        {
            LogService.Debug("[AddProductDialog] 构造开始");
            InitializeComponent();
            LogService.Debug("[AddProductDialog] InitializeComponent 完成");

            Product = new Product
            {
                IsActive = true
            };

            BusinessType = BusinessTypeOptions[0];
            DataContext = this;
            WindowPositionService.AddPositionProtection(this);
            LoadProjectCodeSuggestions();
            Loaded += AddProductDialog_Loaded;
            LogService.Debug("[AddProductDialog] 构造完成");
        }

        private void AddProductDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // 延迟一点确保 Template 已应用
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
            {
                try
                {
                    var textBox = ProjectCodeComboBox.Template.FindName("PART_EditableTextBox", ProjectCodeComboBox) as TextBox;
                    if (textBox != null)
                    {
                        textBox.TextChanged += ProjectCodeComboBox_TextChanged;
                        LogService.Debug("[AddProductDialog] 已绑定 TextBox TextChanged 事件");
                    }
                }
                catch (Exception ex)
                {
                    LogService.Error("[AddProductDialog] 绑定 TextBox 事件失败", ex);
                }
            });
        }

        private void LoadProjectCodeSuggestions()
        {
            try
            {
                var allProjectCodes = _dbService.GetAllProjectCodes();
                ProjectCodeSuggestions = allProjectCodes;
            }
            catch (Exception ex)
            {
                LogService.Error("[AddProductDialog] 加载项目代码建议失败", ex);
            }
        }

        private void ProjectCodeComboBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // 支持上下键选择后回车确认
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                var comboBox = sender as ComboBox;
                if (comboBox != null && comboBox.SelectedItem != null)
                {
                    Product.ProjectCode = comboBox.SelectedItem.ToString() ?? string.Empty;
                }
            }
        }

        private void ProjectCodeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem != null)
            {
                Product.ProjectCode = comboBox.SelectedItem.ToString() ?? string.Empty;
            }
        }

        private void ProjectCodeComboBox_DropDownClosed(object sender, EventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox != null && !string.IsNullOrEmpty(comboBox.Text))
            {
                Product.ProjectCode = comboBox.Text;
            }
        }

        private void ProjectCodeComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            string inputText = textBox.Text.Trim();
            if (string.IsNullOrEmpty(inputText))
            {
                LoadProjectCodeSuggestions();
                return;
            }

            // 过滤建议列表
            var filtered = _dbService.GetAllProjectCodes()
                .Where(code => code.StartsWith(inputText, StringComparison.OrdinalIgnoreCase))
                .ToList();
            ProjectCodeSuggestions = filtered;

            // 自动打开下拉列表
            if (filtered.Count > 0 && !ProjectCodeComboBox.IsDropDownOpen)
            {
                ProjectCodeComboBox.IsDropDownOpen = true;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Product.ProductCode))
            {
                MessageBox.Show("请输入产品编码");
                return;
            }

            if (RequiresResidentialHouseType && string.IsNullOrWhiteSpace(Product.HouseType))
            {
                MessageBox.Show("请输入或选择户型");
                return;
            }

            // 保存产品到数据库（包含部件）
            try
            {
                Product.Id = _dbService.AddProduct(Product, _selectedParts);
                LogService.Info($"[AddProductDialog] 已保存产品，ProductId={Product.Id}");

                // 保存物料
                if (_selectedMaterials != null && _selectedMaterials.Count > 0)
                {
                    _dbService.SaveProductMaterialsToLibrary(Product.Id, _selectedMaterials);
                    LogService.Info($"[AddProductDialog] 已保存 {_selectedMaterials.Count} 个物料");
                }

                IsSaved = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                LogService.Error("[AddProductDialog] 保存产品失败", ex);
                MessageBox.Show($"保存产品失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateProductCodeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var businessTypeCode = GetBusinessTypeCode(BusinessType);
                if (string.IsNullOrWhiteSpace(businessTypeCode))
                {
                    MessageBox.Show("请先选择有效业态", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var projectCodeValue = GetProjectNameCode(Product.ProjectCode);
                if (projectCodeValue == null)
                {
                    MessageBox.Show("项目代码只能输入英文或数字，请重新输入", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var houseTypeCode = RequiresResidentialHouseType
                    ? GetHouseTypeCode(Product.HouseType)
                    : "NA";

                if (RequiresResidentialHouseType && string.IsNullOrWhiteSpace(houseTypeCode))
                {
                    MessageBox.Show("请先选择或输入户型", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string codePrefix = $"{projectCodeValue}-{businessTypeCode}-{houseTypeCode}";

                // 弹出输入框让用户输入户型序号
                string? userInput = PromptForSequenceNumber(codePrefix);
                if (string.IsNullOrWhiteSpace(userInput))
                {
                    return; // 用户取消或空输入
                }

                string fullCode = $"{codePrefix}-{userInput}";

                // 检查编码是否重复
                if (_dbService.CheckProductCodeExists(fullCode))
                {
                    MessageBox.Show($"产品编码「{fullCode}」已存在，请重新输入序号", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Product.ProductCode = fullCode;
                OnPropertyChanged(nameof(Product));
            }
            catch (Exception ex)
            {
                LogService.Error("[AddProductDialog] 生成编码失败", ex);
                MessageBox.Show($"生成编码失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string? PromptForSequenceNumber(string codePrefix)
        {
            string? result = null;
            var inputWindow = new Window
            {
                Title = "输入户型序号",
                Width = 400,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.White
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label = new TextBlock
            {
                Text = $"请输入户型序号（将生成为：{codePrefix}-XX）",
                Margin = new Thickness(0, 0, 0, 10),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(label, 0);

            var textBox = new TextBox
            {
                Height = 32,
                FontSize = 14,
                Padding = new Thickness(8, 4, 8, 4),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            // 只允许输入字母或数字
            textBox.PreviewTextInput += (s, e) =>
            {
                e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"^[a-zA-Z0-9]$");
            };
            DataObject.AddPastingHandler(textBox, (s, e) =>
            {
                if (e.DataObject.GetDataPresent(typeof(string)))
                {
                    var text = (string)e.DataObject.GetData(typeof(string));
                    if (!System.Text.RegularExpressions.Regex.IsMatch(text, @"^[a-zA-Z0-9]*$"))
                    {
                        e.CancelCommand();
                    }
                }
                else
                {
                    e.CancelCommand();
                }
            });
            Grid.SetRow(textBox, 1);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 15, 0, 0)
            };
            Grid.SetRow(buttonPanel, 2);

            var okButton = new Button
            {
                Content = "确定",
                Width = 80,
                Height = 32,
                Margin = new Thickness(0, 0, 10, 0)
            };
            var cancelButton = new Button
            {
                Content = "取消",
                Width = 80,
                Height = 32
            };

            okButton.Click += (s, e) =>
            {
                result = textBox.Text.Trim();
                inputWindow.DialogResult = true;
            };
            cancelButton.Click += (s, e) =>
            {
                inputWindow.DialogResult = false;
            };

            textBox.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    result = textBox.Text.Trim();
                    inputWindow.DialogResult = true;
                }
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            grid.Children.Add(label);
            grid.Children.Add(textBox);
            grid.Children.Add(buttonPanel);
            inputWindow.Content = grid;

            textBox.Focus();

            if (inputWindow.ShowDialog() == true)
            {
                return result;
            }
            return null;
        }

        private static string GetBusinessTypeCode(string businessType)
        {
            return businessType switch
            {
                "公寓" => "A",
                "House" => "H",
                "公区" => "P",
                "酒店" => "HT",
                "商业" => "C",
                _ => string.Empty
            };
        }

        private static string GetHouseTypeCode(string houseType)
        {
            return houseType.Trim() switch
            {
                "一房一卫" => "1R1B",
                "两房一卫" => "2R1B",
                "两房两卫" => "2R2B",
                "三房两卫" => "3R2B",
                "四房三卫" => "4R3B",
                _ => houseType.Trim().ToUpperInvariant().Replace(" ", string.Empty)
            };
        }

        private static string? GetProjectNameCode(string projectName)
        {
            if (string.IsNullOrWhiteSpace(projectName))
            {
                return "XX";
            }

            var trimmed = projectName.Trim();
            bool hasChinese = trimmed.Any(c => c >= 0x4E00 && c <= 0x9FFF);
            if (hasChinese)
            {
                return null;
            }

            return trimmed.ToUpperInvariant();
        }

        private void EditPartsButton_Click(object sender, RoutedEventArgs e)
        {
            var partsDialog = new PartManagementDialog(0, true, _selectedParts);
            partsDialog.Owner = this;

            if (partsDialog.ShowDialog() == true)
            {
                _selectedParts = new List<ProductPart>(partsDialog.Parts);
                _selectedParts.AddRange(partsDialog.CustomParts);
                UpdatePartsSummary(partsDialog);
            }
        }

        private void UpdatePartsSummary(PartManagementDialog partsDialog)
        {
            var allParts = partsDialog.Parts.Concat(partsDialog.CustomParts).ToList();
            if (allParts.Count == 0)
            {
                PartsSummaryTextBox.Text = string.Empty;
                Product.HouseType = string.Empty;
                OnPropertyChanged(nameof(HouseTypeDisplay));
                return;
            }

            var summary = string.Join("，", allParts.Select(p => $"{p.PartName}*{p.Quantity}"));
            PartsSummaryTextBox.Text = summary;
            Product.HouseType = CalculateHouseType(allParts);
            OnPropertyChanged(nameof(HouseTypeDisplay));
        }

        private static string CalculateHouseType(List<ProductPart> allParts)
        {
            int bedroomCount = allParts
                .Where(p => p.PartName == "主卧室" || p.PartName == "次卧室")
                .Sum(p => (int)p.Quantity);

            int bathroomCount = allParts
                .Where(p => p.PartName == "主卫生间" || p.PartName == "次卫生间")
                .Sum(p => (int)p.Quantity);

            return $"{bedroomCount}R{bathroomCount}B";
        }

        private List<ProductPart>? _selectedParts;
        private List<SelectedMaterial>? _selectedMaterials;

        private void AddMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            LogService.Debug("[AddProductDialog] AddMaterialButton_Click 开始");

            if (_selectedParts == null || _selectedParts.Count == 0)
            {
                MessageBox.Show("请先选择部件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new AddProductMaterialWindow(0, Product.ProjectCode ?? "", _selectedParts, _selectedMaterials);
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                _selectedMaterials = dialog.SelectedMaterials.ToList();

                // 计算成本合价
                Product.CostTotalPrice = _selectedMaterials.Sum(m => m.TotalPrice);
                OnPropertyChanged(nameof(Product));
                OnPropertyChanged(nameof(AveragePrice));

                MessageBox.Show($"已添加 {_selectedMaterials.Count} 个物料", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ===== 平面图图片处理 =====
        private void FloorPlanDropZone_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void FloorPlanDropZone_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 0) return;

            string filePath = files[0];
            if (IsValidImageFile(filePath))
            {
                LoadFloorPlanImage(filePath);
            }
            else
            {
                MessageBox.Show("请选择有效的图片文件（支持：jpg, jpeg, png, gif, bmp）");
            }
        }

        private void FloorPlanDropZone_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "图片文件 (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp",
                Title = "选择平面图"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadFloorPlanImage(openFileDialog.FileName);
            }
        }

        private bool IsValidImageFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp";
        }

        private void LoadFloorPlanImage(string filePath)
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
                SetFloorPlanPreviewImage(destPath);
                Product.FloorPlan = destPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"图片加载失败: {ex.Message}");
            }
        }

        private void SetFloorPlanPreviewImage(string imagePath)
        {
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            FloorPlanPreviewImage.Source = bitmap;
            FloorPlanHintPanel.Visibility = Visibility.Collapsed;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsSaved = false;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            IsSaved = false;
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
