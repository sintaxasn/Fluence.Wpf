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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="Controls.Flyout"/> / <see cref="Controls.FlyoutBase"/> /
    /// <see cref="Controls.FlyoutPresenter"/> family.
    /// </summary>
    public partial class ControlTests
    {
        [TestMethod]
        public void FlyoutPresenter_DefaultStyle_AppliesFluentSurface()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? style = app?.TryFindResource(typeof(Controls.FlyoutPresenter)) as Style;
                Assert.IsNotNull(style, "A default Style must be registered for Fluence.Wpf.Controls.FlyoutPresenter.");

                Window window = new() { Width = 400, Height = 300 };
                Controls.FlyoutPresenter presenter = new() { Content = "Surface" };

                try
                {
                    window.Content = presenter;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    CornerRadius? overlayRadius = (CornerRadius?)app?.FindResource("OverlayCornerRadius");
                    Border? surface = FindVisualChild<Border>(presenter);

                    Assert.IsNotNull(surface, "FlyoutPresenter template should render its surface Border.");
                    Assert.AreEqual(overlayRadius, surface.CornerRadius,
                        "FlyoutPresenter surface must use OverlayCornerRadius like the other flyout popups.");
                    Assert.AreEqual(new Thickness(1), surface.BorderThickness,
                        "FlyoutPresenter surface must use the 1px flyout stroke.");
                    Assert.AreEqual(new Thickness(16, 15, 16, 17), presenter.Padding,
                        "FlyoutPresenter.Padding must be the WinUI FlyoutContentThemePadding.");
                    Assert.AreEqual(96.0, presenter.MinWidth, 0.01, "FlyoutPresenter.MinWidth must be 96 per WinUI metrics.");
                    Assert.AreEqual(456.0, presenter.MaxWidth, 0.01, "FlyoutPresenter.MaxWidth must be 456 per WinUI metrics.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void Flyout_ShowAt_OpensLightDismissPopupAndPresentsContent()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.Flyout flyout = new() { Content = "Flyout body" };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    bool openingRaised = false;
                    bool openedRaised = false;
                    flyout.Opening += (_, _) => openingRaised = true;
                    flyout.Opened += (_, _) => openedRaised = true;

                    flyout.ShowAt(target);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the flyout popup.");
                    Assert.IsTrue(openingRaised, "ShowAt should raise Opening before the popup opens.");
                    Assert.IsTrue(openedRaised, "ShowAt should raise Opened after the popup opens.");

                    Popup? popup = flyout.HostPopup;
                    Assert.IsNotNull(popup, "ShowAt should lazily create the host popup.");
                    Assert.IsFalse(popup.StaysOpen, "Flyout popups must be light-dismiss (StaysOpen=false).");
                    Assert.IsTrue(popup.AllowsTransparency, "Flyout popups must allow transparency for the rounded surface.");
                    Assert.AreEqual(PopupAnimation.Fade, popup.PopupAnimation, "Flyout popups must use the fade animation.");
                    Assert.AreSame(target, popup.PlacementTarget, "ShowAt must anchor the popup to the placement target.");

                    Controls.FlyoutPresenter? presenter = popup.Child as Controls.FlyoutPresenter;
                    Assert.IsNotNull(presenter, "The popup child must be a FlyoutPresenter.");
                    Assert.AreEqual("Flyout body", presenter.Content, "Flyout.Content must flow to the presenter.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void Flyout_Hide_ClosesPopupAndRaisesClosingThenClosed()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.Flyout flyout = new() { Content = "Closable" };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(target);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the flyout popup before Hide is exercised.");

                    bool closingRaised = false;
                    bool closedRaised = false;
                    flyout.Closing += (_, _) => closingRaised = true;
                    flyout.Closed += (_, _) => closedRaised = true;

                    flyout.Hide();
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => !flyout.IsOpen),
                        "Hide should close the flyout popup.");
                    Assert.IsTrue(closingRaised, "Hide should raise Closing before the popup closes.");

                    // Popup.Closed is raised asynchronously once the fade-out completes, so
                    // sample the flag instead of asserting immediately after Hide returns.
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => closedRaised),
                        "Hide should raise Closed after the popup closes.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void Flyout_ClosingCancel_KeepsFlyoutOpen()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.Flyout flyout = new() { Content = "Sticky" };
                bool cancelClose = true;
                flyout.Closing += (_, args) => args.Cancel = cancelClose;

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(target);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the flyout popup before the cancel scenario.");

                    flyout.Hide();
                    DrainDispatcher(window.Dispatcher);
                    Assert.IsTrue(flyout.IsOpen, "Canceling Closing must keep the flyout open.");

                    cancelClose = false;
                    flyout.Hide();
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => !flyout.IsOpen),
                        "Hide should close the flyout once Closing is no longer canceled.");
                }
                finally
                {
                    cancelClose = false;
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void Flyout_ContentChange_FlowsToPresenter()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.Flyout flyout = new() { Content = "First" };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(target);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the flyout popup before content is swapped.");

                    Controls.FlyoutPresenter? presenter = flyout.HostPopup?.Child as Controls.FlyoutPresenter;
                    Assert.IsNotNull(presenter, "The popup child must be a FlyoutPresenter.");
                    Assert.AreEqual("First", presenter.Content, "The initial Flyout.Content must reach the presenter.");

                    flyout.Content = "Second";
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual("Second", presenter.Content, "Flyout.Content changes must flow to the presenter binding.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void FlyoutBase_ShowAttachedFlyout_OpensAttachedFlyout()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Button owner = new() { Content = "Owner" };
                Controls.Flyout flyout = new() { Content = "Attached" };

                try
                {
                    window.Content = owner;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.FlyoutBase.SetAttachedFlyout(owner, flyout);
                    Assert.AreSame(flyout, Controls.FlyoutBase.GetAttachedFlyout(owner),
                        "GetAttachedFlyout must return the flyout set via SetAttachedFlyout.");

                    Controls.FlyoutBase.ShowAttachedFlyout(owner);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAttachedFlyout should open the attached flyout.");
                    Assert.AreSame(owner, flyout.HostPopup?.PlacementTarget,
                        "ShowAttachedFlyout must anchor the flyout to the owner element.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void Flyout_PlacementModes_MapToPopupPlacement()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.Flyout flyout = new() { Content = "Placed" };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(FlyoutPlacementMode.Top, flyout.Placement,
                        "Flyout placement must default to Top per the WinUI FlyoutBase contract.");
                    Assert.IsTrue(flyout.ShouldConstrainToRootBounds,
                        "ShouldConstrainToRootBounds must default to true.");

                    flyout.ShowAt(target);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the flyout popup before placement mapping is verified.");

                    Popup? popup = flyout.HostPopup;
                    Assert.IsNotNull(popup, "ShowAt should lazily create the host popup.");
                    Assert.AreEqual(PlacementMode.Top, popup.Placement, "Top must map to popup Top placement.");

                    flyout.Placement = FlyoutPlacementMode.Bottom;
                    Assert.AreEqual(PlacementMode.Bottom, popup.Placement, "Bottom must map to popup Bottom placement.");

                    flyout.Placement = FlyoutPlacementMode.Left;
                    Assert.AreEqual(PlacementMode.Left, popup.Placement, "Left must map to popup Left placement.");

                    flyout.Placement = FlyoutPlacementMode.Right;
                    Assert.AreEqual(PlacementMode.Right, popup.Placement, "Right must map to popup Right placement.");

                    flyout.Placement = FlyoutPlacementMode.Full;
                    Assert.AreEqual(PlacementMode.Bottom, popup.Placement, "Full currently maps to popup Bottom placement.");

                    flyout.Placement = FlyoutPlacementMode.Auto;
                    Assert.AreEqual(PlacementMode.Bottom, popup.Placement, "Auto currently maps to popup Bottom placement.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void FlyoutPresenter_ThemeCycle_SurfaceBrushesResolve()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                string[] brushKeys = ["SolidBackgroundFillColorTertiaryBrush", "SurfaceStrokeColorFlyoutBrush", "TextFillColorPrimaryBrush"];

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, true);
                    foreach (string? key in brushKeys)
                    {
                        Assert.IsNotNull(app?.TryFindResource(key),
                            string.Format("Resource '{0}' must resolve in FlyoutPresenter theme cycle step: {1}", key, theme));
                    }
                }
            });
        }
    }
}
