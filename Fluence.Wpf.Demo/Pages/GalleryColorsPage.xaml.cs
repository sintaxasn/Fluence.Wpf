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
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FluenceBorder = Fluence.Wpf.Controls.Border;
using FluenceButton = Fluence.Wpf.Controls.Button;
using FluenceFontIcon = Fluence.Wpf.Controls.FontIcon;
using FluenceStackPanel = Fluence.Wpf.Controls.StackPanel;
using FluenceToggleButton = Fluence.Wpf.Controls.ToggleButton;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryColorsPage : UserControl
    {
        private const string SampleMarkup = "<TextBlock Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\" />";
        private const int TokensPerRow = 4;

        private static readonly ColorSection[] Sections =
        [
            new(
                "Text",
                "Text color resources provide primary, secondary, disabled, accent, and on-accent foreground roles.",
                "TextFillColorPrimaryBrush",
                "SolidBackgroundFillColorBaseBrush",
                "CardStrokeColorDefaultBrush",
                [
                    new("Primary text", "Primary labels and headings.", "TextFillColorPrimaryBrush"),
                    new("Secondary text", "Body text and supporting details.", "TextFillColorSecondaryBrush"),
                    new("Tertiary text", "Low emphasis metadata.", "TextFillColorTertiaryBrush"),
                    new("Disabled text", "Unavailable commands and values.", "TextFillColorDisabledBrush"),
                    new("Placeholder text", "Input placeholder content.", "TextPlaceholderColorBrush"),
                    new("Inverse text", "Text placed on inverse surfaces.", "TextFillColorInverseBrush"),
                    new("Accent text primary", "Links and accent-forward text.", "AccentTextFillColorPrimaryBrush"),
                    new("Accent text secondary", "Pressed or secondary accent text.", "AccentTextFillColorSecondaryBrush"),
                    new("Accent text tertiary", "Hover or tertiary accent text.", "AccentTextFillColorTertiaryBrush"),
                    new("Accent text disabled", "Disabled accent text.", "AccentTextFillColorDisabledBrush"),
                    new("On accent primary", "Text over accent fill.", "TextOnAccentFillColorPrimaryBrush"),
                    new("On accent secondary", "Secondary text over accent fill.", "TextOnAccentFillColorSecondaryBrush"),
                    new("On accent disabled", "Disabled text over accent fill.", "TextOnAccentFillColorDisabledBrush"),
                    new("Selected text", "Selected text over accent selection.", "TextOnAccentFillColorSelectedTextBrush")
                ]),
            new(
                "Fill",
                "Fill resources describe interactive control surfaces, subtle fills, accent fills, and on-image fills.",
                "TextOnAccentFillColorPrimaryBrush",
                "AccentFillColorDefaultBrush",
                "AccentControlElevationBorderBrush",
                [
                    new("Control default", "Resting control fill.", "ControlFillColorDefaultBrush"),
                    new("Control secondary", "Control hover fill.", "ControlFillColorSecondaryBrush"),
                    new("Control tertiary", "Control pressed fill.", "ControlFillColorTertiaryBrush"),
                    new("Control quaternary", "Alternative pressed fill.", "ControlFillColorQuarternaryBrush"),
                    new("Control input active", "Active input fill.", "ControlFillColorInputActiveBrush"),
                    new("Control disabled", "Disabled control fill.", "ControlFillColorDisabledBrush"),
                    new("Control transparent", "Transparent control fill.", "ControlFillColorTransparentBrush"),
                    new("Strong fill default", "Strong foreground fill.", "ControlStrongFillColorDefaultBrush"),
                    new("Strong fill disabled", "Disabled strong fill.", "ControlStrongFillColorDisabledBrush"),
                    new("Solid fill default", "Opaque control fill.", "ControlSolidFillColorDefaultBrush"),
                    new("Subtle transparent", "Transparent subtle fill.", "SubtleFillColorTransparentBrush"),
                    new("Subtle secondary", "Subtle hover fill.", "SubtleFillColorSecondaryBrush"),
                    new("Subtle tertiary", "Subtle pressed fill.", "SubtleFillColorTertiaryBrush"),
                    new("Subtle disabled", "Disabled subtle fill.", "SubtleFillColorDisabledBrush"),
                    new("Alt transparent", "Alternative transparent fill.", "ControlAltFillColorTransparentBrush"),
                    new("Alt secondary", "Alternative rest fill.", "ControlAltFillColorSecondaryBrush"),
                    new("Alt tertiary", "Alternative hover fill.", "ControlAltFillColorTertiaryBrush"),
                    new("Alt quaternary", "Alternative pressed fill.", "ControlAltFillColorQuarternaryBrush"),
                    new("Alt disabled", "Disabled alternative fill.", "ControlAltFillColorDisabledBrush"),
                    new("Accent default", "Primary accent fill.", "AccentFillColorDefaultBrush"),
                    new("Accent secondary", "Accent hover fill.", "AccentFillColorSecondaryBrush"),
                    new("Accent tertiary", "Accent pressed fill.", "AccentFillColorTertiaryBrush"),
                    new("Accent disabled", "Disabled accent fill.", "AccentFillColorDisabledBrush"),
                    new("Selected background", "Selected text background.", "AccentFillColorSelectedTextBackgroundBrush"),
                    new("On image default", "Controls over imagery.", "ControlOnImageFillColorDefaultBrush"),
                    new("On image secondary", "Hovered controls over imagery.", "ControlOnImageFillColorSecondaryBrush"),
                    new("On image tertiary", "Pressed controls over imagery.", "ControlOnImageFillColorTertiaryBrush"),
                    new("On image disabled", "Disabled controls over imagery.", "ControlOnImageFillColorDisabledBrush")
                ]),
            new(
                "Stroke",
                "Stroke resources define dividers, card borders, strong focus rings, surface strokes, and accent strokes.",
                "TextFillColorPrimaryBrush",
                "CardBackgroundFillColorDefaultBrush",
                "ControlStrokeColorDefaultBrush",
                [
                    new("Control stroke default", "Default control outline.", "ControlStrokeColorDefaultBrush"),
                    new("Control stroke secondary", "Secondary control outline.", "ControlStrokeColorSecondaryBrush"),
                    new("Control stroke tertiary", "Tertiary control outline.", "ControlStrokeColorTertiaryBrush"),
                    new("On accent default", "Stroke over accent fill.", "ControlStrokeColorOnAccentDefaultBrush"),
                    new("On accent secondary", "Secondary stroke over accent fill.", "ControlStrokeColorOnAccentSecondaryBrush"),
                    new("On accent tertiary", "Tertiary stroke over accent fill.", "ControlStrokeColorOnAccentTertiaryBrush"),
                    new("On accent disabled", "Disabled stroke over accent fill.", "ControlStrokeColorOnAccentDisabledBrush"),
                    new("Strong on image", "Stroke for strong fills on images.", "ControlStrokeColorForStrongFillWhenOnImageBrush"),
                    new("Card stroke", "Default card outline.", "CardStrokeColorDefaultBrush"),
                    new("Card stroke solid", "Opaque card outline.", "CardStrokeColorDefaultSolidBrush"),
                    new("Strong stroke", "Selection and focus rings.", "ControlStrongStrokeColorDefaultBrush"),
                    new("Strong stroke disabled", "Disabled strong stroke.", "ControlStrongStrokeColorDisabledBrush"),
                    new("Surface stroke", "Default surface outline.", "SurfaceStrokeColorDefaultBrush"),
                    new("Flyout stroke", "Popup and flyout outline.", "SurfaceStrokeColorFlyoutBrush"),
                    new("Inverse surface stroke", "Inverse surface outline.", "SurfaceStrokeColorInverseBrush"),
                    new("Divider stroke", "Inline dividers.", "DividerStrokeColorDefaultBrush")
                ]),
            new(
                "Background",
                "Background resources compose cards, layers, acrylic, mica, navigation content, and solid app surfaces.",
                "TextFillColorPrimaryBrush",
                "LayerFillColorDefaultBrush",
                "CardStrokeColorDefaultBrush",
                [
                    new("Card default", "Default card surface.", "CardBackgroundFillColorDefaultBrush"),
                    new("Card secondary", "Nested card surface.", "CardBackgroundFillColorSecondaryBrush"),
                    new("Card tertiary", "Deeper nested card surface.", "CardBackgroundFillColorTertiaryBrush"),
                    new("Smoke fill", "Modal smoke overlay.", "SmokeFillColorDefaultBrush"),
                    new("Layer default", "Default layer surface.", "LayerFillColorDefaultBrush"),
                    new("Layer alt", "Alternative layer surface.", "LayerFillColorAltBrush"),
                    new("Layer on acrylic", "Layer over acrylic.", "LayerOnAcrylicFillColorDefaultBrush"),
                    new("Layer on accent acrylic", "Layer over accent acrylic.", "LayerOnAccentAcrylicFillColorDefaultBrush"),
                    new("Mica base alt default", "Default layer on mica base alt.", "LayerOnMicaBaseAltFillColorDefaultBrush"),
                    new("Mica base alt secondary", "Secondary layer on mica base alt.", "LayerOnMicaBaseAltFillColorSecondaryBrush"),
                    new("Mica base alt tertiary", "Tertiary layer on mica base alt.", "LayerOnMicaBaseAltFillColorTertiaryBrush"),
                    new("Mica base alt transparent", "Transparent layer on mica base alt.", "LayerOnMicaBaseAltFillColorTransparentBrush"),
                    new("Solid base", "App base surface.", "SolidBackgroundFillColorBaseBrush"),
                    new("Solid secondary", "Secondary solid surface.", "SolidBackgroundFillColorSecondaryBrush"),
                    new("Solid tertiary", "Tertiary solid surface.", "SolidBackgroundFillColorTertiaryBrush"),
                    new("Solid quaternary", "Quaternary solid surface.", "SolidBackgroundFillColorQuarternaryBrush"),
                    new("Solid quinary", "Quinary solid surface.", "SolidBackgroundFillColorQuinaryBrush"),
                    new("Solid senary", "Senary solid surface.", "SolidBackgroundFillColorSenaryBrush"),
                    new("Solid transparent", "Transparent solid surface.", "SolidBackgroundFillColorTransparentBrush"),
                    new("Solid base alt", "Alternative base surface.", "SolidBackgroundFillColorBaseAltBrush"),
                    new("Navigation content", "Navigation content background.", "NavigationViewContentBackgroundBrush"),
                    new("Acrylic default", "Default acrylic fallback.", "AcrylicBackgroundFillColorDefaultBrush"),
                    new("Acrylic base", "Base acrylic fallback.", "AcrylicBackgroundFillColorBaseBrush")
                ]),
            new(
                "Signal",
                "Signal resources communicate attention, information, success, caution, critical, and neutral states.",
                "TextFillColorPrimaryBrush",
                "SystemFillColorSuccessBackgroundBrush",
                "SystemFillColorSuccessBrush",
                [
                    new("Attention", "Attention foreground.", "SystemFillColorAttentionBrush"),
                    new("Informational", "Informational foreground.", "SystemFillColorInformationalBrush"),
                    new("Success", "Success foreground.", "SystemFillColorSuccessBrush"),
                    new("Caution", "Caution foreground.", "SystemFillColorCautionBrush"),
                    new("Critical", "Critical foreground.", "SystemFillColorCriticalBrush"),
                    new("Neutral", "Neutral foreground.", "SystemFillColorNeutralBrush"),
                    new("Solid neutral", "Opaque neutral foreground.", "SystemFillColorSolidNeutralBrush"),
                    new("Attention background", "Attention background.", "SystemFillColorAttentionBackgroundBrush"),
                    new("Success background", "Success background.", "SystemFillColorSuccessBackgroundBrush"),
                    new("Caution background", "Caution background.", "SystemFillColorCautionBackgroundBrush"),
                    new("Critical background", "Critical background.", "SystemFillColorCriticalBackgroundBrush"),
                    new("Neutral background", "Neutral background.", "SystemFillColorNeutralBackgroundBrush"),
                    new("Solid attention background", "Opaque attention background.", "SystemFillColorSolidAttentionBackgroundBrush"),
                    new("Solid neutral background", "Opaque neutral background.", "SystemFillColorSolidNeutralBackgroundBrush")
                ]),
            new(
                "High Contrast",
                "High contrast resources map directly to system colors so content follows the active Windows contrast theme.",
                "SystemColorWindowTextColorBrush",
                "SystemColorWindowColorBrush",
                "SystemColorHighlightColorBrush",
                [
                    new("Window text", "System window foreground.", "SystemColorWindowTextColorBrush"),
                    new("Window", "System window background.", "SystemColorWindowColorBrush"),
                    new("Button face", "System button face.", "SystemColorButtonFaceColorBrush"),
                    new("Button text", "System button text.", "SystemColorButtonTextColorBrush"),
                    new("Highlight", "System selection background.", "SystemColorHighlightColorBrush"),
                    new("Highlight text", "System selection foreground.", "SystemColorHighlightTextColorBrush"),
                    new("Hotlight", "System link color.", "SystemColorHotlightColorBrush"),
                    new("Gray text", "System disabled text.", "SystemColorGrayTextColorBrush")
                ])
        ];

        public GalleryColorsPage()
        {
            InitializeComponent();
            CopyCodeSampleButton.Tag = SampleMarkup;
            BuildSectionSelector();
            ShowSection(0, false);
        }

        private void BuildSectionSelector()
        {
            for (int i = 0; i < Sections.Length; i++)
            {
                FluenceToggleButton button = new()
                {
                    Content = Sections[i].Title,
                    CornerRadius = new CornerRadius(16),
                    IsChecked = i == 0,
                    MinWidth = 0,
                    Padding = new Thickness(23, 5, 23, 6),
                    Tag = i
                };
                button.Click += SectionButton_Click;
                _ = SectionSelectorHost.Children.Add(button);
            }
        }

        private UIElement CreateSection(ColorSection section)
        {
            FluenceStackPanel sectionPanel = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 4
            };
            _ = sectionPanel.Children.Add(CreateExamplePanel(section));
            _ = sectionPanel.Children.Add(CreateTokenRows(section.Tokens));
            return sectionPanel;
        }

        private void SectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FluenceToggleButton button || button.Tag is not int selectedIndex)
            {
                return;
            }

            ShowSection(selectedIndex, true);
        }

        private void ShowSection(int selectedIndex, bool animate)
        {
            int previousIndex = GetSelectedIndex();
            for (int i = 0; i < SectionSelectorHost.Children.Count; i++)
            {
                if (SectionSelectorHost.Children[i] is FluenceToggleButton button)
                {
                    button.IsChecked = i == selectedIndex;
                }
            }

            UIElement sectionContent = CreateSection(Sections[selectedIndex]);
            SectionContentHost.Content = sectionContent;

            if (animate)
            {
                AnimateSectionContent(sectionContent, selectedIndex >= previousIndex);
            }
        }

        private int GetSelectedIndex()
        {
            for (int i = 0; i < SectionSelectorHost.Children.Count; i++)
            {
                if (SectionSelectorHost.Children[i] is FluenceToggleButton { IsChecked: true })
                {
                    return i;
                }
            }

            return 0;
        }

        private static void AnimateSectionContent(UIElement element, bool forward)
        {
            element.BeginAnimation(OpacityProperty, null);
            TranslateTransform transform = new(forward ? 20.0 : -20.0, 0.0);
            element.RenderTransform = transform;
            element.Opacity = 0.0;

            CubicEase easing = new() { EasingMode = EasingMode.EaseOut };
            DoubleAnimation opacityAnimation = new(0.0, 1.0, new Duration(TimeSpan.FromMilliseconds(167)))
            {
                EasingFunction = easing
            };
            opacityAnimation.Completed += delegate
            {
                element.BeginAnimation(OpacityProperty, null);
                element.Opacity = 1.0;
            };
            element.BeginAnimation(OpacityProperty, opacityAnimation);

            DoubleAnimation slideAnimation = new(transform.X, 0.0, new Duration(TimeSpan.FromMilliseconds(167)))
            {
                EasingFunction = easing
            };
            slideAnimation.Completed += delegate
            {
                transform.BeginAnimation(TranslateTransform.XProperty, null);
                transform.X = 0.0;
            };
            transform.BeginAnimation(TranslateTransform.XProperty, slideAnimation);
        }

        private UIElement CreateExamplePanel(ColorSection section)
        {
            FluenceBorder panel = new()
            {
                Margin = new Thickness(0, 36, 0, 8),
                Padding = new Thickness(12),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8)
            };
            panel.SetResourceReference(Border.BackgroundProperty, "SolidBackgroundFillColorBaseBrush");
            panel.SetResourceReference(Border.BorderBrushProperty, "CardStrokeColorDefaultBrush");

            FluenceStackPanel stack = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 8
            };
            _ = stack.Children.Add(CreateText(section.Title + " resources", "BodyStrongTextBlockStyle", "TextFillColorPrimaryBrush"));
            _ = stack.Children.Add(CreateText(section.Description, null, "TextFillColorSecondaryBrush"));
            _ = stack.Children.Add(CreatePreviewSurface(section));
            panel.Child = stack;
            return panel;
        }

        private static UIElement CreatePreviewSurface(ColorSection section)
        {
            FluenceBorder preview = new()
            {
                MinHeight = 92,
                Padding = new Thickness(16),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8)
            };
            preview.SetResourceReference(Border.BackgroundProperty, section.ExampleBackgroundKey);
            preview.SetResourceReference(Border.BorderBrushProperty, section.ExampleBorderKey);

            TextBlock textBlock = new()
            {
                Text = section.Title + " preview",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            textBlock.SetResourceReference(TextBlock.ForegroundProperty, section.ExampleForegroundKey);
            preview.Child = textBlock;
            return preview;
        }

        private UIElement CreateTokenRows(ColorToken[] tokens)
        {
            FluenceStackPanel rows = new()
            {
                Orientation = Orientation.Vertical,
                Spacing = 4
            };

            for (int start = 0; start < tokens.Length; start += TokensPerRow)
            {
                int count = Math.Min(TokensPerRow, tokens.Length - start);
                UniformGrid rowGrid = new()
                {
                    Columns = count,
                    Rows = 1
                };

                for (int offset = 0; offset < count; offset++)
                {
                    _ = rowGrid.Children.Add(CreateTokenTile(tokens[start + offset], offset < count - 1));
                }

                FluenceBorder rowBorder = new()
                {
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Child = rowGrid,
                    Tag = "ColorTokenRow"
                };
                rowBorder.SetResourceReference(Border.BackgroundProperty, "SolidBackgroundFillColorBaseBrush");
                rowBorder.SetResourceReference(Border.BorderBrushProperty, "CardStrokeColorDefaultBrush");
                _ = rows.Children.Add(rowBorder);
            }

            return rows;
        }

        private UIElement CreateTokenTile(ColorToken token, bool showSeparator)
        {
            Grid tile = new()
            {
                MinHeight = 132,
                Tag = token.ResourceKey
            };
            tile.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tile.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            FluenceStackPanel content = new()
            {
                Margin = new Thickness(12),
                Orientation = Orientation.Vertical,
                Spacing = 6
            };
            _ = content.Children.Add(CreateTokenHeader(token));
            _ = content.Children.Add(CreateText(token.Description, null, "TextFillColorSecondaryBrush", 12));
            _ = content.Children.Add(CreateResourceKeyRow(token.ResourceKey));
            Grid.SetColumn(content, 0);
            _ = tile.Children.Add(content);

            if (showSeparator)
            {
                Border separator = new()
                {
                    Width = 1,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                separator.SetResourceReference(Border.BackgroundProperty, "DividerStrokeColorDefaultBrush");
                Grid.SetColumn(separator, 1);
                _ = tile.Children.Add(separator);
            }

            return tile;
        }

        private UIElement CreateTokenHeader(ColorToken token)
        {
            Grid header = new();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Border swatch = new()
            {
                Width = 24,
                Height = 24,
                Margin = new Thickness(0, 1, 10, 0),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                VerticalAlignment = VerticalAlignment.Top
            };
            swatch.SetResourceReference(Border.BackgroundProperty, token.ResourceKey);
            swatch.SetResourceReference(Border.BorderBrushProperty, "DividerStrokeColorDefaultBrush");
            Grid.SetColumn(swatch, 0);
            _ = header.Children.Add(swatch);

            TextBlock title = CreateText(token.Title, null, "TextFillColorPrimaryBrush", 13);
            title.FontWeight = FontWeights.SemiBold;
            Grid.SetColumn(title, 1);
            _ = header.Children.Add(title);

            FluenceButton copyButton = new()
            {
                Appearance = ControlAppearance.Subtle,
                Icon = new FluenceFontIcon { Glyph = "\uE8C8", IconFontSize = 14 },
                MinHeight = 28,
                MinWidth = 28,
                Padding = new Thickness(6, 3, 6, 3),
                Tag = token.ResourceKey,
                ToolTip = "Copy " + token.ResourceKey
            };
            copyButton.Click += CopyTokenButton_Click;
            Grid.SetColumn(copyButton, 2);
            _ = header.Children.Add(copyButton);

            return header;
        }

        private UIElement CreateResourceKeyRow(string resourceKey)
        {
            TextBlock resourceText = CreateText(resourceKey, null, "TextFillColorTertiaryBrush", 12);
            resourceText.FontFamily = new FontFamily("Cascadia Mono, Consolas");
            resourceText.TextWrapping = TextWrapping.Wrap;
            return resourceText;
        }

        private TextBlock CreateText(string text, string? styleKey, string foregroundKey, double? fontSize = null)
        {
            TextBlock textBlock = new()
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap
            };
            textBlock.SetResourceReference(TextBlock.ForegroundProperty, foregroundKey);

            if (styleKey is not null && TryFindResource(styleKey) is Style style)
            {
                textBlock.Style = style;
            }

            if (fontSize is not null)
            {
                textBlock.FontSize = fontSize.Value;
            }

            return textBlock;
        }

        private void CopyCodeSampleButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(SampleMarkup);
        }

        private void CopyTokenButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FluenceButton { Tag: string resourceKey } && !string.IsNullOrWhiteSpace(resourceKey))
            {
                Clipboard.SetText(resourceKey);
            }
        }

        private sealed class ColorSection(
            string title,
            string description,
            string exampleForegroundKey,
            string exampleBackgroundKey,
            string exampleBorderKey,
            ColorToken[] tokens)
        {
            public string Title { get; } = title;

            public string Description { get; } = description;

            public string ExampleForegroundKey { get; } = exampleForegroundKey;

            public string ExampleBackgroundKey { get; } = exampleBackgroundKey;

            public string ExampleBorderKey { get; } = exampleBorderKey;

            public ColorToken[] Tokens { get; } = tokens;
        }

        private sealed class ColorToken(string title, string description, string resourceKey)
        {
            public string Title { get; } = title;

            public string Description { get; } = description;

            public string ResourceKey { get; } = resourceKey;
        }
    }
}
