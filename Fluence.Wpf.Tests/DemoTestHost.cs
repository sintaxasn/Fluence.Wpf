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
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace Fluence.Wpf.Tests
{
    internal static class DemoTestHost
    {
        private static readonly Uri DemoSharedStylesUri = new(
            "/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml",
            UriKind.Relative);

        internal static void RunOnSta(Action action)
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

        internal static Application EnsureDemoTheme(BackdropType backdrop = BackdropType.None)
        {
            Application application = WpfTestSta.EnsureApplication() ?? throw new InvalidOperationException("WPF application was not created.");
            ResetApplication(application);
            ApplicationThemeManager.Apply(ApplicationTheme.Light, backdrop, true);
            ApplicationAccentColorManager.ApplySystemAccent();
            AddDemoSharedStyles(application);
            return application;
        }

        internal static void AddDemoSharedStyles(Application application)
        {
            application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = DemoSharedStylesUri });
        }

        internal static Window CreateHostWindow(UIElement content)
        {
            Window window = new()
            {
                Left = -20000,
                Top = -20000,
                Width = 1040,
                Height = 720,
                WindowStartupLocation = WindowStartupLocation.Manual,
                ShowInTaskbar = false,
                Content = content
            };
            window.Show();
            Drain(window.Dispatcher);
            window.UpdateLayout();
            Drain(window.Dispatcher);
            return window;
        }

        internal static void CloseWindow(Window window)
        {
            window.Content = null;
            window.Close();
            Drain(window.Dispatcher);
        }

        internal static void Drain(Dispatcher? dispatcher)
        {
            _ = dispatcher?.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));
        }

        internal static T? FindByName<T>(DependencyObject? root, string name)
            where T : FrameworkElement
        {
            foreach (T item in FindVisualChildren<T>(root))
            {
                if (string.Equals(item.Name, name, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            return null;
        }

        internal static IEnumerable<T> FindVisualChildren<T>(DependencyObject? root)
            where T : DependencyObject
        {
            HashSet<DependencyObject> visited = [];
            foreach (T item in FindVisualChildren<T>(root, visited))
            {
                yield return item;
            }
        }

        internal static string GetRepositoryFilePath(params string[] relativeSegments)
        {
            string root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\.."));
            string[] pathParts = new string[relativeSegments.Length + 1];
            pathParts[0] = root;
            Array.Copy(relativeSegments, 0, pathParts, 1, relativeSegments.Length);
            return Path.Combine(pathParts);
        }

        internal static string ReadRepositoryFile(params string[] relativeSegments)
        {
            string path = GetRepositoryFilePath(relativeSegments);
            return File.ReadAllText(path);
        }

        private static void ResetApplication(Application application)
        {
            Window[] windows = [.. application.Windows.Cast<Window>()];
            foreach (Window window in windows)
            {
                window.Content = null;
                window.Close();
            }

            Drain(Dispatcher.CurrentDispatcher);
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application.Resources.MergedDictionaries.Clear();
            application.Resources.Clear();
        }

        private static IEnumerable<T> FindVisualChildren<T>(
            DependencyObject? root,
            HashSet<DependencyObject> visited)
            where T : DependencyObject
        {
            if (root is null || !visited.Add(root))
            {
                yield break;
            }

            if (root is T match)
            {
                yield return match;
            }

            int visualChildren = 0;
            if (root is Visual or Visual3D)
            {
                visualChildren = VisualTreeHelper.GetChildrenCount(root);
            }

            for (int i = 0; i < visualChildren; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                foreach (T item in FindVisualChildren<T>(child, visited))
                {
                    yield return item;
                }
            }

            foreach (object logicalChild in LogicalTreeHelper.GetChildren(root))
            {
                if (logicalChild is DependencyObject dependencyObject)
                {
                    foreach (T item in FindVisualChildren<T>(dependencyObject, visited))
                    {
                        yield return item;
                    }
                }
            }
        }
    }
}
