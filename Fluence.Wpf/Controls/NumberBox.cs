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
using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using Fluence.Wpf.Automation;

// IMPORTANT: every reference to RepeatButton / TextBox in this file MUST be
// fully qualified (System.Windows.Controls.Primitives.RepeatButton,
// System.Windows.Controls.TextBox). The Fluence.Wpf.Controls namespace
// defines its own RepeatButton and TextBox subclasses, and because this file
// sits inside that namespace, any unqualified reference resolves to the
// Fluence subclass. The default NumberBox template instantiates the stock
// WPF primitives, so `as RepeatButton` against the Fluence subclass silently
// returns null and the spin-button Click handlers never get attached.
// Using aliases do not work here either — C# enforces CS0576 when an alias
// collides with a namespace member, so fully-qualified names are the only
// option.
namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Event data for <see cref="NumberBox.ValueChanged"/>.
    /// </summary>
    public sealed class NumberBoxValueChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NumberBoxValueChangedEventArgs"/> class.
        /// </summary>
        /// <param name="oldValue">The previous value.</param>
        /// <param name="newValue">The new value.</param>
        public NumberBoxValueChangedEventArgs(double oldValue, double newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        /// <summary>
        /// Gets the previous value.
        /// </summary>
        public double OldValue { get; }

        /// <summary>
        /// Gets the new value.
        /// </summary>
        public double NewValue { get; }
    }

    /// <summary>
    /// A numeric input control with optional spin buttons and min/max clamping.
    /// </summary>
    [TemplatePart(Name = PartTextBox, Type = typeof(System.Windows.Controls.TextBox))]
    [TemplatePart(Name = PartUpButton, Type = typeof(System.Windows.Controls.Primitives.RepeatButton))]
    [TemplatePart(Name = PartDownButton, Type = typeof(System.Windows.Controls.Primitives.RepeatButton))]
    public class NumberBox : Control
    {
        private const string PartTextBox = "PART_TextBox";
        private const string PartUpButton = "PART_UpButton";
        private const string PartDownButton = "PART_DownButton";

        private System.Windows.Controls.TextBox _partTextBox;
        private System.Windows.Controls.Primitives.RepeatButton _partUpButton;
        private System.Windows.Controls.Primitives.RepeatButton _partDownButton;
        private bool _suppressTextSync;

        static NumberBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(NumberBox),
                new FrameworkPropertyMetadata(typeof(NumberBox)));
        }

        /// <summary>
        /// Identifies the <see cref="Value"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(NumberBox),
                new FrameworkPropertyMetadata(
                    0.0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnValuePropertyChanged,
                    CoerceValueCallback));

        /// <summary>
        /// Identifies the <see cref="Minimum"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(NumberBox),
                new FrameworkPropertyMetadata(double.MinValue, OnMinMaxPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="Maximum"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(double),
                typeof(NumberBox),
                new FrameworkPropertyMetadata(double.MaxValue, OnMinMaxPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="SmallChange"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SmallChangeProperty =
            DependencyProperty.Register(
                nameof(SmallChange),
                typeof(double),
                typeof(NumberBox),
                new FrameworkPropertyMetadata(1.0));

        /// <summary>
        /// Identifies the <see cref="LargeChange"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LargeChangeProperty =
            DependencyProperty.Register(
                nameof(LargeChange),
                typeof(double),
                typeof(NumberBox),
                new FrameworkPropertyMetadata(10.0));

        /// <summary>
        /// Identifies the <see cref="Header"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(object),
                typeof(NumberBox),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="SpinButtonPlacementMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SpinButtonPlacementModeProperty =
            DependencyProperty.Register(
                nameof(SpinButtonPlacementMode),
                typeof(SpinButtonPlacementMode),
                typeof(NumberBox),
                new FrameworkPropertyMetadata(SpinButtonPlacementMode.Compact));

        /// <summary>
        /// Identifies the <see cref="AcceptsExpression"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AcceptsExpressionProperty =
            DependencyProperty.Register(
                nameof(AcceptsExpression),
                typeof(bool),
                typeof(NumberBox),
                new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="PlaceholderText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(
                nameof(PlaceholderText),
                typeof(string),
                typeof(NumberBox),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="Description"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(
                nameof(Description),
                typeof(string),
                typeof(NumberBox),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="Text"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(NumberBox),
                new FrameworkPropertyMetadata(
                    "0",
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnTextPropertyChanged));

        /// <summary>
        /// Occurs when <see cref="Value"/> changes after coercion.
        /// </summary>
        public event EventHandler<NumberBoxValueChangedEventArgs> ValueChanged;

        /// <summary>
        /// Gets or sets the numeric value.
        /// </summary>
        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        /// <summary>
        /// Gets or sets the minimum allowed value.
        /// </summary>
        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        /// <summary>
        /// Gets or sets the maximum allowed value.
        /// </summary>
        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        /// <summary>
        /// Gets or sets the increment used by spin buttons.
        /// </summary>
        public double SmallChange
        {
            get { return (double)GetValue(SmallChangeProperty); }
            set { SetValue(SmallChangeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the large increment (reserved for keyboard/page navigation).
        /// </summary>
        public double LargeChange
        {
            get { return (double)GetValue(LargeChangeProperty); }
            set { SetValue(LargeChangeProperty, value); }
        }

        /// <summary>
        /// Gets or sets an optional header displayed above the input.
        /// </summary>
        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        /// <summary>
        /// Gets or sets where spin buttons are shown.
        /// </summary>
        public SpinButtonPlacementMode SpinButtonPlacementMode
        {
            get { return (SpinButtonPlacementMode)GetValue(SpinButtonPlacementModeProperty); }
            set { SetValue(SpinButtonPlacementModeProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the control may parse simple expressions (reserved).
        /// </summary>
        public bool AcceptsExpression
        {
            get { return (bool)GetValue(AcceptsExpressionProperty); }
            set { SetValue(AcceptsExpressionProperty, value); }
        }

        /// <summary>
        /// Gets or sets watermark text shown when the text box is empty.
        /// </summary>
        public string PlaceholderText
        {
            get { return (string)GetValue(PlaceholderTextProperty); }
            set { SetValue(PlaceholderTextProperty, value); }
        }

        /// <summary>
        /// Gets or sets helper text displayed below the control.
        /// </summary>
        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        /// <summary>
        /// Gets or sets the text representation of the value.
        /// </summary>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        /// <summary>
        /// Updates <see cref="Value"/> from <see cref="Text"/> if parsing succeeds.
        /// </summary>
        /// <returns><c>true</c> if a number was parsed and applied; otherwise <c>false</c>.</returns>
        public bool TryParseText()
        {
            string s = Text;
            if (s == null)
            {
                s = string.Empty;
            }

            double parsed;
            if (AcceptsExpression)
            {
                s = s.Trim();
            }

            if (!double.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out parsed))
            {
                return false;
            }

            Value = parsed;
            return true;
        }

        /// <summary>
        /// Raises the <see cref="ValueChanged"/> event.
        /// </summary>
        /// <param name="oldValue">The previous value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnValueChanged(double oldValue, double newValue)
        {
            EventHandler<NumberBoxValueChangedEventArgs> handler = ValueChanged;
            if (handler != null)
            {
                handler(this, new NumberBoxValueChangedEventArgs(oldValue, newValue));
            }
        }

        /// <summary>
        /// Increments <see cref="Value"/> by <see cref="SmallChange"/> with clamping.
        /// </summary>
        protected virtual void OnUpClick()
        {
            Value = ClampValue(Value + SmallChange);
        }

        /// <summary>
        /// Decrements <see cref="Value"/> by <see cref="SmallChange"/> with clamping.
        /// </summary>
        protected virtual void OnDownClick()
        {
            Value = ClampValue(Value - SmallChange);
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new NumberBoxAutomationPeer(this);
        }

        /// <inheritdoc />
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            if (_partTextBox != null && !_partTextBox.IsKeyboardFocusWithin)
            {
                _partTextBox.Focus();
            }
        }

        /// <inheritdoc />
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            if (_partTextBox != null && !_partTextBox.IsKeyboardFocusWithin)
            {
                _partTextBox.Focus();
            }
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_partTextBox != null)
            {
                _partTextBox.KeyDown -= OnPartTextBoxKeyDown;
                _partTextBox.LostKeyboardFocus -= OnPartTextBoxLostKeyboardFocus;
            }

            if (_partUpButton != null)
            {
                _partUpButton.Click -= OnPartUpButtonClick;
            }

            if (_partDownButton != null)
            {
                _partDownButton.Click -= OnPartDownButtonClick;
            }

            _partTextBox = GetTemplateChild(PartTextBox) as System.Windows.Controls.TextBox;
            _partUpButton = GetTemplateChild(PartUpButton) as System.Windows.Controls.Primitives.RepeatButton;
            _partDownButton = GetTemplateChild(PartDownButton) as System.Windows.Controls.Primitives.RepeatButton;

            if (_partTextBox != null)
            {
                _partTextBox.KeyDown += OnPartTextBoxKeyDown;
                _partTextBox.LostKeyboardFocus += OnPartTextBoxLostKeyboardFocus;
            }

            if (_partUpButton != null)
            {
                _partUpButton.Click += OnPartUpButtonClick;
            }

            if (_partDownButton != null)
            {
                _partDownButton.Click += OnPartDownButtonClick;
            }

            UpdateTextFromValue();
        }

        private static object CoerceValueCallback(DependencyObject d, object baseValue)
        {
            var box = (NumberBox)d;
            double v = (double)baseValue;
            if (double.IsNaN(v))
            {
                return baseValue;
            }

            double clamped = box.ClampValue(v);
            return double.IsNaN(clamped) ? baseValue : (object)clamped;
        }

        private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Value is already clamped by CoerceValueCallback; OldValue/NewValue are both committed values.
            var box = (NumberBox)d;
            box.OnValueChanged((double)e.OldValue, (double)e.NewValue);
            box.UpdateTextFromValue();
        }

        private static void OnMinMaxPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Re-coerce Value so it stays within the new bounds.
            ((NumberBox)d).CoerceValue(ValueProperty);
        }

        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var box = (NumberBox)d;
            if (box._suppressTextSync)
            {
                return;
            }

            if (box._partTextBox != null && !string.Equals(box._partTextBox.Text, box.Text as string, StringComparison.Ordinal))
            {
                box._partTextBox.Text = box.Text != null ? box.Text : string.Empty;
            }
        }

        private void OnPartTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TryParseText();
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                TryParseText();
                OnUpClick();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                TryParseText();
                OnDownClick();
                e.Handled = true;
            }
        }

        private void OnPartTextBoxLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TryParseText();
        }

        private void OnPartUpButtonClick(object sender, RoutedEventArgs e)
        {
            OnUpClick();
        }

        private void OnPartDownButtonClick(object sender, RoutedEventArgs e)
        {
            OnDownClick();
        }

        private void UpdateTextFromValue()
        {
            string formatted = Value.ToString(CultureInfo.CurrentCulture);
            _suppressTextSync = true;
            try
            {
                SetCurrentValue(TextProperty, formatted);
                if (_partTextBox != null && !string.Equals(_partTextBox.Text, formatted, StringComparison.Ordinal))
                {
                    _partTextBox.Text = formatted;
                }
            }
            finally
            {
                _suppressTextSync = false;
            }
        }

        private double ClampValue(double value)
        {
            double min = Minimum;
            double max = Maximum;
            if (min > max)
            {
                double t = min;
                min = max;
                max = t;
            }

            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private static bool AreClose(double a, double b)
        {
            const double eps = 1e-9;
            return Math.Abs(a - b) < eps;
        }
    }
}
