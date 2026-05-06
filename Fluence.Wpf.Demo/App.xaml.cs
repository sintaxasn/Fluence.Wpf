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
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Fluence.Wpf.Demo
{
    public partial class App : Application
    {
        private const string IconographyPageTitle = "Iconography";
        private const string SmokeTestArgument = "--smoke-test";
        private static readonly Uri DemoSharedStylesUri = new Uri(
            "/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml",
            UriKind.Relative);

        static App()
        {
            var textOptionsMetadata = FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsRender |
                FrameworkPropertyMetadataOptions.Inherits;

            TextOptions.TextFormattingModeProperty.OverrideMetadata(
                typeof(Window),
                new FrameworkPropertyMetadata(TextFormattingMode.Display, textOptionsMetadata));
            TextOptions.TextRenderingModeProperty.OverrideMetadata(
                typeof(Window),
                new FrameworkPropertyMetadata(TextRenderingMode.ClearType, textOptionsMetadata));
            TextOptions.TextHintingModeProperty.OverrideMetadata(
                typeof(Window),
                new FrameworkPropertyMetadata(TextHintingMode.Fixed, textOptionsMetadata));
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ApplicationThemeManager.Apply(ApplicationTheme.Auto, BackdropType.Auto, true);
            ApplicationAccentColorManager.ApplySystemAccent();
            LoadDemoSharedStyles();

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();

            if (IsSmokeTest(e.Args))
            {
                Dispatcher.BeginInvoke(new Action(delegate { RunSmokeTest(mainWindow); }), DispatcherPriority.ApplicationIdle);
            }
        }

        private static void RunSmokeTest(MainWindow mainWindow)
        {
            foreach (var item in DemoNavigationCatalog.Items)
            {
                mainWindow.NavigateTo(item.Title);
                mainWindow.UpdateLayout();
                DrainDispatcher(mainWindow.Dispatcher);
                if (string.Equals(item.Title, IconographyPageTitle, StringComparison.Ordinal))
                {
                    RealizeIconographyList(mainWindow);
                }
            }

            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.Auto, true);
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.Auto, true);
            ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.Auto, true);
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.Auto, true);

            mainWindow.Close();
        }

        private static void LoadDemoSharedStyles()
        {
            Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = DemoSharedStylesUri });
        }

        private static void RealizeIconographyList(DependencyObject root)
        {
            var list = FindVisualChildByName<ListView>(root, "IconCatalogList");
            if (list == null)
            {
                throw new InvalidOperationException("The Iconography page did not create IconCatalogList.");
            }

            list.ApplyTemplate();
            list.UpdateLayout();
            if (list.Items.Count == 0)
            {
                throw new InvalidOperationException("The Iconography page did not load any icon rows.");
            }

            list.ScrollIntoView(list.Items[0]);
            list.UpdateLayout();

            var firstContainer = list.ItemContainerGenerator.ContainerFromIndex(0) as FrameworkElement;
            if (firstContainer == null)
            {
                throw new InvalidOperationException("The Iconography page did not realize the first icon row.");
            }

            firstContainer.ApplyTemplate();
            firstContainer.UpdateLayout();

            var lastIndex = list.Items.Count - 1;
            list.ScrollIntoView(list.Items[lastIndex]);
            list.UpdateLayout();
            DrainDispatcher(list.Dispatcher);

            var lastContainer = list.ItemContainerGenerator.ContainerFromIndex(lastIndex) as FrameworkElement;
            if (lastContainer == null)
            {
                throw new InvalidOperationException("The Iconography page did not realize the final icon row after scrolling.");
            }

            lastContainer.ApplyTemplate();
            lastContainer.UpdateLayout();
        }

        private static T FindVisualChildByName<T>(DependencyObject root, string name)
            where T : FrameworkElement
        {
            if (root == null)
            {
                return null;
            }

            var rootElement = root as T;
            if (rootElement != null && string.Equals(rootElement.Name, name, StringComparison.Ordinal))
            {
                return rootElement;
            }

            var childCount = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < childCount; i++)
            {
                var match = FindVisualChildByName<T>(VisualTreeHelper.GetChild(root, i), name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static void DrainDispatcher(Dispatcher dispatcher)
        {
            dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));
        }

        private static bool IsSmokeTest(string[] args)
        {
            if (args == null)
            {
                return false;
            }

            for (var i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], SmokeTestArgument, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
