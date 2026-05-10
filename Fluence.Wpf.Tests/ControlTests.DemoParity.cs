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
using System.Windows.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf.Demo.Pages;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [TestMethod]
        public void GalleryInputsPage_SliderSamplesIncludeHorizontalAndVerticalTicks()
        {
            RunDemoPageTest(() => new GalleryInputsPage(), window =>
            {
                Controls.Slider? horizontal = FindVisualChildByName<Controls.Slider>(window, "HorizontalTickSlider");
                Controls.Slider? vertical = FindVisualChildByName<Controls.Slider>(window, "VerticalTickSlider");

                Assert.IsNotNull(horizontal, "Inputs page should include a named horizontal slider with tick marks.");
                Assert.IsNotNull(vertical, "Inputs page should include a named vertical slider with tick marks.");
                Assert.AreNotEqual(System.Windows.Controls.Primitives.TickPlacement.None, horizontal.TickPlacement);
                Assert.AreNotEqual(System.Windows.Controls.Primitives.TickPlacement.None, vertical.TickPlacement);
                Assert.IsTrue(horizontal.TickFrequency > 0);
                Assert.IsTrue(vertical.TickFrequency > 0);
            });
        }

        [TestMethod]
        public void GalleryButtonsPage_RepeatButtonIncrementsNearbyCountText()
        {
            RunDemoPageTest(() => new GalleryButtonsPage(), window =>
            {
                Controls.RepeatButton? button = FindVisualChildByName<Controls.RepeatButton>(window, "RepeatCounterButton");
                TextBlock? count = FindVisualChildByName<TextBlock>(window, "RepeatButtonCountText");

                Assert.IsNotNull(button, "Buttons page should include a repeat button wired to a count label.");
                Assert.IsNotNull(count, "Buttons page should include a nearby repeat count text block.");
                Assert.AreEqual("Clicks: 0", count.Text);

                button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                DrainDispatcher(window.Dispatcher);

                Assert.AreEqual("Clicks: 2", count.Text);
            });
        }

        [TestMethod]
        public void GallerySelectionPage_CheckBoxSamplesMatchWinUIGalleryStates()
        {
            RunDemoPageTest(() => new GallerySelectionPage(), window =>
            {
                Controls.CheckBox? twoState = FindVisualChildByName<Controls.CheckBox>(window, "TwoStateCheckBox");
                Controls.CheckBox? threeState = FindVisualChildByName<Controls.CheckBox>(window, "ThreeStateCheckBox");
                Controls.CheckBox? selectAll = FindVisualChildByName<Controls.CheckBox>(window, "SelectAllCheckBox");
                Controls.CheckBox? optionOne = FindVisualChildByName<Controls.CheckBox>(window, "OptionOneCheckBox");
                Controls.CheckBox? optionTwo = FindVisualChildByName<Controls.CheckBox>(window, "OptionTwoCheckBox");
                Controls.CheckBox? optionThree = FindVisualChildByName<Controls.CheckBox>(window, "OptionThreeCheckBox");

                Assert.IsNotNull(twoState, "Selection page should include a two-state CheckBox example.");
                Assert.IsFalse(twoState.IsThreeState);
                Assert.IsNotNull(threeState, "Selection page should include a three-state CheckBox example.");
                Assert.IsTrue(threeState.IsThreeState);
                Assert.IsNotNull(selectAll, "Selection page should include a tri-state Select All CheckBox.");
                Assert.IsNotNull(optionOne);
                Assert.IsNotNull(optionTwo);
                Assert.IsNotNull(optionThree);

                selectAll.IsChecked = true;
                DrainDispatcher(window.Dispatcher);

                Assert.IsTrue(optionOne.IsChecked.GetValueOrDefault());
                Assert.IsTrue(optionTwo.IsChecked.GetValueOrDefault());
                Assert.IsTrue(optionThree.IsChecked.GetValueOrDefault());

                optionTwo.IsChecked = false;
                DrainDispatcher(window.Dispatcher);

                Assert.IsNull(selectAll.IsChecked,
                    "Select All should become indeterminate when only some child options are checked.");
            });
        }

        [TestMethod]
        public void GallerySelectionPage_RatingAndToggleProgressSamplesArePresent()
        {
            RunDemoPageTest(() => new GallerySelectionPage(), window =>
            {
                Controls.RatingControl? rating = FindVisualChildByName<Controls.RatingControl>(window, "RatingSample");
                Controls.RatingControl? readOnlyRating = FindVisualChildByName<Controls.RatingControl>(window, "ReadOnlyRatingSample");
                Controls.ToggleSwitch? toggle = FindVisualChildByName<Controls.ToggleSwitch>(window, "ToggleProgressSwitch");
                Controls.ProgressRing? ring = FindVisualChildByName<Controls.ProgressRing>(window, "ToggleProgressRing");

                Assert.IsNotNull(rating, "Selection page should include an editable RatingControl example.");
                Assert.IsNotNull(readOnlyRating, "Selection page should include a read-only RatingControl example.");
                Assert.IsNotNull(toggle, "Selection page should include the ToggleSwitch that controls progress.");
                Assert.IsNotNull(ring, "Selection page should include a ProgressRing bound to the ToggleSwitch.");

                toggle.IsChecked = false;
                DrainDispatcher(window.Dispatcher);
                Assert.IsFalse(ring.IsActive);

                toggle.IsChecked = true;
                DrainDispatcher(window.Dispatcher);
                Assert.IsTrue(ring.IsActive);
            });
        }

        [TestMethod]
        public void GalleryTreesPage_IncludesMultipleSelectionTreeView()
        {
            RunDemoPageTest(() => new GalleryTreesPage(), window =>
            {
                Controls.TreeView? treeView = FindVisualChildByName<Controls.TreeView>(window, "MultiSelectTreeView");

                Assert.IsNotNull(treeView, "Trees page should include a TreeView with multi-select checkboxes.");
                Assert.AreEqual(Fluence.Wpf.TreeViewSelectionMode.Multiple, treeView.SelectionMode);
            });
        }

        private static void RunDemoPageTest(Func<UserControl> createPage, Action<Window> verify)
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                UserControl page = createPage();
                Window window = new()
                {
                    Width = 900,
                    Height = 700,
                    Content = page
                };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    verify(window);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }
    }
}
