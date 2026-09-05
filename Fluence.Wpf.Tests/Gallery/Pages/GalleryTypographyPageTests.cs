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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Gallery.Pages
{
    /// <summary>
    /// Covers <see cref="GalleryTypographyPage"/>.
    /// </summary>
    public sealed class GalleryTypographyPageTests : IAsyncLifetime
    {
        private Window? _host;
        private GalleryTypographyPage? _page;

        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(() =>
            {
                _ = TestApp.EnsureDemoTheme();
                _page = new GalleryTypographyPage();
                _host = DemoTestHost.CreateHostWindow(_page);
            }));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(() =>
            {
                if (_host is not null)
                {
                    DemoTestHost.CloseWindow(_host);
                    _host = null;
                }

                _page = null;
            }));
        }

        [Fact]
        public Task GalleryTypographyPage_TableUsesCompactRowSpacingAsync()
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                GalleryTypographyPage page = _page ?? throw new InvalidOperationException("Page was not initialized.");

                Grid table = Assert.IsType<Grid>(DemoTestHost.FindByName<Grid>(page, "TypographyTable"), exactMatch: false);

                TextBlock firstBodyCell = Assert.IsType<TextBlock>(table.Children
                    .OfType<TextBlock>().FirstOrDefault(static textBlock => Grid.GetRow(textBlock) is 1 && Grid.GetColumn(textBlock) is 0), exactMatch: false);
                Assert.Equal(new Thickness(24, 8, 16, 8), firstBodyCell.Margin);

                Border firstShadedRow = Assert.IsType<Border>(table.Children
                    .OfType<Border>().FirstOrDefault(static border => Grid.GetRow(border) is 1), exactMatch: false);
                Assert.Equal(new Thickness(0, 2, 0, 2), firstShadedRow.Margin);
            });
        }

        [Fact]
        public Task GalleryTypographyPage_TypeRampSampleCarriesSourceAndCopyColumnAsync()
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                GalleryTypographyPage page = _page ?? throw new InvalidOperationException("Page was not initialized.");

                List<DemoSampleControl> samples = [.. DemoTestHost.FindVisualChildren<DemoSampleControl>(page)];
                DemoSampleControl sample = Assert.Single(samples);
                Assert.Equal("Type ramp", sample.SampleDescription, StringComparer.Ordinal);
                Assert.Contains("CaptionTextBlockStyle", sample.XamlSource, StringComparison.Ordinal);
                Assert.Contains("DisplayTextBlockStyle", sample.XamlSource, StringComparison.Ordinal);

                Grid table = Assert.IsType<Grid>(DemoTestHost.FindByName<Grid>(page, "TypographyTable"), exactMatch: false);

                List<Controls.Button> copyButtons = [.. DemoTestHost.FindVisualChildren<Controls.Button>(table)];
                Assert.NotEmpty(copyButtons);
                Assert.True(copyButtons.Exists(static button => "BodyTextBlockStyle".Equals(button.Tag as string, StringComparison.Ordinal)),
                    "Typography table should keep per-row style-key copy actions.");
            });
        }
    }
}
