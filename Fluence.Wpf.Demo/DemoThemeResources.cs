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

namespace Fluence.Wpf.Demo
{
    /// <summary>
    /// Maintains demo-only surface brushes that align the gallery shell with WinUI Gallery.
    /// </summary>
    public static class DemoThemeResources
    {
        private const string DemoPageBackgroundBrushKey = "DemoPageBackgroundBrush";
        private const string DemoControlSurfaceBrushKey = "DemoControlSurfaceBrush";
        private const string DemoSourceSurfaceBrushKey = "DemoSourceSurfaceBrush";
        private const string DemoSettingsCardBrushKey = "DemoSettingsCardBrush";
        private const string DemoSectionCardBrushKey = "DemoSectionCardBrush";
        private const string DemoFieldLabelForegroundBrushKey = "DemoFieldLabelForegroundBrush";
        private static bool _initialized;

        /// <summary>
        /// Starts listening for theme changes and promotes the current demo brushes.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            ApplicationThemeManager.Changed += OnThemeChanged;
            RefreshForCurrentTheme();
        }

        /// <summary>
        /// Re-promotes demo brushes for the current effective application theme.
        /// </summary>
        public static void RefreshForCurrentTheme()
        {
            Apply(GetEffectiveTheme());
        }

        private static void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            Apply(e.Theme);
        }

        private static ApplicationTheme GetEffectiveTheme()
        {
            ApplicationTheme current = ApplicationThemeManager.CurrentTheme;
            return SystemParameters.HighContrast
                ? ApplicationTheme.HighContrast
                : current != ApplicationTheme.Auto
                    ? current
                    : ApplicationThemeManager.IsAppInDarkMode
                        ? ApplicationTheme.Dark
                        : ApplicationTheme.Light;
        }

        private static void Apply(ApplicationTheme theme)
        {
            ResourceDictionary? resources = Application.Current?.Resources;
            if (resources is null)
            {
                return;
            }

            if (theme == ApplicationTheme.Dark)
            {
                SetBrush(resources, DemoPageBackgroundBrushKey, Color.FromRgb(0x27, 0x27, 0x27));
                SetBrush(resources, DemoControlSurfaceBrushKey, Color.FromRgb(0x20, 0x20, 0x20));
                SetBrush(resources, DemoSourceSurfaceBrushKey, Color.FromRgb(0x32, 0x32, 0x32));
                SetBrush(resources, DemoSettingsCardBrushKey, Color.FromRgb(0x32, 0x32, 0x32));
                SetBrush(resources, DemoSectionCardBrushKey, Color.FromRgb(0x20, 0x20, 0x20));
                PromoteBrush(resources, DemoFieldLabelForegroundBrushKey, "TextFillColorSecondaryBrush");
                return;
            }

            PromoteBrush(resources, DemoPageBackgroundBrushKey, "SolidBackgroundFillColorBaseBrush");
            PromoteBrush(resources, DemoControlSurfaceBrushKey, "CardBackgroundFillColorDefaultBrush");
            PromoteBrush(resources, DemoSourceSurfaceBrushKey, "CardBackgroundFillColorSecondaryBrush");
            PromoteBrush(resources, DemoSettingsCardBrushKey, "CardBackgroundFillColorDefaultBrush");
            PromoteBrush(resources, DemoSectionCardBrushKey, "CardBackgroundFillColorDefaultBrush");
            PromoteBrush(resources, DemoFieldLabelForegroundBrushKey, "TextFillColorSecondaryBrush");
        }

        private static void SetBrush(ResourceDictionary resources, string key, Color color)
        {
            resources[key] = new SolidColorBrush(color);
        }

        private static void PromoteBrush(ResourceDictionary resources, string targetKey, string sourceKey)
        {
            if (resources[sourceKey] is SolidColorBrush source)
            {
                SetBrush(resources, targetKey, source.Color);
                return;
            }

            if (Application.Current?.TryFindResource(sourceKey) is SolidColorBrush fallback)
            {
                SetBrush(resources, targetKey, fallback.Color);
            }
        }
    }
}
