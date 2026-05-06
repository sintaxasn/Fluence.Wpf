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

namespace Fluence.Wpf.Native
{
    internal static class NativeConstants
    {
        // DWM Window Attributes
        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        public const int DWMWA_BORDER_COLOR = 34;
        public const int DWMWA_CAPTION_COLOR = 35;
        public const int DWMWA_TEXT_COLOR = 36;
        public const int DWMWA_VISIBLE_FRAME_BORDER_THICKNESS = 37;
        public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        public const int DWMWA_MICA_EFFECT = 1029;

        // DWM System Backdrop Types (DWMSBT_*)
        public const int DWMSBT_AUTO = 0;
        public const int DWMSBT_NONE = 1;
        public const int DWMSBT_MAINWINDOW = 2;       // Mica
        public const int DWMSBT_TRANSIENTWINDOW = 3;  // Acrylic
        public const int DWMSBT_TABBEDWINDOW = 4;     // Tabbed

        // DWM Window Corner Preferences (DWMWCP_*)
        public const int DWMWCP_DEFAULT = 0;
        public const int DWMWCP_DONOTROUND = 1;
        public const int DWMWCP_ROUND = 2;
        public const int DWMWCP_ROUNDSMALL = 3;

        // DWM Color Special Values
        public const int DWMWA_COLOR_NONE = unchecked((int)0xFFFFFFFE);
        public const int DWMWA_COLOR_DEFAULT = unchecked((int)0xFFFFFFFF);

        // Window Messages
        public const int WM_NCHITTEST = 0x0084;
        public const int WM_NCLBUTTONUP = 0x00A2;
        public const int WM_NCCALCSIZE = 0x0083;
        public const int WM_NCMOUSELEAVE = 0x02A2;
        public const int WM_GETMINMAXINFO = 0x0024;
        public const int WM_SETTINGCHANGE = 0x001A;
        public const int WM_SYSCOLORCHANGE = 0x0015;
        public const int WM_SYSCOMMAND = 0x0112;
        public const int WM_THEMECHANGED = 0x031A;
        public const int WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320;
        public const int WM_DWMCOMPOSITIONCHANGED = 0x031E;

        // System Commands
        public const int SC_MOVE = 0xF010;

        // Hit Test Results
        public const int HTCLIENT = 1;
        public const int HTCAPTION = 2;
        public const int HTMINBUTTON = 8;
        public const int HTMAXBUTTON = 9;
        public const int HTLEFT = 10;
        public const int HTRIGHT = 11;
        public const int HTTOP = 12;
        public const int HTTOPLEFT = 13;
        public const int HTTOPRIGHT = 14;
        public const int HTBOTTOM = 15;
        public const int HTBOTTOMLEFT = 16;
        public const int HTBOTTOMRIGHT = 17;
        public const int HTCLOSE = 20;

        // Monitor Defaults
        public const uint MONITOR_DEFAULTTONEAREST = 2;

        // DWM Boolean Values
        public const int DWM_TRUE = 1;
        public const int DWM_FALSE = 0;

        // Registry Paths
        public const string PersonalizeRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        public const string DwmRegistryPath = @"Software\Microsoft\Windows\DWM";
        public const string AccentRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent";

        // Registry Value Names
        public const string AppsUseLightTheme = "AppsUseLightTheme";
        public const string SystemUsesLightTheme = "SystemUsesLightTheme";
        public const string ColorPrevalence = "ColorPrevalence";
        public const string AccentPalette = "AccentPalette";
        public const string AccentColor = "AccentColor";
        public const string AccentColorInactive = "AccentColorInactive";
        public const string ColorizationColor = "ColorizationColor";
        public const string ColorizationColorBalance = "ColorizationColorBalance";
    }
}
