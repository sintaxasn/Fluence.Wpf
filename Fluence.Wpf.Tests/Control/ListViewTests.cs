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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Fluent <see cref="Controls.ListView"/> control: selection indicator and IsItemSelectable.
    /// Authority: WinUI 3 ListViewItem_themeresources.xaml
    /// (ListViewItemSelectionIndicatorCornerRadius=1.5, AccentFillColorDefaultBrush).
    /// </summary>
    public sealed class ListViewTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        // ---------------------------------------------------------------------------
        // WI-3 C20  ListView SelectionIndicator
        // ---------------------------------------------------------------------------

        [Fact]
        public Task ListView_SelectionIndicator_PresentInItemTemplateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ListView lv = new();
                _ = lv.Items.Add(new ListViewItem { Content = "Item A" });
                _ = lv.Items.Add(new ListViewItem { Content = "Item B" });
                Window w = new() { Content = lv, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // Find the first ListViewItem in the visual tree
                ListViewItem item = Assert.IsType<ListViewItem>(FindVisualChild<ListViewItem>(lv), exactMatch: false);

                Border indicator = Assert.IsType<Border>(FindVisualChildByName<Border>(item, "SelectionIndicator"), exactMatch: false);
                w.Close();
            });
        }

        [Fact]
        public Task ListView_SelectionIndicator_WidthIsCanonicalAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ListView lv = new();
                _ = lv.Items.Add(new ListViewItem { Content = "Item A" });
                Window w = new() { Content = lv, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ListViewItem item = Assert.IsType<ListViewItem>(FindVisualChild<ListViewItem>(lv), exactMatch: false);
                Border indicator = Assert.IsType<Border>(FindVisualChildByName<Border>(item, "SelectionIndicator"), exactMatch: false);

                Assert.Equal(3.0, indicator.Width, 0.01);
                w.Close();
            });
        }

        [Fact]
        public Task ListView_SelectionIndicator_CornerRadiusIsCanonicalAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ListView lv = new();
                _ = lv.Items.Add(new ListViewItem { Content = "Item A" });
                Window w = new() { Content = lv, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ListViewItem item = Assert.IsType<ListViewItem>(FindVisualChild<ListViewItem>(lv), exactMatch: false);
                Border indicator = Assert.IsType<Border>(FindVisualChildByName<Border>(item, "SelectionIndicator"), exactMatch: false);

                Assert.Equal(new CornerRadius(1.5), indicator.CornerRadius);
                w.Close();
            });
        }

        [Fact]
        public Task ListView_SelectionIndicator_BackgroundIsAccentBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                Controls.ListView lv = new();
                _ = lv.Items.Add(new ListViewItem { Content = "Item A" });
                Window w = new() { Content = lv, Width = 300, Height = 200 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ListViewItem item = Assert.IsType<ListViewItem>(FindVisualChild<ListViewItem>(lv), exactMatch: false);
                Border indicator = Assert.IsType<Border>(FindVisualChildByName<Border>(item, "SelectionIndicator"), exactMatch: false);

                SolidColorBrush expected = Assert.IsType<SolidColorBrush>(app.TryFindResource("AccentFillColorDefaultBrush"));

                SolidColorBrush actual = Assert.IsType<SolidColorBrush>(indicator.Background);
                Assert.Equal(
                    expected.Color,
                    actual.Color);
                w.Close();
            });
        }

        [Fact]
        public Task ListView_AnimateRemove_RemovesItemFromBoundObservableCollectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                ObservableCollection<string> items = ["One", "Two", "Three"];
                Controls.ListView lv = new()
                {
                    Width = 300,
                    Height = 180,
                    ItemsSource = items,
                    ItemAnimationsEnabled = true,
                };
                Window w = new() { Content = lv, Width = 360, Height = 240 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);
                w.UpdateLayout();

                bool completed = false;
                lv.AnimateRemove("Two", delegate { completed = true; });

                bool removed = await WaitUntilAsync(w.Dispatcher, 1000, delegate
                {
                    return completed && !items.Contains("Two");
                }).ConfigureAwait(true);

                Assert.True(removed, "AnimateRemove should animate then remove the item from the bound ObservableCollection.");
                Assert.Equal(2, items.Count);
                w.Close();
            });
        }

        // ---------------------------------------------------------------------------
        // IsItemSelectable
        // ---------------------------------------------------------------------------

        [Fact]
        public Task IsItemSelectable_DefaultIsTrueAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ListView lv = new();
                Assert.True(lv.IsItemSelectable);
            });
        }

        [Fact]
        public Task IsItemSelectable_False_ClearsSelectionWhenSetAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();
                Controls.ListView lv = new() { Width = 260, Height = 120 };
                _ = lv.Items.Add("a");
                _ = lv.Items.Add("b");

                try
                {
                    window.Content = lv;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    lv.SelectedIndex = 0;
                    Assert.Equal(0, lv.SelectedIndex);

                    lv.IsItemSelectable = false;
                    Assert.Equal(-1, lv.SelectedIndex);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task IsItemSelectable_False_SelectedIndexStaysMinusOne_AfterDirectSetAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();
                Controls.ListView lv = new()
                {
                    Width = 260,
                    Height = 120,
                    IsItemSelectable = false,
                };
                _ = lv.Items.Add("a");

                try
                {
                    window.Content = lv;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    lv.SelectedIndex = 0;
                    Assert.Equal(-1, lv.SelectedIndex);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task IsItemSelectable_False_ContainerIsNotFocusableAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();
                Controls.ListView lv = new()
                {
                    Width = 260,
                    Height = 120,
                    IsItemSelectable = false,
                };
                _ = lv.Items.Add("a");

                try
                {
                    window.Content = lv;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ListViewItem container = Assert.IsType<ListViewItem>(lv.ItemContainerGenerator.ContainerFromIndex(0));
                    Assert.False(container.Focusable);
                    Assert.False(Controls.ListView.GetParentIsItemSelectable(container));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task IsItemSelectable_True_ContainerIsFocusableAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();
                Controls.ListView lv = new()
                {
                    Width = 260,
                    Height = 120,
                    IsItemSelectable = true,
                };
                _ = lv.Items.Add("a");

                try
                {
                    window.Content = lv;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ListViewItem container = Assert.IsType<ListViewItem>(lv.ItemContainerGenerator.ContainerFromIndex(0));
                    Assert.True(container.Focusable);
                    Assert.True(Controls.ListView.GetParentIsItemSelectable(container));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ItemAnimationsEnabled_IndependentOfIsItemSelectableAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ListView lv = new() { IsItemSelectable = false, ItemAnimationsEnabled = true };
                Assert.False(lv.IsItemSelectable);
                Assert.True(lv.ItemAnimationsEnabled);

                lv.ItemAnimationsEnabled = false;
                lv.IsItemSelectable = true;
                Assert.True(lv.IsItemSelectable);
                Assert.False(lv.ItemAnimationsEnabled);
            });
        }

        private static async Task<bool> WaitUntilAsync(Dispatcher dispatcher, int milliseconds, Func<bool> condition)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(milliseconds);
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;

            do
            {
                await dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.ApplicationIdle, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
                if (condition())
                {
                    return true;
                }

                DispatcherFrame frame = new();
                DispatcherTimer timer = new(
                    TimeSpan.FromMilliseconds(16),
                    DispatcherPriority.Normal,
                    delegate { frame.Continue = false; },
                    dispatcher);
                timer.Start();
                Dispatcher.PushFrame(frame);
                timer.Stop();
            }
            while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested);

            await dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.ApplicationIdle, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
            return condition();
        }
    }
}
