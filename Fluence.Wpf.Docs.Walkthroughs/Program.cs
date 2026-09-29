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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Fluence.Wpf.Docs.Walkthroughs
{
    internal static class Program
    {
        private static readonly (string Slug, string Title)[] Scenarios =
        [
            ("basic-usage", "Basic usage"),
            ("window-and-title-bar", "Window and title bar"),
            ("theme-and-accent", "Theme, accent and backdrop"),
            ("inputs-and-data", "Inputs and data"),
            ("navigation-and-tabs", "Navigation and tabs"),
            ("dialogs-and-feedback", "Dialogs and feedback"),
            ("controls-from-csharp", "Controls from C#"),
        ];

        [STAThread]
        private static void Main(string[] args)
        {
            Application app = new() { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            ApplicationThemeManager.Apply(ApplicationTheme.Light);
            WalkthroughWindow window = new(Scenarios);
            app.MainWindow = window;

            if (Array.Exists(args, static arg => string.Equals(arg, "--capture-all", StringComparison.OrdinalIgnoreCase)))
            {
                window.Loaded += (_, _) => _ = window.Dispatcher.BeginInvoke(
                    new Action(() => _ = CaptureAllAsync(window, app)), DispatcherPriority.Loaded);
            }

            window.Show();
            _ = app.Run();
        }

        private static async Task CaptureAllAsync(WalkthroughWindow window, Application app)
        {
            string outputDirectory = Path.Combine(FindRepositoryRoot(), "docs", "screenshots", "tutorials");
            _ = Directory.CreateDirectory(outputDirectory);

            try
            {
                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark })
                {
                    if (theme is ApplicationTheme.Dark)
                    {
                        window.Close();
                        ApplicationThemeManager.Apply(theme);
                        window = new WalkthroughWindow(window.Scenarios);
                        app.MainWindow = window;
                        window.Show();
                    }

                    ApplicationThemeManager.Apply(theme);
                    window.SystemBackdropType = WindowBackdropType.None;
                    foreach ((string slug, _) in Scenarios)
                    {
                        window.ShowScenario(slug);
                        window.UpdateLayout();
                        await window.Dispatcher.InvokeAsync(static () => { }, DispatcherPriority.ApplicationIdle, CancellationToken.None);
                        await Task.Delay(220, CancellationToken.None).ConfigureAwait(true);
                        window.UpdateLayout();

                        int width = (int)Math.Ceiling(window.ActualWidth);
                        int height = (int)Math.Ceiling(window.ActualHeight);
                        RenderTargetBitmap bitmap = new(width, height, 96, 96, PixelFormats.Pbgra32);
                        bitmap.Render(window);
                        PngBitmapEncoder encoder = new();
                        encoder.Frames.Add(BitmapFrame.Create(bitmap));
                        string file = Path.Combine(outputDirectory, $"{slug}-{theme.ToString().ToLowerInvariant()}.png");
                        FileStream stream = File.Create(file);
                        await using (stream.ConfigureAwait(false))
                        {
                            encoder.Save(stream);
                        }
                    }
                }
            }
            finally
            {
                app.Shutdown();
            }
        }

        private static string FindRepositoryRoot()
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

            throw new DirectoryNotFoundException("The Fluence.Wpf.sln ancestor was not found.");
        }
    }
}
