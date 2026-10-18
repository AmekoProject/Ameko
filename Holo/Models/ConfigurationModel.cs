// SPDX-License-Identifier: MPL-2.0

using Holo.Configuration;

namespace Holo.Models;

internal sealed class ConfigurationModel
{
    public uint Cps { get; init; } = 18;
    public bool CpsIncludesWhitespace { get; init; }
    public bool CpsIncludesPunctuation { get; init; }
    public bool LineWidthIncludesWhitespace { get; init; } = true;
    public bool LineWidthIncludesPunctuation { get; init; } = true;
    public bool UseSoftLinebreaks { get; init; }
    public RichPresenceLevel RichPresenceLevel { get; init; } = RichPresenceLevel.Enabled;
    public SaveFrames SaveFrames { get; init; } = SaveFrames.WithSubtitles;
    public bool AutosaveEnabled { get; init; } = true;
    public uint AutosaveInterval { get; init; } = 60;
    public uint IndexCacheExpiration { get; init; } = 8;
    public bool AutoloadAudioTracks { get; init; } = true;
    public TimingMode TimingMode { get; init; } = TimingMode.SnapToFrame;
    public int DefaultLayer { get; init; }
    public string Culture { get; init; } = "en-US";
    public string SpellcheckCulture { get; init; } = "en_US";
    public Theme Theme { get; init; } = Theme.Default;
    public uint GridPadding { get; init; } = 2;
    public decimal EditorFontSize { get; init; } = 16m;
    public decimal GridFontSize { get; init; } = 14m;
    public decimal ReferenceFontSize { get; init; } = 12m;
    public PropagateFields PropagateFields { get; init; } = PropagateFields.NonText;
    public List<string> RepositoryUrls { get; init; } = [];
    public Dictionary<string, string> ScriptMenuOverrides { get; init; } = [];

    public TimingConfiguration Timing { get; init; } =
        new()
        {
            LeadIn = 120,
            LeadOut = 400,
            SnapStartEarlierThreshold = 350,
            SnapStartLaterThreshold = 100,
            SnapEndEarlierThreshold = 300,
            SnapEndLaterThreshold = 900,
        };

    public ColorConfiguration Colors { get; init; } = new();
}
