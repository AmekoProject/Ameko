// SPDX-License-Identifier: GPL-3.0-only

using System.Windows.Input;
using Ameko.Messages;
using Ameko.Utilities;
using AssCS;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;
using Color = AssCS.Color;

namespace Ameko.ViewModels.Dialogs;

public class StyleEditorDialogViewModel : ViewModelBase
{
    private readonly Style _backupStyle;
    private readonly StyleManager _styleManager;
    private string _styleName;

    public ReactiveCommand<RxVoid, StyleEditorDialogClosedMessage> SaveCommand { get; }
    public Interaction<ColorDialogViewModel, Color?> ShowColorDialog { get; }
    public ICommand EditColorCommand { get; }

    public Style Style { get; }
    public Document? Document { get; }

    public Color PrimaryColor => Style.PrimaryColor;
    public Color SecondaryColor => Style.SecondaryColor;
    public Color OutlineColor => Style.OutlineColor;
    public Color ShadowColor => Style.ShadowColor;

    /// <summary>
    /// Editable style name
    /// </summary>
    public string StyleName
    {
        get => _styleName;
        set => this.RaiseAndSetIfChanged(ref _styleName, value);
    }

    /// <summary>
    /// Determines if the <see cref="StyleName"/> is invalid
    /// </summary>
    public bool IsNameInvalid =>
        string.IsNullOrWhiteSpace(_styleName)
        || (
            _styleName != _backupStyle.Name
            && (string.IsNullOrWhiteSpace(_styleName) || _styleManager.TryGet(_styleName, out _))
        );

    public bool IsNewStyle => string.IsNullOrEmpty(_backupStyle.Name);

    /// <summary>
    /// Commit a style name change
    /// </summary>
    /// <remarks>
    /// If the style is from a <see cref="AssCS.Document"/>,
    /// <see cref="EventManager.ChangeStyle(string, string)"/> will be called.
    /// </remarks>
    /// <returns><see langword="true"/> if we are good to go</returns>
    public bool CommitNameChange()
    {
        if (IsNameInvalid)
        {
            this.RaisePropertyChanged(nameof(IsNameInvalid));
            return false;
        }
        if (StyleName == _backupStyle.Name)
            return true;

        Style.Name = StyleName;
        Document?.EventManager.ChangeStyle(_backupStyle.Name, StyleName);

        return true;
    }

    /// <summary>
    /// Initialize a style editor
    /// </summary>
    /// <param name="vmFactory">ViewModel factory</param>
    /// <param name="style">Style being edited</param>
    /// <param name="manager">Manager the <paramref name="style"/> belongs to</param>
    public StyleEditorDialogViewModel(
        IViewModelFactory vmFactory,
        Style style,
        StyleManager manager
    )
        // ReSharper disable once IntroduceOptionalParameters.Global
        : this(vmFactory, style, manager, null) { }

    /// <summary>
    /// Initialize a style editor
    /// </summary>
    /// <param name="vmFactory">ViewModel factory</param>
    /// <param name="style">Style being edited</param>
    /// <param name="manager">Manager the <paramref name="style"/> belongs to</param>
    /// <param name="document">Document the manager belongs to, if applicable</param>
    public StyleEditorDialogViewModel(
        IViewModelFactory vmFactory,
        Style style,
        StyleManager manager,
        Document? document
    )
    {
        Style = style;
        _styleName = Style.Name;
        _backupStyle = Style.Clone();
        _styleManager = manager;
        Document = document;

        ShowColorDialog = new Interaction<ColorDialogViewModel, Color?>();
        SaveCommand = ReactiveCommand.Create(() =>
            new StyleEditorDialogClosedMessage(style != _backupStyle ? style : null) // Modified style or null
        );

        EditColorCommand = ReactiveCommand.CreateFromTask(
            async (Color color) =>
            {
                var vm = vmFactory.Create<ColorDialogViewModel>(color);
                _ = await ShowColorDialog.Handle(vm);

                // Get the buttons to update
                this.RaisePropertyChanged(nameof(PrimaryColor));
                this.RaisePropertyChanged(nameof(SecondaryColor));
                this.RaisePropertyChanged(nameof(OutlineColor));
                this.RaisePropertyChanged(nameof(ShadowColor));
            }
        );
    }
}
