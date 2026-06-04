// SPDX-License-Identifier: MPL-2.0

using System.Diagnostics.CodeAnalysis;
using AssCS;

namespace Holo;

/// <summary>
/// Provides methods for managing reference files within a <see cref="Workspace"/>
/// </summary>
public class ReferenceFileManager : BindableBase
{
    private readonly SelectionManager _selectionManager;

    /// <summary>
    /// Document being referenced
    /// </summary>
    public Document? Reference
    {
        get;
        set
        {
            SetProperty(ref field, value);
            IsReferenceLoaded = value is not null;
            RaisePropertyChanged(nameof(IsReferenceLoaded));
            GetCorrespondingLines();
        }
    }

    /// <summary>
    /// If the reference file is loaded
    /// </summary>
    [MemberNotNullWhen(true, nameof(Reference))]
    public bool IsReferenceLoaded { get; private set; }

    /// <summary>
    /// Currently-visible lines
    /// </summary>
    public string CurrentLines
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>
    /// Shift the timing of the reference file
    /// </summary>
    /// <param name="seconds">Number of seconds to shift by</param>
    public void Shift(int seconds)
    {
        if (!IsReferenceLoaded)
            return;

        var offset = Time.FromSeconds(seconds);
        foreach (var @event in Reference.EventManager.Events)
        {
            @event.Start += offset;
            @event.End += offset;
        }
        GetCorrespondingLines();
    }

    private void GetCorrespondingLines()
    {
        if (!IsReferenceLoaded)
            return;
        var hits = Reference
            .EventManager.Events.Where(e => e.CollidesWith(_selectionManager.ActiveEvent))
            .Select(e => e.Text);
        CurrentLines = string.Join(Environment.NewLine, hits);
    }

    /// <summary>
    /// Instantiate the Reference File Manager
    /// </summary>
    /// <param name="selectionManager">Selection Manager instance to use</param>
    public ReferenceFileManager(SelectionManager selectionManager)
    {
        _selectionManager = selectionManager;
        _selectionManager.PropertyChanged += (_, _) => GetCorrespondingLines();
    }
}
