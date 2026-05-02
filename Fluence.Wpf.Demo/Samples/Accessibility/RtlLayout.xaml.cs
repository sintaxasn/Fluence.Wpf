using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Samples.Accessibility
{
    public partial class RtlLayout : UserControl
    {
        public RtlLayout()
        {
            InitializeComponent();
        }

        private void RtlToggle_Changed(object sender, RoutedEventArgs e)
        {
            RtlDemoCard.FlowDirection = RtlToggle.IsChecked == true
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;
        }
    }
}
