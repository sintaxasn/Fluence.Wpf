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
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>
    /// A shared gallery page header: title on the left, and docs / theme-toggle / favorite
    /// actions on the right, mirroring the WinUI Gallery item page header.
    /// </summary>
    public partial class GalleryPageHeader : UserControl
    {
        private static readonly Uri DocsBaseUri = new UriBuilder("https", "github.com", -1, "sintaxasn/Fluence.Wpf/blob/main/docs/controls.md").Uri;

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(GalleryPageHeader), new PropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="DocsAnchor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DocsAnchorProperty = DependencyProperty.Register(
            nameof(DocsAnchor), typeof(string), typeof(GalleryPageHeader), new PropertyMetadata(string.Empty, OnDocsAnchorChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="GalleryPageHeader"/> class.
        /// </summary>
        public GalleryPageHeader()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the page title.
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Gets or sets the docs/controls.md anchor for the docs link button. An empty or
        /// whitespace anchor (including the default, unset value) collapses the docs button.
        /// </summary>
        public string DocsAnchor
        {
            get => (string)GetValue(DocsAnchorProperty);
            set => SetValue(DocsAnchorProperty, value);
        }

        private static void OnDocsAnchorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            GalleryPageHeader header = (GalleryPageHeader)d;
            header.DocsButton.Visibility = string.IsNullOrWhiteSpace(e.NewValue as string)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void DocsButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo startInfo = new(DocsBaseUri.AbsoluteUri + "#" + DocsAnchor) { UseShellExecute = true };
            _ = Process.Start(startInfo);
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationTheme next = ApplicationThemeManager.ResolvedTheme is ApplicationTheme.Dark
                ? ApplicationTheme.Light
                : ApplicationTheme.Dark;

            // Preserve the shell's current backdrop (GallerySettingsPage.AppThemeComboBox_SelectionChanged
            // precedent); a plain single-argument Apply would reset the backdrop to Auto.
            if (Application.Current?.MainWindow is MainWindow owner)
            {
                ApplicationThemeManager.Apply(next, owner.SystemBackdropType);
            }
            else
            {
                ApplicationThemeManager.Apply(next);
            }
        }

        private void FavoriteToggleButton_CheckedChanged(object sender, RoutedEventArgs e)
        {
            FavoriteIcon.Glyph = FavoriteToggleButton.IsChecked is true ? "\uE735" : "\uE734";
        }
    }
}
