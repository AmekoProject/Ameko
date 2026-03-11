// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Tasks;
using Holo.Providers;
using Holo.Scripting;
using Microsoft.Extensions.Logging;

namespace Ameko.Services;

/// <summary>
/// Provides a way for <see cref="HoloScript"/>s to invoke other scripts, with optional arguments
/// </summary>
public sealed class ScriptInvoker(IScriptService scriptService, ILogger<ScriptInvoker> logger)
    : IScriptInvoker
{
    /// <inheritdoc />
    public async Task<ExecutionResult> InvokeAsync(string qualifiedName, ScriptArgs? args = null)
    {
        logger.LogTrace("Attempting to invoke script {ScriptName}", qualifiedName);
        return await scriptService.ExecuteScriptAsync(qualifiedName, args);
    }
}
