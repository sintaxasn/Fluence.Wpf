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
    internal sealed class WindowCapabilities
    {
        public WindowCapabilities(
            bool supportsSystemBackdropType,
            bool supportsMicaEffect,
            bool supportsRoundedCorners,
            bool supportsCaptionColor,
            bool supportsBorderColor = false)
        {
            SupportsSystemBackdropType = supportsSystemBackdropType;
            SupportsMicaEffect = supportsMicaEffect;
            SupportsRoundedCorners = supportsRoundedCorners;
            SupportsCaptionColor = supportsCaptionColor;
            SupportsBorderColor = supportsBorderColor;
        }

        public bool SupportsSystemBackdropType { get; private set; }

        public bool SupportsMicaEffect { get; private set; }

        public bool SupportsRoundedCorners { get; private set; }

        public bool SupportsCaptionColor { get; private set; }

        public bool SupportsBorderColor { get; private set; }

        public static WindowCapabilities Current => new WindowCapabilities(
                    OsVersionHelper.SupportsSystemBackdropType,
                    OsVersionHelper.SupportsMicaEffect,
                    OsVersionHelper.SupportsRoundedCorners,
                    OsVersionHelper.SupportsCaptionColor,
                    OsVersionHelper.SupportsBorderColor);
    }

    internal sealed class BackdropPlan
    {
        public BackdropPlan(
            BackdropType effectiveBackdrop,
            bool useTransparentBackground,
            Color backgroundColor,
            int captionColor,
            int? systemBackdropType,
            bool useLegacyMicaEffect,
            bool useImmersiveDarkMode)
        {
            EffectiveBackdrop = effectiveBackdrop;
            UseTransparentBackground = useTransparentBackground;
            BackgroundColor = backgroundColor;
            CaptionColor = captionColor;
            SystemBackdropType = systemBackdropType;
            UseLegacyMicaEffect = useLegacyMicaEffect;
            UseImmersiveDarkMode = useImmersiveDarkMode;
        }

        public BackdropType EffectiveBackdrop { get; private set; }

        public bool UseTransparentBackground { get; private set; }

        public Color BackgroundColor { get; private set; }

        public int CaptionColor { get; private set; }

        public int? SystemBackdropType { get; private set; }

        public bool UseLegacyMicaEffect { get; private set; }

        public bool UseImmersiveDarkMode { get; private set; }
    }

    internal sealed class FramePlan
    {
        public FramePlan(Thickness templateBorderThickness, string templateBorderBrushResourceKey, int dwmBorderColor)
        {
            TemplateBorderThickness = templateBorderThickness;
            TemplateBorderBrushResourceKey = templateBorderBrushResourceKey;
            DwmBorderColor = dwmBorderColor;
        }

        public Thickness TemplateBorderThickness { get; private set; }

        public string TemplateBorderBrushResourceKey { get; private set; }

        public int DwmBorderColor { get; private set; }
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
            if (windowState == WindowState.Maximized ||
                resizeMode == ResizeMode.NoResize ||
                resizeMode == ResizeMode.CanMinimize)
            {
                return new Thickness(0);
            }

            return new Thickness(4);
        }

        public static FramePlan BuildFramePlan(
            WindowState windowState,
            bool isActive,
            bool isAccentBorderEnabled,
            WindowCapabilities capabilities,
            Color accentColor)
        {
            var templateBorderThickness = windowState == WindowState.Maximized
                ? new Thickness(0)
                : new Thickness(2);
            string templateBorderBrushResourceKey;
            if (isActive && isAccentBorderEnabled)
            {
                templateBorderBrushResourceKey = "SystemAccentColorBrush";
            }
            else
            {
                templateBorderBrushResourceKey = "CardStrokeColorDefaultSolidBrush";
            }
            var dwmBorderColor = NativeConstants.DWMWA_COLOR_DEFAULT;

            if (capabilities.SupportsBorderColor && isActive && isAccentBorderEnabled)
            {
                dwmBorderColor = NativeMethods.ColorToAbgr(accentColor);
            }

            return new FramePlan(templateBorderThickness, templateBorderBrushResourceKey, dwmBorderColor);
        }

        public static BackdropType ResolveEffectiveBackdrop(BackdropType requestedBackdrop, WindowCapabilities capabilities)
        {
            if (requestedBackdrop == BackdropType.Auto)
            {
                if (capabilities.SupportsSystemBackdropType || capabilities.SupportsMicaEffect)
                {
                    return BackdropType.Mica;
                }

                return BackdropType.None;
            }

            if (requestedBackdrop == BackdropType.None)
            {
                return BackdropType.None;
            }

            if (requestedBackdrop == BackdropType.Mica)
            {
                return capabilities.SupportsSystemBackdropType || capabilities.SupportsMicaEffect
                    ? BackdropType.Mica
                    : BackdropType.None;
            }

            if (requestedBackdrop == BackdropType.Acrylic || requestedBackdrop == BackdropType.Tabbed)
            {
                if (capabilities.SupportsSystemBackdropType)
                {
                    return requestedBackdrop;
                }

                if (capabilities.SupportsMicaEffect)
                {
                    return BackdropType.Mica;
                }

                return BackdropType.None;
            }

            return requestedBackdrop;
        }

        public static BackdropPlan BuildBackdropPlan(
            BackdropType requestedBackdrop,
            ApplicationTheme resolvedTheme,
            WindowCapabilities capabilities,
            Color fallbackBackgroundColor)
        {
            var effectiveBackdrop = ResolveEffectiveBackdrop(requestedBackdrop, capabilities);
            var isDark = resolvedTheme == ApplicationTheme.Dark;

            if (effectiveBackdrop == BackdropType.None)
            {
                return new BackdropPlan(
                    effectiveBackdrop,
                    false,
                    fallbackBackgroundColor,
                    NativeConstants.DWMWA_COLOR_DEFAULT,
                    capabilities.SupportsSystemBackdropType ? (int?)NativeConstants.DWMSBT_NONE : null,
                    false,
                    isDark);
            }

            if (effectiveBackdrop == BackdropType.Mica &&
                !capabilities.SupportsSystemBackdropType &&
                capabilities.SupportsMicaEffect)
            {
                return new BackdropPlan(
                    effectiveBackdrop,
                    true,
                    Colors.Transparent,
                    NativeConstants.DWMWA_COLOR_NONE,
                    null,
                    true,
                    isDark);
            }

            return new BackdropPlan(
                effectiveBackdrop,
                true,
                Colors.Transparent,
                NativeConstants.DWMWA_COLOR_NONE,
                MapSystemBackdropType(effectiveBackdrop),
                false,
                isDark);
        }

        public static int GetCornerPreference(CornerPreference preference)
        {
            switch (preference)
            {
                case CornerPreference.DoNotRound:
                    return NativeConstants.DWMWCP_DONOTROUND;
                case CornerPreference.RoundSmall:
                    return NativeConstants.DWMWCP_ROUNDSMALL;
                case CornerPreference.Default:
                case CornerPreference.Round:
                    return NativeConstants.DWMWCP_ROUND;
                default:
                    return NativeConstants.DWMWCP_ROUND;
            }
        }

        private static int MapSystemBackdropType(BackdropType backdropType)
        {
            switch (backdropType)
            {
                case BackdropType.Acrylic:
                    return NativeConstants.DWMSBT_TRANSIENTWINDOW;
                case BackdropType.Tabbed:
                    return NativeConstants.DWMSBT_TABBEDWINDOW;
                case BackdropType.Mica:
                default:
                    return NativeConstants.DWMSBT_MAINWINDOW;
            }
        }
    }
}
