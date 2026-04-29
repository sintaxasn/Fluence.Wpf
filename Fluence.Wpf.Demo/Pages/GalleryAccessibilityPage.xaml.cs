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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>Gallery page demonstrating accessibility features: focus rings, tab order, HC brush mapping, and RTL layout.</summary>
    public partial class GalleryAccessibilityPage : UserControl
    {
        // Representative Fluence key → Windows HC system colour pairs shown in the mapping table.
        private static readonly string[][] HcPairs = new string[][]
        {
            new string[] { "TextFillColorPrimaryBrush",       "WindowText"        },
            new string[] { "TextFillColorSecondaryBrush",     "WindowText"        },
            new string[] { "TextFillColorTertiaryBrush",      "GrayText"          },
            new string[] { "TextFillColorDisabledBrush",      "GrayText"          },
            new string[] { "AccentFillColorDefaultBrush",     "Highlight"         },
            new string[] { "AccentTextFillColorPrimaryBrush", "HotTrack"          },
            new string[] { "ControlFillColorDefaultBrush",    "Control"           },
            new string[] { "ControlStrokeColorDefaultBrush",  "ControlDark"       },
            new string[] { "FocusStrokeColorOuterBrush",      "Highlight"         },
            new string[] { "FocusStrokeColorInnerBrush",      "HighlightText"     },
            new string[] { "CardBackgroundFillColorDefaultBrush", "Control"       },
            new string[] { "SolidBackgroundFillColorBaseBrush",   "Window"        },
        };

        /// <summary>Initializes a new instance of <see cref="GalleryAccessibilityPage"/>.</summary>
        public GalleryAccessibilityPage()
        {
            InitializeComponent();

            DemoSourceAction.Replace(FocusAndTabOrderSourceLink, "Accessibility/FocusAndTabOrder.xaml");
            DemoSourceAction.Replace(HighContrastMappingSourceLink, "Accessibility/HighContrastMapping.xaml");
            DemoSourceAction.Replace(AutomationPropertiesSourceLink, "Accessibility/AutomationProperties.xaml");
            DemoSourceAction.Replace(RtlLayoutSourceLink, "Accessibility/RtlLayout.xaml");

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            PopulateHcTable();
        }

        private void PopulateHcTable()
        {
            if (HcMappingTable == null) return;

            var rows = new List<HcBrushEntry>();
            foreach (var pair in HcPairs)
            {
                var brush = TryFindResource(pair[0]) as Brush ?? Brushes.Transparent;
                rows.Add(new HcBrushEntry
                {
                    Key = pair[0],
                    HcMapping = pair[1],
                    Brush = brush,
                });
            }

            HcMappingTable.ItemsSource = rows;
        }

        private void RtlToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (RtlDemoCard == null || RtlToggle == null) return;
            RtlDemoCard.FlowDirection = RtlToggle.IsChecked == true
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;
        }
    }

    /// <summary>Row model for the High Contrast brush mapping table.</summary>
    public sealed class HcBrushEntry
    {
        /// <summary>Gets or sets the Fluence resource key (e.g. <c>TextFillColorPrimaryBrush</c>).</summary>
        public string Key { get; set; }

        /// <summary>Gets or sets the Windows HC system colour name (e.g. <c>WindowText</c>).</summary>
        public string HcMapping { get; set; }

        /// <summary>Gets or sets the live brush resolved from the current theme dictionary.</summary>
        public Brush Brush { get; set; }
    }
}
