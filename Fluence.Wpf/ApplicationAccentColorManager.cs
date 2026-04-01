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

        private static bool _useSystemAccent = true;

        /// <summary>
        /// Occurs after accent ramp colors and application resources have been updated.
        /// </summary>
        public static event EventHandler<EventArgs> AccentColorChanged;

        static ApplicationAccentColorManager()
        {
            _systemAccentColor = Color.FromRgb(0x00, 0x78, 0xD4);
            GenerateAccentRamp(_systemAccentColor);
        }

        /// <summary>
        /// Gets the current base system accent color (ARGB). Default is a Windows blue until <see cref="ApplySystemAccent"/> runs.
        /// </summary>
        public static Color SystemAccentColor
        {
            get { return _systemAccentColor; }
        }

        /// <summary>
        /// Gets the lightest tint on the generated accent ramp. Default matches <see cref="SystemAccentColor"/> until the ramp is loaded.
        /// </summary>
        public static Color SystemAccentColorLight1
        {
            get { return _systemAccentColorLight1; }
        }

        public static Color SystemAccentColorLight2
        {
            get { return _systemAccentColorLight2; }
        }

        public static Color SystemAccentColorLight3
        {
            get { return _systemAccentColorLight3; }
        }

        public static Color SystemAccentColorDark1
        {
            get { return _systemAccentColorDark1; }
        }

        public static Color SystemAccentColorDark2
        {
            get { return _systemAccentColorDark2; }
        }

        public static Color SystemAccentColorDark3
        {
            get { return _systemAccentColorDark3; }
        }

        public static Color SystemAccentColorPrimary
        {
            get { return _systemAccentColorPrimary; }
        }

        public static Color SystemAccentColorSecondary
        {
            get { return _systemAccentColorSecondary; }
        }

        public static Color SystemAccentColorTertiary
        {
            get { return _systemAccentColorTertiary; }
        }

        public static bool IsAccentColorOnTitleBarsEnabled
        {
            get { return RegistryHelper.GetColorPrevalence(); }
        }

        public static void ApplySystemAccent()
        {
            _useSystemAccent = true;

            Color accent;
            Color[] palette;

            if (RegistryHelper.TryGetAccentPalette(out palette))
            {
                accent = palette[3];
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
                accent = GetAccentFromDwm();
                _systemAccentColor = accent;
                GenerateAccentRamp(accent);
            }

            var resolvedTheme = ApplicationThemeManager.GetResolvedTheme();
            UpdateThemeAdaptiveColors(resolvedTheme);
            UpdateResources();
        }

        public static void ApplyApplicationAccent()
        {
            ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
        }

        public static void ApplyCustomAccent(Color color)
        {
            _useSystemAccent = false;
            _systemAccentColor = color;
            GenerateAccentRamp(color);

            var resolvedTheme = ApplicationThemeManager.GetResolvedTheme();
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
        }

        internal static void RefreshAccent()
        {
            if (_useSystemAccent)
            {
                ApplySystemAccent();
            }
            else
            {
                var resolvedTheme = ApplicationThemeManager.GetResolvedTheme();
                UpdateThemeAdaptiveColors(resolvedTheme);
                UpdateResources();
            }
        }

        private static void GenerateAccentRamp(Color baseColor)
        {
            _systemAccentColorLight1 = HsvColorHelper.GetLightVariant(baseColor, 1);
            _systemAccentColorLight2 = HsvColorHelper.GetLightVariant(baseColor, 2);
            _systemAccentColorLight3 = HsvColorHelper.GetLightVariant(baseColor, 3);
            _systemAccentColorDark1 = HsvColorHelper.GetDarkVariant(baseColor, 1);
            _systemAccentColorDark2 = HsvColorHelper.GetDarkVariant(baseColor, 2);
            _systemAccentColorDark3 = HsvColorHelper.GetDarkVariant(baseColor, 3);
        }

        private static Color GetAccentFromDwm()
        {
            try
            {
                DWMCOLORIZATIONPARAMS parameters;
                NativeMethods.DwmGetColorizationParameters(out parameters);

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

            var resources = Application.Current.Resources;

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

            resources["AccentTextFillColorPrimaryBrush"] = new SolidColorBrush(_systemAccentColorSecondary);
            resources["AccentTextFillColorSecondaryBrush"] = new SolidColorBrush(_systemAccentColorTertiary);
            resources["AccentTextFillColorTertiaryBrush"] = new SolidColorBrush(_systemAccentColorPrimary);

            OnAccentColorChanged();
        }

        private static void OnAccentColorChanged()
        {
            var handler = AccentColorChanged;
            if (handler != null)
            {
                handler(null, EventArgs.Empty);
            }
        }

        internal static void ResetForTesting()
        {
            _systemAccentColor = Color.FromRgb(0x00, 0x78, 0xD4);
            GenerateAccentRamp(_systemAccentColor);
            _useSystemAccent = true;
        }
    }
}
