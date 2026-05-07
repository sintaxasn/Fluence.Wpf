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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using FluenceCard = Fluence.Wpf.Controls.Card;

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

        private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
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

        public static readonly DependencyProperty XamlSourceProperty =
            DependencyProperty.Register(
                "XamlSource",
                typeof(string),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(string.Empty, OnSourceChanged));

        public static readonly DependencyProperty CSharpSourceProperty =
            DependencyProperty.Register(
                "CSharpSource",
                typeof(string),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(string.Empty, OnSourceChanged));

        public static readonly DependencyProperty SampleContentProperty =
            DependencyProperty.Register(
                "SampleContent",
                typeof(UIElement),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(null, OnSampleContentChanged));

        private bool _sourceLoaded;

        public DemoSampleControl()
        {
            InitializeComponent();
            UpdateHeaderVisibility();
            UpdateSampleContentVisibility();
            UpdateSourceVisibility();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public string XamlSource
        {
            get => (string)GetValue(XamlSourceProperty);
            set => SetValue(XamlSourceProperty, value);
        }

        public string CSharpSource
        {
            get => (string)GetValue(CSharpSourceProperty);
            set => SetValue(CSharpSourceProperty, value);
        }

        public UIElement SampleContent
        {
            get => (UIElement)GetValue(SampleContentProperty);
            set => SetValue(SampleContentProperty, value);
        }

        private static void OnHeaderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DemoSampleControl control)
            {
                control.UpdateHeaderVisibility();
            }
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DemoSampleControl control)
            {
                control.ResetSource();
            }
        }

        private static void OnSampleContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DemoSampleControl control)
            {
                control.UpdateSampleContentVisibility();
            }
        }

        private void UpdateHeaderVisibility()
        {
            if (TitleTextBlock is null || DescriptionTextBlock is null || HeaderPanel is null)
            {
                return;
            }

            TitleTextBlock.Visibility = string.IsNullOrWhiteSpace(Title) ? Visibility.Collapsed : Visibility.Visible;
            DescriptionTextBlock.Visibility = string.IsNullOrWhiteSpace(Description) ? Visibility.Collapsed : Visibility.Visible;
            HeaderPanel.Visibility = string.IsNullOrWhiteSpace(Title) && string.IsNullOrWhiteSpace(Description)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void UpdateSourceVisibility()
        {
            _ = (SourceExpander?.Visibility = string.IsNullOrWhiteSpace(XamlSource) && string.IsNullOrWhiteSpace(CSharpSource)
                    ? Visibility.Collapsed
                    : Visibility.Visible);
        }

        private void UpdateSampleContentVisibility()
        {
            if (SampleCard is null || SourceExpander is null)
            {
                return;
            }

            if (SampleContent is null)
            {
                SampleCard.Visibility = Visibility.Collapsed;
                SourceExpander.BorderThickness = new Thickness(1);
                SourceExpander.CornerRadius = new CornerRadius(8);
                return;
            }

            SampleCard.Visibility = Visibility.Visible;
            SourceExpander.BorderThickness = new Thickness(1, 0, 1, 1);
            SourceExpander.CornerRadius = new CornerRadius(0, 0, 8, 8);
        }

        private void ResetSource()
        {
            _sourceLoaded = false;
            SourceTabs?.Items.Clear();

            UpdateSourceVisibility();
        }

        private void SourceExpander_Expanded(object sender, RoutedEventArgs e)
        {
            LoadSourceTabs();
        }

        private void LoadSourceTabs()
        {
            if (_sourceLoaded || (string.IsNullOrWhiteSpace(XamlSource) && string.IsNullOrWhiteSpace(CSharpSource)))
            {
                return;
            }

            _sourceLoaded = true;
            SourceTabs.Items.Clear();
            if (!string.IsNullOrWhiteSpace(XamlSource))
            {
                AddSourceTab("XAML", XamlSource, SourceLanguage.Xaml);
            }

            if (!string.IsNullOrWhiteSpace(CSharpSource))
            {
                AddSourceTab("C# Code-behind", CSharpSource, SourceLanguage.CSharp);
            }
        }

        private void AddSourceTab(string header, string source, SourceLanguage language)
        {
            _ = SourceTabs.Items.Add(new Controls.TabViewItem
            {
                Header = header,
                IsClosable = false,
                Content = CreateSourcePane(source, language)
            });

            if (SourceTabs.SelectedIndex < 0)
            {
                SourceTabs.SelectedIndex = 0;
            }
        }

        private Grid CreateSourcePane(string source, SourceLanguage language)
        {
            Grid panel = new();
            panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Controls.Button copyButton = CreateCopyButton(source);
            Grid.SetRow(copyButton, 0);
            _ = panel.Children.Add(copyButton);

            RichTextBox viewer = CreateSourceViewer(source, language);
            Grid.SetRow(viewer, 1);
            _ = panel.Children.Add(viewer);

            return panel;
        }

        private Controls.Button CreateCopyButton(string source)
        {
            Controls.Button button = new()
            {
                Name = "CopySourceButton",
                Appearance = ControlAppearance.Subtle,
                Content = "Copy",
                HorizontalAlignment = HorizontalAlignment.Right,
                Icon = new Controls.FontIcon { Glyph = "\uE8C8" },
                Margin = new Thickness(0, 0, 0, 8),
                Tag = source
            };
            button.Click += OnCopySourceButtonClick;
            return button;
        }

        private void OnCopySourceButtonClick(object sender, RoutedEventArgs e)
        {
            string? source = sender is FrameworkElement element ? element.Tag as string : null;
            if (!string.IsNullOrWhiteSpace(source))
            {
                Clipboard.SetText(source);
            }
        }

        private static RichTextBox CreateSourceViewer(string source, SourceLanguage language)
        {
            RichTextBox viewer = new()
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
            viewer.SetResourceReference(BackgroundProperty, "SolidBackgroundFillColorTertiaryBrush");
            viewer.SetResourceReference(ForegroundProperty, "TextFillColorPrimaryBrush");
            viewer.SetResourceReference(BorderBrushProperty, "CardStrokeColorDefaultBrush");
            viewer.Document = CreateSourceDocument(source, language);
            return viewer;
        }

        private static FlowDocument CreateSourceDocument(string source, SourceLanguage language)
        {
            FlowDocument document = new()
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                PagePadding = new Thickness(12)
            };
            document.SetResourceReference(TextElement.ForegroundProperty, "TextFillColorPrimaryBrush");

            Paragraph paragraph = new()
            {
                LineHeight = 18,
                Margin = new Thickness(0)
            };
            document.Blocks.Add(paragraph);

            string normalized = (source ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            string[] lines = normalized.Split('\n');
            for (int i = 0; i < lines.Length; i++)
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
            int index = 0;
            while (index < line.Length)
            {
                if (StartsWith(line, index, "<!--"))
                {
                    AddRun(paragraph, line.Substring(index), "TextFillColorSecondaryBrush");
                    return;
                }

                char current = line[index];
                if (current is '"' or '\'')
                {
                    int end = FindQuotedTextEnd(line, index, current);
                    AddRun(paragraph, line.Substring(index, end - index), "SystemFillColorCautionBrush");
                    index = end;
                    continue;
                }

                if (current is '<' or '>' or '/')
                {
                    AddRun(paragraph, line.Substring(index, 1), "AccentTextFillColorPrimaryBrush");
                    index++;
                    continue;
                }

                if (IsXamlNameStart(current))
                {
                    int start = index;
                    while (index < line.Length && IsXamlNameChar(line[index]))
                    {
                        index++;
                    }

                    string name = line.Substring(start, index - start);
                    int next = SkipWhiteSpace(line, index);
                    string resourceKey = next < line.Length && line[next] == '='
                        ? "SystemFillColorSuccessBrush"
                        : "AccentTextFillColorPrimaryBrush";
                    AddRun(paragraph, name, resourceKey);
                    continue;
                }

                int plainStart = index;
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
            int index = 0;
            while (index < line.Length)
            {
                if (StartsWith(line, index, "//"))
                {
                    AddRun(paragraph, line.Substring(index), "TextFillColorSecondaryBrush");
                    return;
                }

                char current = line[index];
                if (current == '"')
                {
                    int end = FindQuotedTextEnd(line, index, current);
                    AddRun(paragraph, line.Substring(index, end - index), "SystemFillColorCautionBrush");
                    index = end;
                    continue;
                }

                if (current == '\'' && index + 2 < line.Length)
                {
                    int end = FindQuotedTextEnd(line, index, current);
                    AddRun(paragraph, line.Substring(index, end - index), "SystemFillColorCautionBrush");
                    index = end;
                    continue;
                }

                if (char.IsLetter(current) || current == '_')
                {
                    int start = index;
                    while (index < line.Length && (char.IsLetterOrDigit(line[index]) || line[index] == '_'))
                    {
                        index++;
                    }

                    string word = line.Substring(start, index - start);
                    AddRun(paragraph, word, CSharpKeywords.Contains(word)
                        ? "AccentTextFillColorPrimaryBrush"
                        : "TextFillColorPrimaryBrush");
                    continue;
                }

                int plainStart = index;
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
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            Run run = new(text);
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
            int index = start + 1;
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S2302:\"nameof\" should be used", Justification = "False positive.")]
        public static DemoSampleControl ReplaceSourceLink(FrameworkElement placeholder, string xamlSource, string csharpSource)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(placeholder);
#else
            if (placeholder is null)
            {
                throw new ArgumentNullException(nameof(placeholder));
            }
#endif

            DemoSampleControl sample = new()
            {
                Name = placeholder.Name,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = placeholder.VerticalAlignment,
                XamlSource = xamlSource,
                CSharpSource = csharpSource
            };

            FluenceCard? hostCard = FindAncestorCard(placeholder);
            if (hostCard is not null && hostCard.Content is UIElement sampleContent)
            {
                sample.Margin = hostCard.Margin;
                sample.VerticalAlignment = hostCard.VerticalAlignment;
                CopyAttachedLayout(hostCard, sample);
                RemovePlaceholderFromParent(placeholder);
                hostCard.Content = null;
                sample.SampleContent = sampleContent;

                if (hostCard.Parent is Panel hostPanel)
                {
                    int index = hostPanel.Children.IndexOf(hostCard);
                    if (index >= 0)
                    {
                        hostPanel.Children.RemoveAt(index);
                        hostPanel.Children.Insert(index, sample);
                        return sample;
                    }
                }

                if (hostCard.Parent is ContentControl hostContent && ReferenceEquals(hostContent.Content, hostCard))
                {
                    hostContent.Content = sample;
                    return sample;
                }
            }

            sample.Margin = placeholder.Margin;
            CopyAttachedLayout(placeholder, sample);

            if (placeholder.Parent is Panel parentPanel)
            {
                int index = parentPanel.Children.IndexOf(placeholder);
                if (index >= 0)
                {
                    parentPanel.Children.RemoveAt(index);
                    parentPanel.Children.Insert(index, sample);
                    return sample;
                }
            }

            if (placeholder.Parent is ContentControl parentContent && ReferenceEquals(parentContent.Content, placeholder))
            {
                parentContent.Content = sample;
                return sample;
            }

            throw new InvalidOperationException("Source link placeholder must be hosted by a Panel or ContentControl.");
        }

        private static FluenceCard? FindAncestorCard(FrameworkElement element)
        {
            DependencyObject? current = element;
            while (current is not null)
            {
                if (current is FluenceCard card)
                {
                    return card;
                }

                current = GetParentObject(current);
            }

            return null;
        }

        private static DependencyObject? GetParentObject(DependencyObject current)
        {
            return current is not null ? VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current) : null;
        }

        private static void RemovePlaceholderFromParent(FrameworkElement placeholder)
        {
            if (placeholder.Parent is Panel parentPanel)
            {
                parentPanel.Children.Remove(placeholder);
                return;
            }

            if (placeholder.Parent is ContentControl parentContent && ReferenceEquals(parentContent.Content, placeholder))
            {
                parentContent.Content = null;
            }
        }

        private static void CopyAttachedLayout(FrameworkElement source, FrameworkElement target)
        {
            Grid.SetRow(target, Grid.GetRow(source));
            Grid.SetColumn(target, Grid.GetColumn(source));
            Grid.SetRowSpan(target, Grid.GetRowSpan(source));
            Grid.SetColumnSpan(target, Grid.GetColumnSpan(source));
            DockPanel.SetDock(target, DockPanel.GetDock(source));
        }
    }
}
