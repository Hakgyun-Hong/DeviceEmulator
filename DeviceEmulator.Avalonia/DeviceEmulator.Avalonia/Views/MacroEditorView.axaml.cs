using System;
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

        /// <summary>
        /// When a compact step row is clicked, set it as the SelectedStep for the Properties panel.
        /// </summary>
        private void OnStepTapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            if (sender is Avalonia.Controls.Border border && border.DataContext is Models.MacroStep step)
            {
                if (DataContext is ViewModels.DeviceTreeItemViewModel vm)
                {
                    vm.SelectedStep = step;
                }
            }
        }

        /// <summary>
        /// When the user opens the suggestion ComboBox dropdown, refresh the suggestions list
        /// from PlatformAutomation (e.g. open windows, UI elements, processes).
        /// </summary>
        private void OnSuggestionDropDownOpened(object? sender, EventArgs e)
        {
            if (sender is ComboBox combo && combo.DataContext is Models.MacroArgumentViewModel argVm)
            {
                argVm.RefreshSuggestions();
            }
        }
    }
}
