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
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace Fluence.Wpf.Helpers
{
    [StructLayout(LayoutKind.Auto)]
    internal readonly struct AcrylicRecipeSnapshot : IEquatable<AcrylicRecipeSnapshot>
    {
        internal AcrylicRecipeSnapshot(Color tintColor, double tintOpacity,
            double? tintLuminosityOpacity, Color fallbackColor, bool alwaysUseFallback)
        {
            TintColor = tintColor;
            TintOpacity = tintOpacity;
            TintLuminosityOpacity = tintLuminosityOpacity;
            FallbackColor = fallbackColor;
            AlwaysUseFallback = alwaysUseFallback;
        }

        internal Color TintColor { get; }
        internal double TintOpacity { get; }
        internal double? TintLuminosityOpacity { get; }
        internal Color FallbackColor { get; }
        internal bool AlwaysUseFallback { get; }

        internal static AcrylicRecipeSnapshot Create(AcrylicMaterialRecipe recipe)
        {
            ArgumentNullException.ThrowIfNull(recipe);

            return new AcrylicRecipeSnapshot(recipe.TintColor, recipe.TintOpacity,
                recipe.TintLuminosityOpacity, recipe.FallbackColor, recipe.AlwaysUseFallback);
        }

        public bool Equals(AcrylicRecipeSnapshot other)
        {
            return TintColor == other.TintColor
                && TintOpacity.Equals(other.TintOpacity)
                && TintLuminosityOpacity.Equals(other.TintLuminosityOpacity)
                && FallbackColor == other.FallbackColor
                && AlwaysUseFallback == other.AlwaysUseFallback;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is AcrylicRecipeSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = TintColor.GetHashCode();
                hash = (hash * 397) ^ TintOpacity.GetHashCode();
                hash = (hash * 397) ^ TintLuminosityOpacity.GetHashCode();
                hash = (hash * 397) ^ FallbackColor.GetHashCode();
                return (hash * 397) ^ AlwaysUseFallback.GetHashCode();
            }
        }
    }
}
