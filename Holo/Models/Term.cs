// SPDX-License-Identifier: MPL-2.0

using AssCS;

namespace Holo.Models;

/// <summary>
/// Represents an entry in the Key Names and Phrases bible
/// </summary>
public class Term : BindableBase
{
    /// <summary>
    /// User-specified categorization scheme
    /// </summary>
    /// <example>Name, Spell, Place</example>
    public string Category
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Where the entry can be found
    /// </summary>
    /// <example>Volume 1, Chapter 5</example>
    public string Context
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Term in the original language
    /// </summary>
    /// <example>人を殺す魔法</example>
    public string Original
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Alternative term in the original language
    /// </summary>
    /// <example>ゾルトラーク</example>
    public string Alternate
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Translation of the term
    /// </summary>
    /// <example>Solltrag</example>
    public string Translation
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// If the term only applies to a specific casing
    /// </summary>
    /// <example>"Hero" vs "hero"</example>
    public bool IsCaseSensitive
    {
        get;
        set => SetProperty(ref field, value);
    } = false;

    /// <summary>
    /// Any additional reference notes
    /// </summary>
    public string Notes
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;
}
