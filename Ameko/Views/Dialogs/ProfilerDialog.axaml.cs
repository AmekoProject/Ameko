// SPDX-License-Identifier: GPL-3.0-only

using Ameko.ViewModels.Dialogs;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace Ameko.Views.Dialogs;

public partial class ProfilerDialog : ReactiveWindow<ProfilerDialogViewModel>
{
    public ProfilerDialog()
    {
        InitializeComponent();

        this.WhenActivated(disposables => { });
    }
}
