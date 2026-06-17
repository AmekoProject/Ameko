// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Threading.Tasks;
using Ameko.Messages;
using Ameko.ViewModels;
using Ameko.ViewModels.Controls;
using Ameko.ViewModels.Dialogs;
using Ameko.Views.Dialogs;
using AssCS;
using AssCS.History;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Holo.Configuration.Keybinds;
using Holo.Models;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace Ameko.Views.Controls;

public partial class TabItem : ReactiveUserControl<TabItemViewModel>
{
    /// <summary>
    /// Show an async dialog window
    /// </summary>
    /// <param name="interaction">Interaction</param>
    /// <typeparam name="TDialog">Dialog type</typeparam>
    /// <typeparam name="TViewModel">ViewModel type</typeparam>
    private async Task DoShowDialogAsync<TDialog, TViewModel>(
        IInteractionContext<TViewModel, Unit> interaction
    )
        where TDialog : Window, new()
        where TViewModel : ViewModelBase
    {
        var window = TopLevel.GetTopLevel(this);
        if (window is null)
        {
            interaction.SetOutput(Unit.Default);
            return;
        }
        var dialog = new TDialog { DataContext = interaction.Input };
        await dialog.ShowDialog((Window)window);
        interaction.SetOutput(Unit.Default);
    }

    private async Task DoCopyEventsAsync(IInteractionContext<TabItemViewModel, string?> interaction)
    {
        var window = TopLevel.GetTopLevel(this);
        var selection = interaction.Input.Workspace.SelectionManager.SelectedEventCollection;
        if (window is null || selection.Count == 0)
        {
            interaction.SetOutput(string.Empty);
            return;
        }

        var output = string.Join(
            Environment.NewLine,
            selection.Select(e => e.AsAss(interaction.Input.Workspace.Document.Version))
        );
        await window.Clipboard!.SetTextAsync(output);
        interaction.SetOutput(output);
    }

    private async Task DoCopyPlaintextEventsAsync(
        IInteractionContext<TabItemViewModel, string?> interaction
    )
    {
        var window = TopLevel.GetTopLevel(this);
        var selection = interaction.Input.Workspace.SelectionManager.SelectedEventCollection;
        if (window is null || selection.Count == 0)
        {
            interaction.SetOutput(string.Empty);
            return;
        }

        var output = string.Join(Environment.NewLine, selection.Select(e => e.GetStrippedText()));
        await window.Clipboard!.SetTextAsync(output);
        interaction.SetOutput(output);
    }

    private async Task DoCutEventsAsync(IInteractionContext<TabItemViewModel, string?> interaction)
    {
        await DoCopyEventsAsync(interaction);

        // Do the cutting part
        var workspace = interaction.Input.Workspace;
        var eventManager = workspace.Document.EventManager;
        var selectionManager = workspace.SelectionManager;

        if (selectionManager.SelectedEventCollection.Count > 1)
        {
            // Remove all but the primary selection
            eventManager.Remove(
                selectionManager
                    .SelectedEventCollection.Where(e => e.Id != selectionManager.ActiveEvent.Id)
                    .Select(e => e.Id)
                    .ToList()
            );
        }
        // Get or create the next event to select
        var nextEvent =
            eventManager.GetBefore(selectionManager.ActiveEvent.Id)
            ?? eventManager.GetOrCreateAfter(selectionManager.ActiveEvent.Id);

        eventManager.Remove(selectionManager.ActiveEvent.Id);
        workspace.Commit(nextEvent, ChangeType.RemoveEvent);
        selectionManager.Select(nextEvent);
    }

    private async Task DoPasteEventsAsync(
        IInteractionContext<TabItemViewModel, string[]?> interaction
    )
    {
        var window = TopLevel.GetTopLevel(this);
        if (window is null)
        {
            interaction.SetOutput([]);
            return;
        }

        var output = await window.Clipboard!.TryGetTextAsync();
        if (output is null)
        {
            interaction.SetOutput([]);
            return;
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            interaction.SetOutput([]);
            return;
        }

        interaction.SetOutput(output.Split(Environment.NewLine));
    }

    private async Task DoShowPasteOverDialogAsync(
        IInteractionContext<PasteOverDialogViewModel, PasteOverDialogClosedMessage> interaction
    )
    {
        var window = TopLevel.GetTopLevel(this);
        if (window is null)
        {
            interaction.SetOutput(new PasteOverDialogClosedMessage(EventField.None));
            return;
        }

        var dialog = new PasteOverDialog { DataContext = interaction.Input };
        var result = await dialog.ShowDialog<PasteOverDialogClosedMessage>((Window)window);
        interaction.SetOutput(result);
    }

    private async Task DoShowFileModifiedDialogAsync(
        IInteractionContext<
            FileModifiedDialogViewModel,
            FileModifiedDialogClosedMessage
        > interaction
    )
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var window = TopLevel.GetTopLevel(this);
            if (window is null)
            {
                interaction.SetOutput(
                    new FileModifiedDialogClosedMessage(FileModifiedDialogClosedResult.Ignore)
                );
                return;
            }

            var dialog = new FileModifiedDialog { DataContext = interaction.Input };
            var result = await dialog.ShowDialog<FileModifiedDialogClosedMessage>((Window)window);
            interaction.SetOutput(result);
        });
    }

    private async Task DoShowSaveFrameAsDialogAsync(IInteractionContext<Unit, Uri?> interaction)
    {
        var window = TopLevel.GetTopLevel(this);
        if (window is null)
        {
            interaction.SetOutput(null);
            return;
        }

        var file = await window.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = I18N.Other.FileDialog_SaveFrame_Title,
                FileTypeChoices =
                [
                    new FilePickerFileType(I18N.Other.FileDialog_FileType_Png)
                    {
                        Patterns = ["*.png"],
                    },
                ],
                SuggestedFileName = string.Empty,
            }
        );

        if (file is not null)
        {
            var path = file.Path;
            if (!Path.HasExtension(path.LocalPath))
                path = new Uri(Path.ChangeExtension(path.LocalPath, ".png"));

            interaction.SetOutput(path);
            return;
        }
        interaction.SetOutput(null);
    }

    private async Task DoCopyFrameAsync(IInteractionContext<string, Unit> interaction)
    {
        interaction.SetOutput(Unit.Default);

        var window = TopLevel.GetTopLevel(this);
        if (window is null)
            return;

        var file = await window.StorageProvider.TryGetFileFromPathAsync(interaction.Input);
        if (file is null)
            return;

        await window.Clipboard!.SetFileAsync(file);
    }

    private async Task DoShowStyleEditor(
        IInteractionContext<StyleEditorDialogViewModel, StyleEditorDialogClosedMessage?> interaction
    )
    {
        var window = TopLevel.GetTopLevel(this);
        if (window is null)
        {
            interaction.SetOutput(null);
            return;
        }

        var editor = new StyleEditorDialog { DataContext = interaction.Input };
        var result = await editor.ShowDialog<StyleEditorDialogClosedMessage?>((Window)window);
        interaction.SetOutput(result);
    }

    public TabItem()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.GetObservable(ViewModelProperty)
                .WhereNotNull()
                .Subscribe(vm =>
                {
                    // Alert that the working space has changed
                    MessageBus.Current.SendMessage(new WorkingSpaceChangedMessage());
                    // Listen for scroll messages (?)

                    ApplyLayout(vm, vm.LayoutProvider.Current);
                    AttachKeybinds(vm);
                    TabItemEditorArea.EditBox.Focus();

                    Dispatcher.UIThread.Post(
                        () =>
                        {
                            vm.Workspace.SelectionManager.EndSelectionChange();
                            vm.ProjectProvider.Current.EndSelectionChange();
                        },
                        DispatcherPriority.Background
                    );
                    // csharpier-ignore-start
                    vm.CopyEvents.RegisterHandler(DoCopyEventsAsync);
                    vm.CopyPlaintextEvents.RegisterHandler(DoCopyPlaintextEventsAsync);
                    vm.CutEvents.RegisterHandler(DoCutEventsAsync);
                    vm.PasteEvents.RegisterHandler(DoPasteEventsAsync);
                    vm.ShowPasteOverDialog.RegisterHandler(DoShowPasteOverDialogAsync);
                    vm.ShowFileModifiedDialog.RegisterHandler(DoShowFileModifiedDialogAsync);
                    vm.ShowSpellcheckDialog.RegisterHandler(DoShowDialogAsync<SpellcheckDialog, SpellcheckDialogViewModel>);
                    vm.ShowStyleEditorWindow.RegisterHandler(DoShowStyleEditor);
                    vm.SaveFrameAs.RegisterHandler(DoShowSaveFrameAsDialogAsync);
                    vm.CopyFrame.RegisterHandler(DoCopyFrameAsync);
                    // csharpier-ignore-end

                    // Register keybinds
                    vm.KeybindService.KeybindRegistrar.KeybindsChanged += (_, _) =>
                    {
                        AttachKeybinds(vm);
                    };

                    // Apply layouts
                    vm.LayoutProvider.LayoutChanged += (_, args) =>
                    {
                        ApplyLayout(vm, args.Layout);
                    };
                })
                .DisposeWith(disposables);
        });
    }

    private void ApplyLayout(TabItemViewModel? vm, Layout? layout)
    {
        if (vm is null || layout is null)
            return;

        TabItemGrid.Children.RemoveAll(TabItemGrid.Children.OfType<GridSplitter>());

        TabItemGrid.ColumnDefinitions = new ColumnDefinitions(layout.ColumnDefinitions);
        TabItemGrid.RowDefinitions = new RowDefinitions(layout.RowDefinitions);

        var video = layout.Video;
        TabItemVideoArea.IsVisible = video.IsVisible;
        TabItemVideoArea.SetValue(Grid.ColumnProperty, video.Column);
        TabItemVideoArea.SetValue(Grid.RowProperty, video.Row);
        TabItemVideoArea.SetValue(Grid.ColumnSpanProperty, video.ColumnSpan);
        TabItemVideoArea.SetValue(Grid.RowSpanProperty, video.RowSpan);

        var audio = layout.Audio;
        TabItemAudioArea.IsVisible = audio.IsVisible;
        TabItemAudioArea.SetValue(Grid.ColumnProperty, audio.Column);
        TabItemAudioArea.SetValue(Grid.RowProperty, audio.Row);
        TabItemAudioArea.SetValue(Grid.ColumnSpanProperty, audio.ColumnSpan);
        TabItemAudioArea.SetValue(Grid.RowSpanProperty, audio.RowSpan);

        var editor = layout.Editor;
        TabItemEditorArea.IsVisible = editor.IsVisible;
        TabItemEditorArea.SetValue(Grid.ColumnProperty, editor.Column);
        TabItemEditorArea.SetValue(Grid.RowProperty, editor.Row);
        TabItemEditorArea.SetValue(Grid.ColumnSpanProperty, editor.ColumnSpan);
        TabItemEditorArea.SetValue(Grid.RowSpanProperty, editor.RowSpan);

        var events = layout.Events;
        TabItemEventsArea.IsVisible = events.IsVisible;
        TabItemEventsArea.SetValue(Grid.ColumnProperty, events.Column);
        TabItemEventsArea.SetValue(Grid.RowProperty, events.Row);
        TabItemEventsArea.SetValue(Grid.ColumnSpanProperty, events.ColumnSpan);
        TabItemEventsArea.SetValue(Grid.RowSpanProperty, events.RowSpan);

        foreach (var split in layout.Splitters)
        {
            var splitter = new GridSplitter
            {
                ResizeDirection = split.IsVertical
                    ? GridResizeDirection.Columns
                    : GridResizeDirection.Rows,
                // Background = Brushes.Black,
            };
            splitter.SetValue(Grid.ColumnProperty, split.Column);
            splitter.SetValue(Grid.RowProperty, split.Row);
            splitter.SetValue(Grid.ColumnSpanProperty, split.ColumnSpan);
            splitter.SetValue(Grid.RowSpanProperty, split.RowSpan);
            TabItemGrid.Children.Add(splitter);
        }
    }

    private void AttachKeybinds(TabItemViewModel? vm)
    {
        if (vm is null)
            return;

        vm.CommandService.RegisterCommands(vm.Workspace.Id, vm);
        vm.KeybindService.RegisterCommands(vm.Workspace.Id);

        vm.KeybindService.AttachKeybinds(
            KeybindContext.Grid,
            TabItemEventsArea.EventsGrid,
            vm.Workspace.Id
        );
        vm.KeybindService.AttachKeybinds(KeybindContext.Editor, TabItemEditorArea, vm.Workspace.Id);
        vm.KeybindService.AttachKeybinds(KeybindContext.Video, TabItemVideoArea, vm.Workspace.Id);
        vm.KeybindService.AttachKeybinds(KeybindContext.Audio, TabItemAudioArea, vm.Workspace.Id);

        vm.KeybindService.AttachScriptKeybinds(
            vm.ExecuteScriptCommand,
            KeybindContext.Grid,
            TabItemEventsArea
        );
        vm.KeybindService.AttachScriptKeybinds(
            vm.ExecuteScriptCommand,
            KeybindContext.Editor,
            TabItemEditorArea
        );
        vm.KeybindService.AttachScriptKeybinds(
            vm.ExecuteScriptCommand,
            KeybindContext.Video,
            TabItemVideoArea
        );
        vm.KeybindService.AttachScriptKeybinds(
            vm.ExecuteScriptCommand,
            KeybindContext.Audio,
            TabItemAudioArea
        );
    }
}
