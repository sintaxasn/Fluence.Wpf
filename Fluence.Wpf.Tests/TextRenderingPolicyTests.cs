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
using FluentButton = Fluence.Wpf.Controls.Button;
using FluentCard = Fluence.Wpf.Controls.Card;
using FluentCheckBox = Fluence.Wpf.Controls.CheckBox;
using FluentComboBox = Fluence.Wpf.Controls.ComboBox;
using FluentContextMenu = Fluence.Wpf.Controls.ContextMenu;
using FluentDropDownButton = Fluence.Wpf.Controls.DropDownButton;
using FluentExpander = Fluence.Wpf.Controls.Expander;
using FluentHyperlinkButton = Fluence.Wpf.Controls.HyperlinkButton;
using FluentInfoBadge = Fluence.Wpf.Controls.InfoBadge;
using FluentInfoBar = Fluence.Wpf.Controls.InfoBar;
using FluentListBox = Fluence.Wpf.Controls.ListBox;
using FluentListBoxItem = Fluence.Wpf.Controls.ListBoxItem;
using FluentListView = Fluence.Wpf.Controls.ListView;
using FluentMenu = Fluence.Wpf.Controls.Menu;
using FluentMenuItem = Fluence.Wpf.Controls.MenuItem;
using FluentNavigationView = Fluence.Wpf.Controls.NavigationView;
using FluentNavigationViewItem = Fluence.Wpf.Controls.NavigationViewItem;
using FluentNavigationViewItemHeader = Fluence.Wpf.Controls.NavigationViewItemHeader;
using FluentNumberBox = Fluence.Wpf.Controls.NumberBox;
using FluentPasswordBox = Fluence.Wpf.Controls.PasswordBox;
using FluentRadioButton = Fluence.Wpf.Controls.RadioButton;
using FluentRepeatButton = Fluence.Wpf.Controls.RepeatButton;
using FluentSplitButton = Fluence.Wpf.Controls.SplitButton;
using FluentTabView = Fluence.Wpf.Controls.TabView;
using FluentTabViewItem = Fluence.Wpf.Controls.TabViewItem;
using FluentTextBlock = Fluence.Wpf.Controls.TextBlock;
using FluentTextBox = Fluence.Wpf.Controls.TextBox;
using FluentTitleBar = Fluence.Wpf.Controls.TitleBar;
using FluentToggleButton = Fluence.Wpf.Controls.ToggleButton;
using FluentToggleSwitch = Fluence.Wpf.Controls.ToggleSwitch;
using FluentToolTip = Fluence.Wpf.Controls.ToolTip;
using FluentTreeView = Fluence.Wpf.Controls.TreeView;
using FluentTreeViewItem = Fluence.Wpf.Controls.TreeViewItem;
using FluentWindow = Fluence.Wpf.Controls.FluenceWindow;
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
                Application? application = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                application?.Resources.Clear();
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);

                AssertStyleTextRenderingPolicy(application, typeof(FluentButton), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentHyperlinkButton), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentDropDownButton), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentSplitButton), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentRepeatButton), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentToggleButton), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentCheckBox), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentRadioButton), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentToggleSwitch), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentComboBox), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentNumberBox), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentTextBox), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentPasswordBox), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentTextBlock), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentCard), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentExpander), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentInfoBar), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentInfoBadge), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentListBox), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentListBoxItem), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentListView), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentNavigationView), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentNavigationViewItem), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentNavigationViewItemHeader), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentTabView), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentTabViewItem), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentTitleBar), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentTreeView), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentTreeViewItem), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentWindow), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentMenu), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentContextMenu), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentMenuItem), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(FluentToolTip), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(RatingControl), TextFormattingMode.Display);
                AssertStyleTextRenderingPolicy(application, typeof(PersonPicture), TextFormattingMode.Display);

                AssertTextBlockStyleRenderingPolicy(application, "ComboBoxItemStyle", TextFormattingMode.Display);
                AssertTextBlockStyleRenderingPolicy(application, "ListViewGroupItemStyle", TextFormattingMode.Display);
                AssertTextBlockStyleRenderingPolicy(application, "ListViewItemStyle", TextFormattingMode.Display);
                AssertTextBlockStyleRenderingPolicy(application, "TabControlStyle", TextFormattingMode.Display);
                AssertTextBlockStyleRenderingPolicy(application, "TabItemStyle", TextFormattingMode.Display);
            });
        }

        [TestMethod]
        public void TextBearingControlStyles_UseFluentFontFamily()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? application = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                application?.Resources.Clear();
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);

                AssertStyleFontFamilyPolicy(application, typeof(FluentButton));
                AssertStyleFontFamilyPolicy(application, typeof(FluentHyperlinkButton));
                AssertStyleFontFamilyPolicy(application, typeof(FluentDropDownButton));
                AssertStyleFontFamilyPolicy(application, typeof(FluentSplitButton));
                AssertStyleFontFamilyPolicy(application, typeof(FluentRepeatButton));
                AssertStyleFontFamilyPolicy(application, typeof(FluentToggleButton));
                AssertStyleFontFamilyPolicy(application, typeof(FluentCheckBox));
                AssertStyleFontFamilyPolicy(application, typeof(FluentRadioButton));
                AssertStyleFontFamilyPolicy(application, typeof(FluentToggleSwitch));
                AssertStyleFontFamilyPolicy(application, typeof(FluentComboBox));
                AssertStyleFontFamilyPolicy(application, typeof(FluentNumberBox));
                AssertStyleFontFamilyPolicy(application, typeof(FluentTextBox));
                AssertStyleFontFamilyPolicy(application, typeof(FluentPasswordBox));
                AssertStyleFontFamilyPolicy(application, typeof(FluentTextBlock));
                AssertStyleFontFamilyPolicy(application, typeof(FluentCard));
                AssertStyleFontFamilyPolicy(application, typeof(FluentExpander));
                AssertStyleFontFamilyPolicy(application, typeof(FluentInfoBar));
                AssertStyleFontFamilyPolicy(application, typeof(FluentInfoBadge));
                AssertStyleFontFamilyPolicy(application, typeof(FluentListBox));
                AssertStyleFontFamilyPolicy(application, typeof(FluentListBoxItem));
                AssertStyleFontFamilyPolicy(application, typeof(FluentListView));
                AssertStyleFontFamilyPolicy(application, typeof(FluentNavigationView));
                AssertStyleFontFamilyPolicy(application, typeof(FluentNavigationViewItem));
                AssertStyleFontFamilyPolicy(application, typeof(FluentNavigationViewItemHeader));
                AssertStyleFontFamilyPolicy(application, typeof(FluentTabView));
                AssertStyleFontFamilyPolicy(application, typeof(FluentTabViewItem));
                AssertStyleFontFamilyPolicy(application, typeof(FluentTitleBar));
                AssertStyleFontFamilyPolicy(application, typeof(FluentTreeView));
                AssertStyleFontFamilyPolicy(application, typeof(FluentTreeViewItem));
                AssertStyleFontFamilyPolicy(application, typeof(FluentWindow));
                AssertStyleFontFamilyPolicy(application, typeof(FluentMenu));
                AssertStyleFontFamilyPolicy(application, typeof(FluentContextMenu));
                AssertStyleFontFamilyPolicy(application, typeof(FluentMenuItem));
                AssertStyleFontFamilyPolicy(application, typeof(FluentToolTip));
                AssertStyleFontFamilyPolicy(application, typeof(RatingControl));
                AssertStyleFontFamilyPolicy(application, typeof(PersonPicture));

                AssertKeyedStyleFontFamilyPolicy(application, "ComboBoxItemStyle");
                AssertKeyedStyleFontFamilyPolicy(application, "ListViewGroupItemStyle");
                AssertKeyedStyleFontFamilyPolicy(application, "ListViewItemStyle");
                AssertKeyedStyleFontFamilyPolicy(application, "TabControlStyle");
                AssertKeyedStyleFontFamilyPolicy(application, "TabItemStyle");
            });
        }

        [TestMethod]
        public void TypographyTextBlockStyles_UseClearTypeFixedHinting()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? application = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                application?.Resources.Clear();
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
                Application? application = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.ResetForTesting();
                ApplicationAccentColorManager.ResetForTesting();
                application?.Resources.Clear();
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);

                WpfTextBlock body = new();
                TextBlockExtensions.SetTypography(body, FluentTypography.Body);

                Assert.AreEqual(TextFormattingMode.Display, TextOptions.GetTextFormattingMode(body));
                Assert.AreEqual(TextRenderingMode.ClearType, TextOptions.GetTextRenderingMode(body));
                Assert.AreEqual(TextHintingMode.Fixed, TextOptions.GetTextHintingMode(body));

                WpfTextBlock title = new();
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
                WpfTextBlock textBlock = new();
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

        private static void AssertStyleTextRenderingPolicy(Application? application, Type targetType, TextFormattingMode expectedFormattingMode)
        {
            Style? style = application?.TryFindResource(targetType) as Style;
            Assert.IsNotNull(style, "Style should resolve for " + targetType.Name + ".");
            AssertStyleSetterOrBasedOn(style, TextOptions.TextFormattingModeProperty, expectedFormattingMode, targetType.Name);
            AssertStyleSetterOrBasedOn(style, TextOptions.TextRenderingModeProperty, TextRenderingMode.ClearType, targetType.Name);
            AssertStyleSetterOrBasedOn(style, TextOptions.TextHintingModeProperty, TextHintingMode.Fixed, targetType.Name);
        }

        private static void AssertStyleFontFamilyPolicy(Application? application, Type targetType)
        {
            Style? style = application?.TryFindResource(targetType) as Style;
            Assert.IsNotNull(style, "Style should resolve for " + targetType.Name + ".");
            AssertStyleFluentFontFamilySetterOrBasedOn(style, targetType.Name);
        }

        private static void AssertKeyedStyleFontFamilyPolicy(Application? application, string styleKey)
        {
            Style? style = application?.TryFindResource(styleKey) as Style;
            Assert.IsNotNull(style, styleKey + " should resolve.");
            AssertStyleFluentFontFamilySetterOrBasedOn(style, styleKey);
        }

        private static void AssertTextBlockStyleRenderingPolicy(Application? application, string styleKey, TextFormattingMode expectedFormattingMode)
        {
            Style? style = application?.TryFindResource(styleKey) as Style;
            Assert.IsNotNull(style, styleKey + " should resolve.");
            // Named styles may inherit TextOptions via BasedOn; walk the full chain.
            AssertStyleSetterOrBasedOn(style, TextOptions.TextFormattingModeProperty, expectedFormattingMode, styleKey);
            AssertStyleSetterOrBasedOn(style, TextOptions.TextRenderingModeProperty, TextRenderingMode.ClearType, styleKey);
            AssertStyleSetterOrBasedOn(style, TextOptions.TextHintingModeProperty, TextHintingMode.Fixed, styleKey);
        }

        private static void AssertStyleFluentFontFamilySetterOrBasedOn(Style style, string description)
        {
            Style current = style;
            while (current != null)
            {
                foreach (SetterBase? setterBase in current.Setters)
                {
                    if (setterBase is Setter setter && setter.Property == Control.FontFamilyProperty)
                    {
                        Assert.IsTrue(IsFluentFontFamilySetterValue(setter.Value),
                            description + " should set FontFamily from FluentFontFamily.");
                        return;
                    }
                }

                current = current.BasedOn;
            }

            Assert.Fail(description + " should set FontFamily from FluentFontFamily (searched full BasedOn chain).");
        }

        private static bool IsFluentFontFamilySetterValue(object value)
        {
            return value is DynamicResourceExtension dynamicResource
                ? Equals(dynamicResource.ResourceKey, "FluentFontFamily")
                : value is FontFamily fontFamily &&
                fontFamily.Source.IndexOf("Segoe UI Variable", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Walks the <see cref="Style.BasedOn"/> chain looking for a setter that matches
        /// <paramref name="property"/> with <paramref name="expectedValue"/>.  A style that
        /// inherits the correct value through <c>BasedOn</c> satisfies the assertion.
        /// </summary>
        private static void AssertStyleSetterOrBasedOn(Style style, DependencyProperty property, object expectedValue, string description)
        {
            Style current = style;
            while (current != null)
            {
                foreach (SetterBase? setterBase in current.Setters)
                {
                    if (setterBase is Setter setter && setter.Property == property)
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
            string source = File.ReadAllText(Path.Combine(FindRepoRoot(), relativePath));

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
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
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
