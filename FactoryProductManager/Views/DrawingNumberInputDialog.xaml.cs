using System.Windows;
using System.Windows.Input;

namespace FactoryProductManager.Views
{
    public partial class DrawingNumberInputDialog : Window
    {
        public string DrawingNumber { get; private set; } = "";

        public DrawingNumberInputDialog(string? initialValue = null)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;

            if (!string.IsNullOrEmpty(initialValue))
            {
                DrawingNumberTextBox.Text = initialValue;
            }

            // 输入时自动转大写
            DrawingNumberTextBox.TextChanged += (s, e) =>
            {
                var text = DrawingNumberTextBox.Text;
                if (text != text.ToUpper())
                {
                    var caretIndex = DrawingNumberTextBox.CaretIndex;
                    DrawingNumberTextBox.Text = text.ToUpper();
                    DrawingNumberTextBox.CaretIndex = caretIndex;
                }
            };

            DrawingNumberTextBox.Focus();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingNumber = DrawingNumberTextBox.Text.Trim().ToUpper();
            DialogResult = true;
            Close();
        }

        private void DrawingNumberTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OkButton_Click(sender, e);
                e.Handled = true;
            }
        }
    }
}
