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
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf.Demo.Pages;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [TestMethod]
        public void GalleryButtonsPage_EnableCheckBoxControlsOnlyVisibleButtonVariants()
        {
            RunDemoPageTest(() => new GalleryButtonsPage(), window =>
            {
                Controls.CheckBox? enable = FindVisualChildByName<Controls.CheckBox>(window, "ButtonEnableCheckBox");
                Controls.Button? standard = FindFluentButtonByContent(window, "Standard");
                Controls.Button? accent = FindFluentButtonByContent(window, "Accent");
                Controls.Button? subtle = FindFluentButtonByContent(window, "Subtle");
                Controls.Button? disabled = FindFluentButtonByContent(window, "Disabled");

                Assert.IsNotNull(enable, "Buttons page should expose the enable toggle in the right rail.");
                Assert.IsNotNull(standard, "Standard button sample should exist.");
                Assert.IsNotNull(accent, "Accent button sample should exist.");
                Assert.IsNotNull(subtle, "Subtle button sample should exist.");
                Assert.IsNull(disabled, "The explicit Disabled button should be removed from the first sample.");

                Assert.IsTrue(standard.IsEnabled);
                Assert.IsTrue(accent.IsEnabled);
                Assert.IsTrue(subtle.IsEnabled);

                enable.IsChecked = false;
                DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();

                Assert.IsFalse(standard.IsEnabled, "Enable toggle should disable the Standard button.");
                Assert.IsFalse(accent.IsEnabled, "Enable toggle should disable the Accent button.");
                Assert.IsFalse(subtle.IsEnabled, "Enable toggle should disable the Subtle button.");
            });
        }

        [TestMethod]
        public void GalleryButtonsPage_SubtleButtonsKeepVisibleBorderAndToggleButtonSampleIsRemoved()
        {
            RunDemoPageTest(() => new GalleryButtonsPage(), window =>
            {
                Controls.Button? subtle = FindFluentButtonByContent(window, "Subtle");
                Controls.Button? refresh = FindFluentButtonByContent(window, "Refresh");

                Assert.IsNotNull(subtle, "Subtle button sample should exist.");
                Assert.IsNotNull(refresh, "Refresh button sample should exist.");
                AssertBrushIsVisible(subtle.BorderBrush, "Subtle button should keep a themed border.");
                AssertBrushIsVisible(refresh.BorderBrush, "Refresh button should keep a themed border.");
                Assert.IsNull(FindToggleButtonByContent(window, "Bold"),
                    "Buttons page should remove the Bold ToggleButton sample.");
                Assert.IsNull(FindToggleButtonByContent(window, "Pinned"),
                    "Buttons page should remove the Pinned ToggleButton sample.");
            });
        }

        [TestMethod]
        public void GalleryPages_RemoveRequestedOutputRegions()
        {
            RunDemoPageTest(() => new GalleryInputsPage(), window =>
            {
                Assert.IsNull(FindVisualChildByName<TextBlock>(window, "CharCountLabel"),
                    "Inputs TextBox sample should no longer expose a character-count output.");
            });

            RunDemoPageTest(() => new GalleryDataBindingPage(), window =>
            {
                Assert.IsNull(FindVisualChildByName<TextBlock>(window, "ItemCountLabel"),
                    "DataBinding first sample should no longer expose an item-count output.");
            });

            RunDemoPageTest(() => new GalleryTreesPage(), window =>
            {
                Assert.IsNull(FindVisualChildByName<TextBlock>(window, "TreeSelectionLabel"),
                    "Trees second sample should no longer expose a selection output.");
            });

            RunDemoPageTest(() => new GalleryNavigationPage(), window =>
            {
                Assert.IsNull(FindVisualChildByName<TextBlock>(window, "CompactNavigationOutputText"),
                    "Navigation compact sample should no longer expose an output text region.");
            });
        }

        [TestMethod]
        public void GalleryStatusPage_NumberBoxDrivesFirstProgressBar()
        {
            RunDemoPageTest(() => new GalleryStatusPage(), window =>
            {
                Controls.NumberBox? numberBox = FindVisualChildByName<Controls.NumberBox>(window, "ProgressValueNumberBox");
                Controls.ProgressBar? progressBar = FindVisualChildByName<Controls.ProgressBar>(window, "StandardProgressBar");
                Controls.ToggleSwitch? indeterminateToggle = FindVisualChildByName<Controls.ToggleSwitch>(window, "IndeterminateToggle");

                Assert.IsNotNull(numberBox, "Status page should use a NumberBox for the first ProgressBar value.");
                Assert.IsNotNull(progressBar, "Status page should expose the first ProgressBar.");
                Assert.IsNotNull(indeterminateToggle, "Status page should expose the indeterminate ProgressBar toggle.");
                Assert.AreEqual("On / Off", indeterminateToggle.OnContent as string,
                    "Indeterminate toggle text should not switch to a state-specific label.");
                Assert.AreEqual("On / Off", indeterminateToggle.OffContent as string,
                    "Indeterminate toggle text should not switch to a state-specific label.");
                Assert.AreEqual(0d, numberBox.Minimum, "Progress value NumberBox should allow the ProgressBar's empty state.");
                Assert.AreEqual(100d, numberBox.Maximum, "Progress value NumberBox should cap at 100.");
                Assert.IsNull(FindVisualChildByName<Controls.Slider>(window, "ProgressSlider"),
                    "The first ProgressBar should no longer be driven by a Slider.");
                Assert.IsNull(FindVisualChildByName<TextBlock>(window, "SliderValueLabel"),
                    "The first ProgressBar should no longer show an output value label.");

                numberBox.Value = 73d;
                DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();

                Assert.AreEqual(73d, progressBar.Value, 0.1,
                    "Changing the NumberBox should update the first ProgressBar.");

                numberBox.Value = 0d;
                DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();

                Assert.AreEqual(0d, progressBar.Value, 0.1,
                    "Changing the NumberBox to 0 should show the ProgressBar empty state.");

                indeterminateToggle.IsChecked = false;
                DrainDispatcher(window.Dispatcher);
                Assert.AreEqual("On / Off", indeterminateToggle.OffContent as string,
                    "Indeterminate toggle text should remain fixed after toggling off.");
            });
        }

        [TestMethod]
        public void GalleryFormsPage_CheckoutFieldsUseStableNamesAndAlignOptionalInput()
        {
            RunDemoPageTest(() => new GalleryFormsPage(), window =>
            {
                Grid? checkoutGrid = FindVisualChildByName<Grid>(window, "CheckoutFieldsGrid");
                Controls.NumberBox? quantity = FindVisualChildByName<Controls.NumberBox>(window, "QuantityNumberBox");
                Controls.TextBox? optional = FindVisualChildByName<Controls.TextBox>(window, "OptionalTextBox");
                Controls.CheckBox? gift = FindVisualChildByName<Controls.CheckBox>(window, "GiftCheckBox");
                StackPanel? actions = FindVisualChildByName<StackPanel>(window, "CheckoutButtonsPanel");

                Assert.IsNotNull(checkoutGrid, "Checkout sample should expose the quantity/options grid.");
                Assert.IsNotNull(quantity, "Checkout sample should expose the Quantity NumberBox.");
                Assert.IsNotNull(optional, "Checkout sample should expose the Optional TextBox.");
                Assert.IsNotNull(gift, "Checkout sample should expose the gift CheckBox.");
                Assert.IsNotNull(actions, "Checkout sample should expose the action button row.");
                Assert.AreEqual(3, checkoutGrid.ColumnDefinitions.Count,
                    "Checkout field grid should preserve the quantity, spacer, and optional columns.");
                Assert.AreEqual(0, Grid.GetColumn(quantity),
                    "Quantity NumberBox should remain in the first column.");
                Assert.AreEqual(2, Grid.GetColumn(optional),
                    "Optional TextBox should remain in the aligned right column.");
                Assert.AreEqual(VerticalAlignment.Bottom, optional.VerticalAlignment,
                    "Optional TextBox should align with the Quantity input row.");
            });
        }

        [TestMethod]
        public void GalleryDataPage_ListBackgroundsAndPersonPicturesUseExpectedAssets()
        {
            RunDemoPageTest(() => new GalleryDataPage(), window =>
            {
                Border? simpleBackground = FindVisualChildByName<Border>(window, "SimpleListViewBackground");
                Border? richBackground = FindVisualChildByName<Border>(window, "RichListViewBackground");

                Assert.IsNotNull(simpleBackground, "Simple ListView sample should have a named background wrapper.");
                Assert.IsNotNull(richBackground, "Rich ListView sample should have a named background wrapper.");

                List<Controls.PersonPicture> personPictures = [.. FindVisualChildren<Controls.PersonPicture>(window)];
                Assert.IsTrue(personPictures.Count(picture => picture.ProfilePicture is not null) >= 6,
                    "PersonPicture sample should include several image-backed portraits.");
                Assert.IsTrue(personPictures.Any(picture => picture.ProfilePicture is not null &&
                    picture.ProfilePicture.ToString().IndexOf("PersonPictureMadisonButler.png", StringComparison.Ordinal) >= 0),
                    "PersonPicture sample should include the Madison Butler portrait asset.");
                Assert.IsTrue(personPictures.Any(picture => picture.ProfilePicture is not null &&
                    picture.ProfilePicture.ToString().IndexOf("PersonPictureOscarWard.png", StringComparison.Ordinal) >= 0),
                    "PersonPicture sample should include the Oscar Ward portrait asset.");
                Assert.IsTrue(personPictures.Any(picture => string.Equals(picture.Initials, "NB", StringComparison.Ordinal)),
                    "PersonPicture sample should preserve the initials fallback example.");
                Assert.IsTrue(personPictures.Any(picture => picture.IsGroup),
                    "PersonPicture sample should preserve the group example.");
            });
        }

        [TestMethod]
        public void GalleryNavigationPage_IconsAreDefaultSizeAndInfoBadgePaneStartsExpanded()
        {
            RunDemoPageTest(() => new GalleryNavigationPage(), window =>
            {
                Controls.NavigationView? leftNavigation = FindVisualChildByName<Controls.NavigationView>(window, "LeftNavigationDemo");
                Assert.IsNotNull(leftNavigation, "Navigation page should expose the left mode sample.");

                List<Controls.FontIcon> leftIcons = [.. FindVisualChildren<Controls.FontIcon>(leftNavigation)];
                Assert.IsTrue(leftIcons.Count >= 3, "Left navigation sample should expose item icons.");
                Assert.IsTrue(leftIcons.All(icon => Math.Abs(icon.IconFontSize - 16d) < 0.1),
                    "NavigationView item icons should align with the compact pane glyph size.");

                Controls.NavigationView? badgeNavigation = FindVisualChildren<Controls.NavigationView>(window)
                    .FirstOrDefault(nav => string.Equals(nav.Header as string, "Inbox", StringComparison.Ordinal));
                Assert.IsNotNull(badgeNavigation, "Navigation page should expose the InfoBadge NavigationView sample.");
                Assert.AreEqual(NavigationViewPaneDisplayMode.Left, badgeNavigation.PaneDisplayMode,
                    "InfoBadge NavigationView sample should start expanded.");
                Assert.IsTrue(badgeNavigation.IsPaneOpen,
                    "InfoBadge NavigationView sample should keep the pane open.");
            });
        }

        [TestMethod]
        public void GalleryTabsPage_PlacementSampleUsesEqualHeaderWidths()
        {
            RunDemoPageTest(() => new GalleryTabsPage(), window =>
            {
                Dictionary<string, TabItem> items = FindVisualChildren<TabItem>(window)
                    .Where(item => item.Header is string)
                    .ToDictionary(item => (string)item.Header);

                double infoWidth = GetExplicitHeaderWidth(items, "Inbox");
                double archiveWidth = GetExplicitHeaderWidth(items, "Archive");
                double previewWidth = GetExplicitHeaderWidth(items, "Preview");
                double detailsWidth = GetExplicitHeaderWidth(items, "Details");

                Assert.AreEqual(infoWidth, archiveWidth, 0.1);
                Assert.AreEqual(infoWidth, previewWidth, 0.1);
                Assert.AreEqual(infoWidth, detailsWidth, 0.1);
                Assert.IsTrue(infoWidth > 0.0, "Placement sample tab headers should use an explicit shared width.");

                TabControl? bottomTabs = FindVisualChildByName<TabControl>(window, "BottomPlacementTabs");
                Assert.IsNotNull(bottomTabs, "Placement sample should expose the bottom TabControl.");
                Grid? detailsPanel = FindVisualChildByName<Grid>(bottomTabs, "DetailsTabContent");
                StackPanel? actionArea = FindVisualChildByName<StackPanel>(bottomTabs, "DetailsActionArea");
                Assert.IsNotNull(detailsPanel, "Bottom tab content should expose the Details fill panel.");
                Assert.IsNotNull(actionArea, "Details tab should expose a named lower action area.");
                Assert.AreEqual(GridUnitType.Star, detailsPanel.RowDefinitions[0].Height.GridUnitType,
                    "The area above the lower action row should consume available height.");
                Assert.AreEqual(GridUnitType.Auto, detailsPanel.RowDefinitions[1].Height.GridUnitType,
                    "The lower Details action area should stay close to the buttons.");
                Assert.AreEqual(1, Grid.GetRow(actionArea),
                    "Details action area should be in the lower auto row.");
            });
        }

        [TestMethod]
        public void GalleryLayoutPage_SeparatesStructuralPrimitiveSamples()
        {
            RunDemoPageTest(() => new GalleryLayoutPage(), window =>
            {
                List<string> descriptions = [.. FindVisualChildren<DemoSampleControl>(window).Select(sample => sample.SampleDescription)];

                Assert.IsTrue(descriptions.Any(description => description.IndexOf("Separator", StringComparison.OrdinalIgnoreCase) >= 0),
                    "Layout page should have a dedicated Separator DemoSampleControl.");
                Assert.IsTrue(descriptions.Any(description => description.IndexOf("DockPanel", StringComparison.OrdinalIgnoreCase) >= 0),
                    "Layout page should have a dedicated DockPanel DemoSampleControl.");
                Assert.IsTrue(descriptions.Any(description => description.IndexOf("Expander", StringComparison.OrdinalIgnoreCase) >= 0),
                    "Layout page should have a dedicated Expander DemoSampleControl.");

                Controls.Expander? dockPanelExpander = FindVisualChildByName<Controls.Expander>(window, "DockPanelOptionsExpander");
                Assert.IsNotNull(dockPanelExpander, "Layout page should expose the DockPanel Expander sample.");
                Assert.IsInstanceOfType(dockPanelExpander.Header, typeof(DockPanel),
                    "DockPanel Expander sample should use DockPanel in the collapsed header.");
                Assert.IsInstanceOfType(dockPanelExpander.Content, typeof(DockPanel),
                    "DockPanel Expander sample should use DockPanel in the expanded content.");
            });
        }

        [TestMethod]
        public void GalleryAccessibilityPage_RtlSampleDefaultsOn()
        {
            RunDemoPageTest(() => new GalleryAccessibilityPage(), window =>
            {
                Controls.ToggleSwitch? toggle = FindVisualChildByName<Controls.ToggleSwitch>(window, "RtlToggle");
                Controls.Card? card = FindVisualChildByName<Controls.Card>(window, "RtlDemoCard");

                Assert.IsNotNull(toggle, "Accessibility page should expose the RTL toggle.");
                Assert.IsNotNull(card, "Accessibility page should expose the RTL demo card.");
                Assert.IsTrue(toggle.IsChecked.GetValueOrDefault(),
                    "RTL sample should default to On.");
                Assert.AreEqual(FlowDirection.RightToLeft, card.FlowDirection,
                    "RTL demo card should default to RightToLeft.");
            });
        }

        private static double GetExplicitHeaderWidth(IDictionary<string, TabItem> items, string header)
        {
            Assert.IsTrue(items.TryGetValue(header, out TabItem? item), "TabItem should exist: " + header);
            return double.IsNaN(item.Width) ? item.MinWidth : item.Width;
        }

        private static void AssertBrushIsVisible(Brush? brush, string message)
        {
            Assert.IsNotNull(brush, message);
            if (brush is SolidColorBrush solid)
            {
                Assert.AreNotEqual(0, solid.Color.A, message);
            }
        }

        private static Controls.ToggleButton? FindToggleButtonByContent(DependencyObject root, string content)
        {
            foreach (Controls.ToggleButton button in FindVisualChildren<Controls.ToggleButton>(root))
            {
                if (string.Equals(button.Content as string, content, StringComparison.Ordinal))
                {
                    return button;
                }
            }

            return null;
        }
    }
}
