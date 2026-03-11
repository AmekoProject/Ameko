// SPDX-License-Identifier: MPL-2.0

using Holo.Scripting;

namespace Holo.Providers;

/// <summary>
/// Defines a way for <see cref="HoloScript"/>s to invoke other scripts, with optional arguments
/// </summary>
public interface IScriptInvoker
{
    /// <summary>
    /// Invokes a script
    /// </summary>
    /// <param name="qualifiedName">Name of the script or method to invoke</param>
    /// <param name="args">Optional arguments</param>
    /// <returns>Result returned by the invoked script</returns>
    /// <remarks>
    /// <para>Returns <see cref="ExecutionStatus.Failure"/> if the script or method is not found.</para>
    /// <para>
    /// This method attempts to execute a script first, then a method.
    /// This relies on method names being <c>scriptName+methodName</c>.
    /// </para>
    /// </remarks>
    Task<ExecutionResult> InvokeAsync(string qualifiedName, ScriptArgs? args = null);
}
