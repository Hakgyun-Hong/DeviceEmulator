using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using DeviceEmulator.Models;
using DeviceEmulator.Services;

namespace DeviceEmulator.ViewModels
{
    public class TemplateManagerViewModel : INotifyPropertyChanged
    {
        private MacroTemplate? _selectedTemplate;

        public ObservableCollection<MacroTemplate> Templates { get; }

        /// <summary>
        /// Templates grouped by Category for TreeView display.
        /// </summary>
        public ObservableCollection<TemplateCategoryGroup> GroupedTemplates { get; } = new();

        public MacroTemplate? SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                _selectedTemplate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanEditTemplate));
                OnPropertyChanged(nameof(CanDeleteTemplate));
            }
        }

        /// <summary>
        /// Built-in templates are read-only.
        /// </summary>
        public bool CanEditTemplate => SelectedTemplate != null && !SelectedTemplate.IsBuiltIn;
        public bool CanDeleteTemplate => SelectedTemplate != null && !SelectedTemplate.IsBuiltIn;

        public TemplateManagerViewModel()
        {
            Templates = new ObservableCollection<MacroTemplate>(MacroTemplateService.Load());
            RebuildGroups();
            if (Templates.Count > 0)
                SelectedTemplate = Templates[0];
        }

        private void RebuildGroups()
        {
            GroupedTemplates.Clear();
            var groups = Templates
                .GroupBy(t => t.Category ?? "General")
                .OrderBy(g => GetCategoryOrder(g.Key));

            foreach (var g in groups)
            {
                var group = new TemplateCategoryGroup(g.Key);
                foreach (var t in g) group.Items.Add(t);
                GroupedTemplates.Add(group);
            }
        }

        private static int GetCategoryOrder(string category)
        {
            return category switch
            {
                "General" => 0,
                "Window" => 1,
                "Input" => 2,
                "UI Automation" => 3,
                "Process" => 4,
                "Clipboard" => 5,
                "Screenshot" => 6,
                _ => 99
            };
        }

        public void AddTemplate()
        {
            var newTemplate = new MacroTemplate { Name = "New Custom Template", Category = "General" };
            Templates.Add(newTemplate);
            RebuildGroups();
            SelectedTemplate = newTemplate;
        }

        public void RemoveTemplate()
        {
            if (SelectedTemplate != null && !SelectedTemplate.IsBuiltIn)
            {
                Templates.Remove(SelectedTemplate);
                RebuildGroups();
                SelectedTemplate = Templates.FirstOrDefault();
            }
        }

        public void AddArgument()
        {
            if (SelectedTemplate != null)
            {
                SelectedTemplate.RequiredArguments.Add("NewArg");
            }
        }

        public void RemoveArgument(object arg)
        {
            if (SelectedTemplate != null && arg is string strArg)
            {
                SelectedTemplate.RequiredArguments.Remove(strArg);
            }
        }

        public void Save()
        {
            MacroTemplateService.Save(Templates);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
