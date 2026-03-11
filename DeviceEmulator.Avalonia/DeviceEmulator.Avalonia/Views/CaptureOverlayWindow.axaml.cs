using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using DeviceEmulator.Services;
using System;
using System.Threading.Tasks;

namespace DeviceEmulator.Views
{
    /// <summary>
    /// A360-style fullscreen overlay for capturing image templates.
    /// When the cursor moves, edge detection automatically identifies the UI component
    /// boundary under the cursor and highlights it with a red rectangle.
    /// Click to capture, ESC to cancel.
    /// </summary>
    public partial class CaptureOverlayWindow : Window
    {
        private string? _capturedImagePath;
        private DispatcherTimer? _trackingTimer;
        private bool _isCaptured = false;

        // Last detected component bounds (screen coordinates)
        private int _detectedX, _detectedY, _detectedW, _detectedH;
        private bool _hasDetection = false;

        // Throttle: skip detection if cursor hasn't moved enough
        private int _lastCursorX = -1, _lastCursorY = -1;

        // Freeze detection while a screenshot is being taken for edge analysis
        private bool _isDetecting = false;

        /// <summary>
        /// The file path of the captured image, or null if cancelled.
        /// </summary>
        public string? CapturedImagePath => _capturedImagePath;

        public CaptureOverlayWindow()
        {
            InitializeComponent();

            _trackingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(150) // Slightly slower to allow screenshot + edge analysis
            };
            _trackingTimer.Tick += OnTrackingTick;
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            _trackingTimer?.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            _trackingTimer?.Stop();
            base.OnClosed(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape)
            {
                _capturedImagePath = null;
                _trackingTimer?.Stop();
                Close();
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (_isCaptured) return;
            _isCaptured = true;
            _trackingTimer?.Stop();

            if (_hasDetection && _detectedW > 4 && _detectedH > 4)
            {
                PerformCapture(_detectedX, _detectedY, _detectedW, _detectedH);
            }
            else
            {
                // Fallback: capture 64x64 around cursor
                var posStr = PlatformAutomation.GetCursorPosition();
                var parts = posStr.Split(',');
                if (parts.Length >= 2 && int.TryParse(parts[0].Trim(), out int cx) && int.TryParse(parts[1].Trim(), out int cy))
                {
                    PerformCapture(cx - 32, cy - 16, 64, 32);
                }
                else
                {
                    _capturedImagePath = null;
                    Close();
                }
            }
        }

        private async void OnTrackingTick(object? sender, EventArgs e)
        {
            if (_isCaptured || _isDetecting) return;

            try
            {
                // Get system cursor position
                var posStr = PlatformAutomation.GetCursorPosition();
                var parts = posStr.Split(',');
                if (parts.Length < 2 || !int.TryParse(parts[0].Trim(), out int cx) || !int.TryParse(parts[1].Trim(), out int cy))
                    return;

                CursorPosText.Text = $"({cx}, {cy})";

                // Skip if cursor hasn't moved enough (>5px)
                if (Math.Abs(cx - _lastCursorX) < 5 && Math.Abs(cy - _lastCursorY) < 5)
                    return;

                _lastCursorX = cx;
                _lastCursorY = cy;
                _isDetecting = true;

                // Hide overlay briefly for clean screenshot
                this.Opacity = 0;
                await Task.Delay(50);

                // Detect component bounds using edge detection (runs on background thread)
                var bounds = await Task.Run(() => ImageMatchService.DetectComponentBounds(cx, cy));

                // Restore overlay
                this.Opacity = 1;
                _isDetecting = false;

                if (bounds.Found)
                {
                    _hasDetection = true;
                    _detectedX = bounds.X;
                    _detectedY = bounds.Y;
                    _detectedW = bounds.Width;
                    _detectedH = bounds.Height;

                    // Update rectangle position/size on the overlay
                    var windowPos = Position;
                    double localX = bounds.X - windowPos.X;
                    double localY = bounds.Y - windowPos.Y;

                    Canvas.SetLeft(CaptureRect, localX);
                    Canvas.SetTop(CaptureRect, localY);
                    CaptureRect.Width = bounds.Width;
                    CaptureRect.Height = bounds.Height;
                    CaptureRect.IsVisible = true;

                    CaptureAreaText.Text = $"{bounds.Width} × {bounds.Height}";
                }
                else
                {
                    // Fallback: show small rectangle at cursor
                    _hasDetection = false;
                    var windowPos = Position;
                    Canvas.SetLeft(CaptureRect, cx - windowPos.X - 32);
                    Canvas.SetTop(CaptureRect, cy - windowPos.Y - 16);
                    CaptureRect.Width = 64;
                    CaptureRect.Height = 32;
                    CaptureRect.IsVisible = true;
                    CaptureAreaText.Text = "64 × 32 (fallback)";
                }
            }
            catch (Exception ex)
            {
                _isDetecting = false;
                this.Opacity = 1;
                Console.WriteLine($"[CaptureOverlay] Tracking error: {ex.Message}");
            }
        }

        private void PerformCapture(int screenX, int screenY, int width, int height)
        {
            try
            {
                // Hide overlay for clean capture
                this.Opacity = 0;

                Task.Delay(200).ContinueWith(_ =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        try
                        {
                            var bitmap = ImageMatchService.CaptureRegion(screenX, screenY, width, height);
                            if (bitmap != null)
                            {
                                _capturedImagePath = ImageMatchService.SaveTemplateImage(bitmap);
                                bitmap.Dispose();
                                Console.WriteLine($"[CaptureOverlay] Captured component: {_capturedImagePath} ({width}x{height})");
                            }
                            else
                            {
                                Console.WriteLine("[CaptureOverlay] Capture returned null");
                                _capturedImagePath = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[CaptureOverlay] Error: {ex.Message}");
                            _capturedImagePath = null;
                        }
                        finally
                        {
                            Close();
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CaptureOverlay] PerformCapture error: {ex.Message}");
                _capturedImagePath = null;
                Close();
            }
        }
    }
}
