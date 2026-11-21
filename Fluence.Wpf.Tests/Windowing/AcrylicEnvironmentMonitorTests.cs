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
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Threading;
using Fluence.Wpf.Helpers;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Windowing
{
    public sealed class AcrylicEnvironmentMonitorTests
    {
        [Fact]
        public void MalformedPowerPayloads_AreRejectedWithoutReadingVariableData()
        {
            Guid right = AcrylicEnvironmentMonitor.PowerSavingStatusGuid;
            Assert.False(AcrylicEnvironmentMonitor.TryParsePowerSetting(IntPtr.Zero, out _));
            Assert.False(AcrylicEnvironmentMonitor.TryDecodePowerSetting(Guid.Empty, 4, 1, out _));
            Assert.False(AcrylicEnvironmentMonitor.TryDecodePowerSetting(right, 0, 1, out _));
            Assert.False(AcrylicEnvironmentMonitor.TryDecodePowerSetting(right, 3, 1, out _));
            Assert.False(AcrylicEnvironmentMonitor.TryDecodePowerSetting(right, 5, 1, out _));
            Assert.False(AcrylicEnvironmentMonitor.TryDecodePowerSetting(right, 4, 2, out _));
            Assert.True(AcrylicEnvironmentMonitor.TryDecodePowerSetting(right, 4, 0, out bool off));
            Assert.False(off);
            Assert.True(AcrylicEnvironmentMonitor.TryDecodePowerSetting(right, 4, 1, out bool on));
            Assert.True(on);
            IntPtr buffer = CreatePayload(right, 4, 1);
            try
            {
                Assert.True(AcrylicEnvironmentMonitor.TryParsePowerSetting(buffer, out bool parsed));
                Assert.True(parsed);
                Marshal.WriteInt32(buffer, 16, 5);
                Assert.False(AcrylicEnvironmentMonitor.TryParsePowerSetting(buffer, out _));
                Marshal.StructureToPtr(Guid.Empty, buffer, fDeleteOld: false);
                Assert.False(AcrylicEnvironmentMonitor.TryParsePowerSetting(buffer, out _));
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        [Fact]
        public Task InitialQuery_OneRegistrationForTwoSubscribers_AndLastDetachOnceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                using HwndSource source = CreateSource();
                FakePowerNative native = new() { Next = false };
                int firstCalls = 0;
                int secondCalls = 0;
                using AcrylicEnvironmentMonitor.Subscription first =
                    AcrylicEnvironmentMonitor.Subscribe(source, () =>
                    {
                        Assert.True(source.Dispatcher.CheckAccess());
                        firstCalls++;
                    }, native);
                using AcrylicEnvironmentMonitor.Subscription second =
                    AcrylicEnvironmentMonitor.Subscribe(source, () => secondCalls++, native);
                Assert.Equal(1, native.RegisterCount);
                Assert.Equal(1, native.QueryCount);
                Assert.False(first.CreateSnapshot().PowerSaving);
                Assert.False(second.CreateSnapshot().PowerSaving);

                IntPtr on = CreatePayload(AcrylicEnvironmentMonitor.PowerSavingStatusGuid, 4, 1);
                IntPtr off = CreatePayload(AcrylicEnvironmentMonitor.PowerSavingStatusGuid, 4, 0);
                IntPtr wrongGuid = CreatePayload(Guid.Empty, 4, 1);
                try
                {
                    AcrylicEnvironmentMonitor.DispatchMessageForTesting(source, 0x218, EventCode(0x8013), wrongGuid);
                    AcrylicEnvironmentMonitor.DispatchMessageForTesting(source, 0x218, EventCode(0x8013), IntPtr.Zero);
                    WpfTestSta.DrainDispatcher(source.Dispatcher);
                    Assert.Equal(0, firstCalls);
                    Assert.Equal(0, secondCalls);

                    AcrylicEnvironmentMonitor.DispatchMessageForTesting(source, 0x218, EventCode(0x8013), on);
                    WpfTestSta.DrainDispatcher(source.Dispatcher);
                    Assert.True(first.CreateSnapshot().PowerSaving);
                    Assert.Equal(1, firstCalls);
                    Assert.Equal(1, secondCalls);

                    AcrylicEnvironmentMonitor.DispatchMessageForTesting(source, 0x218, EventCode(0x8013), on);
                    WpfTestSta.DrainDispatcher(source.Dispatcher);
                    Assert.Equal(1, firstCalls);
                    Assert.Equal(1, secondCalls);

                    first.Dispose();
                    Assert.Equal(0, native.UnregisterCount);
                    AcrylicEnvironmentMonitor.DispatchMessageForTesting(source, 0x218, EventCode(0x8013), off);
                    WpfTestSta.DrainDispatcher(source.Dispatcher);
                    Assert.Equal(1, firstCalls);
                    Assert.Equal(2, secondCalls);
                    second.Dispose();
                    Assert.Equal(1, native.UnregisterCount);
                    second.Dispose();
                    Assert.Equal(1, native.UnregisterCount);
                }
                finally
                {
                    Marshal.FreeHGlobal(on);
                    Marshal.FreeHGlobal(off);
                    Marshal.FreeHGlobal(wrongGuid);
                }
            });
        }

        [Fact]
        public Task SeparateHwndSources_KeepIndependentPowerStateAndRegistrationsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                using HwndSource firstSource = CreateSource();
                using HwndSource secondSource = CreateSource();
                FakePowerNative native = new() { Next = false };
                using AcrylicEnvironmentMonitor.Subscription first =
                    AcrylicEnvironmentMonitor.Subscribe(firstSource, static () => { }, native);
                using AcrylicEnvironmentMonitor.Subscription second =
                    AcrylicEnvironmentMonitor.Subscribe(secondSource, static () => { }, native);
                Assert.Equal(2, native.RegisterCount);
                IntPtr on = CreatePayload(AcrylicEnvironmentMonitor.PowerSavingStatusGuid, 4, 1);
                try
                {
                    AcrylicEnvironmentMonitor.DispatchMessageForTesting(firstSource, 0x218, EventCode(0x8013), on);
                    WpfTestSta.DrainDispatcher(firstSource.Dispatcher);
                    Assert.True(first.CreateSnapshot().PowerSaving);
                    Assert.False(second.CreateSnapshot().PowerSaving);
                }
                finally
                {
                    Marshal.FreeHGlobal(on);
                }

                first.Dispose();
                Assert.Equal(1, native.UnregisterCount);
                second.Dispose();
                Assert.Equal(2, native.UnregisterCount);
            });
        }

        [Fact]
        public Task FailedQueryResumeAndClose_StayConservative_AndDropPendingDeliveryAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                using HwndSource source = CreateSource();
                FakePowerNative native = new() { Next = null };
                int callbacks = 0;
                AcrylicEnvironmentMonitor.Subscription subscription =
                    AcrylicEnvironmentMonitor.Subscribe(source, () => callbacks++, native);
                Assert.Null(subscription.CreateSnapshot().PowerSaving);

                native.Next = false;
                AcrylicEnvironmentMonitor.DispatchMessageForTesting(source, 0x218, EventCode(0x12), IntPtr.Zero);
                WpfTestSta.DrainDispatcher(source.Dispatcher);
                Assert.False(subscription.CreateSnapshot().PowerSaving);
                Assert.Equal(1, callbacks);

                native.Next = null;
                AcrylicEnvironmentMonitor.DispatchMessageForTesting(source, 0x218, EventCode(0x07), IntPtr.Zero);
                Assert.Null(subscription.CreateSnapshot().PowerSaving);
                source.Dispose();
                WpfTestSta.DrainDispatcher(source.Dispatcher);
                Assert.Equal(1, callbacks);
                Assert.Equal(1, native.UnregisterCount);
                subscription.Dispose();
                Assert.Equal(1, native.UnregisterCount);
            });
        }

        [Fact]
        public Task RegistrationFailure_RetriesOnLaterAttachAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                using HwndSource source = CreateSource();
                FakePowerNative native = new() { RegistrationFails = true, Next = false };
                using AcrylicEnvironmentMonitor.Subscription first =
                    AcrylicEnvironmentMonitor.Subscribe(source, static () => { }, native);
                Assert.Null(first.CreateSnapshot().PowerSaving);
                Assert.Equal(1, native.RegisterCount);
                Assert.Equal(0, native.QueryCount);
                native.RegistrationFails = false;
                using AcrylicEnvironmentMonitor.Subscription second =
                    AcrylicEnvironmentMonitor.Subscribe(source, static () => { }, native);
                Assert.False(second.CreateSnapshot().PowerSaving);
                Assert.Equal(2, native.RegisterCount);
                Assert.Equal(1, native.QueryCount);
            });
        }

        [Fact]
        public Task IdleGraphicsModeFlip_NotifiesOnlyOnChangeAndRecoversSameHwndAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                using HwndSource source = CreateSource();
                FakePowerNative native = new() { Next = false };
                int callbacks = 0;
                using AcrylicEnvironmentMonitor.Subscription subscription =
                    AcrylicEnvironmentMonitor.Subscribe(source, () => callbacks++, native);
                Assert.True(subscription.CreateSnapshot().GraphicsSupported);
                HwndTarget target = Assert.IsType<HwndTarget>(source.CompositionTarget);
                RenderMode initialMode = target.RenderMode;
                try
                {
                    target.RenderMode = RenderMode.SoftwareOnly;
                    WaitForCallbacks(source.Dispatcher, () => callbacks is 1);
                    Assert.False(subscription.CreateSnapshot().GraphicsSupported);
                    Assert.Equal(1, callbacks);
                }
                finally
                {
                    target.RenderMode = initialMode;
                }

                WaitForCallbacks(source.Dispatcher, () => callbacks is 2);
                Assert.True(subscription.CreateSnapshot().GraphicsSupported);
                Assert.Equal(2, callbacks);
                subscription.Dispose();
                Assert.Equal(1, native.UnregisterCount);
            });
        }

        [Fact]
        public Task PartialStartQueryFailure_UnregistersAndAllowsFreshSubscribeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                using HwndSource source = CreateSource();
                FakePowerNative failing = new() { ThrowOnQuery = true };
                _ = Assert.Throws<InvalidOperationException>(() =>
                    AcrylicEnvironmentMonitor.Subscribe(source, static () => { }, failing));
                Assert.Equal(1, failing.RegisterCount);
                Assert.Equal(1, failing.UnregisterCount);
                FakePowerNative healthy = new() { Next = false };
                using AcrylicEnvironmentMonitor.Subscription recovered =
                    AcrylicEnvironmentMonitor.Subscribe(source, static () => { }, healthy);
                Assert.False(recovered.CreateSnapshot().PowerSaving);
                Assert.Equal(1, healthy.RegisterCount);
                recovered.Dispose();
                Assert.Equal(1, healthy.UnregisterCount);
            });
        }

        private static void WaitForCallbacks(Dispatcher dispatcher, Func<bool> condition)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(2);
            while (!condition() && DateTime.UtcNow < deadline)
            {
                DispatcherFrame frame = new();
                DispatcherTimer timer = new(TimeSpan.FromMilliseconds(20), DispatcherPriority.Background,
                    (_, _) => frame.Continue = false, dispatcher);
                timer.Start();
                Dispatcher.PushFrame(frame);
                timer.Stop();
            }

            Assert.True(condition());
        }

        private static HwndSource CreateSource()
        {
            HwndSourceParameters parameters = new("AcrylicEnvironmentMonitorTests")
            {
                Width = 16,
                Height = 16,
            };
            return new HwndSource(parameters);
        }

        private static IntPtr CreatePayload(Guid guid, int length, int value)
        {
            IntPtr memory = Marshal.AllocHGlobal(24);
            Marshal.StructureToPtr(guid, memory, fDeleteOld: false);
            Marshal.WriteInt32(memory, 16, length);
            Marshal.WriteInt32(memory, 20, value);
            return memory;
        }

        private static IntPtr EventCode(int value)
        {
            return new(value);
        }

        private sealed class FakePowerNative : IAcrylicPowerNative
        {
            internal bool? Next { get; set; }
            internal bool RegistrationFails { get; set; }
            internal bool ThrowOnQuery { get; set; }
            internal int RegisterCount { get; private set; }
            internal int QueryCount { get; private set; }
            internal int UnregisterCount { get; private set; }

            public bool TryGetPowerSaving(out bool enabled)
            {
                QueryCount++;
                if (ThrowOnQuery)
                {
                    throw new InvalidOperationException("Injected power query failure.");
                }
                bool? next = Next;
                enabled = next ?? false;
                return next is not null;
            }

            public IDisposable? RegisterPowerSaving(IntPtr hwnd)
            {
                Assert.NotEqual(IntPtr.Zero, hwnd);
                RegisterCount++;
                return RegistrationFails ? null : new FakeRegistration(this);
            }

            public void UnregisterPowerSaving(IDisposable registration)
            {
                registration.Dispose();
            }

            private sealed class FakeRegistration : IDisposable
            {
                private readonly FakePowerNative _owner;
                private bool _disposed;

                internal FakeRegistration(FakePowerNative owner)
                {
                    _owner = owner;
                }

                public void Dispose()
                {
                    if (_disposed)
                    {
                        return;
                    }

                    _disposed = true;
                    _owner.UnregisterCount++;
                }
            }
        }
    }
}
