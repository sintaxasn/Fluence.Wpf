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
using Fluence.Wpf.Demo.Pages;

namespace Fluence.Wpf.Demo
{
    public sealed class DemoNavigationItem
    {
        public DemoNavigationItem(
            string category,
            string title,
            string tag,
            string glyph,
            bool isDefault,
            Func<object> createPage)
        {
            Category = category;
            Title = title;
            Tag = tag;
            Glyph = glyph;
            IsDefault = isDefault;
            CreatePage = createPage;
        }

        public string Category { get; private set; }

        public string Title { get; private set; }

        public string Tag { get; private set; }

        public string Glyph { get; private set; }

        public bool IsDefault { get; private set; }

        public Func<object> CreatePage { get; private set; }
    }

    public sealed class DemoNavigationCategory
    {
        public DemoNavigationCategory(
            string title,
            string tag,
            string glyph,
            bool isControlCategory,
            bool isExpandedByDefault)
        {
            Title = title;
            Tag = tag;
            Glyph = glyph;
            IsControlCategory = isControlCategory;
            IsExpandedByDefault = isExpandedByDefault;
        }

        public string Title { get; private set; }

        public string Tag { get; private set; }

        public string Glyph { get; private set; }

        public bool IsControlCategory { get; private set; }

        public bool IsExpandedByDefault { get; private set; }
    }

    public static class DemoNavigationCatalog
    {
        private static readonly DemoNavigationCategory[] CatalogCategories =
        {
            new DemoNavigationCategory("Design", "design color iconography typography", "\uEB3C", false, true),
            new DemoNavigationCategory("Accessibility", "accessibility screen reader keyboard contrast", "\uE776", false, true),
            new DemoNavigationCategory("Basic input", "controls basic input button checkbox combobox slider toggle", "\uE73E", true, false),
            new DemoNavigationCategory("Collections", "controls collections list card tree", "\uE80A", true, false),
            new DemoNavigationCategory("Menus and toolbars", "controls menu contextmenu tooltip toolbar command", "\uE115", true, false),
            new DemoNavigationCategory("Navigation", "controls navigation navigationview tabview", "\uE700", true, false),
            new DemoNavigationCategory("Status and info", "controls status info progress infobar badge person", "\uE916", true, false),
            new DemoNavigationCategory("Layout", "controls layout border dockpanel stackpanel expander separator", "\uECA5", true, false),
            new DemoNavigationCategory("Text", "controls text textblock typography", "\uE8D2", true, false),
            new DemoNavigationCategory("Windowing", "controls window titlebar caption chrome", "\uE737", true, false)
        };

        private static readonly DemoNavigationItem[] CatalogItems =
        {
            new DemoNavigationItem(string.Empty, "Home", "home overview welcome", "\uE80F", true, () => new GalleryHomePage()),

            new DemoNavigationItem("Design", "Color", "color colors brush swatch theme resource high contrast", "\uE790", false, () => new GalleryColorsPage()),
            new DemoNavigationItem("Design", "Iconography", "fonticon icon glyph segoe fluent", "\uED58", false, () => new GalleryGlyphsPage()),
            new DemoNavigationItem("Design", "Typography", "typography text textblock font style", "\uE8D2", false, () => new GalleryTypographyPage()),

            new DemoNavigationItem("Accessibility", "Screen reader support", "accessibility screen reader narrator automation properties", "\uE776", false, () => new GalleryAccessibilityPage()),
            new DemoNavigationItem("Accessibility", "Keyboard support", "accessibility keyboard focus tab order accelerator", "\uE765", false, () => new GalleryAccessibilityPage()),
            new DemoNavigationItem("Accessibility", "Color contrast", "accessibility high contrast color contrast theme", "\uE7F4", false, () => new GalleryAccessibilityPage()),

            new DemoNavigationItem("Basic input", "Button", "button buttons accent icon", "\uE8E5", false, () => DemoControlPages.Button()),
            new DemoNavigationItem("Basic input", "DropDownButton", "dropdownbutton button menu flyout buttons", "\uE70D", false, () => DemoControlPages.DropDownButton()),
            new DemoNavigationItem("Basic input", "HyperlinkButton", "hyperlinkbutton hyperlink button link buttons", "\uE71B", false, () => DemoControlPages.HyperlinkButton()),
            new DemoNavigationItem("Basic input", "RepeatButton", "repeatbutton button repeat buttons", "\uE8EE", false, () => DemoControlPages.RepeatButton()),
            new DemoNavigationItem("Basic input", "ToggleButton", "togglebutton button toggle buttons", "\uE73A", false, () => DemoControlPages.ToggleButton()),
            new DemoNavigationItem("Basic input", "SplitButton", "splitbutton button menu buttons", "\uE8A5", false, () => DemoControlPages.SplitButton()),
            new DemoNavigationItem("Basic input", "CheckBox", "checkbox check selection", "\uE73E", false, () => DemoControlPages.CheckBox()),
            new DemoNavigationItem("Basic input", "ComboBox", "combobox combo dropdown selection", "\uE70D", false, () => DemoControlPages.ComboBox()),
            new DemoNavigationItem("Basic input", "RadioButton", "radiobutton radio selection", "\uE915", false, () => DemoControlPages.RadioButton()),
            new DemoNavigationItem("Basic input", "RatingControl", "ratingcontrol rating selection", "\uE734", false, () => DemoControlPages.RatingControl()),
            new DemoNavigationItem("Basic input", "Slider", "slider range input", "\uE9E9", false, () => DemoControlPages.Slider()),
            new DemoNavigationItem("Basic input", "ToggleSwitch", "toggleswitch toggle switch selection", "\uE73A", false, () => DemoControlPages.ToggleSwitch()),
            new DemoNavigationItem("Basic input", "TextBox", "textbox text input inputs form", "\uE70F", false, () => DemoControlPages.TextBox()),
            new DemoNavigationItem("Basic input", "PasswordBox", "passwordbox password input form", "\uE72E", false, () => DemoControlPages.PasswordBox()),
            new DemoNavigationItem("Basic input", "NumberBox", "numberbox number input form", "\uF16E", false, () => DemoControlPages.NumberBox()),

            new DemoNavigationItem("Collections", "Card", "card collection data", "\uECA5", false, () => DemoCollectionPages.Card()),
            new DemoNavigationItem("Collections", "ListBox", "listbox list collection data selection", "\uEA37", false, () => DemoCollectionPages.ListBox()),
            new DemoNavigationItem("Collections", "ListView", "listview list collection data", "\uE8FD", false, () => DemoCollectionPages.ListView()),
            new DemoNavigationItem("Collections", "TreeView", "treeview tree hierarchy expand collapse", "\uE8EB", false, () => DemoCollectionPages.TreeView()),

            new DemoNavigationItem("Menus and toolbars", "Menu", "menu menuitem command toolbar", "\uE115", false, () => new GalleryMenusPage()),
            new DemoNavigationItem("Menus and toolbars", "ContextMenu", "contextmenu menu command", "\uE700", false, () => new GalleryMenusPage()),
            new DemoNavigationItem("Menus and toolbars", "ToolTip", "tooltip tip help", "\uE946", false, () => new GalleryMenusPage()),

            new DemoNavigationItem("Navigation", "NavigationView", "navigationview navigation pane back", "\uE700", false, () => new GalleryNavigationPage()),
            new DemoNavigationItem("Navigation", "TabView", "tabview tab tabs close add document", "\uF22C", false, () => new GalleryTabsPage()),

            new DemoNavigationItem("Status and info", "InfoBadge", "infobadge badge status info", "\uE946", false, () => DemoStatusPages.InfoBadge()),
            new DemoNavigationItem("Status and info", "InfoBar", "infobar status info message", "\uE946", false, () => DemoStatusPages.InfoBar()),
            new DemoNavigationItem("Status and info", "ProgressBar", "progressbar progress status determinate indeterminate", "\uE9D9", false, () => DemoStatusPages.ProgressBar()),
            new DemoNavigationItem("Status and info", "ProgressRing", "progressring progress status determinate indeterminate ring", "\uE9D9", false, () => DemoStatusPages.ProgressRing()),
            new DemoNavigationItem("Status and info", "PersonPicture", "personpicture avatar profile status", "\uE77B", false, () => DemoStatusPages.PersonPicture()),

            new DemoNavigationItem("Layout", "Border", "border layout", "\uECA5", false, () => new GalleryFormsPage()),
            new DemoNavigationItem("Layout", "DockPanel", "dockpanel layout panel", "\uF0E2", false, () => new GalleryDataPage()),
            new DemoNavigationItem("Layout", "Expander", "expander layout disclosure", "\uE70D", false, () => new GalleryDataPage()),
            new DemoNavigationItem("Layout", "Separator", "separator layout divider", "\uE738", false, () => new GalleryDataPage()),
            new DemoNavigationItem("Layout", "StackPanel", "stackpanel layout panel", "\uF0E2", false, () => new GalleryDataPage()),

            new DemoNavigationItem("Text", "TextBlock", "textblock text typography", "\uE8D2", false, () => new GalleryTypographyPage()),

            new DemoNavigationItem("Windowing", "CaptionButtonChrome", "captionbuttonchrome caption chrome window", "\uE8BB", false, () => new GalleryWindowPage()),
            new DemoNavigationItem("Windowing", "FluenceWindow", "fluencewindow window theme backdrop mica accent titlebar chrome shell", "\uE737", false, () => new GalleryWindowPage()),
            new DemoNavigationItem("Windowing", "TitleBar", "titlebar window chrome caption", "\uE8BB", false, () => new GalleryWindowPage())
        };

        public static IEnumerable<DemoNavigationItem> Items
        {
            get { return CatalogItems; }
        }

        public static bool TryGetCategory(string title, out DemoNavigationCategory category)
        {
            foreach (var item in CatalogCategories)
            {
                if (string.Equals(item.Title, title, StringComparison.Ordinal))
                {
                    category = item;
                    return true;
                }
            }

            category = null;
            return false;
        }
    }
}
