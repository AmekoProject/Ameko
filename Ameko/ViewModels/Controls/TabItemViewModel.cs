// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using Ameko.DataModels;
using Ameko.Messages;
using Ameko.Services;
using Ameko.Utilities;
using Ameko.ViewModels.Dialogs;
using AssCS;
using Holo;
using Holo.Configuration;
using Holo.Configuration.Keybinds;
using Holo.Models;
using Holo.Providers;
using ReactiveUI;

namespace Ameko.ViewModels.Controls;

public partial class TabItemViewModel : ViewModelBase
{
    #region Interactions

    public Interaction<TabItemViewModel, string?> CopyEvents { get; }
    public Interaction<TabItemViewModel, string?> CopyPlaintextEvents { get; }
    public Interaction<TabItemViewModel, string?> CutEvents { get; }
    public Interaction<TabItemViewModel, string[]?> PasteEvents { get; }
    public Interaction<
        PasteOverDialogViewModel,
        PasteOverDialogClosedMessage?
    > ShowPasteOverDialog { get; }
    public Interaction<
        FileModifiedDialogViewModel,
        FileModifiedDialogClosedMessage?
    > ShowFileModifiedDialog { get; }
    public Interaction<SpellcheckDialogViewModel, Unit> ShowSpellcheckDialog { get; }
    public Interaction<Event, Unit> ScrollToAndSelectEvent { get; }
    public Interaction<IList<Event>, Unit> SelectEvents { get; }
    public Interaction<Unit, Uri?> SaveFrameAs { get; }
    public Interaction<string, Unit> CopyFrame { get; }

    #endregion

    #region Commands

    // Grid
    [Command("ameko.event.copy", "Ctrl+C", KeybindContext.Grid)]
    public ICommand CopyEventsCommand { get; }

    [Command("ameko.event.cut", "Ctrl+X", KeybindContext.Grid)]
    public ICommand CutEventsCommand { get; }

    [Command("ameko.event.paste", "Ctrl+V", KeybindContext.Grid)]
    public ICommand PasteEventsCommand { get; }

    [Command("ameko.event.pasteOver", "Ctrl+Shift+V", KeybindContext.Grid)]
    public ICommand PasteOverEventsCommand { get; }

    [Command("ameko.event.copyPlaintext", KeybindContext.Grid)]
    public ICommand CopyPlaintextEventsCommand { get; }

    [Command("ameko.event.duplicate", "Ctrl+D", KeybindContext.Grid)]
    public ICommand DuplicateEventsCommand { get; }

    [Command("ameko.event.insertBefore", KeybindContext.Grid)]
    public ICommand InsertEventBeforeCommand { get; }

    [Command("ameko.event.insertAfter", KeybindContext.Grid)]
    public ICommand InsertEventAfterCommand { get; }

    [Command("ameko.event.insertBefore.videoTime", KeybindContext.Grid)]
    public ICommand InsertEventBeforeAtVideoTimeCommand { get; }

    [Command("ameko.event.insertAfter.videoTime", KeybindContext.Grid)]
    public ICommand InsertEventAfterAtVideoTimeCommand { get; }

    [Command("ameko.event.merge", KeybindContext.Grid)]
    public ICommand MergeEventsCommand { get; }

    [Command("ameko.event.split", KeybindContext.Grid)]
    public ICommand SplitEventsCommand { get; }

    [Command("ameko.event.split.keepTimes", KeybindContext.Grid)]
    public ICommand SplitEventsKeepTimesCommand { get; }

    [Command("ameko.event.delete", "Shift+Delete", KeybindContext.Grid)]
    public ICommand DeleteEventsCommand { get; }

    // I don't think this needs a binding?
    public ICommand GetOrCreateAfterCommand { get; }
    public ICommand ToggleTagCommand { get; }

    [Command("ameko.event.toggleComment", KeybindContext.Grid)]
    public ICommand ToggleCommentCommand { get; }
    public ICommand ExecuteScriptCommand { get; }

    // Editor
    [Command("ameko.event.splitCursor", KeybindContext.Editor)]
    public ICommand SplitEventsAtCursorCommand { get; }

    [Command("ameko.event.splitCursor.keepTimes", KeybindContext.Editor)]
    public ICommand SplitEventsAtCursorKeepTimesCommand { get; }

    [Command("ameko.event.spellcheck", "Shift+F7", KeybindContext.Editor)]
    public ICommand SpellcheckEventCommand { get; }

    // Video
    [Command("ameko.video.play", KeybindContext.Video)]
    public ICommand PlayPauseCommand { get; }

    [Command("ameko.video.stop", KeybindContext.Video)]
    public ICommand StopPlayingCommand { get; }

    [Command("ameko.video.playSelection", KeybindContext.Video)]
    public ICommand PlaySelectionCommand { get; }

    [Command("ameko.video.toggleAutoSeek", KeybindContext.Video)]
    public ICommand ToggleAutoSeekCommand { get; }

    [Command("ameko.video.frame.next", KeybindContext.Video)]
    public ICommand NextFrameCommand { get; }

    [Command("ameko.video.frame.previous", KeybindContext.Video)]
    public ICommand PreviousFrameCommand { get; }

    [Command("ameko.video.boundary.next", KeybindContext.Video)]
    public ICommand NextBoundaryCommand { get; }

    [Command("ameko.video.boundary.previous", KeybindContext.Video)]
    public ICommand PreviousBoundaryCommand { get; }

    [Command("ameko.video.keyframe.next", KeybindContext.Video)]
    public ICommand NextKeyframeCommand { get; }

    [Command("ameko.video.keyframe.previous", KeybindContext.Video)]
    public ICommand PreviousKeyframeCommand { get; }

    [Command("ameko.video.active.start", "Ctrl+D1", KeybindContext.Video)]
    public ICommand ActiveStartCommand { get; }

    [Command("ameko.video.active.end", "Ctrl+D2", KeybindContext.Video)]
    public ICommand ActiveEndCommand { get; }

    [Command("ameko.video.zoom.in", "Ctrl+OemPlus", KeybindContext.Video)]
    public ICommand ZoomInCommand { get; }

    [Command("ameko.video.zoom.out", "Ctrl+OemMinus", KeybindContext.Video)]
    public ICommand ZoomOutCommand { get; }

    [Command("ameko.video.rotate.clockwise", KeybindContext.Video)]
    public ICommand RotateClockwiseCommand { get; }

    [Command("ameko.video.rotate.counterclockwise", KeybindContext.Video)]
    public ICommand RotateCounterclockwiseCommand { get; }

    [Command("ameko.video.mute", KeybindContext.Video)]
    public ICommand ToggleMuteCommand { get; }

    // Audio
    [Command("ameko.audio.play.event", KeybindContext.Audio)]
    public ICommand PlayPauseAudioEventCommand { get; }

    [Command("ameko.audio.play.before", KeybindContext.Audio)]
    public ICommand PlayAudioBeforeCommand { get; }

    [Command("ameko.audio.play.first", KeybindContext.Audio)]
    public ICommand PlayAudioFirstCommand { get; }

    [Command("ameko.audio.play.surround", KeybindContext.Audio)]
    public ICommand PlayAudioSurroundCommand { get; }

    [Command("ameko.audio.play.last", KeybindContext.Audio)]
    public ICommand PlayAudioLastCommand { get; }

    [Command("ameko.audio.play.after", KeybindContext.Audio)]
    public ICommand PlayAudioAfterCommand { get; }

    // References
    [Command("ameko.reference.shift.forward", KeybindContext.Editor)]
    public ICommand ShiftReferenceForwardCommand { get; }

    [Command("ameko.reference.shift.backward", KeybindContext.Editor)]
    public ICommand ShiftReferenceBackwardCommand { get; }

    // Frame Saving & Copying
    [Command("ameko.frame.save", KeybindContext.Global)]
    public ICommand SaveFrameCommand { get; }

    [Command("ameko.frame.save.video", KeybindContext.Global)]
    public ICommand SaveFrameVideoOnlyCommand { get; }

    [Command("ameko.frame.save.subtitles", KeybindContext.Global)]
    public ICommand SaveFrameSubtitlesOnlyCommand { get; }

    [Command("ameko.frame.copy", KeybindContext.Global)]
    public ICommand CopyFrameCommand { get; }

    [Command("ameko.frame.copy.video", KeybindContext.Global)]
    public ICommand CopyFrameVideoOnlyCommand { get; }

    [Command("ameko.frame.copy.subtitles", KeybindContext.Global)]
    public ICommand CopyFrameSubtitlesOnlyCommand { get; }

    #endregion

    private readonly IScriptService _scriptService;
    private readonly IMessageService _messageService;
    private readonly IIoService _ioService;
    private readonly IViewModelFactory _vmFactory;

    public Workspace Workspace { get; }

    public IProjectProvider ProjectProvider { get; }
    public IConfiguration Configuration { get; }
    public ICommandService CommandService { get; }
    public IKeybindService KeybindService { get; }
    public ILayoutProvider LayoutProvider { get; }
    public IMacOsClipboardService MacosClipboardService { get; }

    public bool IsIndexing
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public double IndexingProgress
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public int EditBoxSelectionStart
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public int EditBoxSelectionEnd
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public RangeObservableCollection<Term> VisibleKnpTerms { get; } = [];

    #region Versioned items

    public bool DisplayLayers => Workspace.Document.Version is not AssVersion.V400;
    public bool DisplayMarginTopBtm => Workspace.Document.Version is AssVersion.V400PP;

    #endregion

    private IEnumerable<Term> FindVisibleKnpTerms()
    {
        var terms = ProjectProvider.Current.Terms;
        if (terms is not { Count: > 0 })
            yield break;

        var tl = Workspace.SelectionManager.ActiveEvent.Text;
        var org = Workspace.ReferenceFileManager.IsReferenceLoaded
            ? Workspace.ReferenceFileManager.CurrentLines
            : null;

        if (string.IsNullOrEmpty(tl) && org is null)
            yield break;

        foreach (var term in terms)
        {
            var cmp = term.IsCaseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            if (
                (
                    !string.IsNullOrEmpty(tl)
                    && !string.IsNullOrEmpty(term.Translation)
                    && tl.Contains(term.Translation, cmp)
                )
                || (
                    !string.IsNullOrEmpty(org)
                    && !string.IsNullOrEmpty(term.Original)
                    && org.Contains(term.Original, cmp)
                )
                || (
                    !string.IsNullOrEmpty(org)
                    && !string.IsNullOrEmpty(term.Alternate)
                    && org.Contains(term.Alternate, cmp)
                )
            )
                yield return term;
        }
    }

    public TabItemViewModel(
        IProjectProvider projectProvider,
        IConfiguration configuration,
        ICommandService commandService,
        IKeybindService keybindService,
        IScriptService scriptService,
        ILayoutProvider layoutProvider,
        IMessageService messageService,
        IViewModelFactory vmFactory,
        IIoService ioService,
        IMacOsClipboardService macosClipboardService,
        Workspace workspace
    )
    {
        #region Interactions
        CopyEvents = new Interaction<TabItemViewModel, string?>();
        CopyPlaintextEvents = new Interaction<TabItemViewModel, string?>();
        CutEvents = new Interaction<TabItemViewModel, string?>();
        PasteEvents = new Interaction<TabItemViewModel, string[]?>();
        ShowPasteOverDialog =
            new Interaction<PasteOverDialogViewModel, PasteOverDialogClosedMessage?>();
        ShowFileModifiedDialog =
            new Interaction<FileModifiedDialogViewModel, FileModifiedDialogClosedMessage?>();
        ShowSpellcheckDialog = new Interaction<SpellcheckDialogViewModel, Unit>();
        ScrollToAndSelectEvent = new Interaction<Event, Unit>();
        SelectEvents = new Interaction<IList<Event>, Unit>();
        SaveFrameAs = new Interaction<Unit, Uri?>();
        CopyFrame = new Interaction<string, Unit>();
        #endregion

        #region Commands
        // Grid
        CopyEventsCommand = CreateCopyEventsCommand();
        CutEventsCommand = CreateCutEventsCommand();
        PasteEventsCommand = CreatePasteEventsCommand();
        PasteOverEventsCommand = CreatePasteOverEventsCommand();
        CopyPlaintextEventsCommand = CreateCopyPlaintextEventsCommand();
        DuplicateEventsCommand = CreateDuplicateEventsCommand();
        InsertEventBeforeCommand = CreateInsertEventBeforeCommand();
        InsertEventAfterCommand = CreateInsertEventAfterCommand();
        InsertEventBeforeAtVideoTimeCommand = CreateInsertEventBeforeAtVideoTimeCommand();
        InsertEventAfterAtVideoTimeCommand = CreateInsertEventAfterAtVideoTimeCommand();
        MergeEventsCommand = CreateMergeEventsCommand();
        SplitEventsCommand = CreateSplitEventsCommand();
        SplitEventsKeepTimesCommand = CreateSplitEventsKeepTimesCommand();
        DeleteEventsCommand = CreateDeleteEventsCommand();
        GetOrCreateAfterCommand = CreateGetOrCreateAfterCommand();
        ToggleTagCommand = CreateToggleTagCommand();
        ToggleCommentCommand = CreateToggleCommentCommand();

        ShiftReferenceForwardCommand = CreateShiftReferenceForwardCommand();
        ShiftReferenceBackwardCommand = CreateShiftReferenceBackwardCommand();

        ExecuteScriptCommand = CreateExecuteScriptCommand();

        // Editor
        SplitEventsAtCursorCommand = CreateSplitEventsAtCursorCommand();
        SplitEventsAtCursorKeepTimesCommand = CreateSplitEventsAtCursorKeepTimesCommand();
        SpellcheckEventCommand = CreateSpellcheckEventCommand();

        // Video
        PlayPauseCommand = CreatePlayPauseCommand();
        StopPlayingCommand = CreateStopPlayingCommand();
        PlaySelectionCommand = CreatePlaySelectionCommand();
        ToggleAutoSeekCommand = CreateToggleAutoseekCommand();
        NextFrameCommand = CreateNextFrameCommand();
        PreviousFrameCommand = CreatePreviousFrameCommand();
        NextBoundaryCommand = CreateNextBoundaryCommand();
        PreviousBoundaryCommand = CreatePreviousBoundaryCommand();
        NextKeyframeCommand = CreateNextKeyframeCommand();
        PreviousKeyframeCommand = CreatePreviousKeyframeCommand();
        ActiveStartCommand = CreateActiveStartCommand();
        ActiveEndCommand = CreateActiveEndCommand();
        ZoomInCommand = CreateZoomInCommand();
        ZoomOutCommand = CreateZoomOutCommand();
        RotateClockwiseCommand = CreateRotateClockwiseCommand();
        RotateCounterclockwiseCommand = CreateRotateCounterclockwiseCommand();
        ToggleMuteCommand = CreateToggleMuteCommand();

        // Audio
        PlayPauseAudioEventCommand = CreatePlayPauseAudioEventCommand();
        PlayAudioBeforeCommand = CreatePlayAudioBeforeCommand();
        PlayAudioFirstCommand = CreatePlayAudioFirstCommand();
        PlayAudioSurroundCommand = CreatePlayAudioSurroundCommand();
        PlayAudioLastCommand = CreatePlayAudioLastCommand();
        PlayAudioAfterCommand = CreatePlayAudioAfterCommand();

        // Frame Saving & Copying
        SaveFrameCommand = CreateSaveFrameCommand(SaveFrameMode.Full);
        SaveFrameVideoOnlyCommand = CreateSaveFrameCommand(SaveFrameMode.VideoOnly);
        SaveFrameSubtitlesOnlyCommand = CreateSaveFrameCommand(SaveFrameMode.SubtitlesOnly);
        CopyFrameCommand = CreateCopyFrameCommand(SaveFrameMode.Full);
        CopyFrameVideoOnlyCommand = CreateCopyFrameCommand(SaveFrameMode.VideoOnly);
        CopyFrameSubtitlesOnlyCommand = CreateCopyFrameCommand(SaveFrameMode.SubtitlesOnly);

        #endregion

        Workspace = workspace;
        _scriptService = scriptService;
        _messageService = messageService;
        _ioService = ioService;
        ProjectProvider = projectProvider;
        Configuration = configuration;
        CommandService = commandService;
        KeybindService = keybindService;
        LayoutProvider = layoutProvider;
        MacosClipboardService = macosClipboardService;
        _vmFactory = vmFactory;

        Workspace.SelectionManager.SelectionChanged += async (_, _) =>
        {
            try
            {
                await SelectEvents.Handle(Workspace.SelectionManager.SelectedEventCollection);
                VisibleKnpTerms.ReplaceRange(FindVisibleKnpTerms());
            }
            catch
            {
                // ignored
            }
        };

        Workspace.FileModifiedExternally += async (_, _) =>
        {
            var result = await ShowFileModifiedDialog.Handle(
                new FileModifiedDialogViewModel(Workspace.Title)
            );

            if (result is null || result.Result == FileModifiedDialogClosedResult.Ignore)
                return;

            throw new NotImplementedException("FileModifiedDialogClosedResult.SaveAs");
        };
    }
}
