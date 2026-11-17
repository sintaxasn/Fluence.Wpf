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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Fluence.Wpf.Helpers;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Control
{
    public sealed class AcrylicMaterialBudgetTests
    {
        [Fact]
        public void MeasuredPaddedSurfaceAndTwoPaneAggregateAreAccepted()
        {
            Assert.True(AcrylicMaterialBudget.TryCountPaddedPixels(Map(1296, 736, 1.5), out long wholePixels));
            Assert.Equal(2_146_176L, wholePixels);
            Assert.True(AcrylicMaterialBudget.TryCountPaddedPixels(Map(656, 736, 1.5), out long panePixels));
            Assert.Equal(1_086_336L, panePixels);
            Assert.Equal(AcrylicMaterialBudget.MaxAggregatePaddedPixels, 2 * panePixels);

            BudgetParticipant whole = new();
            BudgetParticipant left = new();
            BudgetParticipant right = new();
            BudgetParticipant waiting = new();
            try
            {
                Assert.True(AcrylicMaterialBudget.TryReserve(whole, Map(1296, 736, 1.5)));
                Assert.False(AcrylicMaterialBudget.TryReserve(waiting, Map(656, 736, 1.5)));
                AcrylicMaterialBudget.Release(whole);
                Assert.Equal(1, waiting.RefreshRequests);

                Assert.True(AcrylicMaterialBudget.TryReserve(left, Map(656, 736, 1.5)));
                Assert.True(AcrylicMaterialBudget.TryReserve(right, Map(656, 736, 1.5)));
                Assert.False(AcrylicMaterialBudget.TryReserve(waiting, Map(656, 736, 1.5)));
                AcrylicMaterialBudget.Release(left);
                Assert.True(waiting.RefreshRequests >= 2);
                Assert.True(AcrylicMaterialBudget.TryReserve(waiting, Map(656, 736, 1.5)));
            }
            finally
            {
                AcrylicMaterialBudget.Release(whole);
                AcrylicMaterialBudget.Release(left);
                AcrylicMaterialBudget.Release(right);
                AcrylicMaterialBudget.Release(waiting);
            }
        }

        [Fact]
        public void OversizedResizeReleasesTheOldReservationAndWakesWaiter()
        {
            BudgetParticipant first = new();
            BudgetParticipant second = new();
            try
            {
                Assert.True(AcrylicMaterialBudget.TryReserve(first, Map(1296, 736, 1.5)));
                Assert.False(AcrylicMaterialBudget.TryReserve(second, Map(656, 736, 1.5)));
                Assert.False(AcrylicMaterialBudget.TryReserve(first, Map(1296.1, 736, 1.5)));
                Assert.Equal(1, second.RefreshRequests);
                Assert.True(AcrylicMaterialBudget.TryReserve(second, Map(656, 736, 1.5)));
            }
            finally
            {
                AcrylicMaterialBudget.Release(first);
                AcrylicMaterialBudget.Release(second);
            }
        }

        [Fact]
        public void TwoSmallSurfacesFillTheCountBudgetAcrossOwners()
        {
            BudgetParticipant firstWindow = new();
            BudgetParticipant secondWindow = new();
            BudgetParticipant thirdWindow = new();
            try
            {
                Assert.True(AcrylicMaterialBudget.TryReserve(firstWindow, Map(10, 10)));
                Assert.True(AcrylicMaterialBudget.TryReserve(secondWindow, Map(10, 10)));
                Assert.False(AcrylicMaterialBudget.TryReserve(thirdWindow, Map(10, 10)));
                AcrylicMaterialBudget.Release(firstWindow);
                Assert.Equal(1, thirdWindow.RefreshRequests);
                Assert.True(AcrylicMaterialBudget.TryReserve(thirdWindow, Map(10, 10)));
            }
            finally
            {
                AcrylicMaterialBudget.Release(firstWindow);
                AcrylicMaterialBudget.Release(secondWindow);
                AcrylicMaterialBudget.Release(thirdWindow);
            }
        }

        [Fact]
        public void OnePixelBeyondProcessAggregateCapIsDenied()
        {
            BudgetParticipant first = new();
            BudgetParticipant second = new();
            try
            {
                Assert.True(AcrylicMaterialBudget.TryReserve(first, Map(1296, 736, 1.5)));
                Assert.True(AcrylicMaterialBudget.TryReserve(second, Map(26_496, 1)));
                Assert.False(AcrylicMaterialBudget.TryReserve(second, Map(26_497, 1)));
                AcrylicMaterialBudget.Release(first);
                Assert.True(AcrylicMaterialBudget.TryReserve(second, Map(26_497, 1)));
            }
            finally
            {
                AcrylicMaterialBudget.Release(first);
                AcrylicMaterialBudget.Release(second);
            }
        }

        [Fact]
        public void InvalidPhysicalScaleFailsConservatively()
        {
            BudgetParticipant participant = new();
            try
            {
                Assert.False(AcrylicMaterialBudget.TryReserve(participant, Map(100, 100, double.NaN)));
                Assert.False(AcrylicMaterialBudget.TryReserve(participant, Map(100, 100, double.PositiveInfinity)));
                Assert.False(AcrylicMaterialBudget.TryReserve(participant, Map(100, 100, 0)));
                Assert.True(AcrylicMaterialBudget.TryReserve(participant, Map(100, 100)));
                Assert.True(AcrylicMaterialBudget.TryReserve(participant, Map(100, 100)));
            }
            finally
            {
                AcrylicMaterialBudget.Release(participant);
            }
        }

        [Fact]
        public void PhysicalCeilingAndOnePixelBeyondSingleSurfaceCapAreConservative()
        {
            Assert.False(AcrylicMaterialBudget.TryCountPaddedPixels(Map(1296.001, 736, 1.5), out _));
            Assert.False(AcrylicMaterialBudget.TryCountPaddedPixels(Map(double.MaxValue, 1), out _));
            Assert.False(AcrylicMaterialBudget.TryCountPaddedPixels(Map(100, 100, double.NaN), out _));
        }

        [Fact]
        public void WeakEntriesDoNotRootAnAbandonedParticipant()
        {
            WeakReference weak = MakeAbandonedReservation();
            for (int attempt = 0; attempt < 3 && weak.IsAlive; attempt++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            Assert.False(weak.IsAlive);
            BudgetParticipant successor = new();
            try
            {
                Assert.True(AcrylicMaterialBudget.TryReserve(successor, Map(1296, 736, 1.5)));
            }
            finally
            {
                AcrylicMaterialBudget.Release(successor);
            }
        }

        [Fact]
        public Task BudgetFallbackIsOpaqueAndHasNoMaterialOrNoiseChildrenAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Border owner = new();
                using AcrylicMaterialRenderer renderer = new(owner);
                renderer.Update(owner, Map(100, 100),
                    AcrylicRecipeSnapshot.Create(new AcrylicMaterialRecipe()),
                    new AcrylicPolicyResult(AcrylicFallbackReason.BudgetExceeded,
                        Color.FromArgb(64, 23, 45, 67)));
                Canvas output = Assert.IsType<Canvas>(renderer.OutputVisual);
                Assert.Equal(AcrylicFallbackReason.BudgetExceeded, renderer.Reason);
                Assert.True(renderer.IsUsingFallback);
                Assert.Empty(output.Children);
                Assert.Null(output.Effect);
                SolidColorBrush brush = Assert.IsType<SolidColorBrush>(output.Background);
                Assert.Equal(Color.FromRgb(23, 45, 67), brush.Color);
                Assert.Equal(byte.MaxValue, brush.Color.A);
            });
        }

        [Fact]
        public Task VisibleHostsCompeteAcrossWindowsAndRecoverAfterFourReleasePathsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                (Window firstWindow, Grid firstRoot, Canvas firstSource) = NewWindow();
                (Window secondWindow, Grid secondRoot, Canvas secondSource) = NewWindow();
                Fluence.Wpf.Controls.AcrylicElement first = AddHost(firstRoot, firstSource);
                Fluence.Wpf.Controls.AcrylicElement second = AddHost(secondRoot, secondSource);
                Fluence.Wpf.Controls.AcrylicElement waiting = AddHost(secondRoot, secondSource);
                bool firstClosed = false;
                try
                {
                    firstWindow.Show();
                    secondWindow.Show();
                    Drain();
                    if (IsEnvironmentGate(first.FallbackReason))
                    {
                        Assert.True(first.IsUsingFallback);
                        return;
                    }

                    Assert.Equal(AcrylicFallbackReason.None, first.FallbackReason);
                    Assert.Equal(AcrylicFallbackReason.None, second.FallbackReason);
                    AssertBudgetFallback(waiting);

                    VisualTree.CloseWindowAndDrain(firstWindow);
                    firstClosed = true;
                    Drain();
                    Assert.Equal(AcrylicFallbackReason.None, waiting.FallbackReason);

                    Fluence.Wpf.Controls.AcrylicElement afterPolicy = AddHost(secondRoot, secondSource);
                    Drain();
                    AssertBudgetFallback(afterPolicy);
                    second.Recipe = new AcrylicMaterialRecipe { AlwaysUseFallback = true };
                    Drain();
                    Assert.Equal(AcrylicFallbackReason.Always, second.FallbackReason);
                    Assert.Equal(AcrylicFallbackReason.None, afterPolicy.FallbackReason);

                    Fluence.Wpf.Controls.AcrylicElement afterSource = AddHost(secondRoot, secondSource);
                    Drain();
                    AssertBudgetFallback(afterSource);
                    waiting.BackdropSource = null;
                    Drain();
                    Assert.Equal(AcrylicFallbackReason.UnavailableSource, waiting.FallbackReason);
                    Assert.Equal(AcrylicFallbackReason.None, afterSource.FallbackReason);

                    Fluence.Wpf.Controls.AcrylicElement afterUnload = AddHost(secondRoot, secondSource);
                    Drain();
                    AssertBudgetFallback(afterUnload);
                    secondRoot.Children.Remove(afterPolicy);
                    Drain();
                    Assert.Equal(AcrylicFallbackReason.None, afterUnload.FallbackReason);
                }
                finally
                {
                    if (!firstClosed)
                    {
                        VisualTree.CloseWindowAndDrain(firstWindow);
                    }

                    VisualTree.CloseWindowAndDrain(secondWindow);
                }
            });
        }

        [Fact]
        public Task VisibleHostReleasesOversizedResizeAndRecoversOnShrinkAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                BudgetParticipant otherWindow = new();
                Assert.True(AcrylicMaterialBudget.TryReserve(otherWindow, Map(1296, 736, 1.5)));
                (Window window, Grid root, Canvas source) = NewWindow();
                Fluence.Wpf.Controls.AcrylicElement host = AddHost(root, source);
                host.Width = 50;
                host.Height = 40;
                try
                {
                    window.Show();
                    Drain();
                    if (IsEnvironmentGate(host.FallbackReason))
                    {
                        Assert.True(host.IsUsingFallback);
                        return;
                    }

                    Assert.Equal(AcrylicFallbackReason.None, host.FallbackReason);
                    host.Width = 400;
                    window.UpdateLayout();
                    Drain();
                    AssertBudgetFallback(host);
                    host.Width = 50;
                    window.UpdateLayout();
                    Drain();
                    Assert.Equal(AcrylicFallbackReason.None, host.FallbackReason);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                    AcrylicMaterialBudget.Release(otherWindow);
                }
            });
        }

        [Fact]
        public Task ForeignThreadReleaseQueuesVisibleRecoveryOnOwningDispatcherAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                _ = WpfTestSta.EnsureApplication();
                BudgetParticipant firstWindow = new();
                BudgetParticipant secondWindow = new();
                Assert.True(AcrylicMaterialBudget.TryReserve(firstWindow, Map(10, 10)));
                Assert.True(AcrylicMaterialBudget.TryReserve(secondWindow, Map(10, 10)));
                (Window window, Grid root, Canvas source) = NewWindow();
                Fluence.Wpf.Controls.AcrylicElement waiting = AddHost(root, source);
                try
                {
                    window.Show();
                    Drain();
                    if (IsEnvironmentGate(waiting.FallbackReason))
                    {
                        Assert.True(waiting.IsUsingFallback);
                        return;
                    }

                    AssertBudgetFallback(waiting);
                    await Task.Run(() => AcrylicMaterialBudget.Release(firstWindow),
                        TestContext.Current.CancellationToken).ConfigureAwait(true);
                    Drain();
                    Assert.Equal(AcrylicFallbackReason.None, waiting.FallbackReason);
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                    AcrylicMaterialBudget.Release(firstWindow);
                    AcrylicMaterialBudget.Release(secondWindow);
                }
            });
        }

        private static (Window Window, Grid Root, Canvas Source) NewWindow()
        {
            Grid root = new();
            Canvas source = new() { Background = Brushes.DarkSlateBlue };
            _ = root.Children.Add(source);
            Window window = new()
            {
                Width = 540,
                Height = 240,
                Content = root,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
            };
            return (window, root, source);
        }

        private static Fluence.Wpf.Controls.AcrylicElement AddHost(Grid root, Canvas source)
        {
            Fluence.Wpf.Controls.AcrylicElement host = new()
            {
                BackdropSource = source,
                Width = 120,
                Height = 80,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(10),
            };
            _ = root.Children.Add(host);
            return host;
        }

        private static void Drain()
        {
            WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
            WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
        }

        private static bool IsEnvironmentGate(AcrylicFallbackReason reason)
        {
            bool gated = reason is AcrylicFallbackReason.UnsupportedGraphics
                or AcrylicFallbackReason.HighContrast
                or AcrylicFallbackReason.TransparencyOff
                or AcrylicFallbackReason.PowerSaving;
            if (gated)
            {
                Assert.False(string.Equals("1", Environment.GetEnvironmentVariable("FLUENCE_REQUIRE_BUDGET_MATERIAL"),
                    StringComparison.Ordinal), "This local verification requires a material-capable Windows session.");
            }

            return gated;
        }

        private static void AssertBudgetFallback(Fluence.Wpf.Controls.AcrylicElement host)
        {
            Assert.Equal(AcrylicFallbackReason.BudgetExceeded, host.FallbackReason);
            Assert.True(host.IsUsingFallback);
            _ = host.ApplyTemplate();
            ControlTemplate template = Assert.IsType<ControlTemplate>(host.Template);
            Border backdrop = Assert.IsType<Border>(template.FindName("PART_Backdrop", host));
            VisualBrush brush = Assert.IsType<VisualBrush>(backdrop.Background);
            Canvas output = Assert.IsType<Canvas>(brush.Visual);
            Assert.Empty(output.Children);
            Assert.Null(output.Effect);
            SolidColorBrush color = Assert.IsType<SolidColorBrush>(output.Background);
            Assert.Equal(byte.MaxValue, color.Color.A);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference MakeAbandonedReservation()
        {
            BudgetParticipant participant = new();
            Assert.True(AcrylicMaterialBudget.TryReserve(participant, Map(1296, 736, 1.5)));
            return new WeakReference(participant);
        }

        private static AcrylicSourceMapResult Map(double physicalWidth, double physicalHeight, double scale = 1)
        {
            Rect padded = new(0, 0, physicalWidth, physicalHeight);
            return new AcrylicSourceMapResult(AcrylicFallbackReason.None, Matrix.Identity, Matrix.Identity,
                padded, padded, padded, padded, scale, scale, 0, 0);
        }

        private sealed class BudgetParticipant : IAcrylicBudgetParticipant
        {
            internal int RefreshRequests;
            public void RequestBudgetRefresh()
            {
                _ = Interlocked.Increment(ref RefreshRequests);
            }
        }
    }
}
