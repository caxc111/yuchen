using MaterialImportTool.Models;
using MaterialImportTool.Services;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.IO;
using System.Windows.Controls;

namespace MaterialImportTool.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private UserControl _currentView = null!;
        public UserControl CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        private DbService _dbService = null!;
        public DbService DbService => _dbService;

        public IRelayCommand ShowHomeCommand { get; }
        public IRelayCommand ShowFactoryCommand { get; }
        public IRelayCommand ShowProductCommand { get; }
        public IRelayCommand ShowSettingsCommand { get; }

        public MainViewModel()
        {
            InitializeDbService();

            ShowHomeCommand = new RelayCommand(ShowHome);
            ShowFactoryCommand = new RelayCommand(ShowFactory);
            ShowProductCommand = new RelayCommand(ShowProduct);
            ShowSettingsCommand = new RelayCommand(ShowSettings);

            ShowHome();
        }

        private void InitializeDbService()
        {
            var settings = AppSettings.Load();
            string dbPath = settings.DbPath;

            if (string.IsNullOrWhiteSpace(dbPath))
            {
                dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FactoryProductDB.db");
            }

            _dbService = new DbService(dbPath);
        }

        private void ShowHome()
        {
            CurrentView = new Views.HomeView(this);
        }

        private void ShowFactory()
        {
            CurrentView = new Views.FactoryView(this);
        }

        private void ShowProduct()
        {
            CurrentView = new Views.ProductView(this);
        }

        private void ShowSettings()
        {
            CurrentView = new Views.SettingsView(this);
        }
    }
}
