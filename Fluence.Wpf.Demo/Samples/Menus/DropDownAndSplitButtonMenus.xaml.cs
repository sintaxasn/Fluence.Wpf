using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Samples.Menus
{
    public partial class DropDownAndSplitButtonMenus : UserControl
    {
        public DropDownAndSplitButtonMenus()
        {
            InitializeComponent();
        }

        private void ExportPrimary_Click(object sender, RoutedEventArgs e)
        {
            FlyoutResultLabel.Text = "Last action: Export - Default";
        }

        private void FlyoutAction_Click(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            var action = element != null ? element.Tag as string : null;
            FlyoutResultLabel.Text = string.Format("Last action: {0}", string.IsNullOrEmpty(action) ? "None" : action);
        }
    }
}
