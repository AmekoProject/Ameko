// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Holo.Scripting;

namespace Ameko.Services;

public interface IScriptService
{
    /// <summary>
    /// List of currently-loaded scripts
    /// </summary>
    AssCS.Utilities.ReadOnlyObservableCollection<IHoloExecutable> Scripts { get; }

    /// <summary>
    /// Get a script by its qualified name
    /// </summary>
    /// <param name="qualifiedName">Name of the script</param>
    /// <param name="script">Script, if found</param>
    /// <returns><see langword="true"/> if found</returns>
    bool TryGetScript(string qualifiedName, [NotNullWhen(true)] out HoloScript? script);

    /// <summary>
    /// Get a scriptlet by its qualified name
    /// </summary>
    /// <param name="qualifiedName">Name of the script</param>
    /// <param name="script">Scriptlet, if found</param>
    /// <returns><see langword="true"/> if found</returns>
    bool TryGetScriptlet(string qualifiedName, [NotNullWhen(true)] out HoloScriptlet? script);

    /// <summary>
    /// Asynchronously execute a script or method
    /// </summary>
    /// <param name="qualifiedName">Name of the script or method</param>
    /// <param name="args">Arguments passed to the script</param>
    /// <returns>Execution result of the script or method</returns>
    /// <remarks>
    /// This method attempts to execute a script first, then a method.
    /// This relies on method names being <c>scriptName+methodName</c>.
    /// </remarks>
    Task<ExecutionResult> ExecuteScriptAsync(string qualifiedName, ScriptArgs? args = null);

    /// <summary>
    /// Execute a playground script
    /// </summary>
    /// <param name="content">Content of the script</param>
    /// <param name="csharp">If this is a C# script</param>
    /// <returns>Status result</returns>
    string ExecutePlaygroundScript(string content, bool csharp);

    /// <summary>
    /// Reload scripts
    /// </summary>
    /// <param name="isManual">Whether to show the message box</param>
    Task Reload(bool isManual);

    event EventHandler<EventArgs>? Reloaded;
}
