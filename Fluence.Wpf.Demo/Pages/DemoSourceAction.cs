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
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf;
using Fluent = Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public static class DemoSourceAction
    {
        private const string UrlGlyph = "\uE71B";

        public static FrameworkElement Create(string samplePath)
        {
            var codeBehindPath = GetCodeBehindSamplePath(samplePath);
            if (SampleExists(codeBehindPath))
            {
                return CreateDropDown(samplePath, codeBehindPath);
            }

            return CreateSingleButton(samplePath);
        }

        public static FrameworkElement Replace(FrameworkElement placeholder, string samplePath)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(placeholder);
#else
            if (placeholder == null)
            {
                throw new ArgumentNullException(nameof(placeholder));
            }
#endif

            var action = Create(samplePath);
            action.Name = placeholder.Name;
            action.Margin = placeholder.Margin;
            action.VerticalAlignment = placeholder.VerticalAlignment;
            action.HorizontalAlignment = HorizontalAlignment.Right;
            CopyAttachedLayout(placeholder, action);

            var parentPanel = placeholder.Parent as Panel;
            if (parentPanel != null)
            {
                var index = parentPanel.Children.IndexOf(placeholder);
                if (index >= 0)
                {
                    parentPanel.Children.RemoveAt(index);
                    parentPanel.Children.Insert(index, action);
                    return action;
                }
            }

            var parentContent = placeholder.Parent as ContentControl;
            if (parentContent != null && ReferenceEquals(parentContent.Content, placeholder))
            {
                parentContent.Content = action;
                return action;
            }

            throw new InvalidOperationException("Source link placeholder must be hosted by a Panel or ContentControl.");
        }

        private static Fluent.DropDownButton CreateDropDown(string samplePath, string codeBehindPath)
        {
            var flyout = new StackPanel
            {
                MinWidth = 180,
                Margin = new Thickness(0)
            };

            flyout.Children.Add(CreateFlyoutButton("XAML", samplePath));
            flyout.Children.Add(CreateFlyoutButton("C# Code-behind", codeBehindPath));

            var button = new Fluent.DropDownButton
            {
                Appearance = ControlAppearance.Accent,
                Content = CreateSourceLabel(),
                Flyout = flyout,
                HorizontalAlignment = HorizontalAlignment.Right,
                MinWidth = 116,
                Tag = samplePath
            };
            ApplyAccentBrushes(button);
            return button;
        }

        private static Fluent.Button CreateSingleButton(string samplePath)
        {
            var button = new Fluent.Button
            {
                Appearance = ControlAppearance.Accent,
                Content = "Source",
                HorizontalAlignment = HorizontalAlignment.Right,
                Icon = CreateUrlIcon(),
                IconPlacement = ElementPlacement.Left,
                Tag = DemoSourceLinkSettings.GetGitHubSourceUri(samplePath)
            };
            button.Click += OnSourceButtonClick;
            return button;
        }

        private static Fluent.Button CreateFlyoutButton(string text, string samplePath)
        {
            var button = new Fluent.Button
            {
                Content = text,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 4),
                Tag = DemoSourceLinkSettings.GetGitHubSourceUri(samplePath)
            };
            button.Click += OnSourceButtonClick;
            return button;
        }

        private static StackPanel CreateSourceLabel()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(CreateUrlIcon());
            var label = new System.Windows.Controls.TextBlock
            {
                Margin = new Thickness(6, 0, 0, 0),
                Text = "Source",
                VerticalAlignment = VerticalAlignment.Center
            };
            label.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextOnAccentFillColorPrimaryBrush");
            panel.Children.Add(label);

            return panel;
        }

        private static Fluent.FontIcon CreateUrlIcon()
        {
            var icon = new Fluent.FontIcon
            {
                Glyph = UrlGlyph,
                IconFontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };
            icon.SetResourceReference(Control.ForegroundProperty, "TextOnAccentFillColorPrimaryBrush");
            return icon;
        }

        private static void ApplyAccentBrushes(Control control)
        {
            control.SetResourceReference(Control.BackgroundProperty, "AccentFillColorDefaultBrush");
            control.SetResourceReference(Control.ForegroundProperty, "TextOnAccentFillColorPrimaryBrush");
            control.SetResourceReference(Control.BorderBrushProperty, "AccentControlElevationBorderBrush");
        }

        private static string GetCodeBehindSamplePath(string samplePath)
        {
            DemoSourceLinkSettings.GetGitHubSourceUri(samplePath);
            return samplePath.Replace('\\', '/').Trim('/') + ".cs";
        }

        private static bool SampleExists(string samplePath)
        {
            var outputRoot = Path.Combine(Path.GetDirectoryName(typeof(DemoSourceAction).Assembly.Location), "Samples");
            var localPath = samplePath.Replace('/', Path.DirectorySeparatorChar);
            return File.Exists(Path.Combine(outputRoot, localPath));
        }

        private static void CopyAttachedLayout(FrameworkElement source, FrameworkElement target)
        {
            Grid.SetRow(target, Grid.GetRow(source));
            Grid.SetColumn(target, Grid.GetColumn(source));
            Grid.SetRowSpan(target, Grid.GetRowSpan(source));
            Grid.SetColumnSpan(target, Grid.GetColumnSpan(source));
            DockPanel.SetDock(target, DockPanel.GetDock(source));
        }

        private static void OnSourceButtonClick(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            var uri = element == null ? null : element.Tag as Uri;
            if (uri == null)
            {
                return;
            }

            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
    }
}
