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
using System.Windows.Input;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Represents an entry inside a <see cref="NavigationView"/> pane.
    /// </summary>
    /// <remarks>Inspired by WinUI3's NavigationView.</remarks>
    public class NavigationViewItem : ListBoxItem
    {
        /// <summary>
        /// Identifies the <see cref="Icon"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            "Icon",
            typeof(object),
            typeof(NavigationViewItem),
            new PropertyMetadata(null));

        static NavigationViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(NavigationViewItem),
                new FrameworkPropertyMetadata(typeof(NavigationViewItem)));
        }

        /// <summary>
        /// Gets or sets the icon content for this item (typically a <see cref="FontIcon"/>).
        /// </summary>
        public object Icon
        {
            get { return GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        /// <inheritdoc />
        /// <remarks>
        /// Parent <see cref="NavigationView"/> derives from <see cref="System.Windows.Controls.Primitives.Selector"/> (not <see cref="ListBox"/>).
        /// <see cref="ListBoxItem"/> handles mouse on the bubbling route and may mark the event handled before selection sync runs;
        /// we handle preview mouse and sync selection on the parent so clicks always update selection.
        /// </remarks>
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!IsEnabled || e.ClickCount != 1)
            {
                base.OnPreviewMouseLeftButtonDown(e);
                return;
            }

            var nav = ItemsControl.ItemsControlFromItemContainer(this) as NavigationView;
            if (nav == null)
            {
                base.OnPreviewMouseLeftButtonDown(e);
                return;
            }

            object data = nav.ItemContainerGenerator.ItemFromContainer(this);
            if (data == DependencyProperty.UnsetValue || data == null)
            {
                data = this;
            }

            if (!object.ReferenceEquals(nav.SelectedItem, data))
            {
                nav.SelectedItem = data;
            }

            Focus();
            e.Handled = true;
        }
    }
}
