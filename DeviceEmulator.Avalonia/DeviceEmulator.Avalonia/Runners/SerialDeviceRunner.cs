using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceEmulator.Models;
using DeviceEmulator.Scripting;

namespace DeviceEmulator.Runners
{
    /// <summary>
    /// Handles serial port communication for device emulation.
    /// Runs on a dedicated thread and responds to incoming messages using compiled scripts.
    /// </summary>
    public class SerialDeviceRunner : IDeviceRunner
    {
        private readonly SerialDeviceConfig _config;
        private SerialPort _serialPort;
        private DeviceScript _script;
        private CancellationTokenSource _cts;
        private bool _isRunning;

        public DeviceConfig Config => _config;
        public bool IsRunning => _isRunning;

        public event Action<string> MessageReceived;
        public event Action<string> MessageSent;
        public event Action<Exception> ErrorOccurred;
        public event Action<bool> RunningStateChanged;
        public event Action<string> LogMessage;

        public SerialDeviceRunner(SerialDeviceConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _script = new DeviceScript();
        }

        public Task StartAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    // Configure serial port
                    _serialPort = new SerialPort
                    {
                        PortName = _config.PortName,
                        BaudRate = _config.BaudRate,
                        Parity = _config.Parity,
                        DataBits = _config.DataBits,
                        StopBits = _config.StopBits,
                        Handshake = _config.Handshake,
                        Encoding = Encoding.UTF8,
                        ReadTimeout = 1000,
                        WriteTimeout = 1000
                    };

                    _serialPort.DataReceived += OnDataReceived;
                    _serialPort.ErrorReceived += OnErrorReceived;

                    _serialPort.Open();
                    _cts = new CancellationTokenSource();

                    _isRunning = true;
                    RunningStateChanged?.Invoke(true);
                    LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] Serial port {_config.PortName} opened");

                    // Keep thread alive while running
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(ex);
                    LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] ERROR: {ex.Message}");
                }
                finally
                {
                    CleanupSerialPort();
                    _isRunning = false;
                    RunningStateChanged?.Invoke(false);
                }
            });
        }

        public Task StopAsync()
        {
            return Task.Run(() =>
            {
                _cts?.Cancel();
                Thread.Sleep(200); // Allow graceful shutdown
                CleanupSerialPort();
                LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] Serial port {_config.PortName} closed");
            });
        }

        public bool UpdateScript(string script, bool enableDebugging)
        {
            _script = new DeviceScript { EnableDebugging = enableDebugging };
            return _script.Compile(script);
        }

        public string GetCompilationErrors()
        {
            return _script?.GetErrorMessages() ?? string.Empty;
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_serialPort == null || !_serialPort.IsOpen) return;

                // Small delay to receive complete message
                Thread.Sleep(50);

                var buffer = new byte[_serialPort.BytesToRead];
                _serialPort.Read(buffer, 0, buffer.Length);
                
                if (buffer.Length == 0) return;

                string message;
                if (_config.IsHexMode)
                {
                    message = BitConverter.ToString(buffer).Replace("-", " ");
                }
                else
                {
                    message = Encoding.UTF8.GetString(buffer).Trim();
                }

                if (string.IsNullOrEmpty(message) && !_config.IsHexMode) return;

                LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] RECEIVED: {message}");
                MessageReceived?.Invoke(message);

                // Generate and send response
                if (_script.IsCompiled)
                {
                    var response = _script.GetResponse(message, buffer);
                    if (response != null)
                    {
                        SendResponse(response);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
                LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] ERROR: {ex.Message}");
            }
        }

        private void SendResponse(object responseObj)
        {
            try
            {
                if (_serialPort == null || !_serialPort.IsOpen) return;

                byte[] bytesToSend = null;
                string logText = "";

                if (responseObj is byte[] rawBytes)
                {
                    bytesToSend = rawBytes;
                    logText = BitConverter.ToString(rawBytes).Replace("-", " ");
                }
                else if (responseObj is string strResp)
                {
                    if (_config.IsHexMode)
                    {
                        try 
                        {
                            var hex = strResp.Replace(" ", "").Replace("-", "");
                            if (hex.Length % 2 != 0) hex = "0" + hex;
                            bytesToSend = new byte[hex.Length / 2];
                            for (int i = 0; i < bytesToSend.Length; i++) 
                                bytesToSend[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                            logText = strResp;
                        }
                        catch
                        {
                            LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] INVALID HEX: {strResp}");
                            return;
                        }
                    }
                    else
                    {
                        var formattedResponse = strResp;
                        if (_config.AppendCR) formattedResponse += "\r";
                        if (_config.AppendLF) formattedResponse += "\n";
                        bytesToSend = Encoding.UTF8.GetBytes(formattedResponse);
                        logText = formattedResponse;
                    }
                }

                if (bytesToSend != null && bytesToSend.Length > 0)
                {
                    _serialPort.Write(bytesToSend, 0, bytesToSend.Length);
                    LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] SENT: {logText}");
                    MessageSent?.Invoke(logText);
                }
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
                LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] SEND ERROR: {ex.Message}");
            }
        }

        private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] Serial error: {e.EventType}");
        }

        private void CleanupSerialPort()
        {
            try
            {
                if (_serialPort != null)
                {
                    _serialPort.DataReceived -= OnDataReceived;
                    _serialPort.ErrorReceived -= OnErrorReceived;

                    if (_serialPort.IsOpen)
                    {
                        _serialPort.DiscardInBuffer();
                        _serialPort.DiscardOutBuffer();
                        _serialPort.Close();
                    }
                    _serialPort.Dispose();
                    _serialPort = null;
                }
            }
            catch { /* Ignore cleanup errors */ }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            CleanupSerialPort();
            _cts?.Dispose();
        }
    }
}
