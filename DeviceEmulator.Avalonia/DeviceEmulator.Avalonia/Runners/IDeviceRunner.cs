using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeviceEmulator.Models;
using DeviceEmulator.Scripting;

namespace DeviceEmulator.Runners
{
    /// <summary>
    /// Interface for device communication runners.
    /// Each runner handles a specific communication protocol (Serial, TCP, etc.)
    /// </summary>
    public interface IDeviceRunner : IDisposable
    {
        /// <summary>
        /// Configuration for this device.
        /// </summary>
        DeviceConfig Config { get; }

        /// <summary>
        /// Whether the device is currently running/listening.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Shared global variables from the interactive console.
        /// </summary>
        SharedDictionary? Globals { get; set; }

        /// <summary>
        /// Raised when a message is received from the external application.
        /// </summary>
        event Action<string> MessageReceived;

        /// <summary>
        /// Raised when a response is sent back to the external application.
        /// </summary>
        event Action<string> MessageSent;

        /// <summary>
        /// Raised when an error occurs during communication.
        /// </summary>
        event Action<Exception> ErrorOccurred;

        /// <summary>
        /// Raised when the running state changes.
        /// </summary>
        event Action<bool> RunningStateChanged;

        /// <summary>
        /// Raised when logging is needed.
        /// </summary>
        event Action<string> LogMessage;

        /// <summary>
        /// Starts the device communication on a background thread.
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Stops the device communication.
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Updates the script used for generating responses.
        /// </summary>
        /// <param name="script">C# script code</param>
        /// <param name="enableDebugging">Whether to enable debugging</param>
        /// <returns>True if script compiled successfully</returns>
        bool UpdateScript(string script, bool enableDebugging);

        /// <summary>
        /// Gets compilation error messages, if any.
        /// </summary>
        string GetCompilationErrors();
    }
}
