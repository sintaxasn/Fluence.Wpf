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
        private const double TitleBarHeight = 32d;

        private System.Windows.Controls.Button _minimizeButton;
        private System.Windows.Controls.Button _maximizeButton;
        private System.Windows.Controls.Button _restoreButton;
        private System.Windows.Controls.Button _closeButton;

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

        public static readonly DependencyProperty WindowBackdropProperty =
            DependencyProperty.Register(
                "WindowBackdrop",
                typeof(BackdropType),
                typeof(FluentWindow),
                new PropertyMetadata(BackdropType.Auto, OnWindowBackdropChanged));

        public static readonly DependencyProperty WindowCornersProperty =
            DependencyProperty.Register(
                "WindowCorners",
                typeof(CornerPreference),
                typeof(FluentWindow),
                new PropertyMetadata(CornerPreference.Round, OnWindowCornersChanged));

        public static readonly DependencyProperty MarginMaximizedProperty =
            DependencyProperty.Register(
                "MarginMaximized",
                typeof(Thickness),
                typeof(FluentWindow),
                new PropertyMetadata(new Thickness(0)));

        public static readonly DependencyProperty MinimizeButtonOverrideProperty =
            DependencyProperty.Register(
                nameof(MinimizeButtonOverride),
                typeof(CaptionButtonOverride),
                typeof(FluentWindow),
                new PropertyMetadata(CaptionButtonOverride.Default, OnCaptionButtonChromeOverrideChanged));

        public static readonly DependencyProperty MaximizeButtonOverrideProperty =
            DependencyProperty.Register(
                nameof(MaximizeButtonOverride),
                typeof(CaptionButtonOverride),
                typeof(FluentWindow),
                new PropertyMetadata(CaptionButtonOverride.Default, OnCaptionButtonChromeOverrideChanged));

        public static readonly DependencyProperty CloseButtonOverrideProperty =
            DependencyProperty.Register(
                nameof(CloseButtonOverride),
                typeof(CaptionButtonOverride),
                typeof(FluentWindow),
                new PropertyMetadata(CaptionButtonOverride.Default, OnCaptionButtonChromeOverrideChanged));

        #endregion

        #region Properties

        public BackdropType WindowBackdrop
        {
            get { return (BackdropType)GetValue(WindowBackdropProperty); }
            set { SetValue(WindowBackdropProperty, value); }
        }

        public CornerPreference WindowCorners
        {
            get { return (CornerPreference)GetValue(WindowCornersProperty); }
            set { SetValue(WindowCornersProperty, value); }
        }

        public Thickness MarginMaximized
        {
            get { return (Thickness)GetValue(MarginMaximizedProperty); }
            set { SetValue(MarginMaximizedProperty, value); }
        }

        public CaptionButtonOverride MinimizeButtonOverride
        {
            get { return (CaptionButtonOverride)GetValue(MinimizeButtonOverrideProperty); }
            set { SetValue(MinimizeButtonOverrideProperty, value); }
        }

        public CaptionButtonOverride MaximizeButtonOverride
        {
            get { return (CaptionButtonOverride)GetValue(MaximizeButtonOverrideProperty); }
            set { SetValue(MaximizeButtonOverrideProperty, value); }
        }

        public CaptionButtonOverride CloseButtonOverride
        {
            get { return (CaptionButtonOverride)GetValue(CloseButtonOverrideProperty); }
            set { SetValue(CloseButtonOverrideProperty, value); }
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
            UpdateShellMetrics();
            ApplicationThemeManager.Changed += OnThemeChanged;
            ApplicationAccentColorManager.AccentColorChanged += OnAccentColorChanged;
            ApplyFrame();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _minimizeButton = GetTemplateChild("MinimizeButton") as System.Windows.Controls.Button;
            _maximizeButton = GetTemplateChild("MaximizeButton") as System.Windows.Controls.Button;
            _restoreButton = GetTemplateChild("RestoreButton") as System.Windows.Controls.Button;
            _closeButton = GetTemplateChild("CloseButton") as System.Windows.Controls.Button;
            UpdateCaptionButtons();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _handle = new WindowInteropHelper(this).EnsureHandle();
            ApplyWindowShell();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            UpdateShellMetrics();
            ApplyFrame();
            UpdateCaptionButtons();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            ApplyFrame();
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            ApplyFrame();
        }

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

        private static void OnCaptionButtonChromeOverrideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var window = d as FluentWindow;
            if (window != null)
            {
                window.UpdateCaptionButtons();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            ApplicationThemeManager.Changed -= OnThemeChanged;
            ApplicationAccentColorManager.AccentColorChanged -= OnAccentColorChanged;
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
            _maximizeButton.Visibility = maxVis;
            _restoreButton.Visibility = restVis;
            _maximizeButton.IsEnabled = maxEn;
            _restoreButton.IsEnabled = restEn;

            CaptionButtonChrome.GetCloseChrome(
                CloseButtonOverride,
                out var closeVisibility,
                out var closeEnabled);
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
