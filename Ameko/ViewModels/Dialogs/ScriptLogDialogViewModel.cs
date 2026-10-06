// SPDX-License-Identifier: GPL-3.0-only

using System;
using Ameko.Messages;
using Holo.Scripting;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace Ameko.ViewModels.Dialogs;

public class ScriptLogDialogViewModel(string displayName, ExecutionResult result, HoloLogger logger)
    : ViewModelBase
{
    public string WindowTitle => displayName;

    public string Status =>
        result.Status switch
        {
            ExecutionStatus.Success => I18N.Other.ScriptExecutionStatus_Success,
            ExecutionStatus.Failure => I18N.Other.ScriptExecutionStatus_Failure,
            ExecutionStatus.Warning => I18N.Other.ScriptExecutionStatus_Warning,
            _ => throw new ArgumentOutOfRangeException(),
        };

    public string Logs => string.Join(Environment.NewLine, logger.Logs);

    public ReactiveCommand<RxVoid, EmptyMessage> OkCommand { get; } =
        ReactiveCommand.Create(() => new EmptyMessage());
}
