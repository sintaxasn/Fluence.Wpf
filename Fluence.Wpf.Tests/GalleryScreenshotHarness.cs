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
using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Fluence.Wpf.Demo.Pages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Maintainer-driven harness that renders representative demo surfaces with
    /// <see cref="RenderTargetBitmap"/> and writes PNGs under
    /// <c>docs/screenshots/</c>. All tests here are <see cref="IgnoreAttribute"/>d by
    /// default — remove the attribute (or run with
    /// <c>--filter TestCategory=Screenshots</c>) to regenerate documentation images.
    /// </summary>
    /// <remarks>
    /// Captures only the WPF visual tree — DWM Mica / Acrylic backdrops are composited
    /// outside WPF and are not included in RenderTargetBitmap output. That is the
    /// intended behavior: the screenshots document control surfaces and theme resources,
    /// not DWM composition.
    /// </remarks>
    [TestClass]
    [TestCategory("Screenshots")]
    public class GalleryScreenshotHarness
    {
        private const int CaptureWidth = 1280;
        private const int CaptureHeight = 800;
        private const double BaseDpi = 96.0;

        private static readonly double[] Scales = [1.0, 1.5];

        private static readonly (ApplicationTheme theme, string slug)[] Themes =
        [
            (ApplicationTheme.Light, "light"),
            (ApplicationTheme.Dark, "dark"),
            (ApplicationTheme.HighContrast, "highcontrast"),
        ];

        private static void RunOnStaThread(Action action)
        {
            Exception? captured = null;
            WpfTestSta.Dispatcher?.Invoke(new Action(delegate
            {
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    captured = exception;
                }
            }));

            if (captured is not null)
            {
                ExceptionDispatchInfo.Capture(captured).Throw();
            }
        }

        private static Application? EnsureApplication()
        {
            return WpfTestSta.EnsureApplication();
        }

        private static void DrainDispatcher(Dispatcher dispatcher)
        {
            _ = dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));
        }

        private static string FindRepoRoot()
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Fluence.Wpf.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException(
                "Could not locate Fluence.Wpf.sln ancestor directory from " + AppContext.BaseDirectory);
        }

        private static string EnsureOutputDirectory()
        {
            string root = FindRepoRoot();
            string path = Path.Combine(root, "docs", "screenshots");
            _ = Directory.CreateDirectory(path);
            return path;
        }

        private static void SavePng(Visual visual, int pixelWidth, int pixelHeight, double dpi, string fullPath)
        {
            RenderTargetBitmap bitmap = new(pixelWidth, pixelHeight, dpi, dpi, PixelFormats.Pbgra32);
            bitmap.Render(visual);

            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using FileStream stream = File.Create(fullPath);
            encoder.Save(stream);
        }

        private static void CaptureHomeAt(ApplicationTheme theme, string themeSlug, double scale, string outputDirectory)
        {
            Application? application = EnsureApplication();
            application?.Resources.MergedDictionaries.Clear();
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            ApplicationThemeManager.Apply(theme, BackdropType.None, true);

            ResourceDictionary demoShared = new()
            {
                Source = new Uri("/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml", UriKind.Relative)
            };
            application?.Resources.MergedDictionaries.Add(demoShared);

            Window? window = null;
            try
            {
                Border host = new()
                {
                    Width = CaptureWidth,
                    Height = CaptureHeight,
                };
                host.SetResourceReference(Border.BackgroundProperty, "SolidBackgroundFillColorBaseBrush");

                GalleryHomePage page = new();
                host.Child = page;

                // Plain Window avoids the FluenceWindow DWM backdrop (which RenderTargetBitmap
                // can't capture) and keeps the capture focused on the control surface.
                window = new Window
                {
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    WindowStyle = WindowStyle.None,
                    ShowInTaskbar = false,
                    Top = -10000,
                    Left = -10000,
                    AllowsTransparency = false,
                    Content = host,
                };

                window.Show();
                DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();

                // Re-fire the theme apply *after* the page has subscribed in its Loaded
                // handler. This guarantees the banner image (and any Changed-driven state)
                // reflects the requested theme.
                ApplicationThemeManager.Apply(theme, BackdropType.None, true);
                DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();
                _ = window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(delegate { }));

                int pixelWidth = (int)Math.Round(host.ActualWidth * scale);
                int pixelHeight = (int)Math.Round(host.ActualHeight * scale);
                double dpi = BaseDpi * scale;
                string slug = Invariant("banner-{0}-{1}x.png", themeSlug, scale.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
                string fullPath = Path.Combine(outputDirectory, slug);

                SavePng(host, pixelWidth, pixelHeight, dpi, fullPath);
                Assert.IsTrue(File.Exists(fullPath), Invariant("Expected to write {0}", fullPath));
            }
            finally
            {
                window?.Close();

                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
            }
        }

        private static string Invariant(string format, params object[] args)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args);
        }

        [TestMethod]
        public void CaptureBannerAcrossThemesAndScales()
        {
            // Opt-in: CI runs skip this test unless FLUENCE_CAPTURE_SCREENSHOTS=1 is set in
            // the environment. Maintainers enable it when refreshing docs/screenshots/.
            if (!string.Equals(
                Environment.GetEnvironmentVariable("FLUENCE_CAPTURE_SCREENSHOTS"),
                "1",
                StringComparison.Ordinal))
            {
                Assert.Inconclusive(
                    "Set FLUENCE_CAPTURE_SCREENSHOTS=1 to regenerate docs/screenshots/. Skipping screenshot capture.");
                return;
            }

            RunOnStaThread(() =>
            {
                string output = EnsureOutputDirectory();
                List<string> written = [];
                foreach ((ApplicationTheme theme, string? slug) in Themes)
                {
                    foreach (double scale in Scales)
                    {
                        CaptureHomeAt(theme, slug, scale, output);
                        written.Add(Invariant("banner-{0}-{1}x.png", slug, scale.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)));
                    }
                }

                Assert.AreEqual(Themes.Length * Scales.Length, written.Count);
            });
        }
    }
}
