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

using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Xml.Linq;
using Fluence.Wpf.Demo;
using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Gallery.Pages
{
    public sealed class GalleryAcrylicPageTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureDemoTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }

        [Fact]
        public void DemoNavigationCatalog_RegistersAcrylicRoute()
        {
            DemoNavigationItem item = Assert.Single(DemoNavigationCatalog.Items, static entry => string.Equals(entry.Route, "acrylic", System.StringComparison.Ordinal));
            Assert.Equal("Acrylic", item.Title);
            Assert.Equal(typeof(GalleryAcrylicPage), item.PageType);
            Assert.Contains("backdrop", item.Keywords, System.StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(item.Glyph));
        }

        [Fact]
        public Task GalleryAcrylicPage_ShowsDirectAndHostSamplesWithSourceBoundaryAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryAcrylicPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    GalleryPageHeader header = Assert.Single(DemoTestHost.FindVisualChildren<GalleryPageHeader>(page));
                    Assert.Equal("acrylic-materials", header.DocsAnchor);
                    List<DemoSampleControl> samples = [.. DemoTestHost.FindVisualChildren<DemoSampleControl>(page)];
                    Assert.Equal(2, samples.Count);
                    Assert.All(samples, static sample =>
                    {
                        Assert.False(string.IsNullOrWhiteSpace(sample.XamlSource));
                        Assert.False(string.IsNullOrWhiteSpace(sample.CSharpSource));
                    });
                    Assert.Contains("Border.Background", samples[0].XamlSource, System.StringComparison.Ordinal);
                    Assert.Contains("AcrylicBrush", samples[0].XamlSource, System.StringComparison.Ordinal);
                    Assert.Contains("AcrylicElement", samples[0].XamlSource, System.StringComparison.Ordinal);
                    Assert.Contains("BackdropSource", samples[0].XamlSource, System.StringComparison.Ordinal);
                    Assert.Contains("Popup", samples[1].XamlSource, System.StringComparison.Ordinal);
                    Assert.Contains("source", samples[0].SampleDescription, System.StringComparison.OrdinalIgnoreCase);

                    Grid source = Assert.IsType<Grid>(page.FindName("BackdropSource"), exactMatch: false);
                    Border direct = Assert.IsType<Border>(page.FindName("DirectAcrylicBorder"), exactMatch: false);
                    Controls.AcrylicElement host = Assert.Single(DemoTestHost.FindVisualChildren<Controls.AcrylicElement>(page));
                    Assert.Same(source.Parent, direct.Parent);
                    Assert.Same(source.Parent, host.Parent);
                    Assert.Equal(3, Grid.GetColumnSpan(source));
                    Assert.Same(source, host.BackdropSource);
                    FieldInfo controllerField = Assert.IsType<FieldInfo>(typeof(Helpers.AcrylicMaterialController)
                        .GetField("DirectControllerProperty", BindingFlags.Static | BindingFlags.NonPublic), exactMatch: false);
                    DependencyProperty attached = Assert.IsType<DependencyProperty>(controllerField.GetValue(obj: null));
                    Helpers.AcrylicMaterialController directController =
                        Assert.IsType<Helpers.AcrylicMaterialController>(direct.GetValue(attached));
                    Assert.Same(source, directController.CurrentSource);
                    Assert.Equal(Helpers.AcrylicFallbackReason.None, directController.Reason);
                    TextBlock directLabel = Assert.IsType<TextBlock>(direct.Child);
                    Assert.Equal(Color.FromRgb(0x1F, 0x1F, 0x1F), Assert.IsType<SolidColorBrush>(directLabel.Foreground).Color);
                    Canvas directVisual = Assert.IsType<Canvas>(Assert.IsType<VisualBrush>(direct.Background).Visual);
                    Assert.Equal(2, directVisual.Children.Count);
                    Rectangle material = Assert.IsType<Rectangle>(directVisual.Children[0]);
                    Rectangle colorPass = Assert.IsType<Rectangle>(Assert.IsType<VisualBrush>(material.Fill).Visual);
                    Assert.True(colorPass.Effect is Helpers.AcrylicBlendEffect);
                    Rectangle luminosityPass = Assert.IsType<Rectangle>(Assert.IsType<VisualBrush>(colorPass.Fill).Visual);
                    UIElement blurredSource = Assert.IsType<UIElement>(Assert.IsType<VisualBrush>(luminosityPass.Fill).Visual, exactMatch: false);
                    BlurEffect blur = Assert.IsType<BlurEffect>(blurredSource.Effect);
                    Assert.Equal(KernelType.Gaussian, blur.KernelType);
                    Assert.Equal(Helpers.AcrylicFallbackReason.None,
                        Helpers.AcrylicSourceMap.Evaluate(host, source,
                            new Size(host.ActualWidth, host.ActualHeight), 8).Reason);
                    Assert.Equal(260, Assert.IsType<StackPanel>(samples[0].RightRailContent).MaxWidth);
                    Assert.Equal(0.5, Assert.IsType<AcrylicMaterialRecipe>(page.Resources["GalleryAcrylicFixedRecipe"]).TintOpacity);

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Color.FromRgb(0x1F, 0x1F, 0x1F), Assert.IsType<SolidColorBrush>(directLabel.Foreground).Color);

                    ApplicationThemeManager.Apply(ApplicationTheme.HighContrast);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Helpers.AcrylicFallbackReason.HighContrast, directController.Reason);
                    Assert.Equal(SystemColors.WindowTextColor, Assert.IsType<SolidColorBrush>(directLabel.Foreground).Color);

                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Color.FromRgb(0x1F, 0x1F, 0x1F), Assert.IsType<SolidColorBrush>(directLabel.Foreground).Color);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                    _ = TestApp.EnsureDemoTheme();
                }
            });
        }

        [Fact]
        public Task GalleryAcrylicPage_ShowsActualFallbackReasonsForHostAndPopupAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryAcrylicPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Controls.AcrylicElement host = Assert.Single(DemoTestHost.FindVisualChildren<Controls.AcrylicElement>(page));
                    CheckBox forceFallback = Assert.IsType<CheckBox>(page.FindName("FallbackCheckBox"), exactMatch: false);
                    TextBlock hostReason = Assert.IsType<TextBlock>(page.FindName("FallbackReasonText"), exactMatch: false);

                    forceFallback.IsChecked = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(host.IsUsingFallback);
                    Assert.Equal("Always", host.FallbackReason.ToString());
                    Assert.Equal(host.FallbackReason.ToString(), hostReason.Text);

                    ButtonBase openPopup = Assert.IsType<Controls.Button>(page.FindName("OpenPopupButton"), exactMatch: false);
                    openPopup.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    TextBlock popupReason = Assert.IsType<TextBlock>(page.FindName("PopupFallbackReasonText"), exactMatch: false);
                    Assert.Equal("DifferentHwnd", popupReason.Text);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public Task DisplayedAcrylicSamplesHaveOneUserControlContentRootAndPublicPopupStateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryAcrylicPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    List<DemoSampleControl> samples = [.. DemoTestHost.FindVisualChildren<DemoSampleControl>(page)];
                    Assert.Equal(2, samples.Count);
                    foreach (DemoSampleControl sample in samples)
                    {
                        XElement root = XDocument.Parse(sample.XamlSource).Root!;
                        Assert.Equal("UserControl", root.Name.LocalName);
                        XElement content = Assert.Single(root.Elements(), static element =>
                            !element.Name.LocalName.Contains(".", System.StringComparison.Ordinal));
                        Assert.Equal("Grid", content.Name.LocalName);
                    }

                    Assert.Contains("popupHost.IsUsingFallback", samples[1].CSharpSource, System.StringComparison.Ordinal);
                    Assert.DoesNotContain("popupHost.FallbackReason", samples[1].CSharpSource, System.StringComparison.Ordinal);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }
    }
}
