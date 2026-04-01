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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Fluence.Wpf;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo
{
    public partial class MainWindow : FluentWindow
    {
        private bool _isSyncingTheme;
        private int _addCounter;
        private const string DefaultTitle = "Fluence.Wpf — Monolithic Gallery";

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            SystemThemeWatcher.Watch(this);
            ApplicationThemeManager.Apply(ApplicationTheme.Auto, BackdropType.Mica, true);
            ApplicationThemeManager.Changed += ApplicationThemeManager_Changed;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= MainWindow_Loaded;
            SyncThemeRadioButtons();
            DefaultToggle_Changed(null, null);
            UpdateFontDetectionLabel();
            ProgressSlider_ValueChanged(null, null);
            WindowChromeToggle_Changed(null, null);
            InlineBackEnabledToggle_Changed(null, null);
            UpdateStatusBar();
        }

        private void ApplicationThemeManager_Changed(object sender, ThemeChangedEventArgs e)
        {
            SyncThemeRadioButtons();
            UpdateStatusBar();
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

            ApplicationThemeManager.Apply(theme, WindowBackdrop, true);
            UpdateThemeStateLabel(ApplicationThemeManager.CurrentTheme);
            UpdateStatusBar();
        }

        private void BackdropCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
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

            WindowBackdrop = backdrop;
            ApplicationThemeManager.Apply(ApplicationThemeManager.CurrentTheme, backdrop, false);
            UpdateStatusBar();
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
                    StatusAccentLabel.Text = string.Format("Accent: {0}", hex);
                }
            }
            catch (FormatException)
            {
            }

            UpdateStatusBar();
        }

        private void SystemAccent_Click(object sender, RoutedEventArgs e)
        {
            ApplicationAccentColorManager.ApplySystemAccent();
            StatusAccentLabel.Text = "Accent: System";
            UpdateStatusBar();
        }

        private void SystemThemeWatcher_Toggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            if (ThemeWatcherToggle.IsChecked == true)
            {
                SystemThemeWatcher.Watch(this);
                SystemThemeLabel.Text = "Watching: Yes";
            }
            else
            {
                SystemThemeWatcher.UnWatch(this);
                SystemThemeLabel.Text = "Watching: No";
            }
        }

        private void CharCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CharCountLabel == null || CharCountTextBox == null)
            {
                return;
            }

            var len = CharCountTextBox.Text != null ? CharCountTextBox.Text.Length : 0;
            CharCountLabel.Text = string.Format("Characters: {0}", len);
        }

        private void ThemeEnumCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeEnumCombo == null)
            {
                return;
            }

            // Keep exact index mapping expected by tests and prior demo behavior.
            if (ThemeEnumCombo.SelectedIndex == 0)
            {
                ThemeEnumCombo.SelectedIndex = 1;
            }
        }

        private void DefaultToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (ToggleStateLabel == null || DefaultToggle == null)
            {
                return;
            }

            ToggleStateLabel.Text = string.Format(
                "Default toggle: {0}",
                DefaultToggle.IsChecked == true ? "On" : "Off");
        }

        private void IndeterminateToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded || IndeterminateProgressBar == null)
            {
                return;
            }

            IndeterminateProgressBar.ProgressMode = IndeterminateToggle.IsChecked == true
                ? ProgressBarMode.Indeterminate
                : ProgressBarMode.Standard;
        }

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SliderValueLabel == null || ProgressSlider == null)
            {
                return;
            }

            SliderValueLabel.Text = string.Format("Value: {0:0}", ProgressSlider.Value);
        }

        private void ProgressStep_Click(object sender, RoutedEventArgs e)
        {
            if (StepProgressBar == null)
            {
                return;
            }

            var button = sender as FrameworkElement;
            var tag = button != null && button.Tag != null ? button.Tag.ToString() : string.Empty;

            if (string.Equals(tag, "Next", StringComparison.OrdinalIgnoreCase))
            {
                if (StepProgressBar.CurrentStep < StepProgressBar.Steps)
                {
                    StepProgressBar.CurrentStep++;
                }
            }
            else if (StepProgressBar.CurrentStep > 0)
            {
                StepProgressBar.CurrentStep--;
            }

            StepLabel.Text = string.Format("Step {0} of {1}", StepProgressBar.CurrentStep, StepProgressBar.Steps);
        }

        private void ResetInfoBars_Click(object sender, RoutedEventArgs e)
        {
            InfoBarInformational.IsOpen = true;
            InfoBarSuccess.IsOpen = true;
            InfoBarWarning.IsOpen = true;
            InfoBarError.IsOpen = true;
        }

        private void AddListItem_Click(object sender, RoutedEventArgs e)
        {
            if (DemoListView == null)
            {
                return;
            }

            _addCounter++;
            DemoListView.Items.Add(new ListViewItem { Content = string.Format("New Item #{0}", _addCounter) });

            if (DemoListViewDetail != null)
            {
                var item = new ListViewItem();
                item.Tag = string.Format("New Item #{0}|Animated subtitle", _addCounter);
                item.Content = BuildDetailListItemContent(string.Format("New Item #{0}", _addCounter), "Animated subtitle");
                DemoListViewDetail.Items.Add(item);
            }
        }

        private void RemoveListItem_Click(object sender, RoutedEventArgs e)
        {
            if (DemoListView == null || DemoListView.Items.Count == 0)
            {
                return;
            }

            var lastItem = DemoListView.Items[DemoListView.Items.Count - 1];
            DemoListView.AnimateRemove(lastItem, null);

            if (DemoListViewDetail != null && DemoListViewDetail.Items.Count > 0)
            {
                var detailItem = DemoListViewDetail.Items[DemoListViewDetail.Items.Count - 1];
                DemoListViewDetail.AnimateRemove(detailItem, null);
            }
        }

        private void InlineNavTopDemo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InlineNavTopDemo == null || InlineNavTopLabel == null || InlineNavTopContentHost == null)
            {
                return;
            }

            var item = TryGetSelectedNavigationViewItem(InlineNavTopDemo);
            var selectedLabel = item != null ? item.Content as string : "Home";
            InlineNavTopLabel.Text = string.Format("Top: {0}", selectedLabel);
            AnimateContentHost(InlineNavTopContentHost);
        }

        private void InlineNavLeftDemo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InlineNavLeftDemo == null || InlineNavLeftLabel == null || InlineNavLeftContentHost == null)
            {
                return;
            }

            var item = TryGetSelectedNavigationViewItem(InlineNavLeftDemo);
            var selectedLabel = item != null ? item.Content as string : "Files";
            InlineNavLeftLabel.Text = string.Format("Left: {0}", selectedLabel);
            AnimateContentHost(InlineNavLeftContentHost);
        }

        private static NavigationViewItem TryGetSelectedNavigationViewItem(NavigationView nav)
        {
            if (nav == null)
            {
                return null;
            }

            var direct = nav.SelectedItem as NavigationViewItem;
            if (direct != null)
            {
                return direct;
            }

            if (nav.SelectedItem == null)
            {
                return null;
            }

            var container = nav.ItemContainerGenerator.ContainerFromItem(nav.SelectedItem) as NavigationViewItem;
            return container;
        }

        private void InlineBackEnabledToggle_Changed(object sender, RoutedEventArgs e)
        {
            var enabled = InlineBackEnabledToggle != null && InlineBackEnabledToggle.IsChecked == true;

            if (InlineNavTopDemo != null)
            {
                InlineNavTopDemo.IsBackEnabled = enabled;
            }

            if (InlineNavLeftDemo != null)
            {
                InlineNavLeftDemo.IsBackEnabled = enabled;
            }

            if (InlineBackStatusLabel != null)
            {
                InlineBackStatusLabel.Text = enabled ? "Back button enabled" : "Back button disabled";
            }
        }

        private void CaptionOverrideCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            MinimizeButtonOverride = GetCaptionOverrideFromCombo(MinimizeOverrideCombo);
            MaximizeButtonOverride = GetCaptionOverrideFromCombo(MaximizeOverrideCombo);
            CloseButtonOverride = GetCaptionOverrideFromCombo(CloseOverrideCombo);
        }

        private void WindowChromeToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (ShowWindowTitleToggle != null)
            {
                Title = ShowWindowTitleToggle.IsChecked == true ? DefaultTitle : string.Empty;
            }

            if (ShowWindowIconToggle != null)
            {
                Icon = ShowWindowIconToggle.IsChecked == true
                    ? new BitmapImage(new Uri("pack://application:,,,/Fluence.Wpf.Demo;component/Resources/AppIcon.png"))
                    : null;
            }
        }

        private void FontIconDemo_Click(object sender, RoutedEventArgs e)
        {
            if (ClickableFontIcon == null)
            {
                return;
            }

            var animation = new DoubleAnimation
            {
                By = 180,
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            ClickableFontIcon.BeginAnimation(FontIcon.RotationProperty, animation);
        }

        private void SyncThemeRadioButtons()
        {
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
                UpdateStatusBar();
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
                ThemeStateLabel.Text = string.Format("Current: {0}", theme);
            }
        }

        private void UpdateStatusBar()
        {
            if (StatusThemeLabel != null)
            {
                StatusThemeLabel.Text = string.Format("Theme: {0}", ApplicationThemeManager.CurrentTheme);
            }

            if (StatusBackdropLabel != null)
            {
                StatusBackdropLabel.Text = string.Format("Backdrop: {0}", WindowBackdrop);
            }
        }

        private void InitializeNavigationViewDemos()
        {
            // Intentionally left without preselection to avoid NavigationView template timing crashes.
        }

        private static CaptionButtonOverride GetCaptionOverrideFromCombo(System.Windows.Controls.ComboBox combo)
        {
            if (combo == null || combo.SelectedItem == null)
            {
                return CaptionButtonOverride.Default;
            }

            var item = combo.SelectedItem as ComboBoxItem;
            var content = item != null ? item.Content as string : null;
            if (string.Equals(content, "Enable", StringComparison.Ordinal))
            {
                return CaptionButtonOverride.Enable;
            }

            if (string.Equals(content, "Disable", StringComparison.Ordinal))
            {
                return CaptionButtonOverride.Disable;
            }

            if (string.Equals(content, "Hide", StringComparison.Ordinal))
            {
                return CaptionButtonOverride.Hide;
            }

            return CaptionButtonOverride.Default;
        }

        private static UIElement BuildDetailListItemContent(string header, string subtitle)
        {
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var image = new Image
            {
                Width = 28,
                Height = 28,
                Source = new BitmapImage(new Uri("pack://application:,,,/Fluence.Wpf.Demo;component/Resources/AppIcon.png"))
            };
            Grid.SetColumn(image, 0);

            var stack = new System.Windows.Controls.StackPanel();
            stack.Children.Add(new TextBlock { Text = header, FontWeight = FontWeights.SemiBold });
            stack.Children.Add(new TextBlock { Text = subtitle, FontSize = 12 });
            Grid.SetColumn(stack, 1);

            grid.Children.Add(image);
            grid.Children.Add(stack);
            return grid;
        }

        private static void AnimateContentHost(FrameworkElement host)
        {
            if (host == null)
            {
                return;
            }

            var transform = host.RenderTransform as TranslateTransform;
            if (transform == null)
            {
                transform = new TranslateTransform();
                host.RenderTransform = transform;
            }

            var slide = new DoubleAnimation
            {
                From = 14,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var fade = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(180)
            };

            transform.BeginAnimation(TranslateTransform.XProperty, slide);
            host.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        private void UpdateFontDetectionLabel()
        {
            if (FontDetectionLabel == null)
            {
                return;
            }

            FontDetectionLabel.Text = FontFamily != null
                ? string.Format("FontFamily: {0}", FontFamily.Source)
                : "FontFamily: (unknown)";
        }

        protected override void OnClosed(EventArgs e)
        {
            ApplicationThemeManager.Changed -= ApplicationThemeManager_Changed;
            SystemThemeWatcher.UnWatch(this);
            base.OnClosed(e);
        }
    }
}
