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
using System.Windows.Controls.Primitives;

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>
    /// Colors design-reference page, a WPF rendering of the WinUI 3 Gallery Color page.
    /// </summary>
    /// <remarks>
    /// The six sections below are transcribed from
    /// <c language="text">WinUIGallery/Controls/DesignGuidance/ColorSections/*Section.xaml</c>: every group is one
    /// <see cref="ColorPageExample"/> card followed by rows of <see cref="ColorTile"/> swatches, in the
    /// Gallery's order and with the Gallery's column counts. Each tile is painted in the brush it names and
    /// takes its text brush from the Gallery's per-tile pairing. Every brush, size, margin and radius is a
    /// resource reference, so the whole page follows theme, accent and high contrast changes.
    /// </remarks>
    public partial class GalleryColorsPage : UserControl
    {
        private const string PrimaryText = "TextFillColorPrimaryBrush";
        private const string InverseText = "TextFillColorInverseBrush";
        private const string OnAccentText = "TextOnAccentFillColorPrimaryBrush";
        private const string QuarternarySurface = "SolidBackgroundFillColorQuarternaryBrush";
        private const string CardStroke = "CardStrokeColorDefaultBrush";
        private const string SingleStroke = "DemoSingleBorderThickness";
        private const string ControlRadius = "ControlCornerRadius";
        private const string OverlayRadius = "OverlayCornerRadius";
        private const string SurfaceWidth = "DemoColorExampleSurfaceWidth";
        private const string SurfaceHeight = "DemoColorExampleSurfaceHeight";

        private static readonly ColorSectionData[] Sections =
        [
            new(
                "Text",
                intro: null,
                [
                    new(
                        "Text",
                        "For UI labels and static text.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateGlyph(PrimaryText),
                        [
                            new(rows: 1,
                            [
                                new("Text / Primary", "Rest or Hover", "TextFillColorPrimaryBrush", OnAccentText),
                                new("Text / Secondary", "Rest or Hover", "TextFillColorSecondaryBrush", OnAccentText),
                                new("Text / Tertiary", "Pressed only (not accessible)", "TextFillColorTertiaryBrush", OnAccentText),
                                new("Text / Disabled", "Disabled only (not accessible)", "TextFillColorDisabledBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Accent Text",
                        "Recommended for links.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateGlyph("AccentTextFillColorPrimaryBrush"),
                        [
                            new(rows: 1,
                            [
                                new("Accent Text / Primary", "Rest or Hover", "AccentTextFillColorPrimaryBrush", OnAccentText),
                                new("Accent Text / Secondary", "Rest or Hover", "AccentTextFillColorSecondaryBrush", OnAccentText),
                                new("Accent Text / Tertiary", "Pressed only (not accessible)", "AccentTextFillColorTertiaryBrush", OnAccentText),
                                new("Accent Text / Disabled", "Disabled only (not accessible)", "AccentTextFillColorDisabledBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Text On Accent",
                        "Used for text on accent colored controls or fills.",
                        "AccentFillColorDefaultBrush",
                        OnAccentText,
                        static () => CreateGlyph(OnAccentText),
                        [
                            new(rows: 1,
                            [
                                new("Text on Accent / Primary", "Rest or Hover", "TextOnAccentFillColorPrimaryBrush", PrimaryText),
                                new("Text on Accent / Secondary", "Pressed only (not accessible)", "TextOnAccentFillColorSecondaryBrush", PrimaryText),
                            ]),
                            new(rows: 1,
                            [
                                new("Text on Accent / Disabled", "Disabled only (not accessible)", "TextOnAccentFillColorDisabledBrush", foregroundKey: null),
                                new("Text on Accent / Selected Text", "For highlighted text in text entry experiences", "TextOnAccentFillColorSelectedTextBrush", foregroundKey: null),
                            ]),
                        ]),
                ]),
            new(
                "Fill",
                intro: null,
                [
                    new(
                        "Control Fill",
                        "Fill used for standard controls.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => new Controls.Button { Content = "Text" },
                        [
                            new(rows: 1,
                            [
                                new("Control / Default", "Rest", "ControlFillColorDefaultBrush", PrimaryText),
                                new("Control / Secondary", "Hover", "ControlFillColorSecondaryBrush", PrimaryText),
                                new("Control / Tertiary", "Pressed", "ControlFillColorTertiaryBrush", PrimaryText),
                                new("Control / Quarternary", "Rest (Pill Button control)", "ControlFillColorQuarternaryBrush", PrimaryText),
                            ]),
                            new(rows: 1,
                            [
                                new("Control / Disabled", "Disabled", "ControlFillColorDisabledBrush", PrimaryText),
                                new("Control / Transparent", "Rest", "ControlFillColorTransparentBrush", PrimaryText),
                                new("Control / Input Active", "Active/focused text input fields", "ControlFillColorInputActiveBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Control Alt Fill",
                        "Fill used for the 'off' states of toggle controls.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateToggleSwitch(narrow: false),
                        [
                            new(rows: 1,
                            [
                                new("Control Alt / Transparent", string.Empty, "ControlAltFillColorTransparentBrush", PrimaryText),
                                new("Control Alt / Secondary", "Rest", "ControlAltFillColorSecondaryBrush", PrimaryText),
                                new("Control Alt / Tertiary", "Hover", "ControlAltFillColorTertiaryBrush", PrimaryText),
                            ]),
                            new(rows: 1,
                            [
                                new("Control Alt / Quarternary", "Pressed", "ControlAltFillColorQuarternaryBrush", PrimaryText),
                                new("Control Alt / Disabled", "Disabled", "ControlAltFillColorDisabledBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Neutral Solid",
                        "Fills used for Sliders thumb control to cover the track beneath it.",
                        QuarternarySurface,
                        foregroundKey: null,
                        CreateSlider,
                        [
                            new(rows: 1,
                            [
                                new("Control Solid / Default", "Rest", "ControlSolidFillColorDefaultBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Neutral Strong",
                        "Used for controls that must meet contrast ratio requirements of 3:1.",
                        QuarternarySurface,
                        foregroundKey: null,
                        CreateScrollBar,
                        [
                            new(rows: 1,
                            [
                                new("Control Strong / Default", "Rest or hover", "ControlStrongFillColorDefaultBrush", InverseText),
                                new("Control Strong / Disabled", "Disabled only (not accessible)", "ControlStrongFillColorDisabledBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Subtle Fill",
                        "Used for list items and fills that are transparent at rest and appear upon interaction.",
                        QuarternarySurface,
                        foregroundKey: null,
                        CreateSubtleFillExample,
                        [
                            new(rows: 1,
                            [
                                new("Subtle / Transparent", "Rest", "SubtleFillColorTransparentBrush", PrimaryText),
                                new("Subtle / Secondary", "Hover", "SubtleFillColorSecondaryBrush", PrimaryText),
                                new("Subtle / Tertiary", "Pressed", "SubtleFillColorTertiaryBrush", PrimaryText),
                                new("Subtle / Disabled", "Disabled only (not accessible)", "SubtleFillColorDisabledBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Control On Image Fill",
                        "Used for controls living on top of imagery.",
                        QuarternarySurface,
                        foregroundKey: null,
                        CreateControlOnImageExample,
                        [
                            new(rows: 1,
                            [
                                new("Control On Image Fill Default", "Rest", "ControlOnImageFillColorDefaultBrush", PrimaryText),
                                new("Control On Image Fill Secondary", "Hover", "ControlOnImageFillColorSecondaryBrush", PrimaryText),
                                new("Control On Image Fill Tertiary", "Pressed", "ControlOnImageFillColorTertiaryBrush", PrimaryText),
                                new("Control On Image Fill Disabled", "Disabled only (not accessible)", "ControlOnImageFillColorDisabledBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Accent Fill",
                        "Used for accent fills on controls.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => new Controls.Button { Content = "Text", Appearance = ControlAppearance.Accent },
                        [
                            new(rows: 1,
                            [
                                new("Accent / Default", "Rest", "AccentFillColorDefaultBrush", OnAccentText),
                                new("Accent / Secondary", "Hover", "AccentFillColorSecondaryBrush", OnAccentText),
                                new("Accent / Tertiary", "Pressed", "AccentFillColorTertiaryBrush", OnAccentText),
                            ]),
                            new(rows: 1,
                            [
                                new("Accent / Disabled", "Disabled", "AccentFillColorDisabledBrush", PrimaryText),
                                new("Accent / Selected Text Background", "Highlighted/selected text background", "AccentFillColorSelectedTextBackgroundBrush", OnAccentText),
                            ]),
                        ]),
                ]),
            new(
                "Stroke",
                intro: null,
                [
                    new(
                        "Card Stroke",
                        "Used for card and layer colors.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateSurface("CardBackgroundFillColorDefaultBrush", CardStroke, ControlRadius, "DemoColorExampleCardWidth", "DemoColorExampleCardStrokeHeight"),
                        [
                            new(rows: 1,
                            [
                                new("Card Stroke / Default", "Card layer and strokes", "CardStrokeColorDefaultBrush", PrimaryText),
                                new("Card Stroke / Default Solid", "Solid equivalent of Card Stroke / Default. Used in command bar for expanded states", "CardStrokeColorDefaultSolidBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Control Elevation (gradient strokes)",
                        "Used for standard control strokes and stroke states.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => new Controls.Button { Content = "Text" },
                        [
                            new(rows: 1,
                            [
                                new("Control / Border", "Rest", "ControlElevationBorderBrush", PrimaryText),
                                new("Circle / Border", "Rest", "CircleElevationBorderBrush", PrimaryText),
                                new("Text Control / Border", "Rest", "TextControlElevationBorderBrush", PrimaryText),
                            ]),
                            new(rows: 1,
                            [
                                new("Text Control / Border Focused", "Active text fields", "TextControlElevationBorderFocusedBrush", PrimaryText),
                                new("Accent Control / Border", "Rest", "AccentControlElevationBorderBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Control Stroke",
                        "Used for gradient stops in elevation borders, and for control states.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => new Controls.Button { Content = "Text" },
                        [
                            new(rows: 1,
                            [
                                new("Control Stroke / Default", "Used in Control Elevation Brushes. Pressed or Disabled", "ControlStrokeColorDefaultBrush", PrimaryText),
                                new("Control Stroke / Secondary", "Used in Control Elevation Brushes", "ControlStrokeColorSecondaryBrush", PrimaryText),
                                new("Control Stroke / On Accent Default", "Used in Control Elevation Brushes. Pressed or Disabled", "ControlStrokeColorOnAccentDefaultBrush", PrimaryText),
                                new("Control Stroke / On Accent Secondary", "Used in Control Elevation Brushes", "ControlStrokeColorOnAccentSecondaryBrush", PrimaryText),
                            ]),
                            new(rows: 1,
                            [
                                new("Control Stroke / On Accent Tertiary", "Linework on Accent controls, ie: dividers", "ControlStrokeColorOnAccentTertiaryBrush", PrimaryText),
                                new("Control Stroke / On Accent Disabled", "Disabled", "ControlStrokeColorOnAccentDisabledBrush", PrimaryText),
                                new("Control Stroke / For Strong Fill When On Image", "When used with a 'strong' fill color, ensures a 3:1 contrast on any background", "ControlStrokeColorForStrongFillWhenOnImageBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Control Strong Stroke",
                        "Used for control strokes that must meet contrast ratio requirements of 3:1.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateToggleSwitch(narrow: true),
                        [
                            new(rows: 1,
                            [
                                new("Control Strong Stroke / Default", "3:1 control border", "ControlStrongStrokeColorDefaultBrush", InverseText),
                                new("Control Strong Stroke / Disabled", "Disabled", "ControlStrongStrokeColorDisabledBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Surface Stroke",
                        "Used for strokes on background surfaces, ie: flyouts, windows, dialogs.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateSurface("AcrylicBackgroundFillColorBaseBrush", "SurfaceStrokeColorDefaultBrush", OverlayRadius, SurfaceWidth, SurfaceHeight),
                        [
                            new(rows: 1,
                            [
                                new("Surface Stroke / Default", "Window and dialog borders, theme inverse", "SurfaceStrokeColorDefaultBrush", PrimaryText),
                                new("Surface Stroke / Flyout", "Control flyouts, always dark", "SurfaceStrokeColorFlyoutBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Divider Stroke",
                        "Used for divider and graphic lines.",
                        QuarternarySurface,
                        foregroundKey: null,
                        CreateDividerExample,
                        [
                            new(rows: 1,
                            [
                                new("Divider Stroke / Default", "Content dividers", "DividerStrokeColorDefaultBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Focus Stroke",
                        "Used for divider and graphic lines. Theme inverse; dark in light theme and light in dark theme.",
                        QuarternarySurface,
                        foregroundKey: null,
                        CreateFocusExample,
                        [
                            new(rows: 1,
                            [
                                new("Focus / Outer", "Outer stroke color", "FocusStrokeColorOuterBrush", InverseText),
                                new("Focus / Inner", "Inner stroke color", "FocusStrokeColorInnerBrush", PrimaryText),
                            ]),
                        ]),
                ]),
            new(
                "Background",
                intro: null,
                [
                    new(
                        "Card Background",
                        "Used to create 'cards' - content blocks that live on page and layer backgrounds.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateSurface("CardBackgroundFillColorDefaultBrush", CardStroke, ControlRadius, "DemoColorExampleCardWidth", "DemoColorExampleCardBackgroundHeight"),
                        [
                            new(rows: 1,
                            [
                                new("Card Background / Default", "Default card color", "CardBackgroundFillColorDefaultBrush", PrimaryText),
                                new("Card Background / Secondary", "Alternate card color: slightly darker", "CardBackgroundFillColorSecondaryBrush", PrimaryText),
                                new("Card Background / Tertiary", "Default card hover and pressed color", "CardBackgroundFillColorTertiaryBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Smoke Background",
                        "Used over windows and desktop to block them out as inaccessible.",
                        "SmokeFillColorDefaultBrush",
                        foregroundKey: null,
                        static () => CreateSurface("CardBackgroundFillColorDefaultBrush", CardStroke, OverlayRadius, SurfaceWidth, SurfaceHeight),
                        [
                            new(rows: 1,
                            [
                                new("Smoke / Default", "Dims the background behind dialogs", "SmokeFillColorDefaultBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Layer",
                        "Used on background colors of any material to create layering.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateLayerExample("AcrylicBackgroundFillColorBaseBrush", "LayerFillColorDefaultBrush"),
                        [
                            new(rows: 1,
                            [
                                new("Layer / Default", "Content layer color", "LayerFillColorDefaultBrush", PrimaryText),
                                new("Layer / Alt", "Alternate content layer color", "LayerFillColorAltBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Layer on Acrylic",
                        "Used on background colors of any material to create layering.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateLayerExample(backgroundKey: null, "LayerOnAcrylicFillColorDefaultBrush"),
                        [
                            new(rows: 1,
                            [
                                new("Layer On Acrylic / Default", "Content layer color on acrylic surfaces", "LayerOnAcrylicFillColorDefaultBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Layer on Mica Base Alt",
                        "Used for fills on Tab control.",
                        QuarternarySurface,
                        foregroundKey: null,
                        CreateTabExample,
                        [
                            new(rows: 1,
                            [
                                new("Layer On Mica Base Alt / Default", "Active Tab Rest, Content layer", "LayerOnMicaBaseAltFillColorDefaultBrush", PrimaryText),
                                new("Layer On Mica Base Alt / Tertiary", "Active Tab Drag", "LayerOnMicaBaseAltFillColorTertiaryBrush", PrimaryText),
                                new("Layer On Mica Base Alt / Transparent", "Inactive Tab Rest", "LayerOnMicaBaseAltFillColorTransparentBrush", PrimaryText),
                                new("Layer On Mica Base Alt / Secondary", "Inactive Tab Hover", "LayerOnMicaBaseAltFillColorSecondaryBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Solid Background",
                        "Solid background colors to place layers, cards or controls on.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateSurface("SolidBackgroundFillColorBaseBrush", CardStroke, ControlRadius, SurfaceWidth, SurfaceHeight),
                        [
                            new(rows: 1,
                            [
                                new("Solid Background / Base", "Used for the bottom most layer of an experience", "SolidBackgroundFillColorBaseBrush", PrimaryText),
                                new("Solid Background / Base Alt", "Used for the bottom most layer of an experience", "SolidBackgroundFillColorBaseAltBrush", PrimaryText),
                                new("Solid Background / Secondary", "Alternate base color for those who need a darker background color", "SolidBackgroundFillColorSecondaryBrush", PrimaryText),
                                new("Solid Background / Tertiary", "Content layer color", "SolidBackgroundFillColorTertiaryBrush", PrimaryText),
                            ]),
                            new(rows: 1,
                            [
                                new("Solid Background / Quarternary", "Alt content layer color", "SolidBackgroundFillColorQuarternaryBrush", PrimaryText),
                                new("Solid Background / Quinary", "Used for solid default card colors", "SolidBackgroundFillColorQuinaryBrush", PrimaryText),
                                new("Solid Background / Senary", "Used for solid default card colors", "SolidBackgroundFillColorSenaryBrush", PrimaryText),
                            ]),
                        ]),
                    new(
                        "Acrylic Background",
                        "Acrylic background colors to place layers, cards, or controls on.",
                        QuarternarySurface,
                        foregroundKey: null,
                        static () => CreateSurface("AcrylicBackgroundFillColorBaseBrush", CardStroke, OverlayRadius, SurfaceWidth, SurfaceHeight),
                        [
                            new(rows: 1,
                            [
                                new("Acrylic Background / Base", "Used for the bottom most layer of an acrylic surface only when the surface will use layers", "AcrylicBackgroundFillColorBaseBrush", PrimaryText),
                                new("Acrylic Background / Default", "Default acrylic recipe used for control flyouts and surfaces that live with in the context of an app", "AcrylicBackgroundFillColorDefaultBrush", PrimaryText),
                            ]),
                        ]),
                ]),
            new(
                "Signal",
                intro: null,
                [
                    new(
                        "System",
                        "Used for accent fills on controls.",
                        QuarternarySurface,
                        foregroundKey: null,
                        CreateInfoBar,
                        [
                            new(rows: 1,
                            [
                                new("System / Success", "Badge", "SystemFillColorSuccessBrush", InverseText),
                                new("System / Caution", "Badge", "SystemFillColorCautionBrush", InverseText),
                                new("System / Critical", "Badge", "SystemFillColorCriticalBrush", InverseText),
                            ]),
                            new(rows: 1,
                            [
                                new("System / Success Background", "Infobar Background", "SystemFillColorSuccessBackgroundBrush", PrimaryText),
                                new("System / Caution Background", "Infobar Background", "SystemFillColorCautionBackgroundBrush", PrimaryText),
                                new("System / Critical Background", "Infobar Background", "SystemFillColorCriticalBackgroundBrush", PrimaryText),
                            ]),
                            new(rows: 1,
                            [
                                new("System / Attention", "Badge", "SystemFillColorAttentionBrush", InverseText),
                                new("System / Neutral", "Badge", "SystemFillColorNeutralBrush", InverseText),
                                new("System / Solid Neutral", "Neutral badges over content", "SystemFillColorSolidNeutralBrush", InverseText),
                            ]),
                            new(rows: 1,
                            [
                                new("System / Attention Background", "Infobar Background", "SystemFillColorAttentionBackgroundBrush", PrimaryText),
                                new("System / Neutral Background", "Infobar Background", "SystemFillColorNeutralBackgroundBrush", PrimaryText),
                                new("System / Solid Neutral Background", "Neutral badges over content", "SystemFillColorSolidNeutralBackgroundBrush", PrimaryText),
                            ]),
                            new(rows: 1,
                            [
                                new("System / Solid Attention Background", string.Empty, "SystemFillColorSolidAttentionBackgroundBrush", PrimaryText),
                            ]),
                        ]),
                ]),
            new(
                "High Contrast",
                "Brush names are the same in every theme; Windows chooses the colors from the active contrast theme. The tiles below show the live system colors.",
                [
                    new(
                        title: null,
                        string.Empty,
                        QuarternarySurface,
                        foregroundKey: null,
                        example: null,
                        [
                            new(rows: 2,
                            [
                                new("Window Text Color", "Foreground / Text color for Headings, body copy, lists, placeholder text, app and window borders, any UI that can't be interacted with", "SystemColorWindowTextColorBrush", "SystemColorWindowColorBrush"),
                                new("Highlight Text Color", "Foreground color for text or UI that is selected, interacted with (hover, pressed), or in progress", "SystemColorHighlightTextColorBrush", "SystemColorHighlightColorBrush"),
                                new("Button Text Color", "Foreground color for buttons and any UI that can be interacted with", "SystemColorButtonTextColorBrush", "SystemColorButtonFaceColorBrush"),
                                new("Hotlight Color", "Foreground / Text color for hyperlink text", "SystemColorHotlightColorBrush", "SystemColorWindowColorBrush"),
                                new("Window Color", "Background of pages, panes, popups, and windows", "SystemColorWindowColorBrush", "SystemColorWindowTextColorBrush"),
                                new("Highlight Color", "Background or accent color for UI that is selected, interacted with (hover, pressed), or in progress", "SystemColorHighlightColorBrush", "SystemColorHighlightTextColorBrush"),
                                new("Button Face Color", "Background color for buttons and any UI that can be interacted with", "SystemColorButtonFaceColorBrush", "SystemColorButtonTextColorBrush"),
                                new("Gray Text Color / Disabled", "Foreground / Text color for Inactive (disabled) UI", "SystemColorGrayTextColorBrush", "SystemColorWindowColorBrush"),
                            ]),
                        ]),
                ]),
        ];

        /// <summary>
        /// Initializes a new instance of the <see cref="GalleryColorsPage"/> class.
        /// </summary>
        public GalleryColorsPage()
        {
            InitializeComponent();
            BuildSections();
        }

        private void BuildSections()
        {
            if (ColorSectionTabs.Items.Count != Sections.Length)
            {
                throw new InvalidOperationException("The Colors page declares " + ColorSectionTabs.Items.Count.ToString(CultureInfo.InvariantCulture) + " tabs but " + Sections.Length.ToString(CultureInfo.InvariantCulture) + " sections.");
            }

            for (int i = 0; i < Sections.Length; i++)
            {
                TabItem tab = (TabItem)ColorSectionTabs.Items[i];
                if (!string.Equals(tab.Header as string, Sections[i].Title, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Colors tab '" + tab.Header + "' does not match section '" + Sections[i].Title + "'.");
                }

                tab.Content = CreateSection(Sections[i]);
            }

            ColorSectionTabs.SelectedIndex = 0;
        }

        private static Controls.StackPanel CreateSection(ColorSectionData section)
        {
            Controls.StackPanel panel = new();
            panel.SetResourceReference(Controls.StackPanel.SpacingProperty, "DemoColorSectionSpacing");

            if (section.Intro is not null)
            {
                TextBlock intro = new() { Text = section.Intro, TextWrapping = TextWrapping.Wrap };
                intro.SetResourceReference(StyleProperty, "BodyTextBlockStyle");
                intro.SetResourceReference(MarginProperty, "DemoMediumTopGapMargin");
                _ = panel.Children.Add(intro);
            }

            foreach (ColorGroupData group in section.Groups)
            {
                if (group.Title is not null)
                {
                    ColorPageExample example = new()
                    {
                        Title = group.Title,
                        Description = group.Description,
                        ExampleContent = group.Example?.Invoke(),
                    };
                    example.SetResourceReference(BackgroundProperty, group.BackgroundKey);
                    if (group.ForegroundKey is not null)
                    {
                        example.SetResourceReference(ForegroundProperty, group.ForegroundKey);
                    }

                    _ = panel.Children.Add(example);
                }

                foreach (ColorTileRowData row in group.Rows)
                {
                    _ = panel.Children.Add(CreateTileGrid(row));
                }
            }

            return panel;
        }

        // WinUI Gallery GalleryTileGridStyle: tiles sit on the base solid background inside a 1px card stroke at
        // OverlayCornerRadius. WPF's Border does not clip children to that radius, so the corner tiles round
        // their own outer corners instead.
        private static Controls.Border CreateTileGrid(ColorTileRowData row)
        {
            int columns = row.Tiles.Length / row.Rows;
            UniformGrid grid = new() { Rows = row.Rows, Columns = columns };

            for (int i = 0; i < row.Tiles.Length; i++)
            {
                ColorTileData data = row.Tiles[i];
                int rowIndex = i / columns;
                int columnIndex = i % columns;
                ColorTile tile = new()
                {
                    ColorName = data.Name,
                    ColorExplanation = data.Explanation,
                    ColorBrushName = data.BrushKey,
                    ShowSeparator = columnIndex < columns - 1,
                    Tag = data.BrushKey,
                };
                tile.SetResourceReference(BackgroundProperty, data.BrushKey);
                if (data.ForegroundKey is null)
                {
                    // The Gallery paints this tile with literal Black; see ColorTile.AutoContrastForeground.
                    tile.AutoContrastForeground = true;
                }
                else
                {
                    tile.SetResourceReference(ForegroundProperty, data.ForegroundKey);
                }
                tile.SetResourceReference(ColorTile.TileCornerRadiusProperty, GetTileCornerRadiusKey(rowIndex, columnIndex, row.Rows, columns));
                _ = grid.Children.Add(tile);
            }

            Controls.Border surface = new() { Child = grid };
            surface.SetResourceReference(BackgroundProperty, "SolidBackgroundFillColorBaseBrush");
            surface.SetResourceReference(BorderBrushProperty, CardStroke);
            surface.SetResourceReference(BorderThicknessProperty, SingleStroke);
            surface.SetResourceReference(Border.CornerRadiusProperty, OverlayRadius);
            return surface;
        }

        private static string GetTileCornerRadiusKey(int rowIndex, int columnIndex, int rows, int columns)
        {
            bool first = columnIndex is 0;
            bool last = columnIndex == columns - 1;
            bool top = rowIndex is 0;
            bool bottom = rowIndex == rows - 1;
            return (rows is 1, first, last, top, bottom) switch
            {
                (true, true, true, _, _) => "DemoColorTileOnlyCornerRadius",
                (true, true, false, _, _) => "DemoColorTileFirstCornerRadius",
                (true, false, true, _, _) => "DemoColorTileLastCornerRadius",
                (true, _, _, _, _) => "DemoColorTileMiddleCornerRadius",
                (false, true, _, true, _) => "DemoColorTileTopLeftCornerRadius",
                (false, _, true, true, _) => "DemoColorTileTopRightCornerRadius",
                (false, true, _, _, true) => "DemoColorTileBottomLeftCornerRadius",
                (false, _, true, _, true) => "DemoColorTileBottomRightCornerRadius",
                _ => "DemoColorTileMiddleCornerRadius",
            };
        }

        private static TextBlock CreateGlyph(string foregroundKey)
        {
            TextBlock glyph = new() { Text = "Aa", FontWeight = FontWeights.SemiBold };
            glyph.SetResourceReference(TextBlock.FontSizeProperty, "DemoColorExampleGlyphFontSize");
            glyph.SetResourceReference(TextBlock.ForegroundProperty, foregroundKey);
            return glyph;
        }

        private static Controls.ToggleSwitch CreateToggleSwitch(bool narrow)
        {
            Controls.ToggleSwitch toggle = new() { OnContent = string.Empty, OffContent = string.Empty };
            if (narrow)
            {
                toggle.SetResourceReference(MinWidthProperty, "DemoColorExampleToggleSwitchWidth");
                toggle.SetResourceReference(MaxWidthProperty, "DemoColorExampleToggleSwitchWidth");
            }

            return toggle;
        }

        private static Controls.Slider CreateSlider()
        {
            Controls.Slider slider = new() { Maximum = 100, Value = 40 };
            slider.SetResourceReference(MinWidthProperty, "DemoColorExampleSliderMinWidth");
            return slider;
        }

        private static ScrollBar CreateScrollBar()
        {
            ScrollBar scrollBar = new()
            {
                Orientation = Orientation.Horizontal,
                Maximum = 100,
                Value = 40,
                ViewportSize = 40,
            };
            scrollBar.SetResourceReference(StyleProperty, "HorizontalScrollBarStyle");
            scrollBar.SetResourceReference(WidthProperty, "DemoColorExampleScrollBarWidth");
            scrollBar.SetResourceReference(HeightProperty, "DemoColorExampleScrollBarHeight");
            return scrollBar;
        }

        private static StackPanel CreateSubtleFillExample()
        {
            Controls.Border rest = new() { Child = new TextBlock { Text = "Rest" } };
            rest.SetResourceReference(PaddingProperty, "DemoColorExampleSubtleRestPadding");

            Controls.Border hover = new() { Child = new TextBlock { Text = "Hover" } };
            hover.SetResourceReference(PaddingProperty, "DemoColorExampleSubtleHoverPadding");
            hover.SetResourceReference(MinWidthProperty, "DemoColorExampleSubtleHoverMinWidth");
            hover.SetResourceReference(BackgroundProperty, "SubtleFillColorSecondaryBrush");
            hover.SetResourceReference(Border.CornerRadiusProperty, ControlRadius);

            StackPanel panel = new();
            _ = panel.Children.Add(rest);
            _ = panel.Children.Add(hover);
            return panel;
        }

        // The Gallery places a control over a photo; Fluence has no sample image asset, so the accent fill stands in for the imagery.
        private static Controls.Border CreateControlOnImageExample()
        {
            Controls.Border badge = CreateSurface("ControlOnImageFillColorDefaultBrush", "ControlStrongStrokeColorDefaultBrush", ControlRadius, "DemoColorExampleOnImageBadgeSize", "DemoColorExampleOnImageBadgeSize");
            badge.HorizontalAlignment = HorizontalAlignment.Right;
            badge.VerticalAlignment = VerticalAlignment.Top;
            badge.SetResourceReference(MarginProperty, "DemoColorExampleOnImageBadgeMargin");

            Controls.Border image = new() { Child = badge };
            image.SetResourceReference(BackgroundProperty, "AccentFillColorDefaultBrush");
            image.SetResourceReference(Border.CornerRadiusProperty, ControlRadius);
            image.SetResourceReference(WidthProperty, "DemoColorExampleImageWidth");
            image.SetResourceReference(HeightProperty, "DemoColorExampleImageHeight");
            return image;
        }

        private static Controls.Border CreateDividerExample()
        {
            Controls.Border divider = new() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Stretch };
            divider.SetResourceReference(BorderBrushProperty, "DividerStrokeColorDefaultBrush");
            divider.SetResourceReference(BorderThicknessProperty, "DemoColorTileSeparatorThickness");

            Controls.Border surface = CreateSurface("AcrylicBackgroundFillColorBaseBrush", "SurfaceStrokeColorDefaultBrush", OverlayRadius, SurfaceWidth, SurfaceHeight);
            surface.Child = divider;
            return surface;
        }

        private static Controls.Border CreateFocusExample()
        {
            Controls.Border content = CreateSurface(backgroundKey: null, "SurfaceStrokeColorDefaultBrush", OverlayRadius, SurfaceWidth, SurfaceHeight);
            content.Child = new TextBlock { Text = "Text", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

            Controls.Border inner = new() { Child = content };
            inner.SetResourceReference(BorderBrushProperty, "FocusStrokeColorInnerBrush");
            inner.SetResourceReference(BorderThicknessProperty, "DemoColorExampleFocusStrokeThickness");
            inner.SetResourceReference(Border.CornerRadiusProperty, "DemoColorExampleFocusInnerCornerRadius");

            Controls.Border outer = new() { Child = inner };
            outer.SetResourceReference(BorderBrushProperty, "FocusStrokeColorOuterBrush");
            outer.SetResourceReference(BorderThicknessProperty, "DemoColorExampleFocusStrokeThickness");
            outer.SetResourceReference(Border.CornerRadiusProperty, "DemoColorExampleFocusOuterCornerRadius");
            return outer;
        }

        private static Controls.Border CreateLayerExample(string? backgroundKey, string layerKey)
        {
            Controls.Border layer = new() { HorizontalAlignment = HorizontalAlignment.Right };
            layer.SetResourceReference(WidthProperty, "DemoColorExampleLayerInnerWidth");
            layer.SetResourceReference(BackgroundProperty, layerKey);
            layer.SetResourceReference(BorderBrushProperty, CardStroke);
            layer.SetResourceReference(BorderThicknessProperty, "DemoColorExampleLayerInnerBorderThickness");

            Controls.Border surface = CreateSurface(backgroundKey, CardStroke, OverlayRadius, SurfaceWidth, SurfaceHeight);
            surface.Child = layer;
            return surface;
        }

        // The Gallery shows a TabViewItem over live Mica; here a tab-shaped surface is painted with the Mica Base Alt layer fallback.
        private static Controls.Border CreateTabExample()
        {
            Controls.Border tab = CreateSurface("LayerOnMicaBaseAltFillColorDefaultBrush", "ControlStrokeColorSecondaryBrush", ControlRadius, "DemoColorExampleTabItemWidth", "DemoColorExampleTabItemHeight");
            tab.Child = new TextBlock { Text = "Text", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            tab.SetResourceReference(MarginProperty, "DemoColorExampleTabItemMargin");
            return tab;
        }

        private static Controls.InfoBar CreateInfoBar()
        {
            return new Controls.InfoBar
            {
                Title = "Title",
                Message = "This is body text. Windows 11 is faster and more intuitive.",
                Severity = InfoBarSeverity.Error,
                IsOpen = true,
                IsClosable = false,
            };
        }

        private static Controls.Border CreateSurface(string? backgroundKey, string borderKey, string cornerRadiusKey, string widthKey, string heightKey)
        {
            Controls.Border surface = new();
            if (backgroundKey is not null)
            {
                surface.SetResourceReference(BackgroundProperty, backgroundKey);
            }

            surface.SetResourceReference(BorderBrushProperty, borderKey);
            surface.SetResourceReference(BorderThicknessProperty, SingleStroke);
            surface.SetResourceReference(Border.CornerRadiusProperty, cornerRadiusKey);
            surface.SetResourceReference(WidthProperty, widthKey);
            surface.SetResourceReference(HeightProperty, heightKey);
            return surface;
        }

        private sealed class ColorSectionData(string title, string? intro, ColorGroupData[] groups)
        {
            public string Title { get; } = title;

            public string? Intro { get; } = intro;

            public ColorGroupData[] Groups { get; } = groups;
        }

        private sealed class ColorGroupData(string? title, string description, string backgroundKey, string? foregroundKey, Func<UIElement>? example, ColorTileRowData[] rows)
        {
            public string? Title { get; } = title;

            public string Description { get; } = description;

            public string BackgroundKey { get; } = backgroundKey;

            public string? ForegroundKey { get; } = foregroundKey;

            public Func<UIElement>? Example { get; } = example;

            public ColorTileRowData[] Rows { get; } = rows;
        }

        private sealed class ColorTileRowData(int rows, ColorTileData[] tiles)
        {
            public int Rows { get; } = rows;

            public ColorTileData[] Tiles { get; } = tiles;
        }

        private sealed class ColorTileData(string name, string explanation, string brushKey, string? foregroundKey)
        {
            public string Name { get; } = name;

            public string Explanation { get; } = explanation;

            public string BrushKey { get; } = brushKey;

            public string? ForegroundKey { get; } = foregroundKey;
        }
    }
}
