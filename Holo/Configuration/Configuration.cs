// SPDX-License-Identifier: MPL-2.0

using System.Collections.ObjectModel;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssCS;
using Holo.IO;
using Holo.Models;
using Microsoft.Extensions.Logging;

namespace Holo.Configuration;

/// <summary>
/// User-controlled configuration options.
/// </summary>
/// <remarks>
/// <para>
/// Some, but not all, options are configurable in both the application space
/// via <see cref="Configuration"/> files and in <see cref="Project"/>s.
/// In such cases, the project's value takes precedence, unless the
/// project's value is <see langword="null"/>.
/// </para>
/// </remarks>
public class Configuration : BindableBase, IConfiguration
{
    private const int CurrentApiVersion = 3;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>
    /// The filesystem being used
    /// </summary>
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;

    private uint _cps;
    private bool _cpsIncludesWhitespace;
    private bool _cpsIncludesPunctuation;
    private bool _useSoftLinebreaks;
    private bool _autosaveEnabled;
    private uint _autosaveInterval;
    private bool _autoloadAudioTracks;
    private uint _indexCacheExpiration;
    private bool _lineWidthIncludesWhitespace;
    private bool _lineWidthIncludesPunctuation;
    private RichPresenceLevel _richPresenceLevel;
    private SaveFrames _saveFrames;
    private TimingMode _timingMode;
    private int _defaultLayer;
    private string _culture;
    private string _spellcheckCulture;
    private Theme _theme;
    private uint _gridPadding;
    private decimal _editorFontSize;
    private decimal _gridFontSize;
    private decimal _referenceFontSize;
    private PropagateFields _propagateFields;
    private RangeObservableCollection<string> _repositoryUrls;
    private Dictionary<string, string> _scriptMenuOverrides;

    /// <inheritdoc />
    public uint Cps
    {
        get => _cps;
        set => SetProperty(ref _cps, value);
    }

    /// <inheritdoc />
    public bool CpsIncludesWhitespace
    {
        get => _cpsIncludesWhitespace;
        set => SetProperty(ref _cpsIncludesWhitespace, value);
    }

    /// <inheritdoc />
    public bool CpsIncludesPunctuation
    {
        get => _cpsIncludesPunctuation;
        set => SetProperty(ref _cpsIncludesPunctuation, value);
    }

    /// <inheritdoc />
    public bool UseSoftLinebreaks
    {
        get => _useSoftLinebreaks;
        set => SetProperty(ref _useSoftLinebreaks, value);
    }

    /// <inheritdoc />
    public int DefaultLayer
    {
        get => _defaultLayer;
        set => SetProperty(ref _defaultLayer, value);
    }

    /// <inheritdoc />
    public TimingMode TimingMode
    {
        get => _timingMode;
        set => SetProperty(ref _timingMode, value);
    }

    /// <inheritdoc />
    public bool AutosaveEnabled
    {
        get => _autosaveEnabled;
        set => SetProperty(ref _autosaveEnabled, value);
    }

    /// <inheritdoc />
    public uint AutosaveInterval
    {
        get => _autosaveInterval;
        set => SetProperty(ref _autosaveInterval, value);
    }

    /// <inheritdoc />
    public uint IndexCacheExpiration
    {
        get => _indexCacheExpiration;
        set => SetProperty(ref _indexCacheExpiration, value);
    }

    /// <inheritdoc />
    public bool AutoloadAudioTracks
    {
        get => _autoloadAudioTracks;
        set => SetProperty(ref _autoloadAudioTracks, value);
    }

    /// <inheritdoc />
    public bool LineWidthIncludesWhitespace
    {
        get => _lineWidthIncludesWhitespace;
        set => SetProperty(ref _lineWidthIncludesWhitespace, value);
    }

    /// <inheritdoc />
    public bool LineWidthIncludesPunctuation
    {
        get => _lineWidthIncludesPunctuation;
        set => SetProperty(ref _lineWidthIncludesPunctuation, value);
    }

    /// <inheritdoc />
    public RichPresenceLevel RichPresenceLevel
    {
        get => _richPresenceLevel;
        set => SetProperty(ref _richPresenceLevel, value);
    }

    /// <inheritdoc />
    public SaveFrames SaveFrames
    {
        get => _saveFrames;
        set => SetProperty(ref _saveFrames, value);
    }

    /// <inheritdoc />
    public string Culture
    {
        get => _culture;
        set => SetProperty(ref _culture, value);
    }

    /// <inheritdoc />
    public string SpellcheckCulture
    {
        get => _spellcheckCulture;
        set => SetProperty(ref _spellcheckCulture, value);
    }

    /// <inheritdoc />
    public Theme Theme
    {
        get => _theme;
        set => SetProperty(ref _theme, value);
    }

    /// <inheritdoc />
    public uint GridPadding
    {
        get => _gridPadding;
        set => SetProperty(ref _gridPadding, value);
    }

    /// <inheritdoc />
    public decimal EditorFontSize
    {
        get => _editorFontSize;
        set => SetProperty(ref _editorFontSize, value);
    }

    /// <inheritdoc />
    public decimal GridFontSize
    {
        get => _gridFontSize;
        set => SetProperty(ref _gridFontSize, value);
    }

    /// <inheritdoc />
    public decimal ReferenceFontSize
    {
        get => _referenceFontSize;
        set => SetProperty(ref _referenceFontSize, value);
    }

    /// <inheritdoc />
    public PropagateFields PropagateFields
    {
        get => _propagateFields;
        set => SetProperty(ref _propagateFields, value);
    }

    /// <inheritdoc />
    public ReadOnlyObservableCollection<string> RepositoryUrls { get; }

    /// <inheritdoc />
    public ReadOnlyDictionary<string, string> ScriptMenuOverrides { get; }

    /// <inheritdoc />
    public TimingConfiguration Timing { get; }

    /// <inheritdoc />
    public void AddRepositoryUrl(string url)
    {
        _logger.LogDebug("Adding repository url {Url}", url);
        _repositoryUrls.Add(url);
        Save();
    }

    /// <inheritdoc />
    public bool RemoveRepositoryUrl(string url)
    {
        _logger.LogDebug("Removing repository url {Url}", url);
        var result = _repositoryUrls.Remove(url);
        Save();
        return result;
    }

    /// <inheritdoc />
    public void SetScriptMenuOverride(string qualifiedName, string @override)
    {
        _scriptMenuOverrides[qualifiedName] = @override;
    }

    /// <inheritdoc />
    public bool RemoveScriptMenuOverride(string qualifiedName)
    {
        return _scriptMenuOverrides.Remove(qualifiedName);
    }

    /// <inheritdoc />
    public bool Save()
    {
        var path = Paths.Configuration.LocalPath;
        _logger.LogInformation("Writing configuration to {Path}...", path);
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

            var model = new Dictionary<string, object?>();
            model.SetVersion(CurrentApiVersion);

            model.Set(Cps);
            model.Set(CpsIncludesWhitespace);
            model.Set(CpsIncludesPunctuation);
            model.Set(UseSoftLinebreaks);
            model.Set(AutosaveEnabled);
            model.Set(AutosaveInterval);
            model.Set(IndexCacheExpiration);
            model.Set(AutoloadAudioTracks);
            model.Set(LineWidthIncludesWhitespace);
            model.Set(LineWidthIncludesPunctuation);
            model.Set(RichPresenceLevel);
            model.Set(SaveFrames);
            model.Set(TimingMode);
            model.Set(DefaultLayer);
            model.Set(Culture);
            model.Set(SpellcheckCulture);
            model.Set(Theme);
            model.Set(GridPadding);
            model.Set(EditorFontSize);
            model.Set(GridFontSize);
            model.Set(ReferenceFontSize);
            model.Set(PropagateFields);
            model.Set(EditorFontSize);
            model.Set(RepositoryUrls.ToArray(), nameof(RepositoryUrls));
            model.Set(ScriptMenuOverrides.ToDictionary(), nameof(ScriptMenuOverrides));
            model.Set(
                new
                {
                    Timing.LeadIn,
                    Timing.LeadOut,
                    Timing.SnapStartEarlierThreshold,
                    Timing.SnapStartLaterThreshold,
                    Timing.SnapEndEarlierThreshold,
                    Timing.SnapEndLaterThreshold,
                },
                nameof(Timing)
            );

            var content = JsonSerializer.Serialize(model, JsonOptions);
            writer.Write(content);
            _logger.LogInformation("Done!");
            return true;
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            _logger.LogError(ex, "Failed to save configuration");
            return false;
        }
    }

    /// <summary>
    /// Parse a saved configuration file
    /// </summary>
    /// <param name="fileSystem">FileSystem to use</param>
    /// <param name="logger">Logger to use</param>
    /// <returns><see cref="Configuration"/> object</returns>
    public static Configuration Parse(IFileSystem fileSystem, ILogger<Configuration> logger)
    {
        logger.LogInformation("Parsing configuration...");
        var path = Paths.Configuration.LocalPath;
        try
        {
            if (!fileSystem.Directory.Exists(Path.GetDirectoryName(path)))
                fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "/");

            var @default = new Configuration(fileSystem, logger);
            if (!fileSystem.File.Exists(path))
            {
                logger.LogWarning("Configuration file does not exist, using defaults...");
                return @default;
            }

            using var fs = fileSystem.FileStream.New(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite
            );

            Dictionary<string, object?>? model;
            try
            {
                using var reader = new StreamReader(fs);
                var content = reader.ReadToEnd();
                model = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                    content,
                    JsonOptions
                );

                if (model is null)
                    return @default;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Configuration deserialization failed");
                return @default;
            }

            var timing = model.GetOrDefault(@default.Timing);

            var result = new Configuration(fileSystem, logger)
            {
                _cps = model.GetOrDefault(@default.Cps),
                _cpsIncludesWhitespace = model.GetOrDefault(@default.CpsIncludesWhitespace),
                _cpsIncludesPunctuation = model.GetOrDefault(@default.CpsIncludesPunctuation),
                _useSoftLinebreaks = model.GetOrDefault(@default.UseSoftLinebreaks),
                _autosaveEnabled = model.GetOrDefault(@default.AutosaveEnabled),
                _autosaveInterval = model.GetOrDefault(@default.AutosaveInterval),
                _indexCacheExpiration = model.GetOrDefault(@default.IndexCacheExpiration),
                _autoloadAudioTracks = model.GetOrDefault(@default.AutoloadAudioTracks),
                _lineWidthIncludesWhitespace = model.GetOrDefault(
                    @default.LineWidthIncludesWhitespace
                ),
                _lineWidthIncludesPunctuation = model.GetOrDefault(
                    @default.LineWidthIncludesPunctuation
                ),
                _richPresenceLevel = model.GetOrDefault(@default.RichPresenceLevel),
                _saveFrames = model.GetOrDefault(@default.SaveFrames),
                _timingMode = model.GetOrDefault(@default.TimingMode),
                _defaultLayer = model.GetOrDefault(@default.DefaultLayer),
                _culture = model.GetOrDefault(@default.Culture),
                _spellcheckCulture = model.GetOrDefault(@default.SpellcheckCulture),
                _theme = model.GetOrDefault(@default.Theme),
                _gridPadding = model.GetOrDefault(@default.GridPadding),
                _editorFontSize = model.GetOrDefault(@default.EditorFontSize),
                _gridFontSize = model.GetOrDefault(@default.GridFontSize),
                _referenceFontSize = model.GetOrDefault(@default.ReferenceFontSize),
                _propagateFields = model.GetOrDefault(@default.PropagateFields),
                _repositoryUrls = new RangeObservableCollection<string>(
                    model.GetOrDefault(nameof(RepositoryUrls), @default._repositoryUrls)
                ),
                _scriptMenuOverrides = new Dictionary<string, string>(
                    model.GetOrDefault(nameof(ScriptMenuOverrides), @default.ScriptMenuOverrides)
                ),
                Timing =
                {
                    LeadIn = timing.LeadIn,
                    LeadOut = timing.LeadOut,
                    SnapStartEarlierThreshold = timing.SnapStartEarlierThreshold,
                    SnapStartLaterThreshold = timing.SnapStartLaterThreshold,
                    SnapEndEarlierThreshold = timing.SnapEndEarlierThreshold,
                    SnapEndLaterThreshold = timing.SnapEndLaterThreshold,
                },
            };
            logger.LogInformation("Done!");
            return result;
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            logger.LogError(ex, "Failed to parse configuration, using defaults...");
            return new Configuration(fileSystem, logger);
        }
    }

    /// <summary>
    /// Instantiate a Configuration instance
    /// </summary>
    /// <param name="fileSystem">FileSystem to use</param>
    /// <param name="logger">Logger to use</param>
    public Configuration(IFileSystem fileSystem, ILogger<Configuration> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _cps = 18;
        _useSoftLinebreaks = false;
        _richPresenceLevel = RichPresenceLevel.Enabled;
        _saveFrames = SaveFrames.WithSubtitles;
        _autosaveEnabled = true;
        _autosaveInterval = 60;
        _indexCacheExpiration = 8;
        _autoloadAudioTracks = true;
        _timingMode = TimingMode.SnapToFrame;
        _culture = "en-US";
        _spellcheckCulture = "en_US";
        _theme = Theme.Default;
        _gridPadding = 2;
        _editorFontSize = 16m;
        _gridFontSize = 14m;
        _referenceFontSize = 12m;
        _propagateFields = PropagateFields.NonText;
        _repositoryUrls = [];
        _scriptMenuOverrides = [];

        Timing = new TimingConfiguration
        {
            LeadIn = 120,
            LeadOut = 400,
            SnapStartEarlierThreshold = 350,
            SnapStartLaterThreshold = 100,
            SnapEndEarlierThreshold = 300,
            SnapEndLaterThreshold = 900,
        };

        RepositoryUrls = new ReadOnlyObservableCollection<string>(_repositoryUrls);
        ScriptMenuOverrides = new ReadOnlyDictionary<string, string>(_scriptMenuOverrides);
    }

    /// <inheritdoc />
    protected override bool SetProperty<T>(
        ref T storage,
        T value,
        [CallerMemberName] string? propertyName = null
    )
    {
        if (Equals(storage, value))
            return false;

        storage = value;
        RaisePropertyChanged(propertyName);
        return true;
    }
}

file static class ModelExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    extension(Dictionary<string, object?> model)
    {
        /// Specifically designed to take in <c>@default.Property</c> calls
        public T GetOrDefault<T>(
            T defaultValue,
            [CallerArgumentExpression(nameof(defaultValue))] string? defaultValueExpression = null
        )
        {
            var separator = defaultValueExpression?.LastIndexOf('.') ?? -1;
            if (separator < 0 || separator == defaultValueExpression!.Length - 1)
                throw new ArgumentException(
                    "The default value must be a property access expression.",
                    nameof(defaultValueExpression)
                );

            var key = defaultValueExpression[(separator + 1)..].TrimStart('@');
            return model.GetOrDefault(key, defaultValue);
        }

        public T GetOrDefault<T>(string key, T defaultValue)
        {
            if (!model.TryGetValue(key, out var value) || value is not JsonElement element)
                return defaultValue;

            try
            {
                return element.Deserialize<T>(JsonOptions) ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public void Set<T>(T value, [CallerArgumentExpression(nameof(value))] string? key = null)
        {
            if (key is null)
                return;

            model[key] = value;
        }

        public void SetVersion(int version)
        {
            model["Version"] = version;
        }
    }
}
