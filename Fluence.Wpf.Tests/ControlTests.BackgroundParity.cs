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

using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Demo.Pages.ColorGuidance;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Xml.Linq;
using FluenceCheckBox = Fluence.Wpf.Controls.CheckBox;
using FluenceExpander = Fluence.Wpf.Controls.Expander;
using FluenceProgressBar = Fluence.Wpf.Controls.ProgressBar;
using FluenceRadioButton = Fluence.Wpf.Controls.RadioButton;
using FluenceToggleSwitch = Fluence.Wpf.Controls.ToggleSwitch;
using WpfBorder = System.Windows.Controls.Border;
using WpfEllipse = System.Windows.Shapes.Ellipse;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [TestMethod]
        public void SelectionControls_OffStateBackgrounds_UseWinUiAltFillRoles()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);

                FluenceCheckBox checkBox = new() { Content = "Check" };
                FluenceRadioButton radioButton = new() { Content = "Radio" };
                FluenceToggleSwitch toggleSwitch = new() { OffContent = "Off", OnContent = "On" };

                StackPanel panel = new();
                _ = panel.Children.Add(checkBox);
                _ = panel.Children.Add(radioButton);
                _ = panel.Children.Add(toggleSwitch);

                Window window = new()
                {
                    Content = panel,
                    Width = 320,
                    Height = 180
                };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = checkBox.ApplyTemplate();
                    _ = radioButton.ApplyTemplate();
                    _ = toggleSwitch.ApplyTemplate();

                    WpfBorder? indicatorFill = FindVisualChildByName<WpfBorder>(checkBox, "IndicatorFill");
                    WpfBorder? indicatorHover = FindVisualChildByName<WpfBorder>(checkBox, "IndicatorHover");
                    WpfBorder? indicatorPressed = FindVisualChildByName<WpfBorder>(checkBox, "IndicatorPressed");
                    Assert.IsNotNull(indicatorFill, "CheckBox template must expose IndicatorFill.");
                    Assert.IsNotNull(indicatorHover, "CheckBox template must expose IndicatorHover.");
                    Assert.IsNotNull(indicatorPressed, "CheckBox template must expose IndicatorPressed.");

                    AssertBrushColor(indicatorFill.Background, "ControlAltFillColorSecondaryBrush",
                        "Unchecked CheckBox fill should use the WinUI off-state rest token.");
                    AssertBrushColor(indicatorHover.Background, "ControlAltFillColorTertiaryBrush",
                        "Unchecked CheckBox hover fill should use the WinUI off-state hover token.");
                    AssertBrushColor(indicatorPressed.Background, "ControlAltFillColorQuarternaryBrush",
                        "Unchecked CheckBox pressed fill should use the WinUI off-state pressed token.");
                    AssertBrushColor(indicatorPressed.BorderBrush, "ControlStrongStrokeColorDisabledBrush",
                        "Unchecked CheckBox pressed stroke should use the WinUI pressed stroke token.");

                    WpfEllipse? outerEllipse = FindVisualChildByName<WpfEllipse>(radioButton, "OuterEllipse");
                    WpfEllipse? outerEllipseHover = FindVisualChildByName<WpfEllipse>(radioButton, "OuterEllipseHover");
                    WpfEllipse? outerEllipsePressed = FindVisualChildByName<WpfEllipse>(radioButton, "OuterEllipsePressed");
                    Assert.IsNotNull(outerEllipse, "RadioButton template must expose OuterEllipse.");
                    Assert.IsNotNull(outerEllipseHover, "RadioButton template must expose OuterEllipseHover.");
                    Assert.IsNotNull(outerEllipsePressed, "RadioButton template must expose OuterEllipsePressed.");

                    AssertBrushColor(outerEllipse.Fill, "ControlAltFillColorSecondaryBrush",
                        "Unchecked RadioButton fill should use the WinUI off-state rest token.");
                    AssertBrushColor(outerEllipseHover.Fill, "ControlAltFillColorTertiaryBrush",
                        "Unchecked RadioButton hover fill should use the WinUI off-state hover token.");
                    AssertBrushColor(outerEllipsePressed.Fill, "ControlAltFillColorQuarternaryBrush",
                        "Unchecked RadioButton pressed fill should use the WinUI off-state pressed token.");
                    AssertBrushColor(outerEllipsePressed.Stroke, "ControlStrongStrokeColorDisabledBrush",
                        "Unchecked RadioButton pressed stroke should use the WinUI pressed stroke token.");

                    WpfBorder? trackOff = FindVisualChildByName<WpfBorder>(toggleSwitch, "TrackOff");
                    WpfBorder? trackOffHover = FindVisualChildByName<WpfBorder>(toggleSwitch, "TrackOffHover");
                    WpfBorder? trackOffPressed = FindVisualChildByName<WpfBorder>(toggleSwitch, "TrackOffPressed");
                    Assert.IsNotNull(trackOff, "ToggleSwitch template must expose TrackOff.");
                    Assert.IsNotNull(trackOffHover, "ToggleSwitch template must expose TrackOffHover.");
                    Assert.IsNotNull(trackOffPressed, "ToggleSwitch template must expose TrackOffPressed.");

                    AssertBrushColor(trackOff.Background, "ControlAltFillColorSecondaryBrush",
                        "Unchecked ToggleSwitch track should use the WinUI off-state rest token.");
                    AssertBrushColor(trackOff.BorderBrush, "ControlStrongStrokeColorDefaultBrush",
                        "Unchecked ToggleSwitch track stroke should use the WinUI off-state stroke token.");
                    AssertBrushColor(trackOffHover.Background, "ControlAltFillColorTertiaryBrush",
                        "Unchecked ToggleSwitch hover track should use the WinUI off-state hover token.");
                    AssertBrushColor(trackOffPressed.Background, "ControlAltFillColorQuarternaryBrush",
                        "Unchecked ToggleSwitch pressed track should use the WinUI off-state pressed token.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [TestMethod]
        public void ProgressBar_TrackBackground_UsesWinUiStrongStrokeRole()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);

                FluenceProgressBar progressBar = new()
                {
                    Width = 240,
                    Height = 24,
                    Value = 40
                };
                Window window = new()
                {
                    Content = progressBar,
                    Width = 300,
                    Height = 120
                };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    _ = progressBar.ApplyTemplate();

                    WpfBorder? track = FindVisualChildByName<WpfBorder>(progressBar, "PART_Track");
                    Assert.IsNotNull(track, "ProgressBar template must expose PART_Track.");
                    AssertBrushColor(track.Background, "ControlStrongStrokeColorDefaultBrush",
                        "ProgressBar track should use the WinUI ProgressBarBackground role.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [TestMethod]
        public void ScrollBar_RailBackground_UsesWinUiTrackFillRole()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);

                ScrollBar scrollBar = new()
                {
                    Orientation = Orientation.Vertical,
                    Style = application?.TryFindResource("VerticalScrollBarStyle") as Style,
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0,
                    ViewportSize = 10,
                    Width = 12,
                    Height = 200
                };
                Window window = new()
                {
                    Content = scrollBar,
                    Width = 60,
                    Height = 300
                };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    _ = scrollBar.ApplyTemplate();

                    WpfBorder? trackBackground = FindVisualChildByName<WpfBorder>(scrollBar, "TrackBackground");
                    Assert.IsNotNull(trackBackground, "ScrollBar template must expose TrackBackground.");
                    AssertBrushColor(trackBackground.Background, "ScrollBarTrackFillBrush",
                        "ScrollBar rail should use the WinUI ScrollBarTrackFill role.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [TestMethod]
        public void DemoSampleControl_Chrome_UsesWinUiGalleryBackgroundRoles()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);
                MergeDemoSharedStyles(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, true);

                DemoSampleControl sample = new()
                {
                    SampleDescription = "Sample",
                    DemoContent = new TextBlock { Text = "Body" },
                    OutputContent = new TextBlock { Text = "Output" },
                    RightRailContent = new CheckBox { Content = "Option" },
                    XamlSource = "<Grid />"
                };
                Window window = new()
                {
                    Content = sample,
                    Width = 420,
                    Height = 300
                };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    WpfBorder? sampleCard = sample.FindName("SampleCard") as WpfBorder;
                    FluenceExpander? sourceExpander = sample.FindName("SourceExpander") as FluenceExpander;
                    Assert.IsNotNull(sampleCard, "DemoSampleControl must expose SampleCard.");
                    Assert.IsNotNull(sourceExpander, "DemoSampleControl must expose SourceExpander.");

                    Assert.AreEqual(new CornerRadius(8, 8, 0, 0), sampleCard.CornerRadius,
                        "Demo sample display should attach to the source section with WinUI Gallery corners.");
                    WpfBorder? rightRail = sample.FindName("RightRailBorder") as WpfBorder;
                    WpfBorder? outputRegion = sample.FindName("OutputRegion") as WpfBorder;
                    Assert.IsNotNull(rightRail, "DemoSampleControl must expose RightRailBorder.");
                    Assert.IsNotNull(outputRegion, "DemoSampleControl must expose OutputRegion.");

                    AssertBrushColor(sampleCard.Background, "CardBackgroundFillColorDefaultBrush",
                        "Demo sample display should use the WinUI Gallery control-example surface.");
                    AssertBrushColor(rightRail.Background, "CardBackgroundFillColorSecondaryBrush",
                        "Demo right rail should use the WinUI Gallery options-pane surface.");
                    AssertBrushColor(sourceExpander.Background, "ControlFillColorDefaultBrush",
                        "Demo source header should use the WinUI Gallery source-code header background.");
                    Assert.AreEqual("Source code", sourceExpander.Header,
                        "Demo source expander header should match the WinUI Gallery source label.");

                    sourceExpander.IsExpanded = true;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    RichTextBox? sourceViewer = FindVisualChildByName<RichTextBox>(sourceExpander, "SourceTextViewer");
                    WpfBorder? copyButtonHost = FindVisualChildByName<WpfBorder>(sourceExpander, "CopySourceButtonHost");
                    Assert.IsNotNull(sourceViewer, "Expanded source should expose the code viewer.");
                    Assert.IsNotNull(copyButtonHost, "Expanded source should expose the overlaid copy-button host.");
                    AssertBrushColor(sourceViewer.Background, "SolidBackgroundFillColorBaseBrush",
                        "Source code should use the darker WinUI Gallery on-image fill role.");
                    AssertBrushColor(copyButtonHost.Background, "CardBackgroundFillColorDefaultBrush",
                        "Copy action should sit on the WinUI Gallery on-image fill bubble.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [TestMethod]
        public void DemoSharedResources_DoNotShadowNativeFluenceSurfaceRoles()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);
                MergeDemoSharedStyles(application);

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark, ApplicationTheme.HighContrast })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, true);

                    Assert.IsInstanceOfType(application?.TryFindResource("CardBackgroundFillColorDefault"), typeof(Color),
                        "Demo shared styles must not shadow the native CardBackgroundFillColorDefault color key under " + theme + ".");
                    AssertBrushResolves("CardBackgroundFillColorDefaultBrush", theme);
                }
            });
        }

        [TestMethod]
        public void DemoSharedResources_NativeBrushesResolveInLightAndHighContrast()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);
                MergeDemoSharedStyles(application);

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Light, ApplicationTheme.HighContrast })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, true);

                    foreach (string key in GetNativeDemoSurfaceBrushKeys())
                    {
                        SolidColorBrush? brush = application?.TryFindResource(key) as SolidColorBrush;
                        Assert.IsNotNull(brush, key + " must resolve under " + theme + ".");
                        Assert.AreNotEqual(Color.FromRgb(0x27, 0x27, 0x27), brush.Color,
                            key + " should not keep the dark page token under " + theme + ".");
                    }
                }
            });
        }

        [TestMethod]
        public void BackgroundParityBrushes_ResolveAcrossThemesAndDeterministicAccent()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);
                MergeDemoSharedStyles(application);

                string[] keys =
                [
                    "ControlAltFillColorSecondaryBrush",
                    "ControlAltFillColorTertiaryBrush",
                    "ControlAltFillColorQuarternaryBrush",
                    "ControlAltFillColorDisabledBrush",
                    "ControlFillColorQuarternaryBrush",
                    "CardBackgroundFillColorTertiaryBrush",
                    "SolidBackgroundFillColorQuinaryBrush",
                    "SolidBackgroundFillColorSenaryBrush",
                    "ScrollBarTrackFillBrush",
                    "SolidBackgroundFillColorBaseBrush",
                    "CardBackgroundFillColorDefaultBrush",
                    "CardBackgroundFillColorSecondaryBrush",
                    "ControlFillColorDefaultBrush",
                    "TextFillColorSecondaryBrush",
                    "AccentFillColorDefaultBrush",
                    "SubtleFillColorTransparentBrush",
                    "SubtleFillColorSecondaryBrush",
                    "SubtleFillColorTertiaryBrush",
                    "ControlOnImageFillColorDefaultBrush",
                    "ControlOnImageFillColorSecondaryBrush",
                    "ControlOnImageFillColorTertiaryBrush",
                    "ControlOnImageFillColorDisabledBrush",
                    "SurfaceStrokeColorDefaultBrush",
                    "SurfaceStrokeColorFlyoutBrush",
                    "SurfaceStrokeColorInverseBrush",
                    "DividerStrokeColorDefaultBrush",
                    "LayerOnAcrylicFillColorDefaultBrush",
                    "LayerOnAccentAcrylicFillColorDefaultBrush",
                    "LayerOnMicaBaseAltFillColorDefaultBrush",
                    "LayerOnMicaBaseAltFillColorSecondaryBrush",
                    "LayerOnMicaBaseAltFillColorTertiaryBrush",
                    "LayerOnMicaBaseAltFillColorTransparentBrush",
                    "AcrylicBackgroundFillColorDefaultBrush",
                    "AcrylicBackgroundFillColorBaseBrush",
                    "SystemFillColorInformationalBrush",
                    "SystemColorWindowTextColorBrush",
                    "SystemColorWindowColorBrush",
                    "SystemColorButtonFaceColorBrush",
                    "SystemColorButtonTextColorBrush",
                    "SystemColorHighlightColorBrush",
                    "SystemColorHighlightTextColorBrush",
                    "SystemColorHotlightColorBrush",
                    "SystemColorGrayTextColorBrush",
                ];

                ApplicationTheme[] themes =
                [
                    ApplicationTheme.Light,
                    ApplicationTheme.Dark,
                    ApplicationTheme.HighContrast
                ];

                foreach (ApplicationTheme theme in themes)
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, true);
                    ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                    foreach (string key in keys)
                    {
                        Assert.IsNotNull(application?.TryFindResource(key),
                            "Resource '" + key + "' must resolve under " + theme + " with deterministic accent #0078D4.");
                    }
                }
            });
        }

        [TestMethod]
        public void GalleryColorsPage_UsesWinUiGalleryColorStructure()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);
                MergeDemoSharedStyles(application);

                GalleryColorsPage page = new();
                Window window = new()
                {
                    Content = page,
                    Width = 980,
                    Height = 720
                };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    DemoSampleControl? sample = FindVisualChild<DemoSampleControl>(window);
                    Assert.IsNotNull(sample, "Colors page must keep DemoSampleControl as the reusable sample surface.");
                    Assert.AreEqual(
                        "Theme brushes and accent resources available to Fluence controls.",
                        sample.SampleDescription,
                        "Colors page sample description should describe the Fluence color system.");

                    FluenceExpander? sourceExpander = sample.FindName("SourceExpander") as FluenceExpander;
                    Assert.IsNotNull(sourceExpander, "Colors page sample must keep the bottom source expander.");

                    TabControl? tabs = FindVisualChild<TabControl>(sample);
                    Assert.IsNotNull(tabs, "Colors page must expose a selector-style TabControl.");
                    Assert.AreEqual(6, tabs.Items.Count, "Colors page must keep the six WinUI Gallery color sections.");

                    string[] expectedHeaders =
                    [
                        "Text",
                        "Fill",
                        "Stroke",
                        "Background",
                        "Signal",
                        "High Contrast"
                    ];
                    for (int i = 0; i < expectedHeaders.Length; i++)
                    {
                        TabItem item = (TabItem)tabs.Items[i];
                        Assert.AreEqual(expectedHeaders[i], item.Header, "Unexpected Colors page section at index " + i + ".");
                    }

                    int totalTiles = 0;
                    bool sawSystemColorAlias = false;
                    bool sawColorPageExample = false;
                    for (int i = 0; i < tabs.Items.Count; i++)
                    {
                        tabs.SelectedIndex = i;
                        DrainDispatcher(window.Dispatcher);
                        window.UpdateLayout();

                        List<ColorTile> tiles = [.. FindVisualChildren<ColorTile>(tabs)];
                        totalTiles += tiles.Count;
                        foreach (ColorTile tile in tiles)
                        {
                            if (tile.BrushResourceKey.Equals("SystemColorWindowTextColorBrush", StringComparison.Ordinal))
                            {
                                sawSystemColorAlias = true;
                            }
                        }

                        if (FindVisualChild<ColorPageExample>(tabs) is not null)
                        {
                            sawColorPageExample = true;
                        }
                    }

                    Assert.IsTrue(totalTiles >= 70, "Colors page should expose the WinUI-style brush catalogue through ColorTile controls.");
                    Assert.IsTrue(sawSystemColorAlias, "High Contrast section must use the new SystemColor alias resources.");
                    Assert.IsTrue(sawColorPageExample, "Colors page sections must include WinUI Gallery-style example surfaces.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [TestMethod]
        public void GalleryColorsPage_DynamicResourceKeys_ResolveAcrossThemes()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? application = EnsureApplication();
                _ = MergeGenericDictionary(application);
                MergeDemoSharedStyles(application);

                SortedSet<string> resourceKeys = GetColorGuidanceResourceKeys();
                List<string> unresolved = [];

                ApplicationTheme[] themes =
                [
                    ApplicationTheme.Light,
                    ApplicationTheme.Dark,
                    ApplicationTheme.HighContrast
                ];

                foreach (ApplicationTheme theme in themes)
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, true);
                    ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                    foreach (string resourceKey in resourceKeys)
                    {
                        if (application?.TryFindResource(resourceKey) is null)
                        {
                            unresolved.Add(theme + ": " + resourceKey);
                        }
                    }
                }

                Assert.AreEqual(0, unresolved.Count,
                    "Colors page guidance must only reference resource keys that resolve: " +
                    string.Join("; ", unresolved));
            });
        }

        [TestMethod]
        public void GalleryColorsPage_Foregrounds_AvoidLiteralBlackAndWhiteInXamlAndSourceSnippets()
        {
            string colorsPagePath = Path.Combine(FindRepoRoot(), "Fluence.Wpf.Demo", "Pages", "GalleryColorsPage.xaml");
            string colorsPageCodePath = Path.Combine(FindRepoRoot(), "Fluence.Wpf.Demo", "Pages", "GalleryColorsPage.xaml.cs");
            string source = File.ReadAllText(colorsPagePath) + Environment.NewLine + File.ReadAllText(colorsPageCodePath);
            string[] forbiddenForegrounds =
            [
                "Foreground=\"Black\"",
                "Foreground=\"White\"",
                "Foreground=\"#"
            ];
            List<string> violations = [];

            foreach (string forbiddenForeground in forbiddenForegrounds)
            {
                if (source.Contains(forbiddenForeground))
                {
                    violations.Add(forbiddenForeground);
                }
            }

            Assert.AreEqual(0, violations.Count,
                "GalleryColorsPage foregrounds must use theme-aware resources: " +
                string.Join("; ", violations));
        }

        [TestMethod]
        public void PowerShellDemoScripts_DeclareStrictModeParameters()
        {
            string scriptsRoot = Path.Combine(FindRepoRoot(), "Fluence.Wpf.Demo.PowerShell");
            string[] scriptNames =
            [
                "Show-ControlsDemo.ps1",
                "Show-ThemeDemo.ps1",
                "Show-ProgressDemo.ps1"
            ];
            List<string> violations = [];

            foreach (string scriptName in scriptNames)
            {
                string path = Path.Combine(scriptsRoot, scriptName);
                string source = File.ReadAllText(path);

                if (ContainsOrdinal(source, "$SmokeTest") &&
                    !ContainsOrdinal(source, "[switch]$SmokeTest"))
                {
                    violations.Add(scriptName + " references $SmokeTest without declaring a switch parameter.");
                }

                if (ContainsOrdinal(source, "$RunInProcess") &&
                    !ContainsOrdinal(source, "[switch]$RunInProcess"))
                {
                    violations.Add(scriptName + " references $RunInProcess without declaring a switch parameter.");
                }

                if (ContainsOrdinal(source, "Show-FluenceDemo.ps1"))
                {
                    violations.Add(scriptName + " still reports the stale Show-FluenceDemo.ps1 script name.");
                }
            }

            Assert.AreEqual(0, violations.Count, string.Join("; ", violations));
        }

        [TestMethod]
        public void PowerShellDemoXaml_UsesCurrentFluenceWindowProperties()
        {
            string path = Path.Combine(FindRepoRoot(), "Fluence.Wpf.Demo.PowerShell", "MainWindow.xaml");
            string source = File.ReadAllText(path);

            Assert.IsFalse(ContainsOrdinal(source, "WindowCorners"),
                "PowerShell demo XAML must not use the old WindowCorners property.");
            Assert.IsFalse(ContainsOrdinal(source, "WindowBackdrop"),
                "PowerShell demo XAML must not use the old WindowBackdrop property.");
            Assert.IsTrue(ContainsOrdinal(source, "CornerStyle=\"Round\""),
                "PowerShell demo XAML should use CornerStyle.");
            Assert.IsTrue(ContainsOrdinal(source, "SystemBackdropType=\"Mica\""),
                "PowerShell demo XAML should use SystemBackdropType.");
        }

        [TestMethod]
        public void XamlBackgroundAndFillLiterals_AreAllowListed()
        {
            string repoRoot = FindRepoRoot();
            string[] roots =
            [
                Path.Combine(repoRoot, "Fluence.Wpf", "Themes", "Controls"),
                Path.Combine(repoRoot, "Fluence.Wpf.Demo")
            ];
            List<string> violations = [];

            foreach (string root in roots)
            {
                foreach (string path in Directory.EnumerateFiles(root, "*.xaml", SearchOption.AllDirectories))
                {
                    if (IsBackgroundLiteralAllowedPath(path))
                    {
                        continue;
                    }

                    string source = File.ReadAllText(path);
                    CollectBackgroundLiteralViolations(source, path, "Background", violations);
                    CollectBackgroundLiteralViolations(source, path, "Fill", violations);
                }
            }

            Assert.AreEqual(0, violations.Count,
                "Background/Fill literals must use theme resources unless intentionally allow-listed: " +
                string.Join("; ", violations));
        }

        private static void CollectBackgroundLiteralViolations(
            string source,
            string path,
            string attributeName,
            List<string> violations)
        {
            string attributePrefix = attributeName + "=\"";
            int searchIndex = 0;
            while (searchIndex < source.Length)
            {
                int matchIndex = source.IndexOf(attributePrefix, searchIndex, StringComparison.Ordinal);
                if (matchIndex < 0)
                {
                    break;
                }

                int valueStart = matchIndex + attributePrefix.Length;
                int valueEnd = source.IndexOf('"', valueStart);
                if (valueEnd < 0)
                {
                    break;
                }

                searchIndex = valueEnd + 1;
                if (!IsWholeXamlAttribute(source, matchIndex))
                {
                    continue;
                }

                string value = source.Substring(valueStart, valueEnd - valueStart);
                if (!IsLiteralBackgroundValue(value) || value.Equals("Transparent", StringComparison.Ordinal))
                {
                    continue;
                }

                violations.Add(GetRepoRelativePath(path) + ": " + attributeName + "=\"" + value + "\"");
            }
        }

        private static SortedSet<string> GetColorGuidanceResourceKeys()
        {
            string repoRoot = FindRepoRoot();
            SortedSet<string> keys = [];
            AddColorGuidanceResourceKeysFromXaml(
                Path.Combine(repoRoot, "Fluence.Wpf.Demo", "Pages", "GalleryColorsPage.xaml"),
                keys);

            string guidanceRoot = Path.Combine(repoRoot, "Fluence.Wpf.Demo", "Pages", "ColorGuidance");
            foreach (string path in Directory.EnumerateFiles(guidanceRoot, "*.xaml", SearchOption.TopDirectoryOnly))
            {
                AddColorGuidanceResourceKeysFromXaml(path, keys);
            }

            return keys;
        }

        private static bool ContainsOrdinal(string source, string value)
        {
            return source.IndexOf(value, StringComparison.Ordinal) >= 0;
        }

        private static void AddColorGuidanceResourceKeysFromXaml(string path, SortedSet<string> keys)
        {
            XDocument document = XDocument.Load(path);

            foreach (XElement element in document.Descendants())
            {
                foreach (XAttribute attribute in element.Attributes())
                {
                    CollectDynamicResourceKeys(attribute.Value, keys);
                    CollectColorTileResourceKey(attribute, keys);
                }
            }
        }

        private static void CollectColorTileResourceKey(XAttribute attribute, SortedSet<string> keys)
        {
            string localName = attribute.Name.LocalName;
            if (!localName.Equals("BrushResourceKey", StringComparison.Ordinal) &&
                !localName.Equals("ForegroundResourceKey", StringComparison.Ordinal) &&
                !localName.Equals("ResourceKey", StringComparison.Ordinal))
            {
                return;
            }

            string value = attribute.Value.Trim();
            if (value.Length > 0)
            {
                _ = keys.Add(value);
            }
        }

        private static void CollectDynamicResourceKeys(string value, SortedSet<string> keys)
        {
            const string prefix = "{DynamicResource ";
            int searchIndex = 0;

            while (searchIndex < value.Length)
            {
                int matchIndex = value.IndexOf(prefix, searchIndex, StringComparison.Ordinal);
                if (matchIndex < 0)
                {
                    break;
                }

                int keyStart = matchIndex + prefix.Length;
                int keyEnd = value.IndexOf('}', keyStart);
                if (keyEnd < 0)
                {
                    break;
                }

                string key = value.Substring(keyStart, keyEnd - keyStart).Trim();
                if (key.Length > 0 && key[0] != '{')
                {
                    _ = keys.Add(key);
                }

                searchIndex = keyEnd + 1;
            }
        }

        private static bool IsWholeXamlAttribute(string source, int attributeIndex)
        {
            if (attributeIndex == 0)
            {
                return true;
            }

            char previous = source[attributeIndex - 1];
            return !char.IsLetterOrDigit(previous) && previous != '_' && previous != ':';
        }

        private static bool IsLiteralBackgroundValue(string value)
        {
            if (value.Length == 0)
            {
                return false;
            }

            if (value[0] == '#')
            {
                return true;
            }

            if (value[0] == '{')
            {
                return false;
            }

            foreach (char character in value)
            {
                if (!char.IsLetter(character))
                {
                    return false;
                }
            }

            return true;
        }

        private static void AssertBrushColor(Brush? actualBrush, string resourceKey, string message)
        {
            SolidColorBrush? actual = actualBrush as SolidColorBrush;
            Assert.IsNotNull(actual, message + " Actual brush must be a SolidColorBrush.");

            SolidColorBrush? expected = Application.Current?.TryFindResource(resourceKey) as SolidColorBrush;
            Assert.IsNotNull(expected, resourceKey + " must resolve.");

            Assert.AreEqual(expected.Color, actual.Color, message);
        }

        private static void AssertBrushResolves(string resourceKey, ApplicationTheme theme)
        {
            SolidColorBrush? brush = Application.Current?.TryFindResource(resourceKey) as SolidColorBrush;
            Assert.IsNotNull(brush, resourceKey + " must resolve under " + theme + ".");
        }

        private static string[] GetNativeDemoSurfaceBrushKeys()
        {
            return
            [
                "SolidBackgroundFillColorBaseBrush",
                "CardBackgroundFillColorDefaultBrush",
                "CardBackgroundFillColorSecondaryBrush",
                "ControlFillColorDefaultBrush",
                "TextFillColorSecondaryBrush"
            ];
        }

        private static void MergeDemoSharedStyles(Application? application)
        {
            ResourceDictionary demoShared = new()
            {
                Source = new Uri("/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml", UriKind.Relative)
            };
            application?.Resources.MergedDictionaries.Add(demoShared);
        }

        private static bool IsBackgroundLiteralAllowedPath(string path)
        {
            string fileName = Path.GetFileName(path);
            return fileName.Equals("fluence-wpf-banner-light.xaml", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("fluence-wpf-banner-dark.xaml", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("GallerySettingsPage.xaml", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("GalleryAccessibilityPage.xaml", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetRepoRelativePath(string path)
        {
            string root = FindRepoRoot();
            if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                int separatorLength = path.Length > root.Length &&
                    (path[root.Length] == Path.DirectorySeparatorChar || path[root.Length] == Path.AltDirectorySeparatorChar)
                    ? 1
                    : 0;
                return path.Substring(root.Length + separatorLength);
            }

            return path;
        }

        private static string FindRepoRoot()
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Fluence.Wpf.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException(
                "Could not locate Fluence.Wpf.sln ancestor directory from " + AppContext.BaseDirectory);
        }
    }
}
