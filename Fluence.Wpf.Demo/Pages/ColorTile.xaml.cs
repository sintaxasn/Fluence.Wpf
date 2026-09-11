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
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>
    /// Colors page brush tile, mirroring the WinUI 3 Gallery <c language="csharp">ColorTile</c> control:
    /// the tile is painted in the brush it documents and shows the tile name, a usage note, the
    /// brush key and a copy button.
    /// </summary>
    /// <remarks>
    /// The page sets <see cref="Control.Background"/> to the documented brush and
    /// <see cref="Control.Foreground"/> to the contrasting text brush with
    /// <see cref="FrameworkElement.SetResourceReference"/>, so both follow theme changes.
    /// <see cref="TileCornerRadius"/> lets the first and last tiles in a row round their outer
    /// corners to sit flush inside the rounded tile-grid surface, which WPF's <c language="csharp">Border</c>
    /// does not clip for them.
    /// </remarks>
    public partial class ColorTile : UserControl
    {
        /// <summary>
        /// Identifies the <see cref="ColorName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorNameProperty =
            DependencyProperty.Register(
                nameof(ColorName),
                typeof(string),
                typeof(ColorTile),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="ColorExplanation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorExplanationProperty =
            DependencyProperty.Register(
                nameof(ColorExplanation),
                typeof(string),
                typeof(ColorTile),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="ColorBrushName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorBrushNameProperty =
            DependencyProperty.Register(
                nameof(ColorBrushName),
                typeof(string),
                typeof(ColorTile),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="ShowSeparator"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowSeparatorProperty =
            DependencyProperty.Register(
                nameof(ShowSeparator),
                typeof(bool),
                typeof(ColorTile),
                new FrameworkPropertyMetadata(defaultValue: true));

        /// <summary>
        /// Identifies the <see cref="TileCornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TileCornerRadiusProperty =
            DependencyProperty.Register(
                nameof(TileCornerRadius),
                typeof(CornerRadius),
                typeof(ColorTile),
                new FrameworkPropertyMetadata(defaultValue: default(CornerRadius)));

        /// <summary>
        /// Identifies the <see cref="AutoContrastForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoContrastForegroundProperty =
            DependencyProperty.Register(
                nameof(AutoContrastForeground),
                typeof(bool),
                typeof(ColorTile),
                new FrameworkPropertyMetadata(defaultValue: false, propertyChangedCallback: OnAutoContrastForegroundChanged));

        private const string PrimaryTextKey = "TextFillColorPrimaryBrush";
        private const string OnAccentTextKey = "TextOnAccentFillColorPrimaryBrush";
        private const string BaseSurfaceKey = "SolidBackgroundFillColorBaseBrush";

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorTile"/> class.
        /// </summary>
        public ColorTile()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets or sets a value indicating whether the tile picks its own text brush by contrast.
        /// </summary>
        /// <remarks>
        /// The WinUI Gallery paints a few tiles whose brush is light in both themes (the on-accent
        /// disabled and selected-text tiles) with a literal black foreground. Fluence has no brush
        /// that is dark in every theme, so when this is set the tile picks whichever of the primary
        /// and on-accent text brushes contrasts more with its <see cref="Control.Background"/>, and
        /// re-evaluates whenever a theme change republishes that background.
        /// </remarks>
        public bool AutoContrastForeground
        {
            get => (bool)GetValue(AutoContrastForegroundProperty);
            set => SetValue(AutoContrastForegroundProperty, value);
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == BackgroundProperty && AutoContrastForeground)
            {
                ApplyAutoContrastForeground();
            }
        }

        private static void OnAutoContrastForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is true)
            {
                ((ColorTile)d).ApplyAutoContrastForeground();
            }
        }

        private void ApplyAutoContrastForeground()
        {
            if (Background is not SolidColorBrush tileBrush)
            {
                return;
            }

            Color surface = TryFindResource(BaseSurfaceKey) is SolidColorBrush surfaceBrush ? surfaceBrush.Color : Colors.White;
            double tileLuminance = Luminance(Composite(tileBrush.Color, surface));
            double primaryLuminance = TryFindResource(PrimaryTextKey) is SolidColorBrush primary ? Luminance(Composite(primary.Color, surface)) : 0;
            double onAccentLuminance = TryFindResource(OnAccentTextKey) is SolidColorBrush onAccent ? Luminance(Composite(onAccent.Color, surface)) : 1;
            string key = Math.Abs(primaryLuminance - tileLuminance) >= Math.Abs(onAccentLuminance - tileLuminance) ? PrimaryTextKey : OnAccentTextKey;
            SetResourceReference(ForegroundProperty, key);
        }

        private static Color Composite(Color color, Color surface)
        {
            double alpha = color.A / 255d;
            return Color.FromRgb(
                (byte)Math.Round((color.R * alpha) + (surface.R * (1 - alpha)), MidpointRounding.AwayFromZero),
                (byte)Math.Round((color.G * alpha) + (surface.G * (1 - alpha)), MidpointRounding.AwayFromZero),
                (byte)Math.Round((color.B * alpha) + (surface.B * (1 - alpha)), MidpointRounding.AwayFromZero));
        }

        private static double Luminance(Color color)
        {
            return ((0.2126 * color.R) + (0.7152 * color.G) + (0.0722 * color.B)) / 255d;
        }

        /// <summary>
        /// Gets or sets the tile name, for example <c language="text">Text / Primary</c>.
        /// </summary>
        public string ColorName
        {
            get => (string)GetValue(ColorNameProperty);
            set => SetValue(ColorNameProperty, value);
        }

        /// <summary>
        /// Gets or sets the usage note, for example <c language="text">Rest or Hover</c>.
        /// </summary>
        public string ColorExplanation
        {
            get => (string)GetValue(ColorExplanationProperty);
            set => SetValue(ColorExplanationProperty, value);
        }

        /// <summary>
        /// Gets or sets the Fluence brush key the tile documents and copies.
        /// </summary>
        public string ColorBrushName
        {
            get => (string)GetValue(ColorBrushNameProperty);
            set => SetValue(ColorBrushNameProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the 1px right-hand separator is shown.
        /// </summary>
        public bool ShowSeparator
        {
            get => (bool)GetValue(ShowSeparatorProperty);
            set => SetValue(ShowSeparatorProperty, value);
        }

        /// <summary>
        /// Gets or sets the corner radius of the painted tile surface.
        /// </summary>
        public CornerRadius TileCornerRadius
        {
            get => (CornerRadius)GetValue(TileCornerRadiusProperty);
            set => SetValue(TileCornerRadiusProperty, value);
        }

        private void CopyBrushNameButton_Click(object sender, RoutedEventArgs e)
        {
            string brushName = ColorBrushName;
            if (string.IsNullOrWhiteSpace(brushName))
            {
                return;
            }

            try
            {
                DemoClipboard.SetText(brushName);
            }
            catch (ExternalException)
            {
                System.Diagnostics.Debug.WriteLine("Clipboard was unavailable while copying a brush name.");
            }
            catch (ThreadStateException)
            {
                System.Diagnostics.Debug.WriteLine("Clipboard access requires an STA thread.");
            }
        }
    }
}
