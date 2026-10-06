// SPDX-License-Identifier: MPL-2.0

using System.Collections.ObjectModel;
using Holo.Models;
using Microsoft.Extensions.Logging;

namespace Holo.Scripting;

/// <summary>
/// Wrapper around <see cref="ILogger"/> for capturing logs emitted during script execution
/// </summary>
public class HoloLogger
{
    private readonly ILogger _base;
    private readonly ObservableCollection<LogEntry> _logs = [];

    /// <summary>
    /// Observable collection of emitted logs
    /// </summary>
    public AssCS.Utilities.ReadOnlyObservableCollection<LogEntry> Logs { get; }

    /// <summary>
    /// Reset the logger for the next run
    /// </summary>
    public void Reset()
    {
        _logs.Clear();
    }

    /// <summary>
    /// Log a message with <see cref="LogLevel.Trace"/>
    /// </summary>
    /// <param name="message">Message to log</param>
    public void LogTrace(string message)
    {
        _logs.Add(new LogEntry(LogLevel.Trace, DateTimeOffset.Now, message));
        _base.LogTrace("{HoloScriptMessage}", message);
    }

    /// <summary>
    /// Log a message with <see cref="LogLevel.Debug"/>
    /// </summary>
    /// <param name="message">Message to log</param>
    public void LogDebug(string message)
    {
        _logs.Add(new LogEntry(LogLevel.Debug, DateTimeOffset.Now, message));
        _base.LogDebug("{HoloScriptMessage}", message);
    }

    /// <summary>
    /// Log a message with <see cref="LogLevel.Information"/>
    /// </summary>
    /// <param name="message">Message to log</param>
    public void LogInformation(string message)
    {
        _logs.Add(new LogEntry(LogLevel.Information, DateTimeOffset.Now, message));
        _base.LogInformation("{HoloScriptMessage}", message);
    }

    /// <summary>
    /// Log a message with <see cref="LogLevel.Warning"/>
    /// </summary>
    /// <param name="message">Message to log</param>
    public void LogWarning(string message)
    {
        _logs.Add(new LogEntry(LogLevel.Warning, DateTimeOffset.Now, message));
        _base.LogWarning("{HoloScriptMessage}", message);
    }

    /// <summary>
    /// Log a message with <see cref="LogLevel.Error"/>
    /// </summary>
    /// <param name="message">Message to log</param>
    public void LogError(string message)
    {
        _logs.Add(new LogEntry(LogLevel.Error, DateTimeOffset.Now, message));
        _base.LogError("{HoloScriptMessage}", message);
    }

    /// <summary>
    /// Log an exception
    /// </summary>
    /// <param name="ex">Exception to log</param>
    public void LogError(Exception ex)
    {
        _logs.Add(new LogEntry(LogLevel.Error, DateTimeOffset.Now, ex.ToString()));
        _base.LogError("{HoloScriptMessage}", ex.ToString());
    }

    /// <summary>
    /// Log an exception
    /// </summary>
    /// <param name="ex">Exception to log</param>
    /// <param name="message">Message to log</param>
    public void LogError(Exception ex, string message)
    {
        _logs.Add(new LogEntry(LogLevel.Error, DateTimeOffset.Now, message));
        _logs.Add(new LogEntry(LogLevel.Error, DateTimeOffset.Now, ex.ToString()));
        _base.LogError("{HoloScriptMessage}", message);
        _base.LogError("{HoloScriptMessage}", ex.ToString());
    }

    /// <summary>
    /// Log a message with <see cref="LogLevel.Critical"/>
    /// </summary>
    /// <param name="message">Message to log</param>
    public void LogCritical(string message)
    {
        _logs.Add(new LogEntry(LogLevel.Critical, DateTimeOffset.Now, message));
        _base.LogCritical("{HoloScriptMessage}", message);
    }

    /// <summary>
    /// Initialize the logger
    /// </summary>
    /// <param name="base">Base logger</param>
    public HoloLogger(ILogger @base)
    {
        _base = @base;
        Logs = new AssCS.Utilities.ReadOnlyObservableCollection<LogEntry>(_logs);
    }
}
