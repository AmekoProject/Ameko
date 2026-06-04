// SPDX-License-Identifier: MPL-2.0

namespace Holo.Models;

/// <summary>
/// A language used for spellchecking
/// </summary>
public class SpellcheckLanguage
{
    /// <summary>
    /// Name of the language
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Locale code
    /// </summary>
    public string Locale { get; }

    /// <summary>
    /// Language directory name
    /// </summary>
    public string Directory { get; }

    /// <summary>
    /// Language directory suffix
    /// </summary>
    public string Suffix { get; }

    private SpellcheckLanguage(
        string name,
        string locale,
        string? directory = null,
        string? suffix = null
    )
    {
        Name = name;
        Locale = locale;
        Directory = directory ?? $"{locale}/";
        Suffix = suffix ?? string.Empty;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object
    /// </summary>
    /// <param name="other">The object to compare with the current object</param>
    /// <returns><see langword="true"/> if the objects are equal</returns>
    protected bool Equals(SpellcheckLanguage other)
    {
        return Locale == other.Locale;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;
        return Equals((SpellcheckLanguage)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Locale.GetHashCode();
    }

    /// <summary>
    /// List of languages available for spellchecking
    /// </summary>
    public static IReadOnlyList<SpellcheckLanguage> AvailableLanguages =>
        new List<SpellcheckLanguage>
        {
            new("English (US)", "en_US", "en/"),
            new("English (GB)", "en_GB", "en/"),
            new("Spanish (ES)", "es_ES", "es/"),
            new("Spanish (MX)", "es_MX", "es/"),
            new("Spanish (AR)", "es_AR", "es/"),
            new("Spanish (PR)", "es_PR", "es/"),
            new("Portuguese (PT)", "pt_PT"),
            new("Portuguese (BR)", "pt_BR"),
            new("French", "fr", "fr_FR/"),
            new("German", "de_DE", "de/", "_frami"),
            new("Italian", "it_IT"),
            new("Russian", "ru_RU"),
            new("Polish", "pl_PL"),
            new("Ukranian", "uk_UA"),
            new("Dutch", "nl_NL"),
            new("Norwegian (NN)", "nn_NO", "no/"),
            new("Norwegian (NB)", "nb_NO", "no/"),
            new("Indonesian", "id_ID", "id/"),
            new("Turkish", "tr_TR"),
            new("Hindi", "hi_IN"),
        }.AsReadOnly();
}
