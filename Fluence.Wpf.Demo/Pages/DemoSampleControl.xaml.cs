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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class DemoSampleControl : UserControl
    {
        private enum SourceLanguage
        {
            PlainText,
            Xaml,
            CSharp
        }

        private static readonly HashSet<string> CSharpKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "abstract",
            "as",
            "base",
            "bool",
            "break",
            "case",
            "catch",
            "class",
            "const",
            "continue",
            "decimal",
            "default",
            "delegate",
            "do",
            "double",
            "else",
            "enum",
            "event",
            "explicit",
            "extern",
            "false",
            "finally",
            "fixed",
            "float",
            "for",
            "foreach",
            "if",
            "implicit",
            "in",
            "int",
            "interface",
            "internal",
            "is",
            "lock",
            "namespace",
            "new",
            "null",
            "object",
            "operator",
            "out",
            "override",
            "params",
            "private",
            "protected",
            "public",
            "readonly",
            "ref",
            "return",
            "sealed",
            "short",
            "sizeof",
            "static",
            "string",
            "struct",
            "switch",
            "this",
            "throw",
            "true",
            "try",
            "typeof",
            "uint",
            "ulong",
            "unchecked",
            "unsafe",
            "ushort",
            "using",
            "var",
            "virtual",
            "void",
            "volatile",
            "while"
        };

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
            var source = ReadSample(samplePath);
            SourceTabs.Items.Add(new Fluence.Wpf.Controls.TabViewItem
            {
                Header = header,
                IsClosable = false,
                Content = CreateSourcePane(source, GetSourceLanguage(samplePath))
            });

            if (SourceTabs.SelectedIndex < 0)
            {
                SourceTabs.SelectedIndex = 0;
            }
        }

        private UIElement CreateSourcePane(string source, SourceLanguage language)
        {
            var panel = new Grid();
            panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var copyButton = CreateCopyButton(source);
            Grid.SetRow(copyButton, 0);
            panel.Children.Add(copyButton);

            var viewer = CreateSourceViewer(source, language);
            Grid.SetRow(viewer, 1);
            panel.Children.Add(viewer);

            return panel;
        }

        private Fluence.Wpf.Controls.Button CreateCopyButton(string source)
        {
            var button = new Fluence.Wpf.Controls.Button
            {
                Name = "CopySourceButton",
                Appearance = ControlAppearance.Subtle,
                Content = "Copy",
                HorizontalAlignment = HorizontalAlignment.Right,
                Icon = new Fluence.Wpf.Controls.FontIcon { Glyph = "\uE8C8" },
                Margin = new Thickness(0, 0, 0, 8),
                Tag = source
            };
            button.Click += OnCopySourceButtonClick;
            return button;
        }

        private void OnCopySourceButtonClick(object sender, RoutedEventArgs e)
        {
            var element = sender as FrameworkElement;
            var source = element != null ? element.Tag as string : null;
            if (!string.IsNullOrEmpty(source))
            {
                Clipboard.SetText(source);
            }
        }

        private static RichTextBox CreateSourceViewer(string source, SourceLanguage language)
        {
            var viewer = new RichTextBox
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsReadOnly = true,
                MinHeight = 220,
                Name = "SourceTextViewer",
                Padding = new Thickness(0),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            viewer.SetResourceReference(Control.BackgroundProperty, "SolidBackgroundFillColorTertiaryBrush");
            viewer.SetResourceReference(Control.ForegroundProperty, "TextFillColorPrimaryBrush");
            viewer.SetResourceReference(Control.BorderBrushProperty, "CardStrokeColorDefaultBrush");
            viewer.Document = CreateSourceDocument(source, language);
            return viewer;
        }

        private static FlowDocument CreateSourceDocument(string source, SourceLanguage language)
        {
            var document = new FlowDocument
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                PagePadding = new Thickness(12)
            };
            document.SetResourceReference(TextElement.ForegroundProperty, "TextFillColorPrimaryBrush");

            var paragraph = new Paragraph
            {
                LineHeight = 18,
                Margin = new Thickness(0)
            };
            document.Blocks.Add(paragraph);

            var normalized = (source ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            var lines = normalized.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                AddFormattedLine(paragraph, lines[i], language);
                if (i < lines.Length - 1)
                {
                    paragraph.Inlines.Add(new LineBreak());
                }
            }

            return document;
        }

        private static void AddFormattedLine(Paragraph paragraph, string line, SourceLanguage language)
        {
            if (language == SourceLanguage.Xaml)
            {
                AddXamlLine(paragraph, line);
                return;
            }

            if (language == SourceLanguage.CSharp)
            {
                AddCSharpLine(paragraph, line);
                return;
            }

            AddRun(paragraph, line, "TextFillColorPrimaryBrush");
        }

        private static void AddXamlLine(Paragraph paragraph, string line)
        {
            var index = 0;
            while (index < line.Length)
            {
                if (StartsWith(line, index, "<!--"))
                {
                    AddRun(paragraph, line.Substring(index), "TextFillColorSecondaryBrush");
                    return;
                }

                var current = line[index];
                if (current == '"' || current == '\'')
                {
                    var end = FindQuotedTextEnd(line, index, current);
                    AddRun(paragraph, line.Substring(index, end - index), "SystemFillColorCautionBrush");
                    index = end;
                    continue;
                }

                if (current == '<' || current == '>' || current == '/')
                {
                    AddRun(paragraph, line.Substring(index, 1), "AccentTextFillColorPrimaryBrush");
                    index++;
                    continue;
                }

                if (IsXamlNameStart(current))
                {
                    var start = index;
                    while (index < line.Length && IsXamlNameChar(line[index]))
                    {
                        index++;
                    }

                    var name = line.Substring(start, index - start);
                    var next = SkipWhiteSpace(line, index);
                    var resourceKey = next < line.Length && line[next] == '='
                        ? "SystemFillColorSuccessBrush"
                        : "AccentTextFillColorPrimaryBrush";
                    AddRun(paragraph, name, resourceKey);
                    continue;
                }

                var plainStart = index;
                while (index < line.Length &&
                       line[index] != '<' &&
                       line[index] != '>' &&
                       line[index] != '/' &&
                       line[index] != '"' &&
                       line[index] != '\'' &&
                       !IsXamlNameStart(line[index]))
                {
                    index++;
                }

                AddRun(paragraph, line.Substring(plainStart, index - plainStart), "TextFillColorPrimaryBrush");
            }
        }

        private static void AddCSharpLine(Paragraph paragraph, string line)
        {
            var index = 0;
            while (index < line.Length)
            {
                if (StartsWith(line, index, "//"))
                {
                    AddRun(paragraph, line.Substring(index), "TextFillColorSecondaryBrush");
                    return;
                }

                var current = line[index];
                if (current == '"')
                {
                    var end = FindQuotedTextEnd(line, index, current);
                    AddRun(paragraph, line.Substring(index, end - index), "SystemFillColorCautionBrush");
                    index = end;
                    continue;
                }

                if (current == '\'' && index + 2 < line.Length)
                {
                    var end = FindQuotedTextEnd(line, index, current);
                    AddRun(paragraph, line.Substring(index, end - index), "SystemFillColorCautionBrush");
                    index = end;
                    continue;
                }

                if (char.IsLetter(current) || current == '_')
                {
                    var start = index;
                    while (index < line.Length && (char.IsLetterOrDigit(line[index]) || line[index] == '_'))
                    {
                        index++;
                    }

                    var word = line.Substring(start, index - start);
                    AddRun(paragraph, word, CSharpKeywords.Contains(word)
                        ? "AccentTextFillColorPrimaryBrush"
                        : "TextFillColorPrimaryBrush");
                    continue;
                }

                var plainStart = index;
                while (index < line.Length &&
                       !StartsWith(line, index, "//") &&
                       line[index] != '"' &&
                       line[index] != '\'' &&
                       !char.IsLetter(line[index]) &&
                       line[index] != '_')
                {
                    index++;
                }

                AddRun(paragraph, line.Substring(plainStart, index - plainStart), "TextFillColorPrimaryBrush");
            }
        }

        private static void AddRun(Paragraph paragraph, string text, string resourceKey)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var run = new Run(text);
            run.SetResourceReference(TextElement.ForegroundProperty, resourceKey);
            paragraph.Inlines.Add(run);
        }

        private static bool StartsWith(string text, int index, string value)
        {
            return index + value.Length <= text.Length &&
                   string.Compare(text, index, value, 0, value.Length, StringComparison.Ordinal) == 0;
        }

        private static int FindQuotedTextEnd(string text, int start, char quote)
        {
            var index = start + 1;
            while (index < text.Length)
            {
                if (text[index] == '\\')
                {
                    index += 2;
                    continue;
                }

                if (text[index] == quote)
                {
                    return index + 1;
                }

                index++;
            }

            return text.Length;
        }

        private static int SkipWhiteSpace(string text, int index)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index++;
            }

            return index;
        }

        private static bool IsXamlNameStart(char value)
        {
            return char.IsLetter(value) || value == '_' || value == ':';
        }

        private static bool IsXamlNameChar(char value)
        {
            return char.IsLetterOrDigit(value) ||
                   value == '_' ||
                   value == ':' ||
                   value == '.' ||
                   value == '-';
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

        private static SourceLanguage GetSourceLanguage(string samplePath)
        {
            var normalized = NormalizeSamplePath(samplePath);
            if (normalized.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                return SourceLanguage.CSharp;
            }

            if (normalized.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                return SourceLanguage.Xaml;
            }

            return SourceLanguage.PlainText;
        }

        private static string NormalizeSamplePath(string samplePath)
        {
            return (samplePath ?? string.Empty).Replace('\\', '/').Trim('/');
        }
    }
}
