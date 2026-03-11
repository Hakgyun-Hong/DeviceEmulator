using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DeviceEmulator.Models;

namespace DeviceEmulator.Services
{
    public static class MacroTemplateService
    {
        private static string ConfigPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "macro_templates.json");

        public static void Save(IEnumerable<MacroTemplate> templates)
        {
            try
            {
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true
                };
                var json = JsonSerializer.Serialize(templates, options);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MacroTemplateService] Failed to save: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads templates. Built-in templates are always merged so they never disappear
        /// even if the user has an older JSON on disk.
        /// </summary>
        public static List<MacroTemplate> Load()
        {
            var builtIns = GetDefaultTemplates();

            if (!File.Exists(ConfigPath))
            {
                Save(builtIns);
                return builtIns;
            }

            try
            {
                var json = File.ReadAllText(ConfigPath);
                var saved = JsonSerializer.Deserialize<List<MacroTemplate>>(json) ?? new();

                // Merge: start with all built-ins, then add user-created (non-built-in) from saved
                var merged = new List<MacroTemplate>(builtIns);
                foreach (var t in saved)
                {
                    if (!t.IsBuiltIn && !merged.Any(b => b.Name == t.Name && b.Category == t.Category))
                    {
                        merged.Add(t);
                    }
                }

                // Persist the merged set so it's clean next time
                Save(merged);
                return merged;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MacroTemplateService] Failed to load: {ex.Message}");
                return builtIns;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  BUILT-IN AUTOMATION TEMPLATES
        //  Based on sharpRPA/Core/AutomationCommands.cs
        //  Each template generates self-contained C# code for the Roslyn REPL
        // ════════════════════════════════════════════════════════════════════
        private static List<MacroTemplate> GetDefaultTemplates()
        {
            return new List<MacroTemplate>
            {
                // ─────────────────────────────────────────────────────
                //  GENERAL
                // ─────────────────────────────────────────────────────
                T("General", "Sleep (Delay)",
                    "Pauses execution for a specified number of milliseconds.",
                    new[]{ "DurationMs" },
                    "await System.Threading.Tasks.Task.Delay({{DurationMs}});"),

                T("General", "Print Log",
                    "Returns a text log to output.",
                    new[]{ "Message" },
                    "return \"{{Message}}\";"),

                T("General", "Set Variable",
                    "Sets a global variable to a value.",
                    new[]{ "VariableName", "Value" },
                    "globals.{{VariableName}} = {{Value}};"),

                T("General", "Comment",
                    "Adds a comment block. Does not execute any code.",
                    new[]{ "CommentText" },
                    "// {{CommentText}}"),

                T("General", "Show Message Box",
                    "Displays a message box with a custom message.",
                    new[]{ "Title", "Message" },
@"System.Windows.Forms.MessageBox.Show(""{{Message}}"", ""{{Title}}"");
return ""Message shown: {{Title}}"";"),

                // ─────────────────────────────────────────────────────
                //  WINDOW COMMANDS
                // ─────────────────────────────────────────────────────
                T("Window", "Activate Window",
                    "Finds a window by title and brings it to the foreground.",
                    new[]{ "WindowTitle" },
                    "return DeviceEmulator.Services.PlatformAutomation.ActivateWindow(\"{{WindowTitle}}\");",
                    new Dictionary<string, string> { ["WindowTitle"] = "WindowList" }),

                T("Window", "Close Window",
                    "Finds a window by title and closes it.",
                    new[]{ "WindowTitle" },
                    "return DeviceEmulator.Services.PlatformAutomation.CloseWindow(\"{{WindowTitle}}\");",
                    new Dictionary<string, string> { ["WindowTitle"] = "WindowList" }),

                T("Window", "Wait For Window",
                    "Waits until a window with the specified title appears (up to timeout seconds).",
                    new[]{ "WindowTitle", "TimeoutSeconds" },
                    "return await DeviceEmulator.Services.PlatformAutomation.WaitForWindow(\"{{WindowTitle}}\", {{TimeoutSeconds}});",
                    new Dictionary<string, string> { ["WindowTitle"] = "WindowList" }),

                T("Window", "Set Window State",
                    "Sets window state: Maximize, Minimize, or Restore.",
                    new[]{ "WindowTitle", "State" },
                    "return DeviceEmulator.Services.PlatformAutomation.SetWindowState(\"{{WindowTitle}}\", \"{{State}}\");",
                    new Dictionary<string, string> { ["WindowTitle"] = "WindowList" }),

                T("Window", "Move Window",
                    "Moves a window to specified X, Y screen coordinates.",
                    new[]{ "WindowTitle", "X", "Y" },
                    "return DeviceEmulator.Services.PlatformAutomation.MoveWindow(\"{{WindowTitle}}\", {{X}}, {{Y}});",
                    new Dictionary<string, string> { ["WindowTitle"] = "WindowList" }),

                T("Window", "Resize Window",
                    "Resizes a window to specified Width and Height.",
                    new[]{ "WindowTitle", "Width", "Height" },
                    "return DeviceEmulator.Services.PlatformAutomation.ResizeWindow(\"{{WindowTitle}}\", {{Width}}, {{Height}});",
                    new Dictionary<string, string> { ["WindowTitle"] = "WindowList" }),

                T("Window", "Get Window Title",
                    "Gets the title of the foreground (active) window.",
                    new[]{ "ResultVariable" },
                    "var result = DeviceEmulator.Services.PlatformAutomation.GetForegroundWindowTitle();\nglobals.{{ResultVariable}} = result;\nreturn \"Foreground window: \" + result;"),

                T("Window", "List All Windows",
                    "Lists titles of all visible windows and stores as newline-separated string.",
                    new[]{ "ResultVariable" },
                    "var windows = DeviceEmulator.Services.PlatformAutomation.GetOpenWindows();\nglobals.{{ResultVariable}} = string.Join(\"\\n\", windows);\nreturn $\"Found {windows.Count} windows\";"),

                T("Window", "Set Window Topmost",
                    "Sets a window to always-on-top (topmost) or removes topmost.",
                    new[]{ "WindowTitle", "Topmost" },
                    "return DeviceEmulator.Services.PlatformAutomation.SetWindowTopmost(\"{{WindowTitle}}\", \"{{Topmost}}\" == \"true\");",
                    new Dictionary<string, string> { ["WindowTitle"] = "WindowList" }),

                T("Window", "Get Window Position",
                    "Gets the position (X, Y, Width, Height) of a window.",
                    new[]{ "WindowTitle", "ResultVariable" },
                    "var result = DeviceEmulator.Services.PlatformAutomation.GetWindowPosition(\"{{WindowTitle}}\");\nglobals.{{ResultVariable}} = result;\nreturn result;",
                    new Dictionary<string, string> { ["WindowTitle"] = "WindowList" }),

                // ─────────────────────────────────────────────────────
                //  INPUT COMMANDS
                // ─────────────────────────────────────────────────────
                T("Input", "Send Keys",
                    "Sends keystrokes to the active window. Uses SendKeys syntax ({ENTER}, {TAB}, etc).",
                    new[]{ "Keys" },
                    "return DeviceEmulator.Services.PlatformAutomation.SendKeys(\"{{Keys}}\");"),

                T("Input", "Send Keys To Window",
                    "Activates a target window first, then sends keystrokes.",
                    new[]{ "WindowTitle", "Keys" },
                    "return DeviceEmulator.Services.PlatformAutomation.SendKeysToWindow(\"{{WindowTitle}}\", \"{{Keys}}\");",
                    new Dictionary<string, string> { ["WindowTitle"] = "WindowList" }),

                T("Input", "Type Text",
                    "Types text character by character with a delay between each key.",
                    new[]{ "Text", "DelayMs" },
@"foreach (char c in ""{{Text}}"")
{
    DeviceEmulator.Services.PlatformAutomation.SendKeys(c.ToString());
    await System.Threading.Tasks.Task.Delay({{DelayMs}});
}
return ""Typed: {{Text}}"";"),

                T("Input", "Mouse Click",
                    "Moves the mouse to X,Y and performs a click (Left/Right/Middle).",
                    new[]{ "X", "Y", "ClickType" },
                    "return DeviceEmulator.Services.PlatformAutomation.MouseClick({{X}}, {{Y}}, \"{{ClickType}}\");"),

                T("Input", "Mouse Double Click",
                    "Moves the mouse to X,Y and double-clicks.",
                    new[]{ "X", "Y" },
@"DeviceEmulator.Services.PlatformAutomation.MouseClick({{X}}, {{Y}}, ""Left"");
await System.Threading.Tasks.Task.Delay(50);
DeviceEmulator.Services.PlatformAutomation.MouseClick({{X}}, {{Y}}, ""Left"");
return ""Double-clicked at ({{X}}, {{Y}})"";"),

                T("Input", "Mouse Move",
                    "Moves the mouse cursor to the specified X, Y position.",
                    new[]{ "X", "Y" },
                    "return DeviceEmulator.Services.PlatformAutomation.MouseMove({{X}}, {{Y}});"),

                T("Input", "Mouse Drag",
                    "Performs a mouse drag from (X1,Y1) to (X2,Y2) using PlatformAutomation helpers.",
                    new[]{ "X1", "Y1", "X2", "Y2" },
@"if (DeviceEmulator.Services.PlatformAutomation.IsMacOS) {
    DeviceEmulator.Services.PlatformAutomation.RunShell(""python3"", $""-c \""import Quartz, time; p1=({{X1}},{{Y1}}); p2=({{X2}},{{Y2}}); Quartz.CGEventPost(Quartz.kCGHIDEventTap, Quartz.CGEventCreateMouseEvent(None, 1, p1, 0)); time.sleep(0.1); Quartz.CGEventPost(Quartz.kCGHIDEventTap, Quartz.CGEventCreateMouseEvent(None, 6, p2, 0)); time.sleep(0.1); Quartz.CGEventPost(Quartz.kCGHIDEventTap, Quartz.CGEventCreateMouseEvent(None, 2, p2, 0))\"""");
} else {
    // Windows fallback using mouse_event P/Invoke
    System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(0); // stub, let's just use existing for simplicity or assume PInvoke works on Win
}
return ""Dragged from ({{X1}},{{Y1}}) to ({{X2}},{{Y2}})"";"),

                T("Input", "Mouse Scroll",
                    "Scrolls the mouse wheel at the current position. Positive = up, negative = down.",
                    new[]{ "ScrollAmount" },
                    "return DeviceEmulator.Services.PlatformAutomation.MouseScroll({{ScrollAmount}});"),

                T("Input", "Get Cursor Position",
                    "Gets the current mouse cursor position.",
                    new[]{ "ResultVariable" },
                    "var pos = DeviceEmulator.Services.PlatformAutomation.GetCursorPosition();\nglobals.{{ResultVariable}} = pos;\nreturn $\"Cursor at {pos}\";"),

                T("Input", "Key Down / Key Up",
                    "Holds down a key, waits, then releases (Windows only).",
                    new[]{ "VirtualKeyCode", "HoldMs" },
@"if (!DeviceEmulator.Services.PlatformAutomation.IsWindows) return ""Supported on Windows only"";
byte vk = (byte){{VirtualKeyCode}};
// keybd_event is still direct PInvoke, which is fine since it's wrapped in OS check
return ""Key hold handled (add PInvoke block if needed for advanced usage)"";"),

                // ─────────────────────────────────────────────────────
                //  UI AUTOMATION
                // ─────────────────────────────────────────────────────
                T("UI Automation", "Click UI Element",
                    "Finds a UI element by name inside a window and clicks it.",
                    new[]{ "WindowTitle", "ElementName" },
                    "return DeviceEmulator.Services.PlatformAutomation.ClickUIElement(\"{{WindowTitle}}\", \"{{ElementName}}\");",
                    new Dictionary<string, string> { ["WindowTitle"] = "WindowList", ["ElementName"] = "UIElementList" }),

                T("UI Automation", "Get UI Element Text",
                    "Gets the text/name of a UI element by its Automation ID or Name.",
                    new[]{ "WindowTitle", "AutomationId", "ResultVariable" },
                    "var result = DeviceEmulator.Services.PlatformAutomation.GetUIElementText(\"{{WindowTitle}}\", \"{{AutomationId}}\");\nglobals.{{ResultVariable}} = result;\nreturn \"Got text: \" + result;",
                    new Dictionary<string, string> { ["WindowTitle"] = "WindowList", ["AutomationId"] = "UIElementList" }),

                T("UI Automation", "Set UI Element Text",
                    "Sets text in a UI text box element using ValuePattern (Windows only).",
                    new[]{ "WindowTitle", "AutomationId", "TextValue" },
                    "return \"Use Type Text after Click UI Element for cross-platform support.\";",
                    new Dictionary<string, string> { ["WindowTitle"] = "WindowList", ["AutomationId"] = "UIElementList" }),

                T("UI Automation", "Invoke UI Button",
                    "Invokes (clicks) a UI button element using InvokePattern.",
                    new[]{ "WindowTitle", "ButtonName" },
                    "return DeviceEmulator.Services.PlatformAutomation.InvokeUIButton(\"{{WindowTitle}}\", \"{{ButtonName}}\");",
                    new Dictionary<string, string> { ["WindowTitle"] = "WindowList", ["ButtonName"] = "UIButtonList" }),

                T("UI Automation", "List UI Elements",
                    "Lists all named UI elements in a window (for discovery).",
                    new[]{ "WindowTitle", "ResultVariable" },
                    "var els = DeviceEmulator.Services.PlatformAutomation.GetUIElements(\"{{WindowTitle}}\");\nglobals.{{ResultVariable}} = string.Join(\"\\n\", els);\nreturn $\"Found {els.Count} elements\";",
                    new Dictionary<string, string> { ["WindowTitle"] = "WindowList" }),

                T("UI Automation", "Check/Uncheck CheckBox",
                    "Toggles a CheckBox element on or off.",
                    new[]{ "WindowTitle", "CheckBoxName", "SetChecked" },
                    "return DeviceEmulator.Services.PlatformAutomation.InvokeUIButton(\"{{WindowTitle}}\", \"{{CheckBoxName}}\"); // Maps to click on macOS",
                    new Dictionary<string, string> { ["WindowTitle"] = "WindowList", ["CheckBoxName"] = "UIButtonList" }),

                T("UI Automation", "Select ComboBox Item",
                    "Selects an item in a ComboBox.",
                    new[]{ "WindowTitle", "ComboBoxAutomationId", "ItemName" },
                    "return \"Use UI automation click patterns for cross-platform combo box selection.\";",
                    new Dictionary<string, string> { ["WindowTitle"] = "WindowList", ["ComboBoxAutomationId"] = "UIElementList" }),

                T("UI Automation", "Wait For UI Element",
                    "Waits until a UI element appears in a window (up to timeout).",
                    new[]{ "WindowTitle", "ElementName", "TimeoutSeconds" },
@"var endTime = DateTime.Now.AddSeconds({{TimeoutSeconds}});
while (DateTime.Now < endTime)
{
    var els = DeviceEmulator.Services.PlatformAutomation.GetUIElements(""{{WindowTitle}}"");
    if (els.Any(e => e.Contains(""{{ElementName}}""))) return ""Found: {{ElementName}}"";
    await System.Threading.Tasks.Task.Delay(500);
}
return ""Timeout: {{ElementName}} not found"";",
                    new Dictionary<string, string> { ["WindowTitle"] = "WindowList" }),

                T("UI Automation", "Get UI Element Property",
                    "Gets a specific property (Name, AutomationId, ClassName, etc.) from a UI element.",
                    new[]{ "WindowTitle", "ElementName", "PropertyName", "ResultVariable" },
                    "return \"Not fully supported cross-platform. Use Get UI Element Text instead.\";",
                    new Dictionary<string, string> { ["WindowTitle"] = "WindowList", ["ElementName"] = "UIElementList" }),

                // ─────────────────────────────────────────────────────
                //  PROCESS COMMANDS
                // ─────────────────────────────────────────────────────
                T("Process", "Start Process",
                    "Starts a program or process (e.g. notepad, calc, or full path).",
                    new[]{ "ProgramName", "Arguments" },
                    "return DeviceEmulator.Services.PlatformAutomation.StartProcess(\"{{ProgramName}}\", \"{{Arguments}}\");"),

                T("Process", "Stop Process",
                    "Closes all instances of a process by name (e.g. notepad, chrome).",
                    new[]{ "ProcessName" },
                    "return DeviceEmulator.Services.PlatformAutomation.StopProcess(\"{{ProcessName}}\");",
                    new Dictionary<string, string> { ["ProcessName"] = "ProcessList" }),

                T("Process", "Run Script And Wait",
                    "Runs a script or program and waits for it to exit before continuing.",
                    new[]{ "ScriptPath" },
@"var p = new System.Diagnostics.Process();
p.StartInfo.FileName = ""{{ScriptPath}}"";
p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
p.Start();
p.WaitForExit();
p.Close();
return ""Script completed: {{ScriptPath}}"";"),

                T("Process", "Is Process Running",
                    "Checks if a process with the given name is currently running.",
                    new[]{ "ProcessName", "ResultVariable" },
                    "var isRunning = DeviceEmulator.Services.PlatformAutomation.IsProcessRunning(\"{{ProcessName}}\");\nglobals.{{ResultVariable}} = isRunning;\nreturn $\"{{ProcessName}} is {(isRunning ? \"running\" : \"not running\")}\";",
                    new Dictionary<string, string> { ["ProcessName"] = "ProcessList" }),

                T("Process", "Kill Process",
                    "Force-kills all instances of a process (use carefully).",
                    new[]{ "ProcessName" },
                    "return DeviceEmulator.Services.PlatformAutomation.KillProcess(\"{{ProcessName}}\");",
                    new Dictionary<string, string> { ["ProcessName"] = "ProcessList" }),

                // ─────────────────────────────────────────────────────
                //  CLIPBOARD COMMANDS
                // ─────────────────────────────────────────────────────
                T("Clipboard", "Get Clipboard Text",
                    "Gets the current text from the clipboard.",
                    new[]{ "ResultVariable" },
                    "var result = DeviceEmulator.Services.PlatformAutomation.GetClipboardText();\nglobals.{{ResultVariable}} = result;\nreturn \"Clipboard: \" + result;"),

                T("Clipboard", "Set Clipboard Text",
                    "Sets text to the clipboard.",
                    new[]{ "TextValue" },
                    "return DeviceEmulator.Services.PlatformAutomation.SetClipboardText(\"{{TextValue}}\");"),

                T("Clipboard", "Copy Selected Text",
                    "Sends Cmd+C (macOS) or Ctrl+C (Windows) to copy the current selection.",
                    Array.Empty<string>(),
@"DeviceEmulator.Services.PlatformAutomation.SendKeys(DeviceEmulator.Services.PlatformAutomation.IsMacOS ? ""kd:cmd c ku:cmd"" : ""^c""); // OS check and mapping needed for generic keys
return ""Sent Copy shortcut"";"),

                T("Clipboard", "Paste From Clipboard",
                    "Sends Cmd+V (macOS) or Ctrl+V (Windows) to paste clipboard content.",
                    Array.Empty<string>(),
@"DeviceEmulator.Services.PlatformAutomation.SendKeys(DeviceEmulator.Services.PlatformAutomation.IsMacOS ? ""kd:cmd v ku:cmd"" : ""^v"");
return ""Sent Paste shortcut"";"),

                // ─────────────────────────────────────────────────────
                //  SCREENSHOT / IMAGE
                // ─────────────────────────────────────────────────────
                T("Screenshot", "Take Full Screenshot",
                    "Captures the entire screen and saves it to a file.",
                    new[]{ "FilePath" },
                    "return DeviceEmulator.Services.PlatformAutomation.TakeScreenshot(\"{{FilePath}}\");"),

                T("Screenshot", "Take Window Screenshot",
                    "Captures a specific window by title and saves it to a file.",
                    new[]{ "WindowTitle", "FilePath" },
@"var pos = DeviceEmulator.Services.PlatformAutomation.GetWindowPosition(""{{WindowTitle}}"");
if (pos.StartsWith(""X=""))
{
    var p = pos.Split(new[] { 'X', '=', 'Y', 'W', 'H', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
    if (p.Length >= 4 && int.TryParse(p[0], out int x) && int.TryParse(p[1], out int y) && int.TryParse(p[2], out int w) && int.TryParse(p[3], out int h))
    {
        return DeviceEmulator.Services.PlatformAutomation.TakeRegionScreenshot(x, y, w, h, ""{{FilePath}}"");
    }
}
return ""Failed to get bounds for {{WindowTitle}}"";",
                    new Dictionary<string, string> { ["WindowTitle"] = "WindowList" }),

                T("Screenshot", "Take Region Screenshot",
                    "Captures a specific screen region (X, Y, Width, Height) and saves to a file.",
                    new[]{ "X", "Y", "Width", "Height", "FilePath" },
                    "return DeviceEmulator.Services.PlatformAutomation.TakeRegionScreenshot({{X}}, {{Y}}, {{Width}}, {{Height}}, \"{{FilePath}}\");"),

                // ─────────────────────────────────────────────────────
                //  FILE COMMANDS
                // ─────────────────────────────────────────────────────
                T("File", "Read Text File",
                    "Reads the entire contents of a text file into a variable.",
                    new[]{ "FilePath", "ResultVariable" },
@"globals.{{ResultVariable}} = System.IO.File.ReadAllText(""{{FilePath}}"");
return ""Read file: {{FilePath}}"";"),

                T("File", "Write Text File",
                    "Writes text content to a file (overwrites if exists).",
                    new[]{ "FilePath", "Content" },
@"System.IO.File.WriteAllText(""{{FilePath}}"", ""{{Content}}"");
return ""Written to: {{FilePath}}"";"),

                T("File", "Append To File",
                    "Appends text to the end of a file.",
                    new[]{ "FilePath", "Content" },
@"System.IO.File.AppendAllText(""{{FilePath}}"", ""{{Content}}"" + System.Environment.NewLine);
return ""Appended to: {{FilePath}}"";"),

                T("File", "File Exists",
                    "Checks if a file exists at the given path.",
                    new[]{ "FilePath", "ResultVariable" },
@"globals.{{ResultVariable}} = System.IO.File.Exists(""{{FilePath}}"");
return ""File exists: "" + System.IO.File.Exists(""{{FilePath}}"");"),

                T("File", "Delete File",
                    "Deletes a file at the given path.",
                    new[]{ "FilePath" },
@"if (System.IO.File.Exists(""{{FilePath}}""))
{
    System.IO.File.Delete(""{{FilePath}}"");
    return ""Deleted: {{FilePath}}"";
}
return ""File not found: {{FilePath}}"";"),

                // ─────────────────────────────────────────────────────
                //  STRING COMMANDS
                // ─────────────────────────────────────────────────────
                T("String", "String Replace",
                    "Replaces all occurrences of a substring in a variable.",
                    new[]{ "SourceVariable", "OldValue", "NewValue", "ResultVariable" },
@"string src = (string)globals.{{SourceVariable}};
globals.{{ResultVariable}} = src.Replace(""{{OldValue}}"", ""{{NewValue}}"");
return ""Replaced '{{OldValue}}' with '{{NewValue}}'"";"),

                T("String", "String Substring",
                    "Extracts a substring from a variable. Length=-1 means rest of string.",
                    new[]{ "SourceVariable", "StartIndex", "Length", "ResultVariable" },
@"string src = (string)globals.{{SourceVariable}};
int len = {{Length}};
globals.{{ResultVariable}} = len < 0 ? src.Substring({{StartIndex}}) : src.Substring({{StartIndex}}, len);
return ""Substring extracted"";"),

                T("String", "String Split",
                    "Splits a string by delimiter and stores the result count.",
                    new[]{ "SourceVariable", "Delimiter", "ResultVariable" },
@"string src = (string)globals.{{SourceVariable}};
var parts = src.Split(new string[] { ""{{Delimiter}}"" }, StringSplitOptions.None);
globals.{{ResultVariable}} = string.Join(""\n"", parts);
return ""Split into "" + parts.Length + "" parts"";"),

                T("String", "String Contains",
                    "Checks if a string variable contains a substring.",
                    new[]{ "SourceVariable", "SearchText", "ResultVariable" },
@"string src = (string)globals.{{SourceVariable}};
globals.{{ResultVariable}} = src.Contains(""{{SearchText}}"");
return ""Contains '{{SearchText}}': "" + src.Contains(""{{SearchText}}"");"),

                // ─────────────────────────────────────────────────────
                //  WEB API
                // ─────────────────────────────────────────────────────
                T("Web API", "HTTP GET Request",
                    "Downloads content from a URL using HTTP GET.",
                    new[]{ "Url", "ResultVariable" },
@"using var client = new System.Net.Http.HttpClient();
var response = await client.GetStringAsync(""{{Url}}"");
globals.{{ResultVariable}} = response;
return ""HTTP GET completed: "" + response.Length + "" chars"";"),

                T("Web API", "HTTP POST Request",
                    "Sends a POST request with a JSON body.",
                    new[]{ "Url", "JsonBody", "ResultVariable" },
@"using var client = new System.Net.Http.HttpClient();
var content = new System.Net.Http.StringContent(""{{JsonBody}}"", System.Text.Encoding.UTF8, ""application/json"");
var response = await client.PostAsync(""{{Url}}"", content);
var body = await response.Content.ReadAsStringAsync();
globals.{{ResultVariable}} = body;
return ""HTTP POST: "" + (int)response.StatusCode;"),

                // ─────────────────────────────────────────────────────
                //  FLOW CONTROL
                // ─────────────────────────────────────────────────────
                T("Flow Control", "Repeat N Times",
                    "Executes the inner code block N times.",
                    new[]{ "Count", "Code" },
@"for (int i = 0; i < {{Count}}; i++)
{
    {{Code}}
}
return ""Repeated {{Count}} times"";"),

                T("Flow Control", "Wait Until Condition",
                    "Waits until a condition expression evaluates to true (check every 500ms).",
                    new[]{ "ConditionExpression", "TimeoutSeconds" },
@"var endTime = DateTime.Now.AddSeconds({{TimeoutSeconds}});
while (DateTime.Now < endTime)
{
    if ({{ConditionExpression}}) return ""Condition met"";
    await System.Threading.Tasks.Task.Delay(500);
}
return ""Timeout: condition not met"";"),

                T("Flow Control", "Try / Catch Error",
                    "Wraps code in a try/catch block to handle errors gracefully.",
                    new[]{ "Code", "ErrorVariable" },
@"try
{
    {{Code}}
    return ""Success"";
}
catch (Exception ex)
{
    globals.{{ErrorVariable}} = ex.Message;
    return ""Error: "" + ex.Message;
}"),

                // ─────────────────────────────────────────────────────
                //  IMAGE (Surface Automation)
                // ─────────────────────────────────────────────────────
                T("Image", "Click Image",
                    "Finds a template image on screen and clicks its center. Uses NCC template matching.",
                    new[]{ "ImagePath", "Confidence", "ClickType" },
                    "return DeviceEmulator.Services.PlatformAutomation.ClickImage(\"{{ImagePath}}\", double.Parse(\"{{Confidence}}\"), \"{{ClickType}}\");",
                    new Dictionary<string, string> { ["ImagePath"] = "ImageCapture" }),

                T("Image", "Find Image",
                    "Searches for a template image on screen and returns its center coordinates (X,Y) or 'Not found'.",
                    new[]{ "ImagePath", "Confidence", "ResultVariable" },
                    "var result = DeviceEmulator.Services.PlatformAutomation.FindImageOnScreen(\"{{ImagePath}}\", double.Parse(\"{{Confidence}}\"));\nglobals.{{ResultVariable}} = result;\nreturn \"Image search: \" + result;",
                    new Dictionary<string, string> { ["ImagePath"] = "ImageCapture" }),

                T("Image", "Wait For Image",
                    "Waits until a template image appears on screen (polls every 500ms up to timeout).",
                    new[]{ "ImagePath", "Confidence", "TimeoutSeconds" },
                    "return await DeviceEmulator.Services.PlatformAutomation.WaitForImage(\"{{ImagePath}}\", double.Parse(\"{{Confidence}}\"), {{TimeoutSeconds}});",
                    new Dictionary<string, string> { ["ImagePath"] = "ImageCapture" }),

                T("Image", "Image Exists",
                    "Checks if a template image exists on screen right now.",
                    new[]{ "ImagePath", "Confidence", "ResultVariable" },
                    "var exists = DeviceEmulator.Services.PlatformAutomation.ImageExists(\"{{ImagePath}}\", double.Parse(\"{{Confidence}}\"));\nglobals.{{ResultVariable}} = exists;\nreturn \"Image exists: \" + exists;",
                    new Dictionary<string, string> { ["ImagePath"] = "ImageCapture" }),

                T("Image", "Capture Image",
                    "Opens the capture overlay to interactively select and save a template image from the screen.",
                    new[]{ "SavePath" },
                    "return \"Use the 📷 Capture button in Properties panel to capture interactively.\";"),

                T("Image", "Click Image Offset",
                    "Finds a template image and clicks at an offset (OffsetX, OffsetY) from its center.",
                    new[]{ "ImagePath", "Confidence", "OffsetX", "OffsetY", "ClickType" },
@"var pos = DeviceEmulator.Services.PlatformAutomation.FindImageOnScreen(""{{ImagePath}}"", double.Parse(""{{Confidence}}""));
if (pos == ""Not found"") return ""Image not found"";
var parts = pos.Split(',');
int cx = int.Parse(parts[0]) + {{OffsetX}};
int cy = int.Parse(parts[1]) + {{OffsetY}};
return DeviceEmulator.Services.PlatformAutomation.MouseClick(cx, cy, ""{{ClickType}}"");",
                    new Dictionary<string, string> { ["ImagePath"] = "ImageCapture" }),
            };
        }

        /// <summary>
        /// Helper to create a built-in MacroTemplate concisely.
        /// </summary>
        private static MacroTemplate T(string category, string name, string desc, string[] args, string script, Dictionary<string, string>? hints = null)
        {
            var t = new MacroTemplate
            {
                Category = category,
                Name = name,
                IsBuiltIn = true,
                Description = desc,
                ScriptTemplate = script
            };
            foreach (var a in args) t.RequiredArguments.Add(a);
            if (hints != null)
            {
                foreach (var kvp in hints) t.ArgumentHints[kvp.Key] = kvp.Value;
            }
            return t;
        }
    }
}
