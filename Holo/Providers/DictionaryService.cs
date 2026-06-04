// SPDX-License-Identifier: MPL-2.0

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Holo.IO;
using Holo.Models;
using Microsoft.Extensions.Logging;

namespace Holo.Providers;

/// <summary>
/// Provides methods for managing spellcheck dictionaries
/// </summary>
public class DictionaryService : IDictionaryService
{
    private static readonly Uri DictionariesRoot = new(
        Path.Combine(Directories.DataHome, "dictionaries")
    );

    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

    private readonly ObservableCollection<SpellcheckDictionary> _installedDictionaries;

    /// <inheritdoc />
    public AssCS.Utilities.ReadOnlyObservableCollection<SpellcheckDictionary> InstalledDictionaries { get; }

    /// <inheritdoc />
    public bool TryGetDictionary(
        string lang,
        [NotNullWhen(true)] out SpellcheckDictionary? dictionary
    )
    {
        dictionary = _installedDictionaries.FirstOrDefault(d => d.Lang.Locale == lang);
        return dictionary is not null;
    }

    /// <inheritdoc />
    public async Task<bool> DownloadDictionary(SpellcheckLanguage lang)
    {
        if (TryGetDictionary(lang.Locale, out _))
            return false;

        _logger.LogInformation("Attempting to download dictionary {SpellcheckLanguage}", lang);
        try
        {
            if (!_fileSystem.Directory.Exists(DictionariesRoot.LocalPath))
                _fileSystem.Directory.CreateDirectory(DictionariesRoot.LocalPath);

            var dicPath = Path.Combine(DictionariesRoot.LocalPath, $"{lang.Locale}.dic");
            var affPath = Path.Combine(DictionariesRoot.LocalPath, $"{lang.Locale}.aff");

            // Download dictionary
            await using var dicStream = await _httpClient.GetStreamAsync(GetDicUri(lang));
            await using var dicFs = _fileSystem.FileStream.New(
                dicPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None
            );
            await dicStream.CopyToAsync(dicFs);

            // Download affixes
            await using var affStream = await _httpClient.GetStreamAsync(GetAffUri(lang));
            await using var affFs = _fileSystem.FileStream.New(
                affPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None
            );
            await affStream.CopyToAsync(affFs);
            PopulateInstalledDictionaries();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to download dictionary {SpellcheckLanguage}", lang);
            return false;
        }
    }

    /// <summary>
    /// Find all installed dictionaries
    /// </summary>
    private void PopulateInstalledDictionaries()
    {
        try
        {
            if (!_fileSystem.Directory.Exists(DictionariesRoot.LocalPath))
                _fileSystem.Directory.CreateDirectory(DictionariesRoot.LocalPath);

            _installedDictionaries.Clear();
            foreach (var dic in _fileSystem.Directory.GetFiles(DictionariesRoot.LocalPath, "*.dic"))
            {
                var name = Path.GetFileNameWithoutExtension(dic);
                _installedDictionaries.Add(
                    new SpellcheckDictionary()
                    {
                        Lang = SpellcheckLanguage.AvailableLanguages.First(l => l.Locale == name),
                        DictionaryPath = new Uri(dic),
                        AffixPath = new Uri(dic.Replace(".dic", ".aff")),
                    }
                );
            }
        }
        catch (Exception)
        {
            _logger.LogError("Failed to populate dictionaries list");
        }
    }

    private static Uri GetDicUri(SpellcheckLanguage locale)
    {
        const string baseUrl =
            "https://raw.githubusercontent.com/LibreOffice/dictionaries/refs/heads/master";
        return new Uri($"{baseUrl}/{locale.Directory}/{locale.Locale}{locale.Suffix}.dic");
    }

    private static Uri GetAffUri(SpellcheckLanguage locale)
    {
        const string baseUrl =
            "https://raw.githubusercontent.com/LibreOffice/dictionaries/refs/heads/master";
        return new Uri($"{baseUrl}/{locale.Directory}/{locale.Locale}{locale.Suffix}.aff");
    }

    /// <summary>
    /// Initialize the dictionary service
    /// </summary>
    public DictionaryService(
        IFileSystem fileSystem,
        ILogger<DictionaryService> logger,
        HttpClient httpClient
    )
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _httpClient = httpClient;
        _installedDictionaries = [];

        InstalledDictionaries =
            new AssCS.Utilities.ReadOnlyObservableCollection<SpellcheckDictionary>(
                _installedDictionaries
            );

        PopulateInstalledDictionaries();
    }
}
