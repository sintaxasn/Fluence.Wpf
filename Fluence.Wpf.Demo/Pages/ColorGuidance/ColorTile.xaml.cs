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
using System.Windows.Documents;

namespace Fluence.Wpf.Demo.Pages.ColorGuidance
{
    public partial class ColorTile : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(ColorTile),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(
                nameof(Description),
                typeof(string),
                typeof(ColorTile),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty BrushResourceKeyProperty =
            DependencyProperty.Register(
                nameof(BrushResourceKey),
                typeof(string),
                typeof(ColorTile),
                new PropertyMetadata("GalleryBackgroundBrush", OnResourcePropertyChanged));

        public static readonly DependencyProperty ForegroundResourceKeyProperty =
            DependencyProperty.Register(
                nameof(ForegroundResourceKey),
                typeof(string),
                typeof(ColorTile),
                new PropertyMetadata("TextFillColorPrimaryBrush", OnResourcePropertyChanged));

        public static readonly DependencyProperty ResourceKeyProperty =
            DependencyProperty.Register(
                nameof(ResourceKey),
                typeof(string),
                typeof(ColorTile),
                new PropertyMetadata(string.Empty, OnResourcePropertyChanged));

        public static readonly DependencyProperty EffectiveResourceKeyProperty =
            DependencyProperty.Register(
                nameof(EffectiveResourceKey),
                typeof(string),
                typeof(ColorTile),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ShowSeparatorProperty =
            DependencyProperty.Register(
                nameof(ShowSeparator),
                typeof(bool),
                typeof(ColorTile),
                new PropertyMetadata(false, OnDisplayPropertyChanged));

        public static readonly DependencyProperty ShowWarningProperty =
            DependencyProperty.Register(
                nameof(ShowWarning),
                typeof(bool),
                typeof(ColorTile),
                new PropertyMetadata(false, OnDisplayPropertyChanged));

        public ColorTile()
        {
            InitializeComponent();
            ApplyResourceReferences();
            UpdateDisplayState();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public string BrushResourceKey
        {
            get => (string)GetValue(BrushResourceKeyProperty);
            set => SetValue(BrushResourceKeyProperty, value);
        }

        public string ForegroundResourceKey
        {
            get => (string)GetValue(ForegroundResourceKeyProperty);
            set => SetValue(ForegroundResourceKeyProperty, value);
        }

        public string ResourceKey
        {
            get => (string)GetValue(ResourceKeyProperty);
            set => SetValue(ResourceKeyProperty, value);
        }

        public string EffectiveResourceKey
        {
            get => (string)GetValue(EffectiveResourceKeyProperty);
            private set => SetValue(EffectiveResourceKeyProperty, value);
        }

        public bool ShowSeparator
        {
            get => (bool)GetValue(ShowSeparatorProperty);
            set => SetValue(ShowSeparatorProperty, value);
        }

        public bool ShowWarning
        {
            get => (bool)GetValue(ShowWarningProperty);
            set => SetValue(ShowWarningProperty, value);
        }

        private static void OnResourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            _ = e;
            ((ColorTile)d).ApplyResourceReferences();
        }

        private static void OnDisplayPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            _ = e;
            ((ColorTile)d).UpdateDisplayState();
        }

        private void ApplyResourceReferences()
        {
            string backgroundKey = string.IsNullOrWhiteSpace(BrushResourceKey) ? "GalleryBackgroundBrush" : BrushResourceKey;
            string foregroundKey = string.IsNullOrWhiteSpace(ForegroundResourceKey) ? "TextFillColorPrimaryBrush" : ForegroundResourceKey;
            EffectiveResourceKey = string.IsNullOrWhiteSpace(ResourceKey) ? backgroundKey : ResourceKey;

            TileBorder.SetResourceReference(Border.BackgroundProperty, backgroundKey);
            TitleText.SetResourceReference(TextElement.ForegroundProperty, foregroundKey);
            DescriptionText.SetResourceReference(TextElement.ForegroundProperty, foregroundKey);
            ResourceText.SetResourceReference(TextElement.ForegroundProperty, foregroundKey);
            WarningIcon.SetResourceReference(ForegroundProperty, foregroundKey);
        }

        private void UpdateDisplayState()
        {
            RightSeparator.Visibility = ShowSeparator ? Visibility.Visible : Visibility.Collapsed;
            WarningIcon.Visibility = ShowWarning ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;
            _ = e;
            Clipboard.SetText(EffectiveResourceKey);
        }
    }
}
