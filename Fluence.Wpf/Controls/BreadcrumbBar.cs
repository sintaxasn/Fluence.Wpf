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
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A horizontal trail of crumbs mirroring the WinUI 3 <c language="csharp">BreadcrumbBar</c>: each item
    /// renders as a clickable <see cref="BreadcrumbBarItem"/> followed by a chevron separator,
    /// with the last crumb shown as the current location (no trailing chevron, primary text,
    /// SemiBold).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Items come from <see cref="ItemsControl.ItemsSource"/> (or the
    /// inline items collection) and render through the normal ItemsControl mechanics, so
    /// <see cref="ItemsControl.DisplayMemberPath"/> and
    /// <see cref="ItemsControl.ItemTemplate"/> are respected. Subscribe
    /// to <see cref="ItemClicked"/> to navigate; the event is raised for every crumb including
    /// the last one, matching WinUI.
    /// </para>
    /// <para>When width is constrained, an ellipsis button gives access to the trail and the
    /// current location remains in view.</para>
    /// </remarks>
    [TemplatePart(Name = PART_OverflowButton, Type = typeof(System.Windows.Controls.Button))]
    [TemplatePart(Name = PART_ItemsScrollViewer, Type = typeof(ScrollViewer))]
    public class BreadcrumbBar : ItemsControl
    {
        private const string PART_OverflowButton = "PART_OverflowButton";
        private const string PART_ItemsScrollViewer = "PART_ItemsScrollViewer";

        private System.Windows.Controls.Button? _overflowButton;
        private ScrollViewer? _itemsScrollViewer;
        private ContextMenu? _overflowMenu;

        /// <summary>
        /// Initializes static members of the BreadcrumbBar class and overrides the default
        /// style metadata so the control picks up its themed template from Generic.xaml.
        /// </summary>
        static BreadcrumbBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(BreadcrumbBar),
                new FrameworkPropertyMetadata(typeof(BreadcrumbBar)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BreadcrumbBar"/> class and subscribes
        /// to child <see cref="BreadcrumbBarItem.Click"/> events for aggregation into
        /// <see cref="ItemClicked"/>.
        /// </summary>
        public BreadcrumbBar()
        {
            AddHandler(BreadcrumbBarItem.ClickEvent, new RoutedEventHandler(OnBreadcrumbItemClick));
        }

        /// <summary>
        /// Occurs when any crumb is clicked, including the last (current) one, matching the
        /// WinUI 3 BreadcrumbBar contract. The event args carry the clicked data item and its
        /// zero-based index in the items collection.
        /// </summary>
        public event EventHandler<BreadcrumbBarItemClickedEventArgs>? ItemClicked;

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            if (_overflowButton is { } previousButton)
            {
                previousButton.Click -= OnOverflowButtonClick;
                if (ReferenceEquals(previousButton.ContextMenu, _overflowMenu))
                {
                    previousButton.ContextMenu = null;
                }
            }

            if (_itemsScrollViewer is { } previousViewer)
            {
                previousViewer.ScrollChanged -= OnItemsScrollChanged;
            }

            if (_overflowMenu is not null)
            {
                _overflowMenu.Opened -= OnOverflowMenuOpened;
                _overflowMenu.IsOpen = false;
                _overflowMenu.Items.Clear();
                _overflowMenu.PlacementTarget = null;
            }

            _overflowButton = null;
            _itemsScrollViewer = null;
            _overflowMenu = null;

            base.OnApplyTemplate();

            _overflowButton = GetTemplateChild(PART_OverflowButton) as System.Windows.Controls.Button;
            _itemsScrollViewer = GetTemplateChild(PART_ItemsScrollViewer) as ScrollViewer;
            if (_overflowButton is not null)
            {
                _overflowMenu = new ContextMenu
                {
                    Placement = PlacementMode.Bottom,
                    PlacementTarget = _overflowButton,
                };
                _overflowMenu.Opened += OnOverflowMenuOpened;
                _overflowButton.ContextMenu = _overflowMenu;
                _overflowButton.Click += OnOverflowButtonClick;
            }

            if (_itemsScrollViewer is { } itemsScrollViewer)
            {
                itemsScrollViewer.ScrollChanged += OnItemsScrollChanged;
            }
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new Automation.BreadcrumbBarAutomationPeer(this);
        }

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new BreadcrumbBarItem();
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is BreadcrumbBarItem;
        }

        /// <inheritdoc />
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            // Containers are generated lazily during layout, after OnItemsChanged has run, so
            // the freshly prepared container must trigger its own last-item refresh.
            UpdateLastItemState();
        }

        /// <inheritdoc />
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            // Adds, removes, moves, and resets can all change which crumb is last; refresh the
            // realized containers immediately (new containers refresh again when prepared).
            UpdateLastItemState();
            if (_overflowMenu?.IsOpen is true)
            {
                RebuildOverflowMenu();
            }
        }

        private void OnItemsScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_overflowButton is null || _itemsScrollViewer is null)
            {
                return;
            }

            bool hasOverflow = _itemsScrollViewer.ExtentWidth > _itemsScrollViewer.ViewportWidth + 0.5;
            _overflowButton.Visibility = hasOverflow ? Visibility.Visible : Visibility.Collapsed;

            if (e.ExtentWidthChange is not 0 || e.ViewportWidthChange is not 0)
            {
                for (int index = 0; index < Items.Count; index++)
                {
                    if (ItemContainerGenerator.ContainerFromIndex(index) is BreadcrumbBarItem { IsKeyboardFocusWithin: true } focused)
                    {
                        focused.BringIntoView();
                        return;
                    }
                }

                if (ItemContainerGenerator.ContainerFromIndex(Items.Count - 1) is BreadcrumbBarItem current)
                {
                    current.BringIntoView();
                }
            }
        }

        private void OnOverflowButtonClick(object sender, RoutedEventArgs e)
        {
            if (_overflowMenu is { } overflowMenu)
            {
                overflowMenu.IsOpen = true;
            }
        }

        private void OnOverflowMenuOpened(object sender, RoutedEventArgs e)
        {
            RebuildOverflowMenu();
        }

        private void RebuildOverflowMenu()
        {
            if (_overflowMenu is null)
            {
                return;
            }

            _overflowMenu.Items.Clear();
            for (int index = 0; index < Items.Count; index++)
            {
                int itemIndex = index;
                object item = Items[index];
                object displayItem = item is BreadcrumbBarItem crumb ? crumb.Content : item;
                string accessibleText = ItemContainerGenerator.ContainerFromIndex(index) is BreadcrumbBarItem container
                    ? container.GetAccessibleText()
                    : string.Empty;
                MenuItem menuItem = new();

                if (displayItem is UIElement element)
                {
                    // A live element cannot be parented into both the crumb and the menu.
                    menuItem.Header = !string.IsNullOrWhiteSpace(accessibleText)
                        ? accessibleText
                        : GetElementLabel(element, index);
                }
                else if (item is not BreadcrumbBarItem && !string.IsNullOrWhiteSpace(DisplayMemberPath))
                {
                    _ = menuItem.SetBinding(HeaderedItemsControl.HeaderProperty,
                        new Binding(DisplayMemberPath) { Source = item });
                }
                else if (item is not BreadcrumbBarItem)
                {
                    menuItem.Header = item;
                    menuItem.HeaderTemplate = ItemTemplate;
                    menuItem.HeaderTemplateSelector = ItemTemplateSelector;
                    menuItem.HeaderStringFormat = ItemStringFormat;
                }
                else
                {
                    BreadcrumbBarItem directCrumb = (BreadcrumbBarItem)item;
                    menuItem.Header = displayItem;
                    menuItem.HeaderTemplate = directCrumb.ContentTemplate;
                    menuItem.HeaderTemplateSelector = directCrumb.ContentTemplateSelector;
                    menuItem.HeaderStringFormat = directCrumb.ContentStringFormat;
                }

                if (!string.IsNullOrWhiteSpace(accessibleText))
                {
                    AutomationProperties.SetName(menuItem, accessibleText);
                }

                menuItem.Click += (_, _) =>
                    ItemClicked?.Invoke(this, new BreadcrumbBarItemClickedEventArgs(item, itemIndex));
                _ = _overflowMenu.Items.Add(menuItem);
            }
        }

        private static string GetElementLabel(UIElement element, int index)
        {
            string name = AutomationProperties.GetName(element);
            return !string.IsNullOrWhiteSpace(name)
                ? name
                : element is System.Windows.Controls.TextBlock textBlock &&
                    !string.IsNullOrWhiteSpace(textBlock.Text)
                        ? textBlock.Text
                        : string.Format(CultureInfo.InvariantCulture, "Location {0}", index + 1);
        }

        private void OnBreadcrumbItemClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not BreadcrumbBarItem crumb)
            {
                return;
            }

            int index = ItemContainerGenerator.IndexFromContainer(crumb);
            if (index < 0)
            {
                return;
            }

            object item = ItemContainerGenerator.ItemFromContainer(crumb);
            if (item == DependencyProperty.UnsetValue)
            {
                item = crumb;
            }

            ItemClicked?.Invoke(this, new BreadcrumbBarItemClickedEventArgs(item, index));
        }

        /// <summary>
        /// Reapplies <see cref="BreadcrumbBarItem.IsLastItem"/> across all realized containers
        /// so exactly the crumb at the final index renders as the current location.
        /// </summary>
        private void UpdateLastItemState()
        {
            int lastIndex = Items.Count - 1;
            for (int index = 0; index <= lastIndex; index++)
            {
                if (ItemContainerGenerator.ContainerFromIndex(index) is BreadcrumbBarItem crumb)
                {
                    crumb.IsLastItem = index == lastIndex;
                }
            }
        }
    }
}
