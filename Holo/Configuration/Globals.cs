// SPDX-License-Identifier: MPL-2.0

using System.Collections.ObjectModel;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssCS;
using Holo.IO;
using Holo.Models;
using Microsoft.Extensions.Logging;

namespace Holo.Configuration;

/// <summary>
/// Container for globally-accessible objects
/// </summary>
public class Globals : BindableBase, IGlobals
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly ObservableCollection<Color> _colors;
    private readonly ObservableCollection<string> _customWords;

    /// <summary>
    /// The filesystem being used
    /// </summary>
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;

    /// <inheritdoc cref="IGlobals.StyleManager"/>
    public StyleManager StyleManager { get; }

    /// <inheritdoc cref="IGlobals.Colors"/>
    public AssCS.Utilities.ReadOnlyObservableCollection<Color> Colors { get; }

    /// <inheritdoc />
    public AssCS.Utilities.ReadOnlyObservableCollection<string> CustomWords { get; }

    /// <inheritdoc cref="IGlobals.AddColor"/>
    public bool AddColor(Color color)
    {
        if (_colors.Contains(color))
            return false;
        _colors.Add(color);
        Save();
        return true;
    }

    /// <inheritdoc cref="IGlobals.RemoveColor"/>
    public bool RemoveColor(Color color)
    {
        if (!_colors.Contains(color))
            return false;
        var result = _colors.Remove(color);
        Save();
        return result;
    }

    /// <inheritdoc />
    public bool AddCustomWord(string word)
    {
        if (_customWords.Contains(word))
            return false;
        _customWords.Add(word);
        Save();
        return true;
    }

    /// <inheritdoc />
    public bool RemoveCustomWord(string word)
    {
        var result = _customWords.Remove(word);
        Save();
        return result;
    }

    /// <inheritdoc cref="IGlobals.Save"/>
    public bool Save()
    {
        var path = Paths.Globals.LocalPath;
        _logger.LogInformation("Writing globals to {Path}...", path);
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

            var model = new GlobalsModel
            {
                Styles = [.. StyleManager.Styles.Select(s => s.AsAss(AssVersion.V400P))],
                Colors = [.. Colors.Select(s => s.AsStyleColor())],
                CustomWords = [.. CustomWords],
            };

            var content = JsonSerializer.Serialize(model, JsonOptions);
            writer.Write(content);
            _logger.LogInformation("Done!");
            return true;
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            _logger.LogError(ex, "Failed to save globals");
            return false;
        }
    }

    /// <summary>
    /// Parse a saved globals file
    /// </summary>
    /// <param name="fileSystem">FileSystem to use</param>
    /// <param name="logger">Logger to use</param>
    /// <returns><see cref="Globals"/> object</returns>
    public static Globals Parse(IFileSystem fileSystem, ILogger<Globals> logger)
    {
        logger.LogInformation("Parsing globals...");
        var path = Paths.Globals.LocalPath;
        try
        {
            if (!fileSystem.Directory.Exists(Path.GetDirectoryName(path)))
                fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "/");

            if (!fileSystem.File.Exists(path))
            {
                logger.LogWarning("Globals file does not exist, using defaults...");
                return new Globals(fileSystem, logger);
            }

            using var fs = fileSystem.FileStream.New(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite
            );

            GlobalsModel? model;
            try
            {
                using var reader = new StreamReader(fs);
                var content = reader.ReadToEnd();
                model = JsonSerializer.Deserialize<GlobalsModel>(content, JsonOptions);

                if (model is null)
                    return new Globals(fileSystem, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Globals deserialization failed");
                return new Globals(fileSystem, logger);
            }

            logger.LogInformation("Done!");
            return new Globals(fileSystem, logger, model);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            logger.LogError(ex, "Failed to parse globals, using defaults...");
            return new Globals(fileSystem, logger);
        }
    }

    /// <summary>
    /// Instantiate a Globals instance
    /// </summary>
    /// <param name="fileSystem">FileSystem to use</param>
    /// <param name="logger">Logger to use</param>
    public Globals(IFileSystem fileSystem, ILogger<Globals> logger)
        : this(fileSystem, logger, new GlobalsModel()) { }

    private Globals(IFileSystem fileSystem, ILogger<Globals> logger, GlobalsModel model)
    {
        _fileSystem = fileSystem;
        _logger = logger;

        StyleManager = new StyleManager();
        _colors = [.. model.Colors.Select(Color.FromAss)];
        _customWords = [.. model.CustomWords];

        Colors = new AssCS.Utilities.ReadOnlyObservableCollection<Color>(_colors);
        CustomWords = new AssCS.Utilities.ReadOnlyObservableCollection<string>(_customWords);

        foreach (var def in model.Styles)
        {
            var style = Style.FromAss(StyleManager.NextId, def, AssVersion.V400P);
            if (style is not null)
                StyleManager.AddOrReplace(style);
        }

        StyleManager.Styles.CollectionChanged += (_, _) => Save();
        Colors.CollectionChanged += (_, _) => Save();
    }
}
