// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using Ameko.Services;
using Avalonia.Platform;

namespace Ameko.ViewModels.Windows;

public class CrashReporterWindowViewModel(string content) : ViewModelBase
{
    public string Content { get; } = content;
}
