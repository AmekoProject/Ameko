// SPDX-License-Identifier: MPL-2.0

using System.Text.Json;
using Holo.Configuration;

namespace Holo.Models;

internal sealed class ProjectModel
{
    public List<ProjectItemModel> ReferencedDocuments { get; init; } = [];
    public List<string> Styles { get; init; } = [];
    public List<string> Colors { get; init; } = [];
    public List<string> CustomWords { get; init; } = [];
    public List<Term> Terms { get; init; } = [];
    public TimingConfiguration Timing = new();
    public Dictionary<string, Dictionary<string, JsonElement>> ScriptConfiguration { get; init; } =
    [];
    public uint? Cps { get; init; } = 18;
    public bool? CpsIncludesWhitespace { get; init; }
    public bool? CpsIncludesPunctuation { get; init; }
    public bool? UseSoftLinebreaks { get; init; }
    public int? DefaultLayer { get; init; }
    public string? SpellcheckCulture { get; init; } = "en_US";
}
