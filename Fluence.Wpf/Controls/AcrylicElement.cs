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
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf.Helpers;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Shows an in-app acrylic backdrop behind unprocessed content.
    /// </summary>
    [TemplatePart(Name = PART_Backdrop, Type = typeof(System.Windows.Controls.Border))]
    public sealed class AcrylicElement : ContentControl
    {
        private const string PART_Backdrop = "PART_Backdrop";
        private static readonly DependencyPropertyKey IsUsingFallbackPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(IsUsingFallback), typeof(bool), typeof(AcrylicElement), new PropertyMetadata(defaultValue: true));
        private readonly AcrylicMaterialController _controller;
        private System.Windows.Controls.Border? _backdrop;
        private AcrylicFallbackReason _lastReason;

        static AcrylicElement()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AcrylicElement),
                new FrameworkPropertyMetadata(typeof(AcrylicElement)));
        }

        /// <summary>
        /// Initializes an acrylic content host.
        /// </summary>
        public AcrylicElement()
        {
            _controller = new AcrylicMaterialController(this, () => BackdropSource, Recipe, UpdateFallbackState);
        }

        /// <summary>
        /// Identifies the <see cref="BackdropSource"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BackdropSourceProperty = DependencyProperty.Register(
            nameof(BackdropSource), typeof(FrameworkElement), typeof(AcrylicElement),
            new PropertyMetadata(defaultValue: null, OnBackdropSourceChanged));

        /// <summary>
        /// Identifies the <see cref="Recipe"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RecipeProperty = DependencyProperty.Register(
            nameof(Recipe), typeof(AcrylicMaterialRecipe), typeof(AcrylicElement),
            new PropertyMetadata(defaultValue: null, OnRecipeChanged));

        /// <summary>
        /// Identifies the read-only <see cref="IsUsingFallback"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsUsingFallbackProperty = IsUsingFallbackPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets or sets a separate, same-window backdrop source.
        /// </summary>
        public FrameworkElement? BackdropSource
        {
            get => (FrameworkElement?)GetValue(BackdropSourceProperty);
            set => SetValue(BackdropSourceProperty, value);
        }

        /// <summary>
        /// Gets or sets material values; null uses class defaults.
        /// </summary>
        public AcrylicMaterialRecipe? Recipe
        {
            get => (AcrylicMaterialRecipe?)GetValue(RecipeProperty);
            set => SetValue(RecipeProperty, value);
        }

        /// <summary>
        /// Gets whether the host currently paints an opaque fallback.
        /// </summary>
        public bool IsUsingFallback => (bool)GetValue(IsUsingFallbackProperty);

        internal AcrylicFallbackReason FallbackReason => _controller.Reason;

        internal event EventHandler? FallbackReasonChanged;

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            Dispatcher.VerifyAccess();
            if (ReferenceEquals(_backdrop?.Background, _controller.Output))
            {
                _backdrop?.ClearValue(System.Windows.Controls.Border.BackgroundProperty);
            }

            base.OnApplyTemplate();
            _backdrop = GetTemplateChild(PART_Backdrop) as System.Windows.Controls.Border;
            _backdrop?.SetValue(System.Windows.Controls.Border.BackgroundProperty, _controller.Output);

            _controller.InvalidateSource();
            UpdateFallbackState();
        }

        private static void OnBackdropSourceChanged(DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs args)
        {
            _ = args;
            AcrylicElement element = (AcrylicElement)dependencyObject;
            element.Dispatcher.VerifyAccess();
            element._controller?.InvalidateSource();
        }

        private static void OnRecipeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            AcrylicElement element = (AcrylicElement)dependencyObject;
            element.Dispatcher.VerifyAccess();
            element._controller?.SetRecipe(args.NewValue as AcrylicMaterialRecipe);
        }

        private void UpdateFallbackState()
        {
            SetValue(IsUsingFallbackPropertyKey, _controller.IsUsingFallback);
            AcrylicFallbackReason next = _controller.Reason;
            if (_lastReason != next)
            {
                _lastReason = next;
                FallbackReasonChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
