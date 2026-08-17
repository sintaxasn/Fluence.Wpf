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

using Fluence.Wpf.Native;
using System.Windows;
using Windows.Win32;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Pins the pure, handle-free interop selectors in <see cref="NativeMethods"/>:
    /// the immersive dark-mode attribute split (19 vs 20), the auto-hide taskbar
    /// maximized-rect shift, and the maximized resize-frame margin conversion. These tests
    /// are deterministic and OS-independent; they do not call any P/Invoke whose result
    /// depends on the host environment, so the live
    /// <see cref="NativeMethods.GetMaximizedFrameMargin(double, double)"/> path is not covered here.
    /// </summary>
    public sealed class NativeMethodsTests
    {
        private const double Tolerance = 1e-9;

        [Fact]
        public void GetImmersiveDarkModeAttribute_Returns20_For18985AndLater()
        {
            Assert.Equal(DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, NativeMethods.GetImmersiveDarkModeAttribute(18985));
            Assert.Equal(DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, NativeMethods.GetImmersiveDarkModeAttribute(19041));
            Assert.Equal(DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, NativeMethods.GetImmersiveDarkModeAttribute(22000));
            Assert.Equal(DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, NativeMethods.GetImmersiveDarkModeAttribute(22631));
        }

        [Fact]
        public void GetImmersiveDarkModeAttribute_Returns19_ForPre18985Builds()
        {
            const DWMWINDOWATTRIBUTE DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = (DWMWINDOWATTRIBUTE)19;
            Assert.Equal(DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, NativeMethods.GetImmersiveDarkModeAttribute(17763));
            Assert.Equal(DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, NativeMethods.GetImmersiveDarkModeAttribute(18000));
            Assert.Equal(DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, NativeMethods.GetImmersiveDarkModeAttribute(18361));
            Assert.Equal(DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, NativeMethods.GetImmersiveDarkModeAttribute(18362));
            Assert.Equal(DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, NativeMethods.GetImmersiveDarkModeAttribute(18363));
            Assert.Equal(DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, NativeMethods.GetImmersiveDarkModeAttribute(18984));
        }

        [Fact]
        public void ApplyAutoHideTaskbarShift_Left_MovesRightAndShrinksWidth()
        {
            MINMAXINFO mmi = SeedMinMaxInfo();
            NativeMethods.ApplyAutoHideTaskbarShift(ref mmi, PInvoke.ABE_LEFT);

            Assert.Equal(102, mmi.ptMaxPosition.X);
            Assert.Equal(200, mmi.ptMaxPosition.Y);
            Assert.Equal(798, mmi.ptMaxSize.X);
            Assert.Equal(600, mmi.ptMaxSize.Y);
        }

        [Fact]
        public void ApplyAutoHideTaskbarShift_Top_MovesDownAndShrinksHeight()
        {
            MINMAXINFO mmi = SeedMinMaxInfo();
            NativeMethods.ApplyAutoHideTaskbarShift(ref mmi, PInvoke.ABE_TOP);

            Assert.Equal(100, mmi.ptMaxPosition.X);
            Assert.Equal(202, mmi.ptMaxPosition.Y);
            Assert.Equal(800, mmi.ptMaxSize.X);
            Assert.Equal(598, mmi.ptMaxSize.Y);
        }

        [Fact]
        public void ApplyAutoHideTaskbarShift_Right_ShrinksWidthOnly()
        {
            MINMAXINFO mmi = SeedMinMaxInfo();
            NativeMethods.ApplyAutoHideTaskbarShift(ref mmi, PInvoke.ABE_RIGHT);

            Assert.Equal(100, mmi.ptMaxPosition.X);
            Assert.Equal(200, mmi.ptMaxPosition.Y);
            Assert.Equal(798, mmi.ptMaxSize.X);
            Assert.Equal(600, mmi.ptMaxSize.Y);
        }

        [Fact]
        public void ApplyAutoHideTaskbarShift_Bottom_ShrinksHeightOnly()
        {
            MINMAXINFO mmi = SeedMinMaxInfo();
            NativeMethods.ApplyAutoHideTaskbarShift(ref mmi, PInvoke.ABE_BOTTOM);

            Assert.Equal(100, mmi.ptMaxPosition.X);
            Assert.Equal(200, mmi.ptMaxPosition.Y);
            Assert.Equal(800, mmi.ptMaxSize.X);
            Assert.Equal(598, mmi.ptMaxSize.Y);
        }

        [Fact]
        public void ApplyAutoHideTaskbarShift_UnrecognizedEdge_LeavesRectUnchanged()
        {
            MINMAXINFO mmi = SeedMinMaxInfo();
            NativeMethods.ApplyAutoHideTaskbarShift(ref mmi, 99);

            Assert.Equal(100, mmi.ptMaxPosition.X);
            Assert.Equal(200, mmi.ptMaxPosition.Y);
            Assert.Equal(800, mmi.ptMaxSize.X);
            Assert.Equal(600, mmi.ptMaxSize.Y);
        }

        [Fact]
        public void ComputeMaximizedFrameMargin_At100Percent_SumsSizeFrameAndPaddedBorder()
        {
            Thickness margin = NativeMethods.ComputeMaximizedFrameMargin(4, 4, 4, 1.0, 1.0);

            Assert.Equal(8.0, margin.Left);
            Assert.Equal(8.0, margin.Top);
            Assert.Equal(8.0, margin.Right);
            Assert.Equal(8.0, margin.Bottom);
        }

        [Fact]
        public void ComputeMaximizedFrameMargin_TakesHorizontalFromXAndVerticalFromYMetrics()
        {
            Thickness margin = NativeMethods.ComputeMaximizedFrameMargin(4, 7, 4, 1.0, 1.0);

            Assert.Equal(8.0, margin.Left);
            Assert.Equal(8.0, margin.Right);
            Assert.Equal(11.0, margin.Top);
            Assert.Equal(11.0, margin.Bottom);
        }

        [Fact]
        public void ComputeMaximizedFrameMargin_At150Percent_DividesByScale()
        {
            Thickness margin = NativeMethods.ComputeMaximizedFrameMargin(4, 4, 4, 1.5, 1.5);

            const double expected = 16.0 / 3.0;
            Assert.Equal(expected, margin.Left, Tolerance);
            Assert.Equal(expected, margin.Top, Tolerance);
            Assert.Equal(expected, margin.Right, Tolerance);
            Assert.Equal(expected, margin.Bottom, Tolerance);
        }

        [Fact]
        public void ComputeMaximizedFrameMargin_ScalesEachAxisIndependently()
        {
            Thickness margin = NativeMethods.ComputeMaximizedFrameMargin(4, 4, 4, 2.0, 1.0);

            Assert.Equal(4.0, margin.Left);
            Assert.Equal(4.0, margin.Right);
            Assert.Equal(8.0, margin.Top);
            Assert.Equal(8.0, margin.Bottom);
        }

        [Theory]
        [InlineData(0.0, 0.0)]
        [InlineData(-1.5, -2.0)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        public void ComputeMaximizedFrameMargin_NonPositiveScale_TreatedAsUnscaled(double dpiScaleX, double dpiScaleY)
        {
            Thickness margin = NativeMethods.ComputeMaximizedFrameMargin(4, 4, 4, dpiScaleX, dpiScaleY);

            Assert.Equal(8.0, margin.Left);
            Assert.Equal(8.0, margin.Top);
            Assert.Equal(8.0, margin.Right);
            Assert.Equal(8.0, margin.Bottom);
        }

        [Fact]
        public void ComputeMaximizedFrameMargin_GuardsOnlyTheFailingAxis()
        {
            Thickness margin = NativeMethods.ComputeMaximizedFrameMargin(4, 4, 4, 2.0, 0.0);

            Assert.Equal(4.0, margin.Left);
            Assert.Equal(4.0, margin.Right);
            Assert.Equal(8.0, margin.Top);
            Assert.Equal(8.0, margin.Bottom);
        }

        private static MINMAXINFO SeedMinMaxInfo()
        {
            MINMAXINFO mmi = default;
            mmi.ptMaxPosition.X = 100;
            mmi.ptMaxPosition.Y = 200;
            mmi.ptMaxSize.X = 800;
            mmi.ptMaxSize.Y = 600;
            return mmi;
        }
    }
}
