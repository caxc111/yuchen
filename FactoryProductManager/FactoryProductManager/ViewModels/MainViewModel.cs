using FactoryProductManager.Views;
using System;
using System.Windows.Input;

namespace FactoryProductManager.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private object _currentView;
        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public ICommand NavigateCommand { get; }

        public MainViewModel()
        {
            NavigateCommand = new RelayCommand<string>(Navigate);
            CurrentView = new FactoryView();
        }

        private void Navigate(string viewName)
        {
            switch (viewName)
            {
                case "Factory":
                    CurrentView = new FactoryView();
                    break;
                case "Product":
                    CurrentView = new ProductView();
                    break;
                case "BOM":
                    CurrentView = new BOMView();
                    break;
                case "Purchase":
                    CurrentView = new PurchaseView();
                    break;
                case "Contract":
                    CurrentView = new ContractView();
                    break;
                case "Report":
                    CurrentView = new ReportView();
                    break;
            }
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute((T)parameter);
        public void Execute(object parameter) => _execute((T)parameter);
        public event EventHandler CanExecuteChanged;
    }
}