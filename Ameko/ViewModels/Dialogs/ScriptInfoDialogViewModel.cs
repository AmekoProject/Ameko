// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Ameko.DataModels;
using Ameko.Messages;
using AssCS;
using AssCS.Utilities;
using Holo.Models;
using Holo.Providers;
using Material.Icons;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace Ameko.ViewModels.Dialogs;

public class ScriptInfoDialogViewModel : ViewModelBase
{
    private readonly Random _random = new();

    public ObservableCollection<ObservableKeyValuePair<string, string>> Infos { get; }
    private readonly HashSet<string> _removed = [];

    public ICommand AddCommand { get; }
    public ICommand RemoveCommand { get; }
    public ReactiveCommand<RxVoid, EmptyMessage> SaveCommand { get; }

    public ScriptInfoDialogViewModel(IMessageBoxService messageBoxService, Document document)
    {
        var messageBoxService1 = messageBoxService;
        Infos = new ObservableCollection<ObservableKeyValuePair<string, string>>(
            document
                .ScriptInfoManager.GetAll(export: false)
                .Select(kvp => new ObservableKeyValuePair<string, string>(kvp.Key, kvp.Value))
        );

        RemoveCommand = ReactiveCommand.Create(
            (string key) =>
            {
                _removed.Add(key);
                Infos.RemoveAll(kvp => kvp.Key == key);
            }
        );

        AddCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var (button, input) =
                await messageBoxService1.ShowInputAsync(
                    I18N.ScriptInfo.ScriptInfo_MsgBox_Add_Title,
                    I18N.ScriptInfo.ScriptInfo_MsgBox_Add_Body,
                    string.Empty,
                    MsgBoxButtonSet.OkCancel,
                    MsgBoxButton.Ok,
                    MaterialIconKind.Rename
                ) ?? (MsgBoxButton.Cancel, string.Empty); // Fallback condition

            if (button != MsgBoxButton.Ok)
                return;

            if (Infos.Any(kvp => kvp.Key == input))
            {
                await messageBoxService1.ShowAsync(
                    I18N.ScriptInfo.ScriptInfo_MsgBox_Conflict_Title,
                    string.Format(I18N.ScriptInfo.ScriptInfo_MsgBox_Conflict_Body, input),
                    MsgBoxButtonSet.Ok,
                    MsgBoxButton.Ok,
                    MaterialIconKind.Error
                );
                return;
            }

            if (input == "!")
            {
                // Guard against duplicate `!` headers
                // This will be further handled by the manager
                input = $"![{_random.Next(100, 1000)}]";
            }

            Infos.Add(new ObservableKeyValuePair<string, string>(input, string.Empty));
            _removed.Remove(input); // In case the key was previously removed
        });

        SaveCommand = ReactiveCommand.Create(() =>
        {
            foreach (var info in _removed)
            {
                document.ScriptInfoManager.Remove(info);
            }

            foreach (var info in Infos)
            {
                document.ScriptInfoManager.Set(info.Key, info.Value ?? string.Empty);
            }

            return new EmptyMessage();
        });
    }
}
