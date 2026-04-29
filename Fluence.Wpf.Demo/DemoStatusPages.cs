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
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf;
using Fluence.Wpf.Demo.Pages;
using Fluent = Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo
{
    internal static class DemoStatusPages
    {
        public static GalleryControlPage InfoBadge()
        {
            return CreatePage(
                "InfoBadge",
                "Use InfoBadge to show compact counts, status, or attention indicators.",
                new[] { Example("Badge variants", "InfoBadge supports dots, numeric values, icons, and severity styles.", "Status/InfoBadges.xaml", CreateInfoBadges) });
        }

        public static GalleryControlPage InfoBar()
        {
            return CreatePage(
                "InfoBar",
                "Use InfoBar for inline status, validation, and recoverable messages.",
                new[] { Example("Severity states", "InfoBar presents informational, success, warning, and error messages with optional actions.", "Status/InfoBars.xaml", CreateInfoBars) });
        }

        public static GalleryControlPage ProgressBar()
        {
            return CreatePage(
                "ProgressBar",
                "Use ProgressBar for measurable work where the current value can be reported.",
                new[]
                {
                    Example("Determinate value", "ProgressBar can show a known percentage.", "Status/ProgressBarValue.xaml", CreateProgressBars),
                    Example("Step progress", "Step mode shows progress through a known sequence.", "Status/ProgressBarSteps.xaml", CreateStepProgressBar)
                });
        }

        public static GalleryControlPage ProgressRing()
        {
            return CreatePage(
                "ProgressRing",
                "Use ProgressRing when progress belongs near compact content or the available space is limited.",
                new[] { Example("Ring states", "ProgressRing supports indeterminate, determinate, and disabled states.", "Status/ProgressRings.xaml", CreateProgressRings) });
        }

        public static GalleryControlPage PersonPicture()
        {
            return CreatePage(
                "PersonPicture",
                "Use PersonPicture to represent people, groups, and identity-related status.",
                new[] { Example("Identity states", "PersonPicture can show initials, group glyphs, and badges.", "Status/PersonPicture.xaml", CreatePersonPictures) });
        }

        private static GalleryControlPage CreatePage(string title, string description, DemoExample[] examples)
        {
            return new GalleryControlPage(title, description, examples);
        }

        private static DemoExample Example(string title, string description, string sourcePath, Func<UIElement> createContent)
        {
            return new DemoExample(title, description, sourcePath, createContent);
        }

        private static Fluent.FontIcon Icon(string glyph)
        {
            return new Fluent.FontIcon { Glyph = glyph, IconFontSize = 14 };
        }

        private static WrapPanel Wrap(params UIElement[] children)
        {
            var panel = new WrapPanel();
            foreach (var child in children)
            {
                panel.Children.Add(child);
            }

            return panel;
        }

        private static StackPanel Stack(params UIElement[] children)
        {
            var panel = new StackPanel();
            foreach (var child in children)
            {
                panel.Children.Add(child);
            }

            return panel;
        }

        private static TextBlock Caption(string text)
        {
            var block = new TextBlock
            {
                Margin = new Thickness(0, 8, 24, 0),
                Text = text,
                TextAlignment = TextAlignment.Center
            };
            block.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            return block;
        }

        private static UIElement CreateInfoBadges()
        {
            return Wrap(
                new Fluent.InfoBadge { Width = 18, Height = 18, Margin = new Thickness(0, 0, 20, 8) },
                new Fluent.InfoBadge { Width = 24, Height = 24, Margin = new Thickness(0, 0, 20, 8), Value = 7 },
                new Fluent.InfoBadge { Width = 24, Height = 24, Margin = new Thickness(0, 0, 20, 8), Value = 99, BadgeStyle = InfoBadgeStyle.Caution },
                new Fluent.InfoBadge { Width = 24, Height = 24, Margin = new Thickness(0, 0, 20, 8), BadgeStyle = InfoBadgeStyle.Critical, IconSource = Icon("\uE783") });
        }

        private static UIElement CreateInfoBars()
        {
            return Stack(
                new Fluent.InfoBar { Title = "Informational", Message = "This is a general information message.", Severity = InfoBarSeverity.Informational, IsOpen = true, Margin = new Thickness(0, 0, 0, 8) },
                new Fluent.InfoBar { Title = "Success", Message = "The operation completed successfully.", Severity = InfoBarSeverity.Success, IsOpen = true, Margin = new Thickness(0, 0, 0, 8) },
                new Fluent.InfoBar { Title = "Warning", Message = "Proceed with caution.", Severity = InfoBarSeverity.Warning, IsOpen = true, Margin = new Thickness(0, 0, 0, 8) },
                new Fluent.InfoBar
                {
                    Title = "Error",
                    Message = "Something went wrong.",
                    Severity = InfoBarSeverity.Error,
                    IsOpen = true,
                    ActionButton = new Fluent.Button { Appearance = ControlAppearance.Accent, Content = "Retry" }
                });
        }

        private static UIElement CreateProgressBars()
        {
            var valueBar = new Fluent.ProgressBar
            {
                Name = "StandardProgressBar",
                Height = 8,
                Margin = new Thickness(0, 0, 0, 12),
                Maximum = 100,
                Minimum = 0,
                Value = 45
            };
            var slider = new Fluent.Slider
            {
                Name = "ProgressSlider",
                Margin = new Thickness(0, 0, 0, 8),
                Maximum = 100,
                Minimum = 0,
                TickFrequency = 5,
                Value = 45
            };
            var label = new TextBlock
            {
                Name = "SliderValueLabel",
                Margin = new Thickness(0, 0, 0, 14),
                Text = "Value: 45"
            };
            label.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            slider.ValueChanged += delegate
            {
                valueBar.Value = slider.Value;
                label.Text = "Value: " + Math.Round(slider.Value).ToString(CultureInfo.InvariantCulture);
            };

            return Stack(
                valueBar,
                slider,
                label,
                new Fluent.ProgressBar { Height = 8, Margin = new Thickness(0, 0, 0, 14), ProgressMode = ProgressBarMode.Indeterminate },
                new Fluent.ProgressBar { Height = 8, ProgressMode = ProgressBarMode.Paused, Maximum = 100, Minimum = 0, Value = 65 });
        }

        private static UIElement CreateStepProgressBar()
        {
            return new Fluent.ProgressBar
            {
                Name = "StepProgressBar",
                Height = 8,
                CurrentStep = 2,
                ProgressMode = ProgressBarMode.StepProgress,
                Steps = 5
            };
        }

        private static UIElement CreateProgressRings()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddRingColumn(grid, 0, new Fluent.ProgressRing { Width = 48, Height = 48, IsActive = true, IsIndeterminate = true }, "Indeterminate");
            AddRingColumn(grid, 1, new Fluent.ProgressRing { Width = 48, Height = 48, IsActive = true, IsIndeterminate = false, Maximum = 100, Minimum = 0, Value = 50 }, "Determinate");
            AddRingColumn(grid, 2, new Fluent.ProgressRing { Width = 48, Height = 48, IsActive = true, IsEnabled = false, IsIndeterminate = true }, "Disabled");

            return grid;
        }

        private static void AddRingColumn(Grid grid, int column, Fluent.ProgressRing ring, string caption)
        {
            ring.HorizontalAlignment = HorizontalAlignment.Center;
            Grid.SetColumn(ring, column);
            Grid.SetRow(ring, 0);
            grid.Children.Add(ring);

            var label = Caption(caption);
            label.Margin = new Thickness(0, 8, 0, 0);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            Grid.SetColumn(label, column);
            Grid.SetRow(label, 1);
            grid.Children.Add(label);
        }

        private static UIElement CreatePersonPictures()
        {
            return Wrap(
                new Fluent.PersonPicture { Width = 48, Height = 48, Margin = new Thickness(0, 0, 24, 8), DisplayName = "Alice Smith" },
                new Fluent.PersonPicture { Width = 48, Height = 48, Margin = new Thickness(0, 0, 24, 8), Initials = "MJ" },
                new Fluent.PersonPicture { Width = 48, Height = 48, Margin = new Thickness(0, 0, 24, 8), DisplayName = "Design Team", IsGroup = true },
                new Fluent.PersonPicture { Width = 48, Height = 48, Margin = new Thickness(0, 0, 24, 8), DisplayName = "Jordan Lee", BadgeNumber = 3 });
        }
    }
}
