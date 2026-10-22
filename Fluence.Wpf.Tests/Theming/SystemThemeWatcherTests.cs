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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Theming
{
    public sealed class SystemThemeWatcherTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () =>
            {
                SystemThemeWatcher.ResetForTesting();
                _ = TestApp.EnsureLibraryTheme();
            }));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () =>
            {
                SystemThemeWatcher.ResetForTesting();
                WpfTestSta.DrainDispatcher(Dispatcher.CurrentDispatcher);
            }));
        }

        [Fact]
        public Task RequestRefresh_BurstBeforeDrain_QueuesOneRefreshAsync()
        {
            return ObserveRefreshesAsync(static operations =>
            {
                SystemThemeWatcher.RequestRefresh();
                SystemThemeWatcher.RequestRefresh();
                _ = Assert.Single(operations);
                WpfTestSta.DrainDispatcher(Dispatcher.CurrentDispatcher);
                Assert.Equal(DispatcherOperationStatus.Completed, operations[0].Status);
            });
        }

        [Fact]
        public Task RequestRefresh_AfterDrain_QueuesAnotherRefreshAsync()
        {
            return ObserveRefreshesAsync(static operations =>
            {
                SystemThemeWatcher.RequestRefresh();
                WpfTestSta.DrainDispatcher(Dispatcher.CurrentDispatcher);
                SystemThemeWatcher.RequestRefresh();
                WpfTestSta.DrainDispatcher(Dispatcher.CurrentDispatcher);
                Assert.Equal(2, operations.Count);
                Assert.All(operations, static operation => Assert.Equal(DispatcherOperationStatus.Completed, operation.Status));
            });
        }

        [Fact]
        public Task RequestRefresh_AfterAbortedOperation_CanQueueAgainAsync()
        {
            return ObserveRefreshesAsync(static operations =>
            {
                SystemThemeWatcher.RequestRefresh();
                Assert.True(Assert.Single(operations).Abort());
                SystemThemeWatcher.RequestRefresh();
                WpfTestSta.DrainDispatcher(Dispatcher.CurrentDispatcher);
                Assert.Equal(2, operations.Count);
                Assert.Equal(DispatcherOperationStatus.Aborted, operations[0].Status);
                Assert.Equal(DispatcherOperationStatus.Completed, operations[1].Status);
            });
        }

        [Theory]
        [InlineData(0x031A, null, 1)]
        [InlineData(0x0015, null, 1)]
        [InlineData(0x0320, null, 1)]
        [InlineData(0x001A, "iMmErSiVeCoLoRsEt", 1)]
        [InlineData(0x001A, "UnrelatedSetting", 0)]
        public Task WndProc_RelevantMessagesReachRefreshQueue_UnrelatedSettingDoesNotAsync(int message, string? setting, int expectedCount)
        {
            return ObserveRefreshesAsync(operations =>
            {
                IntPtr pointer = setting is null ? IntPtr.Zero : Marshal.StringToHGlobalUni(setting);
                try
                {
                    MethodInfo method = Assert.IsType<MethodInfo>(
                        typeof(SystemThemeWatcher).GetMethod("WndProc", BindingFlags.NonPublic | BindingFlags.Static),
                        exactMatch: false);
                    _ = method.Invoke(null, [IntPtr.Zero, message, IntPtr.Zero, pointer, false]);
                    Assert.Equal(expectedCount, operations.Count);
                    WpfTestSta.DrainDispatcher(Dispatcher.CurrentDispatcher);
                    if (expectedCount > 0)
                    {
                        _ = method.Invoke(null, [IntPtr.Zero, message, IntPtr.Zero, pointer, false]);
                        WpfTestSta.DrainDispatcher(Dispatcher.CurrentDispatcher);
                        Assert.Equal(2, operations.Count);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(pointer);
                }
            });
        }

        private static Task ObserveRefreshesAsync(Action<List<DispatcherOperation>> scenario)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Dispatcher dispatcher = Application.Current.Dispatcher;
                List<DispatcherOperation> operations = [];
                void OnPosted(object? sender, DispatcherHookEventArgs args)
                {
                    if (args.Operation.Priority is DispatcherPriority.Normal)
                    {
                        operations.Add(args.Operation);
                    }
                }

                dispatcher.Hooks.OperationPosted += OnPosted;
                try
                {
                    scenario(operations);
                }
                finally
                {
                    dispatcher.Hooks.OperationPosted -= OnPosted;
                    SystemThemeWatcher.ResetForTesting();
                    WpfTestSta.DrainDispatcher(dispatcher);
                }
            });
        }
    }
}
