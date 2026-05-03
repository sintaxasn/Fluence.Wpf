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
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using Fluence.Wpf.Automation;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Represents an entry inside a <see cref="NavigationView"/> pane.
    /// </summary>
    /// <remarks>Inspired by WinUI3's NavigationView.</remarks>
    public class NavigationViewItem : ListBoxItem
    {
        private static readonly DependencyPropertyKey IsPressedPropertyKey = DependencyProperty.RegisterReadOnly(
            "IsPressed",
            typeof(bool),
            typeof(NavigationViewItem),
            new FrameworkPropertyMetadata(false));

        /// <summary>Identifies the read-only <see cref="IsPressed"/> dependency property.</summary>
        public static readonly DependencyProperty IsPressedProperty = IsPressedPropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the <see cref="Icon"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            "Icon",
            typeof(object),
            typeof(NavigationViewItem),
            new PropertyMetadata(null, OnIconChanged));

        /// <summary>
        /// Identifies the <see cref="InfoBadge"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InfoBadgeProperty = DependencyProperty.Register(
            "InfoBadge",
            typeof(object),
            typeof(NavigationViewItem),
            new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="IsChildItem"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsChildItemProperty = DependencyProperty.Register(
            "IsChildItem",
            typeof(bool),
            typeof(NavigationViewItem),
            new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="PageContent"/> dependency property.
        /// </summary>
        /// <remarks>
        /// When set, the parent <see cref="NavigationView"/> shows this in the main content area while
        /// <see cref="ContentControl.Content"/> remains the pane label (typically a short string).
        /// </remarks>
        public static readonly DependencyProperty PageContentProperty = DependencyProperty.Register(
            "PageContent",
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

        /// <summary>
        /// Gets or sets an <see cref="Controls.InfoBadge"/> element shown on this item.
        /// </summary>
        public object InfoBadge
        {
            get { return GetValue(InfoBadgeProperty); }
            set { SetValue(InfoBadgeProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether this item is a child entry in an expanded navigation section.
        /// Child entries keep their selection indicator aligned with the content column.
        /// </summary>
        public bool IsChildItem
        {
            get { return (bool)GetValue(IsChildItemProperty); }
            set { SetValue(IsChildItemProperty, value); }
        }

        /// <summary>
        /// Gets or sets content shown in the <see cref="NavigationView"/> frame when this item is selected.
        /// When null, the frame uses <see cref="ContentControl.Content"/> (e.g. a string label for simple demos).
        /// </summary>
        public object PageContent
        {
            get { return GetValue(PageContentProperty); }
            set { SetValue(PageContentProperty, value); }
        }

        /// <summary>Gets whether the item is currently being pressed by a pointer.</summary>
        public bool IsPressed
        {
            get { return (bool)GetValue(IsPressedProperty); }
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if ((e.Key == Key.Enter || e.Key == Key.Space) && IsEnabled)
            {
                var nav = ItemsControl.ItemsControlFromItemContainer(this) as NavigationView;
                if (nav != null)
                {
                    object data = nav.ItemContainerGenerator.ItemFromContainer(this);
                    if (data == DependencyProperty.UnsetValue || data == null)
                    {
                        data = this;
                    }

                    if (!object.ReferenceEquals(nav.SelectedItem, data))
                    {
                        nav.SelectedItem = data;
                    }

                    e.Handled = true;
                    return;
                }
            }

            base.OnKeyDown(e);
        }

        /// <inheritdoc />
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            SetValue(IsPressedPropertyKey, true);
            Mouse.Capture(this, CaptureMode.SubTree);
            base.OnMouseLeftButtonDown(e);
        }

        /// <inheritdoc />
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            SetValue(IsPressedPropertyKey, false);
            Mouse.Capture(null);
            base.OnMouseLeftButtonUp(e);
        }

        /// <inheritdoc />
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (IsPressed)
            {
                SetValue(IsPressedPropertyKey, false);
            }

            base.OnMouseLeave(e);
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new NavigationViewItemAutomationPeer(this);
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

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ApplyDefaultFontIconSize(e.NewValue as FontIcon);
        }

        private static void ApplyDefaultFontIconSize(FontIcon icon)
        {
            if (icon == null)
            {
                return;
            }

            if (icon.ReadLocalValue(FontIcon.IconFontSizeProperty) == DependencyProperty.UnsetValue)
            {
                icon.SetCurrentValue(FontIcon.IconFontSizeProperty, 20.0);
            }
        }
    }
}
