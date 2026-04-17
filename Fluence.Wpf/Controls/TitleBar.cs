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
    /// A Fluent Design title bar control with optional back button, icon, title, and custom content area.
    /// </summary>
    public class TitleBar : Control
    {
        /// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(TitleBar),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(object), typeof(TitleBar),
                new FrameworkPropertyMetadata(null));

        /// <summary>Identifies the <see cref="IsBackButtonVisible"/> dependency property.</summary>
        public static readonly DependencyProperty IsBackButtonVisibleProperty =
            DependencyProperty.Register(nameof(IsBackButtonVisible), typeof(bool), typeof(TitleBar),
                new FrameworkPropertyMetadata(false));

        /// <summary>Identifies the <see cref="IsCompact"/> dependency property.</summary>
        public static readonly DependencyProperty IsCompactProperty =
            DependencyProperty.Register(nameof(IsCompact), typeof(bool), typeof(TitleBar),
                new FrameworkPropertyMetadata(false));

        /// <summary>Identifies the <see cref="CustomContent"/> dependency property.</summary>
        public static readonly DependencyProperty CustomContentProperty =
            DependencyProperty.Register(nameof(CustomContent), typeof(object), typeof(TitleBar),
                new FrameworkPropertyMetadata(null));

        /// <summary>Identifies the <see cref="BackCommand"/> dependency property.</summary>
        public static readonly DependencyProperty BackCommandProperty =
            DependencyProperty.Register(nameof(BackCommand), typeof(ICommand), typeof(TitleBar),
                new FrameworkPropertyMetadata(null));

        /// <summary>Identifies the <see cref="BackCommandParameter"/> dependency property.</summary>
        public static readonly DependencyProperty BackCommandParameterProperty =
            DependencyProperty.Register(nameof(BackCommandParameter), typeof(object), typeof(TitleBar),
                new FrameworkPropertyMetadata(null));

        static TitleBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TitleBar),
                new FrameworkPropertyMetadata(typeof(TitleBar)));
        }

        /// <summary>Gets or sets the title text.</summary>
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>Gets or sets the title bar icon content.</summary>
        public object Icon
        {
            get { return GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        /// <summary>Gets or sets whether the back button is visible.</summary>
        public bool IsBackButtonVisible
        {
            get { return (bool)GetValue(IsBackButtonVisibleProperty); }
            set { SetValue(IsBackButtonVisibleProperty, value); }
        }

        /// <summary>Gets or sets whether the title bar uses compact height (32px vs 48px).</summary>
        public bool IsCompact
        {
            get { return (bool)GetValue(IsCompactProperty); }
            set { SetValue(IsCompactProperty, value); }
        }

        /// <summary>Gets or sets custom content displayed in the title bar drag region.</summary>
        public object CustomContent
        {
            get { return GetValue(CustomContentProperty); }
            set { SetValue(CustomContentProperty, value); }
        }

        /// <summary>Gets or sets the command invoked when the back button is clicked.</summary>
        public ICommand BackCommand
        {
            get { return (ICommand)GetValue(BackCommandProperty); }
            set { SetValue(BackCommandProperty, value); }
        }

        /// <summary>Gets or sets the parameter for <see cref="BackCommand"/>.</summary>
        public object BackCommandParameter
        {
            get { return GetValue(BackCommandParameterProperty); }
            set { SetValue(BackCommandParameterProperty, value); }
        }
    }
}
