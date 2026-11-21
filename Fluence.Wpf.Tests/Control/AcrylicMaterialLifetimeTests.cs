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
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Fluence.Wpf.Helpers;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Control
{
    public sealed class AcrylicMaterialLifetimeTests
    {
        [Fact]
        public Task SourceRetargetAndUnloadReleaseTheOldVisualAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Grid root = new();
                Border first = new() { Width = 180, Height = 140, Background = Brushes.DarkBlue };
                Border second = new() { Width = 180, Height = 140, Background = Brushes.OrangeRed };
                Border owner = new() { Width = 80, Height = 60 };
                _ = root.Children.Add(first);
                _ = root.Children.Add(second);
                _ = root.Children.Add(owner);
                FrameworkElement selected = first;
                AcrylicMaterialController controller = new(owner, () => selected, recipe: null);
                owner.Background = controller.Output;
                Window window = new() { Width = 300, Height = 230, Content = root, ShowInTaskbar = false };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    controller.InvalidateSource();
                    Assert.Same(first, controller.CurrentSource);
                    Assert.True(controller.OutputVisual.ActualWidth > 0);
                    Assert.True(controller.OutputVisual.ActualHeight > 0);

                    selected = second;
                    controller.InvalidateSource();
                    Assert.Same(second, controller.CurrentSource);
                    root.Children.Remove(second);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Null(controller.CurrentSource);
                    Assert.True(controller.IsUsingFallback);

                    _ = root.Children.Add(second);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    controller.InvalidateSource();
                    Assert.Same(second, controller.CurrentSource);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                }

                Assert.Null(controller.CurrentSource);
                Assert.NotNull(controller.Output.Visual);
                controller.Release();
            });
        }

        [Fact]
        public Task ReferenceWatcherDisposesAfterAcrylicSourceFreezesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                SolidColorBrush drawingBrush = new(Colors.DarkBlue);
                GeometryDrawing drawing = new(drawingBrush, pen: null, Geometry.Empty);
                SolidColorBrush mutableBrush = new(Colors.Black);
                HashSet<DependencyObject> references = [drawing, drawingBrush, mutableBrush];
                int changes = 0;
                using AcrylicSourceReferenceWatcher watcher =
                    AcrylicSourceReferenceWatcher.Observe(references, () => changes++);

                mutableBrush.Color = Colors.White;
                Assert.Equal(1, changes);

                drawing.Freeze();
                Assert.True(drawingBrush.IsFrozen);
                watcher.Dispose();
                Assert.Empty(references);

                int changesAtDisposal = changes;
                mutableBrush.Color = Colors.Gray;
                Assert.Equal(changesAtDisposal, changes);
            });
        }

        [Fact]
        public Task DirectBrushReplacementDisposesBeforeLoadAndAfterUnloadAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Border owner = new();
                AcrylicMaterialController beforeLoad = new(owner, static () => null, recipe: null);
                beforeLoad.AttachDirectProperty(Border.BackgroundProperty);
                owner.Background = beforeLoad.Output;
                owner.Background = Brushes.Black;
                Assert.True(beforeLoad.IsDisposed);

                Grid root = new();
                _ = root.Children.Add(owner);
                Window window = new() { Content = root, Width = 200, Height = 160, ShowInTaskbar = false };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    AcrylicMaterialController afterUnload = new(owner, static () => null, recipe: null);
                    afterUnload.AttachDirectProperty(Border.BackgroundProperty);
                    owner.Background = afterUnload.Output;
                    root.Children.Remove(owner);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    owner.Background = Brushes.White;
                    Assert.True(afterUnload.IsDisposed);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task TargetResizeAndReloadRetainSourceTreeBindingsAndBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Grid root = new();
                TextBlock text = new();
                _ = text.SetBinding(TextBlock.TextProperty, new Binding("Label"));
                Border source = new()
                {
                    Width = 190,
                    Height = 150,
                    Child = text,
                    DataContext = new { Label = "Source tree" },
                };
                Border owner = new() { Width = 80, Height = 60 };
                _ = root.Children.Add(source);
                _ = root.Children.Add(owner);
                AcrylicMaterialController controller = new(owner, () => source, recipe: null);
                VisualBrush brush = controller.Output;
                owner.Background = brush;
                Window window = new() { Width = 300, Height = 230, Content = root, ShowInTaskbar = false };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal("Source tree", text.Text);
                    Assert.Same(source, controller.CurrentSource);
                    Assert.Equal(owner.ActualWidth, controller.OutputVisual.ActualWidth);
                    owner.Width = 120;
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(owner.ActualWidth, controller.OutputVisual.ActualWidth);
                    Assert.False(double.IsNaN(controller.OutputVisual.ActualWidth));
                    Assert.False(double.IsInfinity(controller.OutputVisual.ActualWidth));
                    root.Children.Remove(owner);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Null(controller.CurrentSource);
                    Assert.Same(brush, owner.Background);
                    _ = root.Children.Add(owner);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Same(source, controller.CurrentSource);
                    Assert.Same(brush, owner.Background);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                    controller.Release();
                }
            });
        }

        [Fact]
        public Task DirectPropertyReplacementAllowsOwnerAndControllerCollectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                (WeakReference owner, WeakReference controller) = CreateAndReleaseDirectOwner();
                for (int index = 0; index < 3; index++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                Assert.False(owner.IsAlive);
                Assert.False(controller.IsAlive);
            });
        }

        [Fact]
        public Task UnloadedOwnerAndControllerCollectWhileSourceWindowRemainsOpenAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                (Window window, WeakReference owner, WeakReference controller) = CreateUnloadedControllerWithRetainedSource();
                try
                {
                    for (int index = 0; index < 3; index++)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }

                    Assert.False(owner.IsAlive);
                    Assert.False(controller.IsAlive);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ReplacedSourceAndNestedBrushCollectWhileWindowRemainsOpenAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                (Window window, AcrylicMaterialController controller, WeakReference source, WeakReference brush) =
                    CreateRetargetedControllerWithRemovedSource();
                try
                {
                    for (int index = 0; index < 3; index++)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }

                    Assert.False(source.IsAlive);
                    Assert.False(brush.IsAlive);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                    controller.Release();
                }
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (Window window, AcrylicMaterialController controller, WeakReference source, WeakReference brush)
            CreateRetargetedControllerWithRemovedSource()
        {
            Grid root = new();
            VisualBrush nested = new(new Border { Background = Brushes.CadetBlue });
            Border first = new() { Width = 180, Height = 130, Background = nested };
            Border second = new() { Width = 180, Height = 130, Background = Brushes.OrangeRed };
            Border owner = new() { Width = 80, Height = 60 };
            _ = root.Children.Add(first);
            _ = root.Children.Add(second);
            _ = root.Children.Add(owner);
            FrameworkElement selected = first;
            AcrylicMaterialController controller = new(owner, () => selected, recipe: null);
            owner.Background = controller.Output;
            Window window = new() { Content = root, Width = 250, Height = 200, ShowInTaskbar = false };
            window.Show();
            WpfTestSta.DrainDispatcher(window.Dispatcher);
            Assert.Same(first, controller.CurrentSource);
            selected = second;
            controller.InvalidateSource();
            Assert.Same(second, controller.CurrentSource);
            root.Children.Remove(first);
            WpfTestSta.DrainDispatcher(window.Dispatcher);
            return (window, controller, new WeakReference(first), new WeakReference(nested));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (Window window, WeakReference owner, WeakReference controller) CreateUnloadedControllerWithRetainedSource()
        {
            Grid root = new();
            VisualBrush nested = new(new Border { Background = Brushes.CadetBlue });
            Border source = new() { Width = 180, Height = 130, Background = nested };
            Border owner = new() { Width = 80, Height = 60 };
            _ = root.Children.Add(source);
            _ = root.Children.Add(owner);
            AcrylicMaterialController controller = new(owner, () => source, recipe: null);
            owner.Background = controller.Output;
            Window window = new() { Content = root, Width = 250, Height = 200, ShowInTaskbar = false };
            window.Show();
            WpfTestSta.DrainDispatcher(window.Dispatcher);
            Assert.Same(source, controller.CurrentSource);
            root.Children.Remove(owner);
            WpfTestSta.DrainDispatcher(window.Dispatcher);
            Assert.Null(controller.CurrentSource);
            nested.Visual = new Border { Background = Brushes.CadetBlue };
            return (window, new WeakReference(owner), new WeakReference(controller));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (WeakReference owner, WeakReference controller) CreateAndReleaseDirectOwner()
        {
            Border owner = new();
            AcrylicMaterialController controller = new(owner, static () => null, recipe: null);
            controller.AttachDirectProperty(Border.BackgroundProperty);
            owner.Background = controller.Output;
            owner.Background = Brushes.Red;
            Assert.True(controller.IsDisposed);
            return (new WeakReference(owner), new WeakReference(controller));
        }
    }
}
