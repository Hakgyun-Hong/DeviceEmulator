using System.Text;

namespace DeviceEmulator.Models
{
    /// <summary>
    /// Configuration for TCP socket device emulation.
    /// </summary>
    public class TcpDeviceConfig : DeviceConfig
    {
        private int _port = 12345;
        private Encoding _encoding = Encoding.UTF8;
        private string _lineDelimiter = "\r\n";

        public override string DeviceType => "TCP";

        /// <summary>
        /// TCP port to listen on (binds to 127.0.0.1)
        /// </summary>
        public int Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Encoding for message serialization
        /// </summary>
        public Encoding Encoding
        {
            get => _encoding;
            set { _encoding = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Encoding name for UI display and serialization
        /// </summary>
        public string EncodingName
        {
            get => _encoding.WebName;
            set
            {
                _encoding = Encoding.GetEncoding(value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(Encoding));
            }
        }

        /// <summary>
        /// Line delimiter for message framing
        /// </summary>
        public string LineDelimiter
        {
            get => _lineDelimiter;
            set { _lineDelimiter = value; OnPropertyChanged(); }
        }

        public TcpDeviceConfig()
        {
            Name = "TCP Device";
            Script = @"// Available variable: message (received string)
// Return the response string

return ""ECHO: "" + message;";
        }
    }
}
