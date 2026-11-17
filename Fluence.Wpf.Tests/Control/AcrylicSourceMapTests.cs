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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Fluence.Wpf.Helpers;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Control
{
    public sealed class AcrylicSourceMapTests
    {
        [Fact]
        public Task DisjointSiblingIsAcceptedAsync()
        {
            return InWindowAsync(static (_, source, owner) =>
                Assert.Equal(AcrylicFallbackReason.None, AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason));
        }

        [Fact]
        public Task SelfSourceIsRejectedAsync()
        {
            return InWindowAsync(static (_, _, owner) =>
                Assert.Equal(AcrylicFallbackReason.CyclicSource, AcrylicSourceMap.Evaluate(owner, owner, new Size(80, 60), 8).Reason));
        }

        [Fact]
        public Task AncestorSourceIsRejectedAsync()
        {
            return InWindowAsync(static (root, _, owner) =>
                Assert.Equal(AcrylicFallbackReason.CyclicSource, AcrylicSourceMap.Evaluate(owner, root, new Size(80, 60), 8).Reason));
        }

        [Fact]
        public Task DescendantSourceIsRejectedAsync()
        {
            return InWindowAsync(static (_, _, owner) =>
            {
                Border child = new() { Width = 12, Height = 12 };
                owner.Child = child;
                owner.UpdateLayout();
                WpfTestSta.DrainDispatcher(owner.Dispatcher);
                Assert.Equal(AcrylicFallbackReason.CyclicSource, AcrylicSourceMap.Evaluate(owner, child, new Size(80, 60), 8).Reason);
            });
        }

        [Fact]
        public Task DetachedSourceIsRejectedAsync()
        {
            return InWindowAsync(static (_, _, owner) =>
                Assert.Equal(AcrylicFallbackReason.UnavailableSource,
                    AcrylicSourceMap.Evaluate(owner, new Border(), new Size(80, 60), 8).Reason));
        }

        [Fact]
        public Task OutsideSourceBoundsReturnsUnavailableSourceAsync()
        {
            return InWindowAsync(static (_, source, owner) =>
            {
                source.HorizontalAlignment = HorizontalAlignment.Left;
                owner.HorizontalAlignment = HorizontalAlignment.Right;
                owner.UpdateLayout();
                Assert.Equal(AcrylicFallbackReason.UnavailableSource,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
            });
        }

        [Fact]
        public Task DifferentWindowSourceIsRejectedAsync()
        {
            return InWindowAsync(static (_, _, owner) =>
            {
                Window other = new() { Width = 200, Height = 160, ShowInTaskbar = false };
                Border secondSource = new() { Width = 120, Height = 100 };
                try
                {
                    other.Content = secondSource;
                    other.Show();
                    WpfTestSta.DrainDispatcher(other.Dispatcher);
                    Assert.Equal(AcrylicFallbackReason.DifferentHwnd,
                        AcrylicSourceMap.Evaluate(owner, secondSource, new Size(80, 60), 8).Reason);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(other);
                }
            });
        }

        [Fact]
        public Task PopupSourceIsRejectedAsDifferentHwndAsync()
        {
            return InWindowAsync(static (root, _, owner) =>
            {
                Border popupSource = new() { Width = 120, Height = 100 };
                Popup popup = new() { PlacementTarget = root, Child = popupSource };
                try
                {
                    popup.IsOpen = true;
                    WpfTestSta.DrainDispatcher(root.Dispatcher);
                    Assert.Equal(AcrylicFallbackReason.DifferentHwnd,
                        AcrylicSourceMap.Evaluate(owner, popupSource, new Size(80, 60), 8).Reason);
                }
                finally
                {
                    popup.IsOpen = false;
                    WpfTestSta.DrainDispatcher(root.Dispatcher);
                }
            });
        }

        [Fact]
        public Task NestedDrawingBrushReferenceToRegisteredOutputIsRejectedAsync()
        {
            return InWindowAsync(static (_, source, owner) =>
            {
                Border output = new();
                using IDisposable registration = AcrylicSourceMap.Register(owner, source, output);
                source.Background = new DrawingBrush(new GeometryDrawing(
                    new VisualBrush(output), pen: null, geometry: new RectangleGeometry(new Rect(0, 0, 10, 10))));
                Assert.Equal(AcrylicFallbackReason.CyclicSource,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
            });
        }

        [Fact]
        public Task OrdinaryVisualBrushDoesNotCreateFeedbackAsync()
        {
            return InWindowAsync(static (_, source, owner) =>
            {
                Border output = new();
                using IDisposable registration = AcrylicSourceMap.Register(owner, source, output);
                source.Background = new VisualBrush(new Border());
                Assert.Equal(AcrylicFallbackReason.None,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
            });
        }

        [Fact]
        public Task StyledBorderBrushReferenceToRegisteredOutputIsRejectedAsync()
        {
            return InWindowAsync(static (_, source, owner) =>
            {
                Border output = new();
                using IDisposable registration = AcrylicSourceMap.Register(owner, source, output);
                Style style = new(typeof(Border));
                style.Setters.Add(new Setter(Border.BorderBrushProperty, new VisualBrush(output)));
                source.BorderThickness = new Thickness(2);
                source.Style = style;
                Assert.Equal(AcrylicFallbackReason.CyclicSource,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
            });
        }

        [Fact]
        public Task StyledSafeSharedBorderBrushIsAcceptedAsync()
        {
            return InWindowAsync(static (_, source, owner) =>
            {
                Border shared = new();
                Style style = new(typeof(Border));
                style.Setters.Add(new Setter(Border.BorderBrushProperty, new VisualBrush(shared)));
                source.BorderThickness = new Thickness(2);
                source.Style = style;
                Assert.Equal(AcrylicFallbackReason.None,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
            });
        }

        [Fact]
        public Task TriggeredControlBorderBrushReferenceToOutputIsRejectedAndRecoversAsync()
        {
            return InWindowAsync(static (root, source, owner) =>
            {
                Border output = new();
                using IDisposable registration = AcrylicSourceMap.Register(owner, source, output);
                Button button = new() { Width = 30, Height = 20, BorderThickness = new Thickness(2) };
                Style style = new(typeof(Button));
                style.Setters.Add(new Setter(System.Windows.Controls.Control.BorderBrushProperty, new SolidColorBrush(Colors.Red)));
                Trigger trigger = new() { Property = FrameworkElement.TagProperty, Value = "feedback" };
                trigger.Setters.Add(new Setter(System.Windows.Controls.Control.BorderBrushProperty, new VisualBrush(output)));
                style.Triggers.Add(trigger);
                button.Style = style;
                source.Child = button;
                root.UpdateLayout();
                Assert.Equal(AcrylicFallbackReason.None,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
                button.Tag = "feedback";
                Assert.Equal(AcrylicFallbackReason.CyclicSource,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
                button.Tag = null;
                Assert.Equal(AcrylicFallbackReason.None,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
            });
        }

        [Fact]
        public Task TwoHopBrushCycleIsRejectedAsync()
        {
            return InWindowAsync(static (_, source, owner) =>
            {
                Border other = new() { Background = new VisualBrush(source) };
                source.Background = new VisualBrush(other);
                Assert.Equal(AcrylicFallbackReason.CyclicSource,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
            });
        }

        [Fact]
        public Task LongerBrushCycleIsRejectedAsync()
        {
            return InWindowAsync(static (_, source, owner) =>
            {
                Border first = new();
                Border second = new();
                first.Background = new VisualBrush(second);
                second.Background = new DrawingBrush(new GeometryDrawing(new VisualBrush(source), pen: null,
                    geometry: new RectangleGeometry(new Rect(0, 0, 10, 10))));
                source.Background = new VisualBrush(first);
                Assert.Equal(AcrylicFallbackReason.CyclicSource,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
            });
        }

        [Fact]
        public Task SharedAcyclicVisualBrushContentIsAcceptedAsync()
        {
            return InWindowAsync(static (root, source, owner) =>
            {
                Border shared = new();
                StackPanel content = new();
                _ = content.Children.Add(new Border { Background = new VisualBrush(shared) });
                _ = content.Children.Add(new Border { Background = new VisualBrush(shared) });
                source.Child = content;
                root.UpdateLayout();
                Assert.Equal(AcrylicFallbackReason.None,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
            });
        }

        [Fact]
        public Task IndirectBrushReferenceToRegisteredOutputIsRejectedAsync()
        {
            return InWindowAsync(static (_, source, owner) =>
            {
                Border output = new();
                using IDisposable registration = AcrylicSourceMap.Register(owner, source, output);
                Border other = new() { Background = new VisualBrush(output) };
                source.Background = new VisualBrush(other);
                Assert.Equal(AcrylicFallbackReason.CyclicSource,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
            });
        }

        [Fact]
        public Task IndirectBrushReferenceToRegisteredOwnerIsRejectedAsync()
        {
            return InWindowAsync(static (_, source, owner) =>
            {
                Border otherOwner = new();
                using IDisposable registration = AcrylicSourceMap.Register(otherOwner, source: null, new Border());
                Border other = new() { Background = new VisualBrush(otherOwner) };
                source.Background = new VisualBrush(other);
                Assert.Equal(AcrylicFallbackReason.CyclicSource,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
            });
        }

        [Fact]
        public Task DrawingGroupOpacityMaskReferenceToOutputIsRejectedAsync()
        {
            return InWindowAsync(static (_, source, owner) =>
            {
                Border output = new();
                using IDisposable registration = AcrylicSourceMap.Register(owner, source, output);
                DrawingGroup group = new() { OpacityMask = new VisualBrush(output) };
                source.Background = new DrawingBrush(group);
                Assert.Equal(AcrylicFallbackReason.CyclicSource,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
            });
        }

        [Fact]
        public Task DrawingImageReferenceToOutputIsRejectedAsync()
        {
            return InWindowAsync(static (_, source, owner) =>
            {
                Border output = new();
                using IDisposable registration = AcrylicSourceMap.Register(owner, source, output);
                Drawing drawing = new GeometryDrawing(new VisualBrush(output), pen: null,
                    geometry: new RectangleGeometry(new Rect(0, 0, 10, 10)));
                source.Child = new Image { Width = 10, Height = 10, Source = new DrawingImage(drawing) };
                Assert.Equal(AcrylicFallbackReason.CyclicSource,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
            });
        }

        [Fact]
        public Task HwndHostBehindVisualBrushIsRejectedAsync()
        {
            return InWindowAsync(static (_, source, owner) =>
            {
                Border other = new() { Child = new WebBrowser { Visibility = Visibility.Collapsed } };
                source.Background = new VisualBrush(other);
                Assert.Equal(AcrylicFallbackReason.HwndHostBoundary,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
            });
        }

        [Fact]
        public Task UnregisteredBrushReferenceToOwnerOrSourceIsRejectedAsync()
        {
            return InWindowAsync(static (_, source, owner) =>
            {
                source.Background = new VisualBrush(owner);
                Assert.Equal(AcrylicFallbackReason.CyclicSource,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);

                source.Background = new VisualBrush(source);
                Assert.Equal(AcrylicFallbackReason.CyclicSource,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
            });
        }

        [Fact]
        public Task CrossOwnerGraphCycleIsRejectedWithoutVisualAncestryAsync()
        {
            return InWindowAsync(static (_, source, owner) =>
            {
                using IDisposable first = AcrylicSourceMap.Register(owner, source, new Border());
                Border secondOwner = source;
                Border secondSource = owner;
                using IDisposable second = AcrylicSourceMap.Register(secondOwner, secondSource, new Border());
                Assert.Equal(AcrylicFallbackReason.CyclicSource,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
                second.Dispose();
                Assert.Equal(AcrylicFallbackReason.None,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
            });
        }

        [Theory]
        [InlineData(1.0, 8)]
        [InlineData(1.5, 12)]
        [InlineData(2.0, 16)]
        public void TranslationMapsPadAndCropAtDeviceScale(double scale, int expectedPad)
        {
            AcrylicSourceMapResult result = AcrylicSourceMap.CalculateGeometry(
                new Matrix(1, 0, 0, 1, 10, 20), new Rect(0, 0, 200, 200),
                new Size(80, 60), 8, new Matrix(scale, 0, 0, scale, 0, 0));

            Assert.Equal(AcrylicFallbackReason.None, result.Reason);
            Assert.Equal(new Rect(0, 0, 80, 60), result.TargetRect);
            Assert.Equal(new Rect(0, 0, 80, 60), result.TargetCrop);
            Assert.Equal(new Rect(-8, -8, 96, 76), result.PaddedTargetRect);
            Assert.Equal(new Rect(2, 12, 96, 76), result.SourceRect);
            Assert.Equal(expectedPad, result.PhysicalPadX);
            Assert.Equal(expectedPad, result.PhysicalPadY);
            Assert.Equal(scale, result.DeviceScaleX);
            Assert.Equal(scale, result.DeviceScaleY);
            Assert.Equal(new Point(10, 20), result.OwnerToSource.Transform(new Point(0, 0)));
            Assert.Equal(new Point(0, 0), result.SourceToOwner.Transform(new Point(10, 20)));
        }

        [Fact]
        public void ScaledSourceClampsPaddedSourceBounds()
        {
            AcrylicSourceMapResult result = AcrylicSourceMap.CalculateGeometry(
                new Matrix(2, 0, 0, 1.5, 10, 20), new Rect(0, 0, 300, 300),
                new Size(80, 60), 8, new Matrix(1.5, 0, 0, 1.5, 0, 0));

            Assert.Equal(AcrylicFallbackReason.None, result.Reason);
            Assert.Equal(new Rect(0, 8, 186, 114), result.SourceRect);
            Assert.Equal(new Rect(-8, -8, 96, 76), result.PaddedTargetRect);
            Assert.Equal(new Point(12, 21.5), result.OwnerToSource.Transform(new Point(1, 1)));
            Assert.Equal(new Point(1, 1), result.SourceToOwner.Transform(new Point(12, 21.5)));
        }

        [Fact]
        public void RectangularViewportClipChangesTargetCropAndSourceRect()
        {
            AcrylicSourceMapResult result = AcrylicSourceMap.CalculateGeometry(
                new Matrix(1, 0, 0, 1, 10, 20), new Rect(20, 20, 40, 30),
                new Size(80, 60), 8, Matrix.Identity);

            Assert.Equal(AcrylicFallbackReason.None, result.Reason);
            Assert.Equal(new Rect(10, 0, 40, 30), result.TargetCrop);
            Assert.Equal(new Rect(2, -8, 56, 46), result.PaddedTargetRect);
            Assert.Equal(new Rect(20, 20, 40, 30), result.SourceRect);
        }

        [Theory]
        [InlineData(-1.0, 0.0, 0.0, 1.0)]
        [InlineData(0.0, 0.0, 0.0, 1.0)]
        [InlineData(1.0, 0.1, 0.0, 1.0)]
        [InlineData(1.0, 0.0, 0.1, 1.0)]
        public void ReflectedZeroAndSkewedMatricesAreUnsupported(double m11, double m12, double m21, double m22)
        {
            AcrylicSourceMapResult result = AcrylicSourceMap.CalculateGeometry(
                new Matrix(m11, m12, m21, m22, 10, 20), new Rect(0, 0, 200, 200),
                new Size(80, 60), 8, Matrix.Identity);

            Assert.Equal(AcrylicFallbackReason.UnsupportedTransform, result.Reason);
        }

        [Fact]
        public void NonfiniteDeviceTransformIsUnsupported()
        {
            AcrylicSourceMapResult result = AcrylicSourceMap.CalculateGeometry(
                Matrix.Identity, new Rect(0, 0, 200, 200), new Size(80, 60), 8,
                new Matrix(1, double.NaN, 0, 1, 0, 0));

            Assert.Equal(AcrylicFallbackReason.UnsupportedTransform, result.Reason);
        }

        [Fact]
        public Task NonrectangularSourceClipIsUnsupportedAsync()
        {
            return InWindowAsync(static (_, source, owner) =>
            {
                source.Clip = new EllipseGeometry(new Rect(0, 0, 120, 100));
                Assert.Equal(AcrylicFallbackReason.UnsupportedClip,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
            });
        }

        [Fact]
        public Task RoundedRectangleSourceClipIsUnsupportedAndRecoversAsync()
        {
            return InWindowAsync(static (_, source, owner) =>
            {
                RectangleGeometry clip = new(new Rect(0, 0, 120, 100));
                source.Clip = clip;
                Assert.Equal(AcrylicFallbackReason.None,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
                clip.RadiusX = 5;
                Assert.Equal(AcrylicFallbackReason.UnsupportedClip,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
                clip.RadiusX = 0;
                Assert.Equal(AcrylicFallbackReason.None,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
            });
        }

        [Fact]
        public Task HwndHostBelowSourceIsRejectedAsync()
        {
            return InWindowAsync(static (_, source, owner) =>
            {
                source.Child = new WebBrowser { Visibility = Visibility.Collapsed };
                Assert.Equal(AcrylicFallbackReason.HwndHostBoundary,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
            });
        }

        [Fact]
        public Task ReflectedAndRotatedLiveSourcesAreRejectedAsync()
        {
            return InWindowAsync(static (_, source, owner) =>
            {
                source.RenderTransformOrigin = new Point(0.5, 0.5);
                source.RenderTransform = new ScaleTransform(-1, 1);
                source.UpdateLayout();
                WpfTestSta.DrainDispatcher(source.Dispatcher);
                Assert.Equal(AcrylicFallbackReason.UnsupportedTransform,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);

                source.RenderTransform = new RotateTransform(15);
                source.UpdateLayout();
                WpfTestSta.DrainDispatcher(source.Dispatcher);
                Assert.Equal(AcrylicFallbackReason.UnsupportedTransform,
                    AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8).Reason);
            });
        }

        [Fact]
        public Task LiveAxisAlignedScaleMapsWithoutBoundingBoxGuessAsync()
        {
            return InWindowAsync(static (_, source, owner) =>
            {
                source.RenderTransformOrigin = new Point(0.5, 0.5);
                source.RenderTransform = new ScaleTransform(1.5, 1.5);
                source.UpdateLayout();
                WpfTestSta.DrainDispatcher(source.Dispatcher);
                AcrylicSourceMapResult result = AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8);
                Assert.Equal(AcrylicFallbackReason.None, result.Reason);
                Assert.Equal(2.0 / 3.0, result.OwnerToSource.M11, 6);
                Assert.Equal(2.0 / 3.0, result.OwnerToSource.M22, 6);
                Assert.Equal(0, result.OwnerToSource.M12);
                Assert.Equal(0, result.OwnerToSource.M21);
            });
        }

        [Fact]
        public Task NonAffineTransformMatchingOldSamplesIsRejectedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GeneralTransform transform = new SampleAliasingTransform();
                Assert.False(AcrylicSourceMap.TryGetAffineAxisAlignedMatrix(transform, 80, 60, out _));
                Assert.True(transform.TryTransform(new Point(20, 15), out Point warped));
                Assert.NotEqual(20, warped.X);
            });
        }

        [Fact]
        public Task NestedAffineGeneralTransformGroupsComposeInOrderAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GeneralTransformGroup nested = new();
                nested.Children.Add(new ScaleTransform(2, 3));
                nested.Children.Add(new TranslateTransform(5, 7));
                GeneralTransformGroup outer = new();
                outer.Children.Add(nested);
                outer.Children.Add(new TranslateTransform(11, 13));
                Assert.True(AcrylicSourceMap.TryGetAffineAxisAlignedMatrix(outer, 80, 60, out Matrix matrix));
                Assert.Equal(new Matrix(2, 0, 0, 3, 16, 20), matrix);
            });
        }

        private sealed class SampleAliasingTransform : GeneralTransform
        {
            public override GeneralTransform? Inverse => null;

            public override Rect TransformBounds(Rect rect)
            {
                return rect;
            }

            public override bool TryTransform(Point inPoint, out Point result)
            {
                result = new(inPoint.X + (8 * Math.Sin(Math.PI * inPoint.X / 40)), inPoint.Y);
                return true;
            }

            protected override Freezable CreateInstanceCore()
            {
                return new SampleAliasingTransform();
            }
        }

        [Fact]
        public Task ScrollOffsetRecomputesMappedSourceBoundsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Window window = new() { Width = 360, Height = 240, ShowInTaskbar = false };
                Grid root = new();
                ScrollViewer scroller = new()
                {
                    Width = 160,
                    Height = 110,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                };
                StackPanel source = new() { Width = 160 };
                _ = source.Children.Add(new Border { Height = 300 });
                scroller.Content = source;
                Border owner = new() { Width = 80, Height = 60 };
                _ = root.Children.Add(scroller);
                _ = root.Children.Add(owner);
                try
                {
                    window.Content = root;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    AcrylicSourceMapResult before = AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8);
                    scroller.ScrollToVerticalOffset(40);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    AcrylicSourceMapResult after = AcrylicSourceMap.Evaluate(owner, source, new Size(80, 60), 8);

                    Assert.Equal(AcrylicFallbackReason.None, before.Reason);
                    Assert.Equal(AcrylicFallbackReason.None, after.Reason);
                    Assert.Equal(40, after.SourceRect.Y - before.SourceRect.Y, 1);
                    Assert.Equal(before.TargetCrop, after.TargetCrop);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task SourceBehindClippedScrollViewportCannotBeSampledOutsideViewportAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Canvas root = new();
                ScrollViewer scroller = new()
                {
                    Width = 160,
                    Height = 110,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                };
                StackPanel source = new() { Width = 160 };
                _ = source.Children.Add(new Border { Height = 300, Background = Brushes.CadetBlue });
                scroller.Content = source;
                Border owner = new() { Width = 80, Height = 50 };
                Canvas.SetTop(owner, 130);
                _ = root.Children.Add(scroller);
                _ = root.Children.Add(owner);
                Window window = new() { Content = root, Width = 260, Height = 230, ShowInTaskbar = false };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(source.ActualHeight > 180);
                    Assert.True(scroller.ActualHeight < 120);
                    AcrylicSourceMapResult map = AcrylicSourceMap.Evaluate(owner, source, new Size(80, 50), 8);
                    Assert.Equal(AcrylicFallbackReason.UnavailableSource, map.Reason);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task RegistrationDoesNotRootOwnerSourceOrOutputAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                IDisposable lease = CreateWeakRegistration(out WeakReference owner,
                    out WeakReference source, out WeakReference output);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                Assert.False(owner.IsAlive);
                Assert.False(source.IsAlive);
                Assert.False(output.IsAlive);
                lease.Dispose();
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IDisposable CreateWeakRegistration(out WeakReference ownerRef,
            out WeakReference sourceRef, out WeakReference outputRef)
        {
            Border owner = new();
            Border source = new();
            Border output = new();
            ownerRef = new WeakReference(owner);
            sourceRef = new WeakReference(source);
            outputRef = new WeakReference(output);
            return AcrylicSourceMap.Register(owner, source, output);
        }

        private static Task InWindowAsync(Action<Grid, Border, Border> assertion)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                _ = WpfTestSta.EnsureApplication();
                Window window = new() { Width = 360, Height = 240, ShowInTaskbar = false };
                Grid root = new();
                Border source = new() { Width = 120, Height = 100 };
                Border owner = new() { Width = 80, Height = 60 };
                _ = root.Children.Add(source);
                _ = root.Children.Add(owner);
                try
                {
                    window.Content = root;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    assertion(root, source, owner);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                }
            });
        }
    }
}
