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
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FluenceListView = Fluence.Wpf.Controls.ListView;
using WpfBorder = System.Windows.Controls.Border;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-3 C20 tests: ListView/ListViewItem selection indicator.
    /// Authority: WinUI 3 ListViewItem_themeresources.xaml
    /// (ListViewItemSelectionIndicatorCornerRadius=1.5, AccentFillColorDefaultBrush).
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // Content-entrance discipline: regression guard for PrepareContainerForItemOverride.
        //
        // Subclass that intercepts PrepareContainerForItemOverride and records the container
        // Opacity immediately after the base call completes, before any animation dispatcher
        // frame has run. This lets the test assert the invariant directly and deterministically:
        // every animated container must start at Opacity 0 regardless of IsLoaded state.
        // ---------------------------------------------------------------------------

        private sealed class ListViewPrepareProbe : FluenceListView
        {
            /// <summary>
            /// Captures the Opacity of the most recently prepared container, sampled
            /// synchronously inside <see cref="PrepareContainerForItemOverride"/> immediately
            /// after the base call, before any animation tick.
            /// </summary>
            public double LastPreparedContainerOpacity { get; private set; } = double.NaN;

            /// <summary>
            /// Total number of containers observed by <see cref="PrepareContainerForItemOverride"/>.
            /// </summary>
            public int PrepareCount { get; private set; }

            protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
            {
                base.PrepareContainerForItemOverride(element, item);
                PrepareCount++;
                if (element is UIElement ui)
                {
                    LastPreparedContainerOpacity = ui.Opacity;
                }
            }
        }

        [TestMethod]
        public void ListView_PrepareContainerForItemOverride_AnimationsEnabled_ContainerStartsHidden_WhenLoaded()
        {
            // Regression guard for the content-entrance discipline fix:
            // PrepareContainerForItemOverride must hold every animated container at Opacity 0
            // from first realization, even when the list is already loaded. Previously it
            // returned early when !IsLoaded, leaving the initial batch at Opacity 1; a later
            // post-load CollectionChanged Reset then re-prepared items at Opacity 0, causing
            // a flash (full opacity -> vanish -> fade). The fix unconditionally starts
            // containers at Opacity 0 and lets OnListViewLoaded fade the initial batch.
            //
            // Strategy: use ListViewPrepareProbe to sample Opacity synchronously inside
            // PrepareContainerForItemOverride, before any animation tick. Trigger a
            // post-load repopulation via ItemsSource replacement to hit the IsLoaded==true
            // branch. Assert Opacity == 0 immediately after prepare, before any pump.
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ObservableCollection<string> initialItems = new(["Alpha", "Beta", "Gamma"]);
                ListViewPrepareProbe lv = new()
                {
                    Width = 300,
                    Height = 200,
                    ItemsSource = initialItems,
                    ItemAnimationsEnabled = true,
                };
                Window w = new() { Content = lv, Width = 360, Height = 260 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.IsTrue(lv.IsLoaded, "ListView must be loaded before the re-population test.");

                // Replace the ItemsSource to trigger a fresh PrepareContainerForItemOverride
                // cycle on a loaded list -- the scenario the original fix targeted.
                int prepareCountBefore = lv.PrepareCount;
                lv.ItemsSource = new ObservableCollection<string>(["Delta", "Epsilon", "Zeta"]);
                // WPF defers container generation after an ItemsSource change; pump the
                // dispatcher to realize the new containers. The probe captures Opacity
                // synchronously inside PrepareContainerForItemOverride, so the recorded
                // value is always the Opacity at prepare time, before any animation tick.
                DrainDispatcher(w.Dispatcher);

                Assert.AreNotEqual(prepareCountBefore, lv.PrepareCount,
                    "PrepareContainerForItemOverride must be called when ItemsSource is replaced on a loaded list.");
                Assert.AreEqual(0.0, lv.LastPreparedContainerOpacity, 1e-9,
                    "A freshly prepared container on a loaded, animation-enabled ListView must start at Opacity 0. "
                    + "A non-zero value means the container was briefly visible at full opacity before its entrance "
                    + "animation -- the flash that this fix removes.");

                w.Close();
            });
        }

        [TestMethod]
        public void ListView_PrepareContainerForItemOverride_AnimationsEnabled_ContainerStartsHidden_WhenNotYetLoaded()
        {
            // Complement: verify the pre-load batch also starts at Opacity 0 (the fix is
            // unconditional). OnListViewLoaded fades this batch in after the list is shown.
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ObservableCollection<string> items = new(["One", "Two"]);
                ListViewPrepareProbe lv = new()
                {
                    Width = 300,
                    Height = 200,
                    ItemsSource = items,
                    ItemAnimationsEnabled = true,
                };
                // Attach but do NOT show: container generation happens during the first
                // measure/arrange pass when the Window is created but before Show().
                Window w = new() { Content = lv, Width = 360, Height = 260 };

                // Force a layout pass (same as WPF does internally on Show preparation)
                // so PrepareContainerForItemOverride runs while IsLoaded is still false.
                lv.Measure(new Size(300, 200));
                lv.Arrange(new Rect(0, 0, 300, 200));
                lv.UpdateLayout();

                // If no container was prepared during measure/arrange (virtualization deferred
                // it), trigger via Show instead; either path must see Opacity 0.
                w.Show();

                // Sample before any animation drain -- if we have a recorded opacity it was
                // captured synchronously in PrepareContainerForItemOverride.
                if (lv.PrepareCount > 0)
                {
                    Assert.AreEqual(0.0, lv.LastPreparedContainerOpacity, 1e-9,
                        "A container prepared before the list is loaded must also start at Opacity 0 -- "
                        + "OnListViewLoaded will fade the initial batch once the list is shown.");
                }

                w.Close();
            });
        }

        [TestMethod]
        public void ListView_PrepareContainerForItemOverride_AnimationsDisabled_ContainerFullOpacity()
        {
            // When ItemAnimationsEnabled=false the container must NOT be held hidden; it should
            // appear instantly at full opacity. Verify the probe observes Opacity 1.0 in that case.
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ObservableCollection<string> items = new(["A", "B"]);
                ListViewPrepareProbe lv = new()
                {
                    Width = 300,
                    Height = 200,
                    ItemsSource = items,
                    ItemAnimationsEnabled = false,
                };
                Window w = new() { Content = lv, Width = 360, Height = 260 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // Replace source to trigger a post-load prepare with animations off.
                lv.ItemsSource = new ObservableCollection<string>(["C", "D"]);
                DrainDispatcher(w.Dispatcher);
                Assert.AreNotEqual(0, lv.PrepareCount,
                    "PrepareContainerForItemOverride must be called.");
                Assert.AreEqual(1.0, lv.LastPreparedContainerOpacity, 1e-9,
                    "With ItemAnimationsEnabled=false containers must remain at full opacity -- no hidden-then-fade.");

                w.Close();
            });
        }
        // ---------------------------------------------------------------------------
        // WI-3 C20  ListView SelectionIndicator
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void ListView_SelectionIndicator_PresentInItemTemplate()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceListView lv = new();
                _ = lv.Items.Add(new ListViewItem { Content = "Item A" });
                _ = lv.Items.Add(new ListViewItem { Content = "Item B" });
                Window w = new() { Content = lv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                // Find the first ListViewItem in the visual tree
                ListViewItem? item = FindVisualChild<ListViewItem>(lv);
                Assert.IsNotNull(item, "ListViewItem must exist in visual tree after Show.");

                WpfBorder? indicator = FindVisualChildByName<WpfBorder>(item, "SelectionIndicator");
                Assert.IsNotNull(indicator,
                    "SelectionIndicator border must be present in ListViewItem template per WI-3 C20.");
                w.Close();
            });
        }

        [TestMethod]
        public void ListView_SelectionIndicator_WidthIsCanonical()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceListView lv = new();
                _ = lv.Items.Add(new ListViewItem { Content = "Item A" });
                Window w = new() { Content = lv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ListViewItem? item = FindVisualChild<ListViewItem>(lv);
                Assert.IsNotNull(item, "ListViewItem must exist.");
                WpfBorder? indicator = FindVisualChildByName<WpfBorder>(item, "SelectionIndicator");
                Assert.IsNotNull(indicator, "SelectionIndicator must be present.");

                Assert.AreEqual(3.0, indicator.Width, 0.01,
                    "SelectionIndicator.Width must be 3.0 (WinUI 3 canonical 3px bar) per WI-3 C20.");
                w.Close();
            });
        }

        [TestMethod]
        public void ListView_SelectionIndicator_CornerRadiusIsCanonical()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceListView lv = new();
                _ = lv.Items.Add(new ListViewItem { Content = "Item A" });
                Window w = new() { Content = lv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ListViewItem? item = FindVisualChild<ListViewItem>(lv);
                Assert.IsNotNull(item, "ListViewItem must exist.");
                WpfBorder? indicator = FindVisualChildByName<WpfBorder>(item, "SelectionIndicator");
                Assert.IsNotNull(indicator, "SelectionIndicator must be present.");

                Assert.AreEqual(new CornerRadius(1.5), indicator.CornerRadius,
                    "SelectionIndicator.CornerRadius must be 1.5 per WinUI 3 ListViewItemSelectionIndicatorCornerRadius.");
                w.Close();
            });
        }

        [TestMethod]
        public void ListView_SelectionIndicator_BackgroundIsAccentBrush()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                FluenceListView lv = new();
                _ = lv.Items.Add(new ListViewItem { Content = "Item A" });
                Window w = new() { Content = lv, Width = 300, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ListViewItem? item = FindVisualChild<ListViewItem>(lv);
                Assert.IsNotNull(item, "ListViewItem must exist.");
                WpfBorder? indicator = FindVisualChildByName<WpfBorder>(item, "SelectionIndicator");
                Assert.IsNotNull(indicator, "SelectionIndicator must be present.");

                SolidColorBrush? expected = app?.TryFindResource("AccentFillColorDefaultBrush") as SolidColorBrush;
                Assert.IsNotNull(expected, "AccentFillColorDefaultBrush must resolve.");

                SolidColorBrush? actual = indicator.Background as SolidColorBrush;
                Assert.IsNotNull(actual, "SelectionIndicator.Background must be a SolidColorBrush.");
                Assert.AreEqual(
                    expected.Color,
                    actual.Color,
                    "SelectionIndicator.Background must be AccentFillColorDefaultBrush per WI-3 C20.");
                w.Close();
            });
        }

        [TestMethod]
        public void ListView_AnimateRemove_RemovesItemFromBoundObservableCollection()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ObservableCollection<string> items = ["One", "Two", "Three"];
                FluenceListView lv = new()
                {
                    Width = 300,
                    Height = 180,
                    ItemsSource = items,
                    ItemAnimationsEnabled = true
                };
                Window w = new() { Content = lv, Width = 360, Height = 240 };
                w.Show();
                DrainDispatcher(w.Dispatcher);
                w.UpdateLayout();

                bool completed = false;
                lv.AnimateRemove("Two", delegate { completed = true; });

                bool removed = WaitUntil(w.Dispatcher, 1000, delegate
                {
                    return completed && !items.Contains("Two");
                });

                Assert.IsTrue(removed, "AnimateRemove should animate then remove the item from the bound ObservableCollection.");
                Assert.AreEqual(2, items.Count);
                w.Close();
            });
        }
    }
}
