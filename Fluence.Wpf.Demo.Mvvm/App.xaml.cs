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
using System.Windows;
using System.Windows.Media;
using Fluence.Wpf;

namespace Fluence.Wpf.Demo.Mvvm
{
    public partial class App : Application
    {
        static App()
        {
            // Inherit on-screen ClearType text rendering from root Window downward.
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

            // Apply Fluent theme + system accent. Must run before any Window is shown.
            ApplicationThemeManager.Apply(ApplicationTheme.Auto, BackdropType.Auto, updateAccent: true);
            ApplicationAccentColorManager.ApplySystemAccent();

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}
