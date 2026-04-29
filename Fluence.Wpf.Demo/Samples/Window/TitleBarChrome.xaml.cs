using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo.Samples.Window
{
    public partial class TitleBarChrome : UserControl
    {
        public TitleBarChrome()
        {
            InitializeComponent();
        }

        private FluenceWindow HostFluenceWindow
        {
            get { return System.Windows.Window.GetWindow(this) as FluenceWindow; }
        }

        private void TitleBarToggle_Changed(object sender, RoutedEventArgs e)
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

            if (ExtendsContentToggle != null)
            {
                var extends = ExtendsContentToggle.IsChecked == true;
                window.ExtendsContentIntoTitleBar = extends;
                window.TitleBarLeftIndent = extends ? 48d : 0d;
            }

            if (HasShadowToggle != null)
            {
                window.HasShadow = HasShadowToggle.IsChecked == true;
            }
        }

        private void TitleBarHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var window = HostFluenceWindow;
            if (window != null)
            {
                window.TitleBarHeight = e.NewValue;
            }

            TitleBarHeightLabel.Text = ((int)e.NewValue).ToString();
        }
    }
}
