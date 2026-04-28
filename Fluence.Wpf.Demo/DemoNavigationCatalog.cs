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

    public static class DemoNavigationCatalog
    {
        private static readonly DemoNavigationItem[] CatalogItems =
        {
            new DemoNavigationItem(string.Empty, "Home", "home overview welcome", "\uE80F", true, () => new GalleryHomePage()),

            new DemoNavigationItem("Fundamentals", "Data Binding", "databinding observablecollection listview selectionmode", "\uEA37", false, () => new GalleryDataBindingPage()),
            new DemoNavigationItem("Fundamentals", "Accessibility", "accessibility focus tab rtl highcontrast automation narrator", "\uE776", false, () => new GalleryAccessibilityPage()),

            new DemoNavigationItem("Basic input", "Buttons", "button hyperlink accent", "\uE8E5", false, () => new GalleryButtonsPage()),
            new DemoNavigationItem("Basic input", "Selection", "checkbox radiobutton toggleswitch toggle", "\uE73E", false, () => new GallerySelectionPage()),
            new DemoNavigationItem("Basic input", "Inputs", "textbox password combobox slider number", "\uE70F", false, () => new GalleryInputsPage()),
            new DemoNavigationItem("Basic input", "Forms", "form validation password email numberbox", "\uE943", false, () => new GalleryFormsPage()),

            new DemoNavigationItem("Collections", "Data", "listview list layout card dock stack scroll", "\uE80A", false, () => new GalleryDataPage()),
            new DemoNavigationItem("Collections", "Trees", "treeview tree hierarchy expand collapse", "\uE8EB", false, () => new GalleryTreesPage()),

            new DemoNavigationItem("Navigation", "Navigation", "navigationview pane back", "\uE700", false, () => new GalleryNavigationPage()),
            new DemoNavigationItem("Navigation", "Tabs", "tabcontrol tabview tab close add document", "\uF22C", false, () => new GalleryTabsPage()),
            new DemoNavigationItem("Navigation", "Menus", "menu contextmenu tooltip dropdownbutton splitbutton", "\uE115", false, () => new GalleryMenusPage()),

            new DemoNavigationItem("Status and info", "Status", "progress infobar ring bar", "\uE916", false, () => new GalleryStatusPage()),

            new DemoNavigationItem("Styles", "Colors", "brush swatch theme resource high contrast", "\uE790", false, () => new GalleryColorsPage()),
            new DemoNavigationItem("Styles", "Glyphs", "fonticon icon segoe fluent", "\uED58", false, () => new GalleryGlyphsPage()),

            new DemoNavigationItem("Windowing", "Window", "window theme backdrop mica accent titlebar chrome shell", "\uE737", false, () => new GalleryWindowPage())
        };

        public static IEnumerable<DemoNavigationItem> Items
        {
            get { return CatalogItems; }
        }
    }
}
