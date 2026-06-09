using FactoryProductManager.Models;
using System.Windows;

namespace FactoryProductManager.Views
{
    public partial class MaterialDetailsWindow : Window
    {
        public MaterialDetailsWindow(FactoryMaterial material)
        {
            InitializeComponent();
            DataContext = material;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
