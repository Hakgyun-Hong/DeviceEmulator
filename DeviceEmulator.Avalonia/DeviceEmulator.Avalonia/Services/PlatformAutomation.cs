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
            if (string.IsNullOrWhiteSpace(title))
            {
                // Fallback: Frontmost application
                return RunAppleScript(@"
tell application ""System Events""
    return name of first application process whose frontmost is true
end tell").Trim();
            }

            if (title.StartsWith("pid:", StringComparison.OrdinalIgnoreCase) && int.TryParse(title.Substring(4), out int pid))
            {
                try
                {
                    var p = Process.GetProcessById(pid);
                    return p.ProcessName;
                }
                catch { }
            }

            var result = RunAppleScript($@"
tell application ""System Events""
    set allProcs to every process whose visible is true
    repeat with aProc in allProcs
        try
            -- 1. Check if the process name itself is a match (fallback)
            if name of aProc is ""{EscapeAppleScript(title)}"" then
                return name of aProc
            end if

            -- 2. Check window titles
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
            if (string.IsNullOrWhiteSpace(title))
                return GetForegroundWindow();

            if (title.StartsWith("pid:", StringComparison.OrdinalIgnoreCase))
            {
                var idStr = new string(title.Substring(4).TakeWhile(char.IsDigit).ToArray());
                if (int.TryParse(idStr, out int pid))
                {
                    try
                    {
                        var p = Process.GetProcessById(pid);
                        if (p.MainWindowHandle != IntPtr.Zero)
                            return p.MainWindowHandle;
                    }
                    catch { /* Process not found or no main window */ }
                }
            }

            // 1. Exact or partial match by Window Title
            IntPtr found = IntPtr.Zero;
            EnumWindows((hWnd, _) =>
            {
                if (!IsWindowVisible(hWnd)) return true;
                var sb = new StringBuilder(256);
                GetWindowText(hWnd, sb, 256);
                if (sb.Length > 0 && sb.ToString().Contains(title, StringComparison.OrdinalIgnoreCase))
                {
                    found = hWnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            if (found != IntPtr.Zero) return found;

            // 2. Fallback: match by process name (ensure we get one with a window)
            try
            {
                var procs = Process.GetProcessesByName(title);
                foreach (var p in procs)
                {
                    if (p.MainWindowHandle != IntPtr.Zero)
                        return p.MainWindowHandle;
                }
            }
            catch { /* Process not found */ }

            return IntPtr.Zero;
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

            switch (state.ToLower())
            {
                case "minimize": ShowWindow(hWnd, 6); break;
                case "maximize": ShowWindow(hWnd, 3); break;
                case "restore": ShowWindow(hWnd, 9); break;
                default: return $"Invalid state: {state}. Use minimize, maximize, or restore.";
            }
            return $"Set {state}: {title}";
        }

        private static string MoveWindow_Win(string title, int x, int y)
        {
            var hWnd = FindWindowByTitle_Win(title);
            if (hWnd == IntPtr.Zero) return $"Window not found: {title}";

            SetWindowPos(hWnd, IntPtr.Zero, x, y, 0, 0, 0x0001 | 0x0004); // SWP_NOSIZE | SWP_NOZORDER
            return $"Moved: {title} to ({x}, {y})";
        }

        private static string ResizeWindow_Win(string title, int w, int h)
        {
            var hWnd = FindWindowByTitle_Win(title);
            if (hWnd == IntPtr.Zero) return $"Window not found: {title}";

            SetWindowPos(hWnd, IntPtr.Zero, 0, 0, w, h, 0x0002 | 0x0004); // SWP_NOMOVE | SWP_NOZORDER
            return $"Resized: {title} to {w}x{h}";
        }

        private static string GetForegroundWindowTitle_Win()
        {
            var hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero) return "";
            var sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, 256);
            return sb.ToString();
        }

        private static string SetWindowTopmost_Win(string title, bool topmost)
        {
            var hWnd = FindWindowByTitle_Win(title);
            if (hWnd == IntPtr.Zero) return $"Window not found: {title}";

            IntPtr HWND_TOPMOST = new IntPtr(-1);
            IntPtr HWND_NOTOPMOST = new IntPtr(-2);

            SetWindowPos(hWnd, topmost ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, 0x0001 | 0x0002);
            return $"Set topmost {topmost}: {title}";
        }

        private static string GetWindowPosition_Win(string title)
        {
            var hWnd = FindWindowByTitle_Win(title);
            if (hWnd == IntPtr.Zero) return "Window not found";

            if (GetWindowRect(hWnd, out RECT rect))
            {
                return $"X={rect.Left}, Y={rect.Top}, W={rect.Right - rect.Left}, H={rect.Bottom - rect.Top}";
            }
            return "Failed to get rect";
        }

        // ─── Input (Windows) ───

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);
        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

        private static string SendKeys_Win(string keys)
        {
            try
            {
                // Handle special key sequences like SharpRPA's SendKeys
                var specialKeys = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase)
                {
                    { "{ENTER}", 0x0D }, { "{TAB}", 0x09 }, { "{ESC}", 0x1B },
                    { "{DELETE}", 0x2E }, { "{BACKSPACE}", 0x08 }, { "{BS}", 0x08 },
                    { "{UP}", 0x26 }, { "{DOWN}", 0x28 }, { "{LEFT}", 0x25 }, { "{RIGHT}", 0x27 },
                    { "{HOME}", 0x24 }, { "{END}", 0x23 },
                    { "{PGUP}", 0x21 }, { "{PGDN}", 0x22 },
                    { "{F1}", 0x70 }, { "{F2}", 0x71 }, { "{F3}", 0x72 }, { "{F4}", 0x73 },
                    { "{F5}", 0x74 }, { "{F6}", 0x75 }, { "{F7}", 0x76 }, { "{F8}", 0x77 },
                    { "{F9}", 0x78 }, { "{F10}", 0x79 }, { "{F11}", 0x7A }, { "{F12}", 0x7B },
                    { "{INSERT}", 0x2D }, { "{INS}", 0x2D },
                    { "{CAPSLOCK}", 0x14 }, { "{NUMLOCK}", 0x90 }, { "{SCROLLLOCK}", 0x91 },
                    { "{PRTSC}", 0x2C },
                };

                int i = 0;
                while (i < keys.Length)
                {
                    if (keys[i] == '{')
                    {
                        int end = keys.IndexOf('}', i);
                        if (end > i)
                        {
                            string token = keys.Substring(i, end - i + 1);
                            if (specialKeys.TryGetValue(token, out byte vk))
                            {
                                byte scan = (byte)MapVirtualKey(vk, 0);
                                keybd_event(vk, scan, 0, UIntPtr.Zero);
                                keybd_event(vk, scan, KEYEVENTF_KEYUP, UIntPtr.Zero);
                            }
                            i = end + 1;
                            continue;
                        }
                    }

                    // Modifier keys: + (Shift), ^ (Ctrl), % (Alt)
                    if (keys[i] == '+' || keys[i] == '^' || keys[i] == '%')
                    {
                        byte modVk = keys[i] == '+' ? (byte)0x10 : keys[i] == '^' ? (byte)0x11 : (byte)0x12;
                        keybd_event(modVk, 0, 0, UIntPtr.Zero);
                        if (i + 1 < keys.Length)
                        {
                            i++;
                            SendSingleChar(keys[i]);
                        }
                        keybd_event(modVk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                        i++;
                        continue;
                    }

                    SendSingleChar(keys[i]);
                    i++;
                }
                return $"Sent keys: {keys}";
            }
            catch (Exception ex)
            {
                return $"SendKeys error: {ex.Message}";
            }
        }

        private static void SendSingleChar(char c)
        {
            short vkResult = VkKeyScan(c);
            byte vk = (byte)(vkResult & 0xFF);
            bool needShift = (vkResult & 0x100) != 0;

            if (needShift)
                keybd_event(0x10, 0, 0, UIntPtr.Zero); // VK_SHIFT down

            byte scan = (byte)MapVirtualKey(vk, 0);
            keybd_event(vk, scan, 0, UIntPtr.Zero);
            keybd_event(vk, scan, KEYEVENTF_KEYUP, UIntPtr.Zero);

            if (needShift)
                keybd_event(0x10, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // VK_SHIFT up
        }

        private static string MouseClick_Win(int x, int y, string clickType)
        {
            SetCursorPos(x, y);
            System.Threading.Thread.Sleep(50);
            uint down = clickType.ToLower() == "right" ? 0x0008u : clickType.ToLower() == "middle" ? 0x0020u : 0x0002u;
            uint up = clickType.ToLower() == "right" ? 0x0010u : clickType.ToLower() == "middle" ? 0x0040u : 0x0004u;
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

        // ─── UI Automation (Windows) ── COM UIAutomation P/Invoke ───
        // References SharpRPA's ThickAppClickItemCommand / ThickAppGetTextCommand patterns
        // using COM UIAutomation (UIAutomationClient.dll) instead of managed System.Windows.Automation

        // COM CLSIDs & IIDs
        private static readonly Guid CLSID_CUIAutomation = new Guid("ff48dba4-60ef-4201-aa87-54103eef594e");
        private static readonly Guid IID_IUIAutomation = new Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee");

        // UIAutomation Property IDs
        private const int UIA_NamePropertyId = 30005;
        private const int UIA_AutomationIdPropertyId = 30011;
        private const int UIA_ControlTypePropertyId = 30003;
        private const int UIA_ButtonControlTypeId = 50000;
        private const int UIA_LocalizedControlTypePropertyId = 30004;

        // UIAutomation Pattern IDs
        private const int UIA_InvokePatternId = 10000;
        private const int UIA_ValuePatternId = 10002;

        // TreeScope
        private const int TreeScope_Children = 0x2;
        private const int TreeScope_Descendants = 0x4;

        [DllImport("ole32.dll")]
        private static extern int CoCreateInstance(
            [In] ref Guid rclsid, IntPtr pUnkOuter, uint dwClsContext,
            [In] ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);

        // ─── COM Interface declarations for UIAutomation ───

        [ComImport, Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IUIAutomation
        {
            // Method 0: CompareElements
            int CompareElements(object el1, object el2);
            // Method 1: CompareRuntimeIds
            int CompareRuntimeIds(object runtimeId1, object runtimeId2);
            // Method 2: GetRootElement
            IUIAutomationElement GetRootElement();
            // Method 3: ElementFromHandle
            IUIAutomationElement ElementFromHandle(IntPtr hwnd);
            // Method 4: ElementFromPoint
            IUIAutomationElement ElementFromPoint(tagPOINT pt);
            // Method 5: GetFocusedElement
            IUIAutomationElement GetFocusedElement();
            // Method 6: GetRootElementBuildCache
            IUIAutomationElement GetRootElementBuildCache(object cacheRequest);
            // Method 7: ElementFromHandleBuildCache
            IUIAutomationElement ElementFromHandleBuildCache(IntPtr hwnd, object cacheRequest);
            // Method 8: ElementFromPointBuildCache
            IUIAutomationElement ElementFromPointBuildCache(tagPOINT pt, object cacheRequest);
            // Method 9: GetFocusedElementBuildCache
            IUIAutomationElement GetFocusedElementBuildCache(object cacheRequest);
            // Method 10: CreateTreeWalker
            object CreateTreeWalker(IUIAutomationCondition pCondition);
            // Method 11: ControlViewWalker
            object ControlViewWalker { get; }
            // Method 12: ContentViewWalker
            object ContentViewWalker { get; }
            // Method 13: RawViewWalker
            object RawViewWalker { get; }
            // Method 14: RawViewCondition
            IUIAutomationCondition RawViewCondition { get; }
            // Method 15: ControlViewCondition
            IUIAutomationCondition ControlViewCondition { get; }
            // Method 16: ContentViewCondition
            IUIAutomationCondition ContentViewCondition { get; }
            // Method 17: CreateCacheRequest
            object CreateCacheRequest();
            // Method 18: CreateTrueCondition
            IUIAutomationCondition CreateTrueCondition();
            // Method 19: CreateFalseCondition
            IUIAutomationCondition CreateFalseCondition();
            // Method 20: CreatePropertyCondition
            IUIAutomationCondition CreatePropertyCondition(int propertyId, object value);
            // Method 21: CreatePropertyConditionEx
            IUIAutomationCondition CreatePropertyConditionEx(int propertyId, object value, int flags);
            // Method 22: CreateAndCondition
            IUIAutomationCondition CreateAndCondition(IUIAutomationCondition condition1, IUIAutomationCondition condition2);
        }

        [ComImport, Guid("d22108aa-8ac5-49a5-837b-37bbb3d7591e")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IUIAutomationElement
        {
            // Method 0: SetFocus
            void SetFocus();
            // Method 1: GetRuntimeId
            int[] GetRuntimeId();
            // Method 2: FindFirst
            IUIAutomationElement FindFirst(int scope, IUIAutomationCondition condition);
            // Method 3: FindAll
            IUIAutomationElementArray FindAll(int scope, IUIAutomationCondition condition);
            // Method 4: FindFirstBuildCache
            IUIAutomationElement FindFirstBuildCache(int scope, IUIAutomationCondition condition, object cacheRequest);
            // Method 5: FindAllBuildCache
            IUIAutomationElementArray FindAllBuildCache(int scope, IUIAutomationCondition condition, object cacheRequest);
            // Method 6: BuildUpdatedCache
            IUIAutomationElement BuildUpdatedCache(object cacheRequest);
            // Method 7: GetCurrentPropertyValue
            object GetCurrentPropertyValue(int propertyId);
            // Method 8: GetCurrentPropertyValueEx
            object GetCurrentPropertyValueEx(int propertyId, int ignoreDefaultValue);
            // Method 9: GetCachedPropertyValue
            object GetCachedPropertyValue(int propertyId);
            // Method 10: GetCachedPropertyValueEx
            object GetCachedPropertyValueEx(int propertyId, int ignoreDefaultValue);
            // Method 11: QueryInterface pattern
            IntPtr GetCurrentPatternAs(int patternId, ref Guid riid);
            // Method 12: GetCachedPatternAs
            IntPtr GetCachedPatternAs(int patternId, ref Guid riid);
            // Method 13: GetCurrentPattern
            [return: MarshalAs(UnmanagedType.IUnknown)]
            object GetCurrentPattern(int patternId);
            // Method 14-16: CachedPattern and extra
            [return: MarshalAs(UnmanagedType.IUnknown)]
            object GetCachedPattern(int patternId);
            IUIAutomationElement GetCachedParent();
            IUIAutomationElementArray GetCachedChildren();
            // Method 17+: Current properties - we use GetCurrentPropertyValue instead
        }

        [ComImport, Guid("14314595-b4bc-4055-95f2-58f2e42c9855")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IUIAutomationElementArray
        {
            int Length { get; }
            IUIAutomationElement GetElement(int index);
        }

        [ComImport, Guid("352ffba8-0973-437c-a61f-f64cafd81df9")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IUIAutomationCondition { }

        [ComImport, Guid("fb377fbe-8ea6-46d5-9c73-6499642d3059")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IUIAutomationInvokePattern
        {
            void Invoke();
        }

        [ComImport, Guid("a94cd8b1-0844-4cd6-9d2d-640537ab39e9")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IUIAutomationValuePattern
        {
            void SetValue([MarshalAs(UnmanagedType.BStr)] string val);
            string CurrentValue { [return: MarshalAs(UnmanagedType.BStr)] get; }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct tagPOINT { public int x, y; }

        [DllImport("user32.dll")]
        private static extern bool GetPhysicalCursorPos(out tagPOINT lpPoint);

        // ─── UIAutomation helper: create COM automation object ───

        private static IUIAutomation? _uiAutomation;
        private static readonly object _uiaLock = new object();

        private static IUIAutomation GetUIAutomation()
        {
            if (_uiAutomation == null)
            {
                lock (_uiaLock)
                {
                    if (_uiAutomation == null)
                    {
                        var clsid = CLSID_CUIAutomation;
                        var iid = IID_IUIAutomation;
                        int hr = CoCreateInstance(ref clsid, IntPtr.Zero, 1 /*CLSCTX_INPROC_SERVER*/ | 4 /*CLSCTX_LOCAL_SERVER*/,
                            ref iid, out object obj);
                        if (hr != 0)
                            throw new COMException($"Failed to create CUIAutomation instance. HRESULT: 0x{hr:X8}", hr);
                        _uiAutomation = (IUIAutomation)obj;
                    }
                }
            }
            return _uiAutomation;
        }

        /// <summary>
        /// Finds a top-level window element by title (or fallback logic)
        /// Uses FindWindowByTitle_Win to get the HWND, then ElementFromHandle.
        /// </summary>
        private static IUIAutomationElement? FindWindowElement(string windowTitle)
        {
            try
            {
                var hwnd = FindWindowByTitle_Win(windowTitle);
                if (hwnd == IntPtr.Zero) return null;

                var uia = GetUIAutomation();
                return uia.ElementFromHandle(hwnd);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PlatformAutomation] FindWindowElement error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets all UI elements in a window.
        /// Mirrors SharpRPA's ThickAppClickItemCommand.FindHandleObjects():
        ///   automationElement.FindAll(TreeScope.Descendants, TrueCondition)
        /// </summary>
        private static List<string> GetUIElements_Win(string windowTitle)
        {
            var result = new List<string>();
            try
            {
                var windowEl = FindWindowElement(windowTitle);
                if (windowEl == null) return new List<string> { $"Window not found: {windowTitle}" };

                var uia = GetUIAutomation();
                var trueCond = uia.CreateTrueCondition();
                var allElements = windowEl.FindAll(TreeScope_Descendants, trueCond);

                if (allElements != null)
                {
                    for (int i = 0; i < allElements.Length; i++)
                    {
                        try
                        {
                            var el = allElements.GetElement(i);
                            var name = el.GetCurrentPropertyValue(UIA_NamePropertyId) as string ?? "";
                            var controlType = el.GetCurrentPropertyValue(UIA_LocalizedControlTypePropertyId) as string ?? "";

                            if (!string.IsNullOrWhiteSpace(name))
                                result.Add($"{controlType}: {name}");
                        }
                        catch { /* skip inaccessible elements */ }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Add($"Error: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// Gets all buttons in a window.
        /// Like SharpRPA's FindHandleObjects but filtered to ControlType.Button.
        /// </summary>
        private static List<string> GetUIButtons_Win(string windowTitle)
        {
            var result = new List<string>();
            try
            {
                var windowEl = FindWindowElement(windowTitle);
                if (windowEl == null) return result;

                var uia = GetUIAutomation();
                var buttonCond = uia.CreatePropertyCondition(UIA_ControlTypePropertyId, UIA_ButtonControlTypeId);
                var buttons = windowEl.FindAll(TreeScope_Descendants, buttonCond);

                if (buttons != null)
                {
                    for (int i = 0; i < buttons.Length; i++)
                    {
                        try
                        {
                            var el = buttons.GetElement(i);
                            var name = el.GetCurrentPropertyValue(UIA_NamePropertyId) as string ?? "";
                            if (!string.IsNullOrWhiteSpace(name))
                                result.Add(name);
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PlatformAutomation] GetUIButtons_Win error: {ex.Message}");
            }
            return result.Distinct().ToList();
        }

        /// <summary>
        /// Clicks a UI element by name (like SharpRPA's ThickAppClickItemCommand.RunCommand):
        ///   1. Find element by name
        ///   2. GetClickablePoint
        ///   3. SetCursorPos + mouse_event
        /// </summary>
        private static string ClickUIElement_Win(string windowTitle, string elementName)
        {
            try
            {
                var windowEl = FindWindowElement(windowTitle);
                if (windowEl == null) return $"Window not found: {windowTitle}";

                var uia = GetUIAutomation();
                var cond = uia.CreatePropertyCondition(UIA_NamePropertyId, elementName);
                var el = windowEl.FindFirst(TreeScope_Descendants, cond);
                if (el == null) return $"Element not found: {elementName}";

                // Activate the window first (like SharpRPA does)
                ActivateWindow_Win(windowTitle);
                System.Threading.Thread.Sleep(200);

                // Try to get clickable point via bounding rectangle
                try
                {
                    // UIA_BoundingRectanglePropertyId = 30001
                    var rectObj = el.GetCurrentPropertyValue(30001);
                    if (rectObj is double[] rectArr && rectArr.Length == 4)
                    {
                        int cx = (int)(rectArr[0] + rectArr[2] / 2);
                        int cy = (int)(rectArr[1] + rectArr[3] / 2);
                        SetCursorPos(cx, cy);
                        System.Threading.Thread.Sleep(50);
                        mouse_event(0x0002, cx, cy, 0, 0); // LEFTDOWN
                        mouse_event(0x0004, cx, cy, 0, 0); // LEFTUP
                        return $"Clicked: {elementName} at ({cx}, {cy})";
                    }
                }
                catch { }

                // Fallback: try InvokePattern
                try
                {
                    var pattern = (IUIAutomationInvokePattern)el.GetCurrentPattern(UIA_InvokePatternId);
                    pattern.Invoke();
                    return $"Invoked click: {elementName}";
                }
                catch { }

                return $"Could not click element: {elementName} (no clickable point or invoke pattern)";
            }
            catch (Exception ex)
            {
                return $"ClickUIElement error: {ex.Message}";
            }
        }

        /// <summary>
        /// Invokes a button by name using InvokePattern.
        /// Mirrors SharpRPA's InvokePattern.Invoke() approach.
        /// </summary>
        private static string InvokeUIButton_Win(string windowTitle, string buttonName)
        {
            try
            {
                var windowEl = FindWindowElement(windowTitle);
                if (windowEl == null) return $"Window not found: {windowTitle}";

                var uia = GetUIAutomation();

                // Find button by name + ControlType.Button
                var nameCond = uia.CreatePropertyCondition(UIA_NamePropertyId, buttonName);
                var typeCond = uia.CreatePropertyCondition(UIA_ControlTypePropertyId, UIA_ButtonControlTypeId);
                var andCond = uia.CreateAndCondition(nameCond, typeCond);

                var el = windowEl.FindFirst(TreeScope_Descendants, andCond);

                // Fallback: search by name only if button type didn't match
                if (el == null)
                    el = windowEl.FindFirst(TreeScope_Descendants, nameCond);

                if (el == null) return $"Button not found: {buttonName}";

                // Activate window first
                ActivateWindow_Win(windowTitle);
                System.Threading.Thread.Sleep(100);

                try
                {
                    var pattern = (IUIAutomationInvokePattern)el.GetCurrentPattern(UIA_InvokePatternId);
                    pattern.Invoke();
                    return $"Invoked: {buttonName}";
                }
                catch
                {
                    // Fallback: click via bounding rect
                    try
                    {
                        var rectObj = el.GetCurrentPropertyValue(30001); // BoundingRectangle
                        if (rectObj is double[] rectArr && rectArr.Length == 4)
                        {
                            int cx = (int)(rectArr[0] + rectArr[2] / 2);
                            int cy = (int)(rectArr[1] + rectArr[3] / 2);
                            SetCursorPos(cx, cy);
                            System.Threading.Thread.Sleep(50);
                            mouse_event(0x0002, cx, cy, 0, 0);
                            mouse_event(0x0004, cx, cy, 0, 0);
                            return $"Clicked button: {buttonName} at ({cx}, {cy})";
                        }
                    }
                    catch { }
                    return $"Could not invoke button: {buttonName}";
                }
            }
            catch (Exception ex)
            {
                return $"InvokeUIButton error: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets the text/value of a UI element.
        /// Like SharpRPA's ThickAppGetTextCommand: searches by AutomationId or Name,
        /// then gets Current.Name or ValuePattern.Value.
        /// </summary>
        private static string GetUIElementText_Win(string windowTitle, string elementId)
        {
            try
            {
                var windowEl = FindWindowElement(windowTitle);
                if (windowEl == null) return $"Window not found: {windowTitle}";

                var uia = GetUIAutomation();
                IUIAutomationElement? el = null;

                // Try by AutomationId first (like SharpRPA's ThickAppGetTextCommand)
                try
                {
                    var cond = uia.CreatePropertyCondition(UIA_AutomationIdPropertyId, elementId);
                    el = windowEl.FindFirst(TreeScope_Descendants, cond);
                }
                catch { }

                // Fallback: try by Name
                if (el == null)
                {
                    var cond = uia.CreatePropertyCondition(UIA_NamePropertyId, elementId);
                    el = windowEl.FindFirst(TreeScope_Descendants, cond);
                }

                if (el == null) return "";

                // Try ValuePattern first (for text boxes, etc.)
                try
                {
                    var valuePattern = (IUIAutomationValuePattern)el.GetCurrentPattern(UIA_ValuePatternId);
                    return valuePattern.CurrentValue ?? "";
                }
                catch { }

                // Fallback: return Name property
                var name = el.GetCurrentPropertyValue(UIA_NamePropertyId) as string;
                return name ?? "";
            }
            catch (Exception ex)
            {
                return $"GetUIElementText error: {ex.Message}";
            }
        }

        // ─── Clipboard (Windows) ───

        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);
        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardData(uint uFormat);
        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();
        [DllImport("user32.dll")]
        private static extern bool EmptyClipboard();
        [DllImport("user32.dll")]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);
        [DllImport("kernel32.dll")]
        private static extern bool GlobalUnlock(IntPtr hMem);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);
        [DllImport("kernel32.dll")]
        private static extern UIntPtr GlobalSize(IntPtr hMem);

        private const uint CF_UNICODETEXT = 13;
        private const uint GMEM_MOVEABLE = 0x0002;

        private static string GetClipboardText_Win()
        {
            string result = "";
            if (OpenClipboard(IntPtr.Zero))
            {
                var hData = GetClipboardData(CF_UNICODETEXT);
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
            try
            {
                if (!OpenClipboard(IntPtr.Zero))
                    return "Failed to open clipboard";

                EmptyClipboard();
                var bytes = (text.Length + 1) * 2; // Unicode: 2 bytes per char + null terminator
                var hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes);
                if (hGlobal == IntPtr.Zero)
                {
                    CloseClipboard();
                    return "Failed to allocate global memory";
                }

                var lpData = GlobalLock(hGlobal);
                Marshal.Copy(text.ToCharArray(), 0, lpData, text.Length);
                // Write null terminator
                Marshal.WriteInt16(lpData + text.Length * 2, 0);
                GlobalUnlock(hGlobal);

                SetClipboardData(CF_UNICODETEXT, hGlobal);
                CloseClipboard();
                return "Clipboard set";
            }
            catch (Exception ex)
            {
                try { CloseClipboard(); } catch { }
                return $"SetClipboard error: {ex.Message}";
            }
        }

        // ─── Screenshot (Windows) ── GDI P/Invoke ───

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);
        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest,
            IntPtr hdcSrc, int xSrc, int ySrc, uint rop);
        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);
        [DllImport("gdi32.dll")]
        private static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint uStartScan, uint cScanLines,
            byte[] lpvBits, ref BITMAPINFO lpbi, uint uUsage);
        [DllImport("gdi32.dll")]
        private static extern int GetObject(IntPtr hObject, int nCount, ref BITMAP lpObject);

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAP
        {
            public int bmType, bmWidth, bmHeight, bmWidthBytes;
            public short bmPlanes, bmBitsPixel;
            public IntPtr bmBits;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth, biHeight;
            public ushort biPlanes, biBitCount;
            public uint biCompression, biSizeImage;
            public int biXPelsPerMeter, biYPelsPerMeter;
            public uint biClrUsed, biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            // bmiColors placeholder (not needed for 32-bit)
        }

        private const uint SRCCOPY = 0x00CC0020;
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        private static string CaptureScreenRegionToBmp(int x, int y, int w, int h, string filePath)
        {
            IntPtr hdcScreen = GetDC(IntPtr.Zero);
            IntPtr hdcMem = CreateCompatibleDC(hdcScreen);
            IntPtr hBitmap = CreateCompatibleBitmap(hdcScreen, w, h);
            IntPtr hOld = SelectObject(hdcMem, hBitmap);

            BitBlt(hdcMem, 0, 0, w, h, hdcScreen, x, y, SRCCOPY);

            SelectObject(hdcMem, hOld);

            // Get bitmap data
            var bmpInfo = new BITMAPINFO();
            bmpInfo.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            bmpInfo.bmiHeader.biWidth = w;
            bmpInfo.bmiHeader.biHeight = -h; // top-down
            bmpInfo.bmiHeader.biPlanes = 1;
            bmpInfo.bmiHeader.biBitCount = 32;
            bmpInfo.bmiHeader.biCompression = 0; // BI_RGB

            int stride = ((w * 32 + 31) / 32) * 4;
            int imageSize = stride * h;
            byte[] pixels = new byte[imageSize];

            GetDIBits(hdcMem, hBitmap, 0, (uint)h, pixels, ref bmpInfo, 0);

            // Write BMP file
            using (var fs = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
            using (var bw = new System.IO.BinaryWriter(fs))
            {
                int fileHeaderSize = 14;
                int infoHeaderSize = 40;
                int totalSize = fileHeaderSize + infoHeaderSize + imageSize;

                // BMP file header
                bw.Write((byte)'B');
                bw.Write((byte)'M');
                bw.Write(totalSize);
                bw.Write((short)0); // reserved1
                bw.Write((short)0); // reserved2
                bw.Write(fileHeaderSize + infoHeaderSize); // offset to pixel data

                // BMP info header (top-down, so biHeight is negative)
                bw.Write(infoHeaderSize); // biSize
                bw.Write(w); // biWidth
                bw.Write(-h); // biHeight (negative = top-down)
                bw.Write((short)1); // biPlanes
                bw.Write((short)32); // biBitCount
                bw.Write(0); // biCompression = BI_RGB
                bw.Write(imageSize); // biSizeImage
                bw.Write(0); // biXPelsPerMeter
                bw.Write(0); // biYPelsPerMeter
                bw.Write(0); // biClrUsed
                bw.Write(0); // biClrImportant

                bw.Write(pixels);
            }

            // Cleanup GDI
            DeleteObject(hBitmap);
            DeleteDC(hdcMem);
            ReleaseDC(IntPtr.Zero, hdcScreen);

            return filePath;
        }

        private static string TakeScreenshot_Win(string filePath)
        {
            try
            {
                int screenW = GetSystemMetrics(SM_CXSCREEN);
                int screenH = GetSystemMetrics(SM_CYSCREEN);
                CaptureScreenRegionToBmp(0, 0, screenW, screenH, filePath);
                return $"Screenshot: {filePath}";
            }
            catch (Exception ex)
            {
                return $"Screenshot error: {ex.Message}";
            }
        }

        private static string TakeRegionScreenshot_Win(int x, int y, int w, int h, string filePath)
        {
            try
            {
                CaptureScreenRegionToBmp(x, y, w, h, filePath);
                return $"Region screenshot: {filePath}";
            }
            catch (Exception ex)
            {
                return $"Region screenshot error: {ex.Message}";
            }
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
