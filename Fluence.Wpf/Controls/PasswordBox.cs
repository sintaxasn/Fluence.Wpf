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
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A Fluent Design styled password box with reveal toggle support.
    /// </summary>
    [TemplatePart(Name = "PART_PasswordBox", Type = typeof(System.Windows.Controls.PasswordBox))]
    [TemplatePart(Name = "PART_RevealTextBox", Type = typeof(System.Windows.Controls.TextBox))]
    [TemplatePart(Name = "PART_RevealButton", Type = typeof(System.Windows.Controls.Button))]
    [TemplatePart(Name = "PART_CapsLockIndicator", Type = typeof(FrameworkElement))]
    public partial class PasswordBox : Control
    {
        private const string LowercasePasswordPattern = "[a-z]";
        private const string UppercasePasswordPattern = "[A-Z]";
        private const string DigitPasswordPattern = "[0-9]";
        private const string SymbolPasswordPattern = "[^a-zA-Z0-9]";

#if !NET7_0_OR_GREATER
        private static readonly Regex LowercasePasswordRegexFallback = new Regex(LowercasePasswordPattern, RegexOptions.CultureInvariant);
        private static readonly Regex UppercasePasswordRegexFallback = new Regex(UppercasePasswordPattern, RegexOptions.CultureInvariant);
        private static readonly Regex DigitPasswordRegexFallback = new Regex(DigitPasswordPattern, RegexOptions.CultureInvariant);
        private static readonly Regex SymbolPasswordRegexFallback = new Regex(SymbolPasswordPattern, RegexOptions.CultureInvariant);
#endif

        private System.Windows.Controls.PasswordBox _passwordBox;
        private System.Windows.Controls.TextBox _revealTextBox;
        private System.Windows.Controls.Button _revealButton;
        private bool _isUpdatingPassword;
        private DispatcherTimer _capsPollTimer;
        private readonly EventHandler _capsPollTick;

        static PasswordBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(typeof(PasswordBox)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordBox"/> class.
        /// </summary>
        public PasswordBox()
        {
            _capsPollTick = OnCapsPollTick;
        }

        private void OnCapsPollTick(object sender, EventArgs e)
        {
            UpdateCapsLockIndicator();
        }

        /// <summary>
        /// Identifies the <see cref="Password"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(
                nameof(Password),
                typeof(string),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPasswordChanged));

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PasswordBox control = (PasswordBox)d;
            if (control._isUpdatingPassword)
            {
                return;
            }

            control._isUpdatingPassword = true;
            try
            {
                if (control._passwordBox != null)
                {
                    control._passwordBox.Password = (string)e.NewValue ?? string.Empty;
                }
                if (control._revealTextBox != null)
                {
                    control._revealTextBox.Text = (string)e.NewValue ?? string.Empty;
                }
            }
            finally
            {
                control._isUpdatingPassword = false;
            }

            control.UpdatePasswordStrengthFromPassword();
            control.UpdateStrengthMeter();
        }

        /// <summary>
        /// Identifies the <see cref="PasswordChar"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PasswordCharProperty =
            DependencyProperty.Register(
                nameof(PasswordChar),
                typeof(char),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata('\u2022')); // bullet character

        /// <summary>
        /// Gets or sets the masking character for the password.
        /// </summary>
        public char PasswordChar
        {
            get { return (char)GetValue(PasswordCharProperty); }
            set { SetValue(PasswordCharProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="RevealButtonEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RevealButtonEnabledProperty =
            DependencyProperty.Register(
                nameof(RevealButtonEnabled),
                typeof(bool),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether the reveal button is enabled.
        /// </summary>
        public bool RevealButtonEnabled
        {
            get { return (bool)GetValue(RevealButtonEnabledProperty); }
            set { SetValue(RevealButtonEnabledProperty, value); }
        }

        private static readonly DependencyPropertyKey IsPasswordRevealedPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsPasswordRevealed),
                typeof(bool),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="IsPasswordRevealed"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPasswordRevealedProperty =
            IsPasswordRevealedPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets whether the password is currently revealed.
        /// </summary>
        public bool IsPasswordRevealed
        {
            get { return (bool)GetValue(IsPasswordRevealedProperty); }
            private set { SetValue(IsPasswordRevealedPropertyKey, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PlaceholderText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(
                nameof(PlaceholderText),
                typeof(string),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the placeholder text.
        /// </summary>
        public string PlaceholderText
        {
            get { return (string)GetValue(PlaceholderTextProperty); }
            set { SetValue(PlaceholderTextProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MaxLength"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register(
                nameof(MaxLength),
                typeof(int),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(0));

        /// <summary>
        /// Gets or sets the maximum length of the password.
        /// </summary>
        public int MaxLength
        {
            get { return (int)GetValue(MaxLengthProperty); }
            set { SetValue(MaxLengthProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(new CornerRadius(4)));

        /// <summary>
        /// Gets or sets the corner radius.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ShowCapsLockIndicator"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowCapsLockIndicatorProperty =
            DependencyProperty.Register(
                nameof(ShowCapsLockIndicator),
                typeof(bool),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(true, OnChromePropertyChanged));

        /// <summary>
        /// Gets or sets whether the Caps Lock indicator is shown when Caps Lock is active.
        /// </summary>
        public bool ShowCapsLockIndicator
        {
            get { return (bool)GetValue(ShowCapsLockIndicatorProperty); }
            set { SetValue(ShowCapsLockIndicatorProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ShowPasswordStrength"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowPasswordStrengthProperty =
            DependencyProperty.Register(
                nameof(ShowPasswordStrength),
                typeof(bool),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(true, OnChromePropertyChanged));

        /// <summary>
        /// Gets or sets whether the password strength meter is displayed.
        /// </summary>
        public bool ShowPasswordStrength
        {
            get { return (bool)GetValue(ShowPasswordStrengthProperty); }
            set { SetValue(ShowPasswordStrengthProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PasswordStrength"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PasswordStrengthProperty =
            DependencyProperty.Register(
                nameof(PasswordStrength),
                typeof(int),
                typeof(PasswordBox),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// Strength score from 0 (weakest) to 4 (strongest). Updated when <see cref="Password"/> changes unless overridden by binding.
        /// </summary>
        public int PasswordStrength
        {
            get { return (int)GetValue(PasswordStrengthProperty); }
            set { SetValue(PasswordStrengthProperty, value); }
        }

        private static void OnChromePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PasswordBox box = (PasswordBox)d;
            box.UpdateCapsLockIndicator();
            box.UpdateStrengthMeter();
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_passwordBox != null)
            {
                _passwordBox.PasswordChanged -= OnPasswordBoxPasswordChanged;
                _passwordBox.GotKeyboardFocus -= OnInnerKeyboardFocusChanged;
                _passwordBox.LostKeyboardFocus -= OnInnerKeyboardFocusChanged;
                _passwordBox.PreviewKeyDown -= OnInnerPreviewKeyDown;
            }

            if (_revealTextBox != null)
            {
                _revealTextBox.TextChanged -= OnRevealTextBoxTextChanged;
                _revealTextBox.GotKeyboardFocus -= OnInnerKeyboardFocusChanged;
                _revealTextBox.LostKeyboardFocus -= OnInnerKeyboardFocusChanged;
                _revealTextBox.PreviewKeyDown -= OnInnerPreviewKeyDown;
            }
            if (_revealButton != null)
            {
                _revealButton.PreviewMouseLeftButtonDown -= OnRevealButtonDown;
                _revealButton.PreviewMouseLeftButtonUp -= OnRevealButtonUp;
                _revealButton.MouseLeave -= OnRevealButtonLeave;
            }

            StopCapsPoll();

            _passwordBox = GetTemplateChild("PART_PasswordBox") as System.Windows.Controls.PasswordBox;
            _revealTextBox = GetTemplateChild("PART_RevealTextBox") as System.Windows.Controls.TextBox;
            _revealButton = GetTemplateChild("PART_RevealButton") as System.Windows.Controls.Button;

            if (_passwordBox != null)
            {
                _passwordBox.PasswordChanged += OnPasswordBoxPasswordChanged;
                _passwordBox.Password = Password ?? string.Empty;
            }
            if (_revealTextBox != null)
            {
                _revealTextBox.TextChanged += OnRevealTextBoxTextChanged;
                _revealTextBox.Text = Password ?? string.Empty;
            }
            if (_revealButton != null)
            {
                _revealButton.PreviewMouseLeftButtonDown += OnRevealButtonDown;
                _revealButton.PreviewMouseLeftButtonUp += OnRevealButtonUp;
                _revealButton.MouseLeave += OnRevealButtonLeave;
            }

            if (_passwordBox != null)
            {
                _passwordBox.GotKeyboardFocus += OnInnerKeyboardFocusChanged;
                _passwordBox.LostKeyboardFocus += OnInnerKeyboardFocusChanged;
                _passwordBox.PreviewKeyDown += OnInnerPreviewKeyDown;
            }

            if (_revealTextBox != null)
            {
                _revealTextBox.GotKeyboardFocus += OnInnerKeyboardFocusChanged;
                _revealTextBox.LostKeyboardFocus += OnInnerKeyboardFocusChanged;
                _revealTextBox.PreviewKeyDown += OnInnerPreviewKeyDown;
            }

            UpdatePasswordStrengthFromPassword();
            UpdateCapsLockIndicator();
            UpdateStrengthMeter();
        }

        private void StartCapsPoll()
        {
            if (_capsPollTimer != null)
            {
                return;
            }

            _capsPollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _capsPollTimer.Tick += _capsPollTick;
            _capsPollTimer.Start();
        }

        private void StopCapsPoll()
        {
            if (_capsPollTimer == null)
            {
                return;
            }

            _capsPollTimer.Tick -= _capsPollTick;
            _capsPollTimer.Stop();
            _capsPollTimer = null;
        }

        private void OnInnerKeyboardFocusChanged(object sender, KeyboardFocusChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(
                new Action(
                    () =>
                    {
                        UpdateCapsLockIndicator();
                        if (IsKeyboardFocusWithin)
                        {
                            StartCapsPoll();
                        }
                        else
                        {
                            StopCapsPoll();
                        }
                    }),
                DispatcherPriority.Input);
        }

        private void OnInnerPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.CapsLock)
            {
                Dispatcher.BeginInvoke(new Action(UpdateCapsLockIndicator), DispatcherPriority.Input);
            }
        }

        private void OnPasswordBoxPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingPassword)
            {
                return;
            }

            _isUpdatingPassword = true;
            try
            {
                Password = _passwordBox.Password;
                if (_revealTextBox != null)
                {
                    _revealTextBox.Text = _passwordBox.Password;
                }

                UpdatePasswordStrengthFromPassword();
                UpdateStrengthMeter();
            }
            finally
            {
                _isUpdatingPassword = false;
            }
        }

        private void OnRevealTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingPassword)
            {
                return;
            }

            _isUpdatingPassword = true;
            try
            {
                Password = _revealTextBox.Text;
                if (_passwordBox != null)
                {
                    _passwordBox.Password = _revealTextBox.Text;
                }

                UpdatePasswordStrengthFromPassword();
                UpdateStrengthMeter();
            }
            finally
            {
                _isUpdatingPassword = false;
            }
        }

        private void UpdatePasswordStrengthFromPassword()
        {
            string pwd = Password ?? string.Empty;
            PasswordStrength = ComputePasswordStrength(pwd);
        }

        private static int ComputePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return 0;
            }

            int score = 0;
            if (password.Length >= 6)
            {
                score++;
            }

            if (password.Length >= 10)
            {
                score++;
            }

            if (HasLowercasePasswordCharacter(password) && HasUppercasePasswordCharacter(password))
            {
                score++;
            }

            if (HasDigitPasswordCharacter(password))
            {
                score++;
            }

            if (HasSymbolPasswordCharacter(password))
            {
                score++;
            }

            return Math.Min(4, score);
        }

        private static bool HasLowercasePasswordCharacter(string password)
        {
#if NET7_0_OR_GREATER
            return LowercasePasswordRegex().IsMatch(password);
#else
            return LowercasePasswordRegexFallback.IsMatch(password);
#endif
        }

        private static bool HasUppercasePasswordCharacter(string password)
        {
#if NET7_0_OR_GREATER
            return UppercasePasswordRegex().IsMatch(password);
#else
            return UppercasePasswordRegexFallback.IsMatch(password);
#endif
        }

        private static bool HasDigitPasswordCharacter(string password)
        {
#if NET7_0_OR_GREATER
            return DigitPasswordRegex().IsMatch(password);
#else
            return DigitPasswordRegexFallback.IsMatch(password);
#endif
        }

        private static bool HasSymbolPasswordCharacter(string password)
        {
#if NET7_0_OR_GREATER
            return SymbolPasswordRegex().IsMatch(password);
#else
            return SymbolPasswordRegexFallback.IsMatch(password);
#endif
        }

#if NET7_0_OR_GREATER
        [GeneratedRegex(LowercasePasswordPattern, RegexOptions.CultureInvariant)]
        private static partial Regex LowercasePasswordRegex();

        [GeneratedRegex(UppercasePasswordPattern, RegexOptions.CultureInvariant)]
        private static partial Regex UppercasePasswordRegex();

        [GeneratedRegex(DigitPasswordPattern, RegexOptions.CultureInvariant)]
        private static partial Regex DigitPasswordRegex();

        [GeneratedRegex(SymbolPasswordPattern, RegexOptions.CultureInvariant)]
        private static partial Regex SymbolPasswordRegex();
#endif

        private void UpdateCapsLockIndicator()
        {
            UIElement? el = GetTemplateChild("PART_CapsLockIndicator") as UIElement;
            if (el == null)
            {
                return;
            }

            bool capsOn = Keyboard.IsKeyToggled(Key.CapsLock);
            bool show = ShowCapsLockIndicator && IsKeyboardFocusWithin && capsOn;
            el.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateStrengthMeter()
        {
            string brushKey;
            if (PasswordStrength <= 1)
            {
                brushKey = "SystemFillColorCriticalBrush";
            }
            else if (PasswordStrength == 2)
            {
                brushKey = "SystemFillColorCautionBrush";
            }
            else
            {
                brushKey = "SystemFillColorSuccessBrush";
            }

            for (int i = 0; i < 4; i++)
            {
                System.Windows.Controls.Border? segment = GetTemplateChild("PART_StrengthSegment" + i) as System.Windows.Controls.Border;
                if (segment == null)
                {
                    continue;
                }

                if (!ShowPasswordStrength)
                {
                    segment.Visibility = Visibility.Collapsed;
                    continue;
                }

                segment.Visibility = Visibility.Visible;
                bool active = PasswordStrength > i;
                segment.Opacity = active ? 1.0 : 0.25;
                segment.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, brushKey);
            }

            UIElement? container = GetTemplateChild("PART_StrengthMeter") as UIElement;
            if (container != null)
            {
                container.Visibility = ShowPasswordStrength ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void OnRevealButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            IsPasswordRevealed = true;
        }

        private void OnRevealButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            IsPasswordRevealed = false;
        }

        private void OnRevealButtonLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            IsPasswordRevealed = false;
        }

        /// <summary>
        /// Selects all text in the password field.
        /// </summary>
        public void SelectAll()
        {
            if (IsPasswordRevealed && _revealTextBox != null)
            {
                _revealTextBox.SelectAll();
            }
            else if (_passwordBox != null)
            {
                _passwordBox.SelectAll();
            }
        }
    }
}
