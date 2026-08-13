// SPDX-License-Identifier: GPL-3.0-only

using Ameko.Messages;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace Ameko.ViewModels.Dialogs;

public class FileModifiedDialogViewModel(string fileName) : ViewModelBase
{
    public string Header { get; } = string.Format(I18N.FileModified.FileModified_Header, fileName);

    public ReactiveCommand<RxVoid, FileModifiedDialogClosedMessage> IgnoreCommand { get; } =
        ReactiveCommand.Create(() =>
            new FileModifiedDialogClosedMessage(FileModifiedDialogClosedResult.Ignore)
        );

    public ReactiveCommand<RxVoid, FileModifiedDialogClosedMessage> SaveAsCommand { get; } =
        ReactiveCommand.Create(() =>
            new FileModifiedDialogClosedMessage(FileModifiedDialogClosedResult.SaveAs)
        );
}
