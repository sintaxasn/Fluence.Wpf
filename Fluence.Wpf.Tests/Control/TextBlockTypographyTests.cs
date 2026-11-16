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
using System.Windows.Data;
using System.Windows.Media;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Effective typography of the Fluence TextBlock wrapper.
    /// </summary>
    public sealed class TextBlockTypographyTests : IClassFixture<LightThemeFixture>
    {
        public TextBlockTypographyTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        [Theory]
        [InlineData(FluentTypography.Body, 14.0)]
        [InlineData(FluentTypography.Title, 28.0)]
        [InlineData(FluentTypography.TitleLarge, 40.0)]
        [InlineData(FluentTypography.Display, 68.0)]
        public Task NamedTypography_ControlsEffectiveFontSizeAndRenderedHeightAsync(FluentTypography typography, double expectedSize)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Controls.TextBlock body = new() { Text = "Fluence", Typography = FluentTypography.Body };
                Controls.TextBlock subject = new() { Text = "Fluence", Typography = typography };
                System.Windows.Controls.StackPanel panel = new();
                _ = panel.Children.Add(body);
                _ = panel.Children.Add(subject);
                Window window = new() { Content = panel, Width = 420, Height = 320 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.TextBlock bodyPart = Assert.IsType<System.Windows.Controls.TextBlock>(
                        FindVisualChildByName<System.Windows.Controls.TextBlock>(body, "PART_TextBlock"), exactMatch: false);
                    System.Windows.Controls.TextBlock subjectPart = Assert.IsType<System.Windows.Controls.TextBlock>(
                        FindVisualChildByName<System.Windows.Controls.TextBlock>(subject, "PART_TextBlock"), exactMatch: false);

                    Assert.Equal(14.0, bodyPart.FontSize);
                    Assert.Equal(expectedSize, subjectPart.FontSize);
                    Assert.True(subjectPart.ActualHeight > 0);
                    if (typography is not FluentTypography.Body)
                    {
                        Assert.True(subjectPart.ActualHeight > bodyPart.ActualHeight,
                            $"{typography} must render taller than Body, not merely select a style object.");
                    }
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ExplicitFontSizeAndBindingOverrideThenRestoreNamedRampAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.TextBlock subject = new()
                {
                    Text = "Fluence",
                    Typography = FluentTypography.Title,
                };
                Window window = new() { Content = subject, Width = 420, Height = 200 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    System.Windows.Controls.TextBlock part = Assert.IsType<System.Windows.Controls.TextBlock>(
                        FindVisualChildByName<System.Windows.Controls.TextBlock>(subject, "PART_TextBlock"), exactMatch: false);
                    Assert.Equal(28.0, part.FontSize);
                    double inheritedSize = subject.FontSize;
                    subject.FontSize = inheritedSize;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(inheritedSize, part.FontSize);

                    subject.FontSize = 24;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(24.0, part.FontSize);

                    System.Windows.Controls.Slider source = new() { Maximum = 100, Value = 32 };
                    _ = BindingOperations.SetBinding(subject, System.Windows.Controls.Control.FontSizeProperty,
                        new Binding(nameof(System.Windows.Controls.Slider.Value)) { Source = source });
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(32.0, part.FontSize);

                    source.Value = 36;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(36.0, part.FontSize);

                    BindingOperations.ClearBinding(subject, System.Windows.Controls.Control.FontSizeProperty);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(28.0, part.FontSize);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ExplicitStyleFontSizeAndFontFamilyOverrideNamedDefaultsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                Style libraryStyle = Assert.IsType<Style>(application.TryFindResource(typeof(Controls.TextBlock)));
                Style consumerStyle = new(typeof(Controls.TextBlock)) { BasedOn = libraryStyle };
                consumerStyle.Setters.Add(new Setter(System.Windows.Controls.Control.FontSizeProperty, 33.0));
                consumerStyle.Setters.Add(new Setter(System.Windows.Controls.Control.FontFamilyProperty, new FontFamily("Consolas")));
                Controls.TextBlock subject = new()
                {
                    Text = "Fluence",
                    Typography = FluentTypography.Title,
                    Style = consumerStyle,
                };
                Window window = new() { Content = subject, Width = 420, Height = 200 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    System.Windows.Controls.TextBlock part = Assert.IsType<System.Windows.Controls.TextBlock>(
                        FindVisualChildByName<System.Windows.Controls.TextBlock>(subject, "PART_TextBlock"), exactMatch: false);
                    Assert.Equal(33.0, part.FontSize);
                    Assert.Equal("Consolas", part.FontFamily.Source);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task TypographyChangesPreserveTextWrappingTrimmingAndDynamicFamilyAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.TextBlock subject = new()
                {
                    Text = "A long line of text that can wrap",
                    Typography = FluentTypography.Body,
                    TextWrapping = TextWrapping.Wrap,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                };
                Window window = new() { Content = subject, Width = 200, Height = 180 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    System.Windows.Controls.TextBlock part = Assert.IsType<System.Windows.Controls.TextBlock>(
                        FindVisualChildByName<System.Windows.Controls.TextBlock>(subject, "PART_TextBlock"), exactMatch: false);

                    subject.Typography = FluentTypography.Title;
                    subject.Text = "Updated title";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(28.0, part.FontSize);
                    Assert.Equal("Updated title", part.Text);
                    Assert.Equal(TextWrapping.Wrap, part.TextWrapping);
                    Assert.Equal(TextTrimming.CharacterEllipsis, part.TextTrimming);

                    window.Resources["FluentFontFamily"] = new FontFamily("Consolas");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal("Consolas", part.FontFamily.Source);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }
    }
}
