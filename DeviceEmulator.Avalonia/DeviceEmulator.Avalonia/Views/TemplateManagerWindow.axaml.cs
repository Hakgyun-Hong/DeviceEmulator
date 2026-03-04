using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DeviceEmulator.Models;

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

            // Wire TreeView selection to ViewModel
            var tree = this.FindControl<TreeView>("TemplateTree");
            if (tree != null)
            {
                tree.SelectionChanged += (s, e) =>
                {
                    if (DataContext is ViewModels.TemplateManagerViewModel vm && tree.SelectedItem is MacroTemplate t)
                    {
                        vm.SelectedTemplate = t;
                    }
                };
            }
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
