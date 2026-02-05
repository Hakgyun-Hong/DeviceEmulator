using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceEmulator.Models;
using DeviceEmulator.Scripting;

namespace DeviceEmulator.Runners
{
    /// <summary>
    /// Handles TCP socket communication for device emulation.
    /// Listens on 127.0.0.1:[port] and responds to incoming messages using compiled scripts.
    /// </summary>
    public class TcpDeviceRunner : IDeviceRunner
    {
        private readonly TcpDeviceConfig _config;
        private TcpListener _listener;
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

        public TcpDeviceRunner(TcpDeviceConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _script = new DeviceScript();
        }

        public Task StartAsync()
        {
            return Task.Run(async () =>
            {
                try
                {
                    _listener = new TcpListener(IPAddress.Loopback, _config.Port);
                    _listener.Start();
                    _cts = new CancellationTokenSource();

                    _isRunning = true;
                    RunningStateChanged?.Invoke(true);
                    LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] TCP listening on 127.0.0.1:{_config.Port}");
                    Console.WriteLine($"[DEBUG] TCP listener started on port {_config.Port}");

                    while (!_cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            // Accept connection with cancellation support
                            Console.WriteLine("[DEBUG] Waiting for client...");
                            var client = await _listener.AcceptTcpClientAsync();
                            Console.WriteLine($"[DEBUG] Client connected!");
                            LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] Client connected");
                            _ = HandleClientAsync(client);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (ObjectDisposedException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[DEBUG] Accept error: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] Startup error: {ex.Message}");
                    ErrorOccurred?.Invoke(ex);
                    LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] ERROR: {ex.Message}");
                }
                finally
                {
                    CleanupListener();
                    _isRunning = false;
                    RunningStateChanged?.Invoke(false);
                }
            });
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            var clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
            LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] Client connected: {clientEndpoint}");
            Console.WriteLine($"[DEBUG] HandleClientAsync started for {clientEndpoint}");

            try
            {
                using (client)
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, _config.Encoding))
                using (var writer = new StreamWriter(stream, _config.Encoding) { AutoFlush = true })
                {
                    Console.WriteLine("[DEBUG] StreamReader/Writer created, entering read loop");
                    while (!_cts.Token.IsCancellationRequested && client.Connected)
                    {
                        try
                        {
                            Console.WriteLine("[DEBUG] Waiting for ReadLineAsync...");
                            var message = await reader.ReadLineAsync();
                            Console.WriteLine($"[DEBUG] ReadLineAsync returned: '{message}'");
                            
                            if (message == null)
                            {
                                Console.WriteLine("[DEBUG] Message is null, client disconnected");
                                break;
                            }

                            message = message.Trim();
                            if (string.IsNullOrEmpty(message))
                            {
                                Console.WriteLine("[DEBUG] Empty message, continue");
                                continue;
                            }

                            LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] RECEIVED: {message}");
                            MessageReceived?.Invoke(message);
                            Console.WriteLine($"[DEBUG] IsCompiled: {_script.IsCompiled}");

                            // Generate and send response
                            if (_script.IsCompiled)
                            {
                                Console.WriteLine("[DEBUG] Calling GetResponse...");
                                var response = _script.GetResponse(message);
                                Console.WriteLine($"[DEBUG] Response: '{response}'");
                                if (!string.IsNullOrEmpty(response))
                                {
                                    await writer.WriteLineAsync(response);
                                    LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] SENT: {response}");
                                    MessageSent?.Invoke(response);
                                }
                            }
                        }
                        catch (IOException ex)
                        {
                            Console.WriteLine($"[DEBUG] IOException: {ex.Message}");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] HandleClient exception: {ex.Message}");
                ErrorOccurred?.Invoke(ex);
                LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] Client error: {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"[DEBUG] Client {clientEndpoint} cleanup");
                LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] Client disconnected: {clientEndpoint}");
            }
        }

        public Task StopAsync()
        {
            return Task.Run(() =>
            {
                _cts?.Cancel();
                CleanupListener();
                LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] TCP listener stopped");
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

        private void CleanupListener()
        {
            try
            {
                _listener?.Stop();
                _listener = null;
            }
            catch { /* Ignore cleanup errors */ }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            CleanupListener();
            _cts?.Dispose();
        }
    }
}
