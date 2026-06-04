// SPDX-License-Identifier: MPL-2.0

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Holo.IO;
using Microsoft.Extensions.Logging;

namespace Holo.Scripting;

/// <summary>
/// Provides methods for reading and writing per-script configuration.
/// </summary>
public class ScriptConfigurationService(
    IFileSystem fileSystem,
    ILogger<ScriptConfigurationService> logger
) : IScriptConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
    private static readonly Uri ScriptingRoot = new(Path.Combine(Directories.DataHome, "scripts"));

    private readonly Dictionary<string, Dictionary<string, JsonElement>> _cache = [];

    /// <inheritdoc />
    public bool TryGet<T>(IHoloExecutable caller, string key, [NotNullWhen(true)] out T? value)
    {
        value = default;
        var qName = caller.Info.QualifiedName;
        if (!_cache.TryGetValue(qName, out var data))
            data ??= Read(qName);

        if (!data.TryGetValue(key, out var element))
            return false;

        try
        {
            value = element.Deserialize<T>(JsonOptions);
            return value is not null;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public void Set<T>(IHoloExecutable caller, string key, T value)
    {
        var qName = caller.Info.QualifiedName;
        if (!_cache.TryGetValue(qName, out var data))
            _cache[qName] = data ??= Read(qName);

        var element = JsonSerializer.SerializeToElement(value, JsonOptions);

        data[key] = element;

        Write(qName);
    }

    /// <inheritdoc />
    public bool Remove(IHoloExecutable caller, string key)
    {
        var qName = caller.Info.QualifiedName;
        if (!_cache.TryGetValue(qName, out var data))
            _cache[qName] = data ??= Read(qName);

        var result = data.Remove(key);
        if (result)
            Write(qName);
        return result;
    }

    /// <inheritdoc />
    public bool Contains(IHoloExecutable caller, string key)
    {
        var qName = caller.Info.QualifiedName;
        if (!_cache.TryGetValue(qName, out var data))
            _cache[qName] = data ??= Read(qName);
        return data.ContainsKey(key);
    }

    /// <inheritdoc />
    public bool TryGet<T>(
        IHoloExecutable caller,
        Project project,
        string key,
        [NotNullWhen(true)] out T? value
    )
    {
        value = default;
        var qName = caller.Info.QualifiedName;
        if (!project.ScriptConfiguration.TryGetValue(qName, out var data))
            return false;

        if (!data.TryGetValue(key, out var element))
            return false;

        try
        {
            value = element.Deserialize<T>(JsonOptions);
            return value is not null;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public void Set<T>(IHoloExecutable caller, Project project, string key, T value)
    {
        var qName = caller.Info.QualifiedName;
        if (!project.ScriptConfiguration.TryGetValue(qName, out var data))
            project.ScriptConfiguration[qName] = data ??= [];

        var element = JsonSerializer.SerializeToElement(value, JsonOptions);
        data[key] = element;
        project.IsSaved = false;
    }

    /// <inheritdoc />
    public bool Remove(IHoloExecutable caller, Project project, string key)
    {
        var qName = caller.Info.QualifiedName;
        if (!project.ScriptConfiguration.TryGetValue(qName, out var data))
            project.ScriptConfiguration[qName] = data ??= [];

        var result = data.Remove(key);
        if (result)
            project.IsSaved = false;
        return result;
    }

    /// <inheritdoc />
    public bool Contains(IHoloExecutable caller, Project project, string key)
    {
        var qName = caller.Info.QualifiedName;
        if (!project.ScriptConfiguration.TryGetValue(qName, out var data))
            project.ScriptConfiguration[qName] = data ??= [];

        return data.ContainsKey(key);
    }

    /// <summary>
    /// Read a package's configuration from disk
    /// </summary>
    /// <param name="qualifiedName">Package's qualified name</param>
    /// <returns>Configuration data if found, else empty</returns>
    private Dictionary<string, JsonElement> Read(string qualifiedName)
    {
        logger.LogTrace("Reading configuration file for package {Package}", qualifiedName);
        var path = ConfigPath(qualifiedName);
        try
        {
            if (!fileSystem.Directory.Exists(Path.GetDirectoryName(path)))
                fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "/");

            if (!fileSystem.File.Exists(path))
            {
                logger.LogTrace("Configuration file does not exist, using empty...");
                return [];
            }

            using var fs = fileSystem.FileStream.New(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite
            );
            using var reader = new StreamReader(fs);

            var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                reader.ReadToEnd(),
                JsonOptions
            );
            logger.LogTrace("Done!");
            return result ?? [];
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            logger.LogError(ex, "Failed to parse configuration, using empty...");
            return [];
        }
    }

    /// <summary>
    /// Write a package's configuration data to disk
    /// </summary>
    /// <param name="qualifiedName">Package's qualified name</param>
    /// <returns>
    /// <see langword="true"/> if successful or skipped, <see langword="false"/> if failed
    /// </returns>
    private bool Write(string qualifiedName)
    {
        logger.LogTrace("Writing configuration file for package {Package}", qualifiedName);
        var path = ConfigPath(qualifiedName);
        try
        {
            if (!fileSystem.Directory.Exists(Path.GetDirectoryName(path)))
                fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "/");

            if (!_cache.TryGetValue(qualifiedName, out var data))
            {
                logger.LogTrace("No data found for package {package}, skipping...", qualifiedName);
                return true;
            }

            using var fs = fileSystem.FileStream.New(
                path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None
            );
            using var writer = new StreamWriter(fs);

            var content = JsonSerializer.Serialize(data, JsonOptions);
            writer.Write(content);
            logger.LogTrace("Done!");
            return true;
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            logger.LogError(ex, "Failed to parse configuration, using empty...");
            return false;
        }
    }

    /// <summary>
    /// Get the filepath for a package
    /// </summary>
    /// <param name="qualifiedName">Package's qualified name</param>
    /// <returns>The filepath, ending in <c>.json</c></returns>
    public static string ConfigPath(string qualifiedName)
    {
        return Path.Combine(ScriptingRoot.LocalPath, "configuration", $"{qualifiedName}.json");
    }
}
