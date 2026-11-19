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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Fluence.Wpf.Theming;
using Xunit;

namespace Fluence.Wpf.Tests.Theming
{
    public sealed class AcrylicRecipePublicationTests : IAsyncLifetime
    {
        private const string AccentDefaultRecipeKey = "AcrylicInAppAccentDefaultRecipe";
        private const string AccentBaseRecipeKey = "AcrylicInAppAccentBaseRecipe";

        private static readonly string[] RecipeKeys =
        [
            "AcrylicInAppDefaultRecipe",
            "AcrylicInAppBaseRecipe",
            AccentDefaultRecipeKey,
            AccentBaseRecipeKey,
        ];

        [Theory]
        [InlineData(ApplicationTheme.Light, "AcrylicInAppDefaultRecipe", 0xFC, 0xFC, 0xFC, 0, 0xF9, 0xF9, 0xF9, 0.85)]
        [InlineData(ApplicationTheme.Light, "AcrylicInAppBaseRecipe", 0xF3, 0xF3, 0xF3, 0, 0xEE, 0xEE, 0xEE, 0.9)]
        [InlineData(ApplicationTheme.Dark, "AcrylicInAppDefaultRecipe", 0x2C, 0x2C, 0x2C, 0.15, 0x2C, 0x2C, 0x2C, 0.96)]
        [InlineData(ApplicationTheme.Dark, "AcrylicInAppBaseRecipe", 0x20, 0x20, 0x20, 0.5, 0x1C, 0x1C, 0x1C, 0.96)]
        public Task Apply_PublishesFrozenInAppRecipeWithPinnedValuesAsync(
            ApplicationTheme theme, string key, byte tintR, byte tintG, byte tintB, double opacity,
            byte fallbackR, byte fallbackG, byte fallbackB, double luminosity)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                ApplicationThemeManager.Apply(theme, WindowBackdropType.None);
                ResourceDictionary slotZero = Application.Current.Resources.MergedDictionaries[0];
                AcrylicMaterialRecipe recipe = Assert.IsType<AcrylicMaterialRecipe>(slotZero[key]);
                Assert.True(recipe.IsFrozen);
                Assert.Equal(Color.FromRgb(tintR, tintG, tintB), recipe.TintColor);
                Assert.Equal(opacity, recipe.TintOpacity);
                Assert.Equal(Color.FromRgb(fallbackR, fallbackG, fallbackB), recipe.FallbackColor);
                Assert.Equal(luminosity, recipe.TintLuminosityOpacity);
                Assert.False(recipe.AlwaysUseFallback);

                bool dark = theme is ApplicationTheme.Dark;
                bool isDefault = string.Equals(key, "AcrylicInAppDefaultRecipe", StringComparison.Ordinal);
                string darkRampKey = isDefault ? "SystemAccentColorDark1" : "SystemAccentColorDark2";
                _ = AssertAccentRecipe(
                    slotZero,
                    isDefault ? AccentDefaultRecipeKey : AccentBaseRecipeKey,
                    dark ? darkRampKey : "SystemAccentColorLight3",
                    dark ? 0.8d : 0.9d);
            });
        }

        [Fact]
        public Task Apply_ChangedThemeReplacesOnlyComputedSlot_AndRepeatIsNoOpAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = Application.Current;
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                Collection<ResourceDictionary> dictionaries = app.Resources.MergedDictionaries;
                ResourceDictionary light = dictionaries[0];
                ResourceDictionary typography = dictionaries[1];
                ResourceDictionary generic = dictionaries[2];
                ResourceDictionary foreignA = new() { ["ForeignA"] = 1 };
                ResourceDictionary foreignB = new() { ["ForeignB"] = 2 };
                dictionaries.Add(foreignA);
                dictionaries.Add(foreignB);

                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                Assert.Same(light, dictionaries[0]);

                ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                Assert.NotSame(light, dictionaries[0]);
                Assert.Same(typography, dictionaries[1]);
                Assert.Same(generic, dictionaries[2]);
                Assert.Same(foreignA, dictionaries[3]);
                Assert.Same(foreignB, dictionaries[4]);
                Assert.NotSame(light["AcrylicInAppDefaultRecipe"], dictionaries[0]["AcrylicInAppDefaultRecipe"]);
                Assert.NotSame(dictionaries[0]["AcrylicInAppDefaultRecipe"], dictionaries[0]["AcrylicInAppBaseRecipe"]);
                ResourceDictionary dark = dictionaries[0];
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                Assert.Same(dark, dictionaries[0]);
            });
        }

        [Fact]
        public Task Apply_HighContrastRecipesUseOpaqueLiveWindowColorAndFallbackAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, WindowBackdropType.None);
                ResourceDictionary computed = Application.Current.Resources.MergedDictionaries[0];
                Color window = SystemColors.WindowColor;
                Color opaqueWindow = Color.FromRgb(window.R, window.G, window.B);
                foreach (string key in RecipeKeys)
                {
                    AcrylicMaterialRecipe recipe = Assert.IsType<AcrylicMaterialRecipe>(computed[key]);
                    Assert.True(recipe.IsFrozen);
                    Assert.Equal(opaqueWindow, recipe.TintColor);
                    Assert.Equal(opaqueWindow, recipe.FallbackColor);
                    Assert.Equal(0d, recipe.TintOpacity);
                    Assert.Null(recipe.TintLuminosityOpacity);
                    Assert.True(recipe.AlwaysUseFallback);
                }
            });
        }

        [Theory]
        [InlineData(ApplicationTheme.Light, 0xF9, 0xEE)]
        [InlineData(ApplicationTheme.Dark, 0x2C, 0x1C)]
        public Task Apply_PreservesLegacySolidAcrylicBrushesAndNoiseAsync(ApplicationTheme theme, byte expectedDefault, byte expectedBase)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                ApplicationThemeManager.Apply(theme, WindowBackdropType.None);
                ResourceDictionary computed = Application.Current.Resources.MergedDictionaries[0];
                SolidColorBrush defaultBrush = Assert.IsType<SolidColorBrush>(computed["AcrylicBackgroundFillColorDefaultBrush"]);
                SolidColorBrush baseBrush = Assert.IsType<SolidColorBrush>(computed["AcrylicBackgroundFillColorBaseBrush"]);
                Assert.True(defaultBrush.IsFrozen);
                Assert.True(baseBrush.IsFrozen);
                Assert.Equal(Color.FromRgb(expectedDefault, expectedDefault, expectedDefault), defaultBrush.Color);
                Assert.Equal(Color.FromRgb(expectedBase, expectedBase, expectedBase), baseBrush.Color);

                SolidColorBrush accentDefault = Assert.IsType<SolidColorBrush>(computed["AccentAcrylicBackgroundFillColorDefaultBrush"]);
                SolidColorBrush accentBase = Assert.IsType<SolidColorBrush>(computed["AccentAcrylicBackgroundFillColorBaseBrush"]);
                Assert.True(accentDefault.IsFrozen);
                Assert.True(accentBase.IsFrozen);
                Assert.Equal(Assert.IsType<Color>(computed["AccentAcrylicBackgroundFillColorDefault"]), accentDefault.Color);
                Assert.Equal(Assert.IsType<Color>(computed["AccentAcrylicBackgroundFillColorBase"]), accentBase.Color);
                _ = Assert.IsType<ImageBrush>(computed["AcrylicNoiseBrush"]);
            });
        }

        [Theory]
        [InlineData(ApplicationTheme.Light)]
        [InlineData(ApplicationTheme.Dark)]
        public Task BuildStandalone_ContainsIndependentFrozenRecipeValuesAsync(ApplicationTheme theme)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                ResourceDictionary first = FluenceThemeEngine.BuildStandalone(theme);
                ResourceDictionary second = FluenceThemeEngine.BuildStandalone(theme);
                foreach (string key in RecipeKeys)
                {
                    AcrylicMaterialRecipe a = Assert.IsType<AcrylicMaterialRecipe>(first[key]);
                    AcrylicMaterialRecipe b = Assert.IsType<AcrylicMaterialRecipe>(second[key]);
                    Assert.True(a.IsFrozen);
                    Assert.True(b.IsFrozen);
                    Assert.NotSame(a, b);
                }
                Assert.NotSame(first["AcrylicInAppDefaultRecipe"], first["AcrylicInAppBaseRecipe"]);

                bool dark = theme is ApplicationTheme.Dark;
                double accentLuminosity = dark ? 0.8d : 0.9d;
                AcrylicMaterialRecipe accentDefault = AssertAccentRecipe(
                    first, AccentDefaultRecipeKey, dark ? "SystemAccentColorDark1" : "SystemAccentColorLight3", accentLuminosity);
                AcrylicMaterialRecipe accentBase = AssertAccentRecipe(
                    first, AccentBaseRecipeKey, dark ? "SystemAccentColorDark2" : "SystemAccentColorLight3", accentLuminosity);
                Assert.NotSame(accentDefault, accentBase);
                Assert.False(first.Contains("AcrylicNoiseBrush"));
            });
        }

        [Fact]
        public Task Apply_UpdatesRealizedHostDynamicRecipeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                AcrylicElement host = new() { Width = 80, Height = 50 };
                host.SetResourceReference(AcrylicElement.RecipeProperty, "AcrylicInAppDefaultRecipe");
                Window window = new() { Content = host, Width = 220, Height = 160, ShowInTaskbar = false };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    AcrylicMaterialRecipe light = Assert.IsType<AcrylicMaterialRecipe>(host.Recipe);
                    Assert.Equal(Color.FromRgb(0xFC, 0xFC, 0xFC), light.TintColor);

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    AcrylicMaterialRecipe dark = Assert.IsType<AcrylicMaterialRecipe>(host.Recipe);
                    Assert.NotSame(light, dark);
                    Assert.Equal(Color.FromRgb(0x2C, 0x2C, 0x2C), dark.TintColor);
                    Assert.True(host.IsUsingFallback);
                    AssertAccentRecipeUpdates(host);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                }
            });
        }

        private static void AssertAccentRecipeUpdates(AcrylicElement host)
        {
            Application app = Application.Current;
            host.SetResourceReference(AcrylicElement.RecipeProperty, AccentDefaultRecipeKey);
            WpfTestSta.DrainDispatcher(host.Dispatcher);

            ResourceDictionary computed = app.Resources.MergedDictionaries[0];
            AcrylicMaterialRecipe systemDefault = AssertAccentRecipe(computed, AccentDefaultRecipeKey, "SystemAccentColorDark1", 0.8d);
            Assert.Same(systemDefault, host.Recipe);

            Color systemAccent = Assert.IsType<Color>(computed["SystemAccentColor"]);
            Color darkAccent = systemAccent == Colors.Crimson ? Colors.RoyalBlue : Colors.Crimson;
            AcrylicMaterialRecipe customBase;
            try
            {
                ApplicationAccentColorManager.ApplyCustomAccent(Colors.DarkOrange, darkAccent, exact: true);
                WpfTestSta.DrainDispatcher(host.Dispatcher);

                computed = app.Resources.MergedDictionaries[0];
                AcrylicMaterialRecipe customDefault = AssertAccentRecipe(computed, AccentDefaultRecipeKey, "SystemAccentColorDark1", 0.8d);
                Assert.NotSame(systemDefault, customDefault);
                Assert.Same(customDefault, host.Recipe);

                host.SetResourceReference(AcrylicElement.RecipeProperty, AccentBaseRecipeKey);
                WpfTestSta.DrainDispatcher(host.Dispatcher);
                customBase = AssertAccentRecipe(computed, AccentBaseRecipeKey, "SystemAccentColorDark2", 0.8d);
                Assert.Same(customBase, host.Recipe);
            }
            finally
            {
                ApplicationAccentColorManager.ApplySystemAccent();
            }

            WpfTestSta.DrainDispatcher(host.Dispatcher);
            computed = app.Resources.MergedDictionaries[0];
            AcrylicMaterialRecipe systemBase = AssertAccentRecipe(computed, AccentBaseRecipeKey, "SystemAccentColorDark2", 0.8d);
            Assert.NotSame(customBase, systemBase);
            Assert.Same(systemBase, host.Recipe);
        }

        private static AcrylicMaterialRecipe AssertAccentRecipe(
            ResourceDictionary dictionary, string recipeKey, string rampKey, double luminosity)
        {
            Color shade = Assert.IsType<Color>(dictionary[rampKey]);
            Color opaqueShade = Color.FromRgb(shade.R, shade.G, shade.B);
            AcrylicMaterialRecipe recipe = Assert.IsType<AcrylicMaterialRecipe>(dictionary[recipeKey]);
            Assert.True(recipe.IsFrozen);
            Assert.Equal(opaqueShade, recipe.TintColor);
            Assert.Equal(0.8d, recipe.TintOpacity);
            Assert.Equal(luminosity, recipe.TintLuminosityOpacity);
            Assert.Equal(opaqueShade, recipe.FallbackColor);
            Assert.False(recipe.AlwaysUseFallback);
            return recipe;
        }

        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }
    }
}
