using System.Windows;
using System.Windows.Input;
using FactoryProductManager.Models;

namespace FactoryProductManager.Views
{
    public partial class MaterialDialogWindow : Window
    {
        private readonly MaterialDialogUserControl _userControl;

        public FactoryMaterial Material => _userControl.Material;
        public bool IsSaved => _userControl.IsSaved;

        public MaterialDialogWindow(FactoryMaterial? material = null)
        {
            InitializeComponent();
            _userControl = new MaterialDialogUserControl(material);
            DialogContent.Content = _userControl;
            Title = _userControl.Title;

            _userControl.OkClicked += (_, _) =>
            {
                DialogResult = true;
                Close();
            };

            _userControl.CancelClicked += (_, _) =>
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
