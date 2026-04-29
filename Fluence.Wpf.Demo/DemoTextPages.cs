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
using System.Windows;
using Fluence.Wpf.Demo.Pages;
using Fluent = Fluence.Wpf.Controls;

namespace Fluence.Wpf.Demo
{
    internal static class DemoTextPages
    {
        public static GalleryControlPage TextBlock()
        {
            return CreatePage(
                "TextBlock",
                "Use TextBlock to display read-only text with Fluent typography, wrapping, and trimming.",
                new[] { Example("Typography and overflow", "TextBlock supports the Fluent type ramp plus standard text wrapping and trimming.", "Text/TextBlock.xaml", CreateTextBlocks) });
        }

        private static GalleryControlPage CreatePage(string title, string description, DemoExample[] examples)
        {
            return new GalleryControlPage(title, description, examples);
        }

        private static DemoExample Example(string title, string description, string sourcePath, Func<UIElement> createContent)
        {
            return new DemoExample(title, description, sourcePath, createContent);
        }

        private static UIElement CreateTextBlocks()
        {
            var stack = new Fluent.StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 560,
                Spacing = 10
            };

            stack.Children.Add(new Fluent.TextBlock { Text = "Title text", Typography = FluentTypography.Title });
            stack.Children.Add(new Fluent.TextBlock { Text = "Body text uses Fluent typography by default." });
            stack.Children.Add(new Fluent.TextBlock { Text = "Caption text", Typography = FluentTypography.Caption });
            stack.Children.Add(new Fluent.TextBlock
            {
                Text = "Wrapped text keeps longer read-only content readable within the available width.",
                TextWrapping = TextWrapping.Wrap,
                Width = 360
            });
            stack.Children.Add(new Fluent.TextBlock
            {
                Text = "Trimmed text shows an ellipsis when the line cannot fit in the available width.",
                TextTrimming = TextTrimming.CharacterEllipsis,
                Width = 260
            });

            return stack;
        }
    }
}
