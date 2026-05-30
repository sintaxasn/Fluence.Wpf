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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace Fluence.Wpf.Theming
{
    /// <summary>
    /// The single-pipeline theme engine that resolves theme and accent intent into a computed
    /// <see cref="ResourceDictionary"/> and publishes it into application resources.
    /// Facades (<c>ApplicationThemeManager</c>, <c>ApplicationAccentColorManager</c>) will
    /// delegate to this engine once Stage 2 wiring is complete; for Stage 1 it compiles and
    /// is exercised by unit tests without yet driving application resources via the
    /// public <c>Apply</c> facades.
    /// </summary>
    internal static class FluenceThemeEngine
    {
        private const string PackBase = "pack://application:,,,/Fluence.Wpf;component/";
        private static AccentIntent _intent = AccentIntent.System;
        private static bool _initialized;

        /// <summary>Gets the most recently resolved <see cref="AccentPalette"/>.</summary>
        internal static AccentPalette CurrentPalette { get; private set; }

        /// <summary>Gets the most recently resolved concrete theme (Light, Dark, or HighContrast).</summary>
        internal static ApplicationTheme ResolvedTheme { get; private set; } = ApplicationTheme.Light;

        /// <summary>Gets the title-bar colors computed during the most recent <see cref="Apply"/> call.
        /// Populated by <see cref="ColorMap.Build"/> so the computation lives in a single place.</summary>
        internal static (Color active, Color inactive, Color border) CurrentTitleBarColors { get; private set; }

        /// <summary>
        /// Raised after the computed dictionary has been published into application resources.
        /// Facade classes raise their own public events by subscribing here.
        /// </summary>
        internal static event EventHandler<EventArgs>? Published;

        /// <summary>Sets the accent intent that the next <see cref="Apply"/> call will use.</summary>
        internal static void SetAccentIntent(AccentIntent intent)
        {
            _intent = intent;
        }

        /// <summary>
        /// Resolves the theme and accent, builds the computed dictionary, and publishes it into
        /// application resources.
        /// </summary>
        internal static void Apply(ApplicationTheme request)
        {
            ApplicationTheme theme = ThemeResolver.Resolve(request);
            AccentPalette palette = AccentResolver.Resolve(_intent);
            ResolvedTheme = theme;
            CurrentPalette = palette;

            ResourceDictionary dict = BuildComputedDictionary(theme, palette);
            Publish(dict);
            Published?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Builds the single computed dictionary published at slot [0] entirely in C#. Base
        /// Color tokens are read (Color entries only) from the per-theme and shared XAML via
        /// <see cref="BaseColorTables"/>; accent-derived Colors are computed by
        /// <see cref="ColorMap"/>; <see cref="BrushFactory"/> produces the solid brush twin for
        /// every Color token; and <see cref="SpecialBrushes"/> adds the non-twin brushes
        /// (elevation gradients, High-Contrast SystemColors brushes, ScrollBar track, accent
        /// overrides) plus the shared layout/shadow/focus tokens. No brush XAML is merged.
        /// </summary>
        private static ResourceDictionary BuildComputedDictionary(ApplicationTheme theme, AccentPalette palette)
        {
            Dictionary<string, Color> colors = ColorMap.Build(theme, palette);
            CurrentTitleBarColors = (colors["TitleBarActiveColor"], colors["TitleBarInactiveColor"], colors["WindowBorderColor"]);
            ResourceDictionary computed = BrushFactory.Build(colors);
            SpecialBrushes.Add(computed, colors, theme);
            computed["AcrylicNoiseBrush"] = AcrylicNoiseHelper.GetNoiseBrush(); // preserve existing token
            return computed;
        }

        private static void Publish(ResourceDictionary computed)
        {
            if (Application.Current is null) { return; }
            Collection<ResourceDictionary> dicts = Application.Current.Resources.MergedDictionaries;
            if (!_initialized)
            {
                // Slot model: [0] computed, [1] Typography, [2] Generic
                RemoveFluenceDictionaries(dicts);
                dicts.Insert(0, computed);
                dicts.Add(Load("Themes/Typography/Typography.xaml"));
                dicts.Add(Load("Themes/Generic.xaml"));
                _initialized = true;
            }
            else
            {
                dicts[0] = computed; // replace -> DynamicResource consumers re-resolve
            }
        }

        private static ResourceDictionary Load(string rel)
        {
            return new() { Source = new Uri(PackBase + rel, UriKind.Absolute) };
        }

        private static void RemoveFluenceDictionaries(Collection<ResourceDictionary> dicts)
        {
            for (int i = dicts.Count - 1; i >= 0; i--)
            {
                string s = dicts[i].Source?.OriginalString.ToLowerInvariant() ?? string.Empty;
                if (s.Contains("fluence.wpf;component") && s.Contains("themes/"))
                {
                    dicts.RemoveAt(i);
                }
            }
        }

        /// <summary>Resets engine state for test isolation.</summary>
        internal static void ResetForTesting()
        {
            _initialized = false;
            _intent = AccentIntent.System;
            ResolvedTheme = ApplicationTheme.Light;
            CurrentPalette = default;
            CurrentTitleBarColors = default;
        }
    }
}
