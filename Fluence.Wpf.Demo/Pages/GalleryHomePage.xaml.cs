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
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryHomePage : Page
    {
        // The rounded cards in the brand vector meet at this normalized coordinate.
        private static readonly Point BrandFanCenter = new(0.50, 0.713);
        private (Size CatalogSize, int Columns, CornerRadius CornerRadius) _maskLayout;
        private Size _artworkSize;

        public GalleryHomePage()
        {
            InitializeComponent();
            Loaded += GalleryHomePage_Loaded;
            Unloaded += GalleryHomePage_Unloaded;
        }

        private void GalleryHomePage_Loaded(object sender, RoutedEventArgs e)
        {
            ApplicationThemeManager.Changed += ApplicationThemeManager_Changed;
            UpdateArtworkVisibility();
        }

        private void GalleryHomePage_Unloaded(object sender, RoutedEventArgs e)
        {
            ApplicationThemeManager.Changed -= ApplicationThemeManager_Changed;
        }

        private void ApplicationThemeManager_Changed(object? sender, ThemeChangedEventArgs e)
        {
            UpdateArtworkVisibility();
        }

        private void UpdateArtworkVisibility()
        {
            HeroArtwork.Visibility = ApplicationThemeManager.ResolvedTheme is ApplicationTheme.HighContrast
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void PageContent_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            int columns = e.NewSize.Width >= 720 ? 3 : e.NewSize.Width >= 480 ? 2 : 1;
            FeaturedControlsGrid.Columns = columns;
            ControlsCatalogGrid.Columns = columns;
            FoundationLinksGrid.Columns = columns is 3 ? 3 : 1;
            UpdateFeaturedCards(columns);
        }

        private void UpdateFeaturedCards(int columns)
        {
            int current = FeaturedControlsGrid.Children.Count;
            if (current == columns)
            {
                return;
            }

            bool promote = current < columns;
            UniformGrid source = promote ? ControlsCatalogGrid : FeaturedControlsGrid;
            UniformGrid destination = promote ? FeaturedControlsGrid : ControlsCatalogGrid;
            int moves = Math.Abs(current - columns);

            // Keep the same card instances and reading order when a breakpoint moves the row.
            for (int index = 0; index < moves; index++)
            {
                int sourceIndex = promote ? 0 : source.Children.Count - 1;
                Card card = (Card)source.Children[sourceIndex];
                bool restoreFocus = card.IsKeyboardFocused;
                source.Children.RemoveAt(sourceIndex);
                card.Variant = promote ? CardVariant.Outlined : CardVariant.Default;
                card.SetResourceReference(StyleProperty, promote ? "HomeFeaturedCardStyle" : "HomeCatalogCardStyle");
                destination.Children.Insert(promote ? destination.Children.Count : 0, card);
                if (restoreFocus)
                {
                    _ = card.Focus();
                }
            }
        }

        private void UpdateArtwork(double rowTop, double rowHeight)
        {
            // Fade behind the bottom of the featured cards, leaving the catalog entirely clear.
            double artworkHeight = rowTop + (rowHeight * 0.82);
            HeroBackdropSource.Height = rowTop + rowHeight;

            Size artworkSize = new(HeroBackdropCanvas.ActualWidth, artworkHeight);
            if (_artworkSize == artworkSize)
            {
                return;
            }

            _artworkSize = artworkSize;
            HeroArtwork.Height = artworkHeight;
            HeroVerticalFade.EndPoint = new Point(0, artworkHeight);

            double imageSize = Math.Max(artworkSize.Width * 2.35, artworkHeight * 4.5);
            BrandHeroImage.Width = imageSize;
            BrandHeroImage.Height = imageSize;
            BrandHeroImage.RenderTransformOrigin = BrandFanCenter;
            Canvas.SetLeft(BrandHeroImage, (artworkSize.Width * 0.56) - (imageSize * BrandFanCenter.X));
            Canvas.SetTop(BrandHeroImage, (artworkHeight * 0.90) - (imageSize * BrandFanCenter.Y));
        }

        private void FeaturedControlsGrid_LayoutUpdated(object? sender, EventArgs e)
        {
            if (!IsLoaded || !FeaturedControlsGrid.IsArrangeValid || ButtonsCard.ActualHeight <= 0)
            {
                return;
            }

            double rowHeight = ButtonsCard.ActualHeight;
            double rowTop = FeaturedControlsGrid.TranslatePoint(default, HeroBackdropCanvas).Y;
            UpdateArtwork(rowTop, rowHeight);

            (Size, int, CornerRadius) layout = (FeaturedControlsGrid.RenderSize, FeaturedControlsGrid.Columns, ButtonsCard.CornerRadius);
            if (_maskLayout == layout)
            {
                return;
            }

            _maskLayout = layout;
            FeaturedRowAcrylic.Height = rowHeight;

            // One live surface supplies the whole row. The mask leaves the gaps between cards clear.
            DrawingGroup mask = new();
            double radius = ButtonsCard.CornerRadius.TopLeft;
            for (int column = 0; column < FeaturedControlsGrid.Columns; column++)
            {
                Card card = (Card)FeaturedControlsGrid.Children[column];
                Rect bounds = new(card.TranslatePoint(default, FeaturedControlsGrid), card.RenderSize);
                mask.Children.Add(new GeometryDrawing(Brushes.White, pen: null, new RectangleGeometry(bounds, radius, radius)));
            }

            Rect row = new(0, 0, FeaturedControlsGrid.ActualWidth, rowHeight);
            DrawingBrush opacityMask = new(mask)
            {
                Viewbox = row,
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewport = row,
                ViewportUnits = BrushMappingMode.Absolute,
                Stretch = Stretch.Fill,
            };
            opacityMask.Freeze();
            FeaturedRowAcrylic.OpacityMask = opacityMask;
        }

        private void Card_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element
                || element.Tag is not string tag
                || string.IsNullOrWhiteSpace(tag))
            {
                return;
            }

            if (Window.GetWindow(this) is MainWindow host)
            {
                host.NavigateTo(tag);
            }
        }
    }
}
