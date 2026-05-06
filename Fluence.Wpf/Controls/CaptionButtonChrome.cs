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

namespace Fluence.Wpf.Controls
{
    internal static class CaptionButtonChrome
    {
        internal static void GetMinimizeChrome(ResizeMode resizeMode, out Visibility visibility, out bool isEnabled)
        {
            isEnabled = resizeMode != ResizeMode.NoResize;
            visibility = isEnabled ? Visibility.Visible : Visibility.Collapsed;
        }

        internal static void GetCloseChrome(out Visibility visibility, out bool isEnabled)
        {
            visibility = Visibility.Visible;
            isEnabled = true;
        }

        internal static void GetMaximizeRestoreChrome(
            ResizeMode resizeMode,
            WindowState windowState,
            out Visibility maximizeVisibility,
            out Visibility restoreVisibility,
            out bool maximizeEnabled,
            out bool restoreEnabled)
        {
            GetMaximizeRestoreLayout(resizeMode, windowState, out maximizeVisibility, out restoreVisibility);
            if (resizeMode is not ResizeMode.NoResize and not ResizeMode.CanMinimize)
            {
                maximizeEnabled = maximizeVisibility == Visibility.Visible;
                restoreEnabled = restoreVisibility == Visibility.Visible;
            }
            else
            {
                maximizeEnabled = false;
                restoreEnabled = false;
            }
        }

        private static void GetMaximizeRestoreLayout(
            ResizeMode resizeMode,
            WindowState windowState,
            out Visibility maximizeVisibility,
            out Visibility restoreVisibility)
        {
            if (resizeMode == ResizeMode.NoResize)
            {
                maximizeVisibility = Visibility.Collapsed;
                restoreVisibility = Visibility.Collapsed;
                return;
            }
            if (windowState == WindowState.Maximized)
            {
                maximizeVisibility = Visibility.Collapsed;
                restoreVisibility = Visibility.Visible;
            }
            else
            {
                maximizeVisibility = Visibility.Visible;
                restoreVisibility = Visibility.Collapsed;
            }
        }
    }
}
