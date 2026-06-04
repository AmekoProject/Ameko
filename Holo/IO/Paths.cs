// SPDX-License-Identifier: MPL-2.0

namespace Holo.IO;

/// <summary>
/// Paths to specific files
/// </summary>
public static class Paths
{
    /// <summary>
    /// Default file location for the application configuration
    /// </summary>
    public static Uri Configuration => new(Path.Combine(Directories.ConfigHome, "config.json"));

    /// <summary>
    /// Default file location for application persistence
    /// </summary>
    public static Uri Persistence => new(Path.Combine(Directories.ConfigHome, "persist.json"));

    /// <summary>
    /// Default file location for globally-accessible objects
    /// </summary>
    public static Uri Globals => new(Path.Combine(Directories.ConfigHome, "globals.json"));

    /// <summary>
    /// Default file location for keybinds
    /// </summary>
    public static Uri Keybinds => new(Path.Combine(Directories.ConfigHome, "keybinds.json"));
}
