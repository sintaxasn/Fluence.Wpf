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
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Shared single-threaded STA dispatcher for WPF tests so <see cref="Application.Current"/>
    /// and all windows share one dispatcher (avoids cross-thread DynamicResource failures).
    /// </summary>
    internal static class WpfTestSta
    {
        private static Dispatcher? _dispatcher;
        private static readonly object LockObj = new();

        internal static Dispatcher? Dispatcher => EnsureDispatcher();

        internal static Application? EnsureApplication()
        {
            return Invoke(static () =>
            {
                if (Application.Current is null)
                {
                    Application app = new()
                    {
                        ShutdownMode = ShutdownMode.OnExplicitShutdown
                    };
                }

                return Application.Current;
            });
        }

        internal static void Invoke(Action action)
        {
            EnsureDispatcher()?.Invoke(action);
        }

        internal static T? Invoke<T>(Func<T> func) where T : class?
        {
            return EnsureDispatcher()?.Invoke(func);
        }

        private static Dispatcher? EnsureDispatcher()
        {
            lock (LockObj)
            {
                if (_dispatcher is not null && _dispatcher.Thread.IsAlive)
                {
                    return _dispatcher;
                }

                Dispatcher? created = null;
                using ManualResetEventSlim ready = new(false);
                Thread thread = new(() =>
                {
                    created = Dispatcher.CurrentDispatcher;
                    ready.Set();
                    Dispatcher.Run();
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Start();
                ready.Wait();
                _dispatcher = created;
                return _dispatcher;
            }
        }
    }
}
