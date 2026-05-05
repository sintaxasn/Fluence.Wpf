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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Fluent-styled combo box with placeholder, icon, and rounded dropdown.
    /// Authority: WinUI 3 ComboBox_themeresources.xaml (FocusedStates / EditableFocusedStates VSM groups — WI-3 C18).
    /// </summary>
    [TemplatePart(Name = PART_Popup, Type = typeof(Popup))]
    [TemplatePart(Name = PART_DropdownBorder, Type = typeof(System.Windows.Controls.Border))]
    [TemplateVisualState(GroupName = "FocusedStates", Name = "Focused")]
    [TemplateVisualState(GroupName = "FocusedStates", Name = "Unfocused")]
    [TemplateVisualState(GroupName = "EditableFocusedStates", Name = "EditableFocused")]
    [TemplateVisualState(GroupName = "EditableFocusedStates", Name = "EditableUnfocused")]
    public class ComboBox : System.Windows.Controls.ComboBox
    {
        private const string PART_Popup = "PART_Popup";
        private const string PART_DropdownBorder = "PART_DropdownBorder";

        private static readonly DependencyPropertyKey SelectedContentPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(SelectedContent),
                typeof(object),
                typeof(ComboBox),
                new FrameworkPropertyMetadata(null));

        private static readonly DependencyPropertyKey SelectedTextPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(SelectedText),
                typeof(string),
                typeof(ComboBox),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="SelectedContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedContentProperty =
            SelectedContentPropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the <see cref="SelectedText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedTextProperty =
            SelectedTextPropertyKey.DependencyProperty;

        static ComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ComboBox),
                new FrameworkPropertyMetadata(typeof(ComboBox)));
        }

        /// <summary>
        /// Identifies the <see cref="PlaceholderText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(
                nameof(PlaceholderText),
                typeof(string),
                typeof(ComboBox),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the placeholder text displayed when no item is selected.
        /// </summary>
        public string PlaceholderText
        {
            get { return (string)GetValue(PlaceholderTextProperty); }
            set { SetValue(PlaceholderTextProperty, value); }
        }

        /// <summary>
        /// Gets the content of the currently selected item.
        /// </summary>
        public object SelectedContent
        {
            get { return GetValue(SelectedContentProperty); }
            private set { SetValue(SelectedContentPropertyKey, value); }
        }

        /// <summary>
        /// Gets the text representation of the currently selected item.
        /// </summary>
        public string SelectedText
        {
            get { return (string)GetValue(SelectedTextProperty); }
            private set { SetValue(SelectedTextPropertyKey, value); }
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(ComboBox),
                new FrameworkPropertyMetadata(new CornerRadius(4)));

        /// <summary>
        /// Gets or sets the corner radius of the combo box.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Icon"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(
                nameof(Icon),
                typeof(object),
                typeof(ComboBox),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the icon displayed in the combo box.
        /// </summary>
        public object Icon
        {
            get { return GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="DropdownCornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DropdownCornerRadiusProperty =
            DependencyProperty.Register(
                nameof(DropdownCornerRadius),
                typeof(CornerRadius),
                typeof(ComboBox),
                new FrameworkPropertyMetadata(new CornerRadius(8)));

        /// <summary>
        /// Gets or sets the corner radius of the dropdown popup.
        /// </summary>
        public CornerRadius DropdownCornerRadius
        {
            get { return (CornerRadius)GetValue(DropdownCornerRadiusProperty); }
            set { SetValue(DropdownCornerRadiusProperty, value); }
        }

        private static readonly DependencyPropertyKey IsDropDownOpenedUpwardPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsDropDownOpenedUpward),
                typeof(bool),
                typeof(ComboBox),
                new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="IsDropDownOpenedUpward"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsDropDownOpenedUpwardProperty =
            IsDropDownOpenedUpwardPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets whether the dropdown is currently displayed above the control.
        /// </summary>
        public bool IsDropDownOpenedUpward
        {
            get { return (bool)GetValue(IsDropDownOpenedUpwardProperty); }
            private set { SetValue(IsDropDownOpenedUpwardPropertyKey, value); }
        }

        private Popup _popup;
        private bool _isAutoSelecting;

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _popup = GetTemplateChild(PART_Popup) as Popup;
            UpdateSelectedContent();
            UpdateFocusState(false);

            if (SelectedIndex == -1 && Items.Count > 0 && !IsSelectedIndexExplicitlySet())
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    TryAutoSelectFirstItem();
                }), DispatcherPriority.Loaded);
            }
        }

        /// <inheritdoc />
        protected override void OnDropDownOpened(EventArgs e)
        {
            base.OnDropDownOpened(e);
            UpdateDropDownDirection();
        }

        /// <inheritdoc />
        protected override void OnDropDownClosed(EventArgs e)
        {
            base.OnDropDownClosed(e);
            IsDropDownOpenedUpward = false;
            if (_popup != null)
            {
                _popup.Placement = PlacementMode.Bottom;
            }
        }

        /// <inheritdoc />
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            UpdateSelectedContent();
        }

        /// <inheritdoc />
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            UpdateSelectedContent();
            TryAutoSelectFirstItem();
        }

        /// <inheritdoc />
        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            UpdateFocusState(true);
        }

        /// <inheritdoc />
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            UpdateFocusState(true);
        }

        private void UpdateFocusState(bool useTransitions)
        {
            VisualStateManager.GoToState(
                this,
                IsKeyboardFocusWithin ? "Focused" : "Unfocused",
                useTransitions);
        }

        private void UpdateDropDownDirection()
        {
            if (_popup == null)
            {
                return;
            }

            try
            {
                var source = PresentationSource.FromVisual(this);
                if (source == null || source.CompositionTarget == null)
                {
                    return;
                }

                var bottomEdge = PointToScreen(new Point(0, ActualHeight));
                var topEdge = PointToScreen(new Point(0, 0));

                double dpiY = source.CompositionTarget.TransformToDevice.M22;
                double maxHeightPx = (MaxDropDownHeight > 0 ? MaxDropDownHeight : 480) * dpiY;
                double workBottom = SystemParameters.WorkArea.Bottom * dpiY;
                double workTop = SystemParameters.WorkArea.Top * dpiY;

                double spaceBelow = workBottom - bottomEdge.Y;
                double spaceAbove = topEdge.Y - workTop;

                bool openUpward = spaceBelow < maxHeightPx && spaceAbove > spaceBelow;

                IsDropDownOpenedUpward = openUpward;
                _popup.Placement = openUpward ? PlacementMode.Top : PlacementMode.Bottom;
            }
            catch
            {
                IsDropDownOpenedUpward = false;
                if (_popup != null)
                {
                    _popup.Placement = PlacementMode.Bottom;
                }
            }
        }

        private bool IsSelectedIndexExplicitlySet()
        {
            var source = DependencyPropertyHelper.GetValueSource(
                this, Selector.SelectedIndexProperty);
            return source.BaseValueSource != BaseValueSource.Default;
        }

        private void TryAutoSelectFirstItem()
        {
            if (_isAutoSelecting || SelectedIndex != -1 || Items.Count == 0)
            {
                return;
            }

            if (!IsSelectedIndexExplicitlySet())
            {
                _isAutoSelecting = true;
                try
                {
                    SelectedIndex = 0;
                }
                finally
                {
                    _isAutoSelecting = false;
                }
            }
        }

        private void UpdateSelectedContent()
        {
            var item = SelectedItem;
            var comboBoxItem = item as ComboBoxItem;
            if (comboBoxItem != null)
            {
                SelectedContent = comboBoxItem.Content;
                SelectedText = comboBoxItem.Content != null ? comboBoxItem.Content.ToString() : string.Empty;
                return;
            }

            SelectedContent = item;
            SelectedText = item != null ? item.ToString() : string.Empty;
        }
    }
}
