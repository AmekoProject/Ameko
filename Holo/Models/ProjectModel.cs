// SPDX-License-Identifier: MPL-2.0

using System.Text.Json;
using System.Text.Json.Serialization;
using Holo.Configuration;

namespace Holo.Models;

internal record ProjectModelBase
{
    [JsonIgnore]
    internal const int CurrentApiVersion = 2;
    public required int Version;
}

/// <summary>
/// Simplified representation of a <see cref="Project"/> for
/// serialization purposes
/// </summary>
internal record ProjectModel : ProjectModelBase
{
    public required ProjectItemModel[] ReferencedDocuments;
    public required string[] Styles;
    public required string[] Colors;
    public required uint? Cps;
    public required bool? CpsIncludesWhitespace;
    public required bool? CpsIncludesPunctuation;
    public required bool? UseSoftLinebreaks;
    public required int? DefaultLayer;
    public required string? SpellcheckCulture;
    public required string[] CustomWords;
    public required Term[] Terms;
    public required TimingModel Timing;
    public required Dictionary<string, Dictionary<string, JsonElement>> ScriptConfiguration;
}

#region Versions

internal record ProjectModelV1 : ProjectModelBase
{
    public required ProjectItemModel[] ReferencedDocuments;
    public required string[] Styles;
    public required uint? Cps;
    public required bool? CpsIncludesWhitespace;
    public required bool? CpsIncludesPunctuation;
    public required bool? UseSoftLinebreaks;
    public required int? DefaultLayer;
    public required string? SpellcheckCulture;
    public required string[] CustomWords;
    public required TimingModel Timing;
    public required Dictionary<string, Dictionary<string, JsonElement>> ScriptConfiguration;
}

internal record ProjectModelV2 : ProjectModelBase
{
    public required ProjectItemModel[] ReferencedDocuments;
    public required string[] Styles;
    public required string[] Colors;
    public required uint? Cps;
    public required bool? CpsIncludesWhitespace;
    public required bool? CpsIncludesPunctuation;
    public required bool? UseSoftLinebreaks;
    public required int? DefaultLayer;
    public required string? SpellcheckCulture;
    public required string[] CustomWords;
    public required Term[] Terms;
    public required TimingModel Timing;
    public required Dictionary<string, Dictionary<string, JsonElement>> ScriptConfiguration;
}

#endregion
