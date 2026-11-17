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
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDER AND CONTRIBUTORS "AS IS"
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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Theming
{
    public sealed class AcrylicMaterialRecipeTests
    {
        [Fact]
        public void DefaultsMatchContract()
        {
            AcrylicMaterialRecipe recipe = new();
            Assert.Equal(Color.FromArgb(204, 255, 255, 255), recipe.TintColor);
            Assert.Equal(1d, recipe.TintOpacity);
            Assert.Null(recipe.TintLuminosityOpacity);
            Assert.Equal(Color.FromArgb(255, 249, 249, 249), recipe.FallbackColor);
            Assert.False(recipe.AlwaysUseFallback);
        }

        [Theory]
        [InlineData(-1d, 0d)]
        [InlineData(0d, 0d)]
        [InlineData(0.4d, 0.4d)]
        [InlineData(1d, 1d)]
        [InlineData(2d, 1d)]
        public void OpacityCoercesFiniteRange(double input, double expected)
        {
            AcrylicMaterialRecipe recipe = new() { TintOpacity = input };
            Assert.Equal(expected, recipe.TintOpacity);
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void RejectsNaNInfinity(double input)
        {
            AcrylicMaterialRecipe recipe = new();
            _ = Assert.Throws<ArgumentException>(() => recipe.TintOpacity = input);
            _ = Assert.Throws<ArgumentException>(() => recipe.TintLuminosityOpacity = input);
        }

        [Theory]
        [InlineData(-0.25d, 0d)]
        [InlineData(1.25d, 1d)]
        public void NullableLuminosityCoercesFiniteRange(double input, double expected)
        {
            AcrylicMaterialRecipe recipe = new() { TintLuminosityOpacity = input };
            Assert.Equal(expected, recipe.TintLuminosityOpacity);
        }

        [Fact]
        public void CloneAndFreezeContainOnlyValues()
        {
            AcrylicMaterialRecipe recipe = new()
            {
                TintColor = Color.FromArgb(91, 12, 34, 56),
                TintOpacity = 0.5,
                TintLuminosityOpacity = 0.75,
                FallbackColor = Color.FromArgb(37, 90, 80, 70),
                AlwaysUseFallback = true,
            };
            AcrylicMaterialRecipe clone = (AcrylicMaterialRecipe)recipe.Clone();
            Assert.NotSame(recipe, clone);
            Assert.Equal(recipe.TintColor, clone.TintColor);
            Assert.Equal(recipe.TintOpacity, clone.TintOpacity);
            Assert.Equal(recipe.TintLuminosityOpacity, clone.TintLuminosityOpacity);
            Assert.Equal(recipe.FallbackColor, clone.FallbackColor);
            Assert.Equal(recipe.AlwaysUseFallback, clone.AlwaysUseFallback);
            Assert.True(recipe.CanFreeze);
            recipe.Freeze();
            Assert.True(recipe.IsFrozen);
            Assert.Equal(Color.FromArgb(37, 90, 80, 70), recipe.FallbackColor);
        }

        [Fact]
        public void FallbackInputColorIsPreserved()
        {
            AcrylicMaterialRecipe recipe = new() { FallbackColor = Color.FromArgb(63, 11, 22, 33) };
            Assert.Equal(Color.FromArgb(63, 11, 22, 33), recipe.FallbackColor);
        }

        [Fact]
        public Task CompiledBamlPreservesNullableLuminosityValuesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                ResourceDictionary values = new()
                {
                    Source = new Uri(
                        "pack://application:,,,/Fluence.Wpf.Tests;component/Assets/Acrylic/AcrylicRecipeValues.xaml",
                        UriKind.Absolute),
                };

                AcrylicMaterialRecipe defaults = (AcrylicMaterialRecipe)values["Defaults"];
                AcrylicMaterialRecipe nullLuminosity = (AcrylicMaterialRecipe)values["NullLuminosity"];
                AcrylicMaterialRecipe explicitLuminosity = (AcrylicMaterialRecipe)values["ExplicitLuminosity"];
                AcrylicMaterialRecipe coercedOpacity = (AcrylicMaterialRecipe)values["CoercedOpacity"];

                Assert.Null(defaults.TintLuminosityOpacity);
                Assert.Null(nullLuminosity.TintLuminosityOpacity);
                Assert.Equal(0.96d, explicitLuminosity.TintLuminosityOpacity);
                Assert.Equal(0.7d, explicitLuminosity.TintOpacity);
                Assert.Equal(1d, coercedOpacity.TintLuminosityOpacity);
                Assert.Equal(1d, coercedOpacity.TintOpacity);
            });
        }
    }
}
