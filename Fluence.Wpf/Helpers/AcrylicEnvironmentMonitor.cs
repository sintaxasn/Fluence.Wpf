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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Fluence.Wpf.Native;
using Fluence.Wpf.Theming;
using Windows.Win32;

namespace Fluence.Wpf.Helpers
{
    internal static class AcrylicEnvironmentMonitor
    {
        internal static readonly Guid PowerSavingStatusGuid = new(
            unchecked((int)0xE00958C0), unchecked((short)0xC213), 0x4ACE,
            0xAC, 0x77, 0xFE, 0xCC, 0xED, 0x2E, 0xEE, 0xA5);
        private const int WmNcDestroy = 0x0082;
        private static readonly ConcurrentDictionary<HwndSource, MonitorEntry> Entries = new();

        internal static Subscription Subscribe(HwndSource source, Action changed)
        {
            ArgumentNullException.ThrowIfNull(source);
            source.Dispatcher.VerifyAccess();
            return Subscribe(source, changed, Win32PowerNative.Instance);
        }

        internal static Subscription Subscribe(HwndSource source, Action changed, IAcrylicPowerNative native)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(changed);
            ArgumentNullException.ThrowIfNull(native);
            source.Dispatcher.VerifyAccess();
            if (source.Handle == IntPtr.Zero)
            {
                throw new ArgumentException("Acrylic requires a realized HWND.", nameof(source));
            }

            MonitorEntry entry = Entries.GetOrAdd(source, static (key, argument) => new MonitorEntry(key, argument), native);
            return entry.Attach(changed);
        }

        internal static bool TryDecodePowerSetting(Guid guid, uint length, int value, out bool enabled)
        {
            enabled = false;
            if (guid != PowerSavingStatusGuid || length is not 4 || value is not (0 or 1))
            {
                return false;
            }

            enabled = value is 1;
            return true;
        }

        internal static bool TryParsePowerSetting(IntPtr payload, out bool enabled)
        {
            enabled = false;
            if (payload == IntPtr.Zero)
            {
                return false;
            }

            // The OS contract supplies POWERBROADCAST_SETTING. Its GUID occupies bytes 0-15,
            // DataLength bytes 16-19, and the four-byte DWORD payload starts at byte 20.
            // No independent allocation length exists; this is not a parser for arbitrary pointers.
            Guid guid = Marshal.PtrToStructure<Guid>(payload);
            if (guid != PowerSavingStatusGuid)
            {
                return false;
            }

            int length = Marshal.ReadInt32(payload, 16);
            return length is 4
                && TryDecodePowerSetting(guid, (uint)length, Marshal.ReadInt32(payload, 20), out enabled);
        }

        internal static void DispatchMessageForTesting(HwndSource source, int message, IntPtr wParam, IntPtr lParam)
        {
            source.Dispatcher.VerifyAccess();
            if (Entries.TryGetValue(source, out MonitorEntry? entry))
            {
                bool handled = false;
                _ = entry.WndProc(source.Handle, message, wParam, lParam, ref handled);
            }
        }

        internal sealed class Subscription : IDisposable
        {
            private MonitorEntry? Entry { get; set; }

            internal Subscription(MonitorEntry entry, Action changed)
            {
                Entry = entry;
                Changed = changed;
            }

            internal bool IsDisposed { get; private set; }
            internal Action Changed { get; }

            internal AcrylicPolicySnapshot CreateSnapshot()
            {
                MonitorEntry entry = Entry ?? throw new ObjectDisposedException(nameof(Subscription));
                entry.Source.Dispatcher.VerifyAccess();
                return entry.CreateSnapshot();
            }

            public void Dispose()
            {
                MonitorEntry? entry = Entry;
                if (entry is null)
                {
                    return;
                }

                entry.Source.Dispatcher.VerifyAccess();
                Entry = null;
                IsDisposed = true;
                entry.Detach(this);
            }

            internal void Invalidate()
            {
                Entry = null;
                IsDisposed = true;
            }
        }

        internal sealed class MonitorEntry
        {
            private readonly IAcrylicPowerNative _native;
            private readonly List<Subscription> _subscriptions = [];
            private Window? _window;
            private IDisposable? _registration;
            private AcrylicPolicySnapshot? _lastEnvironment;
            private Color _lastWindowColor;
            private bool? _powerSaving;
            private bool _closed;
            private bool _deliveryQueued;
            private int _generation;
            private bool _hookAdded;
            private DispatcherTimer? _graphicsTimer;

            internal MonitorEntry(HwndSource source, IAcrylicPowerNative native)
            {
                Source = source;
                _native = native;
            }

            internal HwndSource Source { get; }

            internal Subscription Attach(Action changed)
            {
                Source.Dispatcher.VerifyAccess();
                ObjectDisposedException.ThrowIf(_closed, typeof(AcrylicEnvironmentMonitor));

                Subscription subscription = new(this, changed);
                _subscriptions.Add(subscription);
                if (_subscriptions.Count is 1)
                {
                    Start();
                }
                else if (_registration is null)
                {
                    RefreshPower();
                }

                return subscription;
            }

            private void Start()
            {
                try
                {
                    Source.AddHook(WndProc);
                    _hookAdded = true;
                    _window = Source.RootVisual is DependencyObject root ? Window.GetWindow(root) : null;
                    if (_window is Window window)
                    {
                        window.Closed += OnWindowClosed;
                    }

                    FluenceThemeEngine.Published += OnEnvironmentChanged;
                    ApplicationThemeManager.Changed += OnThemeChanged;
                    SystemParameters.StaticPropertyChanged += OnStaticPropertyChanged;
                    RenderCapability.TierChanged += OnEnvironmentChanged;

                    RefreshPower(publish: false);
                    _lastEnvironment = ReadEnvironment();
                    _lastWindowColor = SystemColors.WindowColor;
                    _graphicsTimer = new DispatcherTimer(DispatcherPriority.Background, Source.Dispatcher)
                    {
                        Interval = TimeSpan.FromMilliseconds(250),
                    };
                    _graphicsTimer.Tick += OnGraphicsTimer;
                    _graphicsTimer.Start();
                }
                catch
                {
                    Close();
                    throw;
                }
            }

            internal void Detach(Subscription subscription)
            {
                Source.Dispatcher.VerifyAccess();
                _ = _subscriptions.Remove(subscription);
                if (_subscriptions.Count is 0)
                {
                    Close();
                }
            }

            internal AcrylicPolicySnapshot CreateSnapshot()
            {
                Source.Dispatcher.VerifyAccess();
                return ReadEnvironment();
            }

            private AcrylicPolicySnapshot ReadEnvironment()
            {
                return new AcrylicPolicySnapshot
                {
                    OsHighContrast = SystemParameters.HighContrast,
                    AppHighContrast = ApplicationThemeManager.ResolvedTheme is ApplicationTheme.HighContrast,
                    TransparencyEnabled = RegistryHelper.GetEnableTransparency(),
                    PowerSaving = _powerSaving,
                    GraphicsSupported = ReadGraphicsSupported(),
                };
            }

            private bool ReadGraphicsSupported()
            {
                return (RenderCapability.Tier >> 16) > 0
                    && RenderOptions.ProcessRenderMode is not RenderMode.SoftwareOnly
                    && Source.CompositionTarget is HwndTarget { RenderMode: not RenderMode.SoftwareOnly }
                    && RenderCapability.IsPixelShaderVersionSupported(3, 0);
            }

            private void OnGraphicsTimer(object? sender, EventArgs e)
            {
                _ = sender;
                _ = e;
                if (!_closed && _lastEnvironment?.GraphicsSupported != ReadGraphicsSupported())
                {
                    RefreshEnvironment();
                }
            }

            private void RefreshPower(bool publish = true)
            {
                if (_closed)
                {
                    return;
                }

                _registration ??= _native.RegisterPowerSaving(Source.Handle);

                bool? next = _registration is not null && _native.TryGetPowerSaving(out bool saving)
                    ? saving
                    : null;
                SetPower(next, publish);
            }

            private void SetPower(bool? next, bool publish = true)
            {
                if (_powerSaving == next)
                {
                    return;
                }

                _powerSaving = next;
                if (publish)
                {
                    QueueDelivery();
                }
            }

            private void RefreshEnvironment()
            {
                if (_closed)
                {
                    return;
                }

                AcrylicPolicySnapshot next = ReadEnvironment();
                Color windowColor = SystemColors.WindowColor;
                AcrylicPolicySnapshot? previous = _lastEnvironment;
                bool changed = previous is null
                    || previous.OsHighContrast != next.OsHighContrast
                    || previous.AppHighContrast != next.AppHighContrast
                    || previous.TransparencyEnabled != next.TransparencyEnabled
                    || previous.GraphicsSupported != next.GraphicsSupported
                    || _lastWindowColor != windowColor;
                _lastEnvironment = next;
                _lastWindowColor = windowColor;
                if (changed)
                {
                    QueueDelivery();
                }
            }

            private void QueueDelivery()
            {
                if (_closed || _subscriptions.Count is 0 || _deliveryQueued)
                {
                    return;
                }

                _deliveryQueued = true;
                int generation = _generation;
                _ = Source.Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() =>
                {
                    _deliveryQueued = false;
                    if (_closed || generation != _generation)
                    {
                        return;
                    }

                    Subscription[] current = new Subscription[_subscriptions.Count];
                    _subscriptions.CopyTo(current);
                    foreach (Subscription subscription in current)
                    {
                        if (!subscription.IsDisposed && !_closed)
                        {
                            subscription.Changed();
                        }
                    }
                }));
            }

            internal IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                _ = hwnd;
                _ = handled;
                if (_closed)
                {
                    return IntPtr.Zero;
                }

                if (message is WmNcDestroy)
                {
                    Close();
                    return IntPtr.Zero;
                }

                if ((uint)message == PInvoke.WM_POWERBROADCAST)
                {
                    uint eventCode = unchecked((uint)wParam.ToInt64());
                    if (eventCode == PInvoke.PBT_POWERSETTINGCHANGE)
                    {
                        if (TryParsePowerSetting(lParam, out bool enabled))
                        {
                            SetPower(enabled);
                        }
                    }
                    else if (eventCode is PInvoke.PBT_APMRESUMEAUTOMATIC
                        or PInvoke.PBT_APMRESUMESUSPEND
                        or PInvoke.PBT_APMRESUMECRITICAL
                        or PInvoke.PBT_APMPOWERSTATUSCHANGE)
                    {
                        RefreshPower();
                    }
                }
                else if ((uint)message is PInvoke.WM_SETTINGCHANGE
                    or PInvoke.WM_THEMECHANGED
                    or PInvoke.WM_SYSCOLORCHANGE)
                {
                    RefreshEnvironment();
                }

                return IntPtr.Zero;
            }

            private void OnWindowClosed(object? sender, EventArgs e)
            {
                _ = sender;
                _ = e;
                Close();
            }

            private void OnEnvironmentChanged(object? sender, EventArgs e)
            {
                _ = sender;
                _ = e;
                RequestEnvironmentRefresh();
            }

            private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
            {
                _ = sender;
                _ = e;
                RequestEnvironmentRefresh();
            }

            private void OnStaticPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                _ = sender;
                _ = e;
                RequestEnvironmentRefresh();
            }

            private void RequestEnvironmentRefresh()
            {
                if (_closed)
                {
                    return;
                }

                if (Source.Dispatcher.CheckAccess())
                {
                    RefreshEnvironment();
                }
                else
                {
                    int generation = _generation;
                    _ = Source.Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() =>
                    {
                        if (!_closed && generation == _generation)
                        {
                            RefreshEnvironment();
                        }
                    }));
                }
            }

            private void Close()
            {
                if (_closed)
                {
                    return;
                }

                _closed = true;
                _generation++;
                if (_graphicsTimer is DispatcherTimer timer)
                {
                    timer.Stop();
                    timer.Tick -= OnGraphicsTimer;
                    _graphicsTimer = null;
                }
                foreach (Subscription subscription in _subscriptions)
                {
                    subscription.Invalidate();
                }
                _subscriptions.Clear();
                FluenceThemeEngine.Published -= OnEnvironmentChanged;
                ApplicationThemeManager.Changed -= OnThemeChanged;
                SystemParameters.StaticPropertyChanged -= OnStaticPropertyChanged;
                RenderCapability.TierChanged -= OnEnvironmentChanged;
                if (_window is Window window)
                {
                    window.Closed -= OnWindowClosed;
                    _window = null;
                }

                if (_hookAdded)
                {
                    Source.RemoveHook(WndProc);
                    _hookAdded = false;
                }
                IDisposable? registration = _registration;
                _registration = null;
                if (registration is not null)
                {
                    _native.UnregisterPowerSaving(registration);
                }

                if (Entries.TryGetValue(Source, out MonitorEntry? current)
                    && ReferenceEquals(current, this))
                {
                    _ = Entries.TryRemove(Source, out _);
                }
            }
        }

        private sealed class Win32PowerNative : IAcrylicPowerNative
        {
            internal static readonly Win32PowerNative Instance = new();

            public bool TryGetPowerSaving(out bool enabled)
            {
                return NativeMethods.TryGetPowerSaving(out enabled);
            }

            public IDisposable? RegisterPowerSaving(IntPtr hwnd)
            {
                return NativeMethods.RegisterPowerSaving(hwnd);
            }

            public void UnregisterPowerSaving(IDisposable registration)
            {
                NativeMethods.UnregisterPowerSaving(registration);
            }
        }
    }
}
