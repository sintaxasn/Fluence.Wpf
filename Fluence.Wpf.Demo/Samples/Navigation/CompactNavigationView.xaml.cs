using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Samples.Navigation
{
    public partial class CompactNavigationView : UserControl
    {
        private int _backRequestCount;

        public CompactNavigationView()
        {
            InitializeComponent();
        }

        private void BackEnabledToggle_Changed(object sender, RoutedEventArgs e)
        {
            UpdateBackState();
        }

        private void CompactNavigationDemo_BackRequested(object sender, Fluence.Wpf.Controls.NavigationViewBackRequestedEventArgs e)
        {
            _backRequestCount++;
            UpdateBackState();
        }

        private void UpdateBackState()
        {
            var isBackEnabled = BackEnabledToggle != null && BackEnabledToggle.IsChecked == true;

            if (CompactNavigationDemo != null)
            {
                CompactNavigationDemo.IsBackEnabled = isBackEnabled;
            }

            if (BackStatusLabel != null)
            {
                BackStatusLabel.Text = isBackEnabled
                    ? string.Format("Back button enabled ({0} requests)", _backRequestCount)
                    : "Back button disabled";
            }
        }
    }
}
