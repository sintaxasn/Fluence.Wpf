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
using System.Windows;
using Fluence.Wpf.Specs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        private static DialogSpec BuildSpecUserFlowDialog()
        {
            DialogSpec dialog = new()
            {
                Title = "Contoso IT",
            };
            dialog.Content.Add(new TextBlockSpec { Text = "Before we upgrade, tell us where you sit." });
            TextBoxSpec desk = new() { Name = "Desk", PlaceholderText = "Desk number" };
            desk.Rules.Add(new NotEmptyRule());
            dialog.Content.Add(desk);
            ComboBoxSpec site = new() { Name = "Site", SelectedItem = "Sydney" };
            site.Items.Add("Sydney");
            site.Items.Add("Melbourne");
            site.Items.Add("Auckland");
            dialog.Content.Add(site);
            dialog.Content.Add(new CheckBoxSpec { Name = "Vpn", Content = "I use VPN daily" });
            dialog.Buttons.Add(new ButtonSpec { Text = "Continue", IsDefault = true });
            dialog.Buttons.Add(new ButtonSpec { Text = "Defer", IsCancel = true });
            return dialog;
        }

        [TestMethod]
        public void SpecMaterializer_MaterializesUserFlowTree()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);

                SpecDialogWindow window = SpecMaterializer.Materialize(BuildSpecUserFlowDialog());
                window.Show();
                DrainDispatcher(window.Dispatcher);
                try
                {
                    Assert.AreEqual("Contoso IT", window.Title);
                    List<Controls.TextBox> textBoxes = [.. WpfTestSta.FindVisualDescendants<Controls.TextBox>(window)];
                    Assert.AreEqual(1, textBoxes.Count, "one TextBox expected");
                    Assert.AreEqual("Desk number", textBoxes[0].PlaceholderText);

                    Controls.ComboBox combo = WpfTestSta.FindVisualDescendants<Controls.ComboBox>(window).Single();
                    Assert.AreEqual(3, combo.Items.Count);
                    Assert.AreEqual("Sydney", combo.SelectedItem);

                    _ = WpfTestSta.FindVisualDescendants<Controls.CheckBox>(window).Single();
                    _ = WpfTestSta.FindVisualDescendants<Controls.InfoBar>(window).Single();

                    List<Controls.Button> buttons = [.. WpfTestSta.FindVisualDescendants<Controls.Button>(window).Where(static button => button.IsDefault || button.IsCancel)];
                    Assert.AreEqual(2, buttons.Count, "two dialog buttons expected");
                    Assert.AreEqual(ControlAppearance.Accent, buttons.Single(static button => button.IsDefault).Appearance);
                }
                finally
                {
                    window.Close();
                    DrainDispatcher(window.Dispatcher);
                }
            });
        }

        [TestMethod]
        public void SpecDialogWindow_RulesBlockCommit_UntilValid()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);

                SpecDialogWindow window = SpecMaterializer.Materialize(BuildSpecUserFlowDialog());
                window.Show();
                DrainDispatcher(window.Dispatcher);
                try
                {
                    Controls.InfoBar validationBar = WpfTestSta.FindVisualDescendants<Controls.InfoBar>(window).Single();
                    Controls.Button defaultButton = WpfTestSta.FindVisualDescendants<Controls.Button>(window).Single(static button => button.IsDefault);
                    Assert.IsFalse(validationBar.IsOpen, "validation bar starts closed");

                    // Empty required Desk: commit is blocked and the InfoBar opens with the failure.
                    defaultButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                    DrainDispatcher(window.Dispatcher);
                    Assert.IsTrue(validationBar.IsOpen, "validation bar opens on rule failure");
                    StringAssert.Contains(validationBar.Message, "Desk", StringComparison.Ordinal);
                    Assert.IsTrue(window.IsVisible, "window stays open on rule failure");

                    // Valid input: the same button closes the dialog.
                    Controls.TextBox desk = WpfTestSta.FindVisualDescendants<Controls.TextBox>(window).Single();
                    desk.Text = "42A";
                    defaultButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                    DrainDispatcher(window.Dispatcher);
                    Assert.IsFalse(window.IsVisible, "window closes once rules pass");
                }
                finally
                {
                    if (window.IsVisible)
                    {
                        window.Close();
                    }
                    DrainDispatcher(window.Dispatcher);
                }
            });
        }

        [TestMethod]
        public void SpecMaterializer_HarvestsTypedValues_WithModuleParitySemantics()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);

                List<KeyValuePair<SpecNode, FrameworkElement>> pairs = [];
                TextBoxSpec deskSpec = new() { Name = "Desk" };
                ComboBoxSpec siteSpec = new() { Name = "Site" };
                siteSpec.Items.Add("Sydney");
                CheckBoxSpec vpnSpec = new() { Name = "Vpn" };
                PasswordBoxSpec secretSpec = new() { Name = "Secret" };
                NumberBoxSpec countSpec = new() { Name = "Count" };
                StackPanelSpec radios = new();
                radios.Children.Add(new RadioButtonSpec { GroupName = "Fruit", Content = "Apple", IsChecked = true });
                radios.Children.Add(new RadioButtonSpec { GroupName = "Fruit", Content = "Pear" });

                Controls.TextBox desk = (Controls.TextBox)SpecMaterializer.CreateElementTracked(deskSpec, pairs);
                Controls.ComboBox site = (Controls.ComboBox)SpecMaterializer.CreateElementTracked(siteSpec, pairs);
                Controls.CheckBox vpn = (Controls.CheckBox)SpecMaterializer.CreateElementTracked(vpnSpec, pairs);
                Controls.PasswordBox secret = (Controls.PasswordBox)SpecMaterializer.CreateElementTracked(secretSpec, pairs);
                _ = SpecMaterializer.CreateElementTracked(countSpec, pairs);
                _ = SpecMaterializer.CreateElementTracked(radios, pairs);

                desk.Text = "42A";
                site.SelectedItem = "Sydney";
                vpn.IsChecked = true;
                secret.Password = "hunter2";

                Dictionary<string, object?> values = new(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<SpecNode, FrameworkElement> pair in pairs)
                {
                    SpecMaterializer.HarvestValue(pair.Key, pair.Value, values);
                }

                Assert.AreEqual("42A", values["Desk"]);
                Assert.AreEqual("Sydney", values["Site"]);
                Assert.IsTrue(values["Vpn"] is true, "checked CheckBox harvests true");
                Assert.AreEqual("hunter2", values["Secret"], "PasswordBox harvests by pull at commit");
                _ = Assert.IsInstanceOfType<double>(values["Count"], "untouched NumberBox harvests its live numeric value (0), not null");
                Assert.AreEqual("Apple", values["Fruit"], "checked radio's Content harvests under the GroupName key");
            });
        }

        [TestMethod]
        public void SpecMaterializer_AppliesCommonProperties_AndParsesThickness()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);

                List<KeyValuePair<SpecNode, FrameworkElement>> pairs = [];
                TextBlockSpec spec = new()
                {
                    Text = "hello",
                    Margin = "8,4",
                    IsEnabled = false,
                    Width = 200,
                    MinWidth = 120,
                };
                FrameworkElement element = SpecMaterializer.CreateElementTracked(spec, pairs);

                Assert.AreEqual(new Thickness(8, 4, 8, 4), element.Margin);
                Assert.IsFalse(element.IsEnabled);
                Assert.AreEqual(200, element.Width);
                Assert.AreEqual(120, element.MinWidth);
                Assert.AreEqual(1, pairs.Count);

                _ = Assert.ThrowsExactly<FormatException>(static () => SpecMaterializer.ParseThickness("1,2,3"));
                _ = Assert.ThrowsExactly<FormatException>(static () => SpecMaterializer.ParseThickness("abc"));
            });
        }

        [TestMethod]
        public void SpecDialogWindow_SurvivesStandardThemeCycle()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);

                SpecDialogWindow window = SpecMaterializer.Materialize(BuildSpecUserFlowDialog());
                window.Show();
                DrainDispatcher(window.Dispatcher);
                try
                {
                    ThemeTestHelpers.ApplyStandardThemeCycle();
                    DrainDispatcher(window.Dispatcher);
                    ThemeTestHelpers.AssertKeyThemeBrushesResolve(application);
                    Assert.IsTrue(window.IsVisible);
                }
                finally
                {
                    window.Close();
                    DrainDispatcher(window.Dispatcher);
                }
            });
        }
    }
}
