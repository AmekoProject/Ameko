// SPDX-License-Identifier: MPL-2.0

using AssCS;
using Holo.Models;

namespace Holo.Providers;

/// <summary>
/// Facilitates spellchecking
/// </summary>
public interface ISpellcheckService
{
    /// <summary>
    /// Language currently in use
    /// </summary>
    SpellcheckLanguage CurrentLanguage { get; }

    /// <summary>
    /// Check the current document,
    /// using basic heuristics to exclude assumed typesetting events
    /// </summary>
    /// <remarks>
    ///<b>THIS METHOD IS NAIVE!</b> It uses spaces to find word boundaries.
    /// In the future, it may be worth looking into additional libraries,
    /// like ICU4N's BreakIterator, to find word boundaries in a fully
    /// language-agnostic way.
    /// </remarks>
    IEnumerable<SpellcheckSuggestion> CheckSpelling();

    /// <summary>
    /// Check the current document,
    /// using basic heuristics to exclude assumed typesetting events
    /// </summary>
    /// <param name="event">Event to spellcheck</param>
    /// <remarks>
    ///<b>THIS METHOD IS NAIVE!</b> It uses spaces to find word boundaries.
    /// In the future, it may be worth looking into additional libraries,
    /// like ICU4N's BreakIterator, to find word boundaries in a fully
    /// language-agnostic way.
    /// </remarks>
    public IEnumerable<SpellcheckSuggestion> CheckSpelling(Event @event);

    /// <summary>
    /// Rebuild the spellcheck dictionary using the current culture and custom words
    /// </summary>
    void RebuildDictionary();

    /// <summary>
    /// Check if a dictionary for the <paramref name="culture"/> is installed
    /// </summary>
    /// <param name="culture">Culture code</param>
    /// <returns><see langword="true"/> if it is installed or invalid</returns>
    bool IsDictionaryInstalled(string culture);

    /// <summary>
    /// Add a word to the current <see cref="Project"/>
    /// </summary>
    /// <param name="word">Word to add</param>
    void AddWordToProject(string word);

    /// <summary>
    /// Add a word to the Globals
    /// </summary>
    /// <param name="word">Word to add</param>
    void AddWordToGlobals(string word);
}
