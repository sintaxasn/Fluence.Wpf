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
    // Fluence.Wpf - ApplicationAccentColorManager
    // Reads Windows accent palette, builds ramp colors, and updates merged resource dictionaries.

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
        private static Color _systemAccentColor;
        private static Color _systemAccentColorLight1;
        private static Color _systemAccentColorLight2;
        private static Color _systemAccentColorLight3;
        private static Color _systemAccentColorDark1;
        private static Color _systemAccentColorDark2;
        private static Color _systemAccentColorDark3;

        private static Color _systemAccentColorPrimary;
        private static Color _systemAccentColorSecondary;
        private static Color _systemAccentColorTertiary;

        private static Color _titleBarActiveColor;
        private static Color _titleBarInactiveColor;
        private static Color _windowBorderColor;

        private static bool _useSystemAccent = true;

        /// <summary>
        /// Occurs after accent ramp colors and application resources have been updated.
        /// </summary>
        public static event EventHandler<EventArgs>? AccentColorChanged;

        static ApplicationAccentColorManager()
        {
            _systemAccentColor = Color.FromRgb(0x00, 0x78, 0xD4);
            GenerateAccentRamp(_systemAccentColor);
        }

        /// <summary>
        /// Gets the current base system accent color (ARGB). Default is a Windows blue until <see cref="ApplySystemAccent"/> runs.
        /// </summary>
        public static Color SystemAccentColor => _systemAccentColor;

        /// <summary>
        /// Gets the lightest tint on the generated accent ramp. Default matches <see cref="SystemAccentColor"/> until the ramp is loaded.
        /// </summary>
        public static Color SystemAccentColorLight1 => _systemAccentColorLight1;

        /// <summary>
        /// Gets the second lightest tint on the generated accent ramp.
        /// </summary>
        public static Color SystemAccentColorLight2 => _systemAccentColorLight2;

        /// <summary>
        /// Gets the lightest tint on the generated accent ramp.
        /// </summary>
        public static Color SystemAccentColorLight3 => _systemAccentColorLight3;

        /// <summary>
        /// Gets the first dark shade on the generated accent ramp.
        /// </summary>
        public static Color SystemAccentColorDark1 => _systemAccentColorDark1;

        /// <summary>
        /// Gets the second dark shade on the generated accent ramp.
        /// </summary>
        public static Color SystemAccentColorDark2 => _systemAccentColorDark2;

        /// <summary>
        /// Gets the darkest shade on the generated accent ramp.
        /// </summary>
        public static Color SystemAccentColorDark3 => _systemAccentColorDark3;

        /// <summary>
        /// Gets the primary accent color used for emphasis surfaces.
        /// </summary>
        public static Color SystemAccentColorPrimary => _systemAccentColorPrimary;

        /// <summary>
        /// Gets the secondary accent color used for layered emphasis.
        /// </summary>
        public static Color SystemAccentColorSecondary => _systemAccentColorSecondary;

        /// <summary>
        /// Gets the tertiary accent color used for subtle accent fills.
        /// </summary>
        public static Color SystemAccentColorTertiary => _systemAccentColorTertiary;

        /// <summary>
        /// Gets a value indicating whether Windows is configured to show accent color on title bars and window borders.
        /// </summary>
        public static bool IsAccentColorOnTitleBarsEnabled => RegistryHelper.GetColorPrevalence();

        /// <summary>Gets the active titlebar color (from DWM AccentColor or default gray).</summary>
        public static Color TitleBarActiveColor => _titleBarActiveColor;

        /// <summary>Gets the inactive titlebar color (from DWM AccentColorInactive or default gray).</summary>
        public static Color TitleBarInactiveColor => _titleBarInactiveColor;

        /// <summary>Gets the window border color (titlebar active on Win11, blended on Win10).</summary>
        public static Color WindowBorderColor => _windowBorderColor;

        /// <summary>
        /// Loads the current Windows accent palette from the registry or DWM and updates application resources.
        /// </summary>
        public static void ApplySystemAccent()
        {
            _useSystemAccent = true;

            if (RegistryHelper.TryGetAccentPalette(out Color[]? palette) && palette is not null)
            {
                _systemAccentColorLight3 = palette[0];
                _systemAccentColorLight2 = palette[1];
                _systemAccentColorLight1 = palette[2];
                _systemAccentColor = palette[3];
                _systemAccentColorDark1 = palette[4];
                _systemAccentColorDark2 = palette[5];
                _systemAccentColorDark3 = palette[6];
            }
            else
            {
                Color accent = GetAccentFromDwm();
                _systemAccentColor = accent;
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
            _systemAccentColor = color;
            GenerateAccentRamp(color);

            ApplicationTheme resolvedTheme = ApplicationThemeManager.GetResolvedTheme();
            UpdateThemeAdaptiveColors(resolvedTheme);
            UpdateResources();
        }

        internal static void UpdateThemeAdaptiveColors(ApplicationTheme resolvedTheme)
        {
            if (resolvedTheme == ApplicationTheme.Dark)
            {
                _systemAccentColorPrimary = _systemAccentColorLight2;
                _systemAccentColorSecondary = _systemAccentColorLight1;
                _systemAccentColorTertiary = _systemAccentColor;
            }
            else
            {
                _systemAccentColorPrimary = _systemAccentColorDark1;
                _systemAccentColorSecondary = _systemAccentColorDark2;
                _systemAccentColorTertiary = _systemAccentColorDark3;
            }

            UpdateResources();
            UpdateTextOnAccentColors(resolvedTheme);
        }

        internal static void UpdateThemeDependentColors(ApplicationTheme resolvedTheme)
        {
            if (Application.Current == null)
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

            Color primary = isDark ? _systemAccentColorLight3 : _systemAccentColorDark2;
            Color secondary = isDark ? _systemAccentColorLight3 : _systemAccentColorDark3;
            Color tertiary = isDark ? _systemAccentColorLight2 : _systemAccentColorDark1;
            Color disabled = isDark ? Color.FromArgb(0x5D, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x5C, 0x00, 0x00, 0x00);

            resources["AccentTextFillColorPrimaryBrush"] = new SolidColorBrush(primary);
            resources["AccentTextFillColorSecondaryBrush"] = new SolidColorBrush(secondary);
            resources["AccentTextFillColorTertiaryBrush"] = new SolidColorBrush(tertiary);
            resources["AccentTextFillColorDisabled"] = disabled;
            resources["AccentTextFillColorDisabledBrush"] = new SolidColorBrush(disabled);
        }

        private static void UpdateTextOnAccentColors(ApplicationTheme resolvedTheme)
        {
            if (Application.Current == null)
            {
                return;
            }

            ResourceDictionary resources = Application.Current.Resources;

            bool useWhite = HsvColorHelper.ShouldUseWhiteText(_systemAccentColorPrimary);

            Color primary;
            Color secondary;
            Color disabled;

            if (useWhite)
            {
                primary = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
                secondary = Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF);
            }
            else
            {
                primary = Color.FromArgb(0xFF, 0x00, 0x00, 0x00);
                secondary = Color.FromArgb(0x80, 0x00, 0x00, 0x00);
            }

            disabled = resolvedTheme == ApplicationTheme.Dark
                ? Color.FromArgb(0x87, 0xFF, 0xFF, 0xFF)
                : Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

            resources["TextOnAccentFillColorPrimary"] = primary;
            resources["TextOnAccentFillColorSecondary"] = secondary;
            resources["TextOnAccentFillColorDisabled"] = disabled;

            resources["TextOnAccentFillColorPrimaryBrush"] = new SolidColorBrush(primary);
            resources["TextOnAccentFillColorSecondaryBrush"] = new SolidColorBrush(secondary);
            resources["TextOnAccentFillColorDisabledBrush"] = new SolidColorBrush(disabled);
        }

        internal static void RefreshAccent()
        {
            if (_useSystemAccent)
            {
                ApplySystemAccent();
            }
            else
            {
                ApplicationTheme resolvedTheme = ApplicationThemeManager.GetResolvedTheme();
                UpdateThemeAdaptiveColors(resolvedTheme);
                UpdateResources();
            }
        }

        private static void GenerateAccentRamp(Color baseColor)
        {
            HsvColorHelper.GenerateAccentRampWinaccent(
                baseColor,
                out _systemAccentColorLight1,
                out _systemAccentColorLight2,
                out _systemAccentColorLight3,
                out _systemAccentColorDark1,
                out _systemAccentColorDark2,
                out _systemAccentColorDark3);
        }

        private static Color GetAccentFromDwm()
        {
            try
            {
                NativeMethods.DwmGetColorizationParameters(out DWMCOLORIZATIONPARAMS parameters);

                uint color = parameters.clrColor;
                byte r = (byte)((color >> 16) & 0xFF);
                byte g = (byte)((color >> 8) & 0xFF);
                byte b = (byte)(color & 0xFF);

                return Color.FromRgb(r, g, b);
            }
            catch
            {
                return RegistryHelper.GetAccentColor();
            }
        }

        private static void UpdateResources()
        {
            if (Application.Current == null)
            {
                return;
            }

            ResourceDictionary resources = Application.Current.Resources;

            resources["SystemAccentColor"] = _systemAccentColor;
            resources["SystemAccentColorLight1"] = _systemAccentColorLight1;
            resources["SystemAccentColorLight2"] = _systemAccentColorLight2;
            resources["SystemAccentColorLight3"] = _systemAccentColorLight3;
            resources["SystemAccentColorDark1"] = _systemAccentColorDark1;
            resources["SystemAccentColorDark2"] = _systemAccentColorDark2;
            resources["SystemAccentColorDark3"] = _systemAccentColorDark3;

            resources["SystemAccentColorPrimary"] = _systemAccentColorPrimary;
            resources["SystemAccentColorSecondary"] = _systemAccentColorSecondary;
            resources["SystemAccentColorTertiary"] = _systemAccentColorTertiary;

            resources["SystemAccentColorBrush"] = new SolidColorBrush(_systemAccentColor);
            resources["SystemAccentColorLight1Brush"] = new SolidColorBrush(_systemAccentColorLight1);
            resources["SystemAccentColorLight2Brush"] = new SolidColorBrush(_systemAccentColorLight2);
            resources["SystemAccentColorLight3Brush"] = new SolidColorBrush(_systemAccentColorLight3);
            resources["SystemAccentColorDark1Brush"] = new SolidColorBrush(_systemAccentColorDark1);
            resources["SystemAccentColorDark2Brush"] = new SolidColorBrush(_systemAccentColorDark2);
            resources["SystemAccentColorDark3Brush"] = new SolidColorBrush(_systemAccentColorDark3);

            resources["SystemAccentColorPrimaryBrush"] = new SolidColorBrush(_systemAccentColorPrimary);
            resources["SystemAccentColorSecondaryBrush"] = new SolidColorBrush(_systemAccentColorSecondary);
            resources["SystemAccentColorTertiaryBrush"] = new SolidColorBrush(_systemAccentColorTertiary);

            resources["AccentFillColorDefault"] = _systemAccentColorPrimary;
            resources["AccentFillColorSecondary"] = HsvColorHelper.WithAlpha(_systemAccentColorPrimary, 0xE6);
            resources["AccentFillColorTertiary"] = HsvColorHelper.WithAlpha(_systemAccentColorPrimary, 0xCC);

            resources["AccentFillColorDefaultBrush"] = new SolidColorBrush(_systemAccentColorPrimary);
            resources["AccentFillColorSecondaryBrush"] = new SolidColorBrush(HsvColorHelper.WithAlpha(_systemAccentColorPrimary, 0xE6));
            resources["AccentFillColorTertiaryBrush"] = new SolidColorBrush(HsvColorHelper.WithAlpha(_systemAccentColorPrimary, 0xCC));

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
            bool colorPrevalence = RegistryHelper.GetColorPrevalence();
            bool isDark = ApplicationThemeManager.GetResolvedTheme() == ApplicationTheme.Dark;

            if (colorPrevalence)
            {
                _titleBarActiveColor = !RegistryHelper.TryGetDwmAccentColor(out Color dwmAccent)
                    ? _systemAccentColor
                    : dwmAccent;

                _titleBarInactiveColor = !RegistryHelper.TryGetDwmAccentColorInactive(out Color inactive)
                    ? isDark ? Color.FromRgb(0x2B, 0x2B, 0x2B) : Color.FromRgb(0xFF, 0xFF, 0xFF)
                    : inactive;
            }
            else
            {
                _titleBarActiveColor = isDark
                    ? Color.FromRgb(0x2B, 0x2B, 0x2B)
                    : Color.FromRgb(0xFF, 0xFF, 0xFF);
                _titleBarInactiveColor = isDark
                    ? Color.FromRgb(0x2B, 0x2B, 0x2B)
                    : Color.FromRgb(0xFF, 0xFF, 0xFF);
            }

            _windowBorderColor = Environment.OSVersion.Version.Build < 22000 && RegistryHelper.TryGetColorizationBalance(out Color colorizationColor, out int balance)
                ? HsvColorHelper.BlendColors(colorizationColor, Color.FromRgb(0xD9, 0xD9, 0xD9), balance)
                : _titleBarActiveColor;

            resources["TitleBarActiveColor"] = _titleBarActiveColor;
            resources["TitleBarInactiveColor"] = _titleBarInactiveColor;
            resources["WindowBorderColor"] = _windowBorderColor;

            resources["TitleBarActiveColorBrush"] = new SolidColorBrush(_titleBarActiveColor);
            resources["TitleBarInactiveColorBrush"] = new SolidColorBrush(_titleBarInactiveColor);
            resources["WindowBorderColorBrush"] = new SolidColorBrush(_windowBorderColor);
        }

        private static void OnAccentColorChanged()
        {
            AccentColorChanged?.Invoke(null, EventArgs.Empty);
        }

        internal static void ResetForTesting()
        {
            _systemAccentColor = Color.FromRgb(0x00, 0x78, 0xD4);
            GenerateAccentRamp(_systemAccentColor);
            _useSystemAccent = true;
        }
    }
}
