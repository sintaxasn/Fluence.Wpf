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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Phase 2 tests: Fluent <see cref="Controls.Image"/>.
    /// Authority: in-tree precedent (PersonPicture stroke tokens, FontIcon non-interactive shape);
    /// WinUI 3 ships no styled Image control.
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // Phase 2  Image
        // ---------------------------------------------------------------------------

        /// <summary>
        /// Builds a small frozen probe bitmap without any file IO.
        /// </summary>
        private static BitmapSource CreateProbeBitmap()
        {
            DrawingVisual visual = new();
            using (DrawingContext context = visual.RenderOpen())
            {
                context.DrawRectangle(Brushes.OrangeRed, pen: null, new Rect(0, 0, 16, 16));
            }
            RenderTargetBitmap bitmap = new(16, 16, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();
            return bitmap;
        }

        [TestMethod]
        public void Image_DefaultStyle_TemplatePartsPresent()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Image image = new();
                Window w = new() { Content = image, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Image? inner = FindVisualChildByName<System.Windows.Controls.Image>(image, "PART_Image");
                Assert.IsNotNull(inner, "PART_Image must be present.");

                System.Windows.Controls.Border? frame = FindVisualChildByName<System.Windows.Controls.Border>(image, "PART_ImageBorder");
                Assert.IsNotNull(frame, "PART_ImageBorder must be present.");
                w.Close();
            });
        }

        [TestMethod]
        public void Image_SourceAndStretch_FlowToInnerImage()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                BitmapSource probe = CreateProbeBitmap();
                Controls.Image image = new() { Source = probe, Stretch = Stretch.UniformToFill };
                Window w = new() { Content = image, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Image? inner = FindVisualChildByName<System.Windows.Controls.Image>(image, "PART_Image");
                Assert.IsNotNull(inner);
                Assert.AreSame(probe, inner.Source,
                    "Source must flow to PART_Image via TemplateBinding.");
                Assert.AreEqual(Stretch.UniformToFill, inner.Stretch,
                    "Stretch must flow to PART_Image via TemplateBinding.");
                w.Close();
            });
        }

        [TestMethod]
        public void Image_CornerRadius_SetsAndClearsInnerClip()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Image image = new() { Source = CreateProbeBitmap(), CornerRadius = new CornerRadius(8) };
                Window w = new() { Content = image, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);
                w.UpdateLayout();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Image? inner = FindVisualChildByName<System.Windows.Controls.Image>(image, "PART_Image");
                Assert.IsNotNull(inner);

                RectangleGeometry? clip = inner.Clip as RectangleGeometry;
                Assert.IsNotNull(clip, "CornerRadius > 0 must set a RectangleGeometry clip on PART_Image.");
                Assert.AreEqual(8.0, clip.RadiusX, "The clip must use the top-left corner radius uniformly.");
                Assert.IsTrue(clip.IsFrozen, "The clip geometry must be frozen.");

                image.CornerRadius = new CornerRadius(0);
                DrainDispatcher(w.Dispatcher);
                Assert.IsNull(inner.Clip, "CornerRadius = 0 must clear the clip on PART_Image.");
                w.Close();
            });
        }

        [TestMethod]
        public void Image_ThemeCycle_StyleRemainsApplied()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Image image = new() { Source = CreateProbeBitmap() };
                Window w = new() { Content = image, Width = 200, Height = 200 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                DrainDispatcher(w.Dispatcher);

                System.Windows.Controls.Border? frame = FindVisualChildByName<System.Windows.Controls.Border>(image, "PART_ImageBorder");
                Assert.IsNotNull(frame, "PART_ImageBorder must still be present after theme cycle.");
                Assert.IsNotNull(frame.BorderBrush, "The stroke brush must re-resolve after theme cycle.");
                w.Close();
            });
        }

        [TestMethod]
        public void Image_AutomationPeer_IsImageAutomationPeer()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Image image = new();
                Window w = new() { Content = image, Width = 200, Height = 200 };
                w.Show();
                _ = image.ApplyTemplate();
                DrainDispatcher(w.Dispatcher);

                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(image);
                _ = Assert.IsInstanceOfType<Automation.ImageAutomationPeer>(peer,
                    "Image must create a Fluence ImageAutomationPeer.");
                Assert.AreEqual(AutomationControlType.Image, peer.GetAutomationControlType(),
                    "Image automation peer must report control type Image.");
                Assert.AreEqual("Image", peer.GetClassName(), StringComparer.Ordinal,
                    "Image automation peer must report class name 'Image'.");
                w.Close();
            });
        }

        /// <summary>
        /// An image with no accessible name is decorative and must leave the UI Automation tree
        /// entirely, matching WinUI's AccessibilityView=Raw default and FontIcon's treatment. If it
        /// stayed in the content view a screen reader would announce an empty image element.
        /// </summary>
        [TestMethod]
        public void Image_AutomationPeer_UnnamedImageIsExcludedFromBothViews()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Image image = new();
                Window w = new() { Content = image, Width = 200, Height = 200 };
                w.Show();
                _ = image.ApplyTemplate();
                DrainDispatcher(w.Dispatcher);

                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(image);
                Assert.IsFalse(peer.IsControlElement(),
                    "An unnamed Image is decorative and must be excluded from the UI Automation control view.");
                Assert.IsFalse(peer.IsContentElement(),
                    "An unnamed Image must be excluded from the UI Automation content view so nothing announces an empty image.");
                w.Close();
            });
        }

        /// <summary>
        /// Setting <c>AutomationProperties.Name</c> makes the image meaningful, so it must appear in
        /// both automation views and report that name.
        /// </summary>
        [TestMethod]
        public void Image_AutomationPeer_NamedImageIsIncludedInBothViews()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.Image image = new();
                AutomationProperties.SetName(image, "Company logo");
                Window w = new() { Content = image, Width = 200, Height = 200 };
                w.Show();
                _ = image.ApplyTemplate();
                DrainDispatcher(w.Dispatcher);

                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(image);
                Assert.IsTrue(peer.IsControlElement(),
                    "A named Image conveys meaning and must appear in the UI Automation control view.");
                Assert.IsTrue(peer.IsContentElement(),
                    "A named Image must appear in the UI Automation content view so a screen reader reads it.");
                Assert.AreEqual("Company logo", peer.GetName(), StringComparer.Ordinal,
                    "The peer must surface the accessible name the consumer set.");
                w.Close();
            });
        }

        /// <summary>
        /// Labelling by another element is a legitimate way to name an image, and the base peer
        /// already resolves its name through it, so a LabeledBy image must not be dropped from the
        /// tree. This is why the peer checks LabeledBy as well as Name.
        /// </summary>
        [TestMethod]
        public void Image_AutomationPeer_LabeledByImageIsIncludedInBothViews()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                System.Windows.Controls.TextBlock label = new() { Text = "Product photo" };
                Controls.Image image = new();
                AutomationProperties.SetLabeledBy(image, label);
                System.Windows.Controls.StackPanel host = new();
                _ = host.Children.Add(label);
                _ = host.Children.Add(image);
                Window w = new() { Content = host, Width = 200, Height = 200 };
                w.Show();
                _ = image.ApplyTemplate();
                DrainDispatcher(w.Dispatcher);

                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(image);
                Assert.IsTrue(peer.IsControlElement(),
                    "An Image named through AutomationProperties.LabeledBy must appear in the control view.");
                Assert.IsTrue(peer.IsContentElement(),
                    "An Image named through AutomationProperties.LabeledBy must appear in the content view.");
                w.Close();
            });
        }
    }
}
