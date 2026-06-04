// SPDX-License-Identifier: MPL-2.0

using System.Globalization;
using Holo.Models;

namespace Holo.Providers;

/// <summary>
/// Service providing methods for managing application cultures
/// </summary>
public interface ICultureService
{
    /// <summary>
    /// List of available languages
    /// </summary>
    public IReadOnlyCollection<DisplayLanguage> AvailableLanguages { get; }

    /// <summary>
    /// List of culture instances
    /// </summary>
    public IReadOnlyCollection<CultureInfo> AvailableCultures { get; }

    /// <summary>
    /// Currently-applied language
    /// </summary>
    public DisplayLanguage CurrentLanguage { get; }
}
