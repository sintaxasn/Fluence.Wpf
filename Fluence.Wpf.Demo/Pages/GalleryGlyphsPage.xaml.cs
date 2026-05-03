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
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryGlyphsPage : UserControl
    {

        private const string IconCatalogXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Glyphs.IconCatalog""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width=""56"" />
            <ColumnDefinition Width=""*"" />
            <ColumnDefinition Width=""96"" />
        </Grid.ColumnDefinitions>

        <ui:FontIcon
            HorizontalAlignment=""Center""
            VerticalAlignment=""Center""
            Glyph=""&#xE713;""
            IconFontSize=""24"" />
        <TextBlock
            Grid.Column=""1""
            VerticalAlignment=""Center""
            Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
            Text=""Settings"" />
        <TextBlock
            Grid.Column=""2""
            VerticalAlignment=""Center""
            FontFamily=""Consolas""
            Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
            Text=""U+E713"" />
    </Grid>
</UserControl>
";

        private const string IconCatalogCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Glyphs
{
    public partial class IconCatalog : UserControl
    {
        public IconCatalog()
        {
            InitializeComponent();
        }
    }
}
";

        private const int IconsPerRow = 4;
        private static readonly object IconRowsLock = new object();
        private static List<IconCatalogRow> cachedIconRows;
        private static int cachedIconCount;

        private static readonly Uri KnownIconNamesResourceUri = new Uri(
            "/Fluence.Wpf.Demo;component/Resources/SegoeFluentIcons.tsv",
            UriKind.Relative);

        public GalleryGlyphsPage()
        {
            InitializeComponent();

            var rows = GetIconRows();
            IconCatalogList.ItemsSource = rows;
            IconCatalogCountText.Text = string.Format(
                CultureInfo.InvariantCulture,
                "{0:N0} Segoe Fluent Icons",
                cachedIconCount);

            var sample = new DemoSampleControl
            {
                Title = "FontIcon",
                Description = "FontIcon renders one Segoe Fluent Icons glyph by private-use code point.",
                XamlSource = IconCatalogXamlSource,
                CSharpSource = IconCatalogCSharpSource,
                SampleContent = FontIconSampleContent
            };
            Grid.SetRow(sample, 2);
            PageRoot.Children.Remove(FontIconSampleContent);
            PageRoot.Children.Add(sample);
        }

        private static List<IconCatalogRow> GetIconRows()
        {
            lock (IconRowsLock)
            {
                if (cachedIconRows == null)
                {
                    var icons = LoadIconCatalog();
                    cachedIconCount = icons.Count;
                    cachedIconRows = CreateIconRows(icons);
                }

                return cachedIconRows;
            }
        }

        private static List<IconCatalogItem> LoadIconCatalog()
        {
            var knownNames = LoadKnownIconNames();
            var typeface = new Typeface(
                new FontFamily("Segoe Fluent Icons"),
                FontStyles.Normal,
                FontWeights.Normal,
                FontStretches.Normal);

            GlyphTypeface glyphTypeface;
            if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
            {
                throw new InvalidOperationException("Segoe Fluent Icons is required to render the iconography catalog.");
            }

            var codes = new List<int>();
            foreach (var character in glyphTypeface.CharacterToGlyphMap.Keys)
            {
                if (character >= 0xE000 && character <= 0xF8FF)
                {
                    codes.Add(character);
                }
            }

            codes.Sort();

            var namedIcons = new List<IconCatalogItem>(knownNames.Count);
            var unnamedIcons = new List<IconCatalogItem>();
            foreach (var code in codes)
            {
                var codeText = code.ToString("X4", CultureInfo.InvariantCulture);
                string name;
                var glyph = char.ConvertFromUtf32(code);
                if (knownNames.TryGetValue(codeText, out name))
                {
                    namedIcons.Add(new IconCatalogItem(name, codeText, glyph));
                }
                else
                {
                    unnamedIcons.Add(new IconCatalogItem("Private-use glyph", codeText, glyph));
                }
            }

            var icons = new List<IconCatalogItem>(namedIcons.Count + unnamedIcons.Count);
            icons.AddRange(namedIcons);
            icons.AddRange(unnamedIcons);
            return icons;
        }

        private static List<IconCatalogRow> CreateIconRows(List<IconCatalogItem> icons)
        {
            var rows = new List<IconCatalogRow>((icons.Count + IconsPerRow - 1) / IconsPerRow);
            for (var index = 0; index < icons.Count; index += IconsPerRow)
            {
                var rowItems = new List<IconCatalogItem>(IconsPerRow);
                for (var offset = 0; offset < IconsPerRow && index + offset < icons.Count; offset++)
                {
                    rowItems.Add(icons[index + offset]);
                }

                rows.Add(new IconCatalogRow(rowItems));
            }

            return rows;
        }

        private static Dictionary<string, string> LoadKnownIconNames()
        {
            var info = Application.GetResourceStream(KnownIconNamesResourceUri);
            if (info == null)
            {
                throw new InvalidOperationException("Segoe Fluent Icons name data was not found.");
            }

            var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var reader = new StreamReader(info.Stream, Encoding.UTF8, true))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length == 0)
                    {
                        continue;
                    }

                    var parts = line.Split('\t');
                    if (parts.Length < 2)
                    {
                        continue;
                    }

                    var name = parts[0].Trim();
                    var code = parts[1].Trim().ToUpperInvariant();
                    if (name.Length == 0 || code.Length == 0 || names.ContainsKey(code))
                    {
                        continue;
                    }

                    names.Add(code, name);
                }
            }

            return names;
        }

        public sealed class IconCatalogRow
        {
            public IconCatalogRow(IList<IconCatalogItem> items)
            {
                Items = items;
            }

            public IList<IconCatalogItem> Items { get; private set; }
        }

        public sealed class IconCatalogItem
        {
            public IconCatalogItem(string name, string code, string glyph)
            {
                Name = name;
                Code = code;
                DisplayCode = "U+" + code;
                Glyph = glyph;
            }

            public string Name { get; private set; }

            public string Code { get; private set; }

            public string DisplayCode { get; private set; }

            public string Glyph { get; private set; }
        }
    }
}