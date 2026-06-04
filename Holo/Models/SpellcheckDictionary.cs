// SPDX-License-Identifier: MPL-2.0

namespace Holo.Models;

/// <summary>
/// A dictionary used for spellchecking
/// </summary>
public class SpellcheckDictionary
{
    /// <summary>
    /// Dictionary language
    /// </summary>
    public required SpellcheckLanguage Lang { get; init; }

    /// <summary>
    /// Path to the dictionary file
    /// </summary>
    public required Uri DictionaryPath { get; init; }

    /// <summary>
    /// Path to the affix file
    /// </summary>
    public required Uri AffixPath { get; init; }
}
