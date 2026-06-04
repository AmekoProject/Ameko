// SPDX-License-Identifier: MPL-2.0

using Holo.Models;

namespace Holo.Providers;

using AssCS.Utilities;

/// <summary>
/// Provides access to application logs
/// </summary>
public interface ILogProvider
{
    /// <summary>
    /// Observable collection of formatted log entries
    /// </summary>
    public ReadOnlyObservableCollection<LogEntry> LogEntries { get; }
}
