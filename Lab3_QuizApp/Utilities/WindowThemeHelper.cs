using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows;

namespace QuizAppExtended.Utilities
{
    internal static class WindowThemeHelper
    {
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public static void TryEnableImmersiveDarkMode(Window window)
        {
            if (window == null)
            {
                return;
            }

            window.SourceInitialized += (_, _) =>
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero)
                {
                    return;
                }

                try
                {
                    int enabled = 1;
                    _ = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref enabled, Marshal.SizeOf<int>());
                }
                catch
                {

                }
            };
        }
    }
}
