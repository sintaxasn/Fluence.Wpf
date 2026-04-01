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
using System.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public class CaptionButtonChromeTests
    {
        [TestMethod]
        public void Minimize_Default_NoResize_Hides()
        {
            CaptionButtonChrome.GetMinimizeChrome(
                ResizeMode.NoResize,
                CaptionButtonOverride.Default,
                out var vis,
                out var en);

            Assert.AreEqual(Visibility.Collapsed, vis);
            Assert.IsFalse(en);
        }

        [TestMethod]
        public void Minimize_Enable_OverridesNoResize()
        {
            CaptionButtonChrome.GetMinimizeChrome(
                ResizeMode.NoResize,
                CaptionButtonOverride.Enable,
                out var vis,
                out var en);

            Assert.AreEqual(Visibility.Visible, vis);
            Assert.IsTrue(en);
        }

        [TestMethod]
        public void MaximizeRestore_Default_CanResize_Normal_ShowsMaximizeOnly()
        {
            CaptionButtonChrome.GetMaximizeRestoreChrome(
                ResizeMode.CanResize,
                WindowState.Normal,
                CaptionButtonOverride.Default,
                out var maxVis,
                out var restVis,
                out var maxEn,
                out var restEn);

            Assert.AreEqual(Visibility.Visible, maxVis);
            Assert.AreEqual(Visibility.Collapsed, restVis);
            Assert.IsTrue(maxEn);
            Assert.IsFalse(restEn);
        }

        [TestMethod]
        public void MaximizeRestore_Default_CanResize_Maximized_ShowsRestoreOnly()
        {
            CaptionButtonChrome.GetMaximizeRestoreChrome(
                ResizeMode.CanResize,
                WindowState.Maximized,
                CaptionButtonOverride.Default,
                out var maxVis,
                out var restVis,
                out var maxEn,
                out var restEn);

            Assert.AreEqual(Visibility.Collapsed, maxVis);
            Assert.AreEqual(Visibility.Visible, restVis);
            Assert.IsFalse(maxEn);
            Assert.IsTrue(restEn);
        }

        [TestMethod]
        public void MaximizeRestore_Default_CanMinimize_DisablesBoth()
        {
            CaptionButtonChrome.GetMaximizeRestoreChrome(
                ResizeMode.CanMinimize,
                WindowState.Normal,
                CaptionButtonOverride.Default,
                out var maxVis,
                out var restVis,
                out var maxEn,
                out var restEn);

            Assert.AreEqual(Visibility.Visible, maxVis);
            Assert.IsFalse(maxEn);
            Assert.IsFalse(restEn);
        }

        [TestMethod]
        public void Close_Default_VisibleAndEnabled()
        {
            CaptionButtonChrome.GetCloseChrome(
                CaptionButtonOverride.Default,
                out var vis,
                out var en);

            Assert.AreEqual(Visibility.Visible, vis);
            Assert.IsTrue(en);
        }

        [TestMethod]
        public void Close_Hide_Collapsed()
        {
            CaptionButtonChrome.GetCloseChrome(
                CaptionButtonOverride.Hide,
                out var vis,
                out var en);

            Assert.AreEqual(Visibility.Collapsed, vis);
            Assert.IsFalse(en);
        }
    }
}
