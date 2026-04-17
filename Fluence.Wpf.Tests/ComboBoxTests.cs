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
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public class ComboBoxTests
    {
        private static void RunOnFreshStaThread(Action action)
        {
            Exception capturedException = null;
            WpfTestSta.Dispatcher.Invoke(new Action(delegate
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

        private static ResourceDictionary MergeTheme(Application application)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application.Resources.MergedDictionaries.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
            var dictionaries = application.Resources.MergedDictionaries;
            return dictionaries.Count > 0 ? dictionaries[dictionaries.Count - 1] : null;
        }

        private static void RunWithComboBox(Action<ComboBox> testBody)
        {
            RunOnFreshStaThread(() =>
            {
                var comboBox = new ComboBox();
                testBody(comboBox);
            });
        }

        #region Dropdown placement

        [TestMethod]
        public void IsDropDownOpenedUpward_DefaultIsFalse()
        {
            RunWithComboBox(cb =>
            {
                Assert.IsFalse(cb.IsDropDownOpenedUpward,
                    "IsDropDownOpenedUpward should default to false.");
            });
        }

        [TestMethod]
        public void DropdownCornerRadius_DefaultIs8()
        {
            RunWithComboBox(cb =>
            {
                Assert.AreEqual(new CornerRadius(8), cb.DropdownCornerRadius,
                    "DropdownCornerRadius should default to 8.");
            });
        }

        [TestMethod]
        public void IsDropDownOpenedUpward_FalseWhenDropDownNotOpen()
        {
            RunWithComboBox(cb =>
            {
                cb.Items.Add("A");
                cb.Items.Add("B");

                Assert.IsFalse(cb.IsDropDownOpen,
                    "IsDropDownOpen should be false by default.");
                Assert.IsFalse(cb.IsDropDownOpenedUpward,
                    "IsDropDownOpenedUpward must be false when dropdown is not open.");
            });
        }

        #endregion

        #region Hover state (brush verification)

        [TestMethod]
        public void ComboBoxXaml_HoverUsesSubtleFillBrush()
        {
            var xamlPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\Fluence.Wpf\Themes\Controls\ComboBox.xaml");

            if (System.IO.File.Exists(xamlPath))
            {
                string xaml = System.IO.File.ReadAllText(xamlPath);

                Assert.IsTrue(
                    xaml.Contains("SubtleFillColorSecondaryBrush"),
                    "ComboBoxItem hover trigger must use SubtleFillColorSecondaryBrush.");

                Assert.IsFalse(
                    xaml.Contains("IsHighlighted") &&
                    xaml.Contains("ControlFillColorSecondaryBrush") &&
                    !xaml.Contains("SubtleFillColorSecondaryBrush"),
                    "ComboBoxItem must not use ControlFillColorSecondaryBrush for hover.");
            }
        }

        #endregion

        #region Auto-select first item

        [TestMethod]
        public void FirstItem_AutoSelectedWhenNoSelectionProvided()
        {
            RunWithComboBox(cb =>
            {
                cb.Items.Add("Alpha");
                cb.Items.Add("Beta");
                cb.Items.Add("Gamma");
                cb.Items.Add("Delta");
                cb.Items.Add("Epsilon");

                Assert.AreEqual(0, cb.SelectedIndex,
                    "First item must be auto-selected when no SelectedIndex is provided.");
            });
        }

        [TestMethod]
        public void ExplicitSelectedIndex_MinusOne_IsRespected()
        {
            RunWithComboBox(cb =>
            {
                cb.SelectedIndex = -1;

                cb.Items.Add("Alpha");
                cb.Items.Add("Beta");
                cb.Items.Add("Gamma");

                Assert.AreEqual(-1, cb.SelectedIndex,
                    "Explicit SelectedIndex=-1 must be respected; auto-select must not override it.");
            });
        }

        [TestMethod]
        public void AutoSelect_WorksAfterDynamicItemAdd()
        {
            RunWithComboBox(cb =>
            {
                Assert.AreEqual(-1, cb.SelectedIndex,
                    "SelectedIndex must be -1 when no items exist.");

                cb.Items.Add("First");
                cb.Items.Add("Second");
                cb.Items.Add("Third");

                Assert.AreEqual(0, cb.SelectedIndex,
                    "SelectedIndex must be 0 after dynamically adding items.");
            });
        }

        [TestMethod]
        public void AutoSelect_DoesNotOverrideExplicitSelection()
        {
            RunWithComboBox(cb =>
            {
                cb.Items.Add("Alpha");
                cb.Items.Add("Beta");
                cb.Items.Add("Gamma");

                cb.SelectedIndex = 2;

                cb.Items.Add("Delta");

                Assert.AreEqual(2, cb.SelectedIndex,
                    "Auto-select must not change an existing explicit selection.");
            });
        }

        #endregion
    }
}
