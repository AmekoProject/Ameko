// SPDX-License-Identifier: MPL-2.0

using System.Collections.ObjectModel;
using System.ComponentModel;
using AssCS;
using Holo.Models;

namespace Holo.Configuration;

/// <summary>
/// Application configuration file
/// </summary>
public interface IConfiguration
{
    /// <summary>
    /// Characters-per-second threshold
    /// </summary>
    /// <remarks>This value may be overloaded by <see cref="Project.Cps"/>.</remarks>
    uint Cps { get; set; }

    /// <summary>
    /// If whitespace should be included in <see cref="Event.Cps"/> calculation
    /// </summary>
    /// <remarks>This value may be overloaded by <see cref="Project.UseSoftLinebreaks"/>.</remarks>
    bool CpsIncludesWhitespace { get; set; }

    /// <summary>
    /// If punctuation should be included in <see cref="Event.Cps"/> calculation
    /// </summary>
    /// <remarks>This value may be overloaded by <see cref="Project.UseSoftLinebreaks"/>.</remarks>
    bool CpsIncludesPunctuation { get; set; }

    /// <summary>
    /// Soft linebreaks preference
    /// </summary>
    /// <remarks>This value may be overloaded by <see cref="Project.UseSoftLinebreaks"/>.</remarks>
    bool UseSoftLinebreaks { get; set; }

    /// <summary>
    /// Default layer preference
    /// </summary>
    /// <remarks>This value may be overloaded by <see cref="Project.DefaultLayer"/>.</remarks>
    int DefaultLayer { get; set; }

    /// <summary>
    /// Whether autosave is enabled
    /// </summary>
    bool AutosaveEnabled { get; set; }

    /// <summary>
    /// Interval between autosave attempts, in seconds
    /// </summary>
    uint AutosaveInterval { get; set; }

    /// <summary>
    /// How long to keep cached indexes
    /// </summary>
    uint IndexCacheExpiration { get; set; }

    /// <summary>
    /// If the last-used audio track should be loaded automatically when there are multiple audio tracks
    /// </summary>
    bool AutoloadAudioTracks { get; set; }

    /// <summary>
    /// If whitespace should be included in <see cref="Event.MaxLineWidth"/> calculation
    /// </summary>
    /// <remarks>This value may be overloaded by <see cref="Project.UseSoftLinebreaks"/>.</remarks>
    bool LineWidthIncludesWhitespace { get; set; }

    /// <summary>
    /// If punctuation should be included in <see cref="Event.MaxLineWidth"/> calculation
    /// </summary>
    /// <remarks>This value may be overloaded by <see cref="Project.UseSoftLinebreaks"/>.</remarks>
    bool LineWidthIncludesPunctuation { get; set; }

    /// <summary>
    /// Amount of detail to announce to Rich Presence providers (Discord, etc.)
    /// </summary>
    RichPresenceLevel RichPresenceLevel { get; set; }

    /// <summary>
    /// Where to save frames
    /// </summary>
    SaveFrames SaveFrames { get; set; }

    /// <summary>
    /// Current display language
    /// </summary>
    string Culture { get; set; }

    /// <summary>
    /// Culture to use for spellchecking
    /// </summary>
    string SpellcheckCulture { get; set; }

    /// <summary>
    /// Display theme to use
    /// </summary>
    Theme Theme { get; set; }

    /// <summary>
    /// Vertical padding in the event grid
    /// </summary>
    uint GridPadding { get; set; }

    /// <summary>
    /// Font size of the editor text box
    /// </summary>
    decimal EditorFontSize { get; set; }

    /// <summary>
    /// Font size of event grid entries
    /// </summary>
    decimal GridFontSize { get; set; }

    /// <summary>
    /// Font size of the reference text box
    /// </summary>
    decimal ReferenceFontSize { get; set; }

    /// <summary>
    /// Which fields should propagate changes from the active event to other selected events
    /// </summary>
    PropagateFields PropagateFields { get; set; }

    /// <summary>
    /// List of user-added repository URLs
    /// </summary>
    ReadOnlyObservableCollection<string> RepositoryUrls { get; }

    /// <summary>
    /// Map between script/method qualified names and user-specified overrides
    /// </summary>
    ReadOnlyDictionary<string, string> ScriptMenuOverrides { get; }

    /// <summary>
    /// Timing Butler configuration
    /// </summary>
    TimingConfiguration Timing { get; }

    /// <summary>
    /// Add a repository
    /// </summary>
    /// <param name="url">Repository to add</param>
    void AddRepositoryUrl(string url);

    /// <summary>
    /// Remove a repository
    /// </summary>
    /// <param name="url">Repository to remove</param>
    /// <returns><see langword="true"/> if successful</returns>
    bool RemoveRepositoryUrl(string url);

    /// <summary>
    /// Override a script or method's location in the scripts menu
    /// </summary>
    /// <param name="qualifiedName">Qualified name of the script or method</param>
    /// <param name="override">New submenu, or empty for root</param>
    void SetScriptMenuOverride(string qualifiedName, string @override);

    /// <summary>
    /// Remove a script menu override
    /// </summary>
    /// <param name="qualifiedName">Qualified name of the script or method</param>
    /// <returns><see langword="true"/> if successful</returns>
    bool RemoveScriptMenuOverride(string qualifiedName);

    /// <summary>
    /// Write the configuration to file
    /// </summary>
    /// <returns><see langword="true"/> if saving was successful</returns>
    bool Save();

    /// <summary>
    /// Raised when a configuration property is modified
    /// </summary>
    event PropertyChangedEventHandler? PropertyChanged;
}
