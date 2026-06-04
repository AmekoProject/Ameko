// SPDX-License-Identifier: MPL-2.0

using System.ComponentModel;
using AssCS;

namespace Holo.Configuration;

/// <summary>
/// Global (not per-project) values
/// </summary>
public interface IGlobals
{
    /// <summary>
    /// The globals style manager
    /// </summary>
    StyleManager StyleManager { get; }

    /// <summary>
    /// Global colors
    /// </summary>
    AssCS.Utilities.ReadOnlyObservableCollection<Color> Colors { get; }

    /// <summary>
    /// Global custom words
    /// </summary>
    AssCS.Utilities.ReadOnlyObservableCollection<string> CustomWords { get; }

    /// <summary>
    /// Add a global color
    /// </summary>
    /// <param name="color">Color to add</param>
    /// <returns><see langword="true"/> if the color was added</returns>
    bool AddColor(Color color);

    /// <summary>
    /// Remove a global color
    /// </summary>
    /// <param name="color">Color to remove</param>
    /// <returns><see langword="true"/> if the color was removed</returns>
    bool RemoveColor(Color color);

    /// <summary>
    /// Add a global custom word
    /// </summary>
    /// <param name="word">Word to add</param>
    /// <returns><see langword="true"/> if the word was added</returns>
    bool AddCustomWord(string word);

    /// <summary>
    /// Remove a global custom word
    /// </summary>
    /// <param name="word">Word to remove</param>
    /// <returns><see langword="true"/> if the word was removed</returns>
    bool RemoveCustomWord(string word);

    /// <summary>
    /// Write the globals to file
    /// </summary>
    /// <returns><see langword="true"/> if saving was successful</returns>
    bool Save();

    /// <summary>
    /// Raised when a globals property is modified
    /// </summary>
    event PropertyChangedEventHandler? PropertyChanged;
}
