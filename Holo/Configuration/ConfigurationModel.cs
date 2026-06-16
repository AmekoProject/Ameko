// SPDX-License-Identifier: MPL-2.0

using System.Text.Json.Serialization;
using Holo.Models;

namespace Holo.Configuration;

internal record ConfigurationModelBase
{
    [JsonIgnore]
    internal const int CurrentApiVersion = 3;
    public required int Version;
}

internal record ConfigurationModel : ConfigurationModelBase
{
    public required uint Cps;
    public required bool CpsIncludesWhitespace;
    public required bool CpsIncludesPunctuation;
    public required bool UseSoftLinebreaks;
    public required bool AutosaveEnabled;
    public required uint AutosaveInterval;
    public required uint IndexCacheExpiration;
    public required bool AutoloadAudioTracks;
    public required bool LineWidthIncludesWhitespace;
    public required bool LineWidthIncludesPunctuation;
    public required RichPresenceLevel RichPresenceLevel;
    public required SaveFrames SaveFrames;
    public required int DefaultLayer;
    public required TimingMode TimingMode;
    public required string Culture;
    public required string SpellcheckCulture;
    public required Theme Theme;
    public required uint GridPadding;
    public required decimal EditorFontSize;
    public required decimal GridFontSize;
    public required decimal ReferenceFontSize;
    public required PropagateFields PropagateFields;
    public required string[] RepositoryUrls;
    public required Dictionary<string, string> ScriptMenuOverrides;
    public required TimingModel Timing;
}

internal record TimingModel
{
    public required uint LeadIn;
    public required uint LeadOut;
    public required uint SnapStartEarlierThreshold;
    public required uint SnapStartLaterThreshold;
    public required uint SnapEndEarlierThreshold;
    public required uint SnapEndLaterThreshold;
}

#region Versions

internal record ConfigurationModelV1 : ConfigurationModelBase
{
    public required uint Cps;
    public required bool CpsIncludesWhitespace;
    public required bool CpsIncludesPunctuation;
    public required bool UseSoftLinebreaks;
    public required bool AutosaveEnabled;
    public required uint AutosaveInterval;
    public required bool AutoloadAudioTracks;
    public required bool LineWidthIncludesWhitespace;
    public required bool LineWidthIncludesPunctuation;
    public required RichPresenceLevel RichPresenceLevel;
    public required SaveFrames SaveFrames;
    public required int DefaultLayer;
    public required string Culture;
    public required string SpellcheckCulture;
    public required Theme Theme;
    public required uint GridPadding;
    public required decimal EditorFontSize;
    public required decimal GridFontSize;
    public required decimal ReferenceFontSize;
    public required PropagateFields PropagateFields;
    public required string[] RepositoryUrls;
    public required Dictionary<string, string> ScriptMenuOverrides;
    public required TimingModel Timing;
}

internal record ConfigurationModelV2 : ConfigurationModelBase
{
    public required uint Cps;
    public required bool CpsIncludesWhitespace;
    public required bool CpsIncludesPunctuation;
    public required bool UseSoftLinebreaks;
    public required bool AutosaveEnabled;
    public required uint AutosaveInterval;
    public required uint IndexCacheExpiration;
    public required bool AutoloadAudioTracks;
    public required bool LineWidthIncludesWhitespace;
    public required bool LineWidthIncludesPunctuation;
    public required RichPresenceLevel RichPresenceLevel;
    public required SaveFrames SaveFrames;
    public required int DefaultLayer;
    public required string Culture;
    public required string SpellcheckCulture;
    public required Theme Theme;
    public required uint GridPadding;
    public required decimal EditorFontSize;
    public required decimal GridFontSize;
    public required decimal ReferenceFontSize;
    public required PropagateFields PropagateFields;
    public required string[] RepositoryUrls;
    public required Dictionary<string, string> ScriptMenuOverrides;
    public required TimingModel Timing;
}

internal record ConfigurationModelV3 : ConfigurationModelBase
{
    public required uint Cps;
    public required bool CpsIncludesWhitespace;
    public required bool CpsIncludesPunctuation;
    public required bool UseSoftLinebreaks;
    public required bool AutosaveEnabled;
    public required uint AutosaveInterval;
    public required uint IndexCacheExpiration;
    public required bool AutoloadAudioTracks;
    public required bool LineWidthIncludesWhitespace;
    public required bool LineWidthIncludesPunctuation;
    public required RichPresenceLevel RichPresenceLevel;
    public required SaveFrames SaveFrames;
    public required int DefaultLayer;
    public required TimingMode TimingMode;
    public required string Culture;
    public required string SpellcheckCulture;
    public required Theme Theme;
    public required uint GridPadding;
    public required decimal EditorFontSize;
    public required decimal GridFontSize;
    public required decimal ReferenceFontSize;
    public required PropagateFields PropagateFields;
    public required string[] RepositoryUrls;
    public required Dictionary<string, string> ScriptMenuOverrides;
    public required TimingModel Timing;
}

#endregion
