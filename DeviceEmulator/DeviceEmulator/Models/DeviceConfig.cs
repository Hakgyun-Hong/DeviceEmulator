using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DeviceEmulator.Models
{
    /// <summary>
    /// Base device configuration with common properties for Serial and TCP devices.
    /// </summary>
    public abstract class DeviceConfig : INotifyPropertyChanged
    {
        private string _name = "New Device";
        private string _script = "";
        private bool _isDebuggingEnabled = false;

        /// <summary>
        /// Display name for the device in TreeView.
        /// </summary>
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// C# script code for response generation.
        /// </summary>
        public string Script
        {
            get => _script;
            set { _script = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Whether to inject debugging breakpoints into the script.
        /// </summary>
        public bool IsDebuggingEnabled
        {
            get => _isDebuggingEnabled;
            set { _isDebuggingEnabled = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Device type identifier for UI display.
        /// </summary>
        public abstract string DeviceType { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
