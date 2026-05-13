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
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class DemoSampleControl : ContentControl
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

        public static readonly DependencyProperty SampleDescriptionProperty =
            DependencyProperty.Register(
                "SampleDescription",
                typeof(string),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(string.Empty, OnSampleDescriptionChanged));

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

        public static readonly DependencyProperty DemoContentProperty =
            DependencyProperty.Register(
                "DemoContent",
                typeof(object),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(null, OnDemoContentChanged));

        public static readonly DependencyProperty OutputContentProperty =
            DependencyProperty.Register(
                "OutputContent",
                typeof(object),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(null, OnOutputContentChanged));

        public static readonly DependencyProperty RightRailContentProperty =
            DependencyProperty.Register(
                "RightRailContent",
                typeof(object),
                typeof(DemoSampleControl),
                new FrameworkPropertyMetadata(null, OnRightRailContentChanged));

        private bool _sourceLoaded;

        public DemoSampleControl()
        {
            InitializeComponent();
            UpdateSampleDescriptionVisibility();
            UpdateDemoContentVisibility();
            UpdateOutputVisibility();
            UpdateRightRailVisibility();
            UpdateSourceVisibility();
        }

        public string SampleDescription
        {
            get => (string)GetValue(SampleDescriptionProperty);
            set => SetValue(SampleDescriptionProperty, value);
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

        public object? DemoContent
        {
            get => GetValue(DemoContentProperty);
            set => SetValue(DemoContentProperty, value);
        }

        public object? OutputContent
        {
            get => GetValue(OutputContentProperty);
            set => SetValue(OutputContentProperty, value);
        }

        public object? RightRailContent
        {
            get => GetValue(RightRailContentProperty);
            set => SetValue(RightRailContentProperty, value);
        }

        private static void OnSampleDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DemoSampleControl control)
            {
                control.UpdateSampleDescriptionVisibility();
            }
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DemoSampleControl control)
            {
                control.ResetSource();
            }
        }

        private static void OnDemoContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DemoSampleControl control)
            {
                control.UpdateDemoContentVisibility();
            }
        }

        private static void OnOutputContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DemoSampleControl control)
            {
                control.UpdateOutputVisibility();
            }
        }

        private static void OnRightRailContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DemoSampleControl control)
            {
                control.UpdateRightRailVisibility();
            }
        }

        private void UpdateSampleDescriptionVisibility()
        {
            if (SampleDescriptionTextBlock is null)
            {
                return;
            }

            SampleDescriptionTextBlock.Visibility = string.IsNullOrWhiteSpace(SampleDescription)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void UpdateSourceVisibility()
        {
            if (SourceExpander is null)
            {
                return;
            }

            SourceExpander.Visibility = string.IsNullOrWhiteSpace(XamlSource) && string.IsNullOrWhiteSpace(CSharpSource)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void UpdateDemoContentVisibility()
        {
            if (SampleCard is null || SourceExpander is null)
            {
                return;
            }

            if (DemoContent is null)
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

        private void UpdateOutputVisibility()
        {
            if (OutputRegion is null)
            {
                return;
            }

            OutputRegion.Visibility = OutputContent is null ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdateRightRailVisibility()
        {
            if (RightRailBorder is null)
            {
                return;
            }

            RightRailBorder.Visibility = RightRailContent is null ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ResetSource()
        {
            _sourceLoaded = false;
            SourceTabControl?.Items.Clear();

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
            SourceTabControl.Items.Clear();
            if (!string.IsNullOrWhiteSpace(XamlSource))
            {
                AddSourceTab("XAML", XamlSource, SourceLanguage.Xaml);
            }

            if (!string.IsNullOrWhiteSpace(CSharpSource))
            {
                AddSourceTab("C#", CSharpSource, SourceLanguage.CSharp);
            }
        }

        private void AddSourceTab(string header, string source, SourceLanguage language)
        {
            TabItem tab = new()
            {
                Header = header,
                Content = CreateSourcePane(source, language)
            };
            _ = SourceTabControl.Items.Add(tab);

            if (SourceTabControl.SelectedIndex < 0)
            {
                SourceTabControl.SelectedIndex = 0;
            }
        }

        private Grid CreateSourcePane(string source, SourceLanguage language)
        {
            Grid panel = new();

            RichTextBox viewer = CreateSourceViewer(source, language);
            _ = panel.Children.Add(viewer);

            Border copyButtonHost = CreateCopyButtonHost(CreateCopyButton(source));
            _ = panel.Children.Add(copyButtonHost);

            return panel;
        }

        private static Border CreateCopyButtonHost(Controls.Button copyButton)
        {
            Border border = new()
            {
                Name = "CopySourceButtonHost",
                Child = copyButton,
                CornerRadius = new CornerRadius(4),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = GetThicknessResource("DemoSourceCopyButtonHostMargin", new Thickness(0, 8, 8, 0)),
                VerticalAlignment = VerticalAlignment.Top
            };
            border.SetResourceReference(BackgroundProperty, "DemoSampleCardBackgroundBrush");
            return border;
        }

        private Controls.Button CreateCopyButton(string source)
        {
            Controls.Button button = new()
            {
                Name = "CopySourceButton",
                Appearance = ControlAppearance.Subtle,
                Content = "\uE8C8",
                HorizontalAlignment = HorizontalAlignment.Right,
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                MinWidth = 0,
                Padding = GetThicknessResource("DemoSourceCopyButtonPadding", new Thickness(8, 4, 8, 4)),
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
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsReadOnly = true,
                MinHeight = 220,
                Name = "SourceTextViewer",
                Padding = new Thickness(0),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            viewer.SetResourceReference(BackgroundProperty, "DemoSampleSourceContentBackgroundBrush");
            viewer.SetResourceReference(ForegroundProperty, "TextFillColorPrimaryBrush");
            viewer.Document = CreateSourceDocument(source, language);
            return viewer;
        }

        private static FlowDocument CreateSourceDocument(string source, SourceLanguage language)
        {
            FlowDocument document = new()
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                PagePadding = GetThicknessResource("DemoSourceCodeDocumentPadding", new Thickness(12))
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
            if (text.Length == 0)
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

        internal static void ApplySources(DependencyObject root, params string[] sourcePairs)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(root);
#else
            if (root is null)
            {
                throw new ArgumentNullException(nameof(root));
            }
#endif

            if (sourcePairs.Length % 2 != 0)
            {
                throw new ArgumentException("Source pairs must contain XAML and C# values.", nameof(sourcePairs));
            }

            List<DemoSampleControl> samples = [];
            CollectDemoSampleControls(root, samples);
            ApplyContentSlots(root, samples);

            int sampleCount = sourcePairs.Length / 2;
            if (samples.Count < sampleCount)
            {
                throw new InvalidOperationException("The page has fewer DemoSampleControl instances than source pairs.");
            }

            for (int i = 0; i < sampleCount; i++)
            {
                DemoSampleControl sample = samples[i];
                sample.XamlSource = sourcePairs[i * 2];
                sample.CSharpSource = sourcePairs[(i * 2) + 1];
            }
        }

        private static void ApplyContentSlots(DependencyObject root, IList<DemoSampleControl> samples)
        {
            Dictionary<int, ContentControl> demoSlots = [];
            Dictionary<int, ContentControl> outputSlots = [];
            Dictionary<int, ContentControl> rightRailSlots = [];
            CollectContentSlots(root, demoSlots, outputSlots, rightRailSlots);

            for (int i = 0; i < samples.Count; i++)
            {
                int slotIndex = i + 1;
                DemoSampleControl sample = samples[i];
                if (demoSlots.TryGetValue(slotIndex, out ContentControl? demoSlot))
                {
                    sample.DemoContent = TakeSlotContent(demoSlot);
                }

                if (outputSlots.TryGetValue(slotIndex, out ContentControl? outputSlot))
                {
                    sample.OutputContent = TakeSlotContent(outputSlot);
                }

                if (rightRailSlots.TryGetValue(slotIndex, out ContentControl? rightRailSlot))
                {
                    sample.RightRailContent = TakeSlotContent(rightRailSlot);
                }
            }
        }

        private static object? TakeSlotContent(ContentControl slot)
        {
            object? content = slot.Content;
            slot.Content = null;
            return content;
        }

        private static void CollectContentSlots(
            DependencyObject current,
            IDictionary<int, ContentControl> demoSlots,
            IDictionary<int, ContentControl> outputSlots,
            IDictionary<int, ContentControl> rightRailSlots)
        {
            if (current is ContentControl contentControl && !string.IsNullOrWhiteSpace(contentControl.Name))
            {
                AddSlotIfMatched(contentControl, "DemoContentHost", demoSlots);
                AddSlotIfMatched(contentControl, "OutputContentHost", outputSlots);
                AddSlotIfMatched(contentControl, "RightRailContentHost", rightRailSlots);
            }

            foreach (object child in LogicalTreeHelper.GetChildren(current))
            {
                if (child is DependencyObject childObject)
                {
                    CollectContentSlots(childObject, demoSlots, outputSlots, rightRailSlots);
                }
            }
        }

        private static void AddSlotIfMatched(
            ContentControl slot,
            string suffix,
            IDictionary<int, ContentControl> slots)
        {
            const string prefix = "DemoSampleSlot";
            string name = slot.Name;
            if (!name.StartsWith(prefix, StringComparison.Ordinal) ||
                !name.EndsWith(suffix, StringComparison.Ordinal))
            {
                return;
            }

            string indexText = name.Substring(prefix.Length, name.Length - prefix.Length - suffix.Length);
            if (int.TryParse(indexText, NumberStyles.None, CultureInfo.InvariantCulture, out int index))
            {
                slots[index] = slot;
            }
        }

        private static void CollectDemoSampleControls(DependencyObject current, ICollection<DemoSampleControl> samples)
        {
            if (current is DemoSampleControl sample)
            {
                samples.Add(sample);
                return;
            }

            foreach (object child in LogicalTreeHelper.GetChildren(current))
            {
                if (child is DependencyObject childObject)
                {
                    CollectDemoSampleControls(childObject, samples);
                }
            }
        }

        private static Thickness GetThicknessResource(string key, Thickness fallback)
        {
            return Application.Current?.TryFindResource(key) is Thickness value ? value : fallback;
        }
    }
}
