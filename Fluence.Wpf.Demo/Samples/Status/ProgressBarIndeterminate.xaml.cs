using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf;

namespace Fluence.Wpf.Demo.Samples.Status
{
    public partial class ProgressBarIndeterminate : UserControl
    {
        public ProgressBarIndeterminate()
        {
            InitializeComponent();
        }

        private void IndeterminateToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (IndeterminateProgressBar == null || IndeterminateToggle == null)
            {
                return;
            }

            IndeterminateProgressBar.ProgressMode = IndeterminateToggle.IsChecked == true
                ? ProgressBarMode.Indeterminate
                : ProgressBarMode.Standard;
        }
    }
}
