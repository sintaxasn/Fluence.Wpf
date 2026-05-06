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

namespace Fluence.Wpf.Demo
{
    public sealed class DemoNavigationItem(string title, string route, string keywords, string glyph, bool isDefault)
    {
        public string Title { get; private set; } = title;

        public string Route { get; private set; } = route;

        public string Keywords { get; private set; } = keywords;

        public string Glyph { get; private set; } = glyph;

        public bool IsDefault { get; private set; } = isDefault;
    }

    public static class DemoNavigationCatalog
    {
        private static readonly DemoNavigationItem[] CatalogItems =
        [
            new("Home", "home", "overview welcome start", "\uE80F", true),
            new("Colors", "colors", "color brush swatch theme resource high contrast accent", "\uE790", false),
            new("Iconography", "iconography", "fonticon icon glyph segoe fluent symbols", "\uED58", false),
            new("Typography", "typography", "text textblock font style type ramp", "\uE8D2", false),
            new("Buttons", "buttons", "button dropdownbutton splitbutton hyperlinkbutton repeatbutton togglebutton accent icon", "\uE8E5", false),
            new("Selection", "selection", "checkbox radio radiobutton toggleswitch combobox rating slider", "\uE73E", false),
            new("Inputs", "inputs", "textbox passwordbox numberbox slider text input validation", "\uE70F", false),
            new("Forms", "forms", "signin checkout settings form text input", "\uE7C3", false),
            new("Data", "data", "card listbox listview collection empty state", "\uE8FD", false),
            new("Data binding", "data binding", "observablecollection binding selection datatemplate", "\uE8FD", false),
            new("Trees", "trees", "treeview hierarchy expand collapse selection", "\uE8EB", false),
            new("Menus", "menus", "menu menubar contextmenu tooltip dropdown split flyout command", "\uE115", false),
            new("Navigation", "navigation", "navigationview pane left top compact", "\uE700", false),
            new("Tabs", "tabs", "tabcontrol tabview tab tabs document close add", "\uF22C", false),
            new("Layout", "layout", "border dockpanel stackpanel expander separator layout surface", "\uECA5", false),
            new("Status", "status", "infobar infobadge progressbar progressring personpicture progress ring busy", "\uE916", false),
            new("Accessibility", "accessibility", "screen reader narrator automation keyboard focus contrast rtl", "\uE776", false),
            new("Windowing", "window", "fluencewindow window theme backdrop mica acrylic titlebar caption chrome", "\uE737", false)
        ];

        public static IEnumerable<DemoNavigationItem> Items => CatalogItems;
    }
}
