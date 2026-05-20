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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FluenceExpander = Fluence.Wpf.Controls.Expander;
using WpfButton = System.Windows.Controls.Button;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public sealed class DemoSamplePageWiringTests
    {
        private static readonly Func<UIElement>[] SamplePageFactories =
        [
            static () => new GalleryIconsPage(),
            static () => new GalleryAccessibilityPage(),
            static () => new GalleryButtonsPage(),
            static () => new GallerySelectionPage(),
            static () => new GalleryInputsPage(),
            static () => new GalleryFormsPage(),
            static () => new GalleryDataPage(),
            static () => new GalleryDataBindingPage(),
            static () => new GalleryTreesPage(),
            static () => new GalleryMenusPage(),
            static () => new GalleryNavigationPage(),
            static () => new GalleryTabsPage(),
            static () => new GalleryLayoutPage(),
            static () => new GalleryStatusPage()
        ];

        [TestMethod]
        public void DemoSamplePageWiring_MovesSlotContentAndAppliesTypedSources()
        {
            DemoTestHost.RunOnSta(delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                WpfTextBlock demoContent = new() { Text = "Demo" };
                WpfTextBlock outputContent = new() { Text = "Output" };
                CheckBox rightRailContent = new() { Content = "Option" };
                ContentControl demoSlot = CreateSlot("DemoSampleSlot01DemoContentHost", demoContent);
                ContentControl outputSlot = CreateSlot("DemoSampleSlot01OutputContentHost", outputContent);
                ContentControl rightRailSlot = CreateSlot("DemoSampleSlot01RightRailContentHost", rightRailContent);
                DemoSampleControl sample = new();
                StackPanel root = new();
                _ = root.Children.Add(demoSlot);
                _ = root.Children.Add(outputSlot);
                _ = root.Children.Add(rightRailSlot);
                _ = root.Children.Add(sample);

                DemoSamplePageWiring.Apply(root, new DemoSampleSource(1, "<Grid />", "public void Demo() { }"));

                Assert.AreSame(demoContent, sample.DemoContent, "Demo slot content should move into the sample.");
                Assert.AreSame(outputContent, sample.OutputContent, "Output slot content should move into the sample.");
                Assert.AreSame(rightRailContent, sample.RightRailContent, "Right rail slot content should move into the sample.");
                Assert.IsNull(demoSlot.Content, "Demo slot content should be cleared after transfer.");
                Assert.IsNull(outputSlot.Content, "Output slot content should be cleared after transfer.");
                Assert.IsNull(rightRailSlot.Content, "Right rail slot content should be cleared after transfer.");
                Assert.AreEqual("<Grid />", sample.XamlSource);
                Assert.AreEqual("public void Demo() { }", sample.CSharpSource);
            });
        }

        [TestMethod]
        public void DemoSamplePageWiring_RejectsSourceCountMismatch()
        {
            DemoTestHost.RunOnSta(delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                StackPanel root = new();
                _ = root.Children.Add(new DemoSampleControl());
                _ = root.Children.Add(new DemoSampleControl());

                AssertThrowsInvalidOperation(
                    () => DemoSamplePageWiring.Apply(root, new DemoSampleSource(1, "<Grid />", string.Empty)));
            });
        }

        [TestMethod]
        public void DemoSamplePageWiring_RejectsDuplicateSourceSlots()
        {
            DemoTestHost.RunOnSta(delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                StackPanel root = new();
                _ = root.Children.Add(new DemoSampleControl());

                AssertThrowsInvalidOperation(
                    () => DemoSamplePageWiring.Apply(
                        root,
                        new DemoSampleSource(1, "<Grid />", string.Empty),
                        new DemoSampleSource(1, "<StackPanel />", string.Empty)));
            });
        }

        [TestMethod]
        public void DemoSamplePageWiring_RejectsUnusedContentSlots()
        {
            DemoTestHost.RunOnSta(delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                StackPanel root = new();
                _ = root.Children.Add(CreateSlot("DemoSampleSlot02DemoContentHost", new WpfTextBlock()));
                _ = root.Children.Add(new DemoSampleControl());

                AssertThrowsInvalidOperation(
                    () => DemoSamplePageWiring.Apply(root, new DemoSampleSource(1, "<Grid />", string.Empty)));
            });
        }

        [TestMethod]
        public void DemoSamplePageWiring_RejectsZeroContentSlot()
        {
            DemoTestHost.RunOnSta(delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                StackPanel root = new();
                _ = root.Children.Add(CreateSlot("DemoSampleSlot00DemoContentHost", new WpfTextBlock()));
                _ = root.Children.Add(new DemoSampleControl());

                AssertThrowsInvalidOperation(
                    () => DemoSamplePageWiring.Apply(root, new DemoSampleSource(1, "<Grid />", string.Empty)));
            });
        }

        [TestMethod]
        public void DemoSamplePageWiring_RejectsDuplicateContentSlots()
        {
            DemoTestHost.RunOnSta(delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                StackPanel root = new();
                _ = root.Children.Add(CreateSlot("DemoSampleSlot01DemoContentHost", new WpfTextBlock()));
                _ = root.Children.Add(CreateSlot("DemoSampleSlot01DemoContentHost", new WpfTextBlock()));
                _ = root.Children.Add(new DemoSampleControl());

                AssertThrowsInvalidOperation(
                    () => DemoSamplePageWiring.Apply(root, new DemoSampleSource(1, "<Grid />", string.Empty)));
            });
        }

        [TestMethod]
        public void DemoSampleControl_ReloadsExpandedSourceTabsWhenSourceChanges()
        {
            DemoTestHost.RunOnSta(delegate
            {
                _ = DemoTestHost.EnsureDemoTheme();
                DemoSampleControl sample = new()
                {
                    DemoContent = new WpfTextBlock { Text = "Body" },
                    XamlSource = "<Grid />"
                };
                Window window = DemoTestHost.CreateHostWindow(sample);
                try
                {
                    FluenceExpander? expander = DemoTestHost.FindByName<FluenceExpander>(sample, "SourceExpander");
                    Assert.IsNotNull(expander, "Source expander should exist.");
                    expander.IsExpanded = true;
                    DemoTestHost.Drain(window.Dispatcher);
                    window.UpdateLayout();

                    AssertSourceCopyTag(sample, "<Grid />");
                    sample.XamlSource = "<StackPanel />";
                    DemoTestHost.Drain(window.Dispatcher);
                    window.UpdateLayout();

                    AssertSourceCopyTag(sample, "<StackPanel />");
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [TestMethod]
        public void GallerySamplePages_AllVisibleDemoSamplesExposeSource()
        {
            DemoTestHost.RunOnSta(delegate
            {
                foreach (Func<UIElement> factory in SamplePageFactories)
                {
                    _ = DemoTestHost.EnsureDemoTheme();
                    UIElement page = factory();
                    Window window = DemoTestHost.CreateHostWindow(page);
                    try
                    {
                        List<DemoSampleControl> samples = [.. DemoTestHost.FindVisualChildren<DemoSampleControl>(page)];
                        Assert.IsTrue(samples.Count > 0, "Page should expose DemoSampleControl samples: " + page.GetType().Name);
                        foreach (DemoSampleControl sample in samples.Where(static sample => sample.Visibility == Visibility.Visible))
                        {
                            Assert.IsFalse(string.IsNullOrWhiteSpace(sample.XamlSource),
                                "Visible DemoSampleControl should expose XAML source: " + page.GetType().Name);
                        }
                    }
                    finally
                    {
                        DemoTestHost.CloseWindow(window);
                    }
                }
            });
        }

        private static ContentControl CreateSlot(string name, object content)
        {
            return new ContentControl
            {
                Name = name,
                Content = content,
                Visibility = Visibility.Collapsed
            };
        }

        private static void AssertSourceCopyTag(DemoSampleControl sample, string expectedSource)
        {
            TabControl? tabs = DemoTestHost.FindByName<TabControl>(sample, "SourceTabControl");
            Assert.IsNotNull(tabs, "Source tabs should exist.");
            Assert.AreEqual(1, tabs.Items.Count, "XAML-only sample should expose one source tab.");
            TabItem tab = (TabItem)tabs.Items[0];
            WpfButton? copy = DemoTestHost.FindByName<WpfButton>(tab.Content as DependencyObject, "CopySourceButton");
            Assert.IsNotNull(copy, "Source tab should expose the copy button.");
            Assert.AreEqual(expectedSource, copy.Tag as string);
        }

        private static void AssertThrowsInvalidOperation(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            Assert.Fail("Expected InvalidOperationException.");
        }
    }
}
