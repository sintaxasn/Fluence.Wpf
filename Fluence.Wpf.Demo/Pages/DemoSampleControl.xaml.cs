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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class DemoSampleControl : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                "Title",
                typeof(string),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(string.Empty, OnHeaderTextChanged));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(
                "Description",
                typeof(string),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(string.Empty, OnHeaderTextChanged));

        public static readonly DependencyProperty SourcePathProperty =
            DependencyProperty.Register(
                "SourcePath",
                typeof(string),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(string.Empty, OnSourcePathChanged));

        public static readonly DependencyProperty SampleContentProperty =
            DependencyProperty.Register(
                "SampleContent",
                typeof(UIElement),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(null));

        private bool _sourceLoaded;

        public DemoSampleControl()
        {
            InitializeComponent();
            UpdateHeaderVisibility();
            UpdateSourceVisibility();
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        public string SourcePath
        {
            get { return (string)GetValue(SourcePathProperty); }
            set { SetValue(SourcePathProperty, value); }
        }

        public UIElement SampleContent
        {
            get { return (UIElement)GetValue(SampleContentProperty); }
            set { SetValue(SampleContentProperty, value); }
        }

        private static void OnHeaderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as DemoSampleControl;
            if (control != null)
            {
                control.UpdateHeaderVisibility();
            }
        }

        private static void OnSourcePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as DemoSampleControl;
            if (control != null)
            {
                control.ResetSource();
            }
        }

        private void UpdateHeaderVisibility()
        {
            if (TitleTextBlock == null || DescriptionTextBlock == null || HeaderPanel == null)
            {
                return;
            }

            TitleTextBlock.Visibility = string.IsNullOrEmpty(Title) ? Visibility.Collapsed : Visibility.Visible;
            DescriptionTextBlock.Visibility = string.IsNullOrEmpty(Description) ? Visibility.Collapsed : Visibility.Visible;
            HeaderPanel.Visibility = string.IsNullOrEmpty(Title) && string.IsNullOrEmpty(Description)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void UpdateSourceVisibility()
        {
            if (SourceExpander != null)
            {
                SourceExpander.Visibility = string.IsNullOrEmpty(SourcePath)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
        }

        private void ResetSource()
        {
            _sourceLoaded = false;
            if (SourceTabs != null)
            {
                SourceTabs.Items.Clear();
            }

            UpdateSourceVisibility();
        }

        private void SourceExpander_Expanded(object sender, RoutedEventArgs e)
        {
            LoadSourceTabs();
        }

        private void LoadSourceTabs()
        {
            if (_sourceLoaded || string.IsNullOrEmpty(SourcePath))
            {
                return;
            }

            _sourceLoaded = true;
            SourceTabs.Items.Clear();
            AddSourceTab("XAML", SourcePath);

            var codeBehindPath = GetCodeBehindPath(SourcePath);
            if (SampleExists(codeBehindPath))
            {
                AddSourceTab("C# Code-behind", codeBehindPath);
            }
        }

        private void AddSourceTab(string header, string samplePath)
        {
            SourceTabs.Items.Add(new TabItem
            {
                Header = header,
                Content = CreateSourceTextBox(ReadSample(samplePath))
            });
        }

        private static TextBox CreateSourceTextBox(string source)
        {
            var textBox = new TextBox
            {
                AcceptsReturn = true,
                AcceptsTab = true,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsReadOnly = true,
                MinHeight = 180,
                Padding = new Thickness(12),
                Text = source,
                TextWrapping = TextWrapping.NoWrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            textBox.SetResourceReference(Control.BackgroundProperty, "CardBackgroundFillColorDefaultBrush");
            textBox.SetResourceReference(Control.ForegroundProperty, "TextFillColorPrimaryBrush");
            textBox.SetResourceReference(Control.BorderBrushProperty, "CardStrokeColorDefaultBrush");
            return textBox;
        }

        private static string ReadSample(string samplePath)
        {
            var path = GetSampleFilePath(samplePath);
            if (!File.Exists(path))
            {
                return "Sample source was not copied to the output directory: " + samplePath;
            }

            return File.ReadAllText(path);
        }

        private static bool SampleExists(string samplePath)
        {
            return File.Exists(GetSampleFilePath(samplePath));
        }

        private static string GetSampleFilePath(string samplePath)
        {
            var outputDirectory = Path.GetDirectoryName(typeof(DemoSampleControl).Assembly.Location);
            var localPath = NormalizeSamplePath(samplePath).Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(outputDirectory, "Samples", localPath);
        }

        private static string GetCodeBehindPath(string samplePath)
        {
            return NormalizeSamplePath(samplePath) + ".cs";
        }

        private static string NormalizeSamplePath(string samplePath)
        {
            return (samplePath ?? string.Empty).Replace('\\', '/').Trim('/');
        }
    }
}
