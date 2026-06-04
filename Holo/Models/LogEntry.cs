// SPDX-License-Identifier: MPL-2.0

using Microsoft.Extensions.Logging;

namespace Holo.Models;

/// <summary>
/// A logger entry
/// </summary>
/// <param name="Level">Log level</param>
/// <param name="Timestamp">Log timestamp</param>
/// <param name="Message">Log message</param>
public record LogEntry(LogLevel Level, DateTimeOffset Timestamp, string Message);
