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
using System.Windows.Interop;
using Fluence.Wpf.Native;

namespace Fluence.Wpf
{
    // Fluence.Wpf - SystemThemeWatcher
    // Hooks Win32 settings changes so theme/accent updates propagate while the window is open.

    /// <summary>
    /// Subscribes a <see cref="Window"/> to high-priority settings change notifications (theme, accent) with debouncing.
    /// </summary>
    /// <remarks>
    /// Used with <see cref="ApplicationThemeManager"/> to refresh resources when the user changes Windows light/dark or contrast while the app runs.
    /// </remarks>
    public static class SystemThemeWatcher
    {
        private static readonly List<WatchedWindow> _watchedWindows = [];
        private static readonly object _lock = new();
        private static long _lastUpdateTick;
        private const long DebounceIntervalTicks = 1_000_000;

        /// <summary>
        /// Begins watching the specified window for system theme and accent changes.
        /// </summary>
        /// <param name="window">The WPF window to associate with the watcher. Must not be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="window"/> is <c>null</c>.</exception>
        public static void Watch(Window window)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(window);
#else
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }
#endif

            lock (_lock)
            {
                if (FindWatchedWindow(window) != null)
                {
                    return;
                }

                WatchedWindow watched = new(window);
                _watchedWindows.Add(watched);

                if (window.IsLoaded)
                {
                    AttachHook(watched);
                }
                else
                {
                    // HwndSource does not exist until SourceInitialized. Defer the native
                    // hook rather than forcing handle creation during construction.
                    window.SourceInitialized += OnWindowSourceInitialized;
                }
            }
        }

        /// <summary>
        /// Stops watching the specified window and removes Win32 hooks.
        /// </summary>
        /// <param name="window">The window previously passed to <see cref="Watch"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="window"/> is <c>null</c>.</exception>
        public static void UnWatch(Window window)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(window);
#else
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }
#endif

            lock (_lock)
            {
                if (FindWatchedWindow(window) is not WatchedWindow watched)
                {
                    return;
                }

                DetachHook(watched);
                _watchedWindows.Remove(watched);

                window.SourceInitialized -= OnWindowSourceInitialized;
            }
        }

        private static void OnWindowSourceInitialized(object? sender, EventArgs e)
        {
            if (sender is not Window window)
            {
                return;
            }

            window.SourceInitialized -= OnWindowSourceInitialized;

            lock (_lock)
            {
                if (FindWatchedWindow(window) is WatchedWindow watched)
                {
                    AttachHook(watched);
                }
            }
        }

        private static void AttachHook(WatchedWindow watched)
        {
            if (watched.IsHooked)
            {
                return;
            }

            WindowInteropHelper helper = new(watched.Window);
            IntPtr handle = helper.Handle;

            if (handle == IntPtr.Zero)
            {
                return;
            }

            HwndSource source = HwndSource.FromHwnd(handle);
            if (source == null)
            {
                return;
            }

            watched.HwndSource = source;
            source.AddHook(WndProc);
            watched.IsHooked = true;
        }

        private static void DetachHook(WatchedWindow watched)
        {
            if (!watched.IsHooked || watched.HwndSource == null)
            {
                return;
            }

            watched.HwndSource.RemoveHook(WndProc);
            watched.IsHooked = false;
            watched.HwndSource = null;
        }

        private static WatchedWindow? FindWatchedWindow(Window window)
        {
            for (int i = 0; i < _watchedWindows.Count; i++)
            {
                if (ReferenceEquals(_watchedWindows[i].Window, window))
                {
                    return _watchedWindows[i];
                }
            }
            return null;
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg is NativeConstants.WM_SETTINGCHANGE or
                NativeConstants.WM_DWMCOLORIZATIONCOLORCHANGED or
                NativeConstants.WM_THEMECHANGED or
                NativeConstants.WM_SYSCOLORCHANGE)
            {
                long currentTick = DateTime.UtcNow.Ticks;
                if (currentTick - _lastUpdateTick > DebounceIntervalTicks)
                {
                    _lastUpdateTick = currentTick;
                    OnSystemThemeChanged();
                }
            }

            return IntPtr.Zero;
        }

        private static void OnSystemThemeChanged()
        {
            if (Application.Current == null)
            {
                return;
            }

            _ = Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                // Settings messages arrive on the HWND hook path. Re-enter through the
                // Dispatcher so ResourceDictionary mutation stays on the WPF UI thread.
                if (ApplicationThemeManager.CurrentTheme == ApplicationTheme.Auto)
                {
                    ApplicationThemeManager.ApplySystemTheme();
                }
                else
                {
                    ApplicationAccentColorManager.RefreshAccent();
                }
            }));
        }

        private class WatchedWindow(Window window)
        {
            public Window Window { get; private set; } = window;
            public HwndSource? HwndSource { get; set; }
            public bool IsHooked { get; set; }
        }
    }
}
