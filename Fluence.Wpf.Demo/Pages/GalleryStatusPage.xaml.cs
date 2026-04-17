/*
 * Copyright 2026 Dan Cunningham
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryStatusPage : UserControl
    {
        public GalleryStatusPage()
        {
            InitializeComponent();
            Loaded += GalleryStatusPage_Loaded;
        }

        private void GalleryStatusPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= GalleryStatusPage_Loaded;
            ProgressSlider_ValueChanged(null, null);
            IndeterminateToggle_Toggled(null, null);
            ProgressRingSlider_ValueChanged(null, null);
        }

        private void IndeterminateToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded || IndeterminateProgressBar == null || IndeterminateToggle == null)
            {
                return;
            }

            IndeterminateProgressBar.ProgressMode = IndeterminateToggle.IsChecked == true
                ? ProgressBarMode.Indeterminate
                : ProgressBarMode.Standard;
        }

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
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

        private void ProgressRingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DeterminateProgressRing != null && ProgressRingSlider != null)
            {
                DeterminateProgressRing.Value = ProgressRingSlider.Value;
            }
        }

        private void ProgressStep_Click(object sender, RoutedEventArgs e)
        {
            if (StepProgressBar == null)
            {
                return;
            }

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

        private void ResetInfoBars_Click(object sender, RoutedEventArgs e)
        {
            InfoBarInformational.IsOpen = true;
            InfoBarSuccess.IsOpen = true;
            InfoBarWarning.IsOpen = true;
            InfoBarError.IsOpen = true;
        }
    }
}
