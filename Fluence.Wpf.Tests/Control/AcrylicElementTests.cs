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
 *    may be used to endorse products derived from this software without
 *    specific prior written permission.
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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Control
{
    public sealed class AcrylicElementTests
    {
        [Fact]
        public Task TemplateKeepsContentOutsideMaterialAndUsesDistinctBrushesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Controls.AcrylicElement first = new() { Width = 90, Height = 65, Content = new TextBlock { Text = "Sharp" } };
                Controls.AcrylicElement second = new() { Width = 90, Height = 65, Content = new TextBlock { Text = "Sharp" } };
                StackPanel panel = new();
                _ = panel.Children.Add(first);
                _ = panel.Children.Add(second);
                Window window = new() { Width = 240, Height = 220, Content = panel, ShowInTaskbar = false };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    _ = first.ApplyTemplate();
                    _ = second.ApplyTemplate();
                    ControlTemplate firstTemplate = Assert.IsType<ControlTemplate>(first.Template);
                    ControlTemplate secondTemplate = Assert.IsType<ControlTemplate>(second.Template);
                    Border firstBackdrop = Assert.IsType<Border>(firstTemplate.FindName("PART_Backdrop", first));
                    Border secondBackdrop = Assert.IsType<Border>(secondTemplate.FindName("PART_Backdrop", second));
                    VisualBrush firstBrush = Assert.IsType<VisualBrush>(firstBackdrop.Background);
                    VisualBrush secondBrush = Assert.IsType<VisualBrush>(secondBackdrop.Background);
                    Assert.NotSame(firstBrush, secondBrush);
                    Assert.NotSame(firstBrush.Visual, secondBrush.Visual);
                    Assert.True(first.IsUsingFallback);
                    Assert.True(second.IsUsingFallback);
                    Grid layer = Assert.IsType<Grid>(VisualTreeHelper.GetParent(firstBackdrop));
                    _ = Assert.IsType<ContentPresenter>(layer.Children[1]);

                    first.Template = null;
                    _ = first.ApplyTemplate();
                    first.Template = firstTemplate;
                    _ = first.ApplyTemplate();
                    Border replacementBackdrop = Assert.IsType<Border>(firstTemplate.FindName("PART_Backdrop", first));
                    Assert.NotSame(firstBackdrop, replacementBackdrop);
                    Assert.Null(firstBackdrop.Background);
                    Assert.Same(firstBrush, replacementBackdrop.Background);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DynamicRecipeReplacementAndReadOnlyStateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                AcrylicMaterialRecipe firstRecipe = new() { AlwaysUseFallback = true };
                AcrylicMaterialRecipe secondRecipe = new() { AlwaysUseFallback = true, FallbackColor = Colors.DarkSlateBlue };
                Grid root = new();
                root.Resources["LiveRecipe"] = firstRecipe;
                Controls.AcrylicElement host = new() { Width = 90, Height = 65 };
                host.SetResourceReference(Controls.AcrylicElement.RecipeProperty, "LiveRecipe");
                _ = root.Children.Add(host);
                Window window = new() { Content = root, Width = 220, Height = 180, ShowInTaskbar = false };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Same(firstRecipe, host.Recipe);
                    Assert.True(host.IsUsingFallback);
                    root.Resources["LiveRecipe"] = secondRecipe;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Same(secondRecipe, host.Recipe);
                    Assert.True(host.IsUsingFallback);
                    _ = Assert.Throws<InvalidOperationException>(() => host.SetValue(Controls.AcrylicElement.IsUsingFallbackProperty, value: false));
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task InternalReasonChangeFiresWhileFallbackStaysActiveAndStopsAfterUnloadAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                AcrylicMaterialRecipe recipe = new() { AlwaysUseFallback = true };
                Controls.AcrylicElement host = new() { Recipe = recipe, Width = 80, Height = 50 };
                Grid root = new();
                _ = root.Children.Add(host);
                Window window = new() { Width = 210, Height = 160, Content = root, ShowInTaskbar = false };
                int changes = 0;
                host.FallbackReasonChanged += (_, _) => changes++;
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Helpers.AcrylicFallbackReason.Always, host.FallbackReason);
                    changes = 0;
                    recipe.AlwaysUseFallback = false;
                    Assert.NotEqual(Helpers.AcrylicFallbackReason.Always, host.FallbackReason);
                    Assert.True(host.IsUsingFallback);
                    Assert.Equal(1, changes);

                    root.Children.Remove(host);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    changes = 0;
                    recipe.AlwaysUseFallback = true;
                    Assert.Equal(0, changes);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DynamicResourceRecipeFromGeneratedThemeResolvesOnHostAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                Controls.AcrylicElement host = new() { Width = 90, Height = 60 };
                host.SetResourceReference(Controls.AcrylicElement.RecipeProperty, "AcrylicInAppDefaultRecipe");
                Window window = new() { Content = host, Width = 220, Height = 160, ShowInTaskbar = false };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    AcrylicMaterialRecipe light = Assert.IsType<AcrylicMaterialRecipe>(Application.Current.TryFindResource("AcrylicInAppDefaultRecipe"));
                    Assert.Same(light, host.Recipe);
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    AcrylicMaterialRecipe dark = Assert.IsType<AcrylicMaterialRecipe>(Application.Current.TryFindResource("AcrylicInAppDefaultRecipe"));
                    Assert.Same(dark, host.Recipe);
                    Assert.NotSame(light, dark);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                }
            });
        }

        [Fact]
        public Task LiveSourceTransformAndClipChangesUpdateReasonWithoutManualRefreshAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Border source = new() { Width = 180, Height = 130, Background = Brushes.CadetBlue };
                Controls.AcrylicElement host = new() { BackdropSource = source, Width = 80, Height = 60 };
                Grid root = new();
                _ = root.Children.Add(source);
                _ = root.Children.Add(host);
                Window window = new() { Content = root, Width = 260, Height = 210, ShowInTaskbar = false };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    WaitForReason(host, Helpers.AcrylicFallbackReason.None);
                    RotateTransform rotation = new();
                    source.RenderTransform = rotation;
                    rotation.Angle = 15;
                    WaitForReason(host, Helpers.AcrylicFallbackReason.UnsupportedTransform);
                    rotation.Angle = 0;
                    WaitForReason(host, Helpers.AcrylicFallbackReason.None);

                    source.Clip = new EllipseGeometry(new Rect(0, 0, 100, 80));
                    WaitForReason(host, Helpers.AcrylicFallbackReason.UnsupportedClip);
                    RectangleGeometry rectangle = new(new Rect(0, 0, 100, 80));
                    source.Clip = rectangle;
                    WaitForReason(host, Helpers.AcrylicFallbackReason.None);
                    rectangle.RadiusX = 5;
                    WaitForReason(host, Helpers.AcrylicFallbackReason.UnsupportedClip);
                    rectangle.RadiusX = 0;
                    WaitForReason(host, Helpers.AcrylicFallbackReason.None);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task LiveSourceBrushReplacementRejectsCycleAndRecoversWithoutLayoutAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Border source = new() { Width = 180, Height = 130, Background = Brushes.CadetBlue };
                Controls.AcrylicElement host = new() { BackdropSource = source, Width = 80, Height = 60 };
                Grid root = new();
                _ = root.Children.Add(source);
                _ = root.Children.Add(host);
                Window window = new() { Content = root, Width = 260, Height = 210, ShowInTaskbar = false };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    WaitForReason(host, Helpers.AcrylicFallbackReason.None);

                    source.Background = new VisualBrush(host);
                    Assert.Equal(Helpers.AcrylicFallbackReason.CyclicSource, host.FallbackReason);
                    WaitForReason(host, Helpers.AcrylicFallbackReason.CyclicSource);
                    AssertOpaqueNoiseFreeFallback(host);

                    source.Background = Brushes.CadetBlue;
                    WaitForReason(host, Helpers.AcrylicFallbackReason.None);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task LiveNestedBrushMaskAndDrawingReferencesRejectCyclesAndRecoverAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Border safe = new() { Background = Brushes.CadetBlue };
                VisualBrush nested = new(safe);
                Border source = new() { Width = 180, Height = 130, Background = nested };
                Controls.AcrylicElement host = new() { BackdropSource = source, Width = 80, Height = 60 };
                Grid root = new();
                _ = root.Children.Add(source);
                _ = root.Children.Add(host);
                Window window = new() { Content = root, Width = 260, Height = 210, ShowInTaskbar = false };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    WaitForReason(host, Helpers.AcrylicFallbackReason.None);

                    nested.Visual = host;
                    WaitForReason(host, Helpers.AcrylicFallbackReason.CyclicSource);
                    AssertOpaqueNoiseFreeFallback(host);
                    nested.Visual = safe;
                    WaitForReason(host, Helpers.AcrylicFallbackReason.None);

                    source.OpacityMask = new VisualBrush(host);
                    WaitForReason(host, Helpers.AcrylicFallbackReason.CyclicSource);
                    AssertOpaqueNoiseFreeFallback(host);
                    source.OpacityMask = null;
                    WaitForReason(host, Helpers.AcrylicFallbackReason.None);

                    GeometryDrawing drawing = new(new VisualBrush(safe), pen: null,
                        geometry: new RectangleGeometry(new Rect(0, 0, 20, 20)));
                    source.Background = new DrawingBrush(drawing);
                    WaitForReason(host, Helpers.AcrylicFallbackReason.None);
                    drawing.Brush = new VisualBrush(host);
                    WaitForReason(host, Helpers.AcrylicFallbackReason.CyclicSource);
                    AssertOpaqueNoiseFreeFallback(host);
                    drawing.Brush = new VisualBrush(safe);
                    WaitForReason(host, Helpers.AcrylicFallbackReason.None);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task SoftwareRenderModeFlipUpdatesRealHostAndRecoversSameHwndAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Border source = new() { Width = 180, Height = 130, Background = Brushes.CadetBlue };
                Controls.AcrylicElement host = new() { BackdropSource = source, Width = 80, Height = 60 };
                Grid root = new();
                _ = root.Children.Add(source);
                _ = root.Children.Add(host);
                Window window = new() { Content = root, Width = 260, Height = 210, ShowInTaskbar = false };
                RenderMode processMode = RenderOptions.ProcessRenderMode;
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    WaitForReason(host, Helpers.AcrylicFallbackReason.None);
                    HwndSource hwnd = Assert.IsType<HwndSource>(PresentationSource.FromVisual(host));
                    HwndTarget target = Assert.IsType<HwndTarget>(hwnd.CompositionTarget);
                    RenderMode targetMode = target.RenderMode;
                    try
                    {
                        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
                        target.RenderMode = RenderMode.SoftwareOnly;
                        WaitForReason(host, Helpers.AcrylicFallbackReason.UnsupportedGraphics);
                        Assert.True(host.IsUsingFallback);
                        Border backdrop = Assert.IsType<Border>(host.Template.FindName("PART_Backdrop", host));
                        VisualBrush brush = Assert.IsType<VisualBrush>(backdrop.Background);
                        Canvas visual = Assert.IsType<Canvas>(brush.Visual);
                        Assert.Empty(visual.Children);
                        SolidColorBrush paint = Assert.IsType<SolidColorBrush>(visual.Background);
                        Assert.Equal(byte.MaxValue, paint.Color.A);
                    }
                    finally
                    {
                        RenderOptions.ProcessRenderMode = processMode;
                        target.RenderMode = targetMode;
                    }

                    WaitForReason(host, Helpers.AcrylicFallbackReason.None);
                    Assert.Same(hwnd, PresentationSource.FromVisual(host));
                }
                finally
                {
                    RenderOptions.ProcessRenderMode = processMode;
                    VisualTree.CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task TwoWindowsKeepIndependentBrushesWithOneFrozenRecipeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                AcrylicMaterialRecipe recipe = new() { AlwaysUseFallback = true };
                recipe.Freeze();
                Border firstSource = new() { Width = 160, Height = 120, Background = Brushes.DarkBlue };
                Border secondSource = new() { Width = 160, Height = 120, Background = Brushes.DarkOrange };
                Controls.AcrylicElement first = new() { BackdropSource = firstSource, Recipe = recipe, Width = 80, Height = 60 };
                Controls.AcrylicElement second = new() { BackdropSource = secondSource, Recipe = recipe, Width = 80, Height = 60 };
                Grid firstRoot = new();
                Grid secondRoot = new();
                _ = firstRoot.Children.Add(firstSource);
                _ = firstRoot.Children.Add(first);
                _ = secondRoot.Children.Add(secondSource);
                _ = secondRoot.Children.Add(second);
                Window firstWindow = new() { Content = firstRoot, Width = 230, Height = 180, ShowInTaskbar = false };
                Window secondWindow = new() { Content = secondRoot, Width = 230, Height = 180, ShowInTaskbar = false };
                bool firstClosed = false;
                try
                {
                    firstWindow.Show();
                    secondWindow.Show();
                    WpfTestSta.DrainDispatcher(secondWindow.Dispatcher);
                    Border firstBackdrop = Assert.IsType<Border>(first.Template.FindName("PART_Backdrop", first));
                    Border secondBackdrop = Assert.IsType<Border>(second.Template.FindName("PART_Backdrop", second));
                    VisualBrush firstBrush = Assert.IsType<VisualBrush>(firstBackdrop.Background);
                    VisualBrush secondBrush = Assert.IsType<VisualBrush>(secondBackdrop.Background);
                    Assert.NotSame(firstBrush, secondBrush);
                    Assert.NotSame(firstBrush.Visual, secondBrush.Visual);
                    Assert.NotSame(PresentationSource.FromVisual(first), PresentationSource.FromVisual(second));
                    Assert.Same(recipe, first.Recipe);
                    Assert.Same(recipe, second.Recipe);
                    VisualTree.CloseWindowAndDrain(firstWindow);
                    firstClosed = true;
                    Assert.Same(secondBrush, secondBackdrop.Background);
                    Assert.NotNull(secondBrush.Visual);
                    Assert.Equal(Helpers.AcrylicFallbackReason.Always, second.FallbackReason);
                }
                finally
                {
                    if (!firstClosed)
                    {
                        VisualTree.CloseWindowAndDrain(firstWindow);
                    }

                    VisualTree.CloseWindowAndDrain(secondWindow);
                }
            });
        }

        private static void WaitForReason(Controls.AcrylicElement host, Helpers.AcrylicFallbackReason expected)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(2);
            while (host.FallbackReason != expected && DateTime.UtcNow < deadline)
            {
                DispatcherFrame frame = new();
                DispatcherTimer timer = new(TimeSpan.FromMilliseconds(20), DispatcherPriority.Background,
                    (_, _) => frame.Continue = false, host.Dispatcher);
                timer.Start();
                Dispatcher.PushFrame(frame);
                timer.Stop();
            }

            Assert.Equal(expected, host.FallbackReason);
        }

        private static void AssertOpaqueNoiseFreeFallback(Controls.AcrylicElement host)
        {
            Assert.True(host.IsUsingFallback);
            Border backdrop = Assert.IsType<Border>(host.Template.FindName("PART_Backdrop", host));
            VisualBrush brush = Assert.IsType<VisualBrush>(backdrop.Background);
            Canvas visual = Assert.IsType<Canvas>(brush.Visual);
            Assert.Empty(visual.Children);
            SolidColorBrush paint = Assert.IsType<SolidColorBrush>(visual.Background);
            Assert.Equal(byte.MaxValue, paint.Color.A);
        }
    }
}
