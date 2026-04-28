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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf.Controls;
using FluentContextMenu = Fluence.Wpf.Controls.ContextMenu;
using FluentMenuItem = Fluence.Wpf.Controls.MenuItem;
using FluentToolTip = Fluence.Wpf.Controls.ToolTip;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public class TextRenderingPolicyTests
    {
        [TestMethod]
        public void TextBearingControlStyles_UseDisplayClearTypeFixedHinting()
        {
            WpfTestSta.Invoke(() =>
            {
                var application = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                application.Resources.Clear();
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);

                AssertStyleTextRenderingPolicy(application, typeof(FluentContextMenu), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentMenuItem), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentToolTip), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(RatingControl), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(PersonPicture), TextFormattingMode.Display);
            });
        }

        [TestMethod]
        public void TypographyTextBlockStyles_UseClearTypeFixedHinting()
        {
            WpfTestSta.Invoke(() =>
            {
                var application = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                application.Resources.Clear();
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);

                AssertTextBlockStyleRenderingPolicy(application, "CaptionTextBlockStyle", TextFormattingMode.Display);
                AssertTextBlockStyleRenderingPolicy(application, "BodyTextBlockStyle", TextFormattingMode.Display);
                AssertTextBlockStyleRenderingPolicy(application, "BodyStrongTextBlockStyle", TextFormattingMode.Display);
                AssertTextBlockStyleRenderingPolicy(application, "BodyLargeTextBlockStyle", TextFormattingMode.Display);
                AssertTextBlockStyleRenderingPolicy(application, "SubtitleTextBlockStyle", TextFormattingMode.Ideal);
                AssertTextBlockStyleRenderingPolicy(application, "TitleTextBlockStyle", TextFormattingMode.Ideal);
                AssertTextBlockStyleRenderingPolicy(application, "TitleLargeTextBlockStyle", TextFormattingMode.Ideal);
                AssertTextBlockStyleRenderingPolicy(application, "DisplayTextBlockStyle", TextFormattingMode.Ideal);
            });
        }

        [TestMethod]
        public void TextBlockExtensions_Typography_AppliesClearTypeRenderingPolicy()
        {
            WpfTestSta.Invoke(() =>
            {
                var body = new WpfTextBlock();
                TextBlockExtensions.SetTypography(body, FluentTypography.Body);

                Assert.AreEqual(TextFormattingMode.Display, TextOptions.GetTextFormattingMode(body));
                Assert.AreEqual(TextRenderingMode.ClearType, TextOptions.GetTextRenderingMode(body));
                Assert.AreEqual(TextHintingMode.Fixed, TextOptions.GetTextHintingMode(body));

                var title = new WpfTextBlock();
                TextBlockExtensions.SetTypography(title, FluentTypography.Title);

                Assert.AreEqual(TextFormattingMode.Ideal, TextOptions.GetTextFormattingMode(title));
                Assert.AreEqual(TextRenderingMode.ClearType, TextOptions.GetTextRenderingMode(title));
                Assert.AreEqual(TextHintingMode.Fixed, TextOptions.GetTextHintingMode(title));
            });
        }

        [TestMethod]
        public void TextBlockExtensions_TypographyNone_DoesNotMutateExistingTextPolicy()
        {
            WpfTestSta.Invoke(() =>
            {
                var textBlock = new WpfTextBlock();
                TextBlockExtensions.SetTypography(textBlock, FluentTypography.Body);

                textBlock.FontFamily = new FontFamily("Arial");
                textBlock.FontSize = 13;
                textBlock.FontWeight = FontWeights.Bold;
                textBlock.LineHeight = 17;
                textBlock.LineStackingStrategy = LineStackingStrategy.MaxHeight;
                TextOptions.SetTextFormattingMode(textBlock, TextFormattingMode.Ideal);
                TextOptions.SetTextRenderingMode(textBlock, TextRenderingMode.Grayscale);
                TextOptions.SetTextHintingMode(textBlock, TextHintingMode.Animated);

                TextBlockExtensions.SetTypography(textBlock, FluentTypography.None);

                Assert.AreEqual(new FontFamily("Arial"), textBlock.FontFamily);
                Assert.AreEqual(13d, textBlock.FontSize, 0.01d);
                Assert.AreEqual(FontWeights.Bold, textBlock.FontWeight);
                Assert.AreEqual(17d, textBlock.LineHeight, 0.01d);
                Assert.AreEqual(LineStackingStrategy.MaxHeight, textBlock.LineStackingStrategy);
                Assert.AreEqual(TextFormattingMode.Ideal, TextOptions.GetTextFormattingMode(textBlock));
                Assert.AreEqual(TextRenderingMode.Grayscale, TextOptions.GetTextRenderingMode(textBlock));
                Assert.AreEqual(TextHintingMode.Animated, TextOptions.GetTextHintingMode(textBlock));
            });
        }

        [TestMethod]
        public void DemoAppSources_OverrideWindowTextRenderingMetadata()
        {
            AssertDemoAppTextMetadataOverrides(Path.Combine("Fluence.Wpf.Demo", "App.xaml.cs"));
            AssertDemoAppTextMetadataOverrides(Path.Combine("Fluence.Wpf.Demo.Mvvm", "App.xaml.cs"));
        }

        private static void AssertStyleTextRenderingPolicy(Application application, Type targetType, TextFormattingMode expectedFormattingMode)
        {
            var style = application.TryFindResource(targetType) as Style;
            Assert.IsNotNull(style, "Style should resolve for " + targetType.Name + ".");
            AssertStyleSetter(style, TextOptions.TextFormattingModeProperty, expectedFormattingMode, targetType.Name);
            AssertStyleSetter(style, TextOptions.TextRenderingModeProperty, TextRenderingMode.ClearType, targetType.Name);
            AssertStyleSetter(style, TextOptions.TextHintingModeProperty, TextHintingMode.Fixed, targetType.Name);
        }

        private static void AssertTextBlockStyleRenderingPolicy(Application application, string styleKey, TextFormattingMode expectedFormattingMode)
        {
            var style = application.TryFindResource(styleKey) as Style;
            Assert.IsNotNull(style, styleKey + " should resolve.");
            // Named styles may inherit TextOptions via BasedOn; walk the full chain.
            AssertStyleSetterOrBasedOn(style, TextOptions.TextFormattingModeProperty, expectedFormattingMode, styleKey);
            AssertStyleSetterOrBasedOn(style, TextOptions.TextRenderingModeProperty, TextRenderingMode.ClearType, styleKey);
            AssertStyleSetterOrBasedOn(style, TextOptions.TextHintingModeProperty, TextHintingMode.Fixed, styleKey);
        }

        private static void AssertStyleSetter(Style style, DependencyProperty property, object expectedValue, string description)
        {
            foreach (var setterBase in style.Setters)
            {
                var setter = setterBase as Setter;
                if (setter != null && setter.Property == property)
                {
                    Assert.AreEqual(expectedValue, setter.Value, description + " should set " + property.Name + ".");
                    return;
                }
            }

            Assert.Fail(description + " should set " + property.Name + ".");
        }

        /// <summary>
        /// Walks the <see cref="Style.BasedOn"/> chain looking for a setter that matches
        /// <paramref name="property"/> with <paramref name="expectedValue"/>.  A style that
        /// inherits the correct value through <c>BasedOn</c> satisfies the assertion.
        /// </summary>
        private static void AssertStyleSetterOrBasedOn(Style style, DependencyProperty property, object expectedValue, string description)
        {
            var current = style;
            while (current != null)
            {
                foreach (var setterBase in current.Setters)
                {
                    var setter = setterBase as Setter;
                    if (setter != null && setter.Property == property)
                    {
                        Assert.AreEqual(expectedValue, setter.Value, description + " should set " + property.Name + ".");
                        return;
                    }
                }

                current = current.BasedOn;
            }

            Assert.Fail(description + " should set " + property.Name + " (searched full BasedOn chain).");
        }

        private static void AssertDemoAppTextMetadataOverrides(string relativePath)
        {
            var source = File.ReadAllText(Path.Combine(FindRepoRoot(), relativePath));

            StringAssert.Contains(source, "TextOptions.TextFormattingModeProperty.OverrideMetadata(");
            StringAssert.Contains(source, "new FrameworkPropertyMetadata(TextFormattingMode.Display, textOptionsMetadata)");
            StringAssert.Contains(source, "TextOptions.TextRenderingModeProperty.OverrideMetadata(");
            StringAssert.Contains(source, "new FrameworkPropertyMetadata(TextRenderingMode.ClearType, textOptionsMetadata)");
            StringAssert.Contains(source, "TextOptions.TextHintingModeProperty.OverrideMetadata(");
            StringAssert.Contains(source, "new FrameworkPropertyMetadata(TextHintingMode.Fixed, textOptionsMetadata)");
            StringAssert.Contains(source, "FrameworkPropertyMetadataOptions.Inherits");
        }

        private static string FindRepoRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Fluence.Wpf.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException(
                "Could not locate Fluence.Wpf.sln ancestor directory from " + AppContext.BaseDirectory);
        }
    }
}
