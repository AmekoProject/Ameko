// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Holo;
using Holo.Models;
using Holo.Providers;
using ReactiveUI;

namespace Ameko.ViewModels.Dialogs;

public class ProjectConfigDialogViewModel : ViewModelBase
{
    public Project Project { get; }
    public IReadOnlyList<SpellcheckLanguage> AvailableLanguages { get; }
    public ObservableCollection<string> CustomWordSelection { get; } = [];
    public bool CanAddWord => !string.IsNullOrWhiteSpace(NewCustomWord);
    public bool CanRemoveWords => CustomWordSelection.Count != 0;

    public ICommand AddWordCommand { get; }
    public ICommand RemoveWordsCommand { get; }

    public string NewCustomWord
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(CanAddWord));
        }
    } = string.Empty;

    public ProjectConfigDialogViewModel(IProjectProvider projectProvider)
    {
        Project = projectProvider.Current;
        AvailableLanguages = SpellcheckLanguage.AvailableLanguages;

        CustomWordSelection.CollectionChanged += (_, _) =>
            this.RaisePropertyChanged(nameof(CanRemoveWords));

        AddWordCommand = ReactiveCommand.Create(() =>
        {
            Project.AddCustomWord(NewCustomWord);
            NewCustomWord = string.Empty;
        });

        RemoveWordsCommand = ReactiveCommand.Create(() =>
        {
            foreach (var word in CustomWordSelection.ToList()) // Clone contents so we don't modify mid-loop
            {
                Project.RemoveCustomWord(word);
            }
        });
    }
}
