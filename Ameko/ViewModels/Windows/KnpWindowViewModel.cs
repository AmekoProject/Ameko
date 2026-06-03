// SPDX-License-Identifier: GPL-3.0-only

using System.Windows.Input;
using Holo.Models;
using Holo.Providers;
using ReactiveUI;

namespace Ameko.ViewModels.Windows;

public class KnpWindowViewModel : ViewModelBase
{
    public IProjectProvider ProjectProvider { get; set; }
    public Term? SelectedTerm { get; set; } = null;

    public ICommand AddTermCommand { get; }
    public ICommand RemoveTermCommand { get; }

    public KnpWindowViewModel(IProjectProvider projectProvider)
    {
        ProjectProvider = projectProvider;
        AddTermCommand = ReactiveCommand.Create(projectProvider.Current.AddTerm);
        RemoveTermCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedTerm is not null)
                projectProvider.Current.RemoveTerm(SelectedTerm);
        });
    }
}
