// SPDX-License-Identifier: MPL-2.0

using Holo.Configuration.Keybinds;

namespace Holo.Configuration;

/// <summary>
/// Attribute identifying commands
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class CommandAttribute : Attribute
{
    /// <summary>
    /// Qualified name of the command
    /// </summary>
    public string QualifiedName { get; }

    /// <summary>
    /// Default keybind used to invoke the command
    /// </summary>
    public string? DefaultKey { get; }

    /// <summary>
    /// Default context the keybind is active in
    /// </summary>
    public KeybindContext DefaultContext { get; }

    /// <summary>
    /// If the command should be shown in the command palette
    /// </summary>
    public bool PaletteEnabled { get; }

    /// <summary>
    /// Denotes a command with a default keybind assigned
    /// </summary>
    /// <param name="qualifiedName">Keybind identifier</param>
    /// <param name="defaultKey">Default keybind</param>
    /// <param name="defaultContext">Default keybind context</param>
    /// <param name="paletteEnabled"><see langword="true"/> if the command should be accessible from the Command Palette</param>
    public CommandAttribute(
        string qualifiedName,
        string defaultKey,
        KeybindContext defaultContext,
        bool paletteEnabled = true
    )
    {
        QualifiedName = qualifiedName;
        DefaultKey = defaultKey;
        DefaultContext = defaultContext;
        PaletteEnabled = paletteEnabled;
    }

    /// <summary>
    /// Denotes a command without a default keybind assigned
    /// </summary>
    /// <param name="qualifiedName">Keybind identifier</param>
    /// <param name="defaultContext">Default keybind context</param>
    /// <param name="paletteEnabled"><see langword="true"/> if the command should be accessible from the Command Palette</param>
    public CommandAttribute(
        string qualifiedName,
        KeybindContext defaultContext,
        bool paletteEnabled = true
    )
    {
        QualifiedName = qualifiedName;
        DefaultContext = defaultContext;
        PaletteEnabled = paletteEnabled;
    }
}
