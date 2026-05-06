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
using System.Windows;
using System.Windows.Media;
using System.Windows.Shell;
using Fluence.Wpf.Helpers;
using Fluence.Wpf.Native;

namespace Fluence.Wpf.Controls
{
    internal sealed class WindowCapabilities(
        bool supportsSystemBackdropType,
        bool supportsMicaEffect,
        bool supportsRoundedCorners,
        bool supportsCaptionColor,
        bool supportsBorderColor = false)
    {
        public bool SupportsSystemBackdropType { get; private set; } = supportsSystemBackdropType;

        public bool SupportsMicaEffect { get; private set; } = supportsMicaEffect;

        public bool SupportsRoundedCorners { get; private set; } = supportsRoundedCorners;

        public bool SupportsCaptionColor { get; private set; } = supportsCaptionColor;

        public bool SupportsBorderColor { get; private set; } = supportsBorderColor;

        public static WindowCapabilities Current => new(
                    OsVersionHelper.SupportsSystemBackdropType,
                    OsVersionHelper.SupportsMicaEffect,
                    OsVersionHelper.SupportsRoundedCorners,
                    OsVersionHelper.SupportsCaptionColor,
                    OsVersionHelper.SupportsBorderColor);
    }

    internal sealed class BackdropPlan(
        BackdropType effectiveBackdrop,
        bool useTransparentBackground,
        Color backgroundColor,
        int captionColor,
        int? systemBackdropType,
        bool useLegacyMicaEffect,
        bool useImmersiveDarkMode)
    {
        public BackdropType EffectiveBackdrop { get; private set; } = effectiveBackdrop;

        public bool UseTransparentBackground { get; private set; } = useTransparentBackground;

        public Color BackgroundColor { get; private set; } = backgroundColor;

        public int CaptionColor { get; private set; } = captionColor;

        public int? SystemBackdropType { get; private set; } = systemBackdropType;

        public bool UseLegacyMicaEffect { get; private set; } = useLegacyMicaEffect;

        public bool UseImmersiveDarkMode { get; private set; } = useImmersiveDarkMode;
    }

    internal sealed class FramePlan(Thickness templateBorderThickness, string templateBorderBrushResourceKey, int dwmBorderColor)
    {
        public Thickness TemplateBorderThickness { get; private set; } = templateBorderThickness;

        public string TemplateBorderBrushResourceKey { get; private set; } = templateBorderBrushResourceKey;

        public int DwmBorderColor { get; private set; } = dwmBorderColor;
    }

    internal static class WindowPolicy
    {
        public static WindowChrome CreateWindowChrome(double captionHeight)
        {
            return new WindowChrome
            {
                CaptionHeight = captionHeight,
                CornerRadius = new CornerRadius(0),
                GlassFrameThickness = new Thickness(-1),
                ResizeBorderThickness = new Thickness(4),
                UseAeroCaptionButtons = false,
                NonClientFrameEdges = NonClientFrameEdges.None
            };
        }

        public static Thickness GetResizeBorderThickness(WindowState windowState, ResizeMode resizeMode)
        {
            return windowState == WindowState.Maximized || resizeMode == ResizeMode.NoResize || resizeMode == ResizeMode.CanMinimize
                ? new Thickness(0)
                : new Thickness(4);
        }

        public static FramePlan BuildFramePlan(
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
                dwmBorderColor = NativeMethods.ColorToAbgr(accentColor);
            }

            return new FramePlan(templateBorderThickness, templateBorderBrushResourceKey, dwmBorderColor);
        }

        public static BackdropType ResolveEffectiveBackdrop(BackdropType requestedBackdrop, WindowCapabilities capabilities)
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

        public static BackdropPlan BuildBackdropPlan(
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

        public static int GetCornerPreference(CornerPreference preference)
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
                _ => NativeConstants.DWMSBT_MAINWINDOW,
            };
        }
    }
}
