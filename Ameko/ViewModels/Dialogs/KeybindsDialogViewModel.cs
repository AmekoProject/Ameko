// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Ameko.DataModels;
using Holo.Configuration.Keybinds;
using Holo.Providers;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace Ameko.ViewModels.Dialogs;

public class KeybindsDialogViewModel : ViewModelBase
{
    private readonly IKeybindRegistrar _registrar;
    private readonly IMessageBoxService _messageBoxService;
    private readonly List<string> _keybindsToRemove = [];

    public RangeObservableCollection<EditableKeybind> Keybinds { get; }

    public ReactiveCommand<RxVoid, bool> SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand ResetCommand { get; }

    private async Task<bool> Save()
    {
        // Detect conflicts
        var conflicts = Keybinds
            .Where(k => !string.IsNullOrEmpty(k.Key))
            .GroupBy(k => k.Key)
            .Where(g =>
                g.GroupBy(x => x.OverrideContext.Context).Any(cg => cg.Count() > 1)
                || (
                    g.Any(x => x.OverrideContext.Context == KeybindContext.Global)
                    && g.Any(x => x.OverrideContext.Context != KeybindContext.Global)
                )
            )
            .SelectMany(g => g)
            .Select(k => k.QualifiedName)
            .ToList();

        if (conflicts.Count != 0)
        {
            await _messageBoxService.ShowAsync(
                I18N.Keybinds.Keybinds_MsgBox_Conflict_Title,
                string.Format(
                    I18N.Keybinds.Keybinds_MsgBox_Conflict_Body,
                    $"\n{string.Join('\n', conflicts)}"
                )
            );
            return false;
        }

        foreach (var keybind in Keybinds)
        {
            _registrar.ApplyOverride(
                keybind.QualifiedName,
                keybind.OverrideKey,
                keybind.OverrideContext.Context,
                keybind.IsEnabled
            );
        }

        foreach (var keybind in _keybindsToRemove)
        {
            _registrar.DeregisterKeybind(keybind);
        }

        _registrar.Save();
        return true;
    }

    private static List<EditableKeybind> CreateEditableKeybinds(IEnumerable<Keybind> keybinds)
    {
        return keybinds
            .Select(k => new EditableKeybind
            {
                IsEnabled = k.IsEnabled,
                IsBuiltin = k.IsBuiltin,
                QualifiedName = k.QualifiedName,
                DefaultKey = k.DefaultKey ?? string.Empty,
                OverrideKey = k.OverrideKey,
                DefaultContext = k.DefaultContext,
                OverrideContext = new EditableKeybindContext(k.Context),
            })
            .OrderByDescending(k => k.OverrideContext.Context) // Places Global first and None last
            .ThenBy(k => k.QualifiedName)
            .ToList();
    }

    public KeybindsDialogViewModel(
        IKeybindRegistrar registrar,
        IMessageBoxService messageBoxService
    )
    {
        _registrar = registrar;
        _messageBoxService = messageBoxService;

        Keybinds = new RangeObservableCollection<EditableKeybind>(
            CreateEditableKeybinds(registrar.GetKeybinds())
        );

        SaveCommand = ReactiveCommand.CreateFromTask(Save);
        DeleteCommand = ReactiveCommand.Create(
            (EditableKeybind keybind) =>
            {
                Keybinds.Remove(Keybinds.First(k => k.QualifiedName == keybind.QualifiedName));
                _keybindsToRemove.Add(keybind.QualifiedName);
            }
        );
        ClearCommand = ReactiveCommand.Create(
            (EditableKeybind keybind) =>
            {
                keybind.Key = string.Empty;
            }
        );
        ResetCommand = ReactiveCommand.Create(() =>
        {
            Keybinds.Clear();
            if (registrar.ClearOverrides())
            {
                Keybinds.AddRange(CreateEditableKeybinds(registrar.GetKeybinds()));
            }
        });
    }
}
