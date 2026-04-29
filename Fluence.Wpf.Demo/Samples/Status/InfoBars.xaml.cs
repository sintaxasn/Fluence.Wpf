using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Samples.Status
{
    public partial class InfoBars : UserControl
    {
        public InfoBars()
        {
            InitializeComponent();
        }

        private void ResetInfoBars_Click(object sender, RoutedEventArgs e)
        {
            InfoBarInformational.IsOpen = true;
            InfoBarSuccess.IsOpen = true;
            InfoBarWarning.IsOpen = true;
            InfoBarError.IsOpen = true;
        }
    }
}
