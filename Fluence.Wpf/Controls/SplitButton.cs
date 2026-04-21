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
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Fluence.Wpf.Automation;
using Fluence.Wpf;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A button that combines a primary action (<see cref="System.Windows.Controls.Primitives.ButtonBase.Click"/> /
    /// <see cref="Command"/>) on its left half with a secondary "chevron" half that opens a flyout popup
    /// containing arbitrary WPF content. The canonical WinUI 3 SplitButton pattern.
    /// </summary>
    [TemplatePart(Name = PART_PrimaryButton, Type = typeof(System.Windows.Controls.Button))]
    [TemplatePart(Name = PART_SecondaryButton, Type = typeof(System.Windows.Controls.Primitives.ToggleButton))]
    [TemplatePart(Name = PART_Popup, Type = typeof(Popup))]
    public class SplitButton : ContentControl, ICommandSource
    {
        private const string PART_PrimaryButton = "PART_PrimaryButton";
        private const string PART_SecondaryButton = "PART_SecondaryButton";
        private const string PART_Popup = "PART_Popup";

        private System.Windows.Controls.Button _primaryButton;
        private System.Windows.Controls.Primitives.ToggleButton _secondaryButton;
        private Popup _popup;

        static SplitButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(SplitButton),
                new FrameworkPropertyMetadata(typeof(SplitButton)));
        }

        #region Click routed event

        /// <summary>
        /// Identifies the <see cref="Click"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ClickEvent =
            EventManager.RegisterRoutedEvent(
                "Click",
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(SplitButton));

        /// <summary>
        /// Raised when the primary half of the split button is clicked.
        /// </summary>
        public event RoutedEventHandler Click
        {
            add { AddHandler(ClickEvent, value); }
            remove { RemoveHandler(ClickEvent, value); }
        }

        #endregion

        #region Command / CommandParameter / CommandTarget

        /// <summary>
        /// Identifies the <see cref="Command"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(SplitButton),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the command to invoke when the primary half is clicked.
        /// </summary>
        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="CommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(
                nameof(CommandParameter),
                typeof(object),
                typeof(SplitButton),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the parameter passed to <see cref="Command"/>.
        /// </summary>
        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="CommandTarget"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandTargetProperty =
            DependencyProperty.Register(
                nameof(CommandTarget),
                typeof(IInputElement),
                typeof(SplitButton),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the element that the command is raised on.
        /// </summary>
        public IInputElement CommandTarget
        {
            get { return (IInputElement)GetValue(CommandTargetProperty); }
            set { SetValue(CommandTargetProperty, value); }
        }

        #endregion

        #region Flyout / FlyoutTemplate / DropdownCornerRadius

        /// <summary>
        /// Identifies the <see cref="Flyout"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FlyoutProperty =
            DependencyProperty.Register(
                nameof(Flyout),
                typeof(object),
                typeof(SplitButton),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the content displayed in the secondary-half flyout popup.
        /// </summary>
        public object Flyout
        {
            get { return GetValue(FlyoutProperty); }
            set { SetValue(FlyoutProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="FlyoutTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FlyoutTemplateProperty =
            DependencyProperty.Register(
                nameof(FlyoutTemplate),
                typeof(DataTemplate),
                typeof(SplitButton),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> used to render <see cref="Flyout"/>.
        /// </summary>
        public DataTemplate FlyoutTemplate
        {
            get { return (DataTemplate)GetValue(FlyoutTemplateProperty); }
            set { SetValue(FlyoutTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="DropdownCornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DropdownCornerRadiusProperty =
            DependencyProperty.Register(
                nameof(DropdownCornerRadius),
                typeof(CornerRadius),
                typeof(SplitButton),
                new FrameworkPropertyMetadata(new CornerRadius(8)));

        /// <summary>
        /// Gets or sets the corner radius of the flyout popup surface.
        /// </summary>
        public CornerRadius DropdownCornerRadius
        {
            get { return (CornerRadius)GetValue(DropdownCornerRadiusProperty); }
            set { SetValue(DropdownCornerRadiusProperty, value); }
        }

        #endregion

        #region CornerRadius

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(SplitButton),
                new FrameworkPropertyMetadata(new CornerRadius(4)));

        /// <summary>
        /// Gets or sets the outer corner radius. Note that the inner halves are split
        /// by a 1 px divider; the left half rounds its left corners only and the right
        /// half rounds its right corners only using this radius.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        #endregion

        #region Appearance

        /// <summary>
        /// Identifies the <see cref="Appearance"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AppearanceProperty =
            DependencyProperty.Register(
                nameof(Appearance),
                typeof(ControlAppearance),
                typeof(SplitButton),
                new FrameworkPropertyMetadata(ControlAppearance.Standard));

        /// <summary>
        /// Gets or sets the visual appearance of the split button.
        /// Set to <see cref="ControlAppearance.Accent"/> to apply the accent-colored variant;
        /// the divider stroke will automatically switch to
        /// <c>ControlStrokeColorOnAccentSecondaryBrush</c> per WinUI 3 canonical styling.
        /// </summary>
        public ControlAppearance Appearance
        {
            get { return (ControlAppearance)GetValue(AppearanceProperty); }
            set { SetValue(AppearanceProperty, value); }
        }

        #endregion

        #region IsFlyoutOpen (read-only)

        private static readonly DependencyPropertyKey IsFlyoutOpenPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsFlyoutOpen),
                typeof(bool),
                typeof(SplitButton),
                new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="IsFlyoutOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFlyoutOpenProperty =
            IsFlyoutOpenPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value indicating whether the secondary-half flyout popup is currently open.
        /// </summary>
        public bool IsFlyoutOpen
        {
            get { return (bool)GetValue(IsFlyoutOpenProperty); }
        }

        #endregion

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new SplitButtonAutomationPeer(this);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            DetachPrimaryHandler();
            DetachSecondaryHandler();
            DetachPopupHandler();

            base.OnApplyTemplate();

            _primaryButton = GetTemplateChild(PART_PrimaryButton) as System.Windows.Controls.Button;
            _secondaryButton = GetTemplateChild(PART_SecondaryButton) as System.Windows.Controls.Primitives.ToggleButton;
            _popup = GetTemplateChild(PART_Popup) as Popup;

            if (_primaryButton != null)
            {
                _primaryButton.Click += OnPrimaryButtonClick;
            }

            if (_secondaryButton != null)
            {
                _secondaryButton.Checked += OnSecondaryButtonChecked;
                _secondaryButton.Unchecked += OnSecondaryButtonUnchecked;
            }

            if (_popup != null)
            {
                _popup.StaysOpen = false;
                _popup.PlacementTarget = this;
                _popup.Closed += OnPopupClosed;
                _popup.IsOpen = _secondaryButton != null && _secondaryButton.IsChecked == true;
            }
        }

        private void OnPrimaryButtonClick(object sender, RoutedEventArgs e)
        {
            // Primary half: raise Click and invoke Command.
            RaiseEvent(new RoutedEventArgs(ClickEvent, this));

            var command = Command;
            if (command == null)
            {
                return;
            }

            var parameter = CommandParameter;
            var target = CommandTarget;

            var routedCommand = command as RoutedCommand;
            if (routedCommand != null)
            {
                if (routedCommand.CanExecute(parameter, target))
                {
                    routedCommand.Execute(parameter, target);
                }
            }
            else if (command.CanExecute(parameter))
            {
                command.Execute(parameter);
            }
        }

        private void OnSecondaryButtonChecked(object sender, RoutedEventArgs e)
        {
            if (_popup != null)
            {
                _popup.IsOpen = true;
            }

            SetValue(IsFlyoutOpenPropertyKey, true);
        }

        private void OnSecondaryButtonUnchecked(object sender, RoutedEventArgs e)
        {
            if (_popup != null)
            {
                _popup.IsOpen = false;
            }

            SetValue(IsFlyoutOpenPropertyKey, false);
        }

        private void OnPopupClosed(object sender, EventArgs e)
        {
            // External click (StaysOpen=false) closed the popup; sync the secondary toggle.
            if (_secondaryButton != null && _secondaryButton.IsChecked == true)
            {
                _secondaryButton.IsChecked = false;
            }
            else
            {
                SetValue(IsFlyoutOpenPropertyKey, false);
            }
        }

        private void DetachPrimaryHandler()
        {
            if (_primaryButton != null)
            {
                _primaryButton.Click -= OnPrimaryButtonClick;
                _primaryButton = null;
            }
        }

        private void DetachSecondaryHandler()
        {
            if (_secondaryButton != null)
            {
                _secondaryButton.Checked -= OnSecondaryButtonChecked;
                _secondaryButton.Unchecked -= OnSecondaryButtonUnchecked;
                _secondaryButton = null;
            }
        }

        private void DetachPopupHandler()
        {
            if (_popup != null)
            {
                _popup.Closed -= OnPopupClosed;
                _popup = null;
            }
        }
    }
}
