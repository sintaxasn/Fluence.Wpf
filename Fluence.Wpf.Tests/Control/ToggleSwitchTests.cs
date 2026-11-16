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
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.DispatcherWaits;
using static Fluence.Wpf.Tests.Infrastructure.TemplatePartTransforms;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Fluent <see cref="ToggleSwitch"/> control: knob easing (SplineDoubleKeyFrame /
    /// ControlFastOutSlowIn).
    /// </summary>
    public sealed class ToggleSwitchTests : IClassFixture<LightThemeFixture>
    {
        public ToggleSwitchTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        // ---------------------------------------------------------------------------
        // WI-3 B12  ToggleSwitch knob easing
        // ---------------------------------------------------------------------------

        [Fact]
        public Task ToggleSwitch_StyleApplies_SwitchThumbFoundAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ToggleSwitch ts = new();
                Window w = new() { Content = ts, Width = 160, Height = 60 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    Ellipse thumb = Assert.IsType<Ellipse>(FindVisualChildByName<Ellipse>(ts, "SwitchThumb"), exactMatch: false);
                    Thumb input = Assert.IsType<Thumb>(FindVisualChildByName<Thumb>(ts, "PART_SwitchThumbInput"), exactMatch: false);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task ToggleSwitch_DefaultState_ThumbWidth12Async()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ToggleSwitch ts = new() { IsChecked = false };
                Window w = new() { Content = ts, Width = 160, Height = 60 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    Ellipse thumb = Assert.IsType<Ellipse>(FindVisualChildByName<Ellipse>(ts, "SwitchThumb"), exactMatch: false);
                    Assert.Equal(12.0, thumb.Width, 0.001);
                    Assert.Equal(12.0, thumb.Height, 0.001);

                    ScaleTransform scale = GetToggleSwitchThumbScale(ts);
                    Assert.Equal(1.0, scale.ScaleX, 0.001);
                    Assert.Equal(1.0, scale.ScaleY, 0.001);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task ToggleSwitch_Checked_ThumbTranslateIs20Async()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ToggleSwitch ts = new() { IsChecked = true };
                Window w = new() { Content = ts, Width = 160, Height = 60 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    TranslateTransform tx = GetToggleSwitchKnobTranslate(ts);
                    Assert.Equal(20.0, tx.X, 0.5);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task ToggleSwitch_Unchecked_ThumbTranslateIsZeroAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ToggleSwitch ts = new() { IsChecked = false };
                Window w = new() { Content = ts, Width = 160, Height = 60 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    TranslateTransform tx = GetToggleSwitchKnobTranslate(ts);
                    Assert.Equal(0.0, tx.X, 0.5);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task ToggleSwitch_ProgrammaticToggle_AnimatesKnobToCheckedSideAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                ToggleSwitch ts = new() { IsChecked = false };
                Window w = new() { Content = ts, Width = 160, Height = 60 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    TranslateTransform tx = GetToggleSwitchKnobTranslate(ts);
                    Assert.Equal(0.0, tx.X, 0.5);

                    ts.IsChecked = true;
                    Assert.True(tx.X < 20.0,
                        "Programmatic toggle should start an animation instead of snapping directly to the checked side.");

                    Assert.True(await WaitUntilAsync(w.Dispatcher, 1000,
                        () => Math.Abs(tx.X - 20.0) <= 0.5).ConfigureAwait(true),
                        "The programmatic toggle must reach the checked knob position.");
                    Assert.Equal(20.0, tx.X, 0.5);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task ToggleSwitch_DragInput_ExpandsThumbAndCommitsCheckedStateAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                ToggleSwitch ts = new() { IsChecked = false };
                Window w = new() { Content = ts, Width = 160, Height = 60 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    Ellipse thumb = Assert.IsType<Ellipse>(FindVisualChildByName<Ellipse>(ts, "SwitchThumb"), exactMatch: false);
                    Thumb input = Assert.IsType<Thumb>(FindVisualChildByName<Thumb>(ts, "PART_SwitchThumbInput"), exactMatch: false);
                    TranslateTransform tx = GetToggleSwitchKnobTranslate(ts);
                    ScaleTransform scale = GetToggleSwitchThumbScale(ts);

                    DragStartedEventArgs started = new(0, 0)
                    {
                        RoutedEvent = Thumb.DragStartedEvent,
                    };
                    input.RaiseEvent(started);
                    Assert.True(await WaitUntilAsync(w.Dispatcher, 1000,
                        () => Math.Abs(scale.ScaleX - (17.0 / 12.0)) <= 0.05
                            && Math.Abs(scale.ScaleY - (14.0 / 12.0)) <= 0.05).ConfigureAwait(true),
                        "The drag press must reach the expanded thumb scale.");
                    Assert.Equal(17.0 / 12.0, scale.ScaleX, 0.05);
                    Assert.Equal(14.0 / 12.0, scale.ScaleY, 0.05);
                    Assert.Equal(12.0, thumb.Width, 0.001);
                    Assert.Equal(12.0, thumb.Height, 0.001);

                    DragDeltaEventArgs delta = new(20, 0)
                    {
                        RoutedEvent = Thumb.DragDeltaEvent,
                    };
                    input.RaiseEvent(delta);
                    Assert.Equal(20.0, tx.X, 0.5);

                    DragCompletedEventArgs completed = new(20, 0, canceled: false)
                    {
                        RoutedEvent = Thumb.DragCompletedEvent,
                    };
                    input.RaiseEvent(completed);
                    Assert.Equal(true, ts.IsChecked);
                    Assert.True(await WaitUntilAsync(w.Dispatcher, 1000,
                        () => Math.Abs(tx.X - 20.0) <= 0.5
                            && Math.Abs(scale.ScaleX - 1.0) <= 0.05
                            && Math.Abs(scale.ScaleY - 1.0) <= 0.05).ConfigureAwait(true),
                        "The knob and thumb must settle after drag completion.");
                    Assert.Equal(20.0, tx.X, 0.5);
                    Assert.Equal(1.0, scale.ScaleX, 0.05);
                    Assert.Equal(1.0, scale.ScaleY, 0.05);
                    Assert.Equal(12.0, thumb.Width, 0.001);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task ToggleSwitch_PressedCodePath_ScalesThumbWithoutLayoutSizeChangeAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                ToggleSwitch ts = new() { IsChecked = false };
                Window w = new() { Content = ts, Width = 160, Height = 60 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    Ellipse thumb = Assert.IsType<Ellipse>(FindVisualChildByName<Ellipse>(ts, "SwitchThumb"), exactMatch: false);
                    Thumb input = Assert.IsType<Thumb>(FindVisualChildByName<Thumb>(ts, "PART_SwitchThumbInput"), exactMatch: false);
                    ScaleTransform scale = GetToggleSwitchThumbScale(ts);
                    Assert.Equal(1.0, scale.ScaleX, 0.001);

                    MouseButtonEventArgs pressed = new(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
                    };
                    input.RaiseEvent(pressed);
                    Assert.True(await WaitUntilAsync(w.Dispatcher, 1000,
                        () => Math.Abs(scale.ScaleX - (17.0 / 12.0)) <= 0.05
                            && Math.Abs(scale.ScaleY - (14.0 / 12.0)) <= 0.05).ConfigureAwait(true),
                        "The press path must reach the expanded thumb scale.");

                    Assert.Equal(17.0 / 12.0, scale.ScaleX, 0.05);
                    Assert.Equal(14.0 / 12.0, scale.ScaleY, 0.05);
                    Assert.Equal(12.0, thumb.Width, 0.001);
                    Assert.Equal(12.0, thumb.Height, 0.001);

                    MouseEventArgs lostCapture = new(Mouse.PrimaryDevice, 0)
                    {
                        RoutedEvent = UIElement.LostMouseCaptureEvent,
                    };
                    input.RaiseEvent(lostCapture);
                    Assert.True(await WaitUntilAsync(w.Dispatcher, 1000,
                        () => Math.Abs(scale.ScaleX - 1.0) <= 0.05).ConfigureAwait(true),
                        "The thumb must settle after losing capture.");
                    Assert.Equal(1.0, scale.ScaleX, 0.05);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task ToggleSwitch_ClickReleaseThroughCaptureLoss_CommitsCheckedStateAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                ToggleSwitch ts = new() { IsChecked = false };
                Window w = new() { Content = ts, Width = 160, Height = 60 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    Thumb input = Assert.IsType<Thumb>(FindVisualChildByName<Thumb>(ts, "PART_SwitchThumbInput"), exactMatch: false);
                    List<string> captureTrace = [];
                    void RecordCapture(string stage, MouseEventArgs? args = null)
                    {
                        string captured = ReferenceEquals(Mouse.Captured, input)
                            ? "input"
                            : Mouse.Captured?.GetType().Name ?? "none";
                        captureTrace.Add(string.Format(CultureInfo.InvariantCulture,
                            "{0}: left={1}, captured={2}, checked={3}, eventHandled={4}",
                            stage, Mouse.LeftButton, captured, ts.IsChecked,
                            args?.Handled.ToString() ?? "n/a"));
                    }
                    input.AddHandler(UIElement.LostMouseCaptureEvent,
                        new MouseEventHandler((_, args) => RecordCapture("observed LostMouseCapture", args)),
                        handledEventsToo: true);

                    WithReleasedLeftButton(() =>
                    {
                        MouseButtonEventArgs pressed = new(Mouse.PrimaryDevice, 0, MouseButton.Left)
                        {
                            RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
                        };
                        RecordCapture("before press");
                        input.RaiseEvent(pressed);
                        RecordCapture("after press");

                        MouseEventArgs lostCapture = new(Mouse.PrimaryDevice, 0)
                        {
                            RoutedEvent = UIElement.LostMouseCaptureEvent,
                        };
                        RecordCapture("before synthetic loss");
                        input.RaiseEvent(lostCapture);
                        RecordCapture("after synthetic loss");

                        Assert.True(ts.IsChecked is true,
                            "A released click must commit the checked state. " + string.Join(" | ", captureTrace));
                    });

                    TranslateTransform tx = GetToggleSwitchKnobTranslate(ts);
                    Assert.True(await WaitUntilAsync(w.Dispatcher, 1000,
                        () => Math.Abs(tx.X - 20.0) <= 0.5).ConfigureAwait(true),
                        "The knob must reach the checked position after capture loss.");
                    Assert.Equal(20.0, tx.X, 0.5);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task ToggleSwitch_DragReleaseThroughCaptureLoss_CommitsNearestStateAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                ToggleSwitch ts = new() { IsChecked = false };
                Window w = new() { Content = ts, Width = 160, Height = 60 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    Thumb input = Assert.IsType<Thumb>(FindVisualChildByName<Thumb>(ts, "PART_SwitchThumbInput"), exactMatch: false);
                    TranslateTransform tx = GetToggleSwitchKnobTranslate(ts);
                    List<string> captureTrace = [];
                    void RecordCapture(string stage, MouseEventArgs? args = null)
                    {
                        string captured = ReferenceEquals(Mouse.Captured, input)
                            ? "input"
                            : Mouse.Captured?.GetType().Name ?? "none";
                        captureTrace.Add(string.Format(CultureInfo.InvariantCulture,
                            "{0}: left={1}, captured={2}, checked={3}, eventHandled={4}",
                            stage, Mouse.LeftButton, captured, ts.IsChecked,
                            args?.Handled.ToString() ?? "n/a"));
                    }
                    input.AddHandler(UIElement.LostMouseCaptureEvent,
                        new MouseEventHandler((_, args) => RecordCapture("observed LostMouseCapture", args)),
                        handledEventsToo: true);

                    WithReleasedLeftButton(() =>
                    {
                        DragStartedEventArgs started = new(0, 0)
                        {
                            RoutedEvent = Thumb.DragStartedEvent,
                        };
                        RecordCapture("before drag start");
                        input.RaiseEvent(started);
                        RecordCapture("after drag start");

                        DragDeltaEventArgs delta = new(20, 0)
                        {
                            RoutedEvent = Thumb.DragDeltaEvent,
                        };
                        input.RaiseEvent(delta);
                        Assert.Equal(20.0, tx.X, 0.5);
                        RecordCapture("after drag delta");

                        MouseEventArgs lostCapture = new(Mouse.PrimaryDevice, 0)
                        {
                            RoutedEvent = UIElement.LostMouseCaptureEvent,
                        };
                        RecordCapture("before synthetic loss");
                        input.RaiseEvent(lostCapture);
                        RecordCapture("after synthetic loss");

                        Assert.True(ts.IsChecked is true,
                            "A released drag must commit the nearest state. " + string.Join(" | ", captureTrace));
                    });

                    await WaitForAnimationAndDrainAsync(w.Dispatcher, 250).ConfigureAwait(true);
                    Assert.Equal(20.0, tx.X, 0.5);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task ToggleSwitch_HeaderContent_BecomesAccessibleNameAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    ToggleSwitch ts = new() { HeaderContent = "Airplane mode" };
                    window.Content = ts;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(ts);
                    Assert.True(
                        string.Equals("Airplane mode", peer.GetName(), StringComparison.Ordinal),
                        "ToggleSwitch HeaderContent must be the accessible name when no explicit AutomationProperties.Name is set.");

                    ts.SetValue(AutomationProperties.NameProperty, "Explicit");
                    Assert.True(
                        string.Equals("Explicit", peer.GetName(), StringComparison.Ordinal),
                        "Explicit AutomationProperties.Name must win over HeaderContent.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private const int VirtualKeyLeftButton = 0x01;

        // WPF's Win32MouseDevice reads GetKeyState(VK_LBUTTON). Keep the synthetic
        // release gesture on the STA's synchronous input-state table only.
        private static void WithReleasedLeftButton(Action action)
        {
            byte[] keyboardState = new byte[256];
            if (!NativeMethods.GetKeyboardState(keyboardState))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            byte originalLeftButtonState = keyboardState[VirtualKeyLeftButton];
            keyboardState[VirtualKeyLeftButton] &= 0x7F;
            if (!NativeMethods.SetKeyboardState(keyboardState))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            try
            {
                Assert.Equal(MouseButtonState.Released, Mouse.LeftButton);
                action();
            }
            finally
            {
                RestoreLeftButtonState(keyboardState, originalLeftButtonState);
            }
        }

        private static void RestoreLeftButtonState(byte[] keyboardState, byte originalLeftButtonState)
        {
            // Read again so any other key states changed during the gesture survive.
            if (!NativeMethods.GetKeyboardState(keyboardState))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            keyboardState[VirtualKeyLeftButton] = (byte)((keyboardState[VirtualKeyLeftButton] & 0x7F)
                | (originalLeftButtonState & 0x80));
            if (!NativeMethods.SetKeyboardState(keyboardState))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            if (!NativeMethods.GetKeyboardState(keyboardState))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            Assert.Equal(originalLeftButtonState & 0x80, keyboardState[VirtualKeyLeftButton] & 0x80);
        }

        private static class NativeMethods
        {
            // Source-generated marshalling requires unsafe blocks that this test project disallows.
            // This narrow legacy interop supports both target frameworks.
#pragma warning disable SYSLIB1054
            [DllImport("user32.dll", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetKeyboardState([Out] byte[] keyboardState);

            [DllImport("user32.dll", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetKeyboardState([In] byte[] keyboardState);
#pragma warning restore SYSLIB1054
        }

        private static ScaleTransform GetToggleSwitchThumbScale(ToggleSwitch toggleSwitch)
        {
            Ellipse thumb = Assert.IsType<Ellipse>(FindVisualChildByName<Ellipse>(toggleSwitch, "SwitchThumb"), exactMatch: false);
            return Assert.IsType<ScaleTransform>(thumb.RenderTransform);
        }

        [Fact]
        public Task ToggleSwitch_OnOffContent_SwapsOnCheckAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                ToggleSwitch toggle = new()
                {
                    OnContent = "On",
                    OffContent = "Off",
                    IsChecked = false,
                };

                try
                {
                    window.Content = toggle;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = toggle.ApplyTemplate();
                    FrameworkElement offPresenter = Assert.IsType<FrameworkElement>(toggle.Template.FindName("OffContentPresenter", toggle), exactMatch: false);
                    FrameworkElement onPresenter = Assert.IsType<FrameworkElement>(toggle.Template.FindName("OnContentPresenter", toggle), exactMatch: false);
                    Assert.Equal(Visibility.Visible, offPresenter.Visibility);
                    Assert.Equal(Visibility.Collapsed, onPresenter.Visibility);

                    toggle.IsChecked = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(Visibility.Collapsed, offPresenter.Visibility);
                    Assert.Equal(Visibility.Visible, onPresenter.Visibility);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ToggleSwitch_IsChecked_TogglesOnClickAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                ToggleSwitch toggle = new() { IsChecked = false };

                try
                {
                    window.Content = toggle;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(false, toggle.IsChecked);

                    IToggleProvider toggleProvider = (ToggleButtonAutomationPeer)new(toggle);
                    toggleProvider.Toggle();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(true, toggle.IsChecked);
                }
                finally
                {
                    window.Close();
                }
            });
        }
    }
}
