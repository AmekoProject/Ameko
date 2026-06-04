// SPDX-License-Identifier: MPL-2.0

using Holo.Models;
using Holo.Scripting.Models;
using Material.Icons;

namespace Holo.Providers;

/// <summary>
/// Provides methods for creating and displaying MessageBoxes
/// </summary>
public interface IMessageBoxService
{
    /// <summary>
    /// Display an OK/Info message box
    /// </summary>
    /// <param name="title">Title of the box</param>
    /// <param name="text">Body of the box</param>
    /// <returns>Which button was clicked</returns>
    Task<MsgBoxButton?> ShowAsync(string title, string text);

    /// <summary>
    /// Display a message box
    /// </summary>
    /// <param name="title">Title of the box</param>
    /// <param name="text">Body of the box</param>
    /// <param name="buttonSet">Which buttons to put on the box</param>
    /// <param name="primary">Which button is default</param>
    /// <param name="iconKind">Icon to use</param>
    /// <returns>Which button was clicked</returns>
    Task<MsgBoxButton?> ShowAsync(
        string title,
        string text,
        MsgBoxButtonSet buttonSet,
        MsgBoxButton primary,
        MaterialIconKind iconKind = MaterialIconKind.Info
    );

    /// <summary>
    /// Display an input box
    /// </summary>
    /// <param name="title">Title of the box</param>
    /// <param name="text">Body of the box</param>
    /// <param name="initialText">Initial text to have in the input</param>
    /// <param name="buttonSet">Which buttons to put on the box</param>
    /// <param name="primary">Which button is default</param>
    /// <param name="iconKind">Icon to use</param>
    /// <returns>Which button was clicked and the user input</returns>
    Task<(MsgBoxButton, string)?> ShowInputAsync(
        string title,
        string text,
        string initialText,
        MsgBoxButtonSet buttonSet,
        MsgBoxButton primary,
        MaterialIconKind iconKind = MaterialIconKind.Info
    );

    /// <summary>
    /// Show boxes for <see cref="InstallationResult"/>s
    /// </summary>
    /// <param name="result">installation result</param>
    /// <returns>Button that was clicked</returns>
    Task<MsgBoxButton?> ShowAsync(InstallationResult result);
}
