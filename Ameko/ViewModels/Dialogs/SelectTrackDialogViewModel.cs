// SPDX-License-Identifier: GPL-3.0-only

using System.Linq;
using Ameko.DataModels;
using Ameko.Messages;
using Holo.Media.Providers;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace Ameko.ViewModels.Dialogs;

public class SelectTrackDialogViewModel : ViewModelBase
{
    public TrackInformation[] Tracks { get; }

    public TrackInformation SelectedTrack
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ReactiveCommand<RxVoid, SelectTrackMessage> SelectTrackCommand { get; }

    public SelectTrackDialogViewModel(TrackInfo[] tracks)
    {
        Tracks = tracks
            .Select(t => new TrackInformation { Index = t.Index, Codec = t.Codec })
            .OrderBy(t => t.Index)
            .ToArray();
        SelectedTrack = Tracks[0];

        SelectTrackCommand = ReactiveCommand.Create(() =>
            new SelectTrackMessage(SelectedTrack.Index)
        );
    }
}
