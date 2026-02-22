using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using DeviceEmulator.ViewModels;
using System;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using Avalonia;
using Avalonia.Input;
using System.Linq;
using System.Collections.Specialized;
using System.ComponentModel;

namespace DeviceEmulator.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel? _viewModel;
        private BreakpointMargin? _breakpointMargin;
        private LineHighlighter? _lineHighlighter;
        private bool _isUpdatingScript = false;

        public MainViewModel? ViewModel => DataContext as MainViewModel;

        public MainWindow()
        {
            InitializeComponent();
            
            // Setup TextEditor
            ScriptEditor.Document = new TextDocument(); // Initialize Document explicitly
            ScriptEditor.ShowLineNumbers = true;
            ScriptEditor.FontFamily = new FontFamily("Consolas, Menlo, monospace");
            ScriptEditor.FontSize = 14;
            
            // Add custom margin for breakpoints
            _breakpointMargin = new BreakpointMargin(this);
            ScriptEditor.TextArea.LeftMargins.Insert(0, _breakpointMargin);
            
            // Add custom background renderer for line highlighting
            _lineHighlighter = new LineHighlighter(this);
            ScriptEditor.TextArea.TextView.BackgroundRenderers.Add(_lineHighlighter);

            // Handle text changes
            ScriptEditor.TextChanged += ScriptEditor_TextChanged;
            
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                _viewModel.Breakpoints.CollectionChanged -= Breakpoints_CollectionChanged;
            }

            _viewModel = ViewModel;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                _viewModel.Breakpoints.CollectionChanged += Breakpoints_CollectionChanged;
                
                // Initial load
                UpdateScriptTextFromViewModel();
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.ScriptText))
            {
                UpdateScriptTextFromViewModel();
            }
            else if (e.PropertyName == nameof(MainViewModel.CurrentDebugLine))
            {
                // Redraw to update highlighting
                ScriptEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
                
                // Scroll to line if needed
                var line = _viewModel?.CurrentDebugLine ?? 0;
                if (line > 0 && line <= ScriptEditor.LineCount)
                {
                    ScriptEditor.ScrollToLine(line);
                }
            }
        }

        private void Breakpoints_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Redraw margin to show/hide breakpoints
             _breakpointMargin?.InvalidateVisual();
             // Redraw background for highlighting
             ScriptEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
        }

        private void ScriptEditor_TextChanged(object? sender, EventArgs e)
        {
            if (_isUpdatingScript || _viewModel == null) return;
            
            // Update ViewModel
             _viewModel.ScriptText = ScriptEditor.Text ?? "";
        }

        private void UpdateScriptTextFromViewModel()
        {
            if (_viewModel == null || _isUpdatingScript) return;

            var vmText = _viewModel.ScriptText ?? "";
            
            // Ensure Document exists
            if (ScriptEditor.Document == null)
            {
                ScriptEditor.Document = new TextDocument(vmText);
                return;
            }

            if (ScriptEditor.Document.Text != vmText)
            {
                Console.WriteLine($"[DEBUG] Updating editor text from VM. Len: {vmText.Length}");
                _isUpdatingScript = true;
                ScriptEditor.Document.Text = vmText;
                _isUpdatingScript = false;
            }
            else
            {
                 Console.WriteLine("[DEBUG] Editor text matches VM text.");
            }
        }
        
        // Button event handlers (kept for compatibility with XAML, though commands are preferred)
        public void OnAddSerialDevice(object sender, RoutedEventArgs e) => ViewModel?.AddSerialDevice();
        public void OnAddTcpDevice(object sender, RoutedEventArgs e) => ViewModel?.AddTcpDevice();
        public void OnRemoveDevice(object sender, RoutedEventArgs e) => ViewModel?.RemoveSelectedDevice();
        public void OnToggleRunning(object sender, RoutedEventArgs e) => ViewModel?.ToggleSelectedDeviceRunning();
        public void OnCompileScript(object sender, RoutedEventArgs e)
        {
             ViewModel?.CompileScript();
             // Focus editor to allow immediate typing if needed, or visual feedback
        }
        public void OnClearLog(object sender, RoutedEventArgs e) => ViewModel?.ClearLog();
        
        public void OnDebugContinue(object sender, RoutedEventArgs e) => ViewModel?.DebugContinue();
        public void OnDebugStep(object sender, RoutedEventArgs e) => ViewModel?.DebugStep();
        public void OnDebugStop(object sender, RoutedEventArgs e) => ViewModel?.DebugStop();

        public void OnDeviceItemClick(object sender, TappedEventArgs e)
        {
            Console.WriteLine("[DEBUG] OnDeviceItemClick called");
            var border = sender as Border;
            if (border?.DataContext is DeviceTreeItemViewModel device)
            {
                Console.WriteLine($"[DEBUG] Device clicked: {device.Config.Name}");
                if (ViewModel != null)
                {
                    ViewModel.SelectedDevice = device;
                }
            }
        }

        #region Console Tab Switching

        private void OnOutputTabClick(object sender, TappedEventArgs e)
        {
            OutputPanel.IsVisible = true;
            ConsolePanel.IsVisible = false;
            // Update tab visual
            ((OutputTab.Child as TextBlock)!).Foreground = new SolidColorBrush(Color.Parse("#BBBBBB"));
            ((ConsoleTab.Child as TextBlock)!).Foreground = new SolidColorBrush(Color.Parse("#666666"));
        }

        private void OnConsoleTabClick(object sender, TappedEventArgs e)
        {
            OutputPanel.IsVisible = false;
            ConsolePanel.IsVisible = true;
            // Update tab visual
            ((OutputTab.Child as TextBlock)!).Foreground = new SolidColorBrush(Color.Parse("#666666"));
            ((ConsoleTab.Child as TextBlock)!).Foreground = new SolidColorBrush(Color.Parse("#BBBBBB"));
            ConsoleInputBox.Focus();
        }

        private async void OnConsoleKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && ViewModel != null)
            {
                e.Handled = true;
                await ViewModel.ExecuteConsoleCommandAsync();
            }
        }

        private void OnResetConsole(object sender, RoutedEventArgs e)
        {
            ViewModel?.ResetConsole();
        }

        #endregion
    }

    /// <summary>
    /// Renders the current debug execution line background.
    /// </summary>
    public class LineHighlighter : IBackgroundRenderer
    {
        private readonly MainWindow _window;
        private readonly IBrush _backgroundBrush;
        private readonly IPen _borderPen;

        public KnownLayer Layer => KnownLayer.Background;

        public LineHighlighter(MainWindow window)
        {
            _window = window;
            _backgroundBrush = new SolidColorBrush(Color.Parse("#40FFFF00")); // Semi-transparent yellow
            _borderPen = new Pen(Brushes.Yellow, 1);
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (_window.ViewModel == null) return;

            // 1. Draw Breakpoints Background (Light Red)
            if (_window.ViewModel.Breakpoints.Count > 0)
            {
                var bpBrush = new SolidColorBrush(Color.Parse("#40FF0000")); // Semi-transparent red
                foreach (var bpLineNo in _window.ViewModel.Breakpoints)
                {
                    if (bpLineNo > _window.ScriptEditor.LineCount || bpLineNo < 1) continue;
                    var bpLine = _window.ScriptEditor.Document.GetLineByNumber(bpLineNo);
                    foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, bpLine))
                    {
                        var fullWidthRect = new Rect(0, rect.Y, textView.Bounds.Width, rect.Height);
                        drawingContext.DrawRectangle(bpBrush, null, fullWidthRect);
                    }
                }
            }

            // 2. Draw Current Execution Line (Yellow)
            var lineNo = _window.ViewModel.CurrentDebugLine;
            if (lineNo > 0 && lineNo <= _window.ScriptEditor.LineCount)
            {
                var line = _window.ScriptEditor.Document.GetLineByNumber(lineNo);
                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, line))
                {
                    var fullWidthRect = new Rect(0, rect.Y, textView.Bounds.Width, rect.Height);
                    drawingContext.DrawRectangle(_backgroundBrush, null, fullWidthRect);
                }
            }
        }
    }

    /// <summary>
    /// Margin that displays and toggles breakpoints.
    /// </summary>
    public class BreakpointMargin : AbstractMargin
    {
        private readonly MainWindow _window;
        private readonly IBrush _breakpointBrush;

        public BreakpointMargin(MainWindow window)
        {
            _window = window;
            _breakpointBrush = Brushes.Red;
            Margin = new Thickness(0, 0, 5, 0); // Spacing
        }

        /// <summary>
        /// Measure width required for the margin.
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(20, availableSize.Height); // Increased width slightly
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            Console.WriteLine("[DEBUG] BreakpointMargin PointerPressed");
            base.OnPointerPressed(e);

            if (_window.ViewModel == null) return;
            
            var textView = TextView;
            if (textView == null) return;

            var pos = e.GetPosition(textView);
            var visualLine = textView.GetVisualLineFromVisualTop(pos.Y + textView.ScrollOffset.Y);
            
            if (visualLine != null)
            {
                var lineNumber = visualLine.FirstDocumentLine.LineNumber;
                Console.WriteLine($"[DEBUG] Toggling breakpoint at line {lineNumber}");
                _window.ViewModel.ToggleBreakpoint(lineNumber);
                InvalidateVisual(); // Redraw
            }
        }

        public override void Render(DrawingContext drawingContext)
        {
            // Draw background for the margin to make it visible
            drawingContext.DrawRectangle(new SolidColorBrush(Color.Parse("#333333")), null, new Rect(0, 0, Bounds.Width, Bounds.Height));

            var textView = TextView;
            if (textView == null || !textView.VisualLinesValid || _window.ViewModel == null) return;

            foreach (var visualLine in textView.VisualLines)
            {
                var lineNumber = visualLine.FirstDocumentLine.LineNumber;

                if (_window.ViewModel.Breakpoints.Contains(lineNumber))
                {
                    var y = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.TextTop);
                    var height = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.TextBottom) - y;
                    
                    // Center the dot
                    var d = Math.Min(12, height - 2);
                    var x = (Bounds.Width - d) / 2;
                    var centeredY = y + (height - d) / 2 - textView.ScrollOffset.Y;

                    drawingContext.DrawEllipse(_breakpointBrush, null, new Point(x + d/2, centeredY + d/2), d/2, d/2);
                }
            }
        }
    }
}
