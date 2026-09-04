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
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>
    /// Shared gallery page header modelled on the WinUI 3 Gallery's Controls/PageHeader.xaml:
    /// a page title on the left and a row of Subtle-appearance action buttons on the right
    /// (Documentation, Toggle theme, Favorite).
    /// </summary>
    public partial class GalleryPageHeader : UserControl
    {
        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                "Title",
                typeof(string),
                typeof(GalleryPageHeader),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="DocsAnchor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DocsAnchorProperty =
            DependencyProperty.Register(
                "DocsAnchor",
                typeof(string),
                typeof(GalleryPageHeader),
                new FrameworkPropertyMetadata(string.Empty, OnDocsAnchorChanged));

        private static readonly Uri ControlsDocBaseUri = new UriBuilder("https", "github.com", -1, "sintaxasn/Fluence.Wpf/blob/main/docs/controls.md").Uri;

        /// <summary>
        /// Initializes a new instance of the <see cref="GalleryPageHeader"/> class.
        /// </summary>
        public GalleryPageHeader()
        {
            InitializeComponent();
            UpdateDocsButtonVisibility();
            UpdateFavoriteState(isFavorite: false);
            Loaded += GalleryPageHeader_Loaded;
            Unloaded += GalleryPageHeader_Unloaded;
        }

        /// <summary>
        /// Gets or sets the page title shown at the left of the header.
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Gets or sets the docs/controls.md heading slug the Documentation button opens.
        /// The button is hidden when this value is empty or whitespace.
        /// </summary>
        public string DocsAnchor
        {
            get => (string)GetValue(DocsAnchorProperty);
            set => SetValue(DocsAnchorProperty, value);
        }

        private static void OnDocsAnchorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GalleryPageHeader header)
            {
                header.UpdateDocsButtonVisibility();
            }
        }

        private void GalleryPageHeader_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateThemeToggleEnabled();
            ApplicationThemeManager.Changed -= ApplicationThemeManager_Changed;
            ApplicationThemeManager.Changed += ApplicationThemeManager_Changed;
        }

        private void GalleryPageHeader_Unloaded(object sender, RoutedEventArgs e)
        {
            ApplicationThemeManager.Changed -= ApplicationThemeManager_Changed;
        }

        private void ApplicationThemeManager_Changed(object? sender, ThemeChangedEventArgs e)
        {
            UpdateThemeToggleEnabled();
        }

        private void UpdateThemeToggleEnabled()
        {
            ThemeToggleButton.IsEnabled = ApplicationThemeManager.ResolvedTheme is not ApplicationTheme.HighContrast;
        }

        private void UpdateDocsButtonVisibility()
        {
            DocsButton.Visibility = string.IsNullOrWhiteSpace(DocsAnchor) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void DocsButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DocsAnchor))
            {
                return;
            }

            Uri target = new(ControlsDocBaseUri.AbsoluteUri + "#" + DocsAnchor);
            ProcessStartInfo startInfo = new(target.AbsoluteUri)
            {
                UseShellExecute = true,
            };

            try
            {
                _ = Process.Start(startInfo);
            }
            catch (Win32Exception)
            {
                // Opening the default browser is best-effort: Process.Start throws Win32Exception
                // when no shell handler is registered for the URI, and that must never surface as
                // an unhandled exception from a click handler.
            }
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (ApplicationThemeManager.ResolvedTheme is ApplicationTheme.HighContrast)
            {
                return;
            }

            MainWindow? owner = Application.Current?.MainWindow as MainWindow;
            ApplicationTheme next = ApplicationThemeManager.ResolvedTheme is ApplicationTheme.Dark
                ? ApplicationTheme.Light
                : ApplicationTheme.Dark;
            ApplicationThemeManager.Apply(next, owner?.SystemBackdropType ?? BackdropType.Auto);
        }

        private void FavoriteToggleButton_CheckedChanged(object sender, RoutedEventArgs e)
        {
            UpdateFavoriteState(FavoriteToggleButton.IsChecked is true);
        }

        private void UpdateFavoriteState(bool isFavorite)
        {
            FavoriteIcon.Glyph = isFavorite ? "\uE735" : "\uE734";
            string text = isFavorite ? "Remove from favorites" : "Add to favorites";
            AutomationProperties.SetName(FavoriteToggleButton, text);
            FavoriteToggleButton.ToolTip = text;
        }
    }
}
