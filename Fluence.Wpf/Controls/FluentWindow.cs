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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using Fluence.Wpf.Native;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A window with Windows 11 Fluent Design chrome, backdrop support, and custom caption buttons.
    /// </summary>
    public class FluentWindow : Window
    {
        private const double DefaultTitleBarHeight = 48d;

        private System.Windows.Controls.Button _minimizeButton;
        private System.Windows.Controls.Button _maximizeButton;
        private System.Windows.Controls.Button _restoreButton;
        private System.Windows.Controls.Button _closeButton;
        private HwndSource _hwndSource;

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
        /// Identifies the <see cref="WindowBackdrop"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WindowBackdropProperty =
            DependencyProperty.Register(
                "WindowBackdrop",
                typeof(BackdropType),
                typeof(FluentWindow),
                new PropertyMetadata(BackdropType.Auto, OnWindowBackdropChanged));

        /// <summary>
        /// Identifies the <see cref="WindowCorners"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WindowCornersProperty =
            DependencyProperty.Register(
                "WindowCorners",
                typeof(CornerPreference),
                typeof(FluentWindow),
                new PropertyMetadata(CornerPreference.Round, OnWindowCornersChanged));

        /// <summary>
        /// Identifies the <see cref="MarginMaximized"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MarginMaximizedProperty =
            DependencyProperty.Register(
                "MarginMaximized",
                typeof(Thickness),
                typeof(FluentWindow),
                new PropertyMetadata(new Thickness(0)));

        /// <summary>
        /// Identifies the <see cref="MinimizeButtonOverride"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimizeButtonOverrideProperty =
            DependencyProperty.Register(
                nameof(MinimizeButtonOverride),
                typeof(CaptionButtonOverride),
                typeof(FluentWindow),
                new PropertyMetadata(CaptionButtonOverride.Default, OnCaptionButtonChromeOverrideChanged));

        /// <summary>
        /// Identifies the <see cref="MaximizeButtonOverride"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximizeButtonOverrideProperty =
            DependencyProperty.Register(
                nameof(MaximizeButtonOverride),
                typeof(CaptionButtonOverride),
                typeof(FluentWindow),
                new PropertyMetadata(CaptionButtonOverride.Default, OnCaptionButtonChromeOverrideChanged));

        /// <summary>
        /// Identifies the <see cref="CloseButtonOverride"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CloseButtonOverrideProperty =
            DependencyProperty.Register(
                nameof(CloseButtonOverride),
                typeof(CaptionButtonOverride),
                typeof(FluentWindow),
                new PropertyMetadata(CaptionButtonOverride.Default, OnCaptionButtonChromeOverrideChanged));

        /// <summary>
        /// Identifies the <see cref="ExtendsContentIntoTitleBar"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExtendsContentIntoTitleBarProperty =
            DependencyProperty.Register(
                nameof(ExtendsContentIntoTitleBar),
                typeof(bool),
                typeof(FluentWindow),
                new PropertyMetadata(false, OnExtendsContentIntoTitleBarChanged));

        /// <summary>
        /// Identifies the <see cref="TitleBar"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleBarProperty =
            DependencyProperty.Register(
                nameof(TitleBar),
                typeof(UIElement),
                typeof(FluentWindow),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="TitleBarHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleBarHeightProperty =
            DependencyProperty.Register(
                nameof(TitleBarHeight),
                typeof(double),
                typeof(FluentWindow),
                new PropertyMetadata(DefaultTitleBarHeight, OnTitleBarHeightChanged));

        /// <summary>
        /// Identifies the <see cref="ShowIcon"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowIconProperty =
            DependencyProperty.Register(
                nameof(ShowIcon),
                typeof(bool),
                typeof(FluentWindow),
                new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="ShowTitle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowTitleProperty =
            DependencyProperty.Register(
                nameof(ShowTitle),
                typeof(bool),
                typeof(FluentWindow),
                new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="MinimizeButtonVisibility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimizeButtonVisibilityProperty =
            DependencyProperty.Register(
                nameof(MinimizeButtonVisibility),
                typeof(Visibility),
                typeof(FluentWindow),
                new PropertyMetadata(Visibility.Visible, OnCaptionButtonChromeOverrideChanged));

        /// <summary>
        /// Identifies the <see cref="MaximizeButtonVisibility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximizeButtonVisibilityProperty =
            DependencyProperty.Register(
                nameof(MaximizeButtonVisibility),
                typeof(Visibility),
                typeof(FluentWindow),
                new PropertyMetadata(Visibility.Visible, OnCaptionButtonChromeOverrideChanged));

        /// <summary>
        /// Identifies the <see cref="CloseButtonVisibility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CloseButtonVisibilityProperty =
            DependencyProperty.Register(
                nameof(CloseButtonVisibility),
                typeof(Visibility),
                typeof(FluentWindow),
                new PropertyMetadata(Visibility.Visible, OnCaptionButtonChromeOverrideChanged));

        /// <summary>
        /// Identifies the <see cref="IsMinimizeButtonEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsMinimizeButtonEnabledProperty =
            DependencyProperty.Register(
                nameof(IsMinimizeButtonEnabled),
                typeof(bool),
                typeof(FluentWindow),
                new PropertyMetadata(true, OnCaptionButtonChromeOverrideChanged));

        /// <summary>
        /// Identifies the <see cref="IsMaximizeButtonEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsMaximizeButtonEnabledProperty =
            DependencyProperty.Register(
                nameof(IsMaximizeButtonEnabled),
                typeof(bool),
                typeof(FluentWindow),
                new PropertyMetadata(true, OnCaptionButtonChromeOverrideChanged));

        /// <summary>
        /// Identifies the <see cref="IsCloseButtonEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsCloseButtonEnabledProperty =
            DependencyProperty.Register(
                nameof(IsCloseButtonEnabled),
                typeof(bool),
                typeof(FluentWindow),
                new PropertyMetadata(true, OnCaptionButtonChromeOverrideChanged));

        /// <summary>
        /// Identifies the <see cref="WindowBorderThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WindowBorderThicknessProperty =
            DependencyProperty.Register(
                nameof(WindowBorderThickness),
                typeof(Thickness),
                typeof(FluentWindow),
                new PropertyMetadata(new Thickness(1), OnFramePropertyChanged));

        /// <summary>
        /// Identifies the <see cref="WindowBorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WindowBorderBrushProperty =
            DependencyProperty.Register(
                nameof(WindowBorderBrush),
                typeof(Brush),
                typeof(FluentWindow),
                new PropertyMetadata(null, OnFramePropertyChanged));

        /// <summary>
        /// Identifies the <see cref="HasShadow"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HasShadowProperty =
            DependencyProperty.Register(
                nameof(HasShadow),
                typeof(bool),
                typeof(FluentWindow),
                new PropertyMetadata(true, OnHasShadowChanged));

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the requested system backdrop (Mica, Acrylic, Tabbed, or none).
        /// </summary>
        public BackdropType WindowBackdrop
        {
            get { return (BackdropType)GetValue(WindowBackdropProperty); }
            set { SetValue(WindowBackdropProperty, value); }
        }

        /// <summary>
        /// Gets or sets the preferred window corner rounding policy for DWM.
        /// </summary>
        public CornerPreference WindowCorners
        {
            get { return (CornerPreference)GetValue(WindowCornersProperty); }
            set { SetValue(WindowCornersProperty, value); }
        }

        /// <summary>
        /// Gets or sets extra margin applied when the window is maximized to avoid overlap with the work area.
        /// </summary>
        public Thickness MarginMaximized
        {
            get { return (Thickness)GetValue(MarginMaximizedProperty); }
            set { SetValue(MarginMaximizedProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the minimize caption button is shown, disabled, or hidden.
        /// </summary>
        public CaptionButtonOverride MinimizeButtonOverride
        {
            get { return (CaptionButtonOverride)GetValue(MinimizeButtonOverrideProperty); }
            set { SetValue(MinimizeButtonOverrideProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the maximize/restore caption button is shown, disabled, or hidden.
        /// </summary>
        public CaptionButtonOverride MaximizeButtonOverride
        {
            get { return (CaptionButtonOverride)GetValue(MaximizeButtonOverrideProperty); }
            set { SetValue(MaximizeButtonOverrideProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the close caption button is shown, disabled, or hidden.
        /// </summary>
        public CaptionButtonOverride CloseButtonOverride
        {
            get { return (CaptionButtonOverride)GetValue(CloseButtonOverrideProperty); }
            set { SetValue(CloseButtonOverrideProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the window content extends into the title bar area,
        /// replacing the system title bar with a custom one rendered by the control template.
        /// </summary>
        public bool ExtendsContentIntoTitleBar
        {
            get { return (bool)GetValue(ExtendsContentIntoTitleBarProperty); }
            set { SetValue(ExtendsContentIntoTitleBarProperty, value); }
        }

        /// <summary>
        /// Gets or sets custom content displayed in the title bar region.
        /// When null and <see cref="ExtendsContentIntoTitleBar"/> is true, a default title bar with icon and title is shown.
        /// </summary>
        public UIElement TitleBar
        {
            get { return (UIElement)GetValue(TitleBarProperty); }
            set { SetValue(TitleBarProperty, value); }
        }

        /// <summary>
        /// Gets or sets the height of the title bar region. Standard = 48, compact = 32.
        /// </summary>
        public double TitleBarHeight
        {
            get { return (double)GetValue(TitleBarHeightProperty); }
            set { SetValue(TitleBarHeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the window icon is shown in the title bar.
        /// </summary>
        public bool ShowIcon
        {
            get { return (bool)GetValue(ShowIconProperty); }
            set { SetValue(ShowIconProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the window title text is shown in the title bar.
        /// </summary>
        public bool ShowTitle
        {
            get { return (bool)GetValue(ShowTitleProperty); }
            set { SetValue(ShowTitleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the visibility of the minimize button.
        /// </summary>
        public Visibility MinimizeButtonVisibility
        {
            get { return (Visibility)GetValue(MinimizeButtonVisibilityProperty); }
            set { SetValue(MinimizeButtonVisibilityProperty, value); }
        }

        /// <summary>
        /// Gets or sets the visibility of the maximize button.
        /// </summary>
        public Visibility MaximizeButtonVisibility
        {
            get { return (Visibility)GetValue(MaximizeButtonVisibilityProperty); }
            set { SetValue(MaximizeButtonVisibilityProperty, value); }
        }

        /// <summary>
        /// Gets or sets the visibility of the close button.
        /// </summary>
        public Visibility CloseButtonVisibility
        {
            get { return (Visibility)GetValue(CloseButtonVisibilityProperty); }
            set { SetValue(CloseButtonVisibilityProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the minimize button is enabled.
        /// </summary>
        public bool IsMinimizeButtonEnabled
        {
            get { return (bool)GetValue(IsMinimizeButtonEnabledProperty); }
            set { SetValue(IsMinimizeButtonEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the maximize button is enabled.
        /// </summary>
        public bool IsMaximizeButtonEnabled
        {
            get { return (bool)GetValue(IsMaximizeButtonEnabledProperty); }
            set { SetValue(IsMaximizeButtonEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the close button is enabled.
        /// </summary>
        public bool IsCloseButtonEnabled
        {
            get { return (bool)GetValue(IsCloseButtonEnabledProperty); }
            set { SetValue(IsCloseButtonEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets the window border thickness. When all sides are 0, border is hidden.
        /// </summary>
        public Thickness WindowBorderThickness
        {
            get { return (Thickness)GetValue(WindowBorderThicknessProperty); }
            set { SetValue(WindowBorderThicknessProperty, value); }
        }

        /// <summary>
        /// Gets or sets the window border brush. Null uses theme default (<c>CardStrokeColorDefaultBrush</c>).
        /// </summary>
        public Brush WindowBorderBrush
        {
            get { return (Brush)GetValue(WindowBorderBrushProperty); }
            set { SetValue(WindowBorderBrushProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the window has a drop shadow. Defaults to true.
        /// </summary>
        public bool HasShadow
        {
            get { return (bool)GetValue(HasShadowProperty); }
            set { SetValue(HasShadowProperty, value); }
        }

        #endregion

        private readonly WindowChrome _windowChrome;
        private IntPtr _handle;

        static FluentWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FluentWindow),
                new FrameworkPropertyMetadata(typeof(FluentWindow)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentWindow"/> class, loads the default style, and wires theme and accent updates.
        /// </summary>
        public FluentWindow()
        {
            var resourceDictionary = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Fluence.Wpf;component/Themes/Controls/FluentWindow.xaml", UriKind.Absolute)
            };
            Style = resourceDictionary[typeof(FluentWindow)] as Style;

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

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _minimizeButton = GetTemplateChild("MinimizeButton") as System.Windows.Controls.Button;
            _maximizeButton = GetTemplateChild("MaximizeButton") as System.Windows.Controls.Button;
            _restoreButton = GetTemplateChild("RestoreButton") as System.Windows.Controls.Button;
            _closeButton = GetTemplateChild("CloseButton") as System.Windows.Controls.Button;
            UpdateCaptionButtons();
        }

        /// <inheritdoc />
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _handle = new WindowInteropHelper(this).EnsureHandle();
            _hwndSource = HwndSource.FromHwnd(_handle);
            if (ExtendsContentIntoTitleBar && _hwndSource != null)
            {
                _hwndSource.AddHook(WndProc);
            }

            ApplyWindowShell();
        }

        /// <inheritdoc />
        protected override void OnStateChanged(EventArgs e)
        {
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
            }

            if (e.Property == Window.WindowStateProperty)
            {
                UpdateCaptionButtons();
            }
        }

        #region DP Change Callbacks

        private static void OnCaptionButtonChromeOverrideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = d as FluentWindow;
            if (window != null)
            {
                window.UpdateCaptionButtons();
            }
        }

        private static void OnExtendsContentIntoTitleBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = d as FluentWindow;
            if (window != null)
            {
                window.UpdateWindowChrome();
                if (window._hwndSource != null)
                {
                    if ((bool)e.NewValue)
                    {
                        window._hwndSource.AddHook(window.WndProc);
                    }
                    else
                    {
                        window._hwndSource.RemoveHook(window.WndProc);
                    }
                }
            }
        }

        private static void OnTitleBarHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = d as FluentWindow;
            if (window != null)
            {
                window.UpdateWindowChrome();
            }
        }

        private static void OnHasShadowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = d as FluentWindow;
            if (window != null)
            {
                window.UpdateWindowChrome();
            }
        }

        private static void OnFramePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = d as FluentWindow;
            if (window != null)
            {
                window.ApplyFrame();
            }
        }

        #endregion

        /// <inheritdoc />
        protected override void OnClosed(EventArgs e)
        {
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
            ApplyBackdrop();
            ApplyFrame();
        }

        private void OnAccentColorChanged(object sender, EventArgs e)
        {
            ApplyFrame();
        }

        private static void OnWindowBackdropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = d as FluentWindow;
            if (window != null)
            {
                window.ApplyBackdrop();
            }
        }

        private static void OnWindowCornersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = d as FluentWindow;
            if (window != null)
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
            if (ExtendsContentIntoTitleBar)
            {
                _windowChrome.CaptionHeight = 0;
                _windowChrome.UseAeroCaptionButtons = false;
            }
            else
            {
                _windowChrome.CaptionHeight = TitleBarHeight;
                _windowChrome.UseAeroCaptionButtons = false;
            }

            _windowChrome.GlassFrameThickness = HasShadow ? new Thickness(-1) : new Thickness(0);
        }

        private void UpdateShellMetrics()
        {
            MarginMaximized = WindowState == WindowState.Maximized ? new Thickness(6) : new Thickness(0);
            _windowChrome.ResizeBorderThickness = WindowPolicy.GetResizeBorderThickness(WindowState, ResizeMode);
        }

        private void ApplyBackdrop()
        {
            var capabilities = WindowCapabilities.Current;
            var plan = WindowPolicy.BuildBackdropPlan(
                WindowBackdrop,
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
            var borderThickness = WindowBorderThickness;
            bool isAllZero = borderThickness.Left == 0 && borderThickness.Top == 0 &&
                             borderThickness.Right == 0 && borderThickness.Bottom == 0;

            if (isAllZero)
            {
                BorderThickness = new Thickness(0);
                BorderBrush = Brushes.Transparent;
            }
            else if (WindowBorderBrush != null)
            {
                BorderThickness = WindowState == WindowState.Maximized ? new Thickness(0) : borderThickness;
                BorderBrush = WindowBorderBrush;
            }
            else
            {
                var capabilities = WindowCapabilities.Current;
                var plan = WindowPolicy.BuildFramePlan(
                    WindowState,
                    IsActive,
                    ApplicationAccentColorManager.IsAccentColorOnTitleBarsEnabled,
                    capabilities,
                    ApplicationAccentColorManager.SystemAccentColor);

                BorderThickness = plan.TemplateBorderThickness;
                BorderBrush = TryFindResource(plan.TemplateBorderBrushResourceKey) as Brush ?? Brushes.Transparent;

                if (_handle != IntPtr.Zero && capabilities.SupportsBorderColor)
                {
                    NativeMethods.SetBorderColor(_handle, plan.DwmBorderColor);
                }
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
                MinimizeButtonOverride,
                out var minimizeVisibility,
                out var minimizeEnabled);
            if (MinimizeButtonVisibility != Visibility.Visible)
            {
                minimizeVisibility = MinimizeButtonVisibility;
            }

            if (!IsMinimizeButtonEnabled)
            {
                minimizeEnabled = false;
            }

            _minimizeButton.Visibility = minimizeVisibility;
            _minimizeButton.IsEnabled = minimizeEnabled;

            CaptionButtonChrome.GetMaximizeRestoreChrome(
                ResizeMode,
                WindowState,
                MaximizeButtonOverride,
                out var maxVis,
                out var restVis,
                out var maxEn,
                out var restEn);
            if (MaximizeButtonVisibility != Visibility.Visible)
            {
                maxVis = MaximizeButtonVisibility;
                restVis = MaximizeButtonVisibility;
            }

            if (!IsMaximizeButtonEnabled)
            {
                maxEn = false;
                restEn = false;
            }

            _maximizeButton.Visibility = maxVis;
            _restoreButton.Visibility = restVis;
            _maximizeButton.IsEnabled = maxEn;
            _restoreButton.IsEnabled = restEn;

            CaptionButtonChrome.GetCloseChrome(
                CloseButtonOverride,
                out var closeVisibility,
                out var closeEnabled);
            if (CloseButtonVisibility != Visibility.Visible)
            {
                closeVisibility = CloseButtonVisibility;
            }

            if (!IsCloseButtonEnabled)
            {
                closeEnabled = false;
            }

            _closeButton.Visibility = closeVisibility;
            _closeButton.IsEnabled = closeEnabled;
        }

        private void ApplyCornerPreference()
        {
            if (_handle == IntPtr.Zero)
            {
                return;
            }

            var capabilities = WindowCapabilities.Current;
            if (!capabilities.SupportsRoundedCorners)
            {
                return;
            }

            NativeMethods.SetWindowCornerPreference(_handle, WindowPolicy.GetCornerPreference(WindowCorners));
        }

        #endregion

        #region WndProc

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeConstants.WM_NCHITTEST && ExtendsContentIntoTitleBar)
            {
                var result = HitTestTitleBar(lParam);
                if (result != 0)
                {
                    handled = true;
                    return new IntPtr(result);
                }
            }

            return IntPtr.Zero;
        }

        private int HitTestTitleBar(IntPtr lParam)
        {
            int x = (short)(lParam.ToInt64() & 0xFFFF);
            int y = (short)((lParam.ToInt64() >> 16) & 0xFFFF);

            var point = PointFromScreen(new Point(x, y));

            if (point.Y < 0 || point.Y > TitleBarHeight)
            {
                return 0;
            }

            if (_maximizeButton != null && _maximizeButton.Visibility == Visibility.Visible &&
                IsOverElement(_maximizeButton, point))
            {
                return NativeConstants.HTMAXBUTTON;
            }

            if (_restoreButton != null && _restoreButton.Visibility == Visibility.Visible &&
                IsOverElement(_restoreButton, point))
            {
                return NativeConstants.HTMAXBUTTON;
            }

            if (_minimizeButton != null && _minimizeButton.Visibility == Visibility.Visible &&
                IsOverElement(_minimizeButton, point))
            {
                return NativeConstants.HTMINBUTTON;
            }

            if (_closeButton != null && _closeButton.Visibility == Visibility.Visible &&
                IsOverElement(_closeButton, point))
            {
                return NativeConstants.HTCLOSE;
            }

            return NativeConstants.HTCAPTION;
        }

        private bool IsOverElement(UIElement element, Point windowPoint)
        {
            if (element == null || element.Visibility != Visibility.Visible)
            {
                return false;
            }

            try
            {
                var topLeft = element.TranslatePoint(new Point(0, 0), this);
                var size = element.RenderSize;
                var rect = new Rect(topLeft, size);
                return rect.Contains(windowPoint);
            }
            catch
            {
                return false;
            }
        }

        #endregion

        private Color GetFallbackBackgroundColor()
        {
            var resolvedTheme = ApplicationThemeManager.GetResolvedTheme();

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
            e.CanExecute = ResizeMode == ResizeMode.CanResize || ResizeMode == ResizeMode.CanResizeWithGrip;
        }

        private void OnCanMinimizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ResizeMode != ResizeMode.NoResize;
        }

        private void OnCloseWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void OnMaximizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(this);
        }

        private void OnMinimizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void OnRestoreWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(this);
        }

        #endregion
    }
}
