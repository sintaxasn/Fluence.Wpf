using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Fluence.Wpf;

namespace Fluence.Wpf.Demo.Samples.Window
{
    public partial class ThemeAndAccent : UserControl
    {
        public ThemeAndAccent()
        {
            InitializeComponent();
        }

        private void ThemeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            var rb = sender as RadioButton;
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

            var host = System.Windows.Window.GetWindow(this) as Fluence.Wpf.Controls.FluenceWindow;
            var backdrop = host != null ? host.SystemBackdropType : BackdropType.Mica;
            ApplicationThemeManager.Apply(theme, backdrop, true);
            ThemeStateLabel.Text = string.Format("Current: {0}", theme);
        }

        private void SystemThemeWatcher_Toggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            var host = System.Windows.Window.GetWindow(this);
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

        private void AccentSwatch_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as FrameworkElement;
            var hex = button != null ? button.Tag as string : null;
            if (string.IsNullOrEmpty(hex))
            {
                return;
            }

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
    }
}
