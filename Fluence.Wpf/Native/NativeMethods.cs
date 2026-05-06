/*
 * Copyright 2026 Dan Cunningham
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Runtime.InteropServices;

namespace Fluence.Wpf.Native
{
#pragma warning disable SYSLIB1054 // DllImport keeps the shared net472/net10 interop surface identical.
    internal static class NativeMethods
    {
        private const string Dwmapi = "dwmapi.dll";
        private const string User32 = "user32.dll";
        private const string UxTheme = "uxtheme.dll";
        private const string Ntdll = "ntdll.dll";

        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;

        #region User32 Window Style APIs

        [DllImport(User32, SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport(User32, SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsZoomed(IntPtr hWnd);

        // SW_* constants for ShowWindow. See Win32 docs for full list.
        private const int SW_RESTORE = 9;
        private const int SW_MINIMIZE = 6;
        private const int SW_MAXIMIZE = 3;

        #endregion

        #region NT APIs

        [DllImport(Ntdll, SetLastError = true)]
        private static extern int RtlGetVersion(ref OSVERSIONINFOEX versionInfo);

        #endregion

        #region DWM APIs

        [DllImport(Dwmapi, PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int attr,
            ref int attrValue,
            int attrSize);

        [DllImport(Dwmapi, PreserveSig = true)]
        public static extern int DwmGetWindowAttribute(
            IntPtr hwnd,
            int attr,
            out int attrValue,
            int attrSize);

        [DllImport(Dwmapi, PreserveSig = true)]
        public static extern int DwmExtendFrameIntoClientArea(
            IntPtr hwnd,
            ref MARGINS pMarInset);

        [DllImport(Dwmapi, PreserveSig = true)]
        public static extern int DwmGetColorizationColor(
            out uint pcrColorization,
            out bool pfOpaqueBlend);

        [DllImport(Dwmapi, PreserveSig = true)]
        public static extern int DwmIsCompositionEnabled(
            out bool pfEnabled);

        [DllImport(Dwmapi, EntryPoint = "#127", PreserveSig = false)]
        public static extern void DwmGetColorizationParameters(
            out DWMCOLORIZATIONPARAMS parameters);

        #endregion

        #region User32 APIs

        [DllImport(User32, SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport(User32, SetLastError = true)]
        public static extern bool GetClientRect(IntPtr hwnd, out RECT lpRect);

        [DllImport(User32)]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport(User32, CharSet = CharSet.Unicode)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport(User32, SetLastError = true)]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport(User32, SetLastError = true)]
        public static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        #endregion

        #region UxTheme APIs

        [DllImport(UxTheme, EntryPoint = "#95", CharSet = CharSet.Unicode)]
        public static extern uint GetImmersiveColorFromColorSetEx(
            uint dwImmersiveColorSet,
            uint dwImmersiveColorType,
            bool bIgnoreHighContrast,
            uint dwHighContrastCacheMode);

        [DllImport(UxTheme, EntryPoint = "#96", CharSet = CharSet.Unicode)]
        public static extern uint GetImmersiveColorTypeFromName(string name);

        [DllImport(UxTheme, EntryPoint = "#98", CharSet = CharSet.Unicode)]
        public static extern uint GetImmersiveUserColorSetPreference(
            bool bForceCheckRegistry,
            bool bSkipCheckOnFail);

        #endregion

        #region Helper Methods

        public static bool SetWindowAttribute(IntPtr hwnd, int attribute, int value)
        {
            int result = DwmSetWindowAttribute(hwnd, attribute, ref value, sizeof(int));
            return result == 0;
        }

        public static bool SetWindowCornerPreference(IntPtr hwnd, int cornerPreference)
        {
            return SetWindowAttribute(hwnd, NativeConstants.DWMWA_WINDOW_CORNER_PREFERENCE, cornerPreference);
        }

        public static bool SetImmersiveDarkMode(IntPtr hwnd, bool enabled)
        {
            int value = enabled ? NativeConstants.DWM_TRUE : NativeConstants.DWM_FALSE;
            return SetWindowAttribute(hwnd, NativeConstants.DWMWA_USE_IMMERSIVE_DARK_MODE, value);
        }

        public static bool SetSystemBackdropType(IntPtr hwnd, int backdropType)
        {
            return SetWindowAttribute(hwnd, NativeConstants.DWMWA_SYSTEMBACKDROP_TYPE, backdropType);
        }

        public static bool SetMicaEffect(IntPtr hwnd, bool enabled)
        {
            int value = enabled ? NativeConstants.DWM_TRUE : NativeConstants.DWM_FALSE;
            return SetWindowAttribute(hwnd, NativeConstants.DWMWA_MICA_EFFECT, value);
        }

        public static bool SetCaptionColor(IntPtr hwnd, int color)
        {
            return SetWindowAttribute(hwnd, NativeConstants.DWMWA_CAPTION_COLOR, color);
        }

        public static bool SetBorderColor(IntPtr hwnd, int color)
        {
            return SetWindowAttribute(hwnd, NativeConstants.DWMWA_BORDER_COLOR, color);
        }

        public static bool ExtendFrameIntoClientArea(IntPtr hwnd)
        {
            MARGINS margins = new() { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            int result = DwmExtendFrameIntoClientArea(hwnd, ref margins);
            return result == 0;
        }

        public static int ColorToAbgr(System.Windows.Media.Color color)
        {
            return (color.B << 16) | (color.G << 8) | color.R;
        }

        public static bool IsCompositionEnabled()
        {
            int result = DwmIsCompositionEnabled(out bool enabled);
            return result == 0 && enabled;
        }

        public static void HideAllWindowButtons(IntPtr hwnd)
        {
            int style = GetWindowLong(hwnd, GWL_STYLE);
            _ = SetWindowLong(hwnd, GWL_STYLE, style & ~WS_SYSMENU);
        }

        // Directly drives the native ShowWindow() API to minimize a window. Used as a
        // belt-and-braces fallback from FluenceWindow.OnMinimizeWindow so that the custom
        // caption's minimize button is guaranteed to work even when the chrome has stripped
        // WS_SYSMENU/WS_MINIMIZEBOX (blocking SC_MINIMIZE via DefWindowProc), ResizeMode is
        // NoResize, the window is Topmost, or the window is shown via ShowDialog() inside a
        // nested dispatcher frame. The Win32 ShowWindow call honors SW_MINIMIZE regardless of
        // window styles, so it cannot be silently gated the way WM_SYSCOMMAND can.
        public static bool MinimizeWindowNative(IntPtr hwnd)
        {
            return hwnd != IntPtr.Zero && (IsIconic(hwnd) || ShowWindow(hwnd, SW_MINIMIZE));
        }

        public static bool MaximizeWindowNative(IntPtr hwnd)
        {
            return hwnd != IntPtr.Zero && (IsZoomed(hwnd) || ShowWindow(hwnd, SW_MAXIMIZE));
        }

        public static bool RestoreWindowNative(IntPtr hwnd)
        {
            return hwnd != IntPtr.Zero && ShowWindow(hwnd, SW_RESTORE);
        }

        public static bool RoundWindowCorner(IntPtr hwnd)
        {
            return SetWindowAttribute(hwnd, NativeConstants.DWMWA_WINDOW_CORNER_PREFERENCE, NativeConstants.DWMWCP_ROUND);
        }

        public static Version GetRealOsVersion()
        {
            OSVERSIONINFOEX versionInfo = new()
            {
                OSVersionInfoSize = Marshal.SizeOf<OSVERSIONINFOEX>(),
                CSDVersion = string.Empty
            };

            int result = RtlGetVersion(ref versionInfo);
            return result != 0
                ? throw new InvalidOperationException("RtlGetVersion failed.")
                : new Version(
                versionInfo.MajorVersion,
                versionInfo.MinorVersion,
                versionInfo.BuildNumber,
                versionInfo.Revision);
        }

        #endregion
    }
#pragma warning restore SYSLIB1054
}
