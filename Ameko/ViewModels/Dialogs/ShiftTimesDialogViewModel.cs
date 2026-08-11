// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Linq;
using Ameko.DataModels;
using AssCS;
using AssCS.History;
using Holo;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace Ameko.ViewModels.Dialogs;

public class ShiftTimesDialogViewModel : ViewModelBase
{
    private readonly Workspace _workspace;

    public ReactiveCommand<RxVoid, object?> ConfirmCommand { get; }

    public bool CanShiftFrames { get; set; }

    public Time ShiftTime { get; }

    public int ShiftFrames
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public int ShiftMillis
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ShiftTimesType ShiftType
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ShiftTimesDirection ShiftDirection
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ShiftTimesFilter ShiftFilter
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ShiftTimesTarget ShiftTarget
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    private object? ShiftTimes()
    {
        var events = ShiftFilter switch
        {
            ShiftTimesFilter.AllEvents => _workspace.Document.EventManager.Events,
            ShiftTimesFilter.SelectedEvents =>
                _workspace.SelectionManager.SelectedEventCollection.ToList(),
            _ => throw new ArgumentOutOfRangeException(),
        };

        switch (ShiftType)
        {
            case ShiftTimesType.Time:
                foreach (var @event in events)
                {
                    if (ShiftTarget is ShiftTimesTarget.Start or ShiftTimesTarget.Both)
                    {
                        if (ShiftDirection is ShiftTimesDirection.Forward)
                            @event.Start += ShiftTime;
                        else
                            @event.Start -= ShiftTime;
                    }
                    if (ShiftTarget is ShiftTimesTarget.End or ShiftTimesTarget.Both)
                    {
                        if (ShiftDirection is ShiftTimesDirection.Forward)
                            @event.End += ShiftTime;
                        else
                            @event.End -= ShiftTime;
                    }
                }
                break;
            case ShiftTimesType.Frames:
                var vi = _workspace.MediaController.VideoInfo;
                if (vi is null)
                    break;
                foreach (var @event in events)
                {
                    if (ShiftTarget is ShiftTimesTarget.Start or ShiftTimesTarget.Both)
                    {
                        if (ShiftDirection is ShiftTimesDirection.Forward)
                            @event.Start = vi.TimeFromMidpoint(
                                vi.FrameFromTime(@event.Start) + ShiftFrames - 1,
                                vi.FrameFromTime(@event.Start) + ShiftFrames
                            );
                        else
                            @event.Start = vi.TimeFromMidpoint(
                                vi.FrameFromTime(@event.Start) - ShiftFrames - 1,
                                vi.FrameFromTime(@event.Start) - ShiftFrames
                            );
                    }
                    if (ShiftTarget is ShiftTimesTarget.End or ShiftTimesTarget.Both)
                    {
                        if (ShiftDirection is ShiftTimesDirection.Forward)
                            @event.End = vi.TimeFromMidpoint(
                                vi.FrameFromTime(@event.End) + ShiftFrames - 1,
                                vi.FrameFromTime(@event.End) + ShiftFrames
                            );
                        else
                            @event.End = vi.TimeFromMidpoint(
                                vi.FrameFromTime(@event.End) - ShiftFrames - 1,
                                vi.FrameFromTime(@event.End) - ShiftFrames
                            );
                    }
                }
                break;
            case ShiftTimesType.Milliseconds:
                var msTime = Time.FromMillis(ShiftMillis);
                foreach (var @event in events)
                {
                    if (ShiftTarget is ShiftTimesTarget.Start or ShiftTimesTarget.Both)
                    {
                        if (ShiftDirection is ShiftTimesDirection.Forward)
                            @event.Start += msTime;
                        else
                            @event.Start -= msTime;
                    }
                    if (ShiftTarget is ShiftTimesTarget.End or ShiftTimesTarget.Both)
                    {
                        if (ShiftDirection is ShiftTimesDirection.Forward)
                            @event.End += msTime;
                        else
                            @event.End -= msTime;
                    }
                }
                break;
            default:
                break;
        }

        // Sanity check
        foreach (var @event in events)
        {
            if (ShiftDirection is ShiftTimesDirection.Forward && @event.Start > @event.End)
                @event.End = @event.Start;
            if (ShiftDirection is ShiftTimesDirection.Backward && @event.End < @event.Start)
                @event.Start = @event.End;
        }

        _workspace.Commit(events, ChangeType.ModifyEventMeta);
        return null;
    }

    public ShiftTimesDialogViewModel(Workspace workspace)
    {
        _workspace = workspace;

        ShiftTime = new Time();
        ShiftType = ShiftTimesType.Time;
        ShiftDirection = ShiftTimesDirection.Forward;
        ShiftFilter = ShiftTimesFilter.SelectedEvents;
        ShiftTarget = ShiftTimesTarget.Both;

        CanShiftFrames = workspace.MediaController.IsVideoLoaded;

        ConfirmCommand = ReactiveCommand.Create(ShiftTimes);
    }
}
