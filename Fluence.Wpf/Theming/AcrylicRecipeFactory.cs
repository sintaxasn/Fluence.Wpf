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

using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Fluence.Wpf.Theming
{
    /// <summary>
    /// Builds value-only in-app acrylic recipes from the resolved theme and color map.
    /// </summary>
    internal static class AcrylicRecipeFactory
    {
        internal const string DefaultKey = "AcrylicInAppDefaultRecipe";
        internal const string BaseKey = "AcrylicInAppBaseRecipe";

        /// <summary>
        /// Adds the two frozen in-app recipes to a computed theme dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary receiving the recipes.</param>
        /// <param name="colors">The already resolved and fingerprinted color map.</param>
        /// <param name="theme">The resolved theme.</param>
        internal static void Add(ResourceDictionary dictionary, IReadOnlyDictionary<string, Color> colors, ApplicationTheme theme)
        {
            if (theme is ApplicationTheme.HighContrast)
            {
                // ColorMap maps this token to live SystemColors.WindowColor for High Contrast.
                // Reusing the captured map avoids a second, unfingerprinted OS color read.
                Color window = Opaque(colors["AccentAcrylicBackgroundFillColorDefault"]);
                dictionary[DefaultKey] = Create(window, 0d, window, luminosity: null, alwaysFallback: true);
                dictionary[BaseKey] = Create(window, 0d, window, luminosity: null, alwaysFallback: true);
                return;
            }

            bool dark = theme is ApplicationTheme.Dark;
            dictionary[DefaultKey] = Create(
                dark ? Color.FromRgb(0x2C, 0x2C, 0x2C) : Color.FromRgb(0xFC, 0xFC, 0xFC),
                dark ? 0.15d : 0d,
                Opaque(colors["AcrylicBackgroundFillColorDefault"]),
                dark ? 0.96d : 0.85d,
                alwaysFallback: false);
            dictionary[BaseKey] = Create(
                dark ? Color.FromRgb(0x20, 0x20, 0x20) : Color.FromRgb(0xF3, 0xF3, 0xF3),
                dark ? 0.5d : 0d,
                Opaque(colors["AcrylicBackgroundFillColorBase"]),
                dark ? 0.96d : 0.9d,
                alwaysFallback: false);
        }

        private static AcrylicMaterialRecipe Create(Color tint, double opacity, Color fallback, double? luminosity, bool alwaysFallback)
        {
            AcrylicMaterialRecipe recipe = new()
            {
                TintColor = tint,
                TintOpacity = opacity,
                FallbackColor = fallback,
                TintLuminosityOpacity = luminosity,
                AlwaysUseFallback = alwaysFallback,
            };
            recipe.Freeze();
            return recipe;
        }

        private static Color Opaque(Color color)
        {
            return Color.FromRgb(color.R, color.G, color.B);
        }
    }
}
