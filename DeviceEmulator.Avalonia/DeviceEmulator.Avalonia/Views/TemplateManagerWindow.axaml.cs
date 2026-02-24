using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace DeviceEmulator.Views
{
    public partial class TemplateManagerWindow : Window
    {
        public TemplateManagerWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnSaveAndClose(object? sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.TemplateManagerViewModel vm)
            {
                vm.Save();
            }
            Close();
        }
    }
}
