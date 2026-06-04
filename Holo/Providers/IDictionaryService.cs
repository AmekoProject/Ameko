// SPDX-License-Identifier: MPL-2.0

using System.Diagnostics.CodeAnalysis;
using AssCS.Utilities;
using Holo.Models;

namespace Holo.Providers;

/// <summary>
/// Provides methods for managing spellcheck dictionaries
/// </summary>
public interface IDictionaryService
{
    /// <summary>
    /// List of installed spellcheck dictionaries
    /// </summary>
    ReadOnlyObservableCollection<SpellcheckDictionary> InstalledDictionaries { get; }

    /// <summary>
    /// Try to get a dictionary
    /// </summary>
    /// <param name="lang">Language code</param>
    /// <param name="dictionary">Dictionary, if found</param>
    /// <returns><see langword="true"/> if found</returns>
    bool TryGetDictionary(string lang, [NotNullWhen(true)] out SpellcheckDictionary? dictionary);

    /// <summary>
    /// Download a new dictionary
    /// </summary>
    /// <param name="lang">Language</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    Task<bool> DownloadDictionary(SpellcheckLanguage lang);
}
