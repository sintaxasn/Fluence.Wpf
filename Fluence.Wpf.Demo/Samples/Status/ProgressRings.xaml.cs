using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Samples.Status
{
    public partial class ProgressRings : UserControl
    {
        public ProgressRings()
        {
            InitializeComponent();
        }

        private void ProgressRingSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (DeterminateProgressRing != null && ProgressRingSlider != null)
            {
                DeterminateProgressRing.Value = ProgressRingSlider.Value;
            }
        }
    }
}
