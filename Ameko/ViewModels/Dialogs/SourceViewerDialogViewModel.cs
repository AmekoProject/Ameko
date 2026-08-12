// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Windows.Input;
using AvaloniaEdit.Document;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace Ameko.ViewModels.Dialogs;

public class SourceViewerDialogViewModel : ViewModelBase
{
    public bool IsLocalScript { get; }
    public TextDocument Document { get; }
    public ICommand SaveCommand { get; }

    public SourceViewerDialogViewModel(
        ILogger<SourceViewerDialogViewModel> logger,
        Uri scriptUri,
        string scriptContent
    )
    {
        IsLocalScript = scriptUri.IsFile;
        Document = new TextDocument(scriptContent);

        SaveCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            // Just in case
            if (!IsLocalScript)
                return;

            try
            {
                await using var fs = new FileStream(
                    scriptUri.LocalPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None
                );
                await using var writer = new StreamWriter(fs);
                await writer.WriteAsync(Document.Text);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to write to {ScriptPath}", scriptUri.LocalPath);
            }
        });
    }
}
