// SPDX-License-Identifier: MPL-2.0

using System.Text.Json.Serialization;
using Holo.Media;

namespace Holo.Configuration;

internal record PersistenceModelBase
{
    [JsonIgnore]
    internal const int CurrentApiVersion = 2;
    public required int Version;
}

internal record PersistenceModel : PersistenceModelBase
{
    public required string LayoutName;
    public required bool UseColorRing;
    public required double VisualizationScaleX;
    public required double VisualizationScaleY;
    public required string PlaygroundCs;
    public required string PlaygroundJs;
    public required Dictionary<int, ScaleFactor> ScalesForRes;
    public required Dictionary<string, int> AudioTrackForVideo;
    public required List<string> RecentDocuments;
    public required List<string> RecentProjects;
    public required double WindowWidth;
    public required double WindowHeight;
    public required PersistentWindowState WindowState;
}

/// <summary>
/// Window State options
/// </summary>
public enum PersistentWindowState
{
    /// <summary>The window is neither minimized nor maximized.</summary>
    Normal,

    /// <summary>The window is minimized.</summary>
    Minimized,

    /// <summary>The window is maximized.</summary>
    Maximized,

    /// <summary>The window is fullscreen.</summary>
    FullScreen,
}
