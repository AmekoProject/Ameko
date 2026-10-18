// SPDX-License-Identifier: MPL-2.0

using Holo.Configuration;
using Holo.Media;

namespace Holo.Models;

internal sealed class PersistenceModel
{
    public string LayoutName { get; init; } = "Default";
    public bool UseColorRing { get; init; }
    public double VisualizationScaleX { get; init; } = 4d;
    public double VisualizationScaleY { get; init; } = 2d;
    public string PlaygroundJs { get; init; } = string.Empty;
    public double WindowWidth { get; init; } = 1350d;
    public double WindowHeight { get; init; } = 650d;
    public PersistentWindowState WindowState { get; init; } = PersistentWindowState.Normal;
    public Dictionary<int, ScaleFactor> ScalesForRes { get; init; } = [];
    public Dictionary<string, int> AudioTrackForVideo { get; init; } = [];
    public List<string> RecentDocuments { get; init; } = [];
    public List<string> RecentProjects { get; init; } = [];
}
