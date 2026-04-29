using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace Fluence.Wpf.Demo.Samples.Accessibility
{
    public partial class HighContrastMapping : UserControl
    {
        private static readonly string[][] HcPairs = new string[][]
        {
            new string[] { "TextFillColorPrimaryBrush", "WindowText" },
            new string[] { "TextFillColorSecondaryBrush", "WindowText" },
            new string[] { "TextFillColorTertiaryBrush", "GrayText" },
            new string[] { "TextFillColorDisabledBrush", "GrayText" },
            new string[] { "AccentFillColorDefaultBrush", "Highlight" },
            new string[] { "AccentTextFillColorPrimaryBrush", "HotTrack" },
            new string[] { "ControlFillColorDefaultBrush", "Control" },
            new string[] { "ControlStrokeColorDefaultBrush", "ControlDark" },
            new string[] { "FocusStrokeColorOuterBrush", "Highlight" },
            new string[] { "FocusStrokeColorInnerBrush", "HighlightText" },
            new string[] { "CardBackgroundFillColorDefaultBrush", "Control" },
            new string[] { "SolidBackgroundFillColorBaseBrush", "Window" },
        };

        public HighContrastMapping()
        {
            InitializeComponent();
            PopulateHcTable();
        }

        private void PopulateHcTable()
        {
            var rows = new List<HcBrushEntry>();
            foreach (var pair in HcPairs)
            {
                var brush = TryFindResource(pair[0]) as Brush ?? Brushes.Transparent;
                rows.Add(new HcBrushEntry
                {
                    Key = pair[0],
                    HcMapping = pair[1],
                    Brush = brush
                });
            }

            HcMappingTable.ItemsSource = rows;
        }
    }

    public sealed class HcBrushEntry
    {
        public string Key { get; set; }

        public string HcMapping { get; set; }

        public Brush Brush { get; set; }
    }
}
