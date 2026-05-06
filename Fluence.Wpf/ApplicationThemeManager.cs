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
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using Fluence.Wpf.Helpers;

namespace Fluence.Wpf
{
    /// <summary>
    /// Manages theme initialization and switching for the Fluence.Wpf control library.
    /// Call <see cref="Apply"/> once at application startup to initialize all resource dictionaries.
    /// Subsequent calls swap only the theme color dictionary.
    /// </summary>
    public static class ApplicationThemeManager
    {
        private const string AssemblyComponent = "Fluence.Wpf;component";
        private const string PackBase = "pack://application:,,,/" + AssemblyComponent + "/";

        /*
         * Stable merge order in Application.Current.Resources.MergedDictionaries:
         *   [0] Theme Colors   - Theme.{Light|Dark|HighContrast}.xaml  (SWAPPED on theme change)
         *   [1] Accent          - Accent.xaml                           (loaded once, keys updated in-place)
         *   [2] Brushes         - Brushes.xaml                          (reloaded on non-HC theme swaps)
         *   [3] Typography      - Typography.xaml                       (loaded once, never replaced)
         *   [4] Generic         - Generic.xaml                          (loaded once, never replaced)
         *
         * For HighContrast, the theme dict at [0] contains both Color keys (static fallbacks)
         * and Brush keys (with live SystemColor DynamicResource bindings). These brush keys
         * override the equivalent keys from Brushes.xaml at [2] because we place them
         * directly into Application.Resources AFTER merging, ensuring correct precedence.
         */
        private const int SlotTheme = 0;
        private const int SlotAccent = 1;
        private const int SlotBrushes = 2;
        private const int SlotTypography = 3;
        private const int SlotGeneric = 4;

        private static bool _isInitialized;
        private static bool _isApplying;
        private static System.Collections.Generic.List<object> _promotedHighContrastBrushKeys;

        /// <summary>Gets the currently requested theme (may be <see cref="ApplicationTheme.Auto"/>).</summary>
        public static ApplicationTheme CurrentTheme { get; private set; } = ApplicationTheme.Auto;

        /// <summary>Gets the currently requested backdrop type.</summary>
        public static BackdropType CurrentBackdrop { get; private set; } = BackdropType.Auto;

        /// <summary>
        /// Gets a value indicating whether the Windows system (window-chrome) color mode is currently Dark.
        /// Reflects the live registry value; independent of <see cref="CurrentTheme"/>.
        /// </summary>
        public static bool IsSystemInDarkMode => !RegistryHelper.GetSystemUsesLightTheme();

        /// <summary>
        /// Gets a value indicating whether the Windows app color mode is currently Dark.
        /// Reflects the live registry value; independent of <see cref="CurrentTheme"/>.
        /// </summary>
        public static bool IsAppInDarkMode => !RegistryHelper.GetAppsUseLightTheme();

        /// <summary>Raised after a theme or accent change has been applied.</summary>
        public static event EventHandler<ThemeChangedEventArgs> Changed;

        /// <summary>
        /// Initialize the theme system and apply the specified theme.
        /// The first call loads all resource dictionaries (Colors, Accent, Brushes, Typography, Controls).
        /// Subsequent calls only swap the theme color dictionary.
        /// </summary>
        public static void Apply(ApplicationTheme theme, BackdropType backdrop = BackdropType.Auto, bool updateAccent = true)
        {
            if (_isApplying)
            {
                return;
            }

            _isApplying = true;

            try
            {
                ApplicationTheme resolvedTheme = ResolveTheme(theme);
                CurrentTheme = theme;
                CurrentBackdrop = backdrop;

                if (!_isInitialized)
                {
                    InitializeDictionaries(resolvedTheme);
                }
                else
                {
                    SwapThemeColors(resolvedTheme);
                }

                if (updateAccent)
                {
                    ApplicationAccentColorManager.UpdateThemeAdaptiveColors(resolvedTheme);
                }
                else
                {
                    ApplicationAccentColorManager.UpdateThemeDependentColors(resolvedTheme);
                }

                OnChanged(resolvedTheme);
            }
            finally
            {
                _isApplying = false;
            }
        }

        /// <summary>Re-applies with <see cref="ApplicationTheme.Auto"/> to pick up system changes.</summary>
        public static void ApplySystemTheme()
        {
            Apply(ApplicationTheme.Auto, CurrentBackdrop, true);
        }

        internal static ApplicationTheme ResolveTheme(ApplicationTheme theme)
        {
            return theme == ApplicationTheme.Auto
                ? !RegistryHelper.IsHighContrastEnabled() ? !RegistryHelper.GetAppsUseLightTheme() ? ApplicationTheme.Dark : ApplicationTheme.Light : ApplicationTheme.HighContrast
                : theme;
        }

        internal static ApplicationTheme GetResolvedTheme()
        {
            return ResolveTheme(CurrentTheme);
        }

        private static void InitializeDictionaries(ApplicationTheme resolvedTheme)
        {
            if (Application.Current == null)
            {
                return;
            }

            Collection<ResourceDictionary> dictionaries = Application.Current.Resources.MergedDictionaries;

            ResourceDictionary themeDict = LoadDictionary(GetThemeColorUri(resolvedTheme));
            ResourceDictionary accentDict = LoadDictionary(PackBase + "Themes/Accent/Accent.xaml");
            ResourceDictionary brushesDict = LoadDictionary(PackBase + "Themes/Brushes/Brushes.xaml");
            ResourceDictionary typographyDict = LoadDictionary(PackBase + "Themes/Typography/Typography.xaml");
            ResourceDictionary genericDict = LoadDictionary(PackBase + "Themes/Generic.xaml");

            // Insert into fixed slots instead of appending. Tests assert this shape because
            // theme swaps depend on replacing slot 0 while preserving accent, brushes,
            // typography, and control-template dictionaries.
            dictionaries.Insert(SlotTheme, themeDict);
            dictionaries.Insert(SlotAccent, accentDict);
            dictionaries.Insert(SlotBrushes, brushesDict);
            dictionaries.Insert(SlotTypography, typographyDict);
            dictionaries.Insert(SlotGeneric, genericDict);

            PromoteThemeColors(resolvedTheme);
            EnsureAcrylicNoiseBrush();

            _isInitialized = true;
        }

        private static void SwapThemeColors(ApplicationTheme resolvedTheme)
        {
            if (Application.Current == null)
            {
                return;
            }

            Collection<ResourceDictionary> dictionaries = Application.Current.Resources.MergedDictionaries;

            if (SlotTheme < dictionaries.Count)
            {
                dictionaries[SlotTheme] = LoadDictionary(GetThemeColorUri(resolvedTheme));
            }

            PromoteThemeColors(resolvedTheme);

            if (resolvedTheme != ApplicationTheme.HighContrast)
            {
                // Leaving High Contrast must restore normal brush precedence. Reloading the
                // brush dictionary recreates dynamic SolidColorBrush bindings against the
                // newly-promoted color keys.
                ReloadAndPromoteBrushes(dictionaries);
            }

            EnsureAcrylicNoiseBrush();
        }

        /// <summary>
        /// Replaces the Brushes dictionary with a freshly loaded copy, then promotes
        /// every brush key into top-level <see cref="Application.Resources"/>.
        /// This is necessary because <c>DynamicResource</c> bindings on
        /// <see cref="Freezable"/> properties (e.g. <c>SolidColorBrush.Color</c>)
        /// do not reliably re-evaluate when the target Color resource changes in a
        /// different scope after a dictionary swap.
        /// </summary>
        private static void ReloadAndPromoteBrushes(Collection<ResourceDictionary> dictionaries)
        {
            if (SlotBrushes >= dictionaries.Count)
            {
                return;
            }

            ResourceDictionary freshBrushes = LoadDictionary(PackBase + "Themes/Brushes/Brushes.xaml");
            dictionaries[SlotBrushes] = freshBrushes;

            ResourceDictionary resources = Application.Current.Resources;
            foreach (object? key in freshBrushes.Keys)
            {
                resources[key] = freshBrushes[key];
            }
        }

        /// <summary>
        /// Copies all keys from the active theme dictionary into the top-level
        /// <see cref="Application.Resources"/> so that <c>DynamicResource</c> bindings
        /// in sibling MergedDictionaries (Brushes.xaml, Typography.xaml, etc.) reliably
        /// resolve to the current theme values after a dictionary swap.
        /// For HighContrast, Brush keys are also promoted; when leaving HighContrast,
        /// stale Brush overrides are removed.
        /// </summary>
        private static void PromoteThemeColors(ApplicationTheme resolvedTheme)
        {
            if (Application.Current == null)
            {
                return;
            }

            ResourceDictionary resources = Application.Current.Resources;
            ResourceDictionary themeDict = resources.MergedDictionaries[SlotTheme];

            if (resolvedTheme == ApplicationTheme.HighContrast)
            {
                _promotedHighContrastBrushKeys = [];

                foreach (object? key in themeDict.Keys)
                {
                    resources[key] = themeDict[key];

                    if (key is string keyStr && keyStr.EndsWith("Brush"))
                    {
                        _promotedHighContrastBrushKeys.Add(key);
                    }
                }
            }
            else
            {
                if (_promotedHighContrastBrushKeys != null)
                {
                    foreach (object key in _promotedHighContrastBrushKeys)
                    {
                        resources.Remove(key);
                    }
                    _promotedHighContrastBrushKeys = null;
                }

                foreach (object? key in themeDict.Keys)
                {
                    resources[key] = themeDict[key];
                }
            }
        }

        private static Uri GetThemeColorUri(ApplicationTheme resolvedTheme)
        {
            string themeName = resolvedTheme switch
            {
                ApplicationTheme.Dark => "Dark",
                ApplicationTheme.HighContrast => "HighContrast",
                ApplicationTheme.Light or ApplicationTheme.Auto or _ => "Light",
            };
            return new Uri(PackBase + "Themes/Colors/Theme." + themeName + ".xaml", UriKind.Absolute);
        }

        private static ResourceDictionary LoadDictionary(Uri uri)
        {
            return new ResourceDictionary { Source = uri };
        }

        private static ResourceDictionary LoadDictionary(string uri)
        {
            return LoadDictionary(new Uri(uri, UriKind.Absolute));
        }

        private static void EnsureAcrylicNoiseBrush()
        {
            if (Application.Current == null)
            {
                return;
            }

            const string key = "AcrylicNoiseBrush";
            if (Application.Current.Resources[key] == null)
            {
                Application.Current.Resources[key] = AcrylicNoiseHelper.GetNoiseBrush();
            }
        }

        private static void OnChanged(ApplicationTheme resolvedTheme)
        {
            EventHandler<ThemeChangedEventArgs> handler = Changed;
            if (handler != null)
            {
                Color accent = ApplicationAccentColorManager.SystemAccentColor;
                handler(null, new ThemeChangedEventArgs(resolvedTheme, accent));
            }
        }

        internal static void ResetForTesting()
        {
            _isInitialized = false;
            CurrentTheme = ApplicationTheme.Auto;
            CurrentBackdrop = BackdropType.Auto;
            _isApplying = false;
            _promotedHighContrastBrushKeys = null;
        }
    }
}
