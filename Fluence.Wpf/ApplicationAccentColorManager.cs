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
using System.Windows;
using System.Windows.Media;
using Fluence.Wpf.Helpers;
using Fluence.Wpf.Native;

namespace Fluence.Wpf
{
    /// <summary>
    /// Manages system and custom accent colors and publishes them as <c>DynamicResource</c> brush keys aligned with Windows 11.
    /// </summary>
    /// <remarks>
    /// Call <see cref="ApplySystemAccent"/>, <see cref="ApplyApplicationAccent"/>, or <see cref="ApplyCustomAccent"/> after
    /// <see cref="ApplicationThemeManager.Apply"/> so theme-dependent primary/secondary/tertiary accents resolve correctly.
    /// </remarks>
    /// <example>
    /// <code>
    /// ApplicationThemeManager.Apply(ApplicationTheme.Auto, BackdropType.Mica, updateAccent: true);
    /// ApplicationAccentColorManager.ApplySystemAccent();
    /// </code>
    /// </example>
    public static class ApplicationAccentColorManager
    {
        /// <summary>
        /// Occurs after accent ramp colors and application resources have been updated.
        /// </summary>
        public static event EventHandler<EventArgs>? AccentColorChanged;

        /// <summary>
        /// Initializes static members of the ApplicationAccentColorManager class and sets the default system accent
        /// color.
        /// </summary>
        /// <remarks>This static constructor is called automatically before any static members are
        /// accessed or any instances are created. It sets the initial system accent color and generates the
        /// corresponding accent color ramp.</remarks>
        static ApplicationAccentColorManager()
        {
            SystemAccentColor = Color.FromRgb(0x00, 0x78, 0xD4);
            GenerateAccentRamp(SystemAccentColor);
        }

        /// <summary>
        /// Gets the current base system accent color (ARGB). Default is a Windows blue until <see cref="ApplySystemAccent"/> runs.
        /// </summary>
        public static Color SystemAccentColor { get; private set; }

        /// <summary>
        /// Gets the lightest tint on the generated accent ramp. Default matches <see cref="SystemAccentColor"/> until the ramp is loaded.
        /// </summary>
        public static Color SystemAccentColorLight1 { get; private set; }

        /// <summary>
        /// Gets the second lightest tint on the generated accent ramp.
        /// </summary>
        public static Color SystemAccentColorLight2 { get; private set; }

        /// <summary>
        /// Gets the lightest tint on the generated accent ramp.
        /// </summary>
        public static Color SystemAccentColorLight3 { get; private set; }

        /// <summary>
        /// Gets the first dark shade on the generated accent ramp.
        /// </summary>
        public static Color SystemAccentColorDark1 { get; private set; }

        /// <summary>
        /// Gets the second dark shade on the generated accent ramp.
        /// </summary>
        public static Color SystemAccentColorDark2 { get; private set; }

        /// <summary>
        /// Gets the darkest shade on the generated accent ramp.
        /// </summary>
        public static Color SystemAccentColorDark3 { get; private set; }

        /// <summary>
        /// Gets the primary accent color used for emphasis surfaces.
        /// </summary>
        public static Color SystemAccentColorPrimary { get; private set; }

        /// <summary>
        /// Gets the secondary accent color used for layered emphasis.
        /// </summary>
        public static Color SystemAccentColorSecondary { get; private set; }

        /// <summary>
        /// Gets the tertiary accent color used for subtle accent fills.
        /// </summary>
        public static Color SystemAccentColorTertiary { get; private set; }

        /// <summary>
        /// Gets a value indicating whether Windows is configured to show accent color on title bars and window borders.
        /// </summary>
        public static bool IsAccentColorOnTitleBarsEnabled => RegistryHelper.GetColorPrevalence();

        /// <summary>
        /// Gets the active titlebar color (from DWM AccentColor or default gray).
        /// </summary>
        public static Color TitleBarActiveColor { get; private set; }

        /// <summary>
        /// Gets the inactive titlebar color (from DWM AccentColorInactive or default gray).
        /// </summary>
        public static Color TitleBarInactiveColor { get; private set; }

        /// <summary>
        /// Gets the window border color (titlebar active on Win11, blended on Win10).
        /// </summary>
        public static Color WindowBorderColor { get; private set; }

        /// <summary>
        /// Loads the current Windows accent palette from the registry or DWM and updates application resources.
        /// </summary>
        public static void ApplySystemAccent()
        {
            _useSystemAccent = true;
            if (RegistryHelper.TryGetAccentPalette(out Color[]? palette) && palette is not null)
            {
                SystemAccentColorLight3 = palette[0];
                SystemAccentColorLight2 = palette[1];
                SystemAccentColorLight1 = palette[2];
                SystemAccentColor = palette[3];
                SystemAccentColorDark1 = palette[4];
                SystemAccentColorDark2 = palette[5];
                SystemAccentColorDark3 = palette[6];
            }
            else
            {
                Color accent = GetAccentFromDwm();
                SystemAccentColor = accent;
                GenerateAccentRamp(accent);
            }
            ApplicationTheme resolvedTheme = ApplicationThemeManager.GetResolvedTheme();
            UpdateThemeAdaptiveColors(resolvedTheme);
            UpdateResources();
        }

        /// <summary>
        /// Applies the default application accent (Windows blue) and regenerates the accent ramp.
        /// </summary>
        public static void ApplyApplicationAccent()
        {
            ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
        }

        /// <summary>
        /// Applies a custom base accent color and regenerates the accent ramp and theme resources.
        /// </summary>
        /// <param name="color">The accent color to use as the ramp base.</param>
        public static void ApplyCustomAccent(Color color)
        {
            _useSystemAccent = false;
            SystemAccentColor = color;
            GenerateAccentRamp(color);
            ApplicationTheme resolvedTheme = ApplicationThemeManager.GetResolvedTheme();
            UpdateThemeAdaptiveColors(resolvedTheme);
            UpdateResources();
        }

        internal static void UpdateThemeAdaptiveColors(ApplicationTheme resolvedTheme)
        {
            if (resolvedTheme == ApplicationTheme.Dark)
            {
                SystemAccentColorPrimary = SystemAccentColorLight2;
                SystemAccentColorSecondary = SystemAccentColorLight1;
                SystemAccentColorTertiary = SystemAccentColor;
            }
            else
            {
                SystemAccentColorPrimary = SystemAccentColorDark1;
                SystemAccentColorSecondary = SystemAccentColorDark2;
                SystemAccentColorTertiary = SystemAccentColorDark3;
            }
            UpdateResources();
            UpdateTextOnAccentColors(resolvedTheme);
        }

        internal static void UpdateThemeDependentColors(ApplicationTheme resolvedTheme)
        {
            if (Application.Current is null)
            {
                return;
            }
            ResourceDictionary resources = Application.Current.Resources;
            UpdateDisabledAccentFill(resources, resolvedTheme);
            UpdateAccentTextBrushes(resources);
            UpdateTextOnAccentColors(resolvedTheme);
        }

        private static void UpdateAccentTextBrushes(ResourceDictionary resources)
        {
            bool isDark = ApplicationThemeManager.GetResolvedTheme() == ApplicationTheme.Dark;
            Color primary = isDark ? SystemAccentColorLight3 : SystemAccentColorDark2;
            Color secondary = isDark ? SystemAccentColorLight3 : SystemAccentColorDark3;
            Color tertiary = isDark ? SystemAccentColorLight2 : SystemAccentColorDark1;
            Color disabled = isDark ? Color.FromArgb(0x5D, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x5C, 0x00, 0x00, 0x00);
            resources["AccentTextFillColorPrimaryBrush"] = new SolidColorBrush(primary);
            resources["AccentTextFillColorSecondaryBrush"] = new SolidColorBrush(secondary);
            resources["AccentTextFillColorTertiaryBrush"] = new SolidColorBrush(tertiary);
            resources["AccentTextFillColorDisabled"] = disabled;
            resources["AccentTextFillColorDisabledBrush"] = new SolidColorBrush(disabled);
        }

        private static void UpdateTextOnAccentColors(ApplicationTheme resolvedTheme)
        {
            if (Application.Current is null)
            {
                return;
            }
            Color primary; Color secondary; Color disabled = resolvedTheme == ApplicationTheme.Dark
                ? Color.FromArgb(0x87, 0xFF, 0xFF, 0xFF)
                : Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            if (HsvColorHelper.ShouldUseWhiteText(SystemAccentColorPrimary))
            {
                primary = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
                secondary = Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF);
            }
            else
            {
                primary = Color.FromArgb(0xFF, 0x00, 0x00, 0x00);
                secondary = Color.FromArgb(0x80, 0x00, 0x00, 0x00);
            }
            ResourceDictionary resources = Application.Current.Resources;
            resources["TextOnAccentFillColorPrimary"] = primary;
            resources["TextOnAccentFillColorSecondary"] = secondary;
            resources["TextOnAccentFillColorDisabled"] = disabled;
            resources["TextOnAccentFillColorPrimaryBrush"] = new SolidColorBrush(primary);
            resources["TextOnAccentFillColorSecondaryBrush"] = new SolidColorBrush(secondary);
            resources["TextOnAccentFillColorDisabledBrush"] = new SolidColorBrush(disabled);
        }

        internal static void RefreshAccent()
        {
            if (!_useSystemAccent)
            {
                ApplicationTheme resolvedTheme = ApplicationThemeManager.GetResolvedTheme();
                UpdateThemeAdaptiveColors(resolvedTheme);
                UpdateResources();
            }
            else
            {
                ApplySystemAccent();
            }
        }

        private static void GenerateAccentRamp(Color baseColor)
        {
            HsvColorHelper.GenerateAccentRampWinaccent(baseColor,
                out Color systemAccentColorLight1, out Color systemAccentColorLight2, out Color systemAccentColorLight3,
                out Color systemAccentColorDark1, out Color systemAccentColorDark2, out Color systemAccentColorDark3);
            SystemAccentColorLight1 = systemAccentColorLight1;
            SystemAccentColorLight2 = systemAccentColorLight2;
            SystemAccentColorLight3 = systemAccentColorLight3;
            SystemAccentColorDark1 = systemAccentColorDark1;
            SystemAccentColorDark2 = systemAccentColorDark2;
            SystemAccentColorDark3 = systemAccentColorDark3;
        }

        private static Color GetAccentFromDwm()
        {
            NativeMethods.DwmGetColorizationParameters(out DWMCOLORIZATIONPARAMS parameters);
            uint color = parameters.clrColor;
            byte r = (byte)((color >> 16) & 0xFF);
            byte g = (byte)((color >> 8) & 0xFF);
            byte b = (byte)(color & 0xFF);
            return Color.FromRgb(r, g, b);
        }

        private static void UpdateResources()
        {
            if (Application.Current is null)
            {
                return;
            }
            ResourceDictionary resources = Application.Current.Resources;
            resources["SystemAccentColor"] = SystemAccentColor;
            resources["SystemAccentColorLight1"] = SystemAccentColorLight1;
            resources["SystemAccentColorLight2"] = SystemAccentColorLight2;
            resources["SystemAccentColorLight3"] = SystemAccentColorLight3;
            resources["SystemAccentColorDark1"] = SystemAccentColorDark1;
            resources["SystemAccentColorDark2"] = SystemAccentColorDark2;
            resources["SystemAccentColorDark3"] = SystemAccentColorDark3;
            resources["SystemAccentColorPrimary"] = SystemAccentColorPrimary;
            resources["SystemAccentColorSecondary"] = SystemAccentColorSecondary;
            resources["SystemAccentColorTertiary"] = SystemAccentColorTertiary;
            resources["SystemAccentColorBrush"] = new SolidColorBrush(SystemAccentColor);
            resources["SystemAccentColorLight1Brush"] = new SolidColorBrush(SystemAccentColorLight1);
            resources["SystemAccentColorLight2Brush"] = new SolidColorBrush(SystemAccentColorLight2);
            resources["SystemAccentColorLight3Brush"] = new SolidColorBrush(SystemAccentColorLight3);
            resources["SystemAccentColorDark1Brush"] = new SolidColorBrush(SystemAccentColorDark1);
            resources["SystemAccentColorDark2Brush"] = new SolidColorBrush(SystemAccentColorDark2);
            resources["SystemAccentColorDark3Brush"] = new SolidColorBrush(SystemAccentColorDark3);
            resources["SystemAccentColorPrimaryBrush"] = new SolidColorBrush(SystemAccentColorPrimary);
            resources["SystemAccentColorSecondaryBrush"] = new SolidColorBrush(SystemAccentColorSecondary);
            resources["SystemAccentColorTertiaryBrush"] = new SolidColorBrush(SystemAccentColorTertiary);
            resources["AccentFillColorDefault"] = SystemAccentColorPrimary;
            resources["AccentFillColorSecondary"] = HsvColorHelper.WithAlpha(SystemAccentColorPrimary, 0xE6);
            resources["AccentFillColorTertiary"] = HsvColorHelper.WithAlpha(SystemAccentColorPrimary, 0xCC);
            resources["AccentFillColorDefaultBrush"] = new SolidColorBrush(SystemAccentColorPrimary);
            resources["AccentFillColorSecondaryBrush"] = new SolidColorBrush(HsvColorHelper.WithAlpha(SystemAccentColorPrimary, 0xE6));
            resources["AccentFillColorTertiaryBrush"] = new SolidColorBrush(HsvColorHelper.WithAlpha(SystemAccentColorPrimary, 0xCC));
            ApplicationTheme resolvedTheme = ApplicationThemeManager.GetResolvedTheme();
            UpdateDisabledAccentFill(resources, resolvedTheme);
            UpdateAccentTextBrushes(resources);
            UpdateTitleBarColors(resources);
            OnAccentColorChanged();
        }

        private static void UpdateDisabledAccentFill(ResourceDictionary resources, ApplicationTheme resolvedTheme)
        {
            if (resolvedTheme == ApplicationTheme.HighContrast)
            {
                return;
            }

            Color disabledAccentFill = resolvedTheme == ApplicationTheme.Dark
                ? Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF)
                : Color.FromArgb(0x37, 0x00, 0x00, 0x00);
            resources["AccentFillColorDisabled"] = disabledAccentFill;
            resources["AccentFillColorDisabledBrush"] = new SolidColorBrush(disabledAccentFill);
        }

        private static void UpdateTitleBarColors(ResourceDictionary resources)
        {
            bool isDark = ApplicationThemeManager.GetResolvedTheme() == ApplicationTheme.Dark;
            if (RegistryHelper.GetColorPrevalence())
            {
                TitleBarActiveColor = !RegistryHelper.TryGetDwmAccentColor(out Color dwmAccent)
                    ? SystemAccentColor
                    : dwmAccent;
                TitleBarInactiveColor = !RegistryHelper.TryGetDwmAccentColorInactive(out Color inactive)
                    ? isDark ? Color.FromRgb(0x2B, 0x2B, 0x2B) : Color.FromRgb(0xFF, 0xFF, 0xFF)
                    : inactive;
            }
            else
            {
                TitleBarActiveColor = isDark
                    ? Color.FromRgb(0x2B, 0x2B, 0x2B)
                    : Color.FromRgb(0xFF, 0xFF, 0xFF);
                TitleBarInactiveColor = isDark
                    ? Color.FromRgb(0x2B, 0x2B, 0x2B)
                    : Color.FromRgb(0xFF, 0xFF, 0xFF);
            }
            WindowBorderColor = Environment.OSVersion.Version.Build < 22000 && RegistryHelper.TryGetColorizationBalance(out Color colorizationColor, out int balance)
                ? HsvColorHelper.BlendColors(colorizationColor, Color.FromRgb(0xD9, 0xD9, 0xD9), balance)
                : TitleBarActiveColor;

            resources["TitleBarActiveColor"] = TitleBarActiveColor;
            resources["TitleBarInactiveColor"] = TitleBarInactiveColor;
            resources["WindowBorderColor"] = WindowBorderColor;
            resources["TitleBarActiveColorBrush"] = new SolidColorBrush(TitleBarActiveColor);
            resources["TitleBarInactiveColorBrush"] = new SolidColorBrush(TitleBarInactiveColor);
            resources["WindowBorderColorBrush"] = new SolidColorBrush(WindowBorderColor);
        }

        private static void OnAccentColorChanged()
        {
            AccentColorChanged?.Invoke(null, EventArgs.Empty);
        }

        internal static void ResetForTesting()
        {
            SystemAccentColor = Color.FromRgb(0x00, 0x78, 0xD4);
            GenerateAccentRamp(SystemAccentColor);
            _useSystemAccent = true;
        }

        /// <summary>
        /// Indicates whether the system accent color should be used.
        /// </summary>
        private static bool _useSystemAccent = true;
    }
}
