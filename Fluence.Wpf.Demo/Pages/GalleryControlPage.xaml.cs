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
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public sealed class DemoExample
    {
        public DemoExample(string title, string description, string sourcePath, Func<UIElement> createContent)
        {
            Title = title;
            Description = description;
            SourcePath = sourcePath;
            CreateContent = createContent;
        }

        public string Title { get; private set; }

        public string Description { get; private set; }

        public string SourcePath { get; private set; }

        public Func<UIElement> CreateContent { get; private set; }
    }

    public partial class GalleryControlPage : UserControl
    {
        public GalleryControlPage(string title, string description, IEnumerable<DemoExample> examples)
        {
            InitializeComponent();

            ControlPageTitle.Text = title;
            ControlPageDescription.Text = description;

            foreach (var example in examples)
            {
                AddExample(example);
            }
        }

        private void AddExample(DemoExample example)
        {
            var content = example.CreateContent();
            var contentElement = content as FrameworkElement;
            if (contentElement != null)
            {
                contentElement.Margin = new Thickness(0);
            }

            PageStack.Children.Add(new DemoSampleControl
            {
                Title = example.Title,
                Description = example.Description,
                SourcePath = example.SourcePath,
                SampleContent = content
            });
        }
    }
}
