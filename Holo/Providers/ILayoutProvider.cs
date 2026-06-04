// SPDX-License-Identifier: MPL-2.0

using Holo.Models;

namespace Holo.Providers;

/// <summary>
/// Provides methods for managing UI layouts
/// </summary>
public interface ILayoutProvider
{
    /// <summary>
    /// The current layout
    /// </summary>
    Layout? Current { get; set; }

    /// <summary>
    /// List of available layouts
    /// </summary>
    AssCS.Utilities.ReadOnlyObservableCollection<Layout> Layouts { get; }

    /// <summary>
    /// Reload layouts from disk
    /// </summary>
    void Reload();

    /// <summary>
    /// Event fired when changing layouts
    /// </summary>
    /// <remarks>Also fired when reloading completes</remarks>
    event EventHandler<LayoutChangedEventArgs>? LayoutChanged;

    /// <summary>
    /// EventArgs for the <see cref="ILayoutProvider.LayoutChanged"/> event
    /// </summary>
    /// <param name="layout">The new selected layout</param>
    public class LayoutChangedEventArgs(Layout? layout) : EventArgs
    {
        /// <summary>
        /// The new selected layout
        /// </summary>
        public Layout? Layout { get; } = layout;
    }
}
