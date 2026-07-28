// SPDX-License-Identifier: GPL-3.0-only

using Holo;
using Holo.Models;

namespace Ameko.ViewModels.Dialogs;

public class ProfilerDialogViewModel : ViewModelBase
{
    public Workspace Workspace { get; }
    public ProfileResult? Result { get; }

    public ProfilerDialogViewModel(Workspace workspace, ProfileTarget target)
    {
        Workspace = workspace;
    }
}
