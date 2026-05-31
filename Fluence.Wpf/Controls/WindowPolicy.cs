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
using Fluence.Wpf.Native;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shell;

namespace Fluence.Wpf.Controls
{
    internal static class WindowPolicy
    {
        internal static WindowChrome CreateWindowChrome()
        {
            return new WindowChrome
            {
                CaptionHeight = 0,
                CornerRadius = new CornerRadius(0),
                GlassFrameThickness = new Thickness(-1),
                ResizeBorderThickness = new Thickness(4),
                UseAeroCaptionButtons = false,
                NonClientFrameEdges = NonClientFrameEdges.None
            };
        }

        /// <summary>
        /// Returns the glass-frame thickness appropriate for the given backdrop and shadow state.
        /// When a DWM backdrop (Mica/Acrylic/Tabbed/Auto) is active the thickness is <c>-1</c>
        /// so DWM extends the glass into the client area and the backdrop shows through. When
        /// no backdrop is active and the user wants a shadow, the thickness is still <c>-1</c>
        /// so WPF's <see cref="System.Windows.Shell.WindowChrome"/> draws the resize border with
        /// a glass-frame fallback. When neither is active we use a very-thin-but-nonzero value
        /// (<c>0.00001</c>) so the resize border continues to hit-test while WindowChrome's
        /// renderer does not paint a visible glass-frame artifact - matching the pattern in
        /// <c>wpfui-main\src\Wpf.Ui\Controls\FluentWindow\FluentWindow.cs</c>.
        /// </summary>
        internal static Thickness GetGlassFrameThickness(BackdropType backdrop, bool hasShadow)
        {
            return backdrop != BackdropType.None || hasShadow
                ? new Thickness(-1)
                : new Thickness(0.00001);
        }

        internal static Thickness GetResizeBorderThickness(WindowState windowState, ResizeMode resizeMode)
        {
            return windowState == WindowState.Maximized || resizeMode == ResizeMode.NoResize || resizeMode == ResizeMode.CanMinimize
                ? new Thickness(0)
                : new Thickness(4);
        }

        internal static FramePlan BuildFramePlan(
            WindowState windowState,
            bool isActive,
            bool isAccentBorderEnabled,
            WindowCapabilities capabilities,
            Color accentColor)
        {
            Thickness templateBorderThickness = windowState == WindowState.Maximized
                ? new Thickness(0)
                : new Thickness(2);
            string templateBorderBrushResourceKey = !isActive || !isAccentBorderEnabled
                ? "CardStrokeColorDefaultSolidBrush"
                : "SystemAccentColorBrush";

            int dwmBorderColor = NativeConstants.DWMWA_COLOR_DEFAULT;
            if (capabilities.SupportsBorderColor && isActive && isAccentBorderEnabled)
            {
                dwmBorderColor = NativeMethods.ColorToColorRef(accentColor);
            }
            return new FramePlan(templateBorderThickness, templateBorderBrushResourceKey, dwmBorderColor);
        }

        internal static BackdropType ResolveEffectiveBackdrop(BackdropType requestedBackdrop, WindowCapabilities capabilities)
        {
            return requestedBackdrop switch
            {
                BackdropType.Auto or BackdropType.Mica => capabilities.SupportsSystemBackdropType || capabilities.SupportsMicaEffect
                    ? BackdropType.Mica
                    : BackdropType.None,
                BackdropType.Acrylic or BackdropType.Tabbed => !capabilities.SupportsSystemBackdropType
                    ? capabilities.SupportsMicaEffect ? BackdropType.Mica : BackdropType.None
                    : requestedBackdrop,
                BackdropType.None or _ => requestedBackdrop
            };
        }

        internal static BackdropPlan BuildBackdropPlan(
            BackdropType requestedBackdrop,
            ApplicationTheme resolvedTheme,
            WindowCapabilities capabilities,
            Color fallbackBackgroundColor)
        {
            BackdropType effectiveBackdrop = ResolveEffectiveBackdrop(requestedBackdrop, capabilities);
            bool isDark = resolvedTheme == ApplicationTheme.Dark;
            return effectiveBackdrop == BackdropType.None
                ? new BackdropPlan(effectiveBackdrop, false, fallbackBackgroundColor, NativeConstants.DWMWA_COLOR_DEFAULT, capabilities.SupportsSystemBackdropType ? NativeConstants.DWMSBT_NONE : null, false, resolvedTheme == ApplicationTheme.Dark)
                : effectiveBackdrop != BackdropType.Mica || capabilities.SupportsSystemBackdropType || !capabilities.SupportsMicaEffect
                ? new BackdropPlan(effectiveBackdrop, true, Colors.Transparent, NativeConstants.DWMWA_COLOR_NONE, MapSystemBackdropType(effectiveBackdrop), false, isDark)
                : new BackdropPlan(effectiveBackdrop, true, Colors.Transparent, NativeConstants.DWMWA_COLOR_NONE, null, true, isDark);
        }

        internal static int GetCornerPreference(CornerPreference preference)
        {
            return preference switch
            {
                CornerPreference.DoNotRound => NativeConstants.DWMWCP_DONOTROUND,
                CornerPreference.RoundSmall => NativeConstants.DWMWCP_ROUNDSMALL,
                CornerPreference.Default or CornerPreference.Round => NativeConstants.DWMWCP_ROUND,
                _ => NativeConstants.DWMWCP_ROUND,
            };
        }

        private static int MapSystemBackdropType(BackdropType backdropType)
        {
            return backdropType switch
            {
                BackdropType.Acrylic => NativeConstants.DWMSBT_TRANSIENTWINDOW,
                BackdropType.Tabbed => NativeConstants.DWMSBT_TABBEDWINDOW,
                BackdropType.Mica or BackdropType.Auto or BackdropType.None or _ => NativeConstants.DWMSBT_MAINWINDOW,
            };
        }
    }
}
