using FactoryProductManager.Models;
using FactoryProductManager.Services;
using System.Windows;

namespace FactoryProductManager.Views
{
    public partial class FactoryDialogWindow : Window
    {
        private FactoryDialogUserControl _userControl;
        
        public Factory Factory { get { return _userControl.Factory; } }
        public bool IsSaved { get { return _userControl.IsSaved; } }

        public FactoryDialogWindow(Factory? factory = null)
        {
            InitializeComponent();
            _userControl = new FactoryDialogUserControl(factory);
            DialogContent.Content = _userControl;
            Title = _userControl.Title;
            
            _userControl.OkClicked += (s, e) => 
            {
                DialogResult = true;
                Close();
            };
            
            _userControl.CancelClicked += (s, e) => 
            {
                DialogResult = false;
                Close();
            };

            StateChanged += FactoryDialogWindow_StateChanged;

            WindowPositionService.AddPositionProtection(this);
        }

        private void FactoryDialogWindow_StateChanged(object? sender, System.EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                MaximizeButton.ToolTip = "还原";
            }
            else
            {
                MaximizeButton.ToolTip = "最大化";
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
