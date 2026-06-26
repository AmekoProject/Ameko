// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Holo;
using Holo.Models;
using Holo.Providers;

namespace Ameko.ViewModels.Windows;

public class ProfilerWindowViewModel : ViewModelBase
{
    public IProjectProvider ProjectProvider { get; set; }

    public ReadOnlyObservableCollection<Workspace> Workspaces { get; }
    public Dictionary<Workspace, ProfileResult?> Results { get; }
    public Workspace? SelectedWorkspace { get; set; }

    public ProfilerWindowViewModel(IProjectProvider projectProvider)
    {
        ProjectProvider = projectProvider;
        Workspaces = projectProvider.Current.LoadedWorkspaces;
        SelectedWorkspace = projectProvider.Current.WorkingSpace;
        Results = [];
    }
}
