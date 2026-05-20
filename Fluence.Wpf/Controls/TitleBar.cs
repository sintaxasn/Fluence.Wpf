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
using System.Windows.Controls;
using System.Windows.Input;
using WpfButton = System.Windows.Controls.Button;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A Fluent Design shell title bar with navigation buttons, icon, title text, and header/content slots.
    /// </summary>
    [TemplatePart(Name = PART_BackButton, Type = typeof(WpfButton))]
    [TemplatePart(Name = PART_PaneToggleButton, Type = typeof(WpfButton))]
    [TemplatePart(Name = "PART_IconPresenter", Type = typeof(ContentPresenter))]
    [TemplatePart(Name = "PART_TitleText", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_SubtitleText", Type = typeof(TextBlock))]
    [TemplatePart(Name = "PART_LeftHeaderPresenter", Type = typeof(ContentPresenter))]
    [TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
    [TemplatePart(Name = "PART_RightHeaderPresenter", Type = typeof(ContentPresenter))]
    public class TitleBar : ContentControl
    {
        // Template part names.
        private const string PART_BackButton = "PART_BackButton";
        private const string PART_PaneToggleButton = "PART_PaneToggleButton";

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(TitleBar),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="Subtitle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(TitleBar),
                new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="Icon"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(object), typeof(TitleBar),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="IsBackButtonVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsBackButtonVisibleProperty =
            DependencyProperty.Register(nameof(IsBackButtonVisible), typeof(bool), typeof(TitleBar),
                new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="IsPaneToggleButtonVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPaneToggleButtonVisibleProperty =
            DependencyProperty.Register(nameof(IsPaneToggleButtonVisible), typeof(bool), typeof(TitleBar),
                new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="IsCompact"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsCompactProperty =
            DependencyProperty.Register(nameof(IsCompact), typeof(bool), typeof(TitleBar),
                new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="LeftHeader"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LeftHeaderProperty =
            DependencyProperty.Register(nameof(LeftHeader), typeof(object), typeof(TitleBar),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="RightHeader"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RightHeaderProperty =
            DependencyProperty.Register(nameof(RightHeader), typeof(object), typeof(TitleBar),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="CustomContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CustomContentProperty =
            DependencyProperty.Register(nameof(CustomContent), typeof(object), typeof(TitleBar),
                new FrameworkPropertyMetadata(null, OnCustomContentChanged));

        /// <summary>
        /// Identifies the <see cref="BackCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BackCommandProperty =
            DependencyProperty.Register(nameof(BackCommand), typeof(ICommand), typeof(TitleBar),
                new FrameworkPropertyMetadata(null, OnBackCommandChanged));

        /// <summary>
        /// Identifies the <see cref="BackCommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BackCommandParameterProperty =
            DependencyProperty.Register(nameof(BackCommandParameter), typeof(object), typeof(TitleBar),
                new FrameworkPropertyMetadata(null, OnBackCommandParameterChanged));

        /// <summary>
        /// Identifies the <see cref="PaneToggleCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PaneToggleCommandProperty =
            DependencyProperty.Register(nameof(PaneToggleCommand), typeof(ICommand), typeof(TitleBar),
                new FrameworkPropertyMetadata(null, OnPaneToggleCommandChanged));

        /// <summary>
        /// Identifies the <see cref="PaneToggleCommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PaneToggleCommandParameterProperty =
            DependencyProperty.Register(nameof(PaneToggleCommandParameter), typeof(object), typeof(TitleBar),
                new FrameworkPropertyMetadata(null, OnPaneToggleCommandParameterChanged));

        /// <summary>
        /// Initializes static members of the TitleBar class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the TitleBar control uses its custom style by
        /// default. It is called automatically before any static members are accessed or any instances are
        /// created.</remarks>
        static TitleBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TitleBar),
                new FrameworkPropertyMetadata(typeof(TitleBar)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TitleBar"/> class.
        /// </summary>
        public TitleBar()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        /// <summary>
        /// Occurs when the back button is invoked after command execution has been processed.
        /// </summary>
        public event EventHandler? BackRequested;

        /// <summary>
        /// Occurs when the pane toggle button is invoked after command execution has been processed.
        /// </summary>
        public event EventHandler? PaneToggleRequested;

        /// <summary>
        /// Gets or sets the title text.
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Gets or sets the subtitle text.
        /// </summary>
        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }

        /// <summary>
        /// Gets or sets the title bar icon content.
        /// </summary>
        public object Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the back button is visible.
        /// </summary>
        public bool IsBackButtonVisible
        {
            get => (bool)GetValue(IsBackButtonVisibleProperty);
            set => SetValue(IsBackButtonVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the pane toggle button is visible.
        /// </summary>
        public bool IsPaneToggleButtonVisible
        {
            get => (bool)GetValue(IsPaneToggleButtonVisibleProperty);
            set => SetValue(IsPaneToggleButtonVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the title bar uses compact height (32px vs 48px).
        /// </summary>
        public bool IsCompact
        {
            get => (bool)GetValue(IsCompactProperty);
            set => SetValue(IsCompactProperty, value);
        }

        /// <summary>
        /// Gets or sets content displayed before the icon and title text.
        /// </summary>
        public object LeftHeader
        {
            get => GetValue(LeftHeaderProperty);
            set => SetValue(LeftHeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets content displayed after the central content slot.
        /// </summary>
        public object RightHeader
        {
            get => GetValue(RightHeaderProperty);
            set => SetValue(RightHeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets custom content displayed in the central title bar slot.
        /// </summary>
        public object CustomContent
        {
            get => GetValue(CustomContentProperty);
            set => SetValue(CustomContentProperty, value);
        }

        /// <summary>
        /// Gets or sets the command invoked when the back button is clicked.
        /// </summary>
        public ICommand BackCommand
        {
            get => (ICommand)GetValue(BackCommandProperty);
            set => SetValue(BackCommandProperty, value);
        }

        /// <summary>
        /// Gets or sets the parameter for <see cref="BackCommand"/>.
        /// </summary>
        public object BackCommandParameter
        {
            get => GetValue(BackCommandParameterProperty);
            set => SetValue(BackCommandParameterProperty, value);
        }

        /// <summary>
        /// Gets or sets the command invoked when the pane toggle button is clicked.
        /// </summary>
        public ICommand PaneToggleCommand
        {
            get => (ICommand)GetValue(PaneToggleCommandProperty);
            set => SetValue(PaneToggleCommandProperty, value);
        }

        /// <summary>
        /// Gets or sets the parameter for <see cref="PaneToggleCommand"/>.
        /// </summary>
        public object PaneToggleCommandParameter
        {
            get => GetValue(PaneToggleCommandParameterProperty);
            set => SetValue(PaneToggleCommandParameterProperty, value);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            _backButton?.Click -= OnBackButtonClick;
            _paneToggleButton?.Click -= OnPaneToggleButtonClick;
            base.OnApplyTemplate();
            _backButton = GetTemplateChild(PART_BackButton) as WpfButton;
            _paneToggleButton = GetTemplateChild(PART_PaneToggleButton) as WpfButton;
            _backButton?.Click += OnBackButtonClick;
            _paneToggleButton?.Click += OnPaneToggleButtonClick;
            UpdateBackButtonCommandState();
            UpdatePaneToggleButtonCommandState();
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == IsEnabledProperty)
            {
                UpdateBackButtonCommandState();
                UpdatePaneToggleButtonCommandState();
            }
        }

        /// <summary>
        /// Raises the <see cref="BackRequested"/> event.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected virtual void OnBackRequested(EventArgs e)
        {
            BackRequested?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref="PaneToggleRequested"/> event.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected virtual void OnPaneToggleRequested(EventArgs e)
        {
            PaneToggleRequested?.Invoke(this, e);
        }

        private static void OnCustomContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TitleBar titleBar = (TitleBar)d;
            if (titleBar.Content is null || Equals(titleBar.Content, e.OldValue))
            {
                titleBar.Content = e.NewValue;
            }
        }

        private static void OnBackCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TitleBar titleBar = (TitleBar)d;
            titleBar.UnsubscribeBackCommand(e.OldValue as ICommand);
            if (titleBar.IsLoaded)
            {
                titleBar.SubscribeBackCommand(e.NewValue as ICommand);
            }
            titleBar.UpdateBackButtonCommandState();
        }

        private static void OnBackCommandParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TitleBar)d).UpdateBackButtonCommandState();
        }

        private static void OnPaneToggleCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TitleBar titleBar = (TitleBar)d;
            titleBar.UnsubscribePaneToggleCommand(e.OldValue as ICommand);
            if (titleBar.IsLoaded)
            {
                titleBar.SubscribePaneToggleCommand(e.NewValue as ICommand);
            }
            titleBar.UpdatePaneToggleButtonCommandState();
        }

        private static void OnPaneToggleCommandParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TitleBar)d).UpdatePaneToggleButtonCommandState();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SubscribeBackCommand(BackCommand);
            SubscribePaneToggleCommand(PaneToggleCommand);
            UpdateBackButtonCommandState();
            UpdatePaneToggleButtonCommandState();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeBackCommand(BackCommand);
            UnsubscribePaneToggleCommand(PaneToggleCommand);
        }

        private void OnBackCommandCanExecuteChanged(object? sender, EventArgs e)
        {
            UpdateBackButtonCommandState();
        }

        private void OnPaneToggleCommandCanExecuteChanged(object? sender, EventArgs e)
        {
            UpdatePaneToggleButtonCommandState();
        }

        private void OnBackButtonClick(object sender, RoutedEventArgs e)
        {
            if (TryExecuteCommand(BackCommand, BackCommandParameter))
            {
                OnBackRequested(EventArgs.Empty);
            }
            UpdateBackButtonCommandState();
        }

        private void OnPaneToggleButtonClick(object sender, RoutedEventArgs e)
        {
            if (TryExecuteCommand(PaneToggleCommand, PaneToggleCommandParameter))
            {
                OnPaneToggleRequested(EventArgs.Empty);
            }
            UpdatePaneToggleButtonCommandState();
        }

        private void UpdateBackButtonCommandState()
        {
            _ = _backButton?.IsEnabled = IsEnabled && CanExecuteCommand(BackCommand, BackCommandParameter);
        }

        private void UpdatePaneToggleButtonCommandState()
        {
            _ = _paneToggleButton?.IsEnabled = IsEnabled && CanExecuteCommand(PaneToggleCommand, PaneToggleCommandParameter);
        }

        private static bool TryExecuteCommand(ICommand command, object parameter)
        {
            if (command is null)
            {
                return true;
            }
            if (!command.CanExecute(parameter))
            {
                return false;
            }
            command.Execute(parameter);
            return true;
        }

        private static bool CanExecuteCommand(ICommand command, object parameter)
        {
            return command is null || command.CanExecute(parameter);
        }

        private static void SubscribeCommand(ICommand? command, EventHandler handler)
        {
            command?.CanExecuteChanged += handler;
        }

        private static void UnsubscribeCommand(ICommand? command, EventHandler handler)
        {
            command?.CanExecuteChanged -= handler;
        }

        private void SubscribeBackCommand(ICommand? command)
        {
            if (command is null || _isBackCommandSubscribed)
            {
                return;
            }
            SubscribeCommand(command, OnBackCommandCanExecuteChanged);
            _isBackCommandSubscribed = true;
        }

        private void UnsubscribeBackCommand(ICommand? command)
        {
            if (!_isBackCommandSubscribed)
            {
                return;
            }
            UnsubscribeCommand(command, OnBackCommandCanExecuteChanged);
            _isBackCommandSubscribed = false;
        }

        private void SubscribePaneToggleCommand(ICommand? command)
        {
            if (command is null || _isPaneToggleCommandSubscribed)
            {
                return;
            }
            SubscribeCommand(command, OnPaneToggleCommandCanExecuteChanged);
            _isPaneToggleCommandSubscribed = true;
        }

        private void UnsubscribePaneToggleCommand(ICommand? command)
        {
            if (!_isPaneToggleCommandSubscribed)
            {
                return;
            }
            UnsubscribeCommand(command, OnPaneToggleCommandCanExecuteChanged);
            _isPaneToggleCommandSubscribed = false;
        }

        /// <summary>
        /// Represents the back button control in the WPF user interface.
        /// </summary>
        private WpfButton? _backButton;

        /// <summary>
        /// Represents the toggle button control used to show or hide a pane in the WPF user interface.
        /// </summary>
        private WpfButton? _paneToggleButton;

        private bool _isBackCommandSubscribed;

        private bool _isPaneToggleCommandSubscribed;
    }
}
