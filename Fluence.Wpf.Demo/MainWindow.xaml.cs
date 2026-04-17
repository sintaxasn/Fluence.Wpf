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
using Fluence.Wpf;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo
{
    public partial class MainWindow : FluenceWindow
    {
        internal const string GalleryWindowTitle = "Fluence.Wpf \u2014 Control Gallery";

        public MainWindow()
        {
            InitializeComponent();
            Title = GalleryWindowTitle;
            SystemThemeWatcher.Watch(this);
            ApplicationThemeManager.Apply(ApplicationTheme.Auto, BackdropType.Mica, true);
        }

        /// <summary>
        /// Selects the pane item whose <see cref="FrameworkElement.Tag"/> contains the given token.
        /// The first matching item is used; token matching is case-insensitive and whole-word within the Tag string.
        /// </summary>
        /// <param name="tag">A single token (e.g. "window", "buttons", "colors") that appears in a NavigationViewItem.Tag.</param>
        public void NavigateTo(string tag)
        {
            if (DemoNav == null || string.IsNullOrEmpty(tag))
            {
                return;
            }

            var needle = tag.Trim().ToLowerInvariant();
            foreach (var obj in DemoNav.Items)
            {
                var nvi = obj as NavigationViewItem;
                if (nvi == null)
                {
                    continue;
                }

                var label = (nvi.Content as string) ?? string.Empty;
                var tagText = (nvi.Tag as string) ?? string.Empty;
                var haystack = (label + " " + tagText).ToLowerInvariant();
                if (haystack.IndexOf(needle, StringComparison.Ordinal) >= 0)
                {
                    DemoNav.SelectedItem = nvi;
                    return;
                }
            }
        }

        private void NavSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyNavSearchFilter();
        }

        private void ApplyNavSearchFilter()
        {
            if (DemoNav == null || NavSearchBox == null)
            {
                return;
            }

            var q = (NavSearchBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(q))
            {
                foreach (var obj in DemoNav.Items)
                {
                    var item = obj as NavigationViewItem;
                    if (item != null)
                    {
                        item.Visibility = Visibility.Visible;
                    }
                }

                return;
            }

            var ql = q.ToLowerInvariant();
            var any = false;
            foreach (var obj in DemoNav.Items)
            {
                var nvi = obj as NavigationViewItem;
                if (nvi == null)
                {
                    continue;
                }

                var label = (nvi.Content as string) ?? string.Empty;
                var tag = (nvi.Tag as string) ?? string.Empty;
                var haystack = (label + " " + tag).ToLowerInvariant();
                var match = haystack.IndexOf(ql, StringComparison.Ordinal) >= 0;
                if (match)
                {
                    any = true;
                }

                nvi.Visibility = match ? Visibility.Visible : Visibility.Collapsed;
            }

            if (!any)
            {
                foreach (var obj in DemoNav.Items)
                {
                    var item = obj as NavigationViewItem;
                    if (item != null)
                    {
                        item.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            SystemThemeWatcher.UnWatch(this);
            base.OnClosed(e);
        }
    }
}
