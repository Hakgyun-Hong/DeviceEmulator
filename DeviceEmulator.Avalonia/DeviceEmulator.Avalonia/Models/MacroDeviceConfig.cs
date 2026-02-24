using System.Collections.ObjectModel;

namespace DeviceEmulator.Models
{
    /// <summary>
    /// Configuration for a Macro Scenario device, consisting of sequential steps instead of a single script.
    /// </summary>
    public class MacroDeviceConfig : DeviceConfig
    {
        /// <summary>
        /// The sequential steps in this macro scenario.
        /// </summary>
        public ObservableCollection<MacroStep> Steps { get; set; } = new();

        /// <summary>
        /// Identifies the device type for UI representation.
        /// </summary>
        public override string DeviceType => "Macro";

        public MacroDeviceConfig()
        {
            Name = "Macro Scenario";
            IsHexMode = false; // Often not relevant for macros, but keeping default
        }
    }
}
