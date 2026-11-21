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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Fluence.Wpf.Demo;
using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Helpers;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Gallery.Pages
{
    /// <summary>
    /// Covers <see cref="GalleryHomePage"/>.
    /// </summary>
    public sealed class GalleryHomePageTests : IAsyncLifetime
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
        public Task GalleryHomePage_HeroUsesThemeAcrylicWithHighContrastFallbackAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryHomePage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    Image mark = Find<Image>(page, "BrandHeroImage");
                    Rectangle lockupMark = Find<Rectangle>(page, "HeroLockupMark");
                    DrawingImage drawing = Assert.IsType<DrawingImage>(
                        Application.Current.TryFindResource("FluenceIconBrandDrawingImage"));
                    Controls.AcrylicElement acrylic = Find<Controls.AcrylicElement>(page, "FeaturedRowAcrylic");
                    FrameworkElement source = Find<FrameworkElement>(page, "HeroBackdropSource");
                    FrameworkElement artwork = Find<FrameworkElement>(page, "HeroArtwork");
                    UniformGrid lowerCatalog = Find<UniformGrid>(page, "ControlsCatalogGrid");
                    TextBlock title = Find<TextBlock>(page, "HeroTitle");
                    TextBlock description = Find<TextBlock>(page, "HeroDescription");
                    TextBlock compatibility = Find<TextBlock>(page, "HeroCompatibility");

                    Assert.Same(source, acrylic.BackdropSource);
                    Assert.False(acrylic.IsHitTestVisible);
                    Assert.Equal(AcrylicFallbackReason.None, AcrylicSourceMap.Evaluate(
                        acrylic, source, acrylic.RenderSize, AcrylicMaterialRenderer.BlurRadiusDip).Reason);

                    foreach (ApplicationTheme theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                    {
                        ApplicationThemeManager.Apply(theme, WindowBackdropType.None);
                        Settle(window);

                        Assert.Same(drawing, mark.Source);
                        if (theme is ApplicationTheme.Light)
                        {
                            ImageBrush lockup = Assert.IsType<ImageBrush>(lockupMark.Fill);
                            Assert.Same(drawing, lockup.ImageSource);
                            Assert.True(lockup.Viewbox.Height < 1);
                            Assert.True(lockupMark.IsVisible);
                            Assert.False(lockupMark.IsHitTestVisible);
                        }
                        Assert.Same(Assert.IsType<AcrylicMaterialRecipe>(page.TryFindResource("AcrylicInAppDefaultRecipe")), acrylic.Recipe);
                        Color expected = Assert.IsType<SolidColorBrush>(
                            page.TryFindResource("TextFillColorPrimaryBrush"), exactMatch: false).Color;
                        Assert.Equal(theme is not ApplicationTheme.HighContrast, artwork.IsVisible);
                        foreach (TextBlock text in new[] { title, description, compatibility })
                        {
                            Assert.Equal(expected, Assert.IsType<SolidColorBrush>(text.Foreground, exactMatch: false).Color);
                        }

                        AssertOpaqueBackground(Assert.IsType<Controls.Card>(lowerCatalog.Children[0]));
                        if (theme is ApplicationTheme.HighContrast)
                        {
                            Assert.True(acrylic.IsUsingFallback);
                            Assert.Equal(AcrylicFallbackReason.HighContrast, acrylic.FallbackReason);
                        }
                    }
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public Task GalleryHomePage_UsesProminentAccessibleBrandAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryHomePage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    Image brand = Find<Image>(page, "BrandHeroImage");
                    DrawingImage drawing = Assert.IsType<DrawingImage>(brand.Source);
                    Assert.InRange(drawing.Drawing.Bounds.Width, 1024, 1025);
                    Assert.Equal(1024, drawing.Drawing.Bounds.Height);
                    Assert.Equal(Stretch.Uniform, brand.Stretch);
                    Assert.True(brand.IsVisible);
                    Assert.False(brand.IsHitTestVisible);

                    FrameworkElement artworkLayer = Find<FrameworkElement>(page, "HeroArtwork");
                    Assert.True(artworkLayer.ClipToBounds);
                    Assert.NotEqual(0, Assert.IsType<RotateTransform>(brand.RenderTransform).Angle);
                    Rect artworkLayerBounds = new(artworkLayer.RenderSize);
                    Rect markBounds = brand.TransformToAncestor(artworkLayer).TransformBounds(new Rect(brand.RenderSize));
                    Assert.True(artworkLayerBounds.IntersectsWith(markBounds));
                    Assert.True(markBounds.Left < artworkLayerBounds.Left);
                    Assert.True(markBounds.Right > artworkLayerBounds.Right);
                    Assert.True(markBounds.Top < artworkLayerBounds.Top);
                    Assert.True(markBounds.Width > artworkLayerBounds.Width * 2);
                    LinearGradientBrush fade = Assert.IsType<LinearGradientBrush>(artworkLayer.OpacityMask);
                    Assert.Equal(BrushMappingMode.Absolute, fade.MappingMode);
                    Assert.InRange(fade.EndPoint.Y, artworkLayer.ActualHeight - 1, artworkLayer.ActualHeight + 1);
                    Assert.Equal(fade.StartPoint.X, fade.EndPoint.X);
                    Assert.True(fade.StartPoint.Y < fade.EndPoint.Y);
                    Assert.Contains(fade.GradientStops, static stop => stop.Offset is > 0.66 and < 0.68 && stop.Color.A is 255);
                    Assert.Contains(fade.GradientStops, static stop => stop.Offset > 0.999 && stop.Color.A is 0);

                    TextBlock title = Find<TextBlock>(page, "HeroTitle");
                    TextBlock description = Find<TextBlock>(page, "HeroDescription");
                    TextBlock compatibility = Find<TextBlock>(page, "HeroCompatibility");
                    Assert.True(title.IsVisible);
                    Assert.True(description.IsVisible);
                    Assert.True(compatibility.IsVisible);
                    Assert.Equal("Fluence.WPF", title.Text, StringComparer.Ordinal);
                    Assert.Equal(title.Text, UIElementAutomationPeer.CreatePeerForElement(title).GetName(), StringComparer.Ordinal);
                    Assert.Contains("WPF and PowerShell", description.Text, StringComparison.Ordinal);
                    Assert.Contains(".NET Framework 4.7.2", compatibility.Text, StringComparison.Ordinal);
                    Assert.Contains(".NET 8", compatibility.Text, StringComparison.Ordinal);
                    Assert.Contains(".NET 10", compatibility.Text, StringComparison.Ordinal);
                    Assert.Contains("PowerShell 5.1", compatibility.Text, StringComparison.Ordinal);
                    Assert.Contains("PowerShell 7.4", compatibility.Text, StringComparison.Ordinal);
                    UniformGrid featured = Find<UniformGrid>(page, "FeaturedControlsGrid");
                    Assert.True(featured.TranslatePoint(default, page).Y >= compatibility.TranslatePoint(default, page).Y + compatibility.ActualHeight);

                    (string Route, string FileName)[] artwork =
                    [
                        ("buttons", "Button.png"),
                        ("selection", "Checkbox.png"),
                        ("inputs", "TextBox.png"),
                        ("navigation", "NavigationView.png"),
                        ("tabs", "TabView.png"),
                        ("menus", "MenuFlyout.png"),
                        ("data", "ListView.png"),
                        ("trees", "TreeView.png"),
                        ("status", "InfoBar.png"),
                    ];
                    foreach ((string route, string fileName) in artwork)
                    {
                        Controls.Card card = Assert.Single(DemoTestHost.FindVisualChildren<Controls.Card>(page),
                            candidate => string.Equals(candidate.Tag as string, route, StringComparison.Ordinal));
                        Image illustration = Assert.IsType<Image>(card.Icon);
                        BitmapSource bitmap = Assert.IsType<BitmapSource>(illustration.Source, exactMatch: false);
                        BitmapSource expected = BitmapFrame.Create(new Uri(
                            "pack://application:,,,/Fluence.Wpf.Demo;component/Resources/ControlImages/" + fileName,
                            UriKind.Absolute));
                        Assert.Equal(expected.PixelWidth, bitmap.PixelWidth);
                        Assert.Equal(expected.PixelHeight, bitmap.PixelHeight);
                        Assert.Equal(expected.Format, bitmap.Format);
                        int stride = ((bitmap.PixelWidth * bitmap.Format.BitsPerPixel) + 7) / 8;
                        byte[] actualPixels = new byte[stride * bitmap.PixelHeight];
                        byte[] expectedPixels = new byte[stride * expected.PixelHeight];
                        bitmap.CopyPixels(actualPixels, stride, 0);
                        expected.CopyPixels(expectedPixels, stride, 0);
                        Assert.Equal(expectedPixels, actualPixels);
                        Assert.InRange(illustration.ActualWidth, 47, 49);
                        Assert.InRange(illustration.ActualHeight, 47, 49);
                    }

                    Assert.Null(DemoTestHost.FindByName<FrameworkElement>(page, "HeroPreviewCard"));
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public Task HomeActions_NavigateToTheirControlPagesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                MainWindow window = DemoTestHost.CreateShownMainWindow();
                try
                {
                    Controls.NavigationView navigation = Find<Controls.NavigationView>(window, "DemoNav");
                    Frame frame = Assert.IsType<Frame>(navigation.Content, exactMatch: false);
                    (string Route, Type PageType)[] destinations =
                    [
                        ("menus", typeof(GalleryMenusPage)),
                        ("trees", typeof(GalleryTreesPage)),
                        ("settings", typeof(GallerySettingsPage)),
                    ];
                    foreach ((string route, Type pageType) in destinations)
                    {
                        window.NavigateTo("home");
                        Settle(window);
                        GalleryHomePage home = Assert.IsType<GalleryHomePage>(frame.Content);
                        Controls.Card card = Assert.Single(DemoTestHost.FindVisualChildren<Controls.Card>(home),
                            candidate => string.Equals(candidate.Tag as string, route, StringComparison.Ordinal));
                        Assert.True(card.IsClickable);
                        Invoke(card);
                        Settle(window);
                        Assert.Equal(pageType, frame.Content.GetType());
                    }

                    window.NavigateTo("home");
                    Settle(window);
                    GalleryHomePage page = Assert.IsType<GalleryHomePage>(frame.Content);
                    Controls.Card buttonCard = Assert.Single(DemoTestHost.FindVisualChildren<Controls.Card>(page),
                        static candidate => string.Equals(candidate.Tag as string, "buttons", StringComparison.Ordinal));
                    Assert.True(buttonCard.IsClickable);
                    Invoke(buttonCard);
                    Settle(window);
                    _ = Assert.IsType<GalleryButtonsPage>(frame.Content);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public Task HomeLayout_ReflowsWithoutHorizontalOverflowAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryHomePage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    Controls.AcrylicElement acrylic = Find<Controls.AcrylicElement>(page, "FeaturedRowAcrylic");
                    FrameworkElement source = Find<FrameworkElement>(page, "HeroBackdropSource");
                    FrameworkElement artwork = Find<FrameworkElement>(page, "HeroArtwork");
                    string[] routes =
                    [
                        "buttons", "selection", "inputs", "navigation", "tabs", "menus", "data", "trees", "status",
                    ];
                    TextBlock title = Find<TextBlock>(page, "HeroTitle");
                    TextBlock description = Find<TextBlock>(page, "HeroDescription");
                    TextBlock compatibility = Find<TextBlock>(page, "HeroCompatibility");
                    Rectangle lockupMark = Find<Rectangle>(page, "HeroLockupMark");
                    StackPanel heading = Find<StackPanel>(page, "HeroHeadingText");
                    UniformGrid featured = Find<UniformGrid>(page, "FeaturedControlsGrid");
                    UniformGrid catalog = Find<UniformGrid>(page, "ControlsCatalogGrid");
                    TextBlock catalogHeading = Find<TextBlock>(page, "ControlsCatalogHeading");
                    UniformGrid foundations = Find<UniformGrid>(page, "FoundationLinksGrid");
                    Controls.SmoothScrollViewer scroll = Assert.Single(DemoTestHost.FindVisualChildren<Controls.SmoothScrollViewer>(page));
                    window.Height = 700;
                    (double Width, int Columns)[] sizes = [(1100, 3), (720, 2), (410, 1), (1100, 3)];
                    foreach ((double width, int columns) in sizes)
                    {
                        window.Width = width;
                        Settle(window);
                        Assert.Equal(columns, featured.Columns);
                        Assert.Equal(columns, catalog.Columns);
                        Assert.Equal(columns is 3 ? 3 : 1, foundations.Columns);
                        Assert.Equal(columns, featured.Children.Count);
                        Assert.Equal(routes.Length - columns, catalog.Children.Count);
                        Assert.InRange(source.ActualWidth, scroll.ViewportWidth - 1, scroll.ViewportWidth + 1);
                        Assert.True(acrylic.ActualWidth <= scroll.ViewportWidth + 1);
                        Assert.Equal(AcrylicFallbackReason.None, AcrylicSourceMap.Evaluate(
                            acrylic, source, acrylic.RenderSize, AcrylicMaterialRenderer.BlurRadiusDip).Reason);

                        DrawingBrush mask = Assert.IsType<DrawingBrush>(acrylic.OpacityMask);
                        DrawingGroup cardSilhouettes = Assert.IsType<DrawingGroup>(mask.Drawing);
                        Assert.Equal(columns, cardSilhouettes.Children.Count);
                        Controls.Card firstCard = Assert.IsType<Controls.Card>(featured.Children[0]);
                        double firstRowTop = firstCard.TranslatePoint(default, source).Y;
                        double firstRowBottom = firstRowTop + firstCard.ActualHeight;
                        Assert.InRange(artwork.ActualHeight, firstRowTop + 1, firstRowBottom - 1);

                        double lowerHeadingTop = catalogHeading.TranslatePoint(default, source).Y;
                        Assert.True(lowerHeadingTop >= firstRowBottom + 24);
                        Assert.True(catalog.TranslatePoint(default, source).Y >= lowerHeadingTop + catalogHeading.ActualHeight);

                        for (int index = 0; index < catalog.Children.Count; index++)
                        {
                            Controls.Card card = Assert.IsType<Controls.Card>(catalog.Children[index]);
                            Assert.Equal(routes[index + columns], card.Tag as string, StringComparer.Ordinal);
                            Assert.Equal(CardVariant.Default, card.Variant);
                            AssertOpaqueBackground(card);
                        }

                        for (int index = 0; index < featured.Children.Count; index++)
                        {
                            Controls.Card card = Assert.IsType<Controls.Card>(featured.Children[index]);
                            Assert.Equal(routes[index], card.Tag as string, StringComparer.Ordinal);
                            Assert.Equal(CardVariant.Outlined, card.Variant);
                            Assert.InRange(card.TranslatePoint(default, source).Y - firstRowTop, -1, 1);
                            GeometryDrawing silhouette = Assert.IsType<GeometryDrawing>(cardSilhouettes.Children[index]);
                            RectangleGeometry roundedCard = Assert.IsType<RectangleGeometry>(silhouette.Geometry);
                            Assert.True(roundedCard.RadiusX > 0 && roundedCard.RadiusY > 0);
                            Point origin = card.TranslatePoint(default, acrylic);
                            Assert.InRange(roundedCard.Rect.X - origin.X, -1, 1);
                            Assert.InRange(roundedCard.Rect.Y - origin.Y, -1, 1);
                            Assert.InRange(roundedCard.Rect.Width - card.ActualWidth, -1, 1);
                            Assert.InRange(roundedCard.Rect.Height - card.ActualHeight, -1, 1);
                        }
                        Assert.True(scroll.ExtentWidth <= scroll.ViewportWidth + 1,
                            "The home page must fit the viewport without horizontal clipping.");
                        Assert.Equal(TextWrapping.Wrap, description.TextWrapping);
                        Assert.Equal(TextWrapping.Wrap, compatibility.TextWrapping);
                        double cardLeft = firstCard.TranslatePoint(default, source).X;
                        Point markOrigin = lockupMark.TranslatePoint(default, source);
                        Point headingOrigin = heading.TranslatePoint(default, source);
                        Point titleOrigin = title.TranslatePoint(default, source);
                        Point subtitleOrigin = description.TranslatePoint(default, source);
                        Point runtimeOrigin = compatibility.TranslatePoint(default, source);

                        Assert.True(lockupMark.ActualWidth > 0);
                        Assert.InRange(markOrigin.X - cardLeft, -1, 1);
                        Assert.InRange(markOrigin.Y - headingOrigin.Y, -1, 1);
                        Assert.InRange(lockupMark.ActualHeight - heading.ActualHeight, -1, 1);
                        Assert.InRange(heading.ActualHeight - title.ActualHeight - description.ActualHeight, -1, 1);
                        Assert.True(titleOrigin.X >= markOrigin.X + lockupMark.ActualWidth - 1);
                        Assert.InRange(subtitleOrigin.X - titleOrigin.X, -1, 1);
                        Assert.InRange(runtimeOrigin.X - markOrigin.X, -1, 1);
                        Assert.True(runtimeOrigin.Y >= markOrigin.Y + lockupMark.ActualHeight - 1);

                        foreach (TextBlock text in new[] { title, description, compatibility })
                        {
                            Point origin = text.TranslatePoint(default, source);
                            Assert.InRange(origin.X, 0, source.ActualWidth);
                            Assert.True(origin.X + text.ActualWidth <= source.ActualWidth + 1);
                        }

                        if (columns is 1)
                        {
                            Assert.True(compatibility.ActualHeight > compatibility.LineHeight,
                                "The compatibility text must wrap at the narrow width.");
                        }
                    }
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        private static void AssertOpaqueBackground(Controls.Card card)
        {
            Border border = Assert.IsType<Border>(card.Template.FindName("OuterBorder", card), exactMatch: false);
            SolidColorBrush brush = Assert.IsType<SolidColorBrush>(border.Background, exactMatch: false);
            Assert.Equal(byte.MaxValue, brush.Color.A);
            Assert.Equal(1d, brush.Opacity);
        }

        private static T Find<T>(DependencyObject root, string name)
            where T : FrameworkElement
        {
            return Assert.IsType<T>(DemoTestHost.FindByName<T>(root, name), exactMatch: false);
        }

        private static void Invoke(UIElement element)
        {
            AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(element);
            IInvokeProvider provider = Assert.IsType<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke), exactMatch: false);
            provider.Invoke();
            WpfTestSta.DrainDispatcher(element.Dispatcher);
        }

        private static void Settle(Window window)
        {
            WpfTestSta.DrainDispatcher(window.Dispatcher);
            window.UpdateLayout();
            WpfTestSta.DrainDispatcher(window.Dispatcher);
        }
    }
}
