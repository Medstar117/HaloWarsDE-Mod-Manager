using AutoUpdater.Core.Serialization;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AutoUpdater.Core
{
    internal static class Utils
    {
        public static Uri? GetPatchFileUri(Release? release)
        {
            // Null check
            if (release == null)
                return null;

            // Find the asset containing the desired ReleasePackageName
            foreach (ReleaseAsset asset in release.assets)
                if (asset.name == Constants.ReleasePackageName)
                    return new Uri(asset.browser_download_url);

            // Package not found
            return null;
        }

        public static void HideCloseButton(Window w)
        {
            IntPtr hWnd = new WindowInteropHelper(w).Handle;
            _ = SetWindowLong(hWnd, GWL_STYLE, GetWindowLong(hWnd, GWL_STYLE) & ~WS_SYSMENU);
        }

        public static void ShowCloseButton(Window w)
        {
            IntPtr hWnd = new WindowInteropHelper(w).Handle;
            _ = SetWindowLong(hWnd, GWL_STYLE, GetWindowLong(hWnd, GWL_STYLE) | WS_SYSMENU);
        }

        #region Win32 Native Methods And Constants

        const int GWL_STYLE = -16;
        const int WS_SYSMENU = 0x80000;

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        #endregion
    }
}
