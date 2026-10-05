// SPDX-License-Identifier: MPL-2.0

using Holo.Models;
using Microsoft.Extensions.Logging;

namespace Holo.Scripting;

/// <summary>
/// Wrapper around <see cref="ILogger"/> for capturing logs emitted during script execution
/// </summary>
/// <param name="base">Base logger</param>
public class HoloLogger(ILogger @base)
{
    private readonly List<LogEntry> _logs = [];

    /// <summary>
    /// Get the list of emitted logs and reset the logger for the next run
    /// </summary>
    /// <returns>List of logs</returns>
    public IReadOnlyCollection<LogEntry> GetLogsAndReset()
    {
        var result = _logs.AsReadOnly();
        _logs.Clear();
        return result;
    }

    /// <summary>
    /// Log a message with <see cref="LogLevel.Trace"/>
    /// </summary>
    /// <param name="message">Message to log</param>
    public void LogTrace(string message)
    {
        _logs.Add(new LogEntry(LogLevel.Trace, DateTimeOffset.Now, message));
        @base.LogTrace("{HoloScriptMessage}", message);
    }

    /// <summary>
    /// Log a message with <see cref="LogLevel.Debug"/>
    /// </summary>
    /// <param name="message">Message to log</param>
    public void LogDebug(string message)
    {
        _logs.Add(new LogEntry(LogLevel.Debug, DateTimeOffset.Now, message));
        @base.LogDebug("{HoloScriptMessage}", message);
    }

    /// <summary>
    /// Log a message with <see cref="LogLevel.Information"/>
    /// </summary>
    /// <param name="message">Message to log</param>
    public void LogInformation(string message)
    {
        _logs.Add(new LogEntry(LogLevel.Information, DateTimeOffset.Now, message));
        @base.LogInformation("{HoloScriptMessage}", message);
    }

    /// <summary>
    /// Log a message with <see cref="LogLevel.Warning"/>
    /// </summary>
    /// <param name="message">Message to log</param>
    public void LogWarning(string message)
    {
        _logs.Add(new LogEntry(LogLevel.Warning, DateTimeOffset.Now, message));
        @base.LogWarning("{HoloScriptMessage}", message);
    }

    /// <summary>
    /// Log a message with <see cref="LogLevel.Error"/>
    /// </summary>
    /// <param name="message">Message to log</param>
    public void LogError(string message)
    {
        _logs.Add(new LogEntry(LogLevel.Error, DateTimeOffset.Now, message));
        @base.LogError("{HoloScriptMessage}", message);
    }

    /// <summary>
    /// Log a message with <see cref="LogLevel.Critical"/>
    /// </summary>
    /// <param name="message">Message to log</param>
    public void LogCritical(string message)
    {
        _logs.Add(new LogEntry(LogLevel.Critical, DateTimeOffset.Now, message));
        @base.LogCritical("{HoloScriptMessage}", message);
    }
}
