// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using Ameko.DataModels;
using Ameko.ViewModels.Dialogs;
using AssCS;
using AssCS.History;
using Holo.Media;
using Holo.Models;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;

namespace Ameko.ViewModels.Controls;

public partial class TabItemViewModel
{
    /// <summary>
    /// Command for copying events to the clipboard
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateCopyEventsCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            await CopyEvents.Handle(this);
        });
    }

    /// <summary>
    /// Command for cutting events to the clipboard
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateCutEventsCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            await CutEvents.Handle(this);
        });
    }

    /// <summary>
    /// Paste events from the clipboard
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreatePasteEventsCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var lines = await PasteEvents.Handle(this);
            if (lines is null || lines.Length == 0)
            {
                _messageService.Enqueue(I18N.Other.Message_ClipboardEmpty, TimeSpan.FromSeconds(5));
                return;
            }

            var events = new List<Event>();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                Event? @event;
                if (Event.ValidateAssString(line))
                {
                    @event = Event.FromAss(
                        Workspace.Document.EventManager.NextId,
                        line.Trim(),
                        Workspace.Document.Version // TODO: Detect event version
                    );
                }
                else
                {
                    @event = new Event(Workspace.Document.EventManager.NextId)
                    {
                        Text = line.Trim(),
                    };
                }
                if (@event is not null)
                    events.Add(@event);
            }

            Workspace.Document.EventManager.AddAfter(
                Workspace.SelectionManager.ActiveEvent.Id,
                events
            );
            Workspace.Commit(events, ChangeType.AddEvent);
            Workspace.SelectionManager.Select(events.Last());
        });
    }

    /// <summary>
    /// Paste events from the clipboard over existing events
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreatePasteOverEventsCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var lines = (await PasteEvents.Handle(this))?.ToList();
            if (lines is null || lines.Count == 0)
            {
                _messageService.Enqueue(I18N.Other.Message_ClipboardEmpty, TimeSpan.FromSeconds(5));
                return;
            }

            var vm = _vmFactory.Create<PasteOverDialogViewModel>(lines);
            var fields = (await ShowPasteOverDialog.Handle(vm))?.Fields ?? EventField.None;

            if (fields == EventField.None)
                return;

            var newEvents = new List<Event>();
            var editedEvents = new List<Event>();

            var i = 0;
            var target = Workspace.SelectionManager.ActiveEvent;
            do
            {
                var line = lines[i++];

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (target is not null)
                {
                    if (Event.ValidateAssString(line))
                    {
                        var @event = Event.FromAss(-1, line.Trim(), Workspace.Document.Version); // TODO: Detect event version
                        if (@event is not null)
                            target.SetFields(fields, @event);
                    }
                    else
                    {
                        target.Text = line.Trim();
                    }

                    editedEvents.Add(target);
                }
                else
                {
                    if (Event.ValidateAssString(line))
                    {
                        target = Event.FromAss(
                            Workspace.Document.EventManager.NextId,
                            line.Trim(),
                            Workspace.Document.Version // TODO: Detect event version
                        );
                    }
                    else
                    {
                        target = new Event(Workspace.Document.EventManager.NextId)
                        {
                            Text = line.Trim(),
                        };
                    }

                    if (target is null)
                        continue;
                    Workspace.Document.EventManager.AddLast(target);
                    newEvents.Add(target);
                }
                target = Workspace.Document.EventManager.GetAfter(target.Id);
            } while (i < lines.Count);

            if (editedEvents.Count != 0 && newEvents.Count == 0)
                Workspace.Commit(editedEvents, ChangeType.ComplexEvent); // Use complexEvent to prevent coalescing
            if (newEvents.Count != 0 && editedEvents.Count == 0)
                Workspace.Commit(newEvents, ChangeType.AddEvent);
            if (newEvents.Count != 0 && editedEvents.Count != 0)
            {
                Workspace.Commit(newEvents, ChangeType.ComplexEvent);
                Workspace.SelectionManager.Select(
                    newEvents.Concat(editedEvents).OrderBy(e => e.Id).Last()
                );
            }
        });
    }

    /// <summary>
    /// Command for copying plaintext events to the clipboard
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateCopyPlaintextEventsCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            await CopyPlaintextEvents.Handle(this);
        });
    }

    /// <summary>
    /// Duplicate selected events
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateDuplicateEventsCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            var selection = Workspace.SelectionManager.SelectedEventCollection;
            if (selection.Count == 0)
                return;

            var clones = Workspace.Document.EventManager.Duplicate(selection);

            Workspace.Commit(clones, ChangeType.AddEvent);
            Workspace.SelectionManager.Select(clones.Last(), clones);
        });
    }

    /// <summary>
    /// Insert event before
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateInsertEventBeforeCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            var @event = Workspace.Document.EventManager.InsertBefore(
                Workspace.SelectionManager.ActiveEvent
            );
            Workspace.Commit(@event, ChangeType.AddEvent);
            Workspace.SelectionManager.Select(@event);
        });
    }

    /// <summary>
    /// Insert event after
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateInsertEventAfterCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            var @event = Workspace.Document.EventManager.InsertAfter(
                Workspace.SelectionManager.ActiveEvent
            );
            Workspace.Commit(@event, ChangeType.AddEvent);
            Workspace.SelectionManager.Select(@event);
        });
    }

    /// <summary>
    /// Insert event before
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateInsertEventBeforeAtVideoTimeCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsVideoLoaded)
                return;
            var @event = Workspace.Document.EventManager.InsertBefore(
                Workspace.SelectionManager.ActiveEvent
            );
            @event.Start = Workspace.MediaController.CurrentTime ?? Time.FromSeconds(0);
            @event.End = @event.Start + Time.FromSeconds(5);
            Workspace.Commit(@event, ChangeType.AddEvent);
            Workspace.SelectionManager.Select(@event);
        });
    }

    /// <summary>
    /// Insert event after
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateInsertEventAfterAtVideoTimeCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsVideoLoaded)
                return;
            var @event = Workspace.Document.EventManager.InsertAfter(
                Workspace.SelectionManager.ActiveEvent
            );
            @event.Start = Workspace.MediaController.CurrentTime ?? Time.FromSeconds(0);
            @event.End = @event.Start + Time.FromSeconds(5);
            Workspace.Commit(@event, ChangeType.AddEvent);
            Workspace.SelectionManager.Select(@event);
        });
    }

    /// <summary>
    /// Delete events
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateDeleteEventsCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            var eventManager = Workspace.Document.EventManager;
            var selectionManager = Workspace.SelectionManager;

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
            var toRemove = selectionManager.ActiveEvent;
            var nextEvent =
                eventManager.GetBefore(toRemove.Id) ?? eventManager.GetAfter(toRemove.Id);

            if (nextEvent is null) // We're deleting the only event, need a new "default event"
            {
                nextEvent = eventManager.GetOrCreateAfter(toRemove.Id);
                nextEvent.Start = Time.FromSeconds(0);
                nextEvent.End = Time.FromSeconds(5);

                eventManager.Remove(toRemove.Id);
                Workspace.Commit([nextEvent, toRemove], ChangeType.ComplexEvent);
            }
            else
            {
                eventManager.Remove(toRemove.Id);
                Workspace.Commit(toRemove, ChangeType.RemoveEvent);
            }

            selectionManager.Select(nextEvent);
        });
    }

    /// <summary>
    /// Merge adjacent events
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateMergeEventsCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            var selectionManager = Workspace.SelectionManager;

            var useSoftBreaks =
                ProjectProvider.Current.UseSoftLinebreaks ?? Configuration.UseSoftLinebreaks;

            var newEvent = Workspace.Document.EventManager.Merge(
                selectionManager.SelectedEventCollection,
                useSoftBreaks
            );
            if (newEvent is null)
                return;

            Workspace.Commit(
                [.. selectionManager.SelectedEventCollection, newEvent],
                ChangeType.ComplexEvent
            );
            selectionManager.Select(newEvent);
        });
    }

    /// <summary>
    /// Split events
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateSplitEventsCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            var selectionManager = Workspace.SelectionManager;

            if (selectionManager.SelectedEventCollection.Count == 0)
                return;

            var newEvents = new List<Event>();
            foreach (var @event in selectionManager.SelectedEventCollection)
            {
                newEvents.AddRange(Workspace.Document.EventManager.Split(@event.Id));
            }

            if (newEvents.Count == 0)
                return;

            Workspace.Commit(
                [.. selectionManager.SelectedEventCollection, .. newEvents],
                ChangeType.ComplexEvent
            );
            selectionManager.Select(newEvents.Last());
        });
    }

    /// <summary>
    /// Split events, keeping times
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateSplitEventsKeepTimesCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            var selectionManager = Workspace.SelectionManager;

            if (selectionManager.SelectedEventCollection.Count == 0)
                return;

            var newEvents = new List<Event>();
            foreach (var @event in selectionManager.SelectedEventCollection)
            {
                newEvents.AddRange(
                    Workspace.Document.EventManager.Split(@event.Id, keepTimes: true)
                );
            }

            if (newEvents.Count == 0)
                return;

            Workspace.Commit(
                [.. selectionManager.SelectedEventCollection, .. newEvents],
                ChangeType.ComplexEvent
            );
            selectionManager.Select(newEvents.Last());
        });
    }

    /// <summary>
    /// Split events at cursor
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateSplitEventsAtCursorCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            var selectionManager = Workspace.SelectionManager;

            if (selectionManager.SelectedEventCollection.Count == 0)
                return;

            var newEvents = Workspace
                .Document.EventManager.Split(selectionManager.ActiveEvent.Id, EditBoxSelectionStart)
                .ToList();

            if (newEvents.Count == 0)
                return;

            Workspace.Commit(
                [.. selectionManager.SelectedEventCollection, .. newEvents],
                ChangeType.ComplexEvent
            );
            selectionManager.Select(newEvents.Last());
        });
    }

    /// <summary>
    /// Split events at cursor, keeping times
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateSplitEventsAtCursorKeepTimesCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            var selectionManager = Workspace.SelectionManager;

            if (selectionManager.SelectedEventCollection.Count == 0)
                return;

            var newEvents = Workspace.Document.EventManager.Split(
                selectionManager.ActiveEvent.Id,
                EditBoxSelectionStart,
                keepTimes: true
            );

            if (newEvents.Count == 0)
                return;

            Workspace.Commit(
                [.. selectionManager.SelectedEventCollection, .. newEvents],
                ChangeType.ComplexEvent
            );
            selectionManager.Select(newEvents.Last());
        });
    }

    /// <summary>
    /// Spellcheck the selected event
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateSpellcheckEventCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var selectionManager = Workspace.SelectionManager;

            if (selectionManager.SelectedEventCollection.Count == 0)
                return;

            var vm = _vmFactory.Create<SpellcheckDialogViewModel>(selectionManager.ActiveEvent);
            await ShowSpellcheckDialog.Handle(vm);
        });
    }

    /// <summary>
    /// Edit the current event's style
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateEditStyleCommand()
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            var @event = Workspace.SelectionManager.ActiveEvent;
            if (!Workspace.Document.StyleManager.TryGet(@event.Style, out var style))
            {
                var content = string.Format(I18N.Other.Message_StyleNotFound, @event.Style);
                _messageService.Enqueue(content, TimeSpan.FromSeconds(4));
                return;
            }

            var vm = _vmFactory.Create<StyleEditorDialogViewModel>(
                style,
                Workspace.Document.StyleManager,
                Workspace.Document
            );

            var clone = style.Clone();

            var result = await ShowStyleEditorWindow.Handle(vm);

            // Revert if aborted
            if (result is null)
            {
                style.SetFields(StyleField.All, clone);
                return;
            }
            Workspace.Document.HistoryManager.Commit(ChangeType.ModifyStyle);
        });
    }

    /// <summary>
    /// Get or Create the next event in the document
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateGetOrCreateAfterCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            var initialCount = Workspace.Document.EventManager.Count;
            var nextEvent = Workspace.Document.EventManager.GetOrCreateAfter(
                Workspace.SelectionManager.ActiveEvent.Id
            );
            if (Workspace.Document.EventManager.Count != initialCount)
            {
                Workspace.Commit(nextEvent, ChangeType.AddEvent);
            }
            Workspace.SelectionManager.Select(nextEvent);
        });
    }

    /// <summary>
    /// Toggle an ass tag
    /// </summary>
    private ReactiveCommand<string, RxVoid> CreateToggleTagCommand()
    {
        return ReactiveCommand.Create(
            (string tag) =>
            {
                var @event = Workspace.SelectionManager.ActiveEvent;
                Workspace.Document.StyleManager.TryGet(@event.Style, out var style);

                var shift = @event.ToggleTag(
                    tag,
                    style,
                    EditBoxSelectionStart,
                    EditBoxSelectionEnd
                );
                EditBoxSelectionStart += shift;
                EditBoxSelectionEnd += shift;
            }
        );
    }

    /// <summary>
    /// Toggle whether an event is a comment or not
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateToggleCommentCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            foreach (var @event in Workspace.SelectionManager.SelectedEventCollection)
            {
                @event.IsComment = !@event.IsComment;
            }
            Workspace.Commit(
                Workspace.SelectionManager.SelectedEventCollection,
                ChangeType.ModifyEventMeta
            );
        });
    }

    /// <summary>
    /// Execute a Script
    /// </summary>
    private ReactiveCommand<string, RxVoid> CreateExecuteScriptCommand()
    {
        return ReactiveCommand.CreateFromTask(
            async (string qualifiedName) =>
            {
                await _scriptService.ExecuteScriptAsync(qualifiedName);
            }
        );
    }

    //
    // Video
    //

    /// <summary>
    /// Play/Pause video
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreatePlayPauseCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsVideoLoaded)
                return;
            if (Workspace.MediaController.IsVideoPlaying)
                Workspace.MediaController.Stop();
            else
                Workspace.MediaController.PlayToEnd();
        });
    }

    /// <summary>
    /// Stop video
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateStopPlayingCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsVideoLoaded)
                return;
            Workspace.MediaController.Stop();
        });
    }

    /// <summary>
    /// Play selected events
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreatePlaySelectionCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsVideoLoaded)
                return;
            Workspace.MediaController.PlaySelection(
                Workspace.SelectionManager.SelectedEventCollection
            );
        });
    }

    /// <summary>
    /// Toggle Auto-Seek
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateToggleAutoseekCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsVideoLoaded)
                return;
            Workspace.MediaController.IsAutoSeekEnabled = !Workspace
                .MediaController
                .IsAutoSeekEnabled;
        });
    }

    /// <summary>
    /// Go to next frame
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateNextFrameCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsVideoLoaded)
                return;
            Workspace.MediaController.SeekTo(Workspace.MediaController.CurrentFrame + 1);
        });
    }

    /// <summary>
    /// Go to previous frame
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreatePreviousFrameCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsVideoLoaded)
                return;
            Workspace.MediaController.SeekTo(Workspace.MediaController.CurrentFrame - 1);
        });
    }

    /// <summary>
    /// Go to next boundary
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateNextBoundaryCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsVideoLoaded)
                return;

            var time = Workspace.MediaController.CurrentTime ?? Time.Zero;
            var result = Time.Maximum;

            foreach (var e in Workspace.Document.EventManager.Events)
            {
                if (e.Start > time && e.Start < result)
                    result = e.Start;
                if (e.End > time && e.End < result)
                    result = e.End;
            }

            Workspace.MediaController.SeekTo(result);
        });
    }

    /// <summary>
    /// Go to previous boundary
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreatePreviousBoundaryCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsVideoLoaded)
                return;

            var superSeek = _seekToPrevInvokedAt + FiveHundredMs > DateTimeOffset.Now;

            var time = Workspace.MediaController.CurrentTime ?? Time.Zero;
            var result = Time.Minimum;

            var events = Workspace.Document.EventManager.Events;
            for (var i = 0; i < events.Count; i++)
            {
                var e = events[i];
                if (e.Start < time && e.Start > result)
                {
                    if (i > 0 && superSeek)
                        result = events[i - 1].End;
                    else
                        result = e.Start;
                }

                if (e.End < time && e.End > result)
                {
                    result = !superSeek ? e.End : e.Start;
                }
            }

            Workspace.MediaController.SeekTo(result);

            _seekToPrevInvokedAt = DateTimeOffset.Now;
        });
    }

    /// <summary>
    /// Go to next keyframe
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateNextKeyframeCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsVideoLoaded)
                return;
            var nextKeyframe = Workspace.MediaController.VideoInfo?.Keyframes.FirstOrDefault(kf =>
                kf > Workspace.MediaController.CurrentFrame
            );

            if (nextKeyframe is not null)
                Workspace.MediaController.SeekTo(nextKeyframe.Value);
        });
    }

    /// <summary>
    /// Go to previous keyframe
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreatePreviousKeyframeCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsVideoLoaded)
                return;
            var previousKeyframe = Workspace.MediaController.VideoInfo?.Keyframes.LastOrDefault(
                kf => kf < Workspace.MediaController.CurrentFrame
            );

            if (previousKeyframe is null)
                return;

            if (_seekToPrevInvokedAt + FiveHundredMs > DateTimeOffset.Now)
            {
                var superSeekKeyframe =
                    Workspace.MediaController.VideoInfo?.Keyframes.LastOrDefault(kf =>
                        kf < previousKeyframe
                    );
                if (superSeekKeyframe is not null)
                    previousKeyframe = superSeekKeyframe;
            }

            Workspace.MediaController.SeekTo(previousKeyframe.Value);

            _seekToPrevInvokedAt = DateTimeOffset.Now;
        });
    }

    /// <summary>
    /// Go to start of active event
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateActiveStartCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsVideoLoaded)
                return;
            var selection = Workspace.SelectionManager.ActiveEvent;
            Workspace.MediaController.SeekTo(selection);
        });
    }

    /// <summary>
    /// Go to end of active event
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateActiveEndCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsVideoLoaded)
                return;
            var selection = Workspace.SelectionManager.ActiveEvent;
            Workspace.MediaController.SeekToEnd(selection);
        });
    }

    /// <summary>
    /// Increase video zoom level
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateZoomInCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsVideoLoaded)
                return;
            var scales = ScaleFactor.Scales;
            var index = scales.IndexOf(Workspace.MediaController.ScaleFactor);

            if (index < scales.Count - 1)
                Workspace.MediaController.ScaleFactor = scales[index + 1];
        });
    }

    /// <summary>
    /// Decrease video zoom level
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateZoomOutCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsVideoLoaded)
                return;
            var scales = ScaleFactor.Scales;
            var index = scales.IndexOf(Workspace.MediaController.ScaleFactor);

            if (index > 0)
                Workspace.MediaController.ScaleFactor = scales[index - 1];
        });
    }

    /// <summary>
    /// Rotate Clockwise
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateRotateClockwiseCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsVideoLoaded)
                return;
            var angles = RotationalFactor.Angles;
            var index = angles.IndexOf(Workspace.MediaController.RotationalFactor);

            if (index < angles.Count - 1)
                Workspace.MediaController.RotationalFactor = angles[index + 1];
        });
    }

    /// <summary>
    /// Rotate Counterclockwise
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateRotateCounterclockwiseCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsVideoLoaded)
                return;
            var angles = RotationalFactor.Angles;
            var index = angles.IndexOf(Workspace.MediaController.RotationalFactor);

            if (index > 0)
                Workspace.MediaController.RotationalFactor = angles[index - 1];
        });
    }

    /// <summary>
    /// Toggle mute
    /// </summary>
    /// <returns></returns>
    private ReactiveCommand<RxVoid, RxVoid> CreateToggleMuteCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsVideoLoaded)
                return;
            Workspace.MediaController.IsMuted = !Workspace.MediaController.IsMuted;
        });
    }

    private ReactiveCommand<RxVoid, RxVoid> CreatePlayPauseAudioEventCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsAudioLoaded)
                return;

            if (Workspace.MediaController.IsAudioPlaying)
                Workspace.MediaController.Stop();
            else
                Workspace.MediaController.PlayAudioSelection(
                    Workspace.SelectionManager.ActiveEvent,
                    AudioPlaybackKind.Event
                );
        });
    }

    private ReactiveCommand<RxVoid, RxVoid> CreatePlayAudioBeforeCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsAudioLoaded)
                return;

            Workspace.MediaController.PlayAudioSelection(
                Workspace.SelectionManager.ActiveEvent,
                AudioPlaybackKind.Before
            );
        });
    }

    private ReactiveCommand<RxVoid, RxVoid> CreatePlayAudioFirstCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsAudioLoaded)
                return;

            Workspace.MediaController.PlayAudioSelection(
                Workspace.SelectionManager.ActiveEvent,
                AudioPlaybackKind.First
            );
        });
    }

    private ReactiveCommand<RxVoid, RxVoid> CreatePlayAudioSurroundCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsAudioLoaded)
                return;

            Workspace.MediaController.PlayAudioSelection(
                Workspace.SelectionManager.ActiveEvent,
                AudioPlaybackKind.Surround
            );
        });
    }

    private ReactiveCommand<RxVoid, RxVoid> CreatePlayAudioLastCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsAudioLoaded)
                return;

            Workspace.MediaController.PlayAudioSelection(
                Workspace.SelectionManager.ActiveEvent,
                AudioPlaybackKind.Last
            );
        });
    }

    private ReactiveCommand<RxVoid, RxVoid> CreatePlayAudioAfterCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.MediaController.IsAudioLoaded)
                return;

            Workspace.MediaController.PlayAudioSelection(
                Workspace.SelectionManager.ActiveEvent,
                AudioPlaybackKind.After
            );
        });
    }

    /// <summary>
    /// Shift reference file forwards
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateShiftReferenceForwardCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.ReferenceFileManager.IsReferenceLoaded)
                return;
            Workspace.ReferenceFileManager.Shift(1);
        });
    }

    /// <summary>
    /// Shift reference file forwards
    /// </summary>
    private ReactiveCommand<RxVoid, RxVoid> CreateShiftReferenceBackwardCommand()
    {
        return ReactiveCommand.Create(() =>
        {
            if (!Workspace.ReferenceFileManager.IsReferenceLoaded)
                return;
            Workspace.ReferenceFileManager.Shift(-1);
        });
    }

    /// <summary>
    /// Save frame to disk
    /// </summary>
    /// <param name="mode">Save Frame Mode</param>
    private ReactiveCommand<RxVoid, RxVoid> CreateSaveFrameCommand(SaveFrameMode mode)
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            if (!Workspace.MediaController.IsVideoLoaded)
                return;
            _ = await _ioService.SaveFrameToFile(SaveFrameAs, Workspace, mode);
        });
    }

    /// <summary>
    /// Copy frame to clipboard
    /// </summary>
    /// <param name="mode"></param>
    private ReactiveCommand<RxVoid, RxVoid> CreateCopyFrameCommand(SaveFrameMode mode)
    {
        return ReactiveCommand.CreateFromTask(async () =>
        {
            if (!Workspace.MediaController.IsVideoLoaded)
                return;
            _ = await _ioService.CopyFrameToClipboard(CopyFrame, Workspace, mode);
        });
    }
}
