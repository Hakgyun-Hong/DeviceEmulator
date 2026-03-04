using System.Collections.ObjectModel;
using DeviceEmulator.Models;

namespace DeviceEmulator.ViewModels
{
    /// <summary>
    /// Groups templates by category for TreeView display.
    /// </summary>
    public class TemplateCategoryGroup
    {
        public string CategoryName { get; set; } = "";
        public ObservableCollection<MacroTemplate> Items { get; set; } = new();

        public TemplateCategoryGroup() { }

        public TemplateCategoryGroup(string category)
        {
            CategoryName = category;
        }
    }
}
