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
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Fluence.Wpf.Helpers;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Control
{
    public sealed class AcrylicBrushContractTests
    {
        [Fact]
        public Task CompiledDirectBorderRectangleAndEllipseGetDistinctLiveBrushesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                ResourceDictionary dictionary = Load("AcrylicDirectUses.xaml");
                Grid root = Assert.IsType<Grid>(dictionary["DirectUses"]);
                Border source = Assert.IsType<Border>(VisualTree.FindVisualChildByName<Border>(root, "BackdropRoot"));
                Border border = Assert.IsType<Border>(VisualTree.FindVisualChildByName<Border>(root, "DirectBorder"));
                Rectangle rectangle = Assert.IsType<Rectangle>(VisualTree.FindVisualChildByName<Rectangle>(root, "DirectRectangle"));
                Ellipse ellipse = Assert.IsType<Ellipse>(VisualTree.FindVisualChildByName<Ellipse>(root, "DirectEllipse"));
                Border missing = Assert.IsType<Border>(VisualTree.FindVisualChildByName<Border>(root, "MissingSourceBorder"));
                NameScope.SetNameScope(root, new NameScope());
                root.RegisterName("BackdropRoot", source);
                VisualBrush borderBrush = Assert.IsType<VisualBrush>(border.Background);
                VisualBrush rectangleBrush = Assert.IsType<VisualBrush>(rectangle.Fill);
                VisualBrush ellipseBrush = Assert.IsType<VisualBrush>(ellipse.Fill);
                VisualBrush missingBrush = Assert.IsType<VisualBrush>(missing.Background);
                Assert.NotSame(borderBrush, rectangleBrush);
                Assert.NotSame(borderBrush, ellipseBrush);
                Assert.NotSame(rectangleBrush, ellipseBrush);
                Assert.NotSame(borderBrush.Visual, rectangleBrush.Visual);
                Assert.NotNull(missingBrush.Visual);

                Window window = new() { Width = 300, Height = 240, Content = root, ShowInTaskbar = false };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.NotNull(borderBrush.Visual);
                    Assert.NotNull(rectangleBrush.Visual);
                    Assert.NotNull(ellipseBrush.Visual);
                    Assert.NotNull(missingBrush.Visual);
                    border.Background = Brushes.Black;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Null(borderBrush.Visual);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                }
            });
        }

        [Theory]
        [InlineData("StyleProbe.xaml", "ProbeStyle")]
        [InlineData("ExtensionResourceProbe.xaml", "SharedConsumer")]
        public Task CompiledSharedContextsRejectWithoutSharedMutableBrushAsync(string file, string key)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                _ = WpfTestSta.EnsureApplication();
                Exception error = Assert.ThrowsAny<Exception>(() =>
                {
                    ResourceDictionary dictionary = Load(file);
                    if (string.Equals(file, "StyleProbe.xaml", StringComparison.Ordinal))
                    {
                        Border target = new() { Style = Assert.IsType<Style>(dictionary[key]) };
                        Window window = new() { Content = target, Width = 100, Height = 100, ShowInTaskbar = false };
                        try
                        {
                            window.Show();
                            WpfTestSta.DrainDispatcher(window.Dispatcher);
                        }
                        finally
                        {
                            VisualTree.CloseWindowAndDrain(window);
                        }
                    }
                    else
                    {
                        _ = dictionary[key];
                    }
                });
                Assert.Contains("concrete", error.ToString(), StringComparison.OrdinalIgnoreCase);
            });
        }

        [Fact]
        public Task CompiledDeferredUsesRemainDistinctAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                ResourceDictionary dictionary = Load("DeferredResourceProbe.xaml");
                Border first = Assert.IsType<Border>(dictionary["DeferredBorder"]);
                Border second = Assert.IsType<Border>(dictionary["DeferredBorder"]);
                Assert.NotSame(first, second);
                Assert.NotSame(first.Background, second.Background);
            });
        }

        [Fact]
        public Task CompiledTemplateCreatesOneBrushPerInstanceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                ResourceDictionary dictionary = Load("TemplateProbe.xaml");
                ControlTemplate template = Assert.IsType<ControlTemplate>(dictionary["ProbeTemplate"]);
                Grid root = new();
                NameScope.SetNameScope(root, new NameScope());
                Border source = new() { Name = "BackdropRoot", Width = 180, Height = 140 };
                root.RegisterName(source.Name, source);
                _ = root.Children.Add(source);
                ContentControl first = new() { Template = template, Width = 50, Height = 35 };
                ContentControl second = new() { Template = template, Width = 50, Height = 35 };
                _ = root.Children.Add(first);
                _ = root.Children.Add(second);
                Window window = new() { Content = root, Width = 250, Height = 190, ShowInTaskbar = false };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Border firstBorder = Assert.IsType<Border>(VisualTree.FindVisualChild<Border>(first));
                    Border secondBorder = Assert.IsType<Border>(VisualTree.FindVisualChild<Border>(second));
                    Assert.NotSame(firstBorder.Background, secondBorder.Background);
                    _ = Assert.IsType<VisualBrush>(firstBorder.Background);
                    _ = Assert.IsType<VisualBrush>(secondBorder.Background);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NestedBindingIntoClrRecipeIsRejectedByCompiledXamlAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Exception error = Assert.ThrowsAny<Exception>(static () =>
                {
                    ResourceDictionary dictionary = Load("NestedBindingProbe.xaml");
                    _ = dictionary["NestedBindingConsumer"];
                });
                Assert.Contains("Binding", error.ToString(), StringComparison.OrdinalIgnoreCase);
            });
        }

        [Fact]
        public Task CompiledMissingSourceRetargetsAndReloadsWithTheSameBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                ResourceDictionary dictionary = Load("AcrylicDirectUses.xaml");
                Grid root = Assert.IsType<Grid>(dictionary["DirectUses"]);
                Border owner = Assert.IsType<Border>(VisualTree.FindVisualChildByName<Border>(root, "MissingSourceBorder"));
                VisualBrush brush = Assert.IsType<VisualBrush>(owner.Background);
                AcrylicMaterialController controller = GetDirectController(owner);
                NameScope.SetNameScope(root, new NameScope());
                Window window = new() { Content = root, Width = 270, Height = 210, ShowInTaskbar = false };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(AcrylicFallbackReason.UnavailableSource, controller.Reason);

                    Border first = new() { Name = "NotRegistered", Width = 180, Height = 130, Background = Brushes.DarkOrange };
                    root.RegisterName(first.Name, first);
                    _ = root.Children.Add(first);
                    root.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Same(first, controller.CurrentSource);
                    Assert.Equal(AcrylicFallbackReason.None, controller.Reason);

                    root.UnregisterName(first.Name);
                    root.Children.Remove(first);
                    Border replacement = new() { Name = "NotRegistered", Width = 180, Height = 130, Background = Brushes.DarkBlue };
                    root.RegisterName(replacement.Name, replacement);
                    _ = root.Children.Add(replacement);
                    root.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Same(replacement, controller.CurrentSource);
                    Assert.Same(brush, owner.Background);

                    root.Children.Remove(owner);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Null(controller.CurrentSource);
                    Assert.Same(brush, owner.Background);
                    _ = root.Children.Add(owner);
                    root.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Same(replacement, controller.CurrentSource);
                    Assert.Same(brush, owner.Background);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task CompiledDirectBrushPrefersNearestNamescopeSourceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                ResourceDictionary dictionary = Load("AcrylicDirectUses.xaml");
                Grid root = Assert.IsType<Grid>(dictionary["DirectUses"]);
                Border outer = Assert.IsType<Border>(VisualTree.FindVisualChildByName<Border>(root, "BackdropRoot"));
                Border direct = Assert.IsType<Border>(VisualTree.FindVisualChildByName<Border>(root, "DirectBorder"));
                NameScope.SetNameScope(root, new NameScope());
                root.RegisterName("BackdropRoot", outer);
                root.Children.Remove(direct);
                Grid nested = new();
                NameScope.SetNameScope(nested, new NameScope());
                Border inner = new() { Name = "BackdropRoot", Background = Brushes.CadetBlue };
                nested.RegisterName(inner.Name, inner);
                _ = nested.Children.Add(inner);
                _ = nested.Children.Add(direct);
                _ = root.Children.Add(nested);
                Window window = new() { Content = root, Width = 300, Height = 240, ShowInTaskbar = false };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    AcrylicMaterialController controller = GetDirectController(direct);
                    Assert.Same(inner, controller.CurrentSource);
                    Assert.NotEqual(AcrylicFallbackReason.UnavailableSource, controller.Reason);
                    Assert.NotEqual(AcrylicFallbackReason.CyclicSource, controller.Reason);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task CompiledPathFillRejectsUnsupportedTargetAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Exception error = Assert.ThrowsAny<Exception>(static () =>
                {
                    ResourceDictionary dictionary = Load("PathProbe.xaml");
                    _ = dictionary["PathConsumer"];
                });
                Assert.Contains("concrete", error.ToString(), StringComparison.OrdinalIgnoreCase);
            });
        }

        private static AcrylicMaterialController GetDirectController(Border owner)
        {
            FieldInfo field = Assert.IsType<FieldInfo>(typeof(AcrylicMaterialController).GetField(
                "DirectControllerProperty", BindingFlags.Static | BindingFlags.NonPublic), exactMatch: false);
            DependencyProperty property = Assert.IsType<DependencyProperty>(field.GetValue(obj: null));
            return Assert.IsType<AcrylicMaterialController>(owner.GetValue(property));
        }

        private static ResourceDictionary Load(string name)
        {
            return new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Fluence.Wpf.Tests;component/Assets/Acrylic/" + name),
            };
        }
    }
}
