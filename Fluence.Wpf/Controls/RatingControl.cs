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
using System.Windows.Input;
using System.Windows.Media;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A Fluent Design star-based rating control.
    /// Renders up to <see cref="MaxRating"/> star glyphs using Segoe Fluent Icons
    /// (U+E734 StarEmpty / U+E735 StarFilled).
    /// Authority: WinUI 3 RatingControl_themeresources.xaml + RatingControl.xaml.
    /// Brush states: <c>AccentFillColorDefaultBrush</c> (filled),
    /// <c>TextFillColorSecondaryBrush</c> (unset), <c>TextFillColorDisabledBrush</c> (disabled).
    /// </summary>
    [TemplatePart(Name = PART_StarsPanel, Type = typeof(WpfStackPanel))]
    [TemplatePart(Name = PART_Caption, Type = typeof(WpfTextBlock))]
    public class RatingControl : Control
    {
        private const string PART_StarsPanel = "PART_StarsPanel";
        private const string PART_Caption = "PART_Caption";

        private WpfStackPanel _starsPanel;
        private WpfTextBlock _captionText;
        private int _hoverIndex = -1; // 1-based; -1 = no hover

        // -----------------------------------------------------------------------
        // Static constructor
        // -----------------------------------------------------------------------

        static RatingControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(RatingControl),
                new FrameworkPropertyMetadata(typeof(RatingControl)));
        }

        // -----------------------------------------------------------------------
        // Value DP
        // -----------------------------------------------------------------------

        /// <summary>Identifies the <see cref="Value"/> dependency property.</summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(RatingControl),
                new FrameworkPropertyMetadata(
                    0.0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnValueChanged,
                    CoerceValue));

        /// <summary>Gets or sets the current rating value (0 to <see cref="MaxRating"/>).</summary>
        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RatingControl)d).RefreshStars();
        }

        private static object CoerceValue(DependencyObject d, object baseValue)
        {
            var ctrl = (RatingControl)d;
            double v = (double)baseValue;
            if (v < 0.0) return 0.0;
            if (v > ctrl.MaxRating) return (double)ctrl.MaxRating;
            return v;
        }

        // -----------------------------------------------------------------------
        // MaxRating DP
        // -----------------------------------------------------------------------

        /// <summary>Identifies the <see cref="MaxRating"/> dependency property.</summary>
        public static readonly DependencyProperty MaxRatingProperty =
            DependencyProperty.Register(
                nameof(MaxRating),
                typeof(int),
                typeof(RatingControl),
                new FrameworkPropertyMetadata(5, OnMaxRatingChanged));

        /// <summary>Gets or sets the maximum number of stars displayed. Default is 5.</summary>
        public int MaxRating
        {
            get { return (int)GetValue(MaxRatingProperty); }
            set { SetValue(MaxRatingProperty, value); }
        }

        private static void OnMaxRatingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (RatingControl)d;
            // Re-coerce Value in case it now exceeds the new MaxRating.
            ctrl.CoerceValue(ValueProperty);
            ctrl.BuildAndRefreshStars();
        }

        // -----------------------------------------------------------------------
        // IsReadOnly DP
        // -----------------------------------------------------------------------

        /// <summary>Identifies the <see cref="IsReadOnly"/> dependency property.</summary>
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(RatingControl),
                new FrameworkPropertyMetadata(false, OnIsReadOnlyChanged));

        /// <summary>
        /// Gets or sets whether the user can change the rating.
        /// When <see langword="true"/>, the control is display-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RatingControl)d).RefreshStars();
        }

        // -----------------------------------------------------------------------
        // Caption DP
        // -----------------------------------------------------------------------

        /// <summary>Identifies the <see cref="Caption"/> dependency property.</summary>
        public static readonly DependencyProperty CaptionProperty =
            DependencyProperty.Register(
                nameof(Caption),
                typeof(string),
                typeof(RatingControl),
                new FrameworkPropertyMetadata(string.Empty, OnCaptionChanged));

        /// <summary>Gets or sets the optional caption text shown after the stars.</summary>
        public string Caption
        {
            get { return (string)GetValue(CaptionProperty); }
            set { SetValue(CaptionProperty, value); }
        }

        private static void OnCaptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((RatingControl)d).UpdateCaption();
        }

        // -----------------------------------------------------------------------
        // Template
        // -----------------------------------------------------------------------

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _starsPanel = GetTemplateChild(PART_StarsPanel) as WpfStackPanel;
            _captionText = GetTemplateChild(PART_Caption) as WpfTextBlock;
            IsEnabledChanged -= OnIsEnabledChanged;
            IsEnabledChanged += OnIsEnabledChanged;
            BuildAndRefreshStars();
            UpdateCaption();
        }

        // -----------------------------------------------------------------------
        // Internal helpers
        // -----------------------------------------------------------------------

        private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            RefreshStars();
        }

        private void BuildAndRefreshStars()
        {
            if (_starsPanel == null) return;
            _hoverIndex = -1;
            _starsPanel.Children.Clear();

            int count = Math.Max(1, MaxRating);
            for (int i = 1; i <= count; i++)
            {
                var star = new WpfTextBlock
                {
                    FontFamily = new FontFamily("Segoe Fluent Icons"),
                    FontSize = 20.0,
                    VerticalAlignment = VerticalAlignment.Center,
                    SnapsToDevicePixels = true,
                    Text = "\uE734"
                };
                TextOptions.SetTextFormattingMode(star, TextFormattingMode.Display);
                TextOptions.SetTextRenderingMode(star, TextRenderingMode.ClearType);
                TextOptions.SetTextHintingMode(star, TextHintingMode.Fixed);

                if (i < count)
                    star.Margin = new Thickness(0, 0, 4, 0);

                int capturedIndex = i;
                star.MouseEnter += (s, e) => OnStarMouseEnter(capturedIndex);
                star.MouseLeave += (s, e) => OnStarMouseLeave();
                star.MouseLeftButtonDown += (s, e) => OnStarClick(capturedIndex);

                _starsPanel.Children.Add(star);
            }

            RefreshStars();
        }

        private void OnStarMouseEnter(int index)
        {
            if (IsReadOnly || !IsEnabled) return;
            _hoverIndex = index;
            RefreshStars();
        }

        private void OnStarMouseLeave()
        {
            _hoverIndex = -1;
            RefreshStars();
        }

        private void OnStarClick(int index)
        {
            if (IsReadOnly || !IsEnabled) return;
            // Clicking the already-set star clears the rating (WinUI 3 IsClearEnabled behaviour).
            double newValue = ((int)Math.Round(Value) == index) ? 0.0 : (double)index;
            SetCurrentValue(ValueProperty, newValue);
        }

        private void RefreshStars()
        {
            if (_starsPanel == null) return;
            int displayCount = _hoverIndex > 0 ? _hoverIndex : (int)Math.Round(Value);

            for (int i = 0; i < _starsPanel.Children.Count; i++)
            {
                var star = _starsPanel.Children[i] as WpfTextBlock;
                if (star == null) continue;

                bool filled = (i + 1) <= displayCount;
                star.Text = filled ? "\uE735" : "\uE734"; // StarFilled / StarEmpty

                if (!IsEnabled)
                    star.SetResourceReference(WpfTextBlock.ForegroundProperty, "TextFillColorDisabledBrush");
                else if (filled)
                    star.SetResourceReference(WpfTextBlock.ForegroundProperty, "AccentFillColorDefaultBrush");
                else
                    star.SetResourceReference(WpfTextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");

                star.Cursor = (IsReadOnly || !IsEnabled) ? null : Cursors.Hand;
            }
        }

        private void UpdateCaption()
        {
            if (_captionText == null) return;
            string text = Caption ?? string.Empty;
            _captionText.Text = text;
            _captionText.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
