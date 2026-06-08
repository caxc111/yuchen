using FactoryProductManager.Models;
using System.Windows;
using System.Windows.Input;

namespace FactoryProductManager.Views
{
    public partial class FactoryDialogWindow : Window
    {
        private FactoryDialogUserControl _userControl;
        
        public Factory Factory { get { return _userControl.Factory; } }
        public bool IsSaved { get { return _userControl.IsSaved; } }

        public FactoryDialogWindow(Factory factory = null)
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
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
