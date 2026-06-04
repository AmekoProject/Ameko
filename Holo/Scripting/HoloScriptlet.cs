// SPDX-License-Identifier: MPL-2.0

using Acornima.Ast;
using Holo.Scripting.Models;
using Jint;

namespace Holo.Scripting;

/// <summary>
/// An executable scriptlet
/// </summary>
public class HoloScriptlet : IHoloExecutable
{
    /// <inheritdoc />
    public required PackageInfo Info { get; init; }

    /// <summary>
    /// Compiled script
    /// </summary>
    public Prepared<Script> CompiledScript { get; init; }
}
