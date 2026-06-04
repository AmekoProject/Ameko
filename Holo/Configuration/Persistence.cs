// SPDX-License-Identifier: MPL-2.0

using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using AssCS;
using AssCS.Utilities;
using Holo.IO;
using Holo.Media;
using Microsoft.Extensions.Logging;

namespace Holo.Configuration;

/// <summary>
/// <see cref="Configuration"/>-like utility for application persistence
/// not directly controlled by a user
/// </summary>
public class Persistence : BindableBase, IPersistence
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

    private string _layoutName;
    private bool _useColorRing;
    private string _playgroundCs;
    private string _playgroundJs;
    private double _visualizationScaleX;
    private double _visualizationScaleY;
    private Dictionary<int, ScaleFactor> _scalesForRes;
    private Dictionary<string, int> _audioTrackForVideo;
    private List<Uri> _recentDocuments;
    private List<Uri> _recentProjects;

    /// <inheritdoc />
    public string LayoutName
    {
        get => _layoutName;
        set => SetProperty(ref _layoutName, value);
    }

    /// <inheritdoc />
    public bool UseColorRing
    {
        get => _useColorRing;
        set => SetProperty(ref _useColorRing, value);
    }

    /// <inheritdoc />
    public double VisualizationScaleX
    {
        get => _visualizationScaleX;
        set => SetProperty(ref _visualizationScaleX, value);
    }

    /// <inheritdoc />
    public double VisualizationScaleY
    {
        get => _visualizationScaleY;
        set => SetProperty(ref _visualizationScaleY, value);
    }

    /// <inheritdoc />
    public string PlaygroundCs
    {
        get => _playgroundCs;
        set => SetProperty(ref _playgroundCs, value);
    }

    /// <inheritdoc />
    public string PlaygroundJs
    {
        get => _playgroundJs;
        set => SetProperty(ref _playgroundJs, value);
    }

    /// <inheritdoc />
    public IReadOnlyList<Uri> RecentDocuments => _recentDocuments;

    /// <inheritdoc />
    public IReadOnlyList<Uri> RecentProjects => _recentProjects;

    /// <inheritdoc />
    public void SetScaleForRes(int height, ScaleFactor scaleFactor)
    {
        _scalesForRes[height] = scaleFactor;
    }

    /// <inheritdoc />
    public ScaleFactor GetScaleForRes(int height)
    {
        return _scalesForRes.GetValueOrDefault(height, ScaleFactor.Default);
    }

    /// <inheritdoc />
    public void SetAudioTrackForVideo(string pathHash, int trackNumber)
    {
        _audioTrackForVideo[pathHash] = trackNumber;
    }

    /// <inheritdoc />
    public int GetAudioTrackForVideo(string pathHash)
    {
        return _audioTrackForVideo.GetValueOrDefault(pathHash, -1);
    }

    /// <inheritdoc />
    public void AddRecentDocument(Uri document)
    {
        _recentDocuments.Remove(document);
        _recentDocuments.Insert(0, document);
        if (_recentDocuments.Count > 10)
            _recentDocuments.RemoveAt(_recentDocuments.Count - 1);
        RaisePropertyChanged(nameof(RecentDocuments));
    }

    /// <inheritdoc />
    public void ClearRecentDocuments()
    {
        _recentDocuments.Clear();
        RaisePropertyChanged(nameof(RecentDocuments));
    }

    /// <inheritdoc />
    public void AddRecentProject(Uri project)
    {
        _recentProjects.Remove(project);
        _recentProjects.Insert(0, project);
        if (_recentProjects.Count > 10)
            _recentProjects.RemoveAt(_recentProjects.Count - 1);
        RaisePropertyChanged(nameof(RecentProjects));
    }

    /// <inheritdoc />
    public void ClearRecentProjects()
    {
        _recentProjects.Clear();
        RaisePropertyChanged(nameof(RecentProjects));
    }

    /// <inheritdoc />
    public bool Save()
    {
        var path = Paths.Persistence.LocalPath;
        _logger.LogInformation("Writing persistence to {Path}...", path);
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

            var model = new PersistenceModel
            {
                Version = PersistenceModel.CurrentApiVersion,
                LayoutName = _layoutName,
                UseColorRing = _useColorRing,
                VisualizationScaleX = _visualizationScaleX,
                VisualizationScaleY = _visualizationScaleY,
                PlaygroundCs = StringEncoder.Base64Encode(_playgroundCs),
                PlaygroundJs = StringEncoder.Base64Encode(_playgroundJs),
                ScalesForRes = new Dictionary<int, ScaleFactor>(_scalesForRes),
                AudioTrackForVideo = new Dictionary<string, int>(_audioTrackForVideo),
                RecentDocuments = [.. _recentDocuments],
                RecentProjects = [.. _recentProjects],
            };

            var content = JsonSerializer.Serialize(model, JsonOptions);
            writer.Write(content);
            _logger.LogInformation("Done!");
            return true;
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            _logger.LogError(ex, "Failed to write persistence");
            return false;
        }
    }

    /// <summary>
    /// Load an existing persistence file
    /// </summary>
    /// <param name="fileSystem">FileSystem to use</param>
    /// <param name="logger">Logger to use</param>
    /// <returns>The <see cref="Persistence"/> represented by the loaded file</returns>
    /// <exception cref="InvalidDataException">If the file is invalid</exception>
    public static Persistence Parse(IFileSystem fileSystem, ILogger<Persistence> logger)
    {
        logger.LogInformation("Parsing persistence...");
        var path = Paths.Persistence.LocalPath;
        try
        {
            if (!fileSystem.Directory.Exists(Path.GetDirectoryName(path)))
                fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "/");

            if (!fileSystem.File.Exists(path))
            {
                logger.LogWarning("Persistence file does not exist, using defaults...");
                return new Persistence(fileSystem, logger);
            }

            using var fs = fileSystem.FileStream.New(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite
            );
            using var reader = new StreamReader(fs);

            var model =
                JsonSerializer.Deserialize<PersistenceModel>(reader.ReadToEnd(), JsonOptions)
                ?? throw new InvalidDataException("Persistence model deserialization failed!");

            var result = new Persistence(fileSystem, logger)
            {
                _layoutName = model.LayoutName,
                _useColorRing = model.UseColorRing,
                _visualizationScaleX = model.VisualizationScaleX,
                _visualizationScaleY = model.VisualizationScaleY,
                _playgroundCs = StringEncoder.Base64Decode(model.PlaygroundCs),
                _playgroundJs = StringEncoder.Base64Decode(model.PlaygroundJs),
                _scalesForRes = new Dictionary<int, ScaleFactor>(model.ScalesForRes),
                _audioTrackForVideo = new Dictionary<string, int>(model.AudioTrackForVideo),
                _recentDocuments = [.. model.RecentDocuments],
                _recentProjects = [.. model.RecentProjects],
            };
            logger.LogInformation("Done!");
            return result;
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            logger.LogError(ex, "Failed to parse persistence, using defaults...");
            return new Persistence(fileSystem, logger);
        }
    }

    /// <summary>
    /// Instantiate a Persistence instance
    /// </summary>
    /// <param name="fileSystem">FileSystem to use</param>
    /// <param name="logger">Logger to use</param>
    public Persistence(IFileSystem fileSystem, ILogger<Persistence> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;

        _scalesForRes = [];
        _audioTrackForVideo = [];
        _recentDocuments = [];
        _recentProjects = [];

        _layoutName = "Default";
        _useColorRing = false;
        _visualizationScaleX = 4d;
        _visualizationScaleY = 2d;
        _playgroundCs = string.Empty;
        _playgroundJs = string.Empty;
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
