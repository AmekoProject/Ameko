// SPDX-License-Identifier: MPL-2.0

namespace Holo.Providers;

/// <summary>
/// Provides methods for creating and displaying windows
/// </summary>
public interface IWindowService
{
    /// <summary>
    /// Display a window
    /// </summary>
    /// <param name="window">Window to display</param>
    /// <param name="width">Width of the window. Auto if <see langword="null"/>.</param>
    /// <param name="height">Height of the window. Auto if <see langword="null"/>.</param>
    /// <param name="canResize">If the user can resize the window</param>
    void ShowWindow(object window, int? width = null, int? height = null, bool canResize = false);

    /// <summary>
    /// Display a dialog window
    /// </summary>
    /// <param name="window">Window to display</param>
    /// <param name="width">Width of the window. Auto if <see langword="null"/>.</param>
    /// <param name="height">Height of the window. Auto if <see langword="null"/>.</param>
    /// <param name="canResize">If the user can resize the window</param>
    Task ShowDialogAsync(
        object window,
        int? width = null,
        int? height = null,
        bool canResize = false
    );

    /// <summary>
    /// Display a dialog window that returns an object
    /// </summary>
    /// <param name="window">Window to display</param>
    /// <param name="width">Width of the window. Auto if <see langword="null"/>.</param>
    /// <param name="height">Height of the window. Auto if <see langword="null"/>.</param>
    /// <param name="canResize">If the user can resize the window</param>
    /// <typeparam name="T">Type of object to return</typeparam>
    /// <returns>Returned object</returns>
    Task<T?> ShowDialogAsync<T>(
        object window,
        int? width = null,
        int? height = null,
        bool canResize = false
    )
        where T : class;

    /// <summary>
    /// Open the help window to the page corresponding to the script's
    /// <paramref name="qualifiedName"/>
    /// </summary>
    /// <param name="qualifiedName">Qualified name of the page to open</param>
    /// <remarks>
    /// <paramref name="qualifiedName"/>s beginning with <c>ameko</c> are expected to be builtin pages, not scripts
    /// </remarks>
    void ShowHelpWindow(string qualifiedName);
}
