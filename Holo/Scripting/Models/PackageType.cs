// SPDX-License-Identifier: MPL-2.0

namespace Holo.Scripting.Models;

/// <summary>
/// Type of <see cref="Package"/>
/// </summary>
public enum PackageType
{
    /// <summary>
    /// The <see cref="Package"/> is an executable <see cref="HoloScript"/>
    /// </summary>
    Script = 0,

    /// <summary>
    /// The <see cref="Package"/> is a library
    /// </summary>
    Library = 1,

    /// <summary>
    /// The <see cref="Package"/> is a <see cref="HoloScriptlet"/>
    /// </summary>
    Scriptlet = 2,
}
