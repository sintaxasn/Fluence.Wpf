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

using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Fluent = Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryTypographyPage : UserControl
    {
        private const string TypographyTableXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Typography.TypographyTable""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width=""2.2*"" MinWidth=""180"" />
            <ColumnDefinition Width=""1.2*"" MinWidth=""140"" />
            <ColumnDefinition Width=""1*"" MinWidth=""120"" />
            <ColumnDefinition Width=""1.7*"" MinWidth=""180"" />
        </Grid.ColumnDefinitions>
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto"" />
            <RowDefinition Height=""Auto"" />
            <RowDefinition Height=""Auto"" />
        </Grid.RowDefinitions>

        <TextBlock
            Margin=""24,0,16,16""
            Style=""{StaticResource BodyStrongTextBlockStyle}""
            Text=""Example"" />
        <TextBlock
            Grid.Column=""1""
            Margin=""24,0,16,16""
            Style=""{StaticResource BodyStrongTextBlockStyle}""
            Text=""Variable Font"" />
        <TextBlock
            Grid.Column=""2""
            Margin=""24,0,16,16""
            Style=""{StaticResource BodyStrongTextBlockStyle}""
            Text=""Size/Line height"" />
        <TextBlock
            Grid.Column=""3""
            Margin=""24,0,16,16""
            Style=""{StaticResource BodyStrongTextBlockStyle}""
            Text=""Style"" />

        <TextBlock
            Grid.Row=""1""
            Margin=""24,16""
            Style=""{StaticResource BodyTextBlockStyle}""
            Text=""Body"" />
        <TextBlock
            Grid.Row=""1""
            Grid.Column=""1""
            Margin=""24,16""
            Style=""{StaticResource BodyTextBlockStyle}""
            Text=""Text, Regular"" />
        <TextBlock
            Grid.Row=""1""
            Grid.Column=""2""
            Margin=""24,16""
            Style=""{StaticResource BodyTextBlockStyle}""
            Text=""14/20 epx"" />
        <TextBlock
            Grid.Row=""1""
            Grid.Column=""3""
            Margin=""24,16""
            FontFamily=""Consolas""
            Style=""{StaticResource BodyTextBlockStyle}""
            Text=""BodyTextBlockStyle"" />

        <TextBlock
            Grid.Row=""2""
            Margin=""24,16""
            Style=""{StaticResource TitleTextBlockStyle}""
            Text=""Title"" />
        <TextBlock
            Grid.Row=""2""
            Grid.Column=""1""
            Margin=""24,16""
            Style=""{StaticResource BodyTextBlockStyle}""
            Text=""Display, SemiBold"" />
        <TextBlock
            Grid.Row=""2""
            Grid.Column=""2""
            Margin=""24,16""
            Style=""{StaticResource BodyTextBlockStyle}""
            Text=""28/36 epx"" />
        <TextBlock
            Grid.Row=""2""
            Grid.Column=""3""
            Margin=""24,16""
            FontFamily=""Consolas""
            Style=""{StaticResource BodyTextBlockStyle}""
            Text=""TitleTextBlockStyle"" />
    </Grid>
</UserControl>
";

        private const string TypographyTableCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Typography
{
    public partial class TypographyTable : UserControl
    {
        public TypographyTable()
        {
            InitializeComponent();
        }
    }
}
";

        private const string CopyGlyph = "\uE8C8";

        private static readonly TypographyRow[] Rows =
        [
            new("Caption", "Small, Regular", "12/16 epx", "CaptionTextBlockStyle"),
            new("Body", "Text, Regular", "14/20 epx", "BodyTextBlockStyle"),
            new("Body Strong", "Text, SemiBold", "14/20 epx", "BodyStrongTextBlockStyle"),
            new("Body Large", "Text, Regular", "18/24 epx", "BodyLargeTextBlockStyle"),
            new("Subtitle", "Display, SemiBold", "20/28 epx", "SubtitleTextBlockStyle"),
            new("Title", "Display, SemiBold", "28/36 epx", "TitleTextBlockStyle"),
            new("Title Large", "Display, SemiBold", "40/52 epx", "TitleLargeTextBlockStyle"),
            new("Display", "Display, SemiBold", "68/92 epx", "DisplayTextBlockStyle")
        ];

        public GalleryTypographyPage()
        {
            InitializeComponent();
            BuildTypographyTable();
            WrapTypographyTable();
        }

        private void BuildTypographyTable()
        {
            TypographyTable.RowDefinitions.Clear();
            TypographyTable.Children.Clear();
            TypographyTable.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddHeader(0, "Example");
            AddHeader(1, "Variable Font");
            AddHeader(2, "Size/Line height");
            AddHeader(3, "Style");

            for (int i = 0; i < Rows.Length; i++)
            {
                int rowIndex = i + 1;
                TypographyTable.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                AddTypographyRow(rowIndex, Rows[i], i % 2 == 0);
            }
        }

        private void AddHeader(int column, string text)
        {
            TextBlock header = CreateTextBlock(text, "BodyStrongTextBlockStyle", new Thickness(24, 0, 16, 16));
            AddCell(header, 0, column);
        }

        private void AddTypographyRow(int rowIndex, TypographyRow row, bool shaded)
        {
            if (shaded)
            {
                Border background = new()
                {
                    CornerRadius = new CornerRadius(6),
                    IsHitTestVisible = false,
                    Margin = new Thickness(0, 4, 0, 4)
                };
                background.SetResourceReference(Border.BackgroundProperty, "SubtleFillColorSecondaryBrush");
                Grid.SetRow(background, rowIndex);
                Grid.SetColumnSpan(background, 5);
                _ = TypographyTable.Children.Add(background);
            }

            AddCell(CreateTextBlock(row.Example, row.StyleKey, new Thickness(24, 16, 16, 16)), rowIndex, 0);
            AddCell(CreateTextBlock(row.VariableFont, "BodyTextBlockStyle", new Thickness(24, 16, 16, 16)), rowIndex, 1);
            AddCell(CreateTextBlock(row.SizeAndLineHeight, "BodyTextBlockStyle", new Thickness(24, 16, 16, 16)), rowIndex, 2);
            AddCell(CreateTextBlock(row.StyleKey, "BodyTextBlockStyle", new Thickness(24, 16, 16, 16)), rowIndex, 3);
            AddCell(CreateCopyButton(row.StyleKey), rowIndex, 4);
        }

        private static TextBlock CreateTextBlock(string text, string styleKey, Thickness margin)
        {
            TextBlock textBlock = new()
            {
                Margin = margin,
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            textBlock.SetResourceReference(StyleProperty, styleKey);
            return textBlock;
        }

        private static Fluent.Button CreateCopyButton(string styleKey)
        {
            Fluent.Button button = new()
            {
                Content = new Fluent.FontIcon { Glyph = CopyGlyph, IconFontSize = 16 },
                Height = 36,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(16, 8, 24, 8),
                MinWidth = 40,
                Padding = new Thickness(0),
                Tag = styleKey,
                ToolTip = "Copy style key",
                VerticalAlignment = VerticalAlignment.Center,
                Width = 40
            };
            AutomationProperties.SetName(button, "Copy " + styleKey);
            button.Click += CopyStyleKey_Click;
            return button;
        }

        private void AddCell(FrameworkElement element, int row, int column)
        {
            Grid.SetRow(element, row);
            Grid.SetColumn(element, column);
            _ = TypographyTable.Children.Add(element);
        }

        private void WrapTypographyTable()
        {
            if (TypographyTable.Parent is not Panel parent)
            {
                return;
            }

            int index = parent.Children.IndexOf(TypographyTable);
            TypographyTable.Margin = new Thickness(0);
            parent.Children.Remove(TypographyTable);
            parent.Children.Insert(index, new DemoSampleControl
            {
                Margin = new Thickness(0, 12, 0, 0),
                Title = "Typography scale",
                Description = "Supported text styles, variable font roles, and line-height guidance.",
                XamlSource = TypographyTableXamlSource,
                CSharpSource = TypographyTableCSharpSource,
                SampleContent = TypographyTable
            });
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S6670:\"Trace.Write\" and \"Trace.WriteLine\" should not be used", Justification = "Don't care for now.")]
        private static void CopyStyleKey_Click(object sender, RoutedEventArgs e)
        {
            string? styleKey = sender is Fluent.Button button ? button.Tag as string : null;
            if (string.IsNullOrWhiteSpace(styleKey))
            {
                return;
            }

            try
            {
                Clipboard.SetText(styleKey);
            }
            catch (ExternalException)
            {
                System.Diagnostics.Trace.WriteLine("Clipboard was unavailable while copying a typography style key.");
            }
            catch (ThreadStateException)
            {
                System.Diagnostics.Trace.WriteLine("Clipboard access requires an STA thread.");
            }
        }

        private sealed class TypographyRow(string example, string variableFont, string sizeAndLineHeight, string styleKey)
        {
            public string Example { get; private set; } = example;

            public string VariableFont { get; private set; } = variableFont;

            public string SizeAndLineHeight { get; private set; } = sizeAndLineHeight;

            public string StyleKey { get; private set; } = styleKey;
        }
    }
}
