using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace DeviceEmulator.Models
{
    /// <summary>
    /// Base device configuration with common properties for Serial and TCP devices.
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(SerialDeviceConfig), typeDiscriminator: "serial")]
    [JsonDerivedType(typeof(TcpDeviceConfig), typeDiscriminator: "tcp")]
    public abstract class DeviceConfig : INotifyPropertyChanged
    {
        private string _name = "New Device";
        private string _script = @"// Available variable: message (received string)
// Return the response string
return ""ECHO: "" + message;";
        private bool _isDebuggingEnabled = false;
        private bool _isHexMode = true;

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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
