using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Samples.Menus
{
    public partial class ContextMenuActions : UserControl
    {
        public ContextMenuActions()
        {
            InitializeComponent();
        }

        private void ContextMenu_Click(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            var action = element != null ? element.Tag as string : null;
            ContextMenuResultLabel.Text = string.Format("Last action: {0}", string.IsNullOrEmpty(action) ? "None" : action);
        }
    }
}
