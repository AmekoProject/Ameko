// SPDX-License-Identifier: MPL-2.0

namespace Holo.Configuration.Keybinds;

/// <summary>
/// Provides methods for registering, retrieving,
/// and managing keybinds within specific contexts
/// </summary>
public interface IKeybindRegistrar
{
    /// <summary>
    /// Registers a new keybind or replaces an existing one with the same name
    /// </summary>
    /// <param name="keybind">The <see cref="Keybind"/> to register</param>
    bool RegisterKeybind(Keybind keybind);

    /// <summary>
    /// Registers new keybinds
    /// </summary>
    /// <param name="keybinds">The <see cref="Keybind"/>s to register</param>
    /// <param name="save">Whether to save after adding</param>
    bool RegisterKeybinds(IList<Keybind> keybinds, bool save);

    /// <summary>
    /// Removes the keybind associated with the specified name
    /// </summary>
    /// <param name="qualifiedName">The keybind name</param>
    bool DeregisterKeybind(string qualifiedName);

    /// <summary>
    /// Apply an override on an existing keybind
    /// </summary>
    /// <param name="qualifiedName">Name of the keybind to override</param>
    /// <param name="keybind">Keybind to override the default with</param>
    /// <param name="context">Context to set</param>
    /// <param name="isEnabled">If the keybind is enabled</param>
    /// <returns><see langword="true"/> if successful</returns>
    bool ApplyOverride(
        string qualifiedName,
        string? keybind,
        KeybindContext? context,
        bool? isEnabled
    );

    /// <summary>
    /// Clears overrides from a keybind
    /// </summary>
    /// <param name="qualifiedName">Name of the keybind to clear overrides from</param>
    /// <returns><see langword="true"/> if successful</returns>
    bool ClearOverride(string qualifiedName);

    /// <summary>
    /// Clears overrides from all keybinds, resetting to default
    /// </summary>
    /// <returns><see langword="true"/> if successful</returns>
    bool ClearOverrides();

    /// <summary>
    /// Retrieves the keybind registered under the specified name.
    /// </summary>
    /// <param name="qualifiedName">The keybind name to look up</param>
    /// <returns>The associated <see cref="Keybind"/>, or <see langword="null" /> if not found</returns>
    Keybind? GetKeybind(string qualifiedName);

    /// <summary>
    /// Attempts to retrieve the keybind registered under the specified keybind name
    /// </summary>
    /// <param name="qualifiedName">The keybind name to look up</param>
    /// <param name="keybind">The keybind, if found, otherwise, <see langword="null" /></param>
    /// <returns><see langword="true" /> if the keybind was found</returns>
    bool TryGetKeybind(string qualifiedName, out Keybind? keybind);

    /// <summary>
    /// Determines whether a keybind is registered under the specified name.
    /// </summary>
    /// <param name="qualifiedName">The keybind name to check</param>
    /// <returns><see langword="true" /> if a keybind is registered to the given name</returns>
    bool IsKeybindRegistered(string qualifiedName);

    /// <summary>
    /// Retrieves all keybinds
    /// </summary>
    /// <returns>Keybinds</returns>
    IEnumerable<Keybind> GetKeybinds();

    /// <summary>
    /// Retrieves all keybinds that match the specified context filter
    /// </summary>
    /// <param name="filter">A combination of <see cref="KeybindContext"/> flags used to filter keybinds</param>
    /// <returns>Keybinds that are active in any of the specified contexts</returns>
    IEnumerable<Keybind> GetKeybinds(KeybindContext filter);

    /// <summary>
    /// Retrieves all keybinds that have been overridden
    /// </summary>
    /// <returns>Keybinds that have been overridden</returns>
    IEnumerable<Keybind> GetOverridenKeybinds();

    /// <summary>
    /// Save the current keybinds to disk
    /// </summary>
    void Save();

    /// <summary>
    /// Load keybinds from disk
    /// </summary>
    void Parse();

    /// <summary>
    /// Raised when a keybind property is modified
    /// </summary>
    event EventHandler<EventArgs>? KeybindsChanged;
}
