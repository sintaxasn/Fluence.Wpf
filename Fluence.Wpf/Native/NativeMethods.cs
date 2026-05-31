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

using Fluence.Wpf.Helpers;
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
        private const string Shell32 = "shell32.dll";

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

        [DllImport(User32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            string lParam,
            uint fuFlags,
            uint uTimeout,
            out IntPtr lpdwResult);

        public const int HWND_BROADCAST = 0xFFFF;
        public const uint SMTO_ABORTIFHUNG = 0x0002;

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

        #region Shell32 APIs

        [DllImport(Shell32, SetLastError = true)]
        public static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        #endregion

        #region UxTheme APIs

        [DllImport(UxTheme, EntryPoint = "#94", CharSet = CharSet.Unicode)]
        public static extern uint GetImmersiveColorSetCount();

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

        [DllImport(UxTheme, ExactSpelling = true, PreserveSig = true)]
        public static extern int SetWindowThemeAttribute(
            IntPtr hwnd,
            int eAttribute,
            ref WTA_OPTIONS pvAttribute,
            uint cbAttribute);

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

        /// <summary>
        /// Selects the DWM immersive dark-mode window attribute id for a given OS build. The
        /// attribute moved from <see cref="NativeConstants.DWMWA_USE_IMMERSIVE_DARK_MODE_OLD"/>
        /// (19) to <see cref="NativeConstants.DWMWA_USE_IMMERSIVE_DARK_MODE"/> (20) starting at
        /// Windows 10 build 18362 (version 1903). Builds 17763..18361 (1809 era) must use 19, or
        /// the dark caption silently fails to apply. This selector is pure so it can be unit
        /// tested without a window handle.
        /// </summary>
        /// <param name="osBuild">The OS build number (for example <c>18362</c>).</param>
        /// <returns>The DWM attribute id to pass to <see cref="DwmSetWindowAttribute"/>.</returns>
        public static int GetImmersiveDarkModeAttribute(int osBuild)
        {
            return osBuild >= 18362 ? NativeConstants.DWMWA_USE_IMMERSIVE_DARK_MODE : NativeConstants.DWMWA_USE_IMMERSIVE_DARK_MODE_OLD;
        }

        public static bool SetImmersiveDarkMode(IntPtr hwnd, bool enabled)
        {
            int value = enabled ? NativeConstants.DWM_TRUE : NativeConstants.DWM_FALSE;
            return SetWindowAttribute(hwnd, GetImmersiveDarkModeAttribute(OsVersionHelper.OsBuild), value);
        }

        public static bool SetSystemBackdropType(IntPtr hwnd, int backdropType)
        {
            return SetWindowAttribute(hwnd, NativeConstants.DWMWA_SYSTEMBACKDROP_TYPE, backdropType);
        }

        /// <summary>
        /// Cloaks or uncloaks a window via <see cref="NativeConstants.DWMWA_CLOAK"/>. While cloaked,
        /// DWM keeps the window fully composed off-screen and does not present it, so a window can be
        /// shown, have its backdrop applied, and render its first frame without the empty client area
        /// flashing black. Callers MUST guarantee a matching uncloak; a window left cloaked is invisible.
        /// </summary>
        public static bool SetWindowCloak(IntPtr hwnd, bool cloak)
        {
            int value = cloak ? NativeConstants.DWM_TRUE : NativeConstants.DWM_FALSE;
            return SetWindowAttribute(hwnd, NativeConstants.DWMWA_CLOAK, value);
        }

        /// <summary>
        /// Reads the read-only <see cref="NativeConstants.DWMWA_CLOAKED"/> attribute, returning the
        /// reason flags for why the window is cloaked. Zero means the window is not cloaked. Returns
        /// zero on any failure (for example when DWM composition is disabled).
        /// </summary>
        public static int GetWindowCloakedState(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                return 0;
            }
            int result = DwmGetWindowAttribute(hwnd, NativeConstants.DWMWA_CLOAKED, out int cloaked, sizeof(int));
            return result == 0 ? cloaked : 0;
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

        /// <summary>
        /// Suppresses Win32 default non-client caption drawing so the DWM backdrop shows
        /// through cleanly. Best-effort: classic themes return <c>S_FALSE</c> which is treated
        /// as a no-op success.
        /// </summary>
        public static bool SuppressNonClientCaptionDraw(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                return false;
            }
            WTA_OPTIONS opts = new()
            {
                Flags = NativeConstants.WTNCA_NODRAWCAPTION,
                Mask = NativeConstants.WTNCA_NODRAWCAPTION,
            };
            int hr = SetWindowThemeAttribute(hwnd, NativeConstants.WTA_NONCLIENT, ref opts, (uint)Marshal.SizeOf<WTA_OPTIONS>());
            return hr >= 0; // S_OK or S_FALSE
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

        // Packs a Color into the 0x00BBGGRR COLORREF layout that DWM window attributes such as
        // DWMWA_BORDER_COLOR expect (alpha is ignored). Despite historically being named for ABGR,
        // the byte order produced here is COLORREF; callers must not reuse it for an attribute that
        // genuinely expects ABGR with a meaningful alpha channel.
        public static int ColorToColorRef(System.Windows.Media.Color color)
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

        /// <summary>
        /// Returns <see langword="true"/> when the Windows taskbar is currently in auto-hide
        /// mode. Queries the shell with <see cref="NativeConstants.ABM_GETSTATE"/> and tests the
        /// <see cref="NativeConstants.ABS_AUTOHIDE"/> bit of the returned state.
        /// </summary>
        public static bool IsTaskbarAutoHide()
        {
            APPBARDATA data = new() { cbSize = Marshal.SizeOf<APPBARDATA>() };
            IntPtr state = SHAppBarMessage(NativeConstants.ABM_GETSTATE, ref data);
            return (state.ToInt64() & NativeConstants.ABS_AUTOHIDE) != 0;
        }

        /// <summary>
        /// Returns the screen edge (one of the <c>ABE_*</c> values) on which the auto-hide
        /// taskbar is docked, or <see langword="null"/> when the taskbar is not auto-hide or the
        /// query is unavailable.
        /// </summary>
        /// <param name="monitor">
        /// The monitor a caller intends to match the taskbar against. <see cref="SHAppBarMessage"/>
        /// with <see cref="NativeConstants.ABM_GETTASKBARPOS"/> reports only the primary taskbar,
        /// so this implementation returns the primary taskbar edge and ignores the monitor on
        /// multi-monitor setups. The parameter is retained so a future caller can match per
        /// monitor without an API break.
        /// </param>
        public static uint? GetAutoHideTaskbarEdge(IntPtr monitor)
        {
            _ = monitor;
            if (!IsTaskbarAutoHide())
            {
                return null;
            }
            APPBARDATA data = new() { cbSize = Marshal.SizeOf<APPBARDATA>() };
            IntPtr result = SHAppBarMessage(NativeConstants.ABM_GETTASKBARPOS, ref data);
            return result == IntPtr.Zero ? null : data.uEdge;
        }

        /// <summary>
        /// Shifts a maximized window rect inward by 2 px on the auto-hide taskbar edge so the
        /// maximized window does not fully cover the taskbar, which would block its hover-reveal.
        /// Pure and handle-free for unit testing. Mirrors the per-edge direction and sign used by
        /// the iNKORE MaximizedWindowFixer reference: BOTTOM shrinks height, TOP moves down and
        /// shrinks height, RIGHT shrinks width, LEFT moves right and shrinks width. Unrecognized
        /// edge values leave the rect unchanged.
        /// </summary>
        /// <param name="mmi">The min/max info whose maximized rect is adjusted in place.</param>
        /// <param name="edge">The auto-hide taskbar edge (one of the <c>ABE_*</c> values).</param>
        public static void ApplyAutoHideTaskbarShift(ref MINMAXINFO mmi, uint edge)
        {
            switch (edge)
            {
                case NativeConstants.ABE_LEFT:
                    mmi.ptMaxPosition.X += 2;
                    mmi.ptMaxSize.X -= 2;
                    break;
                case NativeConstants.ABE_TOP:
                    mmi.ptMaxPosition.Y += 2;
                    mmi.ptMaxSize.Y -= 2;
                    break;
                case NativeConstants.ABE_RIGHT:
                    mmi.ptMaxSize.X -= 2;
                    break;
                case NativeConstants.ABE_BOTTOM:
                    mmi.ptMaxSize.Y -= 2;
                    break;
                default:
                    break;
            }
        }

        #endregion
    }
#pragma warning restore SYSLIB1054
}
