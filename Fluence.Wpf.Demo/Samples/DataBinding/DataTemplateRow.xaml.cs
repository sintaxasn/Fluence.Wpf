using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Samples.DataBinding
{
    public partial class DataTemplateRow : UserControl
    {
        public DataTemplateRow()
        {
            InitializeComponent();

            DataTemplateListView.ItemsSource = new ObservableCollection<DataBindingTemplateItem>
            {
                new DataBindingTemplateItem { Name = "Release notes", AddedAt = DateTime.Now.ToString("HH:mm:ss") },
                new DataBindingTemplateItem { Name = "Design tokens", AddedAt = DateTime.Now.ToString("HH:mm:ss") },
                new DataBindingTemplateItem { Name = "Control states", AddedAt = DateTime.Now.ToString("HH:mm:ss") }
            };
        }
    }

    public sealed class DataBindingTemplateItem
    {
        public string Name { get; set; }

        public string AddedAt { get; set; }
    }
}
