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

        public MacroTemplate? SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                _selectedTemplate = value;
                OnPropertyChanged();
            }
        }

        public TemplateManagerViewModel()
        {
            Templates = new ObservableCollection<MacroTemplate>(MacroTemplateService.Load());
            if (Templates.Count > 0)
                SelectedTemplate = Templates[0];
        }

        public void AddTemplate()
        {
            var newTemplate = new MacroTemplate { Name = "New Custom Template" };
            Templates.Add(newTemplate);
            SelectedTemplate = newTemplate;
        }

        public void RemoveTemplate()
        {
            if (SelectedTemplate != null)
            {
                Templates.Remove(SelectedTemplate);
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
