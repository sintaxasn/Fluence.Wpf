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
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Xunit;

namespace Fluence.Wpf.Tests.Infrastructure
{
    /// <summary>
    /// The single application and theme reset for the suite. Every test class that touches
    /// <see cref="Application"/>, application resources, or a <see cref="Window"/> starts from
    /// one of these two entry points, so no test inherits state from the test before it.
    /// </summary>
    internal static class TestApp
    {
        private static readonly Uri DemoSharedStylesUri = new(
            "/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml",
            UriKind.Relative);

        /// <summary>
        /// Resets the application and applies a library theme. The demo resource dictionary is
        /// deliberately not merged: a library control test asserts the library's own template and
        /// brushes, and a demo style merged on top can shadow a library brush.
        /// </summary>
        /// <param name="theme">The theme to apply after the reset.</param>
        /// <param name="backdrop">The backdrop to apply with it.</param>
        internal static Application EnsureLibraryTheme(
            ApplicationTheme theme = ApplicationTheme.Light,
            BackdropType backdrop = BackdropType.None)
        {
            Application application = WpfTestSta.EnsureApplication();
            Reset(application);
            ApplicationThemeManager.Apply(theme, backdrop, updateAccent: true);
            return application;
        }

        /// <summary>
        /// Resets the application, applies the Light theme and the system accent, then merges the
        /// demo shared styles. This is the explicit opt-in for tests whose subject is the demo
        /// gallery. A library test that calls it must say at its own call site which demo style it
        /// depends on.
        /// </summary>
        /// <param name="backdrop">The backdrop to apply with the Light theme.</param>
        internal static Application EnsureDemoTheme(BackdropType backdrop = BackdropType.None)
        {
            Application application = EnsureLibraryTheme(ApplicationTheme.Light, backdrop);
            ApplicationAccentColorManager.ApplySystemAccent();
            application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = DemoSharedStylesUri });
            return application;
        }

        /// <summary>
        /// Returns the merged Themes/Generic.xaml dictionary, slot [2] of the three-slot
        /// layout described in AGENTS.md section 3, and asserts that the layout is intact. A demo
        /// application has a fourth slot on top; slot [2] is still Generic.
        /// </summary>
        /// <param name="application">The application whose merged dictionaries to read.</param>
        internal static ResourceDictionary GenericDictionary(Application application)
        {
            Collection<ResourceDictionary> dictionaries = application.Resources.MergedDictionaries;
            Assert.True(
                dictionaries.Count >= 3,
                $"Expected at least the three theme slots, found {dictionaries.Count}.");
            return dictionaries[2];
        }

        private static void Reset(Application application)
        {
            Keyboard.ClearFocus();

            foreach (Window window in (Window[])[.. application.Windows.Cast<Window>()])
            {
                window.Content = null;
                window.Close();
            }

            // A single ApplicationIdle drain subsumes the higher Loaded and ContextIdle
            // priorities: Invoke blocks until the queue has been processed down to and
            // including the requested priority.
            WpfTestSta.DrainDispatcher(Dispatcher.CurrentDispatcher);

            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application.Resources.MergedDictionaries.Clear();
            application.Resources.Clear();
        }
    }
}
