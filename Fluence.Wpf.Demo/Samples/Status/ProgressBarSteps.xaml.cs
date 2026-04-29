using System;
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Samples.Status
{
    public partial class ProgressBarSteps : UserControl
    {
        public ProgressBarSteps()
        {
            InitializeComponent();
        }

        private void ProgressStep_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as FrameworkElement;
            var tag = button != null && button.Tag != null ? button.Tag.ToString() : string.Empty;

            if (string.Equals(tag, "Next", StringComparison.OrdinalIgnoreCase))
            {
                if (StepProgressBar.CurrentStep < StepProgressBar.Steps)
                {
                    StepProgressBar.CurrentStep++;
                }
            }
            else if (StepProgressBar.CurrentStep > 0)
            {
                StepProgressBar.CurrentStep--;
            }

            StepLabel.Text = string.Format("Step {0} of {1}", StepProgressBar.CurrentStep, StepProgressBar.Steps);
        }
    }
}
