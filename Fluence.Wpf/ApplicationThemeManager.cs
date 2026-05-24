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
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace Fluence.Wpf
{
    /// <summary>
    /// Manages Fluence.Wpf theme resource dictionaries, accent coordination, and runtime theme changes.
    /// </summary>
    /// <remarks>
    /// The first <see cref="Apply"/> call initializes the fixed resource dictionary slots. Later calls replace
    /// the active color dictionary, promote theme keys, and reload promoted brushes when leaving high contrast.
    /// </remarks>
    public static class ApplicationThemeManager
    {
        /// <summary>
        /// Gets the currently requested theme (may be <see cref="ApplicationTheme.Auto"/>).
        /// </summary>
        public static ApplicationTheme CurrentTheme { get; private set; } = ApplicationTheme.Auto;

        /// <summary>
        /// Gets the currently requested backdrop type.
        /// </summary>
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

        /// <summary>
        /// Raised after a theme or accent change has been applied.
        /// </summary>
        public static event EventHandler<ThemeChangedEventArgs>? Changed;

        /// <summary>
        /// Initializes the theme resource stack or applies a later theme change.
        /// </summary>
        /// <param name="theme">The requested application theme. Use <see cref="ApplicationTheme.Auto"/> to follow Windows app theme settings.</param>
        /// <param name="backdrop">The requested window backdrop policy retained for <see cref="CurrentBackdrop"/> consumers.</param>
        /// <param name="updateAccent"><c>true</c> to update accent resources with the full theme-adaptive path; otherwise <c>false</c> to refresh only theme-dependent accent colors.</param>
        /// <remarks>
        /// The first call loads the Colors, Accent, Brushes, Typography, and Generic dictionaries into stable slots.
        /// Later calls replace the Colors slot, promote color keys into application resources, and reload the Brushes
        /// slot on non-high-contrast themes so <c>DynamicResource</c> brush chains re-evaluate.
        /// </remarks>
        public static void Apply(ApplicationTheme theme, BackdropType backdrop = BackdropType.Auto, bool updateAccent = true)
        {
            if (_isApplying)
            {
                return;
            }
            _isApplying = true;
            try
            {
                TabKeyboardNavigation.EnsureRegistered();
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

        /// <summary>
        /// Re-applies with <see cref="ApplicationTheme.Auto"/> to pick up system changes.
        /// </summary>
        public static void ApplySystemTheme()
        {
            Apply(ApplicationTheme.Auto, CurrentBackdrop);
        }

        internal static ApplicationTheme ResolveTheme(ApplicationTheme theme)
        {
            if (theme != ApplicationTheme.Auto)
            {
                return theme;
            }

            // High contrast always wins, regardless of the active Windows theme file.
            if (RegistryHelper.IsHighContrastEnabled())
            {
                return ApplicationTheme.HighContrast;
            }

            // Dual fallback: first try HKCU\...\Themes\CurrentTheme filename so Windows 11
            // named themes (themea.theme ... themed.theme) and HC variants are recognised.
            // If the filename is unknown or absent, fall through to AppsUseLightTheme.
            string? themeFile = RegistryHelper.GetCurrentThemeFileNameLowerInvariant();
            if (themeFile is not null && themeFile.Length > 0)
            {
                if (themeFile.Contains("hc1")
                    || themeFile.Contains("hc2")
                    || themeFile.Contains("hcblack")
                    || themeFile.Contains("hcwhite"))
                {
                    // Defensive backstop in case SystemParameters.HighContrast is unset
                    // mid-transition: a Windows HC theme filename always means HighContrast.
                    return ApplicationTheme.HighContrast;
                }
                if (themeFile.Contains("dark"))
                {
                    return ApplicationTheme.Dark;
                }
                if (themeFile.Contains("aero")
                    || themeFile.Contains("basic")
                    || themeFile.Contains("aerolite")
                    || themeFile.StartsWith("themea")
                    || themeFile.StartsWith("themeb")
                    || themeFile.StartsWith("themec")
                    || themeFile.StartsWith("themed"))
                {
                    return ApplicationTheme.Light;
                }
            }

            return RegistryHelper.GetAppsUseLightTheme() ? ApplicationTheme.Light : ApplicationTheme.Dark;
        }

        internal static ApplicationTheme GetResolvedTheme()
        {
            return ResolveTheme(CurrentTheme);
        }

        private static void InitializeDictionaries(ApplicationTheme resolvedTheme)
        {
            if (Application.Current is null)
            {
                return;
            }
            Collection<ResourceDictionary> dictionaries = Application.Current.Resources.MergedDictionaries;

            // Defensive: if a consumer pre-merged a Fluence dictionary (identified by the
            // SlotMarker key inside each slot's XAML), skip it - don't insert a duplicate.
            // Without this guard, calling Apply on a host that already merged our dictionaries
            // would produce two copies of every Fluence resource (e.g. two Theme.Light.xaml
            // entries), corrupt the SlotColors index for theme swaps, and double the brush
            // promotion work.
            ResourceDictionary themeDict = LoadDictionary(GetThemeColorUri(resolvedTheme));
            ResourceDictionary accentDict = LoadDictionary(PackBase + "Themes/Accent/Accent.xaml");
            ResourceDictionary brushesDict = LoadDictionary(PackBase + "Themes/Brushes/Brushes.xaml");
            ResourceDictionary typographyDict = LoadDictionary(PackBase + "Themes/Typography/Typography.xaml");
            ResourceDictionary genericDict = LoadDictionary(PackBase + "Themes/Generic.xaml");
            ResourceDictionary sharedDict = LoadDictionary(PackBase + "Themes/Shared.xaml");

            // Remove any pre-existing Fluence dictionaries the consumer might have merged
            // before calling Apply; we always own the slot order.
            for (int i = dictionaries.Count - 1; i >= 0; i--)
            {
                if (IsFluenceSlot(dictionaries[i]))
                {
                    dictionaries.RemoveAt(i);
                }
            }

            // Insert into fixed slots instead of appending. Tests assert this shape because
            // theme swaps depend on replacing slot 0 while preserving accent, brushes,
            // typography, control-template, and shared dictionaries.
            dictionaries.Insert(SlotColors, themeDict);
            dictionaries.Insert(SlotAccent, accentDict);
            dictionaries.Insert(SlotBrushes, brushesDict);
            dictionaries.Insert(SlotTypography, typographyDict);
            dictionaries.Insert(SlotGeneric, genericDict);
            dictionaries.Insert(SlotShared, sharedDict);
            PromoteThemeColors(resolvedTheme);
            EnsureAcrylicNoiseBrush();
            _isInitialized = true;
        }

        /// <summary>
        /// Returns <c>true</c> when a candidate dictionary is one of Fluence's slot dictionaries,
        /// identified by either a Fluence-controlled <c>Source</c> URI or a presence sentinel.
        /// Used to defensively detect consumers that pre-merged the library's dictionaries.
        /// </summary>
        private static bool IsFluenceSlot(ResourceDictionary dictionary)
        {
            Uri? source = dictionary.Source;
            if (source is null)
            {
                return false;
            }
            // Lowercase the URI once and use simple Contains to satisfy CA2249 across both
            // net472 (no StringComparison-aware Contains overload) and net10.
            string s = source.OriginalString.ToLowerInvariant();
            bool isFluencePath = s.Contains("themes/colors/theme.")
                || s.Contains("themes/accent/accent.xaml")
                || s.Contains("themes/brushes/brushes.xaml")
                || s.Contains("themes/typography/typography.xaml")
                || s.Contains("themes/generic.xaml")
                || s.Contains("themes/shared.xaml");
            return isFluencePath && s.Contains("fluence.wpf;component");
        }

        private static void SwapThemeColors(ApplicationTheme resolvedTheme)
        {
            if (Application.Current is null)
            {
                return;
            }
            Collection<ResourceDictionary> dictionaries = Application.Current.Resources.MergedDictionaries;
            if (SlotColors < dictionaries.Count)
            {
                dictionaries[SlotColors] = LoadDictionary(GetThemeColorUri(resolvedTheme));
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
            if (Application.Current is null)
            {
                return;
            }
            ResourceDictionary resources = Application.Current.Resources;
            ResourceDictionary themeDict = resources.MergedDictionaries[SlotColors];
            if (resolvedTheme != ApplicationTheme.HighContrast)
            {
                if (_promotedHighContrastBrushKeys is not null)
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
            else
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
            _ = Application.Current?.Resources["AcrylicNoiseBrush"] ??= AcrylicNoiseHelper.GetNoiseBrush();
        }

        private static void OnChanged(ApplicationTheme resolvedTheme)
        {
            if (Changed is not null)
            {
                Color accent = ApplicationAccentColorManager.SystemAccentColor;
                Changed(null, new ThemeChangedEventArgs(resolvedTheme, accent));
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

        // The assembly component name is used in pack URIs to load resource dictionaries.
        private const string AssemblyComponent = "Fluence.Wpf;component";
        private const string PackBase = "pack://application:,,,/" + AssemblyComponent + "/";

        /*
         * Stable merge order in Application.Current.Resources.MergedDictionaries:
         *   [0] Theme Colors   - Theme.{Light|Dark|HighContrast}.xaml  (SWAPPED on theme change)
         *   [1] Accent         - Accent.xaml                           (loaded once, keys updated in-place)
         *   [2] Brushes        - Brushes.xaml                          (reloaded on non-HC theme swaps)
         *   [3] Typography     - Typography.xaml                       (loaded once, never replaced)
         *   [4] Generic        - Generic.xaml                          (loaded once, never replaced)
         *   [5] Shared         - Shared.xaml                           (loaded once, never replaced)
         *
         * For HighContrast, the theme dict at [0] contains both Color keys (static fallbacks)
         * and Brush keys (with live SystemColor DynamicResource bindings). These brush keys
         * override the equivalent keys from Brushes.xaml at [2] because we place them
         * directly into Application.Resources AFTER merging, ensuring correct precedence.
         *
         * Slot [5] (Shared.xaml) holds theme-independent Color tokens that are identical
         * across Light, Dark, and HighContrast (canonical Windows close-button reds, the
         * SmokeFillColorDefault dialog overlay, SurfaceStrokeColorDefault). It is loaded
         * once and never replaced - the per-theme dictionaries at slot [0] no longer
         * carry these keys, so PromoteThemeColors does not iterate over them (and it
         * does not need to: shared values do not change with the theme).
         */
        private const int SlotColors = 0;
        private const int SlotAccent = 1;
        private const int SlotBrushes = 2;
        private const int SlotTypography = 3;
        private const int SlotGeneric = 4;
        private const int SlotShared = 5;

        // Flags to prevent re-entrant calls to Apply() and to track whether the initial load has completed.
        private static bool _isInitialized;
        private static bool _isApplying;
        private static System.Collections.Generic.List<object>? _promotedHighContrastBrushKeys;
    }

    internal static class TabKeyboardNavigation
    {
        private static bool _registered;

        internal static void EnsureRegistered()
        {
            if (_registered)
            {
                return;
            }

            EventManager.RegisterClassHandler(
                typeof(System.Windows.Controls.TabItem),
                UIElement.PreviewKeyDownEvent,
                new System.Windows.Input.KeyEventHandler(OnTabItemPreviewKeyDown));
            _registered = true;
        }

        private static void OnTabItemPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Tab)
            {
                return;
            }

            System.Windows.Input.ModifierKeys modifiers = System.Windows.Input.Keyboard.Modifiers;
            if ((modifiers & ~System.Windows.Input.ModifierKeys.Shift) != 0)
            {
                return;
            }

            if (sender is not System.Windows.Controls.TabItem tabItem)
            {
                return;
            }

            System.Windows.Controls.ItemsControl? owner =
                System.Windows.Controls.ItemsControl.ItemsControlFromItemContainer(tabItem);
            if (owner is not System.Windows.Controls.TabControl tabControl)
            {
                return;
            }

            int currentIndex = tabControl.ItemContainerGenerator.IndexFromContainer(tabItem);
            if (currentIndex < 0)
            {
                return;
            }

            int direction = (modifiers & System.Windows.Input.ModifierKeys.Shift) == System.Windows.Input.ModifierKeys.Shift ? -1 : 1;
            int nextIndex = currentIndex + direction;
            if (nextIndex < 0 || nextIndex >= tabControl.Items.Count)
            {
                return;
            }

            System.Windows.Controls.TabItem? nextTabItem =
                tabControl.ItemContainerGenerator.ContainerFromIndex(nextIndex) as System.Windows.Controls.TabItem;
            nextTabItem ??= tabControl.Items[nextIndex] as System.Windows.Controls.TabItem;
            if (nextTabItem is null)
            {
                return;
            }

            object item = tabControl.ItemContainerGenerator.ItemFromContainer(nextTabItem);
            tabControl.SelectedItem = item != DependencyProperty.UnsetValue ? item : nextTabItem;
            _ = nextTabItem.Focus();
            _ = System.Windows.Input.Keyboard.Focus(nextTabItem);
            e.Handled = true;
        }
    }
}
