// SPDX-License-Identifier: MPL-2.0

namespace Holo.Models;

/// <summary>
/// Spellcheck suggestion
/// </summary>
public readonly struct SpellcheckSuggestion
{
    /// <summary>
    /// ID of the event containing the misspelled word
    /// </summary>
    public int EventId { get; init; }

    /// <summary>
    /// The misspelled word
    /// </summary>
    public string Word { get; init; }

    /// <summary>
    /// Suggestions for correcting the misspelled word
    /// </summary>
    public List<string> Suggestions { get; init; }
}
