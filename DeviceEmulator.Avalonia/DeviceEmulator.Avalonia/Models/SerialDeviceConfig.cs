using System.IO.Ports;

namespace DeviceEmulator.Models
{
    /// <summary>
    /// Configuration for Serial port device emulation.
    /// </summary>
    public class SerialDeviceConfig : DeviceConfig
    {
        private string _portName = "COM1";
        private int _baudRate = 9600;
        private Parity _parity = Parity.None;
        private int _dataBits = 8;
        private StopBits _stopBits = StopBits.One;
        private Handshake _handshake = Handshake.None;
        private bool _appendCR = true;
        private bool _appendLF = true;

        public override string DeviceType => "Serial";

        /// <summary>
        /// COM port name (e.g., COM1, COM2)
        /// </summary>
        public string PortName
        {
            get => _portName;
            set { _portName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Baud rate (e.g., 9600, 115200)
        /// </summary>
        public int BaudRate
        {
            get => _baudRate;
            set { _baudRate = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Parity setting
        /// </summary>
        public Parity Parity
        {
            get => _parity;
            set { _parity = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Data bits (typically 7 or 8)
        /// </summary>
        public int DataBits
        {
            get => _dataBits;
            set { _dataBits = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Stop bits configuration
        /// </summary>
        public StopBits StopBits
        {
            get => _stopBits;
            set { _stopBits = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Hardware/software flow control
        /// </summary>
        public Handshake Handshake
        {
            get => _handshake;
            set { _handshake = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Append Carriage Return (0x0D) to response
        /// </summary>
        public bool AppendCR
        {
            get => _appendCR;
            set { _appendCR = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Append Line Feed (0x0A) to response
        /// </summary>
        public bool AppendLF
        {
            get => _appendLF;
            set { _appendLF = value; OnPropertyChanged(); }
        }

        public SerialDeviceConfig()
        {
            Name = "Serial Device";
            Script = @"// Available variable: message (received string)
// Return the response string

if (message.Contains(""PING""))
    return ""PONG"";

return ""ACK: "" + message;";
        }
    }
}
