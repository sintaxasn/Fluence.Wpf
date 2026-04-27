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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Provides attached properties for extending TextBlock with Fluent Design features.
    /// </summary>
    public static class TextBlockExtensions
    {
        private static readonly FontFamily FluentTypographyFontFamily =
            new FontFamily("Segoe UI Variable, Segoe UI");

        #region Typography

        /// <summary>
        /// Identifies the Typography attached property.
        /// </summary>
        public static readonly DependencyProperty TypographyProperty =
            DependencyProperty.RegisterAttached(
                "Typography",
                typeof(FluentTypography),
                typeof(TextBlockExtensions),
                new FrameworkPropertyMetadata(FluentTypography.None, OnTypographyChanged));

        /// <summary>
        /// Gets the typography style for the specified TextBlock.
        /// </summary>
        public static FluentTypography GetTypography(DependencyObject obj)
        {
            return (FluentTypography)obj.GetValue(TypographyProperty);
        }

        /// <summary>
        /// Sets the typography style for the specified TextBlock.
        /// </summary>
        public static void SetTypography(DependencyObject obj, FluentTypography value)
        {
            obj.SetValue(TypographyProperty, value);
        }

        private static void OnTypographyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBlock = d as System.Windows.Controls.TextBlock;
            if (textBlock == null)
            {
                return;
            }

            var typography = (FluentTypography)e.NewValue;
            ApplyTypography(textBlock, typography);
        }

        private static void ApplyTypography(System.Windows.Controls.TextBlock textBlock, FluentTypography typography)
        {
            if (typography == FluentTypography.None)
            {
                return;
            }

            textBlock.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
            textBlock.FontFamily = FluentTypographyFontFamily;

            var formattingMode = TextFormattingMode.Display;

            switch (typography)
            {
                case FluentTypography.Caption:
                    textBlock.FontSize = 12;
                    textBlock.FontWeight = FontWeights.Normal;
                    textBlock.LineHeight = 16;
                    break;
                case FluentTypography.Body:
                    textBlock.FontSize = 14;
                    textBlock.FontWeight = FontWeights.Normal;
                    textBlock.LineHeight = 20;
                    break;
                case FluentTypography.BodyStrong:
                    textBlock.FontSize = 14;
                    textBlock.FontWeight = FontWeights.SemiBold;
                    textBlock.LineHeight = 20;
                    break;
                case FluentTypography.BodyLarge:
                    textBlock.FontSize = 18;
                    textBlock.FontWeight = FontWeights.Normal;
                    textBlock.LineHeight = 24;
                    break;
                case FluentTypography.Subtitle:
                    textBlock.FontSize = 20;
                    textBlock.FontWeight = FontWeights.SemiBold;
                    textBlock.LineHeight = 28;
                    formattingMode = TextFormattingMode.Ideal;
                    break;
                case FluentTypography.Title:
                    textBlock.FontSize = 28;
                    textBlock.FontWeight = FontWeights.SemiBold;
                    textBlock.LineHeight = 36;
                    formattingMode = TextFormattingMode.Ideal;
                    break;
                case FluentTypography.TitleLarge:
                    textBlock.FontSize = 40;
                    textBlock.FontWeight = FontWeights.Normal;
                    textBlock.LineHeight = 52;
                    formattingMode = TextFormattingMode.Ideal;
                    break;
                case FluentTypography.Display:
                    textBlock.FontSize = 68;
                    textBlock.FontWeight = FontWeights.SemiBold;
                    textBlock.LineHeight = 92;
                    formattingMode = TextFormattingMode.Ideal;
                    break;
                case FluentTypography.None:
                default:
                    break;
            }

            ApplyTextRenderingPolicy(textBlock, formattingMode);
        }

        private static void ApplyTextRenderingPolicy(System.Windows.Controls.TextBlock textBlock, TextFormattingMode formattingMode)
        {
            TextOptions.SetTextFormattingMode(textBlock, formattingMode);
            TextOptions.SetTextRenderingMode(textBlock, TextRenderingMode.ClearType);
            TextOptions.SetTextHintingMode(textBlock, TextHintingMode.Fixed);
        }

        #endregion

        #region TextTrimming

        /// <summary>
        /// Identifies the TextTrimming attached property.
        /// </summary>
        public static readonly DependencyProperty TextTrimmingProperty =
            DependencyProperty.RegisterAttached(
                "TextTrimming",
                typeof(TextTrimming),
                typeof(TextBlockExtensions),
                new FrameworkPropertyMetadata(TextTrimming.None, OnTextTrimmingChanged));

        /// <summary>
        /// Gets the value of the <see cref="TextTrimmingProperty"/> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The target <see cref="System.Windows.Controls.TextBlock"/>.</param>
        /// <returns>The requested text trimming mode.</returns>
        public static TextTrimming GetTextTrimming(DependencyObject obj)
        {
            return (TextTrimming)obj.GetValue(TextTrimmingProperty);
        }

        /// <summary>
        /// Sets the value of the <see cref="TextTrimmingProperty"/> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The target <see cref="System.Windows.Controls.TextBlock"/>.</param>
        /// <param name="value">The text trimming mode to apply.</param>
        public static void SetTextTrimming(DependencyObject obj, TextTrimming value)
        {
            obj.SetValue(TextTrimmingProperty, value);
        }

        private static void OnTextTrimmingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBlock = d as System.Windows.Controls.TextBlock;
            if (textBlock == null)
            {
                return;
            }

            textBlock.TextTrimming = (TextTrimming)e.NewValue;
        }

        #endregion

        #region IsTextSelectionEnabled

        /// <summary>
        /// Identifies the IsTextSelectionEnabled attached property.
        /// </summary>
        public static readonly DependencyProperty IsTextSelectionEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsTextSelectionEnabled",
                typeof(bool),
                typeof(TextBlockExtensions),
                new FrameworkPropertyMetadata(false, OnIsTextSelectionEnabledChanged));

        /// <summary>
        /// Gets the value of the <see cref="IsTextSelectionEnabledProperty"/> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The target <see cref="System.Windows.Controls.TextBlock"/>.</param>
        /// <returns><c>true</c> if selection is enabled; otherwise <c>false</c>.</returns>
        public static bool GetIsTextSelectionEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsTextSelectionEnabledProperty);
        }

        /// <summary>
        /// Sets the value of the <see cref="IsTextSelectionEnabledProperty"/> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The target <see cref="System.Windows.Controls.TextBlock"/>.</param>
        /// <param name="value"><c>true</c> to enable text selection; otherwise <c>false</c>.</param>
        public static void SetIsTextSelectionEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsTextSelectionEnabledProperty, value);
        }

        private static void OnIsTextSelectionEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBlock = d as System.Windows.Controls.TextBlock;
            if (textBlock == null)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                if (textBlock.IsLoaded)
                {
                    ApplySelectionOverlay(textBlock);
                }
                else
                {
                    textBlock.Loaded += OnTextBlockLoadedForSelection;
                }
            }
        }

        private static void OnTextBlockLoadedForSelection(object sender, RoutedEventArgs e)
        {
            var textBlock = (System.Windows.Controls.TextBlock)sender;
            textBlock.Loaded -= OnTextBlockLoadedForSelection;
            ApplySelectionOverlay(textBlock);
        }

        private static void ApplySelectionOverlay(System.Windows.Controls.TextBlock textBlock)
        {
            if (!GetIsTextSelectionEnabled(textBlock))
            {
                return;
            }

            var parent = VisualTreeHelper.GetParent(textBlock) as Panel;
            if (parent == null)
            {
                return;
            }

            var index = parent.Children.IndexOf(textBlock);
            if (index < 0)
            {
                return;
            }

            parent.Children.RemoveAt(index);

            var grid = new Grid();
            textBlock.Opacity = 0;
            textBlock.IsHitTestVisible = false;
            grid.Children.Add(textBlock);

            var overlay = new System.Windows.Controls.TextBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = textBlock.Padding,
                Foreground = textBlock.Foreground,
                FontFamily = textBlock.FontFamily,
                FontSize = textBlock.FontSize,
                FontWeight = textBlock.FontWeight,
                FontStyle = textBlock.FontStyle,
                FontStretch = textBlock.FontStretch,
                TextWrapping = textBlock.TextWrapping,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                IsReadOnly = true,
                CaretBrush = textBlock.Foreground,
                SelectionBrush = SystemColors.HighlightBrush
            };

            TextOptions.SetTextFormattingMode(overlay, TextOptions.GetTextFormattingMode(textBlock));
            TextOptions.SetTextRenderingMode(overlay, TextOptions.GetTextRenderingMode(textBlock));
            TextOptions.SetTextHintingMode(overlay, TextOptions.GetTextHintingMode(textBlock));

            overlay.SetBinding(System.Windows.Controls.TextBox.TextProperty, new Binding
            {
                Path = new PropertyPath(System.Windows.Controls.TextBlock.TextProperty),
                Source = textBlock,
                Mode = BindingMode.OneWay
            });

            grid.Children.Add(overlay);
            parent.Children.Insert(index, grid);
        }

        #endregion

        #region PlaceholderText

        /// <summary>
        /// Identifies the PlaceholderText attached property.
        /// </summary>
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.RegisterAttached(
                "PlaceholderText",
                typeof(string),
                typeof(TextBlockExtensions),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Gets the placeholder text for the specified element.
        /// </summary>
        public static string GetPlaceholderText(DependencyObject obj)
        {
            return (string)obj.GetValue(PlaceholderTextProperty);
        }

        /// <summary>
        /// Sets the placeholder text for the specified element.
        /// </summary>
        public static void SetPlaceholderText(DependencyObject obj, string value)
        {
            obj.SetValue(PlaceholderTextProperty, value);
        }

        #endregion

        #region ShowPlaceholder

        /// <summary>
        /// Identifies the ShowPlaceholder attached property.
        /// </summary>
        public static readonly DependencyProperty ShowPlaceholderProperty =
            DependencyProperty.RegisterAttached(
                "ShowPlaceholder",
                typeof(bool),
                typeof(TextBlockExtensions),
                new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets whether the placeholder should be shown.
        /// </summary>
        public static bool GetShowPlaceholder(DependencyObject obj)
        {
            return (bool)obj.GetValue(ShowPlaceholderProperty);
        }

        /// <summary>
        /// Sets whether the placeholder should be shown.
        /// </summary>
        public static void SetShowPlaceholder(DependencyObject obj, bool value)
        {
            obj.SetValue(ShowPlaceholderProperty, value);
        }

        #endregion

        #region Icon

        /// <summary>
        /// Identifies the Icon attached property.
        /// </summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.RegisterAttached(
                "Icon",
                typeof(object),
                typeof(TextBlockExtensions),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets the icon for the specified element.
        /// </summary>
        public static object GetIcon(DependencyObject obj)
        {
            return obj.GetValue(IconProperty);
        }

        /// <summary>
        /// Sets the icon for the specified element.
        /// </summary>
        public static void SetIcon(DependencyObject obj, object value)
        {
            obj.SetValue(IconProperty, value);
        }

        #endregion

        #region IconPlacement

        /// <summary>
        /// Identifies the IconPlacement attached property.
        /// </summary>
        public static readonly DependencyProperty IconPlacementProperty =
            DependencyProperty.RegisterAttached(
                "IconPlacement",
                typeof(ElementPlacement),
                typeof(TextBlockExtensions),
                new FrameworkPropertyMetadata(ElementPlacement.Left));

        /// <summary>
        /// Gets the icon placement for the specified element.
        /// </summary>
        public static ElementPlacement GetIconPlacement(DependencyObject obj)
        {
            return (ElementPlacement)obj.GetValue(IconPlacementProperty);
        }

        /// <summary>
        /// Sets the icon placement for the specified element.
        /// </summary>
        public static void SetIconPlacement(DependencyObject obj, ElementPlacement value)
        {
            obj.SetValue(IconPlacementProperty, value);
        }

        #endregion
    }
}
