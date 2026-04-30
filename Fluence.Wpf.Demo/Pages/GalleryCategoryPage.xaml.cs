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
using System.Windows.Media.Imaging;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryCategoryPage : UserControl
    {
        private const string ControlImageResourceBase = "pack://application:,,,/Fluence.Wpf.Demo;component/Resources/ControlImages/";

        private static readonly Dictionary<string, string> CardImageFileNames = new Dictionary<string, string>
        {
            { "CaptionButtonChrome", "TitleBar.png" },
            { "Card", "Placeholder.png" },
            { "CheckBox", "Checkbox.png" },
            { "Color", "ColorPaletteResources.png" },
            { "Color contrast", "Accessibility.png" },
            { "ContextMenu", "MenuFlyout.png" },
            { "DockPanel", "Grid.png" },
            { "FluenceWindow", "AppWindow.png" },
            { "Iconography", "IconElement.png" },
            { "Keyboard support", "Accessibility.png" },
            { "Menu", "MenuBar.png" },
            { "Screen reader support", "Accessibility.png" },
            { "Separator", "AppBarSeparator.png" },
            { "Typography", "TextBlock.png" }
        };

        private static readonly Dictionary<string, string> CardSubtitles = new Dictionary<string, string>
        {
            { "Border", "Draw a border, background, or rounded frame around content." },
            { "Button", "A control that responds to user input and raises a Click event." },
            { "CaptionButtonChrome", "Window caption buttons with Fluent hover, press, and glyph states." },
            { "Card", "A container that groups related content and actions." },
            { "CheckBox", "A control that a user can select or clear." },
            { "Color", "Theme colors, semantic brushes, and accent color resources." },
            { "Color contrast", "Color and theme choices that remain readable across contrast modes." },
            { "ComboBox", "A drop-down list of items a user can select from." },
            { "ContextMenu", "A contextual command menu opened from a specific target." },
            { "DockPanel", "A layout panel that docks children to an edge." },
            { "DropDownButton", "A button that displays a flyout of choices when clicked." },
            { "Expander", "A headered container that expands or collapses related content." },
            { "FluenceWindow", "A Fluent application window with title bar, theme, and backdrop support." },
            { "HyperlinkButton", "A button that appears as hyperlink text, and can navigate to a URI or handle a Click event." },
            { "Iconography", "The available Fluent icon glyphs and their usage." },
            { "InfoBadge", "A non-intrusive badge for status, alerts, or new content." },
            { "InfoBar", "An inline message for important status and application changes." },
            { "Keyboard support", "Keyboard focus, tab order, and efficient input patterns." },
            { "ListBox", "A visible list of options that can support single or multiple selection." },
            { "ListView", "A vertical list for displaying a collection of items." },
            { "Menu", "A menu surface for grouped application commands." },
            { "NavigationView", "A navigation pane for top-level app areas and control pages." },
            { "NumberBox", "A text input optimized for numeric values and optional spin buttons." },
            { "PasswordBox", "A masked text input for password entry." },
            { "PersonPicture", "An avatar control for people, groups, and identity status." },
            { "ProgressBar", "A linear progress indicator for determinate or indeterminate work." },
            { "ProgressRing", "A ring progress indicator for ongoing work." },
            { "RadioButton", "A control that allows a user to select a single option from a group of options." },
            { "RatingControl", "Rate something 1 to 5 stars." },
            { "RepeatButton", "A button that raises its Click event repeatedly from the time it's pressed until it's released." },
            { "Screen reader support", "Names, roles, and automation properties for assistive technologies." },
            { "Separator", "A visual divider between related regions or commands." },
            { "Slider", "A control that lets the user select from a range of values by moving a Thumb control along a track." },
            { "SplitButton", "A two-part button that displays a flyout when its secondary part is clicked." },
            { "StackPanel", "A layout panel that stacks children in one direction." },
            { "TabView", "A tabbed surface for switching between related documents or pages." },
            { "TextBlock", "A read-only text display control for labels, prose, and inline text." },
            { "TextBox", "A text input for entering and editing plain text." },
            { "TitleBar", "A Fluent title bar for application chrome and window commands." },
            { "ToggleButton", "A button that can be switched between two states like a CheckBox." },
            { "ToggleSwitch", "A switch that can be toggled between 2 states." },
            { "ToolTip", "A lightweight popup that explains a control or command." },
            { "TreeView", "A hierarchical list with expandable and collapsible nodes." },
            { "Typography", "Type sizes, weights, line heights, and text styles." }
        };

        public GalleryCategoryPage(string title, string description, IEnumerable<DemoNavigationItem> items)
        {
            InitializeComponent();

            CategoryPageTitle.Text = title;
            CategoryPageDescription.Text = description;

            foreach (var item in items)
            {
                AddCard(item);
            }
        }

        private void AddCard(DemoNavigationItem item)
        {
            var card = new Card
            {
                Width = 300,
                Height = 96,
                Margin = new Thickness(0, 0, 12, 12),
                Padding = new Thickness(8),
                IsClickable = true,
                Tag = item.Title,
                Variant = CardVariant.Default
            };
            card.Click += Card_Click;

            card.Content = CreateCardContent(item);

            CategoryCardsGrid.Children.Add(card);
        }

        private static UIElement CreateCardContent(DemoNavigationItem item)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var image = new Image
            {
                Width = 32,
                Height = 32,
                Margin = new Thickness(8, 12, 16, 0),
                Source = CreateImageSource(GetCardImageFileName(item.Title)),
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetRowSpan(image, 2);
            grid.Children.Add(image);

            var title = new System.Windows.Controls.TextBlock
            {
                Margin = new Thickness(0, 12, 0, 0),
                Text = item.Title,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.NoWrap,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            title.SetResourceReference(FrameworkElement.StyleProperty, "BodyStrongTextBlockStyle");
            Grid.SetColumn(title, 1);
            grid.Children.Add(title);

            var subtitle = new System.Windows.Controls.TextBlock
            {
                Text = GetCardSubtitle(item.Title),
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.Wrap
            };
            subtitle.SetResourceReference(FrameworkElement.StyleProperty, "CaptionTextBlockStyle");
            subtitle.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            Grid.SetRow(subtitle, 1);
            Grid.SetColumn(subtitle, 1);
            grid.Children.Add(subtitle);

            return grid;
        }

        private static ImageSource CreateImageSource(string fileName)
        {
            return new BitmapImage(new System.Uri(ControlImageResourceBase + fileName, System.UriKind.Absolute));
        }

        private static string GetCardImageFileName(string title)
        {
            string fileName;
            if (CardImageFileNames.TryGetValue(title, out fileName))
            {
                return fileName;
            }

            return title + ".png";
        }

        private static string GetCardSubtitle(string title)
        {
            string subtitle;
            if (CardSubtitles.TryGetValue(title, out subtitle))
            {
                return subtitle;
            }

            return "Open the " + title + " examples.";
        }

        private void Card_Click(object sender, RoutedEventArgs e)
        {
            var card = sender as Card;
            if (card == null)
            {
                return;
            }

            var tag = card.Tag as string;
            if (string.IsNullOrEmpty(tag))
            {
                return;
            }

            var host = Window.GetWindow(this) as MainWindow;
            if (host != null)
            {
                host.NavigateTo(tag);
            }
        }
    }
}
