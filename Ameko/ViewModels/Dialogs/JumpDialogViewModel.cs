// SPDX-License-Identifier: GPL-3.0-only

using Ameko.Messages;
using AssCS;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace Ameko.ViewModels.Dialogs;

public class JumpDialogViewModel : ViewModelBase
{
    public int Frame { get; set; }
    public int Line { get; set; } = 1;
    public Time Time { get; set; } = Time.FromSeconds(0);
    public bool VideoLoaded { get; set; }

    public ReactiveCommand<RxVoid, JumpDialogClosedMessage> ConfirmCommand { get; }

    public JumpDialogViewModel(bool videoLoaded)
    {
        VideoLoaded = videoLoaded;

        ConfirmCommand = ReactiveCommand.Create(() =>
            new JumpDialogClosedMessage(Frame, Line, Time)
        );
    }
}
