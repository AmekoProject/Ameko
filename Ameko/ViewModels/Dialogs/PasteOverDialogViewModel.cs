// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Ameko.Messages;
using AssCS;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace Ameko.ViewModels.Dialogs;

public class PasteOverDialogViewModel : ViewModelBase
{
    private EventField _fields;

    public ICommand SelectAllCommand { get; }
    public ICommand SelectNoneCommand { get; }
    public ICommand SelectTimesCommand { get; }
    public ICommand SelectTextCommand { get; }
    public ReactiveCommand<RxVoid, PasteOverDialogClosedMessage> ConfirmCommand { get; }

    public bool IsAssContent { get; set; }

    public EventField Fields
    {
        get => _fields;
        set
        {
            _fields = value;
            RaiseChanges();
        }
    }

    public bool Comment
    {
        get => Fields.HasFlag(EventField.Comment);
        set => SetFlag(EventField.Comment, value);
    }

    public bool Layer
    {
        get => Fields.HasFlag(EventField.Layer);
        set => SetFlag(EventField.Layer, value);
    }

    public bool StartTime
    {
        get => Fields.HasFlag(EventField.StartTime);
        set => SetFlag(EventField.StartTime, value);
    }

    public bool EndTime
    {
        get => Fields.HasFlag(EventField.EndTime);
        set => SetFlag(EventField.EndTime, value);
    }

    public bool Style
    {
        get => Fields.HasFlag(EventField.Style);
        set => SetFlag(EventField.Style, value);
    }

    public bool Actor
    {
        get => Fields.HasFlag(EventField.Actor);
        set => SetFlag(EventField.Actor, value);
    }

    public bool MarginLeft
    {
        get => Fields.HasFlag(EventField.MarginLeft);
        set => SetFlag(EventField.MarginLeft, value);
    }

    public bool MarginRight
    {
        get => Fields.HasFlag(EventField.MarginRight);
        set => SetFlag(EventField.MarginRight, value);
    }

    public bool MarginVertical
    {
        get => Fields.HasFlag(EventField.MarginVertical);
        set => SetFlag(EventField.MarginVertical, value);
    }

    public bool Effect
    {
        get => Fields.HasFlag(EventField.Effect);
        set => SetFlag(EventField.Effect, value);
    }

    public bool Text
    {
        get => Fields.HasFlag(EventField.Text);
        set => SetFlag(EventField.Text, value);
    }

    private void SetFlag(EventField flag, bool enabled)
    {
        _fields = enabled ? _fields | flag : _fields & ~flag;
        RaiseChanges();
    }

    private void RaiseChanges()
    {
        this.RaisePropertyChanged(nameof(Fields));
        this.RaisePropertyChanged(nameof(Comment));
        this.RaisePropertyChanged(nameof(Layer));
        this.RaisePropertyChanged(nameof(StartTime));
        this.RaisePropertyChanged(nameof(EndTime));
        this.RaisePropertyChanged(nameof(Style));
        this.RaisePropertyChanged(nameof(Actor));
        this.RaisePropertyChanged(nameof(MarginLeft));
        this.RaisePropertyChanged(nameof(MarginRight));
        this.RaisePropertyChanged(nameof(MarginVertical));
        this.RaisePropertyChanged(nameof(Effect));
        this.RaisePropertyChanged(nameof(Text));
    }

    public PasteOverDialogViewModel(IEnumerable<string> pastedContent)
    {
        _fields = EventField.Text;

        IsAssContent = pastedContent.All(Event.ValidateAssString);

        SelectAllCommand = ReactiveCommand.Create(() =>
            Fields =
                EventField.Comment
                | EventField.Layer
                | EventField.StartTime
                | EventField.EndTime
                | EventField.Style
                | EventField.Actor
                | EventField.MarginLeft
                | EventField.MarginRight
                | EventField.MarginVertical
                | EventField.Effect
                | EventField.Text
        );
        SelectNoneCommand = ReactiveCommand.Create(() => Fields = EventField.None);
        SelectTimesCommand = ReactiveCommand.Create(() =>
            Fields = EventField.StartTime | EventField.EndTime
        );
        SelectTextCommand = ReactiveCommand.Create(() => Fields = EventField.Text);

        ConfirmCommand = ReactiveCommand.Create(() => new PasteOverDialogClosedMessage(Fields));
    }
}
