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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [TestMethod]
        public void GlyphButtons_InPickersAndSpinners_HaveAutomationNames()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Controls.NumberBox numberBox = new()
                {
                    SpinButtonPlacementMode = Fluence.Wpf.SpinButtonPlacementMode.Inline,
                    Width = 160,
                };
                Window window = new() { Content = numberBox, Width = 240, Height = 80 };

                try
                {
                    window.Show();
                    _ = numberBox.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    foreach ((string part, string expectedName) in new[]
                    {
                        ("PART_UpButton", "Increase"),
                        ("PART_DownButton", "Decrease"),
                    })
                    {
                        FrameworkElement? btn = FindVisualChildByName<FrameworkElement>(numberBox, part);
                        Assert.IsNotNull(btn, $"{part} should exist in the NumberBox template.");
                        string actualName = AutomationProperties.GetName(btn);
                        Assert.IsTrue(
                            string.Equals(expectedName, actualName, System.StringComparison.Ordinal),
                            $"{part} must expose accessible name for Narrator. Expected: '{expectedName}', actual: '{actualName}'.");
                    }
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void FluenceWindow_CaptionButtons_HaveAutomationNames()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Controls.FluenceWindow window = new();

                try
                {
                    window.Show();
                    _ = window.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    foreach ((string part, string expectedName) in new[]
                    {
                        ("PART_MinimizeButton", "Minimize"),
                        ("PART_CloseButton", "Close"),
                    })
                    {
                        FrameworkElement? button = FindVisualChildByName<FrameworkElement>(window, part);
                        Assert.IsNotNull(button, $"{part} should exist in the FluenceWindow template.");
                        string actualName = AutomationProperties.GetName(button);
                        Assert.IsTrue(
                            string.Equals(expectedName, actualName, System.StringComparison.Ordinal),
                            $"{part} must expose an accessible name for Narrator. Expected: '{expectedName}', actual: '{actualName}'.");
                    }
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void AutoSuggestBox_QueryButton_HasAutomationName()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                // QueryIcon must be non-null so the template trigger does not clear the icon
                // slot; the button is only wired into the visual tree while QueryIcon is set.
                Controls.AutoSuggestBox autoSuggestBox = new()
                {
                    Width = 200,
                    QueryIcon = new Controls.FontIcon { Glyph = "" },
                };
                Window window = new() { Content = autoSuggestBox, Width = 300, Height = 80 };

                try
                {
                    window.Show();
                    _ = autoSuggestBox.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    ControlTemplate? template = autoSuggestBox.Template;
                    Assert.IsNotNull(template, "AutoSuggestBox must receive its themed template.");
                    FrameworkElement? queryButton = template.FindName("PART_QueryButton", autoSuggestBox) as FrameworkElement;
                    Assert.IsNotNull(queryButton, "PART_QueryButton should exist in the AutoSuggestBox template.");
                    string actualName = AutomationProperties.GetName(queryButton);
                    Assert.IsTrue(
                        string.Equals("Search", actualName, System.StringComparison.Ordinal),
                        $"PART_QueryButton must expose accessible name 'Search' for Narrator. Actual: '{actualName}'.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void DatePicker_AcceptCancelButtons_HaveAutomationNames()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Controls.DatePicker picker = new() { Width = 220 };
                Window window = new() { Content = picker, Width = 300, Height = 120 };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "DatePicker must receive its themed template.");

                    foreach ((string part, string expectedName) in new[]
                    {
                        ("PART_AcceptButton", "Accept"),
                        ("PART_CancelButton", "Cancel"),
                    })
                    {
                        FrameworkElement? btn = template.FindName(part, picker) as FrameworkElement;
                        Assert.IsNotNull(btn, $"{part} should exist in the DatePicker template.");
                        string actualName = AutomationProperties.GetName(btn);
                        Assert.IsTrue(
                            string.Equals(expectedName, actualName, System.StringComparison.Ordinal),
                            $"{part} must expose accessible name '{expectedName}' for Narrator. Actual: '{actualName}'.");
                    }
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void TimePicker_AcceptCancelButtons_HaveAutomationNames()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Controls.TimePicker picker = new() { Width = 220 };
                Window window = new() { Content = picker, Width = 300, Height = 120 };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ControlTemplate? template = picker.Template;
                    Assert.IsNotNull(template, "TimePicker must receive its themed template.");

                    foreach ((string part, string expectedName) in new[]
                    {
                        ("PART_AcceptButton", "Accept"),
                        ("PART_CancelButton", "Cancel"),
                    })
                    {
                        FrameworkElement? btn = template.FindName(part, picker) as FrameworkElement;
                        Assert.IsNotNull(btn, $"{part} should exist in the TimePicker template.");
                        string actualName = AutomationProperties.GetName(btn);
                        Assert.IsTrue(
                            string.Equals(expectedName, actualName, System.StringComparison.Ordinal),
                            $"{part} must expose accessible name '{expectedName}' for Narrator. Actual: '{actualName}'.");
                    }
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }
    }
}
