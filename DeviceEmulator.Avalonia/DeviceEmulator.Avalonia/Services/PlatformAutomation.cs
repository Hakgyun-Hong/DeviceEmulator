using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DeviceEmulator.Services
{
    /// <summary>
    /// Cross-platform automation helper.
    /// macOS: AppleScript via osascript + native CLI tools
    /// Windows: P/Invoke user32.dll + System.Windows.Automation
    /// </summary>
    public static class PlatformAutomation
    {
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        // ════════════════════════════════════════════════════════════════
        //  WINDOW MANAGEMENT
        // ════════════════════════════════════════════════════════════════

        /// <summary>Gets titles of all visible windows.</summary>
        public static List<string> GetOpenWindows()
        {
            if (IsMacOS) return GetOpenWindows_Mac();
            if (IsWindows) return GetOpenWindows_Win();
            return new List<string>();
        }

        /// <summary>Activates a window and brings it to the foreground.</summary>
        public static string ActivateWindow(string title)
        {
            if (IsMacOS) return ActivateWindow_Mac(title);
            if (IsWindows) return ActivateWindow_Win(title);
            return "Unsupported OS";
        }

        /// <summary>Closes a window by title.</summary>
        public static string CloseWindow(string title)
        {
            if (IsMacOS) return CloseWindow_Mac(title);
            if (IsWindows) return CloseWindow_Win(title);
            return "Unsupported OS";
        }

        /// <summary>Waits for a window to appear.</summary>
        public static async Task<string> WaitForWindow(string title, int timeoutSeconds)
        {
            var endTime = DateTime.Now.AddSeconds(timeoutSeconds);
            while (DateTime.Now < endTime)
            {
                var windows = GetOpenWindows();
                if (windows.Any(w => w.Contains(title, StringComparison.OrdinalIgnoreCase)))
                    return $"Found: {title}";
                await Task.Delay(500);
            }
            return $"Timeout: {title} not found";
        }

        /// <summary>Sets window state: Maximize, Minimize, Restore.</summary>
        public static string SetWindowState(string title, string state)
        {
            if (IsMacOS) return SetWindowState_Mac(title, state);
            if (IsWindows) return SetWindowState_Win(title, state);
            return "Unsupported OS";
        }

        /// <summary>Moves a window to the specified position.</summary>
        public static string MoveWindow(string title, int x, int y)
        {
            if (IsMacOS) return MoveWindow_Mac(title, x, y);
            if (IsWindows) return MoveWindow_Win(title, x, y);
            return "Unsupported OS";
        }

        /// <summary>Resizes a window.</summary>
        public static string ResizeWindow(string title, int width, int height)
        {
            if (IsMacOS) return ResizeWindow_Mac(title, width, height);
            if (IsWindows) return ResizeWindow_Win(title, width, height);
            return "Unsupported OS";
        }

        /// <summary>Gets the title of the foreground window.</summary>
        public static string GetForegroundWindowTitle()
        {
            if (IsMacOS) return GetForegroundWindowTitle_Mac();
            if (IsWindows) return GetForegroundWindowTitle_Win();
            return "";
        }

        /// <summary>Sets window to always-on-top.</summary>
        public static string SetWindowTopmost(string title, bool topmost)
        {
            if (IsWindows) return SetWindowTopmost_Win(title, topmost);
            // macOS doesn't have easy topmost via AppleScript
            return IsMacOS ? "Topmost not supported on macOS via AppleScript" : "Unsupported OS";
        }

        /// <summary>Gets window position and size as "X,Y,W,H".</summary>
        public static string GetWindowPosition(string title)
        {
            if (IsMacOS) return GetWindowPosition_Mac(title);
            if (IsWindows) return GetWindowPosition_Win(title);
            return "";
        }

        // ════════════════════════════════════════════════════════════════
        //  INPUT
        // ════════════════════════════════════════════════════════════════

        /// <summary>Sends keystrokes.</summary>
        public static string SendKeys(string keys)
        {
            if (IsMacOS) return SendKeys_Mac(keys);
            if (IsWindows) return SendKeys_Win(keys);
            return "Unsupported OS";
        }

        /// <summary>Sends keys to a specific window (activate first).</summary>
        public static string SendKeysToWindow(string title, string keys)
        {
            ActivateWindow(title);
            System.Threading.Thread.Sleep(200);
            return SendKeys(keys);
        }

        /// <summary>Mouse click at X,Y with ClickType (Left/Right/Middle).</summary>
        public static string MouseClick(int x, int y, string clickType = "Left")
        {
            if (IsMacOS) return MouseClick_Mac(x, y, clickType);
            if (IsWindows) return MouseClick_Win(x, y, clickType);
            return "Unsupported OS";
        }

        /// <summary>Moves mouse cursor to X,Y.</summary>
        public static string MouseMove(int x, int y)
        {
            if (IsMacOS) return MouseMove_Mac(x, y);
            if (IsWindows) return MouseMove_Win(x, y);
            return "Unsupported OS";
        }

        /// <summary>Mouse scroll. Positive = up, negative = down.</summary>
        public static string MouseScroll(int amount)
        {
            if (IsMacOS) return RunAppleScript($"do shell script \"cliclick 'kd:cmd' 'kd:fn' 'w:{amount}' 'ku:fn' 'ku:cmd'\"");
            if (IsWindows) return MouseScroll_Win(amount);
            return "Unsupported OS";
        }

        /// <summary>Gets cursor position as "X,Y".</summary>
        public static string GetCursorPosition()
        {
            if (IsMacOS)
            {
                var result = RunShell("python3", "-c \"import Quartz; loc = Quartz.NSEvent.mouseLocation(); print(f'{int(loc.x)},{int(Quartz.NSScreen.mainScreen().frame().size.height - loc.y)}')\"");
                return result.Trim();
            }
            if (IsWindows) return GetCursorPosition_Win();
            return "0,0";
        }

        // ════════════════════════════════════════════════════════════════
        //  UI ELEMENT DISCOVERY
        // ════════════════════════════════════════════════════════════════

        /// <summary>Gets names of all UI elements in a window.</summary>
        public static List<string> GetUIElements(string windowTitle)
        {
            if (IsMacOS) return GetUIElements_Mac(windowTitle);
            if (IsWindows) return GetUIElements_Win(windowTitle);
            return new List<string>();
        }

        /// <summary>Gets names of all buttons in a window.</summary>
        public static List<string> GetUIButtons(string windowTitle)
        {
            if (IsMacOS) return GetUIButtons_Mac(windowTitle);
            if (IsWindows) return GetUIButtons_Win(windowTitle);
            return new List<string>();
        }

        /// <summary>Clicks a UI element by name inside a window.</summary>
        public static string ClickUIElement(string windowTitle, string elementName)
        {
            if (IsMacOS) return ClickUIElement_Mac(windowTitle, elementName);
            if (IsWindows) return ClickUIElement_Win(windowTitle, elementName);
            return "Unsupported OS";
        }

        /// <summary>Invokes (clicks) a button by name using accessibility APIs.</summary>
        public static string InvokeUIButton(string windowTitle, string buttonName)
        {
            if (IsMacOS) return InvokeUIButton_Mac(windowTitle, buttonName);
            if (IsWindows) return InvokeUIButton_Win(windowTitle, buttonName);
            return "Unsupported OS";
        }

        /// <summary>Gets the text/value of a UI element.</summary>
        public static string GetUIElementText(string windowTitle, string elementId)
        {
            if (IsMacOS) return GetUIElementText_Mac(windowTitle, elementId);
            if (IsWindows) return GetUIElementText_Win(windowTitle, elementId);
            return "";
        }

        // ════════════════════════════════════════════════════════════════
        //  PROCESS
        // ════════════════════════════════════════════════════════════════

        /// <summary>Gets list of running process names.</summary>
        public static List<string> GetRunningProcesses()
        {
            return Process.GetProcesses()
                .Where(p => !string.IsNullOrWhiteSpace(p.ProcessName))
                .Select(p => p.ProcessName)
                .Distinct()
                .OrderBy(n => n)
                .ToList();
        }

        /// <summary>Starts a process.</summary>
        public static string StartProcess(string program, string args = "")
        {
            if (IsMacOS && !program.Contains("/") && !program.Contains("."))
            {
                // On macOS, try opening .app first
                try { Process.Start("open", $"-a \"{program}\" {args}"); return $"Started: {program}"; }
                catch { /* fallback below */ }
            }
            if (string.IsNullOrWhiteSpace(args))
                Process.Start(new ProcessStartInfo(program) { UseShellExecute = true });
            else
                Process.Start(new ProcessStartInfo(program, args) { UseShellExecute = true });
            return $"Started: {program}";
        }

        /// <summary>Stops a process by name (close main window).</summary>
        public static string StopProcess(string name)
        {
            var procs = Process.GetProcessesByName(name);
            foreach (var p in procs)
            {
                try { p.CloseMainWindow(); } catch { }
            }
            return $"Stopped {procs.Length} instance(s) of {name}";
        }

        /// <summary>Force-kills a process by name.</summary>
        public static string KillProcess(string name)
        {
            var procs = Process.GetProcessesByName(name);
            foreach (var p in procs)
            {
                try { p.Kill(); } catch { }
            }
            return $"Killed {procs.Length} instance(s) of {name}";
        }

        /// <summary>Checks if a process is running.</summary>
        public static bool IsProcessRunning(string name)
        {
            return Process.GetProcessesByName(name).Length > 0;
        }

        // ════════════════════════════════════════════════════════════════
        //  CLIPBOARD
        // ════════════════════════════════════════════════════════════════

        /// <summary>Gets text from clipboard.</summary>
        public static string GetClipboardText()
        {
            if (IsMacOS) return RunShell("pbpaste", "").Trim();
            if (IsWindows) return GetClipboardText_Win();
            return "";
        }

        /// <summary>Sets text to clipboard.</summary>
        public static string SetClipboardText(string text)
        {
            if (IsMacOS)
            {
                var psi = new ProcessStartInfo("pbcopy") { RedirectStandardInput = true, UseShellExecute = false };
                var p = Process.Start(psi);
                p?.StandardInput.Write(text);
                p?.StandardInput.Close();
                p?.WaitForExit();
                return "Clipboard set";
            }
            if (IsWindows) return SetClipboardText_Win(text);
            return "Unsupported OS";
        }

        // ════════════════════════════════════════════════════════════════
        //  SCREENSHOT
        // ════════════════════════════════════════════════════════════════

        /// <summary>Takes a full screenshot.</summary>
        public static string TakeScreenshot(string filePath)
        {
            if (IsMacOS) { RunShell("screencapture", $"\"{filePath}\""); return $"Screenshot: {filePath}"; }
            if (IsWindows) return TakeScreenshot_Win(filePath);
            return "Unsupported OS";
        }

        /// <summary>Takes a region screenshot.</summary>
        public static string TakeRegionScreenshot(int x, int y, int w, int h, string filePath)
        {
            if (IsMacOS) { RunShell("screencapture", $"-R{x},{y},{w},{h} \"{filePath}\""); return $"Region screenshot: {filePath}"; }
            if (IsWindows) return TakeRegionScreenshot_Win(x, y, w, h, filePath);
            return "Unsupported OS";
        }

        // ════════════════════════════════════════════════════════════════════
        //  macOS IMPLEMENTATIONS (AppleScript via osascript + CLI)
        // ════════════════════════════════════════════════════════════════════

        #region macOS

        private static List<string> GetOpenWindows_Mac()
        {
            var script = @"
tell application ""System Events""
    set windowList to """"
    set allProcs to every process whose visible is true
    repeat with aProc in allProcs
        set procName to name of aProc
        try
            set procWindows to every window of aProc
            repeat with aWin in procWindows
                set winName to name of aWin
                if winName is not """" then
                    set windowList to windowList & procName & "" - "" & winName & linefeed
                end if
            end repeat
        end try
    end repeat
end tell
return windowList";
            var result = RunAppleScript(script);
            return result.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        private static string ActivateWindow_Mac(string title)
        {
            // Try matching by window title across all apps
            var appName = FindAppByWindowTitle_Mac(title);
            if (string.IsNullOrEmpty(appName))
                return $"Window not found: {title}";

            RunAppleScript($@"
tell application ""{appName}""
    activate
end tell
tell application ""System Events""
    tell process ""{appName}""
        set frontmost to true
        try
            set targetWindow to (first window whose name contains ""{EscapeAppleScript(title)}"")
            perform action ""AXRaise"" of targetWindow
        end try
    end tell
end tell");
            return $"Activated: {title}";
        }

        private static string CloseWindow_Mac(string title)
        {
            var appName = FindAppByWindowTitle_Mac(title);
            if (string.IsNullOrEmpty(appName))
                return $"Window not found: {title}";

            RunAppleScript($@"
tell application ""System Events""
    tell process ""{appName}""
        try
            set targetWindow to (first window whose name contains ""{EscapeAppleScript(title)}"")
            click button 1 of targetWindow
        end try
    end tell
end tell");
            return $"Closed: {title}";
        }

        private static string SetWindowState_Mac(string title, string state)
        {
            var appName = FindAppByWindowTitle_Mac(title);
            if (string.IsNullOrEmpty(appName))
                return $"Window not found: {title}";

            switch (state.ToLower())
            {
                case "minimize":
                    RunAppleScript($@"tell application ""System Events"" to tell process ""{appName}"" to set value of attribute ""AXMinimized"" of (first window whose name contains ""{EscapeAppleScript(title)}"") to true");
                    break;
                case "maximize":
                    RunAppleScript($@"tell application ""System Events"" to tell process ""{appName}"" to set value of attribute ""AXFullScreen"" of (first window whose name contains ""{EscapeAppleScript(title)}"") to true");
                    break;
                case "restore":
                    RunAppleScript($@"tell application ""System Events"" to tell process ""{appName}"" to set value of attribute ""AXMinimized"" of (first window whose name contains ""{EscapeAppleScript(title)}"") to false");
                    break;
            }
            return $"Set {state}: {title}";
        }

        private static string MoveWindow_Mac(string title, int x, int y)
        {
            var appName = FindAppByWindowTitle_Mac(title);
            if (string.IsNullOrEmpty(appName))
                return $"Window not found: {title}";

            RunAppleScript($@"tell application ""System Events"" to tell process ""{appName}"" to set position of (first window whose name contains ""{EscapeAppleScript(title)}"") to {{{x}, {y}}}");
            return $"Moved: {title} to ({x}, {y})";
        }

        private static string ResizeWindow_Mac(string title, int w, int h)
        {
            var appName = FindAppByWindowTitle_Mac(title);
            if (string.IsNullOrEmpty(appName))
                return $"Window not found: {title}";

            RunAppleScript($@"tell application ""System Events"" to tell process ""{appName}"" to set size of (first window whose name contains ""{EscapeAppleScript(title)}"") to {{{w}, {h}}}");
            return $"Resized: {title} to {w}x{h}";
        }

        private static string GetForegroundWindowTitle_Mac()
        {
            return RunAppleScript(@"
tell application ""System Events""
    set frontApp to first application process whose frontmost is true
    set appName to name of frontApp
    try
        set winName to name of front window of frontApp
        return appName & "" - "" & winName
    end try
    return appName
end tell").Trim();
        }

        private static string GetWindowPosition_Mac(string title)
        {
            var appName = FindAppByWindowTitle_Mac(title);
            if (string.IsNullOrEmpty(appName)) return "Window not found";

            var pos = RunAppleScript($@"tell application ""System Events"" to tell process ""{appName}"" to get position of (first window whose name contains ""{EscapeAppleScript(title)}"")").Trim();
            var size = RunAppleScript($@"tell application ""System Events"" to tell process ""{appName}"" to get size of (first window whose name contains ""{EscapeAppleScript(title)}"")").Trim();
            return $"{pos},{size}";
        }

        private static string SendKeys_Mac(string keys)
        {
            // Translate common SendKeys syntax to AppleScript
            var script = $@"tell application ""System Events"" to keystroke ""{EscapeAppleScript(keys)}""";
            // Handle special keys
            if (keys.Contains("{ENTER}")) script = @"tell application ""System Events"" to key code 36";
            else if (keys.Contains("{TAB}")) script = @"tell application ""System Events"" to key code 48";
            else if (keys.Contains("{ESC}")) script = @"tell application ""System Events"" to key code 53";
            else if (keys.Contains("{DELETE}")) script = @"tell application ""System Events"" to key code 51";

            RunAppleScript(script);
            return $"Sent keys: {keys}";
        }

        private static string MouseClick_Mac(int x, int y, string clickType)
        {
            // Use cliclick if available, otherwise AppleScript + python
            string btn1 = clickType.ToLower() == "right" ? "rc" : "c";
            string pButton1 = clickType.ToLower() == "right" ? "Quartz.kCGEventRightMouseDown" : "Quartz.kCGEventLeftMouseDown";
            string pButton2 = clickType.ToLower() == "right" ? "Quartz.kCGMouseButtonRight" : "Quartz.kCGMouseButtonLeft";
            
            var result = RunShell("bash", $"-c \"which cliclick > /dev/null 2>&1 && cliclick {btn1}:{x},{y} || python3 -c \\\"import Quartz; evt = Quartz.CGEventCreateMouseEvent(None, {pButton1}, ({x},{y}), {pButton2}); Quartz.CGEventPost(Quartz.kCGHIDEventTap, evt)\\\"\"");
            
            // Simplified: use python3 + Quartz (available on macOS)
            string bDown = clickType.ToLower() == "right" ? "3" : "1";
            string bUp = clickType.ToLower() == "right" ? "4" : "2";
            string bBtn = clickType.ToLower() == "right" ? "1" : "0";
            RunShell("python3", $"-c \"import Quartz, time; p=({x},{y}); Quartz.CGEventPost(Quartz.kCGHIDEventTap, Quartz.CGEventCreateMouseEvent(None, {bDown}, p, {bBtn})); time.sleep(0.05); Quartz.CGEventPost(Quartz.kCGHIDEventTap, Quartz.CGEventCreateMouseEvent(None, {bUp}, p, {bBtn}))\"");
            return $"Clicked {clickType} at ({x}, {y})";
        }

        private static string MouseMove_Mac(int x, int y)
        {
            RunShell("python3", $"-c \"import Quartz; Quartz.CGEventPost(Quartz.kCGHIDEventTap, Quartz.CGEventCreateMouseEvent(None, Quartz.kCGEventMouseMoved, ({x},{y}), 0))\"");
            return $"Mouse moved to ({x}, {y})";
        }

        private static List<string> GetUIElements_Mac(string windowTitle)
        {
            var appName = FindAppByWindowTitle_Mac(windowTitle);
            if (string.IsNullOrEmpty(appName)) return new List<string>();

            var result = RunAppleScript($@"
tell application ""System Events""
    tell process ""{appName}""
        set elementList to """"
        try
            set targetWindow to (first window whose name contains ""{EscapeAppleScript(windowTitle)}"")
            set allElements to entire contents of targetWindow
            repeat with anElement in allElements
                try
                    set elName to name of anElement
                    set elRole to role of anElement
                    if elName is not """" then
                        set elementList to elementList & elRole & "": "" & elName & linefeed
                    end if
                end try
            end repeat
        end try
    end tell
end tell
return elementList");
            return result.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        private static List<string> GetUIButtons_Mac(string windowTitle)
        {
            var appName = FindAppByWindowTitle_Mac(windowTitle);
            if (string.IsNullOrEmpty(appName)) return new List<string>();

            var result = RunAppleScript($@"
tell application ""System Events""
    tell process ""{appName}""
        set btnList to """"
        try
            set targetWindow to (first window whose name contains ""{EscapeAppleScript(windowTitle)}"")
            set allButtons to every button of targetWindow
            repeat with aBtn in allButtons
                try
                    set btnName to name of aBtn
                    if btnName is not """" then
                        set btnList to btnList & btnName & linefeed
                    end if
                end try
            end repeat
            -- Also check toolbars
            try
                set allToolbarButtons to every button of every toolbar of targetWindow
                repeat with aTB in allToolbarButtons
                    repeat with aBtn in aTB
                        try
                            set btnName to name of aBtn
                            if btnName is not """" then
                                set btnList to btnList & btnName & linefeed
                            end if
                        end try
                    end repeat
                end repeat
            end try
        end try
    end tell
end tell
return btnList");
            return result.Split('\n', StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
        }

        private static string ClickUIElement_Mac(string windowTitle, string elementName)
        {
            var appName = FindAppByWindowTitle_Mac(windowTitle);
            if (string.IsNullOrEmpty(appName)) return $"Window not found: {windowTitle}";

            RunAppleScript($@"
tell application ""System Events""
    tell process ""{appName}""
        set frontmost to true
        try
            set targetWindow to (first window whose name contains ""{EscapeAppleScript(windowTitle)}"")
            set allElements to entire contents of targetWindow
            repeat with anElement in allElements
                try
                    if name of anElement is ""{EscapeAppleScript(elementName)}"" then
                        click anElement
                        exit repeat
                    end if
                end try
            end repeat
        end try
    end tell
end tell");
            return $"Clicked: {elementName}";
        }

        private static string InvokeUIButton_Mac(string windowTitle, string buttonName)
        {
            var appName = FindAppByWindowTitle_Mac(windowTitle);
            if (string.IsNullOrEmpty(appName)) return $"Window not found: {windowTitle}";

            RunAppleScript($@"
tell application ""System Events""
    tell process ""{appName}""
        set frontmost to true
        try
            set targetWindow to (first window whose name contains ""{EscapeAppleScript(windowTitle)}"")
            click button ""{EscapeAppleScript(buttonName)}"" of targetWindow
        end try
    end tell
end tell");
            return $"Invoked: {buttonName}";
        }

        private static string GetUIElementText_Mac(string windowTitle, string elementId)
        {
            var appName = FindAppByWindowTitle_Mac(windowTitle);
            if (string.IsNullOrEmpty(appName)) return "";

            return RunAppleScript($@"
tell application ""System Events""
    tell process ""{appName}""
        try
            set targetWindow to (first window whose name contains ""{EscapeAppleScript(windowTitle)}"")
            set allElements to entire contents of targetWindow
            repeat with anElement in allElements
                try
                    if name of anElement is ""{EscapeAppleScript(elementId)}"" or description of anElement is ""{EscapeAppleScript(elementId)}"" then
                        try
                            return value of anElement
                        on error
                            return name of anElement
                        end try
                    end if
                end try
            end repeat
        end try
    end tell
end tell
return """"").Trim();
        }

        // ─── macOS Helpers ───

        private static string FindAppByWindowTitle_Mac(string title)
        {
            var result = RunAppleScript($@"
tell application ""System Events""
    set allProcs to every process whose visible is true
    repeat with aProc in allProcs
        try
            set procWindows to every window of aProc
            repeat with aWin in procWindows
                if name of aWin contains ""{EscapeAppleScript(title)}"" then
                    return name of aProc
                end if
            end repeat
        end try
    end repeat
end tell
return """"").Trim();
            return result;
        }

        private static string RunAppleScript(string script)
        {
            try
            {
                var psi = new ProcessStartInfo("osascript", $"-e '{script.Replace("'", "'\\''")}'")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var p = Process.Start(psi);
                var output = p?.StandardOutput.ReadToEnd() ?? "";
                p?.WaitForExit(5000);
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PlatformAutomation] AppleScript error: {ex.Message}");
                return "";
            }
        }

        private static string EscapeAppleScript(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        #endregion

        // ════════════════════════════════════════════════════════════════════
        //  WINDOWS IMPLEMENTATIONS (P/Invoke)
        // ════════════════════════════════════════════════════════════════════

        #region Windows

        // ─── Win32 imports ───
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }

        // ─── Window methods (Windows) ───

        private static IntPtr FindWindowByTitle_Win(string title)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((hWnd, _) =>
            {
                if (!IsWindowVisible(hWnd)) return true;
                var sb = new StringBuilder(256);
                GetWindowText(hWnd, sb, 256);
                if (sb.ToString().Contains(title, StringComparison.OrdinalIgnoreCase))
                {
                    found = hWnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        private static List<string> GetOpenWindows_Win()
        {
            var titles = new List<string>();
            EnumWindows((hWnd, _) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    var sb = new StringBuilder(256);
                    GetWindowText(hWnd, sb, 256);
                    if (sb.Length > 0) titles.Add(sb.ToString());
                }
                return true;
            }, IntPtr.Zero);
            return titles;
        }

        private static string ActivateWindow_Win(string title)
        {
            var hWnd = FindWindowByTitle_Win(title);
            if (hWnd == IntPtr.Zero) return $"Window not found: {title}";
            ShowWindow(hWnd, 9);
            SetForegroundWindow(hWnd);
            return $"Activated: {title}";
        }

        private static string CloseWindow_Win(string title)
        {
            var hWnd = FindWindowByTitle_Win(title);
            if (hWnd == IntPtr.Zero) return $"Window not found: {title}";
            SendMessage(hWnd, 0x0010, IntPtr.Zero, IntPtr.Zero);
            return $"Closed: {title}";
        }

        private static string SetWindowState_Win(string title, string state)
        {
            var hWnd = FindWindowByTitle_Win(title);
            if (hWnd == IntPtr.Zero) return $"Window not found: {title}";
            int cmd = state.ToLower() switch { "maximize" => 3, "minimize" => 6, _ => 9 };
            ShowWindow(hWnd, cmd);
            return $"Set {state}: {title}";
        }

        private static string MoveWindow_Win(string title, int x, int y)
        {
            var hWnd = FindWindowByTitle_Win(title);
            if (hWnd == IntPtr.Zero) return $"Window not found: {title}";
            SetWindowPos(hWnd, IntPtr.Zero, x, y, 0, 0, 0x0001 | 0x0004);
            return $"Moved: {title} to ({x}, {y})";
        }

        private static string ResizeWindow_Win(string title, int w, int h)
        {
            var hWnd = FindWindowByTitle_Win(title);
            if (hWnd == IntPtr.Zero) return $"Window not found: {title}";
            SetWindowPos(hWnd, IntPtr.Zero, 0, 0, w, h, 0x0002 | 0x0004);
            return $"Resized: {title} to {w}x{h}";
        }

        private static string GetForegroundWindowTitle_Win()
        {
            var sb = new StringBuilder(256);
            GetWindowText(GetForegroundWindow(), sb, 256);
            return sb.ToString();
        }

        private static string SetWindowTopmost_Win(string title, bool topmost)
        {
            var hWnd = FindWindowByTitle_Win(title);
            if (hWnd == IntPtr.Zero) return $"Window not found: {title}";
            SetWindowPos(hWnd, topmost ? new IntPtr(-1) : new IntPtr(-2), 0, 0, 0, 0, 0x0001 | 0x0002);
            return (topmost ? "Set topmost: " : "Removed topmost: ") + title;
        }

        private static string GetWindowPosition_Win(string title)
        {
            var hWnd = FindWindowByTitle_Win(title);
            if (hWnd == IntPtr.Zero) return "Window not found";
            GetWindowRect(hWnd, out RECT r);
            return $"{r.Left},{r.Top},{r.Right - r.Left},{r.Bottom - r.Top}";
        }

        // ─── Input (Windows) ───

        private static string SendKeys_Win(string keys)
        {
            return "SendKeys on Windows requires System.Windows.Forms reference.";
        }

        private static string MouseClick_Win(int x, int y, string clickType)
        {
            SetCursorPos(x, y);
            System.Threading.Thread.Sleep(50);
            uint down = clickType.ToLower() == "right" ? 0x0008u : 0x0002u;
            uint up = clickType.ToLower() == "right" ? 0x0010u : 0x0004u;
            mouse_event(down, x, y, 0, 0);
            mouse_event(up, x, y, 0, 0);
            return $"Clicked {clickType} at ({x}, {y})";
        }

        private static string MouseMove_Win(int x, int y)
        {
            SetCursorPos(x, y);
            return $"Mouse moved to ({x}, {y})";
        }

        private static string MouseScroll_Win(int amount)
        {
            mouse_event(0x0800, 0, 0, (uint)(amount * 120), 0);
            return $"Scrolled: {amount}";
        }

        private static string GetCursorPosition_Win()
        {
            GetCursorPos(out POINT p);
            return $"{p.X},{p.Y}";
        }

        // ─── UI Automation (Windows) ───

        private static List<string> GetUIElements_Win(string windowTitle)
        {
            return new List<string> { "UIAutomation required on Windows" };
        }

        private static List<string> GetUIButtons_Win(string windowTitle)
        {
            return new List<string>();
        }

        private static string ClickUIElement_Win(string windowTitle, string elementName)
        {
            return "UIAutomation required on Windows";
        }

        private static string InvokeUIButton_Win(string windowTitle, string buttonName)
        {
            return "UIAutomation required on Windows";
        }

        private static string GetUIElementText_Win(string windowTitle, string elementId)
        {
            // Fallback for Windows if UIAutomationClient is not referenced
            return "UIAutomation on Windows requires UIAutomationClient reference.";
        }

        // ─── Clipboard (Windows) ───

        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);
        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardData(uint uFormat);
        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();
        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);
        [DllImport("kernel32.dll")]
        private static extern bool GlobalUnlock(IntPtr hMem);

        private static string GetClipboardText_Win()
        {
            string result = "";
            if (OpenClipboard(IntPtr.Zero))
            {
                var hData = GetClipboardData(13);
                if (hData != IntPtr.Zero)
                {
                    var lpData = GlobalLock(hData);
                    result = Marshal.PtrToStringUni(lpData) ?? "";
                    GlobalUnlock(hData);
                }
                CloseClipboard();
            }
            return result;
        }

        private static string SetClipboardText_Win(string text)
        {
            return "SetClipboardText_Win requires System.Windows.Forms reference.";
        }

        // ─── Screenshot (Windows) ───

        private static string TakeScreenshot_Win(string filePath)
        {
            return "TakeScreenshot_Win requires System.Windows.Forms and System.Drawing reference.";
        }

        private static string TakeRegionScreenshot_Win(int x, int y, int w, int h, string filePath)
        {
            return "TakeRegionScreenshot_Win requires System.Drawing reference.";
        }

        #endregion

        // ════════════════════════════════════════════════════════════════════
        //  SHARED HELPERS
        // ════════════════════════════════════════════════════════════════════

        /// <summary>Runs a shell command and returns stdout.</summary>
        public static string RunShell(string command, string args)
        {
            try
            {
                var psi = new ProcessStartInfo(command, args)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var p = Process.Start(psi);
                var output = p?.StandardOutput.ReadToEnd() ?? "";
                p?.WaitForExit(5000);
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PlatformAutomation] Shell error: {ex.Message}");
                return "";
            }
        }
    }
}
