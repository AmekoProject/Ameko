// SPDX-License-Identifier: GPL-3.0-only

using System.Windows.Input;
using Ameko.Messages;
using Ameko.Utilities;
using Ameko.ViewModels.Dialogs;
using AssCS;
using Holo;
using Holo.Configuration;
using ReactiveUI;

namespace Ameko.ViewModels.Windows;

public partial class StylesManagerWindowViewModel : ViewModelBase
{
    private readonly IPersistence _persistence;
    private readonly IViewModelFactory _vmFactory;

    public Project Project { get; }
    public Document Document { get; }
    public IGlobals Globals { get; }

    public Style? SelectedGlobalStyle
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(GlobalButtonsEnabled));
        }
    }

    public Style? SelectedProjectStyle
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(ProjectButtonsEnabled));
        }
    }

    public Style? SelectedDocumentStyle
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(DocumentButtonsEnabled));
        }
    }

    public bool GlobalButtonsEnabled => SelectedGlobalStyle is not null;
    public bool ProjectButtonsEnabled => SelectedProjectStyle is not null;
    public bool DocumentButtonsEnabled => SelectedDocumentStyle is not null;

    public Interaction<
        StyleEditorDialogViewModel,
        StyleEditorDialogClosedMessage?
    > ShowStyleEditorWindow { get; }

    #region Commands
    public ICommand CopyToCommand { get; }
    public ICommand DuplicateCommand { get; }
    public ICommand EditStyleCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand NewStyleCommand { get; }
    #endregion

    public StylesManagerWindowViewModel(
        IPersistence persistence,
        IViewModelFactory vmFactory,
        IGlobals globals,
        Project project,
        Document document
    )
    {
        _persistence = persistence;
        _vmFactory = vmFactory;

        Globals = globals;
        Project = project;
        Document = document;
        DuplicateCommand = CreateDuplicateCommand();
        DeleteCommand = CreateDeleteCommand();
        CopyToCommand = CreateCopyToCommand();
        EditStyleCommand = CreateEditStyleCommand();
        NewStyleCommand = CreateNewStyleCommand();

        ShowStyleEditorWindow =
            new Interaction<StyleEditorDialogViewModel, StyleEditorDialogClosedMessage?>();
    }
}
