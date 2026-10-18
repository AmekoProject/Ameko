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
    private readonly RangeObservableCollection<string> _repositoryUrls;
    private readonly Dictionary<string, string> _scriptMenuOverrides;

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
    public ColorConfiguration Colors { get; }

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

            var model = new ConfigurationModel
            {
                Cps = Cps,
                CpsIncludesWhitespace = CpsIncludesWhitespace,
                CpsIncludesPunctuation = CpsIncludesPunctuation,
                LineWidthIncludesWhitespace = LineWidthIncludesWhitespace,
                LineWidthIncludesPunctuation = LineWidthIncludesPunctuation,
                UseSoftLinebreaks = UseSoftLinebreaks,
                AutosaveEnabled = AutosaveEnabled,
                AutosaveInterval = AutosaveInterval,
                IndexCacheExpiration = IndexCacheExpiration,
                AutoloadAudioTracks = AutoloadAudioTracks,
                RichPresenceLevel = RichPresenceLevel,
                SaveFrames = SaveFrames,
                TimingMode = TimingMode,
                DefaultLayer = DefaultLayer,
                Culture = Culture,
                SpellcheckCulture = SpellcheckCulture,
                Theme = Theme,
                GridPadding = GridPadding,
                EditorFontSize = EditorFontSize,
                GridFontSize = GridFontSize,
                ReferenceFontSize = ReferenceFontSize,
                PropagateFields = PropagateFields,
                RepositoryUrls = [.. RepositoryUrls],
                ScriptMenuOverrides = new Dictionary<string, string>(ScriptMenuOverrides),
                Timing = Timing,
                Colors = Colors,
            };

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

            if (!fileSystem.File.Exists(path))
            {
                logger.LogWarning("Configuration file does not exist, using defaults...");
                return new Configuration(fileSystem, logger);
            }

            using var fs = fileSystem.FileStream.New(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite
            );

            ConfigurationModel? model;
            try
            {
                using var reader = new StreamReader(fs);
                var content = reader.ReadToEnd();
                model = JsonSerializer.Deserialize<ConfigurationModel>(content, JsonOptions);

                if (model is null)
                    return new Configuration(fileSystem, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Configuration deserialization failed");
                return new Configuration(fileSystem, logger);
            }

            logger.LogInformation("Done!");
            return new Configuration(fileSystem, logger, model);
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
        : this(fileSystem, logger, new ConfigurationModel()) { }

    private Configuration(
        IFileSystem fileSystem,
        ILogger<Configuration> logger,
        ConfigurationModel model
    )
    {
        _fileSystem = fileSystem;
        _logger = logger;

        _cps = model.Cps;
        _cpsIncludesWhitespace = model.CpsIncludesWhitespace;
        _cpsIncludesPunctuation = model.CpsIncludesPunctuation;
        _lineWidthIncludesWhitespace = model.LineWidthIncludesWhitespace;
        _lineWidthIncludesPunctuation = model.LineWidthIncludesPunctuation;
        _useSoftLinebreaks = model.UseSoftLinebreaks;
        _richPresenceLevel = model.RichPresenceLevel;
        _saveFrames = model.SaveFrames;
        _autosaveEnabled = model.AutosaveEnabled;
        _autosaveInterval = model.AutosaveInterval;
        _indexCacheExpiration = model.IndexCacheExpiration;
        _autoloadAudioTracks = model.AutoloadAudioTracks;
        _timingMode = model.TimingMode;
        _defaultLayer = model.DefaultLayer;
        _culture = model.Culture;
        _spellcheckCulture = model.SpellcheckCulture;
        _theme = model.Theme;
        _gridPadding = model.GridPadding;
        _editorFontSize = model.EditorFontSize;
        _gridFontSize = model.GridFontSize;
        _referenceFontSize = model.ReferenceFontSize;
        _propagateFields = model.PropagateFields;
        _repositoryUrls = new RangeObservableCollection<string>(model.RepositoryUrls);
        _scriptMenuOverrides = new Dictionary<string, string>(model.ScriptMenuOverrides);
        Timing = model.Timing;
        Colors = model.Colors;

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
