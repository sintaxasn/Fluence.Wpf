using System;
using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo.Samples.Window
{
    public partial class BackdropAndCaptionButtons : UserControl
    {
        public BackdropAndCaptionButtons()
        {
            InitializeComponent();
        }

        private FluenceWindow HostFluenceWindow
        {
            get { return System.Windows.Window.GetWindow(this) as FluenceWindow; }
        }

        private void BackdropCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            var window = HostFluenceWindow;
            if (window == null)
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

            window.WindowBackdrop = backdrop;
            ApplicationThemeManager.Apply(ApplicationThemeManager.CurrentTheme, backdrop, false);
        }

        private void CaptionOverrideCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            var window = HostFluenceWindow;
            if (window == null)
            {
                return;
            }

            ApplyCaptionOverride(MinimizeOverrideCombo, v => window.MinimizeButtonVisibility = v, en => window.IsMinimizable = en);
            ApplyCaptionOverride(MaximizeOverrideCombo, v => window.MaximizeButtonVisibility = v, en => window.IsMaximizable = en);
            ApplyCaptionOverride(CloseOverrideCombo, v => window.CloseButtonVisibility = v, en => window.IsClosable = en);
        }

        private void WindowChromeToggle_Changed(object sender, RoutedEventArgs e)
        {
            var window = HostFluenceWindow;
            if (window == null)
            {
                return;
            }

            window.ShowIcon = ShowWindowIconToggle != null && ShowWindowIconToggle.IsChecked == true;
            window.ShowTitle = ShowWindowTitleToggle != null && ShowWindowTitleToggle.IsChecked == true;
        }

        private static void ApplyCaptionOverride(
            ComboBox combo,
            Action<Visibility> setVisibility,
            Action<bool> setEnabled)
        {
            var item = combo != null ? combo.SelectedItem as ComboBoxItem : null;
            var content = item != null ? item.Content as string : null;

            if (string.Equals(content, "Hide", StringComparison.Ordinal))
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
