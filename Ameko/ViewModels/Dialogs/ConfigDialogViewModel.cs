// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using Ameko.DataModels;
using Holo.Configuration;
using Holo.Models;
using Holo.Providers;

namespace Ameko.ViewModels.Dialogs;

public class ConfigDialogViewModel(
    IConfiguration config,
    IDictionaryService dictionaryService,
    ICultureService cultureService
) : ViewModelBase
{
    internal static List<Tuple<RichPresenceLevel, string>> RichPresenceLevelChoices =>
        [
            new(RichPresenceLevel.Disabled, I18N.Config.Config_RpcLevel_Disabled),
            new(RichPresenceLevel.Enabled, I18N.Config.Config_RpcLevel_Enabled),
            new(RichPresenceLevel.TimeOnly, I18N.Config.Config_RpcLevel_TimeOnly),
        ];

    internal static List<Tuple<PropagateFields, string>> PropagateFieldsChoices =>
        [
            new(PropagateFields.All, I18N.Config.Config_PropFields_All),
            new(PropagateFields.None, I18N.Config.Config_PropFields_None),
            new(PropagateFields.NonText, I18N.Config.Config_PropFields_NonText),
        ];

    internal static List<Tuple<Theme, string>> ThemeChoices =>
        [
            new(Theme.Default, I18N.Config.Config_Theme_Default),
            new(Theme.Light, I18N.Config.Config_Theme_Light),
            new(Theme.Dark, I18N.Config.Config_Theme_Dark),
        ];

    internal static List<Tuple<SaveFrames, string>> SaveFramesChoices =>
        [
            new(SaveFrames.WithVideo, I18N.Config.Config_SaveFrames_WithVideo),
            new(SaveFrames.WithSubtitles, I18N.Config.Config_SaveFrames_WithSubtitles),
        ];

    public IReadOnlyList<SpellcheckLanguage> AvailableLanguages { get; } =
        SpellcheckLanguage.AvailableLanguages;

    public IConfiguration Config { get; } = config;
    public ICultureService CultureService { get; } = cultureService;
    public IDictionaryService DictionaryService { get; } = dictionaryService;
}
