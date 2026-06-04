// SPDX-License-Identifier: MPL-2.0

using System.Windows.Input;
using Holo.Configuration.Keybinds;

namespace Holo.Models;

/// <summary>
/// Metadata about a command
/// </summary>
public class CommandMetadata
{
    /// <summary>
    /// Command instance
    /// </summary>
    public required ICommand Command { get; init; }

    /// <summary>
    /// Qualified name of the command
    /// </summary>
    public required string QualifiedName { get; init; }

    /// <summary>
    /// Default keybind used to invoke the command
    /// </summary>
    public required string? DefaultKey { get; init; }

    /// <summary>
    /// Default context the keybind is active in
    /// </summary>
    public required KeybindContext DefaultContext { get; init; }

    /// <summary>
    /// If the command should be shown in the command palette
    /// </summary>
    public required bool PaletteEnabled { get; init; }
}
