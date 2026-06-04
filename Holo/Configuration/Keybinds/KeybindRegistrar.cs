// SPDX-License-Identifier: MPL-2.0

using System.Collections.Concurrent;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Holo.IO;
using Microsoft.Extensions.Logging;

namespace Holo.Configuration.Keybinds;

/// <summary>
/// Keybind registration and handling
/// </summary>
public class KeybindRegistrar(IFileSystem fileSystem, ILogger<KeybindRegistrar> logger)
    : IKeybindRegistrar
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>
    /// The filesystem being used
    /// </summary>
    private readonly IFileSystem _fileSystem = fileSystem;

    /// <summary>
    /// Keybind map
    /// </summary>
    private readonly ConcurrentDictionary<string, Keybind> _keybinds = new();

    /// <inheritdoc />
    public bool RegisterKeybind(Keybind keybind)
    {
        var result = _keybinds.TryAdd(keybind.QualifiedName, keybind);
        if (result)
            KeybindsChanged?.Invoke(this, EventArgs.Empty);
        return result;
    }

    /// <inheritdoc />
    public bool RegisterKeybinds(IList<Keybind> keybinds, bool save)
    {
        var changed = false;
        foreach (var keybind in keybinds)
        {
            if (_keybinds.TryAdd(keybind.QualifiedName, keybind))
                changed = true;
        }

        if (changed)
            KeybindsChanged?.Invoke(this, EventArgs.Empty);

        if (save && changed)
            Save();
        return changed;
    }

    /// <inheritdoc />
    public bool DeregisterKeybind(string qualifiedName)
    {
        var result = _keybinds.TryRemove(qualifiedName, out _);
        if (result)
            KeybindsChanged?.Invoke(this, EventArgs.Empty);
        return result;
    }

    /// <inheritdoc />
    public bool ApplyOverride(
        string qualifiedName,
        string? keybind,
        KeybindContext? context,
        bool? isEnabled
    )
    {
        if (!_keybinds.TryGetValue(qualifiedName, out var target))
        {
            logger.LogError("Could not find keybind {QualifiedName}", qualifiedName);
            return false;
        }

        var changed = false;

        if (keybind is not null && keybind != target.Key)
        {
            changed = true;
            target.OverrideKey = keybind;
        }

        if (context is not null && context != target.Context)
        {
            changed = true;
            target.OverrideContext = context.Value;
        }

        if (isEnabled.HasValue && isEnabled.Value != target.IsEnabled)
        {
            changed = true;
            target.IsEnabled = isEnabled.Value;
        }

        if (changed)
            KeybindsChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <inheritdoc />
    public bool ClearOverride(string qualifiedName)
    {
        if (!_keybinds.TryGetValue(qualifiedName, out var target))
        {
            logger.LogError("Could not find keybind {QualifiedName}", qualifiedName);
            return false;
        }

        target.OverrideKey = null;
        target.OverrideContext = null;
        KeybindsChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <inheritdoc />
    public bool ClearOverrides()
    {
        logger.LogInformation("Resetting {Count} keybinds to defaults...", _keybinds.Count);
        foreach (var target in _keybinds.Values)
        {
            target.OverrideKey = null;
            target.OverrideContext = null;
            target.IsEnabled = true;
        }
        KeybindsChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    /// <inheritdoc />
    public Keybind? GetKeybind(string qualifiedName)
    {
        return _keybinds.GetValueOrDefault(qualifiedName);
    }

    /// <inheritdoc />
    public bool TryGetKeybind(string qualifiedName, out Keybind? keybind)
    {
        keybind = _keybinds.GetValueOrDefault(qualifiedName);
        return keybind is not null;
    }

    /// <inheritdoc />
    public bool IsKeybindRegistered(string qualifiedName)
    {
        return _keybinds.ContainsKey(qualifiedName);
    }

    /// <inheritdoc />
    public IEnumerable<Keybind> GetKeybinds()
    {
        return _keybinds.Values;
    }

    /// <inheritdoc />
    public IEnumerable<Keybind> GetKeybinds(KeybindContext filter)
    {
        return _keybinds.Values.Where(keybind => keybind.IsActive && keybind.Context == filter);
    }

    /// <inheritdoc />
    public IEnumerable<Keybind> GetOverridenKeybinds()
    {
        return _keybinds.Values.Where(keybind =>
            !keybind.IsActive
            || keybind.OverrideKey is not null
            || keybind.OverrideContext is not null
        );
    }

    /// <inheritdoc />
    public void Save()
    {
        var path = Paths.Keybinds.LocalPath;
        logger.LogInformation("Saving keybinds to {Path}...", path);
        try
        {
            if (!_fileSystem.Directory.Exists(Path.GetDirectoryName(path)))
                _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "/");

            using var fs = _fileSystem.FileStream.New(
                path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None
            );
            using var writer = new StreamWriter(fs);

            var content = JsonSerializer.Serialize(_keybinds, JsonOptions);
            writer.Write(content);
            logger.LogInformation("Done!");
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            logger.LogError(ex, "Failed to save keybinds");
        }
    }

    /// <inheritdoc />
    public void Parse()
    {
        logger.LogInformation("Parsing keybinds...");
        var path = Paths.Keybinds.LocalPath;
        try
        {
            if (!_fileSystem.Directory.Exists(Path.GetDirectoryName(path)))
                _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "/");

            if (!_fileSystem.File.Exists(path))
            {
                logger.LogWarning("Keybinds file does not exist, using defaults...");
                Save();
                return;
            }

            using var fs = _fileSystem.FileStream.New(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite
            );
            using var reader = new StreamReader(fs);

            var imports = JsonSerializer.Deserialize<Dictionary<string, Keybind>>(
                reader.ReadToEnd(),
                JsonOptions
            );

            if (imports is null)
            {
                logger.LogError("Failed to parse keybinds, using defaults...");
                return;
            }

            foreach (var import in imports)
            {
                import.Value.QualifiedName = import.Key;

                if (_keybinds.TryGetValue(import.Key, out var target))
                {
                    if (import.Value.OverrideKey is not null)
                        target.OverrideKey = import.Value.OverrideKey;
                    if (import.Value.OverrideContext is not null)
                        target.OverrideContext = import.Value.OverrideContext;
                }
                else
                {
                    _keybinds.TryAdd(import.Key, import.Value);
                }
            }
            logger.LogInformation("Done!");
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            logger.LogError(ex, "Failed to parse keybinds, using defaults...");
        }
        finally
        {
            KeybindsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc />
    public event EventHandler<EventArgs>? KeybindsChanged;
}
