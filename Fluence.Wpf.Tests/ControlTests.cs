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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf;
using Fluent = Fluence.Wpf.Controls;
using Fluence.Wpf.Demo;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public partial class ControlTests
    {
        private static readonly Uri GenericDictionaryUri =
            new Uri("/Fluence.Wpf;component/Themes/Generic.xaml", UriKind.Relative);

        [TestInitialize]
        public void ControlTestsInitialize()
        {
            WpfTestSta.Invoke(ResetSharedWpfState);
        }

        [TestCleanup]
        public void ControlTestsCleanup()
        {
            WpfTestSta.Invoke(ResetSharedWpfState);
        }

        private static void ResetSharedWpfState()
        {
            var application = Application.Current;
            if (application == null)
            {
                application = new Application
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };
            }

            Keyboard.ClearFocus();

            var windows = application.Windows.Cast<Window>().ToArray();
            foreach (var window in windows)
            {
                window.Content = null;
                window.Close();
            }

            var dispatcher = Dispatcher.CurrentDispatcher;
            dispatcher.Invoke(DispatcherPriority.Loaded, new Action(delegate { }));
            dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(delegate { }));
            dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));

            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application.Resources.MergedDictionaries.Clear();
            application.Resources.Clear();
        }

        private static void RunOnStaThread(Action action)
        {
            var dispatcher = WpfTestSta.Dispatcher;

            Exception capturedException = null;
            dispatcher.Invoke(new Action(delegate
            {
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    capturedException = exception;
                }
            }));

            if (capturedException != null)
            {
                ExceptionDispatchInfo.Capture(capturedException).Throw();
            }
        }

        private static Application EnsureApplication()
        {
            return WpfTestSta.EnsureApplication();
        }

        private static ResourceDictionary MergeGenericDictionary(Application application)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application.Resources.MergedDictionaries.Clear();
            application.Resources.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
            var dictionaries = application.Resources.MergedDictionaries;
            var genericDictionary = dictionaries.Count > 0 ? dictionaries[dictionaries.Count - 1] : null;

            var demoShared = new ResourceDictionary
            {
                Source = new Uri("/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml", UriKind.Relative)
            };
            application.Resources.MergedDictionaries.Add(demoShared);

            return genericDictionary;
        }

        private static void DrainDispatcher(Dispatcher dispatcher)
        {
            dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));
        }

        private static T FindVisualChild<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null)
            {
                return null;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var index = 0; index < childCount; index++)
            {
                var child = VisualTreeHelper.GetChild(root, index);
                var match = child as T;
                if (match != null)
                {
                    return match;
                }

                match = FindVisualChild<T>(child);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null)
            {
                yield break;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var index = 0; index < childCount; index++)
            {
                var child = VisualTreeHelper.GetChild(root, index);
                var match = child as T;
                if (match != null)
                {
                    yield return match;
                }

                foreach (var descendant in FindVisualChildren<T>(child))
                {
                    yield return descendant;
                }
            }
        }

        private static DependencyObject FindVisualChildByTypeName(DependencyObject root, string typeName)
        {
            if (root == null)
            {
                return null;
            }

            if (string.Equals(root.GetType().Name, typeName, StringComparison.Ordinal))
            {
                return root;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var index = 0; index < childCount; index++)
            {
                var found = FindVisualChildByTypeName(VisualTreeHelper.GetChild(root, index), typeName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static T FindVisualChildByName<T>(DependencyObject root, string name) where T : FrameworkElement
        {
            if (root == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var index = 0; index < childCount; index++)
            {
                var child = VisualTreeHelper.GetChild(root, index) as FrameworkElement;
                if (child != null && child.Name == name)
                {
                    var match = child as T;
                    if (match != null)
                    {
                        return match;
                    }
                }

                var found = FindVisualChildByName<T>(VisualTreeHelper.GetChild(root, index), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static StackPanel GetNavigationViewItemsHostPanel(Fluent.NavigationView nav)
        {
            var presenter = FindVisualChild<ItemsPresenter>(nav);
            if (presenter == null)
            {
                return null;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(presenter);
            if (childCount < 1)
            {
                return null;
            }

            return VisualTreeHelper.GetChild(presenter, 0) as StackPanel;
        }

        [TestMethod]
        public void FontIcon_DefaultFontFamily_IsSegoeFluent()
        {
            RunOnStaThread(() =>
            {
                var fontIcon = new Fluent.FontIcon();

                Assert.AreEqual("Segoe Fluent Icons", fontIcon.IconFontFamily.Source);
            });
        }

        [TestMethod]
        public void FontIcon_GlyphProperty_Roundtrips()
        {
            RunOnStaThread(() =>
            {
                var fontIcon = new Fluent.FontIcon();
                var testGlyph = "\uE710";

                fontIcon.Glyph = testGlyph;

                Assert.AreEqual(testGlyph, fontIcon.Glyph);
            });
        }

        [TestMethod]
        public void Button_DefaultAppearance_IsStandard()
        {
            RunOnStaThread(() =>
            {
                var button = new Fluent.Button();

                Assert.AreEqual(ControlAppearance.Standard, button.Appearance);
            });
        }

        [TestMethod]
        public void Button_AccentAppearance_CanBeSet()
        {
            RunOnStaThread(() =>
            {
                var button = new Fluent.Button();

                button.Appearance = ControlAppearance.Accent;

                Assert.AreEqual(ControlAppearance.Accent, button.Appearance);
            });
        }

        [TestMethod]
        public void TextBox_PlaceholderText_Roundtrips()
        {
            RunOnStaThread(() =>
            {
                var textBox = new Fluent.TextBox();
                var placeholder = "Enter text here...";

                textBox.PlaceholderText = placeholder;

                Assert.AreEqual(placeholder, textBox.PlaceholderText);
            });
        }

        [TestMethod]
        public void TextBox_ClearButtonEnabled_DefaultTrue()
        {
            RunOnStaThread(() =>
            {
                var textBox = new Fluent.TextBox();

                Assert.IsTrue(textBox.ClearButtonEnabled);
            });
        }

        [TestMethod]
        public void PasswordBox_RevealButtonEnabled_DefaultTrue()
        {
            RunOnStaThread(() =>
            {
                var passwordBox = new Fluent.PasswordBox();

                Assert.IsTrue(passwordBox.RevealButtonEnabled);
            });
        }

        [TestMethod]
        public void PasswordBox_IsPasswordRevealed_DefaultFalse()
        {
            RunOnStaThread(() =>
            {
                var passwordBox = new Fluent.PasswordBox();

                Assert.IsFalse(passwordBox.IsPasswordRevealed);
            });
        }

        [TestMethod]
        public void TextBox_DefaultChrome_UsesWinUiReferenceValues()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                var window = new Window();
                var textBox = new Fluent.TextBox
                {
                    Width = 260
                };

                try
                {
                    window.Content = textBox;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var mainBorder = textBox.Template.FindName("MainBorder", textBox) as Border;
                    var clearButton = textBox.Template.FindName("PART_ClearButton", textBox) as Button;

                    Assert.IsNotNull(mainBorder);
                    Assert.IsNotNull(clearButton);
                    Assert.AreEqual(new Thickness(10, 5, 6, 6), textBox.Padding);
                    Assert.AreEqual(32.0, textBox.MinHeight);
                    Assert.IsInstanceOfType(mainBorder.BorderBrush, typeof(LinearGradientBrush), "TextBox should use the text-control elevation border brush.");
                    Assert.AreEqual(30.0, clearButton.Width, 0.1, "Clear button should use the WinUI helper button width.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void TextBox_FocusState_ShowsAccentLineUnderneath()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                var window = new Window();
                var textBox = new Fluent.TextBox
                {
                    Width = 260,
                    Text = "Focused"
                };

                try
                {
                    window.Content = textBox;
                    window.Show();
                    textBox.Focus();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var accentLine = textBox.Template.FindName("FocusAccentLine", textBox) as Border;

                    Assert.IsNotNull(accentLine, "FocusAccentLine should exist in the template.");
                    Assert.AreEqual(1.0, accentLine.Opacity, "Accent line should be visible when focused.");
                    Assert.AreEqual(2.0, accentLine.Height, "Accent line should be 2px tall.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void PasswordBox_DefaultChrome_UsesWinUiReferenceValues()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                var window = new Window();
                var passwordBox = new Fluent.PasswordBox
                {
                    Width = 260
                };

                try
                {
                    window.Content = passwordBox;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var mainBorder = passwordBox.Template.FindName("MainBorder", passwordBox) as Border;
                    var revealButton = passwordBox.Template.FindName("PART_RevealButton", passwordBox) as Button;

                    Assert.IsNotNull(mainBorder);
                    Assert.IsNotNull(revealButton);
                    Assert.AreEqual(new Thickness(10, 5, 6, 6), passwordBox.Padding);
                    Assert.AreEqual(32.0, passwordBox.MinHeight);
                    Assert.IsInstanceOfType(mainBorder.BorderBrush, typeof(LinearGradientBrush), "PasswordBox should use the text-control elevation border brush.");
                    Assert.AreEqual(30.0, revealButton.Width, 0.1, "Reveal button should use the WinUI helper button width.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void PasswordBox_FocusState_ShowsAccentLineUnderneath()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                var window = new Window();
                var passwordBox = new Fluent.PasswordBox
                {
                    Width = 260,
                    Password = "Focused"
                };

                try
                {
                    window.Content = passwordBox;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var innerPasswordBox = passwordBox.Template.FindName("PART_PasswordBox", passwordBox) as PasswordBox;
                    var accentLine = passwordBox.Template.FindName("FocusAccentLine", passwordBox) as Border;

                    Assert.IsNotNull(innerPasswordBox);
                    Assert.IsNotNull(accentLine, "FocusAccentLine should exist in the template.");

                    innerPasswordBox.Focus();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(1.0, accentLine.Opacity, "Accent line should be visible when inner PasswordBox has focus.");
                    Assert.AreEqual(2.0, accentLine.Height, "Accent line should be 2px tall.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void ListView_ItemAnimationsEnabled_DefaultTrue()
        {
            RunOnStaThread(() =>
            {
                var listView = new Fluent.ListView();

                Assert.IsTrue(listView.ItemAnimationsEnabled);
            });
        }

        [TestMethod]
        public void ListView_HoverHighlightEnabled_DefaultTrue()
        {
            RunOnStaThread(() =>
            {
                var listView = new Fluent.ListView();

                Assert.IsTrue(listView.HoverHighlightEnabled);
            });
        }

        [TestMethod]
        public void ListViewItem_DefaultChrome_UsesWinUiReferenceValues()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                var window = new Window();
                var listView = new Fluent.ListView
                {
                    Width = 260,
                    Height = 120
                };
                listView.Items.Add("Item 1");

                try
                {
                    window.Content = listView;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var item = listView.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;

                    Assert.IsNotNull(item);
                    Assert.AreEqual(new Thickness(12, 0, 12, 0), item.Padding);
                    Assert.AreEqual(HorizontalAlignment.Left, item.HorizontalContentAlignment);
                    Assert.AreEqual(40.0, item.MinHeight);
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void ListViewItem_SelectionIndicator_UsesWinUiCornerRadius()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                var window = new Window();
                var listView = new Fluent.ListView
                {
                    Width = 260,
                    Height = 120
                };
                listView.Items.Add("Item 1");

                try
                {
                    window.Content = listView;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var item = listView.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
                    Assert.IsNotNull(item);

                    item.ApplyTemplate();
                    var selectionIndicator = item.Template.FindName("SelectionIndicator", item) as Border;

                    Assert.IsNotNull(selectionIndicator);
                    // WI-3 C20: canonical ListViewItemSelectionIndicatorCornerRadius = 1.5
                    Assert.AreEqual(new CornerRadius(1.5), selectionIndicator.CornerRadius);
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void ListViewItem_SelectedState_UsesWinUiSelectedBrush()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
                var window = new Window();
                var listView = new Fluent.ListView
                {
                    Width = 260,
                    Height = 120,
                    SelectionMode = SelectionMode.Single
                };
                listView.Items.Add("Item 1");

                try
                {
                    window.Content = listView;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    listView.SelectedIndex = 0;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var item = listView.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
                    Assert.IsNotNull(item);

                    item.ApplyTemplate();
                    var selectedOverlay = item.Template.FindName("SelectedOverlay", item) as Border;
                    var selectionIndicator = item.Template.FindName("SelectionIndicator", item) as Border;
                    var expectedSelectedBrush = application.Resources["SubtleFillColorSecondaryBrush"] as SolidColorBrush;
                    var expectedIndicatorBrush = application.Resources["AccentFillColorDefaultBrush"] as SolidColorBrush;

                    Assert.IsNotNull(selectedOverlay);
                    Assert.IsNotNull(selectionIndicator);
                    Assert.IsNotNull(expectedSelectedBrush);
                    Assert.IsNotNull(expectedIndicatorBrush);
                    Assert.IsInstanceOfType(selectedOverlay.Background, typeof(SolidColorBrush));
                    Assert.IsInstanceOfType(selectionIndicator.Background, typeof(SolidColorBrush));
                    Assert.AreEqual(expectedSelectedBrush.Color, ((SolidColorBrush)selectedOverlay.Background).Color);
                    Assert.AreEqual(expectedIndicatorBrush.Color, ((SolidColorBrush)selectionIndicator.Background).Color);
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void TextBlockExtensions_Typography_SetsCorrectFontSize()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);

                try
                {
                    var textBlock = new TextBlock();

                    Fluent.TextBlockExtensions.SetTypography(textBlock, FluentTypography.BodyLarge);

                    Assert.AreSame(application.TryFindResource("BodyLargeTextBlockStyle"), textBlock.Style);
                    Assert.AreEqual(18.0, textBlock.FontSize);
                }
                finally
                {
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void TextBox_TextViewAlignsWithPlaceholder_WhenIconIsShown()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();
                var textBox = new Fluent.TextBox
                {
                    Width = 260,
                    PlaceholderText = "With icon",
                    Icon = new Fluent.FontIcon
                    {
                        Glyph = "\uE721",
                        IconFontSize = 14
                    }
                };

                try
                {
                    window.Content = textBox;
                    window.Show();
                    textBox.Focus();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var placeholder = textBox.Template.FindName("PlaceholderTextBlock", textBox) as FrameworkElement;
                    var textView = FindVisualChildByTypeName(textBox, "TextBoxView") as FrameworkElement;

                    Assert.IsNotNull(placeholder);
                    Assert.IsNotNull(textView);

                    var placeholderX = placeholder.TransformToAncestor(window).Transform(new Point(0, 0)).X;
                    var textViewX = textView.TransformToAncestor(window).Transform(new Point(0, 0)).X;

                    Assert.AreEqual(placeholderX, textViewX, 0.5, "Text caret host should start where placeholder text starts.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void Button_AccentAppearance_UsesAccentBrush()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                var window = new Window();
                var button = new Fluent.Button
                {
                    Width = 140,
                    Content = "Accent",
                    Appearance = ControlAppearance.Accent
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var restFill = button.Template.FindName("RestFill", button) as Border;
                    var accentBrush = application.Resources["AccentFillColorDefaultBrush"] as SolidColorBrush;

                    Assert.IsNotNull(restFill);
                    Assert.IsNotNull(accentBrush);
                    Assert.IsInstanceOfType(restFill.Background, typeof(SolidColorBrush));
                    Assert.AreEqual(accentBrush.Color, ((SolidColorBrush)restFill.Background).Color);
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void Button_LeftIconContentGroup_RemainsCentered()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();
                var button = new Fluent.Button
                {
                    Width = 180,
                    Content = "With Icon",
                    Icon = new Fluent.FontIcon
                    {
                        Glyph = "\uE710",
                        IconFontSize = 14
                    }
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AssertContentGroupIsCentered(window, button, "With Icon", "\uE710");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void Button_RightIconContentGroup_RemainsCentered()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();
                var button = new Fluent.Button
                {
                    Width = 180,
                    Content = "Icon Right",
                    IconPlacement = ElementPlacement.Right,
                    Icon = new Fluent.FontIcon
                    {
                        Glyph = "\uE72A",
                        IconFontSize = 14
                    }
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AssertContentGroupIsCentered(window, button, "Icon Right", "\uE72A");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void Button_LeftIcon_RendersGlyph()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();
                var button = new Fluent.Button
                {
                    Width = 180,
                    Content = "With Icon",
                    Icon = new Fluent.FontIcon
                    {
                        Glyph = "\uE710",
                        IconFontSize = 14
                    }
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    TextBlock glyphTextBlock = null;
                    foreach (var textBlock in FindVisualChildren<TextBlock>(button))
                    {
                        if (string.Equals(textBlock.Text, "\uE710", StringComparison.Ordinal))
                        {
                            glyphTextBlock = textBlock;
                            break;
                        }
                    }

                    Assert.IsNotNull(glyphTextBlock, "Left-placed button icons should render their glyph.");
                    Assert.IsTrue(glyphTextBlock.IsVisible, "Left-placed button icons should be visible, not just present in the tree.");
                    Assert.IsTrue(glyphTextBlock.ActualWidth > 0, "Left-placed button icons should occupy layout space.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void Button_AccentAppearance_UsesDistinctWinUiStateBrushes()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                var window = new Window();
                var button = new Fluent.Button
                {
                    Width = 140,
                    Content = "Accent",
                    Appearance = ControlAppearance.Accent
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var restFill = button.Template.FindName("RestFill", button) as Border;
                    var accentDefaultBrush = application.Resources["AccentFillColorDefaultBrush"] as SolidColorBrush;
                    var accentSecondaryBrush = application.Resources["AccentFillColorSecondaryBrush"] as SolidColorBrush;
                    var accentTertiaryBrush = application.Resources["AccentFillColorTertiaryBrush"] as SolidColorBrush;

                    Assert.IsNotNull(restFill);
                    Assert.IsNotNull(accentDefaultBrush);
                    Assert.IsNotNull(accentSecondaryBrush);
                    Assert.IsNotNull(accentTertiaryBrush);
                    Assert.IsInstanceOfType(restFill.Background, typeof(SolidColorBrush));
                    Assert.AreEqual(accentDefaultBrush.Color, ((SolidColorBrush)restFill.Background).Color);
                    Assert.AreNotEqual(accentDefaultBrush.Color, accentSecondaryBrush.Color, "Accent pointer-over brush should differ from the default accent brush.");
                    Assert.AreNotEqual(accentDefaultBrush.Color, accentTertiaryBrush.Color, "Accent pressed brush should differ from the default accent brush.");
                    Assert.IsTrue(accentSecondaryBrush.Color.A < accentDefaultBrush.Color.A, "Accent pointer-over brush should be visually distinct from default.");
                    Assert.IsTrue(accentTertiaryBrush.Color.A < accentSecondaryBrush.Color.A, "Accent pressed brush should progress further than pointer-over.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void MainWindow_AccentColorButtons_UseButtonControl()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                MainWindow window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SelectMainWindowNavPage(window, window.Dispatcher, "Window");

                    var accentSwatchButtons = FindVisualChildren<Fluent.Button>(window)
                        .Where(b => b.Tag is string hex && hex.Length > 0 && hex[0] == '#')
                        .ToList();

                    Assert.IsTrue(accentSwatchButtons.Count >= 8, "Window page should expose at least 8 accent swatch buttons.");
                    foreach (var swatch in accentSwatchButtons)
                    {
                        Assert.IsInstanceOfType(swatch, typeof(Fluent.Button));
                    }
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void MainWindow_TitleSelectors_UseExpectedControls()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                MainWindow window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SelectMainWindowNavPage(window, window.Dispatcher, "Window");

                    Assert.IsInstanceOfType(FindVisualChildByName<Fluent.RadioButton>(window, "ThemeLight"), typeof(Fluent.RadioButton));
                    Assert.IsInstanceOfType(FindVisualChildByName<Fluent.RadioButton>(window, "ThemeDark"), typeof(Fluent.RadioButton));
                    Assert.IsInstanceOfType(FindVisualChildByName<Fluent.RadioButton>(window, "ThemeHighContrast"), typeof(Fluent.RadioButton));
                    Assert.IsInstanceOfType(FindVisualChildByName<Fluent.RadioButton>(window, "ThemeAuto"), typeof(Fluent.RadioButton));
                    Assert.IsInstanceOfType(FindVisualChildByName<Fluent.ComboBox>(window, "BackdropCombo"), typeof(Fluent.ComboBox));
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void MainWindow_ThemeRadioButton_UpdatesStateLabel()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Auto, BackdropType.Auto, true);
                MainWindow window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SelectMainWindowNavPage(window, window.Dispatcher, "Window");

                    var themeDark = FindVisualChildByName<Fluent.RadioButton>(window, "ThemeDark");
                    var themeStateLabel = FindVisualChildByName<TextBlock>(window, "ThemeStateLabel");

                    Assert.IsNotNull(themeDark);
                    Assert.IsNotNull(themeStateLabel);

                    themeDark.IsChecked = true;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual("Current: Dark", themeStateLabel.Text, "Theme radio button should update the state label when checked.");
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void MainWindow_DemoButtons_RenderTheirIcons()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                MainWindow window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SelectMainWindowNavPage(window, window.Dispatcher, "Buttons");

                    var iconLeftButton = FindFluentButtonByContent(window, "Icon Left");
                    var iconRightButton = FindFluentButtonByContent(window, "Icon Right");

                    Assert.IsNotNull(iconLeftButton, "Buttons page should contain an 'Icon Left' button.");
                    Assert.IsNotNull(iconRightButton, "Buttons page should contain an 'Icon Right' button.");

                    AssertButtonShowsGlyph(iconLeftButton, "\uE774");
                    AssertButtonShowsGlyph(iconRightButton, "\uE8D6");
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void MainWindow_StandardDemoButtonIcons_UsePrimaryTextBrush()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                MainWindow window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SelectMainWindowNavPage(window, window.Dispatcher, "Buttons");

                    var iconLeftButton = FindFluentButtonByContent(window, "Icon Left");
                    var iconRightButton = FindFluentButtonByContent(window, "Icon Right");
                    var expectedBrush = application.Resources["TextFillColorPrimaryBrush"] as SolidColorBrush;

                    Assert.IsNotNull(iconLeftButton, "Buttons page should contain an 'Icon Left' button.");
                    Assert.IsNotNull(iconRightButton, "Buttons page should contain an 'Icon Right' button.");
                    Assert.IsNotNull(expectedBrush);

                    var iconLeftGlyph = FindButtonIconTextBlock(iconLeftButton);
                    var iconRightGlyph = FindButtonIconTextBlock(iconRightButton);

                    Assert.IsNotNull(iconLeftGlyph, "Icon Left demo button should render an icon glyph.");
                    Assert.IsNotNull(iconRightGlyph, "Icon Right demo button should render an icon glyph.");
                    Assert.IsInstanceOfType(iconLeftGlyph.Foreground, typeof(SolidColorBrush));
                    Assert.IsInstanceOfType(iconRightGlyph.Foreground, typeof(SolidColorBrush));
                    Assert.AreEqual(expectedBrush.Color, ((SolidColorBrush)iconLeftGlyph.Foreground).Color, "Standard demo button icons should use the primary text brush.");
                    Assert.AreEqual(expectedBrush.Color, ((SolidColorBrush)iconRightGlyph.Foreground).Color, "Standard demo button icons should use the primary text brush.");
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void FluentTabControl_SelectedTabTouchesContentPanel()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();

                try
                {
                    var tabControl = new TabControl();
                    tabControl.Items.Add(new TabItem { Header = "First", Content = new TextBlock { Text = "A" } });
                    tabControl.Items.Add(new TabItem { Header = "Second", Content = new TextBlock { Text = "B" } });
                    window.Content = tabControl;
                    window.Width = 640;
                    window.Height = 480;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tabControl.SelectedIndex = 1;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var selectedTab = tabControl.ItemContainerGenerator.ContainerFromIndex(1) as TabItem;
                    var contentPanel = tabControl.Template.FindName("ContentPanel", tabControl) as FrameworkElement;

                    Assert.IsNotNull(selectedTab);
                    Assert.IsNotNull(contentPanel);

                    var selectedOrigin = selectedTab.TransformToAncestor(window).Transform(new Point(0, 0));
                    var contentOrigin = contentPanel.TransformToAncestor(window).Transform(new Point(0, 0));
                    var selectedBottom = selectedOrigin.Y + selectedTab.ActualHeight;

                    Assert.IsTrue(selectedBottom >= contentOrigin.Y - 1, "Selected tab should visually join the tab page.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void MainWindow_TabSelection_ActivatesExpectedContent()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                MainWindow window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SelectMainWindowNavPage(window, window.Dispatcher, "Buttons");
                    Assert.IsNotNull(FindFluentButtonByContent(window, "Icon Left"));

                    SelectMainWindowNavPage(window, window.Dispatcher, "Inputs");
                    Assert.IsNotNull(FindVisualChildByName<Fluent.TextBox>(window, "CharCountTextBox"));

                    SelectMainWindowNavPage(window, window.Dispatcher, "Selection");
                    Assert.IsNotNull(FindVisualChildByName<Fluent.ToggleSwitch>(window, "DefaultToggle"));
                    Assert.IsNotNull(FindVisualChildByName<Fluent.ComboBox>(window, "SelectionDemoCombo"));

                    SelectMainWindowNavPage(window, window.Dispatcher, "Status");
                    Assert.IsNotNull(FindVisualChildByName<Fluent.ProgressBar>(window, "StepProgressBar"));

                    SelectMainWindowNavPage(window, window.Dispatcher, "Data");
                    Assert.IsNotNull(FindVisualChildByName<Fluent.ListView>(window, "EmptyStateListView"));
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void MainWindow_NavigationView_HasSixteenNavItems()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                MainWindow window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var nav = window.FindName("DemoNav") as Fluent.NavigationView;
                    Assert.IsNotNull(nav);
                    var count = 0;
                    foreach (var obj in nav.Items)
                    {
                        if (obj is Fluent.NavigationViewItem)
                        {
                            count++;
                        }
                    }

                    Assert.AreEqual(16, count, "Demo navigation should expose 16 pages (11 original + 5 gallery additions).");
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void MainWindow_CaptionButtons_DefaultOverrides()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                MainWindow window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.IsTrue(window.IsMinimizable);
                    Assert.IsTrue(window.IsMaximizable);
                    Assert.IsTrue(window.IsClosable);

                    var closeButton = window.Template.FindName("CloseButton", window) as Button;
                    Assert.IsNotNull(closeButton);
                    Assert.AreEqual(Visibility.Visible, closeButton.Visibility);
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void MainWindow_ThemeWatcherToggle_UpdatesLabel()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                MainWindow window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SelectMainWindowNavPage(window, window.Dispatcher, "Window");

                    var toggle = FindVisualChildByName<Fluent.ToggleSwitch>(window, "ThemeWatcherToggle");
                    var label = FindVisualChildByName<TextBlock>(window, "SystemThemeLabel");

                    Assert.IsNotNull(toggle);
                    Assert.IsNotNull(label);
                    Assert.IsTrue(toggle.IsChecked == true, "ThemeWatcherToggle should default to checked.");
                    Assert.AreEqual("Watching: Yes", label.Text);

                    toggle.IsChecked = false;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual("Watching: No", label.Text, "Unchecking the toggle should update the label.");
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void MainWindow_IconLeftButton_IconIsVerticallyCentered()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                MainWindow window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SelectMainWindowNavPage(window, window.Dispatcher, "Buttons");

                    var button = FindFluentButtonByContent(window, "Icon Left");
                    Assert.IsNotNull(button, "Buttons page should contain an 'Icon Left' button.");

                    var glyphTextBlock = FindButtonGlyphTextBlock(button, "\uE774");
                    Assert.IsNotNull(glyphTextBlock);
                    var buttonOrigin = button.TransformToAncestor(window).Transform(new Point(0, 0));
                    var glyphOrigin = glyphTextBlock.TransformToAncestor(window).Transform(new Point(0, 0));
                    var buttonCenterY = buttonOrigin.Y + (button.ActualHeight / 2.0);
                    var glyphCenterY = glyphOrigin.Y + (glyphTextBlock.ActualHeight / 2.0);

                    Assert.AreEqual(buttonCenterY, glyphCenterY, 1.0, "Button icon should be vertically centered.");
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void MainWindow_StandardButtonIcons_AreInsideButtonBounds()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                MainWindow window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SelectMainWindowNavPage(window, window.Dispatcher, "Buttons");

                    var iconLeftButton = FindFluentButtonByContent(window, "Icon Left");
                    var iconRightButton = FindFluentButtonByContent(window, "Icon Right");

                    Assert.IsNotNull(iconLeftButton, "Buttons page should contain an 'Icon Left' button.");
                    Assert.IsNotNull(iconRightButton, "Buttons page should contain an 'Icon Right' button.");

                    AssertGlyphWithinButtonBounds(window, iconLeftButton, "\uE774");
                    AssertGlyphWithinButtonBounds(window, iconRightButton, "\uE8D6");
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void Stage3_Card_DefaultVariant_IsDefault()
        {
            RunOnStaThread(() =>
            {
                var card = new Fluent.Card();
                Assert.AreEqual(CardVariant.Default, card.Variant);
            });
        }

        [TestMethod]
        public void Stage3_Card_IsClickable_ExposesIsPressed()
        {
            RunOnStaThread(() =>
            {
                var card = new Fluent.Card { IsClickable = true };
                Assert.IsFalse(card.IsPressed);
            });
        }

        [TestMethod]
        public void Stage3_CheckBox_Content_Roundtrips()
        {
            RunOnStaThread(() =>
            {
                var cb = new Fluent.CheckBox { Content = "Test" };
                Assert.AreEqual("Test", cb.Content as string);
            });
        }

        [TestMethod]
        public void Stage3_ComboBox_PlaceholderText_Roundtrips()
        {
            RunOnStaThread(() =>
            {
                var combo = new Fluent.ComboBox { PlaceholderText = "Pick one" };
                Assert.AreEqual("Pick one", combo.PlaceholderText);
            });
        }

        [TestMethod]
        public void ComboBox_SelectionChange_UpdatesDisplayedContent()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();
                var combo = new Fluent.ComboBox { Width = 240 };
                combo.Items.Add(new ComboBoxItem { Content = "Alpha" });
                combo.Items.Add(new ComboBoxItem { Content = "Beta" });
                combo.SelectedIndex = 0;

                try
                {
                    window.Content = combo;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var presenter = combo.Template.FindName("contentPresenter", combo) as ContentPresenter;
                    Assert.IsNotNull(presenter, "contentPresenter should exist in the template.");
                    Assert.AreEqual("Alpha", presenter.Content as string, "Initial displayed content should match first selection.");

                    combo.SelectedIndex = 1;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual("Beta", presenter.Content as string, "Displayed content should update after selection change.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void ComboBox_ItemTemplate_HasHoverOverlay()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();
                var combo = new Fluent.ComboBox { Width = 240 };
                combo.Items.Add(new ComboBoxItem { Content = "Alpha" });
                combo.Items.Add(new ComboBoxItem { Content = "Beta" });

                try
                {
                    window.Content = combo;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    combo.IsDropDownOpen = true;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var item = combo.ItemContainerGenerator.ContainerFromIndex(0) as ComboBoxItem;
                    Assert.IsNotNull(item, "ComboBoxItem container should be generated.");
                    item.ApplyTemplate();

                    var outerBorder = item.Template.FindName("OuterBorder", item);
                    Assert.IsNotNull(outerBorder, "ComboBoxItem template should contain an OuterBorder element.");

                    var selectionIndicator = item.Template.FindName("SelectionIndicator", item);
                    Assert.IsNotNull(selectionIndicator, "ComboBoxItem template should contain a SelectionIndicator element.");

                    combo.IsDropDownOpen = false;
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void ComboBox_NoSelection_ShowsPlaceholder()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();
                var combo = new Fluent.ComboBox
                {
                    Width = 240,
                    PlaceholderText = "Choose...",
                    SelectedIndex = -1
                };
                combo.Items.Add(new ComboBoxItem { Content = "Alpha" });

                try
                {
                    window.Content = combo;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var placeholder = combo.Template.FindName("PlaceholderTextBlock", combo) as TextBlock;
                    Assert.IsNotNull(placeholder, "PlaceholderTextBlock should exist in the template.");
                    Assert.AreEqual(Visibility.Visible, placeholder.Visibility, "Placeholder should be visible when SelectedIndex is explicitly -1.");

                    combo.SelectedIndex = 0;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(Visibility.Collapsed, placeholder.Visibility, "Placeholder should be collapsed after selection.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void ComboBox_ToggleButton_OpensDropDown()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();
                var combo = new Fluent.ComboBox
                {
                    Width = 240,
                    PlaceholderText = "Pick one"
                };
                combo.Items.Add(new ComboBoxItem { Content = "Alpha" });
                combo.Items.Add(new ComboBoxItem { Content = "Beta" });

                try
                {
                    window.Content = combo;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    combo.ApplyTemplate();
                    var toggle = combo.Template.FindName("ToggleButton", combo) as ToggleButton;
                    var popup = combo.Template.FindName("PART_Popup", combo) as Popup;

                    Assert.IsNotNull(toggle);
                    Assert.IsNotNull(popup);

                    var peer = new ToggleButtonAutomationPeer(toggle);
                    var toggleProvider = peer.GetPattern(PatternInterface.Toggle) as IToggleProvider;

                    Assert.IsNotNull(toggleProvider, "ComboBox toggle should expose a toggle pattern.");

                    toggleProvider.Toggle();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.IsTrue(combo.IsDropDownOpen, "ComboBox toggle should open the drop-down.");
                    Assert.IsTrue(popup.IsOpen, "ComboBox popup should open when the toggle is clicked.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void ComboBox_ToggleButton_UsesReleaseClickMode()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();
                var combo = new Fluent.ComboBox
                {
                    Width = 240,
                    PlaceholderText = "Pick one"
                };
                combo.Items.Add(new ComboBoxItem { Content = "Alpha" });

                try
                {
                    window.Content = combo;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    combo.ApplyTemplate();
                    var toggle = combo.Template.FindName("ToggleButton", combo) as ToggleButton;

                    Assert.IsNotNull(toggle);
                    Assert.AreEqual(ClickMode.Release, toggle.ClickMode, "ComboBox toggle should use release-click behavior so the drop-down stays open on a normal click.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void ComboBox_DropDownSelection_UpdatesSelectedIndex()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();
                var combo = new Fluent.ComboBox
                {
                    Width = 240,
                    PlaceholderText = "Pick one"
                };
                combo.Items.Add(new ComboBoxItem { Content = "Alpha" });
                combo.Items.Add(new ComboBoxItem { Content = "Beta" });

                try
                {
                    window.Content = combo;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    combo.IsDropDownOpen = true;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var item = combo.ItemContainerGenerator.ContainerFromIndex(1) as ComboBoxItem;
                    Assert.IsNotNull(item, "ComboBox should generate the drop-down item container when opened.");

                    item.IsSelected = true;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(1, combo.SelectedIndex, "Selecting a drop-down item should update the selected index.");
                    Assert.AreEqual("Beta", combo.SelectedText, "Selecting a drop-down item should update the displayed selected text.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void Stage3_ProgressBar_ProgressMode_DefaultIsStandard()
        {
            RunOnStaThread(() =>
            {
                var bar = new Fluent.ProgressBar();
                Assert.AreEqual(ProgressBarMode.Standard, bar.ProgressMode);
            });
        }

        [TestMethod]
        public void Stage3_Border_Variant_DefaultIsNone()
        {
            RunOnStaThread(() =>
            {
                var border = new Fluent.Border();
                Assert.AreEqual(BorderVariant.None, border.Variant);
            });
        }

        [TestMethod]
        public void Stage3_StackPanel_Spacing_DefaultZero()
        {
            RunOnStaThread(() =>
            {
                var panel = new Fluent.StackPanel();
                Assert.AreEqual(0.0, panel.Spacing);
            });
        }

        [TestMethod]
        public void Stage3_DockPanel_LastChildFill_DefaultTrue()
        {
            RunOnStaThread(() =>
            {
                var dock = new Fluent.DockPanel();
                Assert.IsTrue(dock.LastChildFill);
            });
        }

        [TestMethod]
        public void Stage3_TextBox_ValidationState_DefaultNone()
        {
            RunOnStaThread(() =>
            {
                var tb = new Fluent.TextBox();
                Assert.AreEqual(ValidationState.None, tb.ValidationState);
            });
        }

        [TestMethod]
        public void Stage3_TextBox_HelperText_Roundtrips()
        {
            RunOnStaThread(() =>
            {
                var tb = new Fluent.TextBox { HelperText = "Hint" };
                Assert.AreEqual("Hint", tb.HelperText);
            });
        }

        [TestMethod]
        public void Stage3_PasswordBox_ShowCapsLockIndicator_DefaultTrue()
        {
            RunOnStaThread(() =>
            {
                var pb = new Fluent.PasswordBox();
                Assert.IsTrue(pb.ShowCapsLockIndicator);
            });
        }

        [TestMethod]
        public void Stage3_PasswordBox_ComputesPasswordStrength()
        {
            RunOnStaThread(() =>
            {
                var pb = new Fluent.PasswordBox { Password = "Aa1!aaaaaa" };
                Assert.IsTrue(pb.PasswordStrength >= 3);
            });
        }

        [TestMethod]
        public void Stage3_ListView_EmptyContent_DefaultNull()
        {
            RunOnStaThread(() =>
            {
                var list = new Fluent.ListView();
                Assert.IsNull(list.EmptyContent);
            });
        }

        [TestMethod]
        public void Stage3_FontIcon_Rotation_Roundtrips()
        {
            RunOnStaThread(() =>
            {
                var icon = new Fluent.FontIcon { Rotation = 33 };
                Assert.AreEqual(33.0, icon.Rotation);
            });
        }

        [TestMethod]
        public void Stage3_FontIcon_IsSpinning_Roundtrips()
        {
            RunOnStaThread(() =>
            {
                var icon = new Fluent.FontIcon { IsSpinning = true };
                Assert.IsTrue(icon.IsSpinning);
            });
        }

        [TestMethod]
        public void Stage3_FontIcon_EnableTransitions_DefaultTrue()
        {
            RunOnStaThread(() =>
            {
                var icon = new Fluent.FontIcon();
                Assert.IsTrue(icon.EnableTransitions);
            });
        }

        [TestMethod]
        public void Stage3_TextBox_CharacterCounter_ShowsWithMaxLength()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                var window = new Window();
                var textBox = new Fluent.TextBox
                {
                    Width = 260,
                    MaxLength = 40,
                    Text = "Hi"
                };

                try
                {
                    window.Content = textBox;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var counter = textBox.Template.FindName("PART_CharacterCounter", textBox) as TextBlock;
                    Assert.IsNotNull(counter);
                    Assert.AreEqual("2/40", counter.Text);
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void Stage3_ListView_EmptyContent_VisibleWhenNoItems()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                var window = new Window();
                var list = new Fluent.ListView
                {
                    Width = 200,
                    Height = 100,
                    EmptyContent = new TextBlock { Text = "Empty" }
                };

                try
                {
                    window.Content = list;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.IsFalse(list.HasItems);
                    Assert.IsTrue(FindVisualChildren<TextBlock>(list).Any(tb => tb.Text == "Empty"));
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void Stage3_ProgressBar_Template_HasTrackAndFill()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                var window = new Window();
                var bar = new Fluent.ProgressBar { Width = 200, Height = 8, Value = 40, Maximum = 100 };

                try
                {
                    window.Content = bar;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.IsNotNull(bar.Template.FindName("PART_Track", bar));
                    Assert.IsNotNull(bar.Template.FindName("PART_Fill", bar));
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void Slider_Template_HasTrack()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                var window = new Window();
                var slider = new Fluent.Slider { Width = 220, Minimum = 0, Maximum = 100, Value = 30 };

                try
                {
                    window.Content = slider;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.IsNotNull(slider.Template.FindName("PART_Track", slider), "Slider template should contain PART_Track.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void MainWindow_ProgressSlider_UsesFluentSliderAndUpdatesLabel()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                MainWindow window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SelectMainWindowNavPage(window, window.Dispatcher, "Status");

                    var slider = FindVisualChildByName<Fluent.Slider>(window, "ProgressSlider");
                    var label = FindVisualChildByName<TextBlock>(window, "SliderValueLabel");
                    Assert.IsNotNull(slider, "ProgressSlider should be a Fluent.Slider control.");
                    Assert.IsNotNull(label, "Slider value label should exist in progress tab.");

                    slider.Value = 73;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual("Value: 73", label.Text);
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void MainWindow_SelectionDemoCombo_SelectionUpdatesIndex()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                MainWindow window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SelectMainWindowNavPage(window, window.Dispatcher, "Selection");

                    var combo = FindVisualChildByName<Fluent.ComboBox>(window, "SelectionDemoCombo");
                    Assert.IsNotNull(combo, "SelectionDemoCombo should exist on the Selection page.");
                    Assert.AreEqual(3, combo.Items.Count, "SelectionDemoCombo should list three items.");

                    combo.SelectedIndex = 1;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(1, combo.SelectedIndex, "SelectionDemoCombo selection should persist after layout.");
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void HyperlinkButton_DefaultForeground_IsAccent()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                var window = new Window();
                var button = new Fluent.HyperlinkButton
                {
                    Content = "Link",
                    Width = 120
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var accentBrush = application.Resources["AccentTextFillColorPrimaryBrush"] as SolidColorBrush;
                    Assert.IsNotNull(accentBrush);
                    Assert.IsInstanceOfType(button.Foreground, typeof(SolidColorBrush));
                    Assert.AreEqual(accentBrush.Color, ((SolidColorBrush)button.Foreground).Color,
                        "HyperlinkButton default foreground should be the accent text brush.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void HyperlinkButton_Click_WithNavigateUri_DoesNotThrow()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();
                var button = new Fluent.HyperlinkButton
                {
                    Content = "Link",
                    Width = 120
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                    DrainDispatcher(window.Dispatcher);
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void InfoBar_ErrorSeverity_HasExpectedBackground()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                var window = new Window();
                var infoBar = new Fluent.InfoBar
                {
                    Severity = InfoBarSeverity.Error,
                    Title = "Error",
                    Message = "Something went wrong.",
                    IsOpen = true
                };

                try
                {
                    window.Content = infoBar;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var expectedBrush = application.Resources["SystemFillColorCriticalBackgroundBrush"] as Brush;
                    Assert.IsNotNull(expectedBrush, "SystemFillColorCriticalBackgroundBrush should be defined.");

                    infoBar.ApplyTemplate();
                    var rootBorder = infoBar.Template.FindName("RootBorder", infoBar) as System.Windows.Controls.Border;
                    Assert.IsNotNull(rootBorder);
                    Assert.IsNotNull(rootBorder.Background, "InfoBar Error severity should have a non-null background.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void InfoBar_CloseButton_SetsIsOpenFalse()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();
                var infoBar = new Fluent.InfoBar
                {
                    IsClosable = true,
                    IsOpen = true,
                    Title = "Closable"
                };

                try
                {
                    window.Content = infoBar;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    infoBar.ApplyTemplate();
                    var closeButton = infoBar.Template.FindName("PART_CloseButton", infoBar) as Button;
                    Assert.IsNotNull(closeButton);

                    var peer = new ButtonAutomationPeer(closeButton);
                    var invokeProvider = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                    Assert.IsNotNull(invokeProvider);

                    invokeProvider.Invoke();
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsFalse(infoBar.IsOpen, "Clicking the close button should set IsOpen to false.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void InfoBar_ClosingCancel_PreventsClose()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();
                var infoBar = new Fluent.InfoBar
                {
                    IsClosable = true,
                    IsOpen = true,
                    Title = "Cancelable"
                };

                infoBar.Closing += (sender, args) => { args.Cancel = true; };

                try
                {
                    window.Content = infoBar;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    infoBar.ApplyTemplate();
                    var closeButton = infoBar.Template.FindName("PART_CloseButton", infoBar) as Button;
                    Assert.IsNotNull(closeButton);

                    var peer = new ButtonAutomationPeer(closeButton);
                    var invokeProvider = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                    Assert.IsNotNull(invokeProvider);

                    invokeProvider.Invoke();
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsTrue(infoBar.IsOpen, "Canceling the Closing event should keep IsOpen true.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void RadioButton_Checked_HasAccentFill()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
                var window = new Window();
                var radio = new Fluent.RadioButton
                {
                    Content = "Test",
                    IsChecked = true
                };

                try
                {
                    window.Content = radio;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    radio.ApplyTemplate();
                    var checkedEllipse = radio.Template.FindName("CheckedEllipse", radio) as System.Windows.Shapes.Ellipse;
                    var accentBrush = application.Resources["AccentFillColorDefaultBrush"] as SolidColorBrush;

                    Assert.IsNotNull(checkedEllipse);
                    Assert.IsNotNull(accentBrush);
                    Assert.AreEqual(1.0, checkedEllipse.Opacity, "CheckedEllipse should be visible when IsChecked is true.");
                    Assert.IsInstanceOfType(checkedEllipse.Fill, typeof(SolidColorBrush));
                    Assert.AreEqual(accentBrush.Color, ((SolidColorBrush)checkedEllipse.Fill).Color,
                        "Checked radio button indicator should use the accent fill brush.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void RadioButton_GroupExclusivity_UnchecksOthers()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();
                var panel = new StackPanel();
                var radio1 = new Fluent.RadioButton { Content = "A", GroupName = "TestGroup", IsChecked = true };
                var radio2 = new Fluent.RadioButton { Content = "B", GroupName = "TestGroup" };
                var radio3 = new Fluent.RadioButton { Content = "C", GroupName = "TestGroup" };
                panel.Children.Add(radio1);
                panel.Children.Add(radio2);
                panel.Children.Add(radio3);

                try
                {
                    window.Content = panel;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.IsTrue(radio1.IsChecked == true);

                    radio2.IsChecked = true;
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsFalse(radio1.IsChecked == true, "Radio1 should be unchecked after Radio2 is checked.");
                    Assert.IsTrue(radio2.IsChecked == true);
                    Assert.IsFalse(radio3.IsChecked == true);
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void ToggleSwitch_OnOffContent_SwapsOnCheck()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();
                var toggle = new Fluent.ToggleSwitch
                {
                    OnContent = "On",
                    OffContent = "Off",
                    IsChecked = false
                };

                try
                {
                    window.Content = toggle;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    toggle.ApplyTemplate();
                    var offPresenter = toggle.Template.FindName("OffContentPresenter", toggle) as FrameworkElement;
                    var onPresenter = toggle.Template.FindName("OnContentPresenter", toggle) as FrameworkElement;
                    Assert.IsNotNull(offPresenter);
                    Assert.IsNotNull(onPresenter);
                    Assert.AreEqual(Visibility.Visible, offPresenter.Visibility, "Off content should be visible when unchecked.");
                    Assert.AreEqual(Visibility.Collapsed, onPresenter.Visibility, "On content should be collapsed when unchecked.");

                    toggle.IsChecked = true;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(Visibility.Collapsed, offPresenter.Visibility, "Off content should be collapsed when checked.");
                    Assert.AreEqual(Visibility.Visible, onPresenter.Visibility, "On content should be visible when checked.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void ToggleSwitch_IsChecked_TogglesOnClick()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();
                var toggle = new Fluent.ToggleSwitch { IsChecked = false };

                try
                {
                    window.Content = toggle;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.IsFalse(toggle.IsChecked == true, "ToggleSwitch should start unchecked.");

                    var peer = new System.Windows.Automation.Peers.ToggleButtonAutomationPeer(toggle);
                    var toggleProvider = (System.Windows.Automation.Provider.IToggleProvider)peer;
                    toggleProvider.Toggle();
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsTrue(toggle.IsChecked == true, "ToggleSwitch should be checked after toggle.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void ProgressRing_Determinate_UpdatesArc()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();
                var ring = new Fluent.ProgressRing
                {
                    IsIndeterminate = false,
                    Width = 48,
                    Height = 48,
                    Value = 50,
                    Minimum = 0,
                    Maximum = 100,
                    IsActive = true
                };

                try
                {
                    window.Content = ring;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ring.ApplyTemplate();
                    var arcPath = ring.Template.FindName("PART_DeterminateArc", ring) as System.Windows.Shapes.Path;
                    Assert.IsNotNull(arcPath, "PART_DeterminateArc should exist in the template.");
                    Assert.IsNotNull(arcPath.Data, "Determinate arc should have non-null Data at 50%.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void ProgressRing_Indeterminate_DotHostBecomesVisible()
        {
            // The 2026-04-27 ProgressRing rewrite replaced the single PART_IndeterminateRing
            // ellipse + StrokeDashArray approximation with the WinUI canonical 5-dot orbit
            // (E1…E5), wrapped in a "DotHost" Grid that flips to Visible via a MultiTrigger
            // on (IsActive=True, IsIndeterminate=True).
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                var window = new Window();
                var ring = new Fluent.ProgressRing
                {
                    IsIndeterminate = true,
                    Width = 48,
                    Height = 48,
                    IsActive = true
                };

                try
                {
                    window.Content = ring;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ring.ApplyTemplate();
                    var dotHost = ring.Template.FindName("DotHost", ring) as FrameworkElement;
                    Assert.IsNotNull(dotHost, "DotHost Grid should exist in the indeterminate template.");
                    Assert.AreEqual(Visibility.Visible, dotHost.Visibility,
                        "DotHost should be Visible when IsActive=True and IsIndeterminate=True.");

                    var firstDot = ring.Template.FindName("E1", ring) as System.Windows.Shapes.Ellipse;
                    Assert.IsNotNull(firstDot, "Orbit-dot E1 should exist in the indeterminate template.");
                }
                finally
                {
                    window.Close();
                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void DemoMainWindow_SelectingNavPage_DoesNotThrow()
        {
            RunOnStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);

                ApplicationThemeManager.Apply(ApplicationTheme.Auto, BackdropType.Auto, true);
                ApplicationAccentColorManager.ApplySystemAccent();

                MainWindow window = null;

                try
                {
                    window = new MainWindow();
                    window.Show();
                    window.UpdateLayout();

                    var nav = window.FindName("DemoNav") as Fluent.NavigationView;
                    Assert.IsNotNull(nav);

                    SelectMainWindowNavPage(window, window.Dispatcher, "Buttons");
                    Assert.IsNotNull(nav.SelectedItem);
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    application.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        private static void SelectMainWindowNavPage(MainWindow window, Dispatcher dispatcher, string itemContent)
        {
            var nav = window.FindName("DemoNav") as Fluent.NavigationView;
            Assert.IsNotNull(nav, "Main window should expose DemoNav.");

            foreach (var obj in nav.Items)
            {
                var nvi = obj as Fluent.NavigationViewItem;
                if (nvi != null && string.Equals(nvi.Content as string, itemContent, StringComparison.Ordinal))
                {
                    nav.SelectedItem = nvi;
                    DrainDispatcher(dispatcher);
                    dispatcher.Invoke(new Action(delegate { }), DispatcherPriority.Loaded);
                    dispatcher.Invoke(new Action(delegate { }), DispatcherPriority.ContextIdle);
                    window.UpdateLayout();
                    DrainDispatcher(dispatcher);
                    return;
                }
            }

            Assert.Fail(string.Format("Navigation item '{0}' should exist.", itemContent));
        }

        private static void AssertButtonShowsGlyph(Fluent.Button button, string glyph)
        {
            var glyphTextBlock = FindButtonGlyphTextBlock(button, glyph);
            Assert.IsNotNull(glyphTextBlock, "Expected button glyph was not found in the visual tree.");
            Assert.IsTrue(glyphTextBlock.IsVisible, "Expected button glyph should be visible.");
            Assert.IsTrue(glyphTextBlock.ActualWidth > 0, "Expected button glyph should occupy layout space.");
        }

        private static TextBlock FindButtonIconTextBlock(Fluent.Button button)
        {
            foreach (var textBlock in FindVisualChildren<TextBlock>(button))
            {
                var fontFamily = textBlock.FontFamily;
                if (fontFamily != null &&
                    fontFamily.Source != null &&
                    fontFamily.Source.IndexOf("Segoe Fluent Icons", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return textBlock;
                }
            }

            return null;
        }

        private static TextBlock FindButtonGlyphTextBlock(Fluent.Button button, string glyph)
        {
            foreach (var textBlock in FindVisualChildren<TextBlock>(button))
            {
                if (string.Equals(textBlock.Text, glyph, StringComparison.Ordinal))
                {
                    return textBlock;
                }
            }

            return null;
        }

        private static void AssertGlyphWithinButtonBounds(Window window, Fluent.Button button, string glyph)
        {
            var glyphTextBlock = FindButtonGlyphTextBlock(button, glyph);

            Assert.IsNotNull(glyphTextBlock, "Expected button glyph was not found in the visual tree.");
            Assert.IsTrue(glyphTextBlock.IsVisible, "Expected button glyph should be visible.");
            Assert.IsTrue(glyphTextBlock.ActualWidth > 0, "Expected button glyph should occupy layout space.");

            var buttonOrigin = button.TransformToAncestor(window).Transform(new Point(0, 0));
            var glyphOrigin = glyphTextBlock.TransformToAncestor(window).Transform(new Point(0, 0));
            var buttonRight = buttonOrigin.X + button.ActualWidth;
            var buttonBottom = buttonOrigin.Y + button.ActualHeight;
            var glyphRight = glyphOrigin.X + glyphTextBlock.ActualWidth;
            var glyphBottom = glyphOrigin.Y + glyphTextBlock.ActualHeight;

            Assert.IsTrue(glyphOrigin.X >= buttonOrigin.X - 0.5, "Expected button glyph should not render left of the button.");
            Assert.IsTrue(glyphOrigin.Y >= buttonOrigin.Y - 0.5, "Expected button glyph should not render above the button.");
            Assert.IsTrue(glyphRight <= buttonRight + 0.5, "Expected button glyph should not render right of the button.");
            Assert.IsTrue(glyphBottom <= buttonBottom + 0.5, "Expected button glyph should not render below the button.");
        }

        private static Fluent.Button FindFluentButtonByContent(DependencyObject root, string content)
        {
            foreach (var button in FindVisualChildren<Fluent.Button>(root))
            {
                if (string.Equals(button.Content as string, content, StringComparison.Ordinal))
                {
                    return button;
                }
            }

            return null;
        }

        private static void AssertContentGroupIsCentered(Window window, Fluent.Button button, string content, string glyph)
        {
            var glyphTextBlock = FindButtonGlyphTextBlock(button, glyph);
            Assert.IsNotNull(glyphTextBlock, "Expected button glyph was not found in the visual tree.");

            ContentPresenter textPresenter = null;
            foreach (var presenter in FindVisualChildren<ContentPresenter>(button))
            {
                if (string.Equals(presenter.Content as string, content, StringComparison.Ordinal))
                {
                    textPresenter = presenter;
                    break;
                }
            }

            Assert.IsNotNull(textPresenter, "Expected button content presenter was not found in the visual tree.");

            var buttonOrigin = button.TransformToAncestor(window).Transform(new Point(0, 0));
            var buttonCenter = buttonOrigin.X + (button.ActualWidth / 2.0);

            var glyphOrigin = glyphTextBlock.TransformToAncestor(window).Transform(new Point(0, 0));
            var contentOrigin = textPresenter.TransformToAncestor(window).Transform(new Point(0, 0));
            var groupLeft = Math.Min(glyphOrigin.X, contentOrigin.X);
            var groupRight = Math.Max(glyphOrigin.X + glyphTextBlock.ActualWidth, contentOrigin.X + textPresenter.ActualWidth);
            var groupCenter = groupLeft + ((groupRight - groupLeft) / 2.0);

            Assert.AreEqual(buttonCenter, groupCenter, 1.0, "Button icon and text should stay centered as a single content group.");
        }
    }
}
