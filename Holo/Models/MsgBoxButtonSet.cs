// SPDX-License-Identifier: MPL-2.0

namespace Holo.Models;

/// <summary>
/// Set of buttons present in a MessageBox
/// </summary>
public enum MsgBoxButtonSet
{
    /// <summary>
    /// Just <see cref="MsgBoxButton.Ok"/>
    /// </summary>
    Ok,

    /// <summary>
    /// <see cref="MsgBoxButton.Ok"/> and <see cref="MsgBoxButton.Cancel"/>
    /// </summary>
    OkCancel,

    /// <summary>
    /// <see cref="MsgBoxButton.Yes"/> and <see cref="MsgBoxButton.No"/>
    /// </summary>
    YesNo,

    /// <summary>
    /// <see cref="MsgBoxButton.Yes"/>, <see cref="MsgBoxButton.No"/> and <see cref="MsgBoxButton.Cancel"/>
    /// </summary>
    YesNoCancel,
}
