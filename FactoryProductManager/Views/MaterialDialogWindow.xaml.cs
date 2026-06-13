using FactoryProductManager.Models;
using System;
using System.Windows;

namespace FactoryProductManager.Views
{
    public partial class MaterialDialogWindow : Window
    {
        private MaterialDialogUserControl? _materialDialogControl;

        public FactoryMaterial Material => _materialDialogControl?.Material ?? new FactoryMaterial();
        public bool IsSaved => _materialDialogControl?.IsSaved ?? false;

        public MaterialDialogWindow(FactoryMaterial? material = null)
        {
            InitializeComponent();

            _materialDialogControl = new MaterialDialogUserControl(material);
            _materialDialogControl.OkClicked += MaterialDialogControl_OkClicked;
            _materialDialogControl.CancelClicked += MaterialDialogControl_CancelClicked;

            DialogContent.Content = _materialDialogControl;
            Title = _materialDialogControl.Title;

            StateChanged += MaterialDialogWindow_StateChanged;
        }

        private void MaterialDialogWindow_StateChanged(object? sender, System.EventArgs e)
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

        private void MaterialDialogControl_OkClicked(object? sender, EventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void MaterialDialogControl_CancelClicked(object? sender, EventArgs e)
        {
            DialogResult = false;
            Close();
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
