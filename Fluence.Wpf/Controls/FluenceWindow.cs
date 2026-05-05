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
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using Fluence.Wpf.Native;

namespace Fluence.Wpf.Controls
{
    internal static class FluenceWindowTemplateParts
    {
        internal const string PART_MinimizeButton = "PART_MinimizeButton";
        internal const string PART_MaximizeButton = "PART_MaximizeButton";
        internal const string PART_RestoreButton = "PART_RestoreButton";
        internal const string PART_CloseButton = "PART_CloseButton";
    }

    /// <summary>
    /// A window with Windows 11 Fluent Design chrome, backdrop support, and custom caption buttons.
    /// </summary>
    [TemplatePart(Name = FluenceWindowTemplateParts.PART_MinimizeButton, Type = typeof(System.Windows.Controls.Button))]
    [TemplatePart(Name = FluenceWindowTemplateParts.PART_MaximizeButton, Type = typeof(System.Windows.Controls.Button))]
    [TemplatePart(Name = FluenceWindowTemplateParts.PART_RestoreButton, Type = typeof(System.Windows.Controls.Button))]
    [TemplatePart(Name = FluenceWindowTemplateParts.PART_CloseButton, Type = typeof(System.Windows.Controls.Button))]
    public class FluenceWindow : Window
    {
        private const double DefaultTitleBarHeight = 68d;
        private const string PART_MinimizeButton = FluenceWindowTemplateParts.PART_MinimizeButton;
        private const string PART_MaximizeButton = FluenceWindowTemplateParts.PART_MaximizeButton;
        private const string PART_RestoreButton = FluenceWindowTemplateParts.PART_RestoreButton;
        private const string PART_CloseButton = FluenceWindowTemplateParts.PART_CloseButton;

        private System.Windows.Controls.Button _minimizeButton;
        private System.Windows.Controls.Button _maximizeButton;
        private System.Windows.Controls.Button _restoreButton;
        private System.Windows.Controls.Button _closeButton;
        private HwndSource _hwndSource;
        private System.Windows.Controls.Button _snapHoveredButton;

        /// <summary>
        /// Converts a value to <c>true</c> when it is not null; used by caption button visibility bindings.
        /// </summary>
        public static readonly IValueConverter IsNotNullConverter = new IsNotNullValueConverter();

        private class IsNotNullValueConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return value != null;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="SystemBackdropType"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SystemBackdropTypeProperty =
            DependencyProperty.Register(
                "SystemBackdropType",
                typeof(BackdropType),
                typeof(FluenceWindow),
                new PropertyMetadata(BackdropType.Auto, OnSystemBackdropTypeChanged));

        /// <summary>
        /// Identifies the <see cref="CornerStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerStyleProperty =
            DependencyProperty.Register(
                "CornerStyle",
                typeof(CornerPreference),
                typeof(FluenceWindow),
                new PropertyMetadata(CornerPreference.Round, OnCornerStyleChanged));

        /// <summary>
        /// Identifies the <see cref="MarginMaximized"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MarginMaximizedProperty =
            DependencyProperty.Register(
                "MarginMaximized",
                typeof(Thickness),
                typeof(FluenceWindow),
                new PropertyMetadata(new Thickness(0)));

        /// <summary>
        /// Identifies the <see cref="ExtendsContentIntoTitleBar"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExtendsContentIntoTitleBarProperty =
            DependencyProperty.Register(
                nameof(ExtendsContentIntoTitleBar),
                typeof(bool),
                typeof(FluenceWindow),
                new PropertyMetadata(false, OnExtendsContentIntoTitleBarChanged));

        /// <summary>
        /// Identifies the <see cref="TitleBar"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleBarProperty =
            DependencyProperty.Register(
                nameof(TitleBar),
                typeof(UIElement),
                typeof(FluenceWindow),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="TitleBarHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleBarHeightProperty =
            DependencyProperty.Register(
                nameof(TitleBarHeight),
                typeof(double),
                typeof(FluenceWindow),
                new PropertyMetadata(DefaultTitleBarHeight, OnTitleBarHeightChanged));

        /// <summary>
        /// Identifies the <see cref="ShowIcon"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowIconProperty =
            DependencyProperty.Register(
                nameof(ShowIcon),
                typeof(bool),
                typeof(FluenceWindow),
                new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="ShowTitle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowTitleProperty =
            DependencyProperty.Register(
                nameof(ShowTitle),
                typeof(bool),
                typeof(FluenceWindow),
                new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="IsMinimizeButtonVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsMinimizeButtonVisibleProperty =
            DependencyProperty.Register(
                nameof(IsMinimizeButtonVisible),
                typeof(Visibility),
                typeof(FluenceWindow),
                new PropertyMetadata(Visibility.Visible, OnCaptionButtonChromeOverrideChanged));

        /// <summary>
        /// Identifies the <see cref="IsMaximizeButtonVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsMaximizeButtonVisibleProperty =
            DependencyProperty.Register(
                nameof(IsMaximizeButtonVisible),
                typeof(Visibility),
                typeof(FluenceWindow),
                new PropertyMetadata(Visibility.Visible, OnCaptionButtonChromeOverrideChanged));

        /// <summary>
        /// Identifies the <see cref="IsCloseButtonVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsCloseButtonVisibleProperty =
            DependencyProperty.Register(
                nameof(IsCloseButtonVisible),
                typeof(Visibility),
                typeof(FluenceWindow),
                new PropertyMetadata(Visibility.Visible, OnCaptionButtonChromeOverrideChanged));

        /// <summary>
        /// Identifies the <see cref="MinimizeButtonVisibility"/> dependency property.
        /// </summary>
        [Obsolete("Use IsMinimizeButtonVisibleProperty instead.")]
        public static readonly DependencyProperty MinimizeButtonVisibilityProperty = IsMinimizeButtonVisibleProperty;

        /// <summary>
        /// Identifies the <see cref="MaximizeButtonVisibility"/> dependency property.
        /// </summary>
        [Obsolete("Use IsMaximizeButtonVisibleProperty instead.")]
        public static readonly DependencyProperty MaximizeButtonVisibilityProperty = IsMaximizeButtonVisibleProperty;

        /// <summary>
        /// Identifies the <see cref="CloseButtonVisibility"/> dependency property.
        /// </summary>
        [Obsolete("Use IsCloseButtonVisibleProperty instead.")]
        public static readonly DependencyProperty CloseButtonVisibilityProperty = IsCloseButtonVisibleProperty;

        /// <summary>
        /// Identifies the <see cref="IsMinimizable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsMinimizableProperty =
            DependencyProperty.Register(
                nameof(IsMinimizable),
                typeof(bool),
                typeof(FluenceWindow),
                new PropertyMetadata(true, OnCaptionButtonChromeOverrideChanged));

        /// <summary>
        /// Identifies the <see cref="IsMaximizable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsMaximizableProperty =
            DependencyProperty.Register(
                nameof(IsMaximizable),
                typeof(bool),
                typeof(FluenceWindow),
                new PropertyMetadata(true, OnCaptionButtonChromeOverrideChanged));

        /// <summary>
        /// Identifies the <see cref="IsClosable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsClosableProperty =
            DependencyProperty.Register(
                nameof(IsClosable),
                typeof(bool),
                typeof(FluenceWindow),
                new PropertyMetadata(true, OnCaptionButtonChromeOverrideChanged));

        /// <summary>
        /// Identifies the <see cref="IsMoveable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsMoveableProperty =
            DependencyProperty.Register(
                nameof(IsMoveable),
                typeof(bool),
                typeof(FluenceWindow),
                new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="CanMove"/> dependency property.
        /// </summary>
        [Obsolete("Use IsMoveableProperty instead.")]
        public static readonly DependencyProperty CanMoveProperty = IsMoveableProperty;


        /// <summary>
        /// Identifies the <see cref="HasShadow"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HasShadowProperty =
            DependencyProperty.Register(
                nameof(HasShadow),
                typeof(bool),
                typeof(FluenceWindow),
                new PropertyMetadata(true, OnHasShadowChanged));

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the requested system backdrop (Mica, Acrylic, Tabbed, or none).
        /// </summary>
        public BackdropType SystemBackdropType
        {
            get => (BackdropType)GetValue(SystemBackdropTypeProperty);
            set => SetValue(SystemBackdropTypeProperty, value);
        }

        /// <summary>
        /// Gets or sets the preferred window corner rounding policy for DWM.
        /// </summary>
        public CornerPreference CornerStyle
        {
            get => (CornerPreference)GetValue(CornerStyleProperty);
            set => SetValue(CornerStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets extra margin applied when the window is maximized to avoid overlap with the work area.
        /// </summary>
        public Thickness MarginMaximized
        {
            get => (Thickness)GetValue(MarginMaximizedProperty);
            set => SetValue(MarginMaximizedProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the window content extends into the title bar area,
        /// replacing the system title bar with a custom one rendered by the control template.
        /// </summary>
        public bool ExtendsContentIntoTitleBar
        {
            get => (bool)GetValue(ExtendsContentIntoTitleBarProperty);
            set => SetValue(ExtendsContentIntoTitleBarProperty, value);
        }

        /// <summary>
        /// Gets or sets custom content displayed in the title bar region.
        /// When null and <see cref="ExtendsContentIntoTitleBar"/> is true, a default title bar with icon and title is shown.
        /// </summary>
        public UIElement TitleBar
        {
            get => (UIElement)GetValue(TitleBarProperty);
            set => SetValue(TitleBarProperty, value);
        }

        /// <summary>
        /// Gets or sets the height of the title bar region. Standard = 48, compact = 32.
        /// </summary>
        public double TitleBarHeight
        {
            get => (double)GetValue(TitleBarHeightProperty);
            set => SetValue(TitleBarHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the window icon is shown in the title bar.
        /// </summary>
        public bool ShowIcon
        {
            get => (bool)GetValue(ShowIconProperty);
            set => SetValue(ShowIconProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the window title text is shown in the title bar.
        /// </summary>
        public bool ShowTitle
        {
            get => (bool)GetValue(ShowTitleProperty);
            set => SetValue(ShowTitleProperty, value);
        }

        /// <summary>
        /// Gets or sets the visibility of the minimize button.
        /// </summary>
        public Visibility IsMinimizeButtonVisible
        {
            get => (Visibility)GetValue(IsMinimizeButtonVisibleProperty);
            set => SetValue(IsMinimizeButtonVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets the visibility of the maximize button.
        /// </summary>
        public Visibility IsMaximizeButtonVisible
        {
            get => (Visibility)GetValue(IsMaximizeButtonVisibleProperty);
            set => SetValue(IsMaximizeButtonVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets the visibility of the close button.
        /// </summary>
        public Visibility IsCloseButtonVisible
        {
            get => (Visibility)GetValue(IsCloseButtonVisibleProperty);
            set => SetValue(IsCloseButtonVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets the visibility of the minimize button.
        /// </summary>
        [Obsolete("Use IsMinimizeButtonVisible instead.")]
        public Visibility MinimizeButtonVisibility
        {
            get => IsMinimizeButtonVisible;
            set => IsMinimizeButtonVisible = value;
        }

        /// <summary>
        /// Gets or sets the visibility of the maximize button.
        /// </summary>
        [Obsolete("Use IsMaximizeButtonVisible instead.")]
        public Visibility MaximizeButtonVisibility
        {
            get => IsMaximizeButtonVisible;
            set => IsMaximizeButtonVisible = value;
        }

        /// <summary>
        /// Gets or sets the visibility of the close button.
        /// </summary>
        [Obsolete("Use IsCloseButtonVisible instead.")]
        public Visibility CloseButtonVisibility
        {
            get => IsCloseButtonVisible;
            set => IsCloseButtonVisible = value;
        }

        /// <summary>
        /// Gets or sets whether the minimize button is enabled.
        /// When false, the button is visible but grayed out.
        /// </summary>
        public bool IsMinimizable
        {
            get => (bool)GetValue(IsMinimizableProperty);
            set => SetValue(IsMinimizableProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the maximize button is enabled.
        /// When false, the button is visible but grayed out.
        /// </summary>
        public bool IsMaximizable
        {
            get => (bool)GetValue(IsMaximizableProperty);
            set => SetValue(IsMaximizableProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the close button is enabled.
        /// When false, the button is visible but grayed out.
        /// </summary>
        public bool IsClosable
        {
            get => (bool)GetValue(IsClosableProperty);
            set => SetValue(IsClosableProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the window can be moved by title-bar dragging or the system move command.
        /// </summary>
        public bool IsMoveable
        {
            get => (bool)GetValue(IsMoveableProperty);
            set => SetValue(IsMoveableProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the window can be moved by title-bar dragging or the system move command.
        /// </summary>
        [Obsolete("Use IsMoveable instead.")]
        public bool CanMove
        {
            get => IsMoveable;
            set => IsMoveable = value;
        }

        /// <summary>
        /// Gets or sets whether the window has a drop shadow. Defaults to true.
        /// </summary>
        public bool HasShadow
        {
            get => (bool)GetValue(HasShadowProperty);
            set => SetValue(HasShadowProperty, value);
        }

        #endregion

        private readonly WindowChrome _windowChrome;
        private IntPtr _handle;

        static FluenceWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FluenceWindow),
                new FrameworkPropertyMetadata(typeof(FluenceWindow)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FluenceWindow"/> class, loads the default style, and wires theme and accent updates.
        /// </summary>
        public FluenceWindow()
        {
            ResourceDictionary resourceDictionary = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Fluence.Wpf;component/Themes/Controls/FluenceWindow.xaml", UriKind.Absolute)
            };
            Style = resourceDictionary[typeof(FluenceWindow)] as Style;

            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, OnCloseWindow));
            CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, OnMaximizeWindow, OnCanResizeWindow));
            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, OnMinimizeWindow, OnCanMinimizeWindow));
            CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, OnRestoreWindow, OnCanResizeWindow));

            _windowChrome = WindowPolicy.CreateWindowChrome(TitleBarHeight);
            WindowChrome.SetWindowChrome(this, _windowChrome);
            UpdateWindowChrome();
            UpdateShellMetrics();
            ApplicationThemeManager.Changed += OnThemeChanged;
            ApplicationAccentColorManager.AccentColorChanged += OnAccentColorChanged;
            ApplyFrame();
        }

        /// <summary>
        /// Sets a UIElement as the custom title bar content. The element becomes the
        /// drag region for the window. Call with null to revert to the default title bar.
        /// </summary>
        public void SetTitleBar(UIElement titleBar)
        {
            TitleBar = titleBar;
        }

        /// <summary>
        /// Sets the minimize button visibility.
        /// </summary>
        [Obsolete("Use IsMinimizeButtonVisible instead.")]
        public void SetMinimizeButtonVisibility(Visibility visibility)
        {
            IsMinimizeButtonVisible = visibility;
        }

        /// <summary>
        /// Sets the maximize/restore button visibility.
        /// </summary>
        [Obsolete("Use IsMaximizeButtonVisible instead.")]
        public void SetMaximizeButtonVisibility(Visibility visibility)
        {
            IsMaximizeButtonVisible = visibility;
        }

        /// <summary>
        /// Sets the close button visibility.
        /// </summary>
        [Obsolete("Use IsCloseButtonVisible instead.")]
        public void SetCloseButtonVisibility(Visibility visibility)
        {
            IsCloseButtonVisible = visibility;
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            // Be tolerant of incomplete design-time templates: missing caption parts should
            // disable only caption-button behavior rather than failing the whole window.
            _minimizeButton = GetTemplateChild(PART_MinimizeButton) as System.Windows.Controls.Button;
            _maximizeButton = GetTemplateChild(PART_MaximizeButton) as System.Windows.Controls.Button;
            _restoreButton = GetTemplateChild(PART_RestoreButton) as System.Windows.Controls.Button;
            _closeButton = GetTemplateChild(PART_CloseButton) as System.Windows.Controls.Button;
            UpdateCaptionButtons();
        }

        /// <inheritdoc />
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _handle = new WindowInteropHelper(this).EnsureHandle();
            _hwndSource = HwndSource.FromHwnd(_handle);
            if (_hwndSource != null)
            {
                _hwndSource.AddHook(WndProc);
            }

            UpdateWindowChrome();
            ApplyWindowShell();
            SystemThemeWatcher.Watch(this);
        }

        /// <inheritdoc />
        protected override void OnStateChanged(EventArgs e)
        {
            ClearSnapHover();
            base.OnStateChanged(e);
            UpdateShellMetrics();
            ApplyFrame();
            UpdateCaptionButtons();
        }

        /// <inheritdoc />
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            ApplyFrame();
        }

        /// <inheritdoc />
        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            ApplyFrame();
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == ResizeModeProperty)
            {
                UpdateShellMetrics();
                UpdateCaptionButtons();
                CommandManager.InvalidateRequerySuggested();
            }

            if (e.Property == Window.WindowStateProperty)
            {
                UpdateCaptionButtons();
            }
        }

        #region DP Change Callbacks

        private static void OnCaptionButtonChromeOverrideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FluenceWindow window)
            {
                window.UpdateCaptionButtons();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private static void OnExtendsContentIntoTitleBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FluenceWindow window)
            {
                window.UpdateWindowChrome();
            }
        }

        private static void OnTitleBarHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FluenceWindow window)
            {
                window.UpdateWindowChrome();
            }
        }

        private static void OnHasShadowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FluenceWindow window)
            {
                window.UpdateWindowChrome();
            }
        }

        #endregion

        /// <inheritdoc />
        protected override void OnClosed(EventArgs e)
        {
            SystemThemeWatcher.UnWatch(this);
            ApplicationThemeManager.Changed -= OnThemeChanged;
            ApplicationAccentColorManager.AccentColorChanged -= OnAccentColorChanged;
            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(WndProc);
                _hwndSource = null;
            }

            base.OnClosed(e);
        }

        private void OnThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ApplyBackdrop();
                    ApplyFrame();
                }));
                return;
            }

            ApplyBackdrop();
            ApplyFrame();
        }

        private void OnAccentColorChanged(object sender, EventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(ApplyFrame));
                return;
            }

            ApplyFrame();
        }

        private static void OnSystemBackdropTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FluenceWindow window)
            {
                window.ApplyBackdrop();
            }
        }

        private static void OnCornerStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FluenceWindow window)
            {
                window.ApplyCornerPreference();
            }
        }

        #region Window Shell

        private void ApplyWindowShell()
        {
            if (_handle == IntPtr.Zero)
            {
                return;
            }

            HideNativeCaptionButtons();
            UpdateShellMetrics();
            ApplyBackdrop();
            ApplyCornerPreference();
            ApplyFrame();
        }

        private void HideNativeCaptionButtons()
        {
            if (_handle != IntPtr.Zero)
            {
                NativeMethods.HideAllWindowButtons(_handle);
            }
        }

        private void UpdateWindowChrome()
        {
            _windowChrome.CaptionHeight = 0;
            _windowChrome.UseAeroCaptionButtons = false;
            _windowChrome.GlassFrameThickness = HasShadow ? new Thickness(-1) : new Thickness(0);
        }

        private void UpdateShellMetrics()
        {
            MarginMaximized = WindowState == WindowState.Maximized ? new Thickness(6) : new Thickness(0);
            _windowChrome.ResizeBorderThickness = WindowPolicy.GetResizeBorderThickness(WindowState, ResizeMode);
        }

        private void ApplyBackdrop()
        {
            WindowCapabilities capabilities = WindowCapabilities.Current;
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                SystemBackdropType,
                ApplicationThemeManager.GetResolvedTheme(),
                capabilities,
                GetFallbackBackgroundColor());

            Background = new SolidColorBrush(plan.BackgroundColor);

            if (_handle == IntPtr.Zero)
            {
                return;
            }

            if (capabilities.SupportsCaptionColor)
            {
                NativeMethods.SetCaptionColor(_handle, plan.CaptionColor);
            }

            NativeMethods.SetImmersiveDarkMode(_handle, plan.UseImmersiveDarkMode);

            if (capabilities.SupportsSystemBackdropType)
            {
                NativeMethods.SetSystemBackdropType(
                    _handle,
                    plan.SystemBackdropType.HasValue ? plan.SystemBackdropType.Value : NativeConstants.DWMSBT_AUTO);
            }

            if (capabilities.SupportsMicaEffect)
            {
                NativeMethods.SetMicaEffect(_handle, plan.UseLegacyMicaEffect);
            }
        }

        private void ApplyFrame()
        {
            WindowCapabilities capabilities = WindowCapabilities.Current;
            FramePlan plan = WindowPolicy.BuildFramePlan(
                WindowState,
                IsActive,
                ApplicationAccentColorManager.IsAccentColorOnTitleBarsEnabled,
                capabilities,
                ApplicationAccentColorManager.SystemAccentColor);

            BorderBrush = TryFindResource(plan.TemplateBorderBrushResourceKey) as Brush ?? Brushes.Transparent;

            if (_handle != IntPtr.Zero && capabilities.SupportsBorderColor)
            {
                NativeMethods.SetBorderColor(_handle, plan.DwmBorderColor);
            }
        }

        private void UpdateCaptionButtons()
        {
            if (_minimizeButton == null ||
                _maximizeButton == null ||
                _restoreButton == null ||
                _closeButton == null)
            {
                return;
            }

            CaptionButtonChrome.GetMinimizeChrome(
                ResizeMode,
                out Visibility minimizeVisibility,
                out bool minimizeEnabled);
            // When the user has explicitly set IsMinimizeButtonVisible (e.g. to re-enable the
            // button under ResizeMode=NoResize), that value wins over the ResizeMode-derived
            // baseline. Otherwise we keep the chrome defaults.
            if (IsCaptionChromeOverrideExplicit(IsMinimizeButtonVisibleProperty))
            {
                minimizeVisibility = IsMinimizeButtonVisible;
                minimizeEnabled = minimizeVisibility == Visibility.Visible;
            }

            if (!IsMinimizable)
            {
                minimizeEnabled = false;
            }

            _minimizeButton.Visibility = minimizeVisibility;
            _minimizeButton.IsEnabled = minimizeEnabled;

            CaptionButtonChrome.GetMaximizeRestoreChrome(
                ResizeMode,
                WindowState,
                out Visibility maxVis,
                out Visibility restVis,
                out bool maxEn,
                out bool restEn);
            if (IsCaptionChromeOverrideExplicit(IsMaximizeButtonVisibleProperty))
            {
                ApplyMaximizeRestoreVisibilityOverride(IsMaximizeButtonVisible, out maxVis, out restVis);
                bool explicitlyVisible = IsMaximizeButtonVisible == Visibility.Visible;
                maxEn = explicitlyVisible && WindowState != WindowState.Maximized;
                restEn = explicitlyVisible && WindowState == WindowState.Maximized;
            }

            if (!IsMaximizable)
            {
                maxEn = false;
                restEn = false;
            }

            _maximizeButton.Visibility = maxVis;
            _restoreButton.Visibility = restVis;
            _maximizeButton.IsEnabled = maxEn;
            _restoreButton.IsEnabled = restEn;

            CaptionButtonChrome.GetCloseChrome(
                out Visibility closeVisibility,
                out bool closeEnabled);
            if (IsCaptionChromeOverrideExplicit(IsCloseButtonVisibleProperty))
            {
                closeVisibility = IsCloseButtonVisible;
                closeEnabled = closeVisibility == Visibility.Visible;
            }

            if (!IsClosable)
            {
                closeEnabled = false;
            }

            _closeButton.Visibility = closeVisibility;
            _closeButton.IsEnabled = closeEnabled;

            UpdateCaptionButtonSlots(minimizeVisibility, maxVis, restVis, closeVisibility);
        }

        private void UpdateCaptionButtonSlots(
            Visibility minimizeVisibility,
            Visibility maximizeVisibility,
            Visibility restoreVisibility,
            Visibility closeVisibility)
        {
            bool closeOccupiesSlot = closeVisibility != Visibility.Collapsed;
            bool maximizeOccupiesSlot =
                maximizeVisibility != Visibility.Collapsed ||
                restoreVisibility != Visibility.Collapsed;
            bool minimizeOccupiesSlot = minimizeVisibility != Visibility.Collapsed;

            int nextSlot = 2;
            Grid.SetColumn(_closeButton, 2);
            if (closeOccupiesSlot)
            {
                nextSlot = 1;
            }

            int maximizeSlot = maximizeOccupiesSlot ? nextSlot : 1;
            Grid.SetColumn(_maximizeButton, maximizeSlot);
            Grid.SetColumn(_restoreButton, maximizeSlot);
            if (maximizeOccupiesSlot)
            {
                nextSlot--;
            }

            if (minimizeOccupiesSlot)
            {
                Grid.SetColumn(_minimizeButton, Math.Max(0, nextSlot));
            }
            else
            {
                Grid.SetColumn(_minimizeButton, 0);
            }
        }

        private void ApplyMaximizeRestoreVisibilityOverride(Visibility visibility, out Visibility maximizeVisibility, out Visibility restoreVisibility)
        {
            if (visibility == Visibility.Visible)
            {
                maximizeVisibility = WindowState == WindowState.Maximized ? Visibility.Collapsed : Visibility.Visible;
                restoreVisibility = WindowState == WindowState.Maximized ? Visibility.Visible : Visibility.Collapsed;
                return;
            }

            if (visibility == Visibility.Hidden)
            {
                maximizeVisibility = WindowState == WindowState.Maximized ? Visibility.Collapsed : Visibility.Hidden;
                restoreVisibility = WindowState == WindowState.Maximized ? Visibility.Hidden : Visibility.Collapsed;
                return;
            }

            maximizeVisibility = Visibility.Collapsed;
            restoreVisibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Returns <c>true</c> when the caption-chrome override property has been explicitly assigned
        /// (via code, XAML local value, style, binding, etc.) rather than left at its declared default.
        /// </summary>
        private bool IsCaptionChromeOverrideExplicit(DependencyProperty dp)
        {
            ValueSource source = DependencyPropertyHelper.GetValueSource(this, dp);
            return source.BaseValueSource != BaseValueSource.Default &&
                   source.BaseValueSource != BaseValueSource.Inherited;
        }

        private void ApplyCornerPreference()
        {
            if (_handle == IntPtr.Zero)
            {
                return;
            }

            WindowCapabilities capabilities = WindowCapabilities.Current;
            if (!capabilities.SupportsRoundedCorners)
            {
                return;
            }

            NativeMethods.SetWindowCornerPreference(_handle, WindowPolicy.GetCornerPreference(CornerStyle));
        }

        #endregion

        #region WndProc

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeConstants.WM_NCHITTEST)
            {
                // WindowChrome routes the whole title bar through WM_NCHITTEST. We return
                // HTCAPTION for drag regions, HTMAXBUTTON for Windows 11 snap-layout hover,
                // and 0 for WPF-controlled buttons or interactive custom title-bar content.
                int result = HitTestTitleBar(lParam);
                if (result == NativeConstants.HTMAXBUTTON)
                {
                    System.Windows.Controls.Button btn = WindowState == WindowState.Maximized ? _restoreButton : _maximizeButton;
                    SetSnapHover(btn);
                }
                else
                {
                    ClearSnapHover();
                }

                if (result != 0)
                {
                    handled = true;
                    return new IntPtr(result);
                }
            }
            else if (msg == NativeConstants.WM_NCMOUSELEAVE)
            {
                ClearSnapHover();
            }
            else if (msg == NativeConstants.WM_SYSCOMMAND &&
                (wParam.ToInt64() & 0xFFF0L) == NativeConstants.SC_MOVE &&
                !IsMoveable)
            {
                handled = true;
            }
            else if (msg == NativeConstants.WM_GETMINMAXINFO)
            {
                IntPtr monitor = NativeMethods.MonitorFromWindow(hwnd, NativeConstants.MONITOR_DEFAULTTONEAREST);
                if (monitor != IntPtr.Zero)
                {
                    MONITORINFO monitorInfo = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                    if (NativeMethods.GetMonitorInfo(monitor, ref monitorInfo))
                    {
                        RECT rcWork = monitorInfo.rcWork;
                        RECT rcMonitor = monitorInfo.rcMonitor;

                        MINMAXINFO mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                        mmi.ptMaxPosition.X = rcWork.Left - rcMonitor.Left;
                        mmi.ptMaxPosition.Y = rcWork.Top - rcMonitor.Top;
                        mmi.ptMaxSize.X = rcWork.Width;
                        mmi.ptMaxSize.Y = rcWork.Height;

                        double dpiX = 1.0, dpiY = 1.0;
                        if (_hwndSource != null && _hwndSource.CompositionTarget != null)
                        {
                            Matrix transform = _hwndSource.CompositionTarget.TransformToDevice;
                            dpiX = transform.M11;
                            dpiY = transform.M22;
                        }

                        // Respect MaxWidth/MaxHeight if set on the window.
                        if (!double.IsPositiveInfinity(MaxWidth) || !double.IsPositiveInfinity(MaxHeight))
                        {
                            if (!double.IsPositiveInfinity(MaxWidth))
                            {
                                int maxWidthPx = (int)(MaxWidth * dpiX);
                                if (maxWidthPx < mmi.ptMaxSize.X)
                                {
                                    mmi.ptMaxSize.X = maxWidthPx;
                                }
                                mmi.ptMaxTrackSize.X = maxWidthPx;
                            }

                            if (!double.IsPositiveInfinity(MaxHeight))
                            {
                                int maxHeightPx = (int)(MaxHeight * dpiY);
                                if (maxHeightPx < mmi.ptMaxSize.Y)
                                {
                                    mmi.ptMaxSize.Y = maxHeightPx;
                                }
                                mmi.ptMaxTrackSize.Y = maxHeightPx;
                            }
                        }

                        // Enforce MinWidth/MinHeight on native resize track (handled=true bypasses WPF defaults).
                        if (MinWidth > 0)
                        {
                            int minWidthPx = (int)Math.Ceiling(MinWidth * dpiX);
                            if (minWidthPx > mmi.ptMinTrackSize.X)
                            {
                                mmi.ptMinTrackSize.X = minWidthPx;
                            }
                        }

                        if (MinHeight > 0)
                        {
                            int minHeightPx = (int)Math.Ceiling(MinHeight * dpiY);
                            if (minHeightPx > mmi.ptMinTrackSize.Y)
                            {
                                mmi.ptMinTrackSize.Y = minHeightPx;
                            }
                        }

                        Marshal.StructureToPtr(mmi, lParam, false);
                        handled = true;
                    }
                }
            }
            else if (msg == NativeConstants.WM_NCLBUTTONUP)
            {
                if (wParam.ToInt32() == NativeConstants.HTMAXBUTTON)
                {
                    ClearSnapHover();
                    if (ResizeMode == ResizeMode.CanResize || ResizeMode == ResizeMode.CanResizeWithGrip)
                    {
                        if (WindowState == WindowState.Maximized)
                        {
                            if (_restoreButton != null && _restoreButton.Visibility == Visibility.Visible &&
                                _restoreButton.IsEnabled)
                            {
                                handled = true;
                                SystemCommands.RestoreWindow(this);
                            }
                        }
                        else
                        {
                            if (_maximizeButton != null && _maximizeButton.Visibility == Visibility.Visible &&
                                _maximizeButton.IsEnabled)
                            {
                                handled = true;
                                SystemCommands.MaximizeWindow(this);
                            }
                        }
                    }
                }
            }

            return IntPtr.Zero;
        }

        private int HitTestTitleBar(IntPtr lParam)
        {
            int x = (short)(lParam.ToInt64() & 0xFFFF);
            int y = (short)((lParam.ToInt64() >> 16) & 0xFFFF);

            Point point = PointFromScreen(new Point(x, y));

            if (point.Y < 0 || point.Y > TitleBarHeight)
            {
                return 0;
            }

            if (_maximizeButton != null && _maximizeButton.Visibility == Visibility.Visible &&
                _maximizeButton.IsEnabled &&
                IsOverElement(_maximizeButton, point))
            {
                return NativeConstants.HTMAXBUTTON;
            }

            if (_restoreButton != null && _restoreButton.Visibility == Visibility.Visible &&
                _restoreButton.IsEnabled &&
                IsOverElement(_restoreButton, point))
            {
                return NativeConstants.HTMAXBUTTON;
            }

            // Minimize and close: return 0 so hit falls through to client area; WPF Button + Command fire.
            if ((_minimizeButton != null && _minimizeButton.Visibility == Visibility.Visible &&
                 IsOverElement(_minimizeButton, point)) ||
                (_closeButton != null && _closeButton.Visibility == Visibility.Visible &&
                 IsOverElement(_closeButton, point)))
            {
                return 0;
            }

            // If a custom-content child marked with IsHitTestVisibleInChrome=True is under the
            // cursor (e.g. a search TextBox or ToggleSwitch in the TitleBar content area), return
            // HTCLIENT so Windows passes the click to WPF rather than treating it as a drag.
            if (IsOverInteractiveContent(point))
            {
                return 0;
            }

            return IsMoveable ? NativeConstants.HTCAPTION : 0;
        }

        private void SetSnapHover(System.Windows.Controls.Button button)
        {
            if (_snapHoveredButton == button)
            {
                return;
            }

            ClearSnapHover();
            if (button != null && button.IsEnabled)
            {
                button.Background = TryFindResource("ControlStrongFillColorDefaultBrush") as Brush ?? Brushes.Transparent;
                button.Foreground = TryFindResource("TextFillColorInverseBrush") as Brush ?? Brushes.White;
                _snapHoveredButton = button;
            }
        }

        private void ClearSnapHover()
        {
            if (_snapHoveredButton != null)
            {
                _snapHoveredButton.Background = Brushes.Transparent;
                _snapHoveredButton.ClearValue(System.Windows.Controls.Control.ForegroundProperty);
                _snapHoveredButton = null;
            }
        }

        private bool IsOverElement(UIElement element, Point windowPoint)
        {
            if (element == null || element.Visibility != Visibility.Visible)
            {
                return false;
            }

            try
            {
                Point topLeft = element.TranslatePoint(new Point(0, 0), this);
                Size size = element.RenderSize;
                Rect rect = new Rect(topLeft, size);
                return rect.Contains(windowPoint);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns <c>true</c> when the element under <paramref name="windowPoint"/> (or any of its
        /// visual ancestors) has <see cref="WindowChrome.IsHitTestVisibleInChromeProperty"/> set to
        /// <c>true</c>.  Used by <see cref="HitTestTitleBar"/> to let clicks on interactive controls
        /// inside the title bar (e.g. a search TextBox or ToggleSwitch) fall through to WPF instead
        /// of being swallowed as caption-area drag gestures.
        /// </summary>
        private bool IsOverInteractiveContent(Point windowPoint)
        {
            DependencyObject? hit = InputHitTest(windowPoint) as DependencyObject;
            while (hit != null)
            {
                if (hit is IInputElement element && WindowChrome.GetIsHitTestVisibleInChrome(element))
                {
                    return true;
                }

                hit = VisualTreeHelper.GetParent(hit);
            }

            return false;
        }

        #endregion

        private static Color GetFallbackBackgroundColor()
        {
            ApplicationTheme resolvedTheme = ApplicationThemeManager.GetResolvedTheme();

            if (resolvedTheme == ApplicationTheme.Dark)
            {
                return Color.FromRgb(0x20, 0x20, 0x20);
            }

            if (resolvedTheme == ApplicationTheme.HighContrast)
            {
                return SystemColors.WindowColor;
            }

            return Color.FromRgb(0xFA, 0xFA, 0xFA);
        }

        #region Command Handlers

        private void OnCanResizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            bool allowedByResizeMode =
                ResizeMode == ResizeMode.CanResize ||
                ResizeMode == ResizeMode.CanResizeWithGrip;
            bool allowedByExplicitDp =
                IsCaptionChromeOverrideExplicit(IsMaximizeButtonVisibleProperty) &&
                IsMaximizeButtonVisible == Visibility.Visible;
            e.CanExecute = (allowedByResizeMode || allowedByExplicitDp) && IsMaximizable;
        }

        private void OnCanMinimizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            bool allowedByResizeMode = ResizeMode != ResizeMode.NoResize;
            bool allowedByExplicitDp =
                IsCaptionChromeOverrideExplicit(IsMinimizeButtonVisibleProperty) &&
                IsMinimizeButtonVisible == Visibility.Visible;
            e.CanExecute = (allowedByResizeMode || allowedByExplicitDp) && IsMinimizable;
        }

        private void OnCloseWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        // Note: Maximize/Minimize/Restore are driven by setting WindowState directly
        // rather than via SystemCommands.*Window, which post WM_SYSCOMMAND. DefWindowProc
        // gates SC_MINIMIZE on WS_SYSMENU + WS_MINIMIZEBOX (and SC_MAXIMIZE on
        // WS_MAXIMIZEBOX); those bits are intentionally stripped by
        // NativeMethods.HideAllWindowButtons so the native caption does not paint over the
        // custom chrome, and they are also stripped by WPF whenever ResizeMode is
        // ResizeMode.NoResize (the XAML baseline for every PSADT fluent dialog). If we
        // routed through WM_SYSCOMMAND the messages would be silently dropped and the
        // caption buttons would appear clickable but do nothing. Assigning WindowState
        // uses ShowWindow under the hood, which honours the requested state regardless of
        // sysmenu/style gating and keeps the custom caption authoritative.
        //
        // Belt-and-braces: we also call NativeMethods.{Minimize/Maximize/Restore}WindowNative
        // after the WPF assignment. These perform a direct ShowWindow() call on the HWND.
        // ShowWindow() is not gated by window styles, modal dispatcher state, Topmost, or
        // ShowInTaskbar, so the caption button remains functional even in niche scenarios
        // where WPF's WindowStateProperty change handler's internal ShowWindow might not
        // reach the native window (for example if _hwndSource is transiently unavailable
        // mid-activation, or if a third-party WndProc hook mutates WM_SIZE/WM_WINDOWPOSCHANGING
        // replies). When the native window is already in the requested state, the helpers
        // short-circuit via IsIconic/IsZoomed so there is no double-transition.
        private void OnMaximizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
            if (_handle != IntPtr.Zero)
            {
                NativeMethods.MaximizeWindowNative(_handle);
            }
        }

        private void OnMinimizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            if (_handle != IntPtr.Zero)
            {
                NativeMethods.MinimizeWindowNative(_handle);
            }
        }

        private void OnRestoreWindow(object sender, ExecutedRoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
            if (_handle != IntPtr.Zero)
            {
                NativeMethods.RestoreWindowNative(_handle);
            }
        }

        #endregion
    }
}
