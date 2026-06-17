// SPDX-License-Identifier: GPL-3.0-only

using Avalonia.Controls;
using Holo.Configuration;

namespace Ameko.Utilities;

public static class WindowStateExtensions
{
    public static WindowState ToAvaloniaWindowState(this PersistentWindowState persistent)
    {
        return persistent switch
        {
            PersistentWindowState.Normal => WindowState.Normal,
            PersistentWindowState.Maximized => WindowState.Maximized,
            PersistentWindowState.Minimized => WindowState.Minimized,
            PersistentWindowState.FullScreen => WindowState.FullScreen,
            _ => WindowState.Normal,
        };
    }
}

public static class PersistentWindowStateExtensions
{
    public static PersistentWindowState ToPersistentWindowState(this WindowState avalonia)
    {
        return avalonia switch
        {
            WindowState.Normal => PersistentWindowState.Normal,
            WindowState.Maximized => PersistentWindowState.Maximized,
            WindowState.Minimized => PersistentWindowState.Minimized,
            WindowState.FullScreen => PersistentWindowState.FullScreen,
            _ => PersistentWindowState.Normal,
        };
    }
}
