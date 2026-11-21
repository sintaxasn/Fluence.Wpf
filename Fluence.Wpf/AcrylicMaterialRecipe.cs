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

namespace Fluence.Wpf
{
    /// <summary>
    /// Stores the value inputs used to create an acrylic material.
    /// </summary>
    public sealed class AcrylicMaterialRecipe : Freezable
    {
        /// <summary>
        /// Identifies the <see cref="TintColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TintColorProperty = DependencyProperty.Register(
            name: nameof(TintColor),
            propertyType: typeof(Color),
            ownerType: typeof(AcrylicMaterialRecipe),
            typeMetadata: new PropertyMetadata(defaultValue: Color.FromArgb(204, 255, 255, 255)));

        /// <summary>
        /// Identifies the <see cref="TintOpacity"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TintOpacityProperty = DependencyProperty.Register(
            name: nameof(TintOpacity),
            propertyType: typeof(double),
            ownerType: typeof(AcrylicMaterialRecipe),
            typeMetadata: new PropertyMetadata(defaultValue: 1d, propertyChangedCallback: null, coerceValueCallback: CoerceOpacity),
            validateValueCallback: IsFinite);

        /// <summary>
        /// Identifies the <see cref="TintLuminosityOpacity"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TintLuminosityOpacityProperty = DependencyProperty.Register(
            name: nameof(TintLuminosityOpacity),
            propertyType: typeof(double?),
            ownerType: typeof(AcrylicMaterialRecipe),
            typeMetadata: new PropertyMetadata(defaultValue: null, propertyChangedCallback: null, coerceValueCallback: CoerceNullableOpacity),
            validateValueCallback: IsNullableFinite);

        /// <summary>
        /// Identifies the <see cref="FallbackColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FallbackColorProperty = DependencyProperty.Register(
            name: nameof(FallbackColor),
            propertyType: typeof(Color),
            ownerType: typeof(AcrylicMaterialRecipe),
            typeMetadata: new PropertyMetadata(defaultValue: Color.FromArgb(255, 249, 249, 249)));

        /// <summary>
        /// Identifies the <see cref="AlwaysUseFallback"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AlwaysUseFallbackProperty = DependencyProperty.Register(
            name: nameof(AlwaysUseFallback),
            propertyType: typeof(bool),
            ownerType: typeof(AcrylicMaterialRecipe),
            typeMetadata: new PropertyMetadata(defaultValue: false));

        /// <summary>
        /// Gets or sets the tint color used by the material.
        /// </summary>
        public Color TintColor
        {
            get => (Color)GetValue(dp: TintColorProperty);
            set => SetValue(dp: TintColorProperty, value: value);
        }

        /// <summary>
        /// Gets or sets tint opacity, clamped to the inclusive range from zero to one.
        /// </summary>
        public double TintOpacity
        {
            get => (double)GetValue(dp: TintOpacityProperty);
            set => SetValue(dp: TintOpacityProperty, value: value);
        }

        /// <summary>
        /// Gets or sets explicit luminosity opacity, or <see langword="null"/> for automatic luminosity.
        /// </summary>
        public double? TintLuminosityOpacity
        {
            get => (double?)GetValue(dp: TintLuminosityOpacityProperty);
            set => SetValue(dp: TintLuminosityOpacityProperty, value: value);
        }

        /// <summary>
        /// Gets or sets the fallback color. The supplied alpha channel is preserved.
        /// </summary>
        public Color FallbackColor
        {
            get => (Color)GetValue(dp: FallbackColorProperty);
            set => SetValue(dp: FallbackColorProperty, value: value);
        }

        /// <summary>
        /// Gets or sets whether the material should always use its fallback color.
        /// </summary>
        public bool AlwaysUseFallback
        {
            get => (bool)GetValue(dp: AlwaysUseFallbackProperty);
            set => SetValue(dp: AlwaysUseFallbackProperty, value: value);
        }

        /// <inheritdoc />
        protected override Freezable CreateInstanceCore()
        {
            return new AcrylicMaterialRecipe();
        }

        private static bool IsFinite(object value)
        {
            double number = (double)value;
            return !double.IsNaN(number) && !double.IsInfinity(number);
        }

        private static bool IsNullableFinite(object value)
        {
            if (value is null)
            {
                return true;
            }

            double number = (double)value;
            return !double.IsNaN(number) && !double.IsInfinity(number);
        }

        private static object CoerceOpacity(DependencyObject _, object baseValue)
        {
            double number = (double)baseValue;
            return Math.Max(0d, Math.Min(1d, number));
        }

        private static object? CoerceNullableOpacity(DependencyObject _, object baseValue)
        {
            if (baseValue is null)
            {
                return null;
            }

            double number = (double)baseValue;
            return Math.Max(0d, Math.Min(1d, number));
        }
    }
}
