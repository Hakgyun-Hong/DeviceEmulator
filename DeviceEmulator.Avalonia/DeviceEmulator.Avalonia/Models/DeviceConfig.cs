using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization; // Added this using directive

namespace DeviceEmulator.Models
{
    /// <summary>
    /// Base device configuration with common properties for Serial and TCP devices.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")] // Added this attribute
    [JsonDerivedType(typeof(SerialDeviceConfig), typeDiscriminator: "serial")] // Added this attribute
    [JsonDerivedType(typeof(TcpDeviceConfig), typeDiscriminator: "tcp")] // Added this attribute
    public abstract class DeviceConfig : INotifyPropertyChanged
    {
        private string _name; // Initializer removed as per instruction
        private string _script; // Initializer removed as per instruction
        private bool _isDebuggingEnabled; // Initializer removed as per instruction
        private bool _isHexMode = true; // Added this field with initializer

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
        /// If true, data is treated as Hex/Binary.
        /// Script receives 'bytes' variable and return value is treated as bytes (or Hex String).
        /// </summary>
        public bool IsHexMode
        {
            get => _isHexMode;
            set { _isHexMode = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Device type identifier for UI display.
        /// </summary>
        public abstract string DeviceType { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
