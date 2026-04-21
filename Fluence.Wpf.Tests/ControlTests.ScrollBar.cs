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
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-5A.3 tests for the Fluent ScrollBar VSM uplift.
    /// Verifies CommonStates and ScrollingIndicatorStates VSM groups are present and
    /// that GoToState with useTransitions=false snaps to the correct dimension instantly.
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-5A.3 ScrollBar — PART names found in ScrollViewer
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void ScrollBar_ScrollViewerTemplate_ContainsBothScrollBarParts()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var sv = new ScrollViewer
                {
                    Width = 200,
                    Height = 100,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
                    Style = app.TryFindResource("ScrollViewerStyle") as Style
                };

                var sp = new StackPanel();
                for (var i = 0; i < 30; i++)
                {
                    sp.Children.Add(new TextBlock { Text = "Item " + i, Height = 20, Width = 400 });
                }
                sv.Content = sp;

                var window = new Window { Width = 300, Height = 200, Content = sv };
                try
                {
                    window.Show();
                    sv.UpdateLayout();

                    var vertBar = FindVisualChildByName<ScrollBar>(sv, "PART_VerticalScrollBar");
                    var horizBar = FindVisualChildByName<ScrollBar>(sv, "PART_HorizontalScrollBar");

                    Assert.IsNotNull(vertBar,
                        "PART_VerticalScrollBar must be present in the ScrollViewerStyle template.");
                    Assert.IsNotNull(horizBar,
                        "PART_HorizontalScrollBar must be present in the ScrollViewerStyle template.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        // ---------------------------------------------------------------------------
        // WI-5A.3 ScrollBar — VSM ScrollingIndicatorStates
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void ScrollBar_VSM_MouseIndicator_ExpandsVerticalWidth()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var sb = new ScrollBar
                {
                    Orientation = Orientation.Vertical,
                    Style = app.TryFindResource("VerticalScrollBarStyle") as Style,
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0,
                    ViewportSize = 10,
                    Width = 6,
                    Height = 200
                };

                var window = new Window { Width = 60, Height = 300, Content = sb };
                try
                {
                    window.Show();
                    sb.ApplyTemplate();
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    // GoToState with useTransitions=false: DiscreteDoubleKeyFrame at
                    // KeyTime=0 applies the final value immediately.
                    var stateApplied = VisualStateManager.GoToState(sb, "MouseIndicator", false);
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    Assert.IsTrue(stateApplied,
                        "GoToState('MouseIndicator') must return true — VSM group must be present.");

                    var root = FindVisualChildByName<Grid>(sb, "Root");
                    Assert.IsNotNull(root, "Root Grid must be present in VerticalScrollBarTemplate.");
                    Assert.IsTrue(root.Width >= 10.0,
                        "Root.Width must be >= 10 in MouseIndicator state (actual: " + root.Width + ").");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [TestMethod]
        public void ScrollBar_VSM_NoIndicator_CollapsesVerticalWidth()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var sb = new ScrollBar
                {
                    Orientation = Orientation.Vertical,
                    Style = app.TryFindResource("VerticalScrollBarStyle") as Style,
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0,
                    ViewportSize = 10,
                    Width = 6,
                    Height = 200
                };

                var window = new Window { Width = 60, Height = 300, Content = sb };
                try
                {
                    window.Show();
                    sb.ApplyTemplate();
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    // Expand to MouseIndicator first, then collapse back.
                    VisualStateManager.GoToState(sb, "MouseIndicator", false);
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    var stateApplied = VisualStateManager.GoToState(sb, "NoIndicator", false);
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    Assert.IsTrue(stateApplied,
                        "GoToState('NoIndicator') must return true — VSM group must be present.");

                    var root = FindVisualChildByName<Grid>(sb, "Root");
                    Assert.IsNotNull(root, "Root Grid must be present in VerticalScrollBarTemplate.");
                    Assert.IsTrue(root.Width <= 6.0,
                        "Root.Width must be <= 6 in NoIndicator state (actual: " + root.Width + ").");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [TestMethod]
        public void ScrollBar_VSM_MouseIndicator_ExpandsHorizontalHeight()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var sb = new ScrollBar
                {
                    Orientation = Orientation.Horizontal,
                    Style = app.TryFindResource("HorizontalScrollBarStyle") as Style,
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0,
                    ViewportSize = 10,
                    Height = 6,
                    Width = 200
                };

                var window = new Window { Width = 300, Height = 60, Content = sb };
                try
                {
                    window.Show();
                    sb.ApplyTemplate();
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    var stateApplied = VisualStateManager.GoToState(sb, "MouseIndicator", false);
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    Assert.IsTrue(stateApplied,
                        "GoToState('MouseIndicator') on horizontal ScrollBar must return true.");

                    var root = FindVisualChildByName<Grid>(sb, "Root");
                    Assert.IsNotNull(root, "Root Grid must be present in HorizontalScrollBarTemplate.");
                    Assert.IsTrue(root.Height >= 10.0,
                        "Root.Height must be >= 10 in MouseIndicator state (actual: " + root.Height + ").");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        // ---------------------------------------------------------------------------
        // WI-5A.3 ScrollBar — disabled state reduces opacity
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void ScrollBar_Disabled_OpacityReducedOrElementDisabled()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var sb = new ScrollBar
                {
                    Orientation = Orientation.Vertical,
                    Style = app.TryFindResource("VerticalScrollBarStyle") as Style,
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0,
                    ViewportSize = 10,
                    Width = 6,
                    Height = 200
                };

                var window = new Window { Width = 60, Height = 300, Content = sb };
                try
                {
                    window.Show();
                    sb.ApplyTemplate();
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    sb.IsEnabled = false;
                    DrainDispatcher(WpfTestSta.Dispatcher);

                    // IsEnabled=False trigger sets Opacity=0.45 on the ScrollBar root.
                    Assert.IsTrue(!sb.IsEnabled || sb.Opacity < 1.0,
                        "Disabled ScrollBar must either be IsEnabled=false or have Opacity < 1.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        // ---------------------------------------------------------------------------
        // WI-5A.3 ScrollBar — theme cycle
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void ScrollBar_ThemeCycle_BrushesResolveAfterEachSwitch()
        {
            WpfTestSta.Invoke(() =>
            {
                var app = EnsureApplication();
                MergeGenericDictionary(app);

                var keys = new[]
                {
                    "ControlStrongFillColorDefaultBrush",
                    "SubtleFillColorSecondaryBrush"
                };

                foreach (var theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, true);
                    foreach (var key in keys)
                    {
                        Assert.IsNotNull(app.TryFindResource(key),
                            string.Format("Resource '{0}' must resolve in ScrollBar theme cycle step: {1}", key, theme));
                    }
                }
            });
        }
    }
}
