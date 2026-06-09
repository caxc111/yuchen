using MaterialImportTool.Models;
using MaterialImportTool.Services;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.IO;
using System.Windows;

namespace MaterialImportTool.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly DbService _dbService;
        private readonly MainViewModel _mainViewModel;
        private AppSettings _settings = new();

        private string _dbPath = string.Empty;
        public string DbPath
        {
            get => _dbPath;
            set => SetProperty(ref _dbPath, value);
        }

        private string _codePrefix = "S";
        public string CodePrefix
        {
            get => _codePrefix;
            set => SetProperty(ref _codePrefix, value);
        }

        private int _codeLength;
        public int CodeLength
        {
            get => _codeLength;
            set => SetProperty(ref _codeLength, value);
        }

        private string _ocrLanguage = "chi_sim";
        public string OcrLanguage
        {
            get => _ocrLanguage;
            set => SetProperty(ref _ocrLanguage, value);
        }

        public IRelayCommand SaveSettingsCommand { get; }
        public IRelayCommand ResetSettingsCommand { get; }
        public IRelayCommand BackToHomeCommand { get; }
        public IRelayCommand BrowseDbPathCommand { get; }

        public SettingsViewModel(DbService dbService, MainViewModel mainViewModel)
        {
            _dbService = dbService;
            _mainViewModel = mainViewModel;

            SaveSettingsCommand = new RelayCommand(SaveSettings);
            ResetSettingsCommand = new RelayCommand(ResetSettings);
            BackToHomeCommand = new RelayCommand(() => _mainViewModel.ShowHomeCommand.Execute(null));
            BrowseDbPathCommand = new RelayCommand(BrowseDbPath);

            LoadSettings();
        }

        private void BrowseDbPath()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "SQLite数据库 (*.db)|*.db|所有文件 (*.*)|*.*",
                FileName = "FactoryProductDB.db"
            };

            if (!string.IsNullOrWhiteSpace(DbPath))
            {
                var initialDirectory = Path.GetDirectoryName(DbPath);
                if (!string.IsNullOrWhiteSpace(initialDirectory))
                {
                    dialog.InitialDirectory = initialDirectory;
                }

                dialog.FileName = Path.GetFileName(DbPath);
            }

            if (dialog.ShowDialog() == true)
            {
                DbPath = dialog.FileName;
            }
        }

        private void LoadSettings()
        {
            _settings = AppSettings.Load();
            DbPath = _settings.DbPath;
            CodePrefix = _settings.CodePrefix;
            CodeLength = _settings.CodeLength;
            OcrLanguage = _settings.OcrLanguage;
        }

        private void SaveSettings()
        {
            _settings.DbPath = DbPath;
            _settings.CodePrefix = CodePrefix;
            _settings.CodeLength = CodeLength;
            _settings.OcrLanguage = OcrLanguage;
            _settings.Save();

            MessageBox.Show("设置已保存！\n\n注意：数据库路径修改后需要重启程序才能生效。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ResetSettings()
        {
            if (MessageBox.Show("确定要重置所有设置吗？", "确认重置",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _settings.Reset();
                LoadSettings();
                MessageBox.Show("设置已重置！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
