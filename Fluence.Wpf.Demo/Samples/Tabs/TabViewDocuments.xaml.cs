using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo.Samples.Tabs
{
    public partial class TabViewDocuments : UserControl
    {
        private int _nextDocumentNumber = 3;

        public TabViewDocuments()
        {
            InitializeComponent();
        }

        private void DemoTabView_AddTabButtonClick(object sender, RoutedEventArgs e)
        {
            var number = ++_nextDocumentNumber;
            var tab = new TabViewItem
            {
                Header = string.Format("Document {0}", number),
                Icon = new FontIcon { Glyph = "\uE8A5", IconFontSize = 16 },
                Content = new TextBlock
                {
                    Margin = new Thickness(20),
                    Foreground = (System.Windows.Media.Brush)FindResource("TextFillColorSecondaryBrush"),
                    Text = string.Format("Fresh document {0} content.", number),
                    TextWrapping = TextWrapping.Wrap
                }
            };

            DemoTabView.Items.Add(tab);
            DemoTabView.SelectedItem = tab;
            UpdateStatus();
        }

        private void DemoTabView_TabCloseRequested(object sender, RoutedEventArgs e)
        {
            var args = e as TabViewTabCloseRequestedEventArgs;
            if (args == null || args.Tab == null)
            {
                return;
            }

            DemoTabView.Items.Remove(args.Tab);
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            DemoTabViewStatus.Text = string.Format("Tabs: {0}", DemoTabView.Items.Count);
        }
    }
}
