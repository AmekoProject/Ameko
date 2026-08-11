// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Threading.Tasks;
using Ameko.ViewModels.Controls;
using AssCS.History;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;

namespace Ameko.Views.Controls;

public partial class TabItemEditorArea : ReactiveUserControl<TabItemViewModel>
{
    private bool UseSoftLinebreaks =>
        ViewModel?.ProjectProvider.Current.UseSoftLinebreaks
        ?? ViewModel?.Configuration.UseSoftLinebreaks
        ?? false;

    private void EditBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel is null)
            return;

        // Select Emacs keybind implementations
        // since Avalonia currently doesn't have them >:(
        if (OperatingSystem.IsMacOS() && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            var length = EditBox.Text?.Length ?? 0;
            switch (e.Key)
            {
                // Movement
                case Key.A:
                    EditBox.CaretIndex = 0;
                    e.Handled = true;
                    break;
                case Key.E:
                    EditBox.CaretIndex = length;
                    e.Handled = true;
                    break;
                case Key.F:
                    if (EditBox.CaretIndex < length)
                        EditBox.CaretIndex += 1;
                    e.Handled = true;
                    break;
                case Key.B:
                    if (EditBox.CaretIndex > 0)
                        EditBox.CaretIndex -= 1;
                    e.Handled = true;
                    break;
                // Local clipboard
                case Key.K:
                    if (EditBox.Text is null)
                        break;
                    if (EditBox.SelectionEnd == EditBox.SelectionStart)
                        EditBox.SelectionEnd = length;

                    // Swap if needed
                    if (EditBox.SelectionEnd < EditBox.SelectionStart)
                    {
                        (EditBox.SelectionStart, EditBox.SelectionEnd) = (
                            EditBox.SelectionEnd,
                            EditBox.SelectionStart
                        );
                    }

                    ViewModel.MacosClipboardService.Set(
                        EditBox.Text[EditBox.SelectionStart..EditBox.SelectionEnd]
                    );

                    EditBox.Text =
                        EditBox.Text[..EditBox.SelectionStart]
                        + EditBox.Text[EditBox.SelectionEnd..];

                    EditBox.SelectionEnd = EditBox.SelectionStart; // Clear selection (if applicable)
                    e.Handled = true;
                    break;
                case Key.Y:
                    if (EditBox.Text is null)
                        break;
                    var paste = ViewModel.MacosClipboardService.Get();
                    if (!string.IsNullOrEmpty(paste))
                    {
                        // Swap if needed
                        if (EditBox.SelectionEnd < EditBox.SelectionStart)
                        {
                            (EditBox.SelectionStart, EditBox.SelectionEnd) = (
                                EditBox.SelectionEnd,
                                EditBox.SelectionStart
                            );
                        }

                        EditBox.Text =
                            EditBox.Text[..EditBox.SelectionStart]
                            + paste
                            + EditBox.Text[EditBox.SelectionEnd..];

                        EditBox.CaretIndex += paste.Length;
                    }
                    e.Handled = true;
                    break;
                // Additional newline option ig
                case Key.O:
                    InsertNewline();
                    e.Handled = true;
                    break;
            }

            // Skip everything else if we did something here
            if (e.Handled)
                return;
        }

        if (e.Key != Key.Enter)
            return;

        e.Handled = true;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            InsertNewline();
        }
        else
        {
            ViewModel.GetOrCreateAfterCommand.Execute(RxVoid.Default);
            EditBox.Focus();
        }
    }

    // TODO: These don't differentiate between user input and program input
    private void EditBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var blocked =
            ViewModel?.Workspace.SelectionManager.IsSelectionChanging == true
            || ViewModel?.ProjectProvider.Current.IsSelectionChanging == true;
        if (blocked)
            return;

        ViewModel?.Workspace.Commit(
            ViewModel.Workspace.SelectionManager.ActiveEvent,
            ViewModel.Workspace.SelectionManager.SelectedEventCollection,
            ChangeType.ModifyEventText
        );
    }

    private void AnyControl_EventMetaChanged(object? sender, RoutedEventArgs e)
    {
        var blocked =
            ViewModel?.Workspace.SelectionManager.IsSelectionChanging == true
            || ViewModel?.ProjectProvider.Current.IsSelectionChanging == true;
        if (blocked)
            return;

        ViewModel?.Workspace.Commit(
            ViewModel.Workspace.SelectionManager.ActiveEvent,
            ViewModel.Workspace.SelectionManager.SelectedEventCollection,
            ChangeType.ModifyEventMeta
        );
    }

    private void AnyControl_EventTimeChanged(object? sender, TextChangedEventArgs e)
    {
        var blocked =
            ViewModel?.Workspace.SelectionManager.IsSelectionChanging == true
            || ViewModel?.ProjectProvider.Current.IsSelectionChanging == true;
        if (blocked)
            return;

        ViewModel?.Workspace.Commit(
            ViewModel.Workspace.SelectionManager.ActiveEvent,
            ViewModel.Workspace.SelectionManager.SelectedEventCollection,
            ChangeType.ModifyEventMeta
        );
    }

    private void InsertNewline()
    {
        var idx = EditBox.CaretIndex;
        if (ViewModel!.Workspace.SelectionManager.ActiveEvent.Effect.Contains("code"))
        {
            EditBox.Text = EditBox.Text?.Insert(idx, Environment.NewLine);
            EditBox.CaretIndex += Environment.NewLine.Length;
        }
        else
        {
            EditBox.Text = EditBox.Text?.Insert(idx, UseSoftLinebreaks ? "\\n" : "\\N");
            EditBox.CaretIndex += 2;
        }
    }

    public TabItemEditorArea()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            if (ViewModel is not { } vm)
                return;

            // TODO: Keybinds

            EditBox.AddHandler(
                TextBox.TextChangedEvent,
                EditBox_OnTextChanged,
                RoutingStrategies.Bubble
            );
            EditBox.AddHandler(KeyDownEvent, EditBox_OnKeyDown, RoutingStrategies.Bubble);
            EditBox.AddHandler(
                TextBox.PastingFromClipboardEvent,
                EditBoxOnPastingFromClipboard,
                RoutingStrategies.Bubble
            );
            new ActionDisposable(() => { }).DisposeWith(disposables);
        });
    }

    private async Task EditBoxOnPastingFromClipboard(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null)
            e.Handled = true;
        else
            return;

        var text = await clipboard.TryGetTextAsync();
        if (!ViewModel!.Workspace.SelectionManager.ActiveEvent.Effect.Contains("code"))
            text = text?.Replace(Environment.NewLine, UseSoftLinebreaks ? @"\n" : @"\N");

        if (text is not null && EditBox.Text is not null)
        {
            // Swap if needed
            if (EditBox.SelectionEnd < EditBox.SelectionStart)
            {
                (EditBox.SelectionStart, EditBox.SelectionEnd) = (
                    EditBox.SelectionEnd,
                    EditBox.SelectionStart
                );
            }

            // No selection → insert at caret
            if (EditBox.SelectionStart == EditBox.SelectionEnd)
            {
                EditBox.Text = EditBox.Text?.Insert(EditBox.CaretIndex, text);
                EditBox.CaretIndex += text.Length;
            }
            // Selection → replace selection
            else
            {
                var start = EditBox.SelectionStart;
                var end = EditBox.SelectionEnd;
                EditBox.Text = EditBox.Text[..start] + text + EditBox.Text[end..];
                EditBox.CaretIndex = start + text.Length;
            }
        }
    }
}
