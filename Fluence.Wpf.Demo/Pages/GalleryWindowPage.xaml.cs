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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Fluence.Wpf;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryWindowPage : UserControl
    {
        private bool _isSyncingTheme;
        private bool _isSyncingBackdrop;

        public GalleryWindowPage()
        {
            InitializeComponent();

            DemoSourceAction.Replace(ThemeAndAccentSourceLink, "Window/ThemeAndAccent.xaml");
            DemoSourceAction.Replace(BackdropAndCaptionButtonsSourceLink, "Window/BackdropAndCaptionButtons.xaml");
            DemoSourceAction.Replace(TitleBarChromeSourceLink, "Window/TitleBarChrome.xaml");

            Loaded += GalleryWindowPage_Loaded;
            Unloaded += GalleryWindowPage_Unloaded;
        }

        private FluenceWindow HostFluenceWindow
        {
            get { return Window.GetWindow(this) as FluenceWindow; }
        }

        private void GalleryWindowPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= GalleryWindowPage_Loaded;
            ApplicationThemeManager.Changed += ApplicationThemeManager_Changed;
            SyncThemeRadioButtons();
            SyncBackdropComboFromWindow();
            WindowChromeToggle_Changed(null, null);
            TitleBarToggle_Changed(null, null);
            CaptionVisibilityCombo_SelectionChanged(null, null);
        }

        private void GalleryWindowPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= GalleryWindowPage_Unloaded;
            ApplicationThemeManager.Changed -= ApplicationThemeManager_Changed;
        }

        private void ApplicationThemeManager_Changed(object sender, ThemeChangedEventArgs e)
        {
            SyncThemeRadioButtons();
            SyncBackdropComboFromWindow();
        }

        private void SyncBackdropComboFromWindow()
        {
            var fw = HostFluenceWindow;
            if (fw == null || BackdropCombo == null)
            {
                return;
            }

            _isSyncingBackdrop = true;
            try
            {
                int idx;
                switch (fw.SystemBackdropType)
                {
                    case BackdropType.None:
                        idx = 1;
                        break;
                    case BackdropType.Mica:
                        idx = 2;
                        break;
                    case BackdropType.Acrylic:
                        idx = 3;
                        break;
                    case BackdropType.Tabbed:
                        idx = 4;
                        break;
                    default:
                        idx = 0;
                        break;
                }

                BackdropCombo.SelectedIndex = idx;
            }
            finally
            {
                _isSyncingBackdrop = false;
            }
        }

        private void ThemeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded || _isSyncingTheme)
            {
                return;
            }

            var rb = sender as System.Windows.Controls.RadioButton;
            if (rb == null)
            {
                return;
            }

            ApplicationTheme theme;
            if (ReferenceEquals(rb, ThemeLight))
            {
                theme = ApplicationTheme.Light;
            }
            else if (ReferenceEquals(rb, ThemeDark))
            {
                theme = ApplicationTheme.Dark;
            }
            else if (ReferenceEquals(rb, ThemeHighContrast))
            {
                theme = ApplicationTheme.HighContrast;
            }
            else
            {
                theme = ApplicationTheme.Auto;
            }

            var fw = HostFluenceWindow;
            var backdrop = fw != null ? fw.SystemBackdropType : BackdropType.Mica;
            ApplicationThemeManager.Apply(theme, backdrop, true);
            UpdateThemeStateLabel(ApplicationThemeManager.CurrentTheme);
        }

        private void BackdropCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _isSyncingBackdrop)
            {
                return;
            }

            var fw = HostFluenceWindow;
            if (fw == null)
            {
                return;
            }

            BackdropType backdrop;
            switch (BackdropCombo.SelectedIndex)
            {
                case 1:
                    backdrop = BackdropType.None;
                    break;
                case 2:
                    backdrop = BackdropType.Mica;
                    break;
                case 3:
                    backdrop = BackdropType.Acrylic;
                    break;
                case 4:
                    backdrop = BackdropType.Tabbed;
                    break;
                default:
                    backdrop = BackdropType.Auto;
                    break;
            }

            fw.SystemBackdropType = backdrop;
            ApplicationThemeManager.Apply(ApplicationThemeManager.CurrentTheme, backdrop, false);
        }

        private void AccentSwatch_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as FrameworkElement;
            if (button == null || button.Tag == null)
            {
                return;
            }

            var hex = button.Tag.ToString();
            try
            {
                var converted = ColorConverter.ConvertFromString(hex);
                if (converted != null)
                {
                    ApplicationAccentColorManager.ApplyCustomAccent((Color)converted);
                }
            }
            catch (FormatException)
            {
            }
        }

        private void SystemAccent_Click(object sender, RoutedEventArgs e)
        {
            ApplicationAccentColorManager.ApplySystemAccent();
        }

        private void SystemThemeWatcher_Toggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            var host = HostFluenceWindow;
            if (host == null)
            {
                return;
            }

            if (ThemeWatcherToggle.IsChecked == true)
            {
                SystemThemeWatcher.Watch(host);
                SystemThemeLabel.Text = "Watching: Yes";
            }
            else
            {
                SystemThemeWatcher.UnWatch(host);
                SystemThemeLabel.Text = "Watching: No";
            }
        }

        private void CaptionVisibilityCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            var fw = HostFluenceWindow;
            if (fw == null)
            {
                return;
            }

            ApplyCaptionVisibility(MinimizeVisibilityCombo, v => fw.SetMinimizeButtonVisibility(v), en => fw.IsMinimizable = en);
            ApplyCaptionVisibility(MaximizeVisibilityCombo, v => fw.SetMaximizeButtonVisibility(v), en => fw.IsMaximizable = en);
            ApplyCaptionVisibility(CloseVisibilityCombo, v => fw.SetCloseButtonVisibility(v), en => fw.IsClosable = en);
        }

        private void WindowChromeToggle_Changed(object sender, RoutedEventArgs e)
        {
            var fw = HostFluenceWindow;
            if (fw == null)
            {
                return;
            }

            // Funnel through MainWindow helpers so the Top-pane + extended-chrome hide rule
            // (see MainWindow.ApplyTitleBarContentVisibility) stays authoritative for the
            // actual ShowIcon / ShowTitle DPs.
            var main = fw as MainWindow;

            if (ShowWindowTitleToggle != null)
            {
                bool show = ShowWindowTitleToggle.IsChecked == true;
                string title = show ? MainWindow.GalleryWindowTitle : string.Empty;
                if (main != null)
                {
                    main.SetUserShowTitle(show, title);
                }
                else
                {
                    fw.ShowTitle = show;
                    fw.Title = title;
                }
            }

            if (ShowWindowIconToggle != null)
            {
                bool show = ShowWindowIconToggle.IsChecked == true;
                ImageSource icon = show
                    ? new BitmapImage(new Uri("pack://application:,,,/Fluence.Wpf.Demo;component/Resources/AppIcon.png"))
                    : null;
                if (main != null)
                {
                    main.SetUserShowIcon(show, icon);
                }
                else
                {
                    fw.ShowIcon = show;
                    fw.Icon = icon;
                }
            }
        }

        private void TitleBarToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            var fw = HostFluenceWindow;
            if (fw == null)
            {
                return;
            }

            if (ExtendsContentToggle != null)
            {
                bool extends = ExtendsContentToggle.IsChecked == true;
                fw.ExtendsContentIntoTitleBar = extends;
                // When extending into the title bar and a Left / LeftCompact navigation
                // pane is present, push the icon + title past the compact rail so they do
                // not overlap the pane. 48 matches the compact rail width.
                fw.TitleBarLeftIndent = extends ? 48d : 0d;
            }

            if (HasShadowToggle != null)
            {
                fw.HasShadow = HasShadowToggle.IsChecked == true;
            }
        }

        private void TitleBarHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded)
            {
                return;
            }

            var fw = HostFluenceWindow;
            if (fw != null)
            {
                fw.TitleBarHeight = e.NewValue;
            }

            if (TitleBarHeightLabel != null)
            {
                TitleBarHeightLabel.Text = ((int)e.NewValue).ToString(CultureInfo.CurrentCulture);
            }
        }

        private void SyncThemeRadioButtons()
        {
            if (ThemeLight == null)
            {
                return;
            }

            _isSyncingTheme = true;
            try
            {
                var theme = ApplicationThemeManager.CurrentTheme;
                switch (theme)
                {
                    case ApplicationTheme.Light:
                        ThemeLight.IsChecked = true;
                        break;
                    case ApplicationTheme.Dark:
                        ThemeDark.IsChecked = true;
                        break;
                    case ApplicationTheme.HighContrast:
                        ThemeHighContrast.IsChecked = true;
                        break;
                    default:
                        ThemeAuto.IsChecked = true;
                        break;
                }

                UpdateThemeStateLabel(theme);
            }
            finally
            {
                _isSyncingTheme = false;
            }
        }

        private void UpdateThemeStateLabel(ApplicationTheme theme)
        {
            if (ThemeStateLabel != null)
            {
                ThemeStateLabel.Text = string.Format(CultureInfo.CurrentCulture, "Current: {0}", theme);
            }
        }

        private static void ApplyCaptionVisibility(
            System.Windows.Controls.ComboBox combo,
            Action<Visibility> setVisibility,
            Action<bool> setEnabled)
        {
            var item = combo != null ? combo.SelectedItem as ComboBoxItem : null;
            var content = item != null ? item.Content as string : null;

            if (string.Equals(content, "Hidden", StringComparison.Ordinal))
            {
                setVisibility(Visibility.Hidden);
                setEnabled(false);
            }
            else if (string.Equals(content, "Collapsed", StringComparison.Ordinal) ||
                string.Equals(content, "Hide", StringComparison.Ordinal))
            {
                setVisibility(Visibility.Collapsed);
                setEnabled(false);
            }
            else if (string.Equals(content, "Disable", StringComparison.Ordinal))
            {
                setVisibility(Visibility.Visible);
                setEnabled(false);
            }
            else
            {
                setVisibility(Visibility.Visible);
                setEnabled(true);
            }
        }
    }
}
