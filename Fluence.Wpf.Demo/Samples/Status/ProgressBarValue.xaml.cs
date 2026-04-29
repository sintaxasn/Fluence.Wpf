using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Samples.Status
{
    public partial class ProgressBarValue : UserControl
    {
        public ProgressBarValue()
        {
            InitializeComponent();
        }

        private void ProgressSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (ProgressSlider == null)
            {
                return;
            }

            if (SliderValueLabel != null)
            {
                SliderValueLabel.Text = string.Format("Value: {0:0}", ProgressSlider.Value);
            }

            if (StandardProgressBar != null)
            {
                StandardProgressBar.Value = ProgressSlider.Value;
            }
        }
    }
}
