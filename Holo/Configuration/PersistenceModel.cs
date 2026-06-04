// SPDX-License-Identifier: MPL-2.0

using System.Text.Json.Serialization;
using Holo.Media;

namespace Holo.Configuration;

internal class PersistenceModel
{
    [JsonIgnore]
    internal const double CurrentApiVersion = 1.0d;

    public required double Version;
    public required string LayoutName;
    public required bool UseColorRing;
    public required double VisualizationScaleX;
    public required double VisualizationScaleY;
    public required string PlaygroundCs;
    public required string PlaygroundJs;
    public required Dictionary<int, ScaleFactor> ScalesForRes;
    public required Dictionary<string, int> AudioTrackForVideo;
    public required List<Uri> RecentDocuments;
    public required List<Uri> RecentProjects;
}
