using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WindowsManager.App.Services;

/// <summary>
/// Applies the dark/light title bar (window chrome) via the undocumented but stable
/// DWM "immersive dark mode" attribute, since WPF windows don't natively theme their
/// native caption bar - only the client area content is themeable through XAML resources.
/// </summary>
public static class WindowChromeService
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    // 20 on Windows 11 / recent Windows 10 builds; 19 on older Windows 10 builds that shipped an earlier draft of the API.
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

    public static void Apply(Window window, AppTheme theme)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var useDarkMode = theme == AppTheme.Dark ? 1 : 0;

        if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int)) != 0)
        {
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref useDarkMode, sizeof(int));
        }
    }
}
