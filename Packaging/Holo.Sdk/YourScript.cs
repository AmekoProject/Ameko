// SPDX-License-Identifier: MPL-2.0

using System;
using System.Threading.Tasks;
using Holo.Scripting;
using Holo.Scripting.Models;
using Microsoft.Extensions.Logging;

public class YourScript : HoloScript
{
    private static readonly PackageInfo _info = new()
    {
        DisplayName = "Your Script",
        QualifiedName = "yourName.yourScript"
    };
    public YourScript() : base(_info) {}

    public override async Task<ExecutionResult> ExecuteAsync(string methodName, ScriptArgs args = null)
    {
        Logger.LogInformation($"Hello from {_info.DisplayName}!");
        return ExecutionResult.Success;
    }
}
