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
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>Gallery page demonstrating cohesive form patterns.</summary>
    public partial class GalleryFormsPage : UserControl
    {
        // Minimal email pattern — good enough for a UI demo.
        private static readonly Regex EmailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Initializes a new instance of <see cref="GalleryFormsPage"/>.</summary>
        public GalleryFormsPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            // Wire password DP change — Fluence PasswordBox has no public PasswordChanged routed event.
            if (PasswordBoxControl != null)
            {
                DependencyPropertyDescriptor
                    .FromProperty(Controls.PasswordBox.PasswordProperty, typeof(Controls.PasswordBox))
                    .AddValueChanged(PasswordBoxControl, Password_Changed);
            }
        }

        // ── Validation helpers ──────────────────────────────────────────

        private void NameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSubmitState();
        }

        private void EmailBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (EmailBox == null || EmailValidationLabel == null)
                return;

            var text = EmailBox.Text ?? string.Empty;
            var valid = string.IsNullOrEmpty(text) || EmailRegex.IsMatch(text);
            EmailValidationLabel.Visibility = valid ? Visibility.Collapsed : Visibility.Visible;
            UpdateSubmitState();
        }

        private void Password_Changed(object sender, EventArgs e)
        {
            if (PasswordBoxControl == null || StrengthBar == null || StrengthLabel == null)
                return;

            var pwd = PasswordBoxControl.Password ?? string.Empty;
            int score = 0;
            if (pwd.Length >= 8) score++;
            if (pwd.Length >= 12) score++;
            if (Regex.IsMatch(pwd, @"\d")) score++;
            if (Regex.IsMatch(pwd, @"[^a-zA-Z0-9]")) score++;

            double pct = score / 4.0;
            // StrengthBar.Width is relative to card width; set via parent ActualWidth
            StrengthBar.Width = StrengthBar.ActualWidth > 0
                ? 0   // updated on layout pass; set via RenderSize in layout callback
                : 0;

            // Simpler: use a multiplier tag instead
            StrengthBar.Tag = pct;
            StrengthBar.UpdateLayout();

            string label;
            string color;
            if (score == 0) { label = "—"; color = "#999999"; }
            else if (score <= 1) { label = "Weak"; color = "#C42B1C"; }
            else if (score <= 2) { label = "Fair"; color = "#F7630C"; }
            else if (score <= 3) { label = "Good"; color = "#0099BC"; }
            else { label = "Strong"; color = "#107C10"; }

            StrengthLabel.Text = string.Format("Strength: {0}", label);
            try
            {
                var bc = new System.Windows.Media.BrushConverter();
                var brush = bc.ConvertFromString(color) as System.Windows.Media.Brush;
                if (brush != null)
                    StrengthLabel.Foreground = brush;
            }
            catch { /* ignore */ }

            UpdateSubmitState();
        }

        private void CountryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSubmitState();
        }

        private void Terms_Changed(object sender, RoutedEventArgs e)
        {
            UpdateSubmitState();
        }

        private void UpdateSubmitState()
        {
            if (SubmitButton == null) return;

            bool nameOk = !string.IsNullOrWhiteSpace(FirstNameBox?.Text)
                       && !string.IsNullOrWhiteSpace(LastNameBox?.Text);
            bool emailOk = !string.IsNullOrWhiteSpace(EmailBox?.Text)
                        && EmailRegex.IsMatch(EmailBox.Text ?? string.Empty);
            bool pwdOk = (PasswordBoxControl?.Password ?? string.Empty).Length >= 8;
            bool termsOk = TermsCheckBox?.IsChecked == true;

            SubmitButton.IsEnabled = nameOk && emailOk && pwdOk && termsOk;
        }

        // ── Actions ─────────────────────────────────────────────────────

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmationBar != null)
                ConfirmationBar.IsOpen = true;

            if (SubmitButton != null)
                SubmitButton.IsEnabled = false;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (FirstNameBox != null) FirstNameBox.Text = string.Empty;
            if (LastNameBox != null) LastNameBox.Text = string.Empty;
            if (EmailBox != null) EmailBox.Text = string.Empty;
            if (PasswordBoxControl != null) PasswordBoxControl.Password = string.Empty;
            if (TermsCheckBox != null) TermsCheckBox.IsChecked = false;
            if (MarketingCheckBox != null) MarketingCheckBox.IsChecked = false;
            if (ConfirmationBar != null) ConfirmationBar.IsOpen = false;
            UpdateSubmitState();
        }

        private void QuantityBox_ValueChanged(object sender, Controls.NumberBoxValueChangedEventArgs e)
        {
            if (QuantityLabel != null)
                QuantityLabel.Text = string.Format("Value: {0:0}", e.NewValue);
        }
    }
}
