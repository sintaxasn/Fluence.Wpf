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

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>Gallery page demonstrating menus, tooltips, and flyout controls.</summary>
    public partial class GalleryMenusPage : UserControl
    {
        /// <summary>Initializes a new instance of <see cref="GalleryMenusPage"/>.</summary>
        public GalleryMenusPage()
        {
            InitializeComponent();
        }

        // ── ContextMenu ──────────────────────────────────────────────────

        private void CutMenuItem_Click(object sender, RoutedEventArgs e)
            => SetContextResult("Cut");

        private void CopyMenuItem_Click(object sender, RoutedEventArgs e)
            => SetContextResult("Copy");

        private void PasteMenuItem_Click(object sender, RoutedEventArgs e)
            => SetContextResult("Paste");

        private void ShareCopyLink_Click(object sender, RoutedEventArgs e)
            => SetContextResult("Share → Copy link");

        private void ShareEmail_Click(object sender, RoutedEventArgs e)
            => SetContextResult("Share → Send via email");

        private void SetContextResult(string action)
        {
            if (ContextMenuResultLabel != null)
                ContextMenuResultLabel.Text = string.Format("Last action: {0}", action);
        }

        // ── DropDownButton / SplitButton ─────────────────────────────────

        private void Sort_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as FrameworkElement;
            var tag = mi?.Tag as string ?? string.Empty;
            SetMenuAction(string.Format("Sort: {0}", tag));
        }

        private void ExportPrimary_Click(object sender, RoutedEventArgs e)
            => SetMenuAction("Export (primary) clicked");

        private void ExportFormat_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as FrameworkElement;
            var tag = mi?.Tag as string ?? string.Empty;
            SetMenuAction(string.Format("Export: {0}", tag));
        }

        private void SetMenuAction(string action)
        {
            if (MenuActionLabel != null)
                MenuActionLabel.Text = string.Format("Last action: {0}", action);
        }

        // ── Menu bar ─────────────────────────────────────────────────────

        private void MenuBar_Click(object sender, RoutedEventArgs e)
        {
            var mi = sender as FrameworkElement;
            var tag = mi?.Tag as string ?? string.Empty;
            if (MenuBarResultLabel != null)
                MenuBarResultLabel.Text = string.Format("Last menu action: {0}", tag);
        }
    }
}
