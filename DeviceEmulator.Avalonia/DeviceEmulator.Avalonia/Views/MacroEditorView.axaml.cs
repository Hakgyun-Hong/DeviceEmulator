using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DeviceEmulator.Views
{
    public partial class MacroEditorView : UserControl
    {
        public MacroEditorView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void OnManageTemplates(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var window = new TemplateManagerWindow
            {
                DataContext = new ViewModels.TemplateManagerViewModel()
            };

            var topLevel = TopLevel.GetTopLevel(this) as Window;
            if (topLevel != null)
            {
                await window.ShowDialog(topLevel);
                
                // After closing, we might want to refresh AvailableTemplates in DeviceTreeItemViewModel.
                if (DataContext is ViewModels.DeviceTreeItemViewModel vm)
                {
                    vm.AvailableTemplates.Clear();
                    foreach (var t in Services.MacroTemplateService.Load())
                    {
                        vm.AvailableTemplates.Add(t);
                    }
                }
            }
        }
    }
}
