using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Samples.Menus
{
    public partial class MenuBar : UserControl
    {
        public MenuBar()
        {
            InitializeComponent();
        }

        private void MenuBar_Click(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            var action = element != null ? element.Tag as string : null;
            MenuBarResultLabel.Text = string.Format("Last menu action: {0}", string.IsNullOrEmpty(action) ? "None" : action);
        }
    }
}
