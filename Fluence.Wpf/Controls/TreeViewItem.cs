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

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A Fluent Design tree view item with full-row hover highlight, animated chevron,
    /// and WinUI 3-canonical background brush states.
    /// Authority: WinUI 3 TreeView_themeresources.xaml + TreeViewItem.xaml.
    /// </summary>
    [TemplatePart(Name = PART_Header, Type = typeof(System.Windows.Controls.ContentPresenter))]
    [TemplatePart(Name = PART_ItemsHost, Type = typeof(System.Windows.Controls.ItemsPresenter))]
    public class TreeViewItem : System.Windows.Controls.TreeViewItem
    {
        // Template part names for the header and items host elements in the control template.
        private const string PART_Header = "PART_Header";
        private const string PART_ItemsHost = "ItemsHost";

        /// <summary>
        /// Initializes static members of the TreeViewItem class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that TreeViewItem uses its own style by default,
        /// rather than inheriting the style from its base class. This is important for applying custom control
        /// templates and visual styles specific to TreeViewItem.</remarks>
        static TreeViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(TreeViewItem),
                new FrameworkPropertyMetadata(typeof(TreeViewItem)));
        }

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new TreeViewItem();
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TreeViewItem;
        }
    }
}
