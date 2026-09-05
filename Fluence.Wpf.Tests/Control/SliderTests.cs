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

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Fluent <see cref="Slider"/> control: thumb scale animations.
    /// WinUI canonical: hover 1.167, pressed 0.86, ControlFastOutSlowIn easing.
    /// </summary>
    public sealed class SliderTests : IClassFixture<LightThemeFixture>
    {
        public SliderTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        // ---------------------------------------------------------------------------
        // WI-3 B11  Slider thumb scale
        // ---------------------------------------------------------------------------

        [Fact]
        public Task Slider_StyleApplies_PartTrackFoundAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Slider slider = new() { Value = 50, Minimum = 0, Maximum = 100 };
                Window w = new() { Content = slider, Width = 300, Height = 60 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Track track = Assert.IsType<Track>(FindVisualChildByName<Track>(slider, "PART_Track"), exactMatch: false);
                w.Close();
            });
        }

        [Fact]
        public Task Slider_DefaultState_ThumbInnerDotScaleIsRestValueAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Slider slider = new() { Value = 50, Minimum = 0, Maximum = 100 };
                Window w = new() { Content = slider, Width = 300, Height = 60 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // Only the inner dot carries the ScaleTransform named ThumbScale (WinUI 3 scales the
                // inner dot, not the fixed outer capsule); its rest value is 0.86, not 1.0
                // (Slider_themeresources.xaml: "0.86 is relative scale from 14px to 12px").
                Thumb thumb = Assert.IsType<Thumb>(FindVisualChild<Thumb>(slider), exactMatch: false);

                Ellipse innerDot = Assert.IsType<Ellipse>(FindVisualChildByName<Ellipse>(thumb, "ThumbInnerDot"), exactMatch: false);

                ScaleTransform scale = Assert.IsType<ScaleTransform>(innerDot.RenderTransform);
                Assert.Equal(0.86, scale.ScaleX, 0.001);
                Assert.Equal(0.86, scale.ScaleY, 0.001);
                w.Close();
            });
        }

        [Fact]
        public Task Slider_ThumbTemplate_HasEllipseAndInnerDotAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Slider slider = new() { Value = 30, Minimum = 0, Maximum = 100 };
                Window w = new() { Content = slider, Width = 300, Height = 60 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Ellipse thumbEllipse = Assert.IsType<Ellipse>(FindVisualChildByName<Ellipse>(slider, "ThumbEllipse"), exactMatch: false);
                Ellipse innerDot = Assert.IsType<Ellipse>(FindVisualChildByName<Ellipse>(slider, "ThumbInnerDot"), exactMatch: false);

                w.Close();
            });
        }

        [Fact]
        public Task Slider_Template_HasTrackAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Slider slider = new() { Width = 220, Minimum = 0, Maximum = 100, Value = 30 };

                try
                {
                    window.Content = slider;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.NotNull(slider.Template.FindName("PART_Track", slider));
                }
                finally
                {
                    window.Close();
                }
            });
        }
    }
}
