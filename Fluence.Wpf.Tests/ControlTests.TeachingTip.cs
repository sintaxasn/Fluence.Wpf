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
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Shapes;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="Controls.TeachingTip"/> control: default style and
    /// template parts, popup hosting, placement and beak resolution, light dismiss mapping,
    /// footer button behavior, and surface brush theming.
    /// </summary>
    public partial class ControlTests
    {
        [TestMethod]
        public void TeachingTip_DefaultStyle_AppliesAndTemplatePartsFound()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TeachingTip defaults = new();
                Assert.AreEqual(string.Empty, defaults.Title, "Title must default to an empty string.");
                Assert.AreEqual(string.Empty, defaults.Subtitle, "Subtitle must default to an empty string.");
                Assert.IsFalse(defaults.IsOpen, "IsOpen must default to false.");
                Assert.IsFalse(defaults.IsLightDismissEnabled, "IsLightDismissEnabled must default to false.");
                Assert.IsNull(defaults.Target, "Target must default to null.");
                Assert.AreEqual(TeachingTipPlacementMode.Auto, defaults.PreferredPlacement,
                    "PreferredPlacement must default to Auto per the WinUI TeachingTip contract.");

                Window window = new() { Width = 640, Height = 480 };
                Grid host = new();
                Controls.TeachingTip tip = new()
                {
                    Title = "Title",
                    Subtitle = "Subtitle",
                    Content = "Body",
                };
                _ = host.Children.Add(tip);
                window.Content = host;

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(320.0, tip.MinWidth, 0.01, "TeachingTip.MinWidth must be the WinUI TeachingTipMinWidth (320).");
                    Assert.AreEqual(336.0, tip.MaxWidth, 0.01, "TeachingTip.MaxWidth must be the WinUI TeachingTipMaxWidth (336).");
                    Assert.AreEqual(new Thickness(16, 15, 16, 17), tip.Padding,
                        "TeachingTip.Padding must reuse the WinUI FlyoutContentThemePadding like FlyoutPresenter.");

                    Border? surface = FindVisualChildByName<Border>(tip, "TipSurface");
                    Assert.IsNotNull(surface, "TipSurface must exist in the TeachingTip template (Fluence style applied).");
                    CornerRadius? overlayRadius = (CornerRadius?)app?.FindResource("OverlayCornerRadius");
                    Assert.AreEqual(overlayRadius, surface.CornerRadius,
                        "TeachingTip surface must use OverlayCornerRadius like the other flyout popups.");
                    Assert.AreEqual(new Thickness(1), surface.BorderThickness,
                        "TeachingTip surface must use the 1px flyout stroke.");

                    ButtonBase? action = FindVisualChildByName<ButtonBase>(tip, "PART_ActionButton");
                    ButtonBase? close = FindVisualChildByName<ButtonBase>(tip, "PART_CloseButton");
                    Assert.IsNotNull(action, "PART_ActionButton must exist in the TeachingTip template.");
                    Assert.IsNotNull(close, "PART_CloseButton must exist in the TeachingTip template.");
                    Assert.AreEqual(Visibility.Collapsed, action.Visibility,
                        "The action button must collapse while ActionButtonContent is null.");
                    Assert.AreEqual(Visibility.Visible, close.Visibility,
                        "The close button must always be visible as the close affordance.");
                    Assert.AreEqual("", close.Content,
                        "The close button must fall back to the Fluent close glyph while CloseButtonContent is null.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_IsOpenTrue_OpensPopupAndRendersContent()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 640, Height = 480 };
                Button target = new() { Content = "Anchor" };
                Controls.TeachingTip tip = new()
                {
                    Title = "Update ready",
                    Subtitle = "Restart to apply the update",
                    Content = "Body text",
                    Target = target,
                };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true }),
                        "IsOpen=true must open the host popup.");

                    Popup? popup = tip.HostPopup;
                    Assert.IsNotNull(popup, "Opening the tip must lazily create the host popup.");
                    Assert.IsTrue(popup.AllowsTransparency, "TeachingTip popups must allow transparency for the rounded surface.");
                    Assert.AreEqual(PopupAnimation.Fade, popup.PopupAnimation, "TeachingTip popups must use the fade animation.");
                    Assert.AreSame(tip, popup.Child, "The popup child must be the templated TeachingTip itself.");
                    Assert.AreSame(target, popup.PlacementTarget, "The popup must anchor to TeachingTip.Target.");
                    Assert.AreEqual(PlacementMode.Bottom, popup.Placement, "Auto must map to popup Bottom placement.");
                    Assert.IsTrue(popup.StaysOpen, "Light dismiss is disabled by default, so the popup must stay open.");

                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => FindVisualChildren<TextBlock>(tip)
                            .Any(t => string.Equals(t.Text, "Update ready", StringComparison.Ordinal))),
                        "The title must render inside the open tip.");
                    Assert.IsTrue(FindVisualChildren<TextBlock>(tip)
                            .Any(t => string.Equals(t.Text, "Restart to apply the update", StringComparison.Ordinal)),
                        "The subtitle must render inside the open tip.");
                    Assert.IsTrue(FindVisualChildren<TextBlock>(tip)
                            .Any(t => string.Equals(t.Text, "Body text", StringComparison.Ordinal)),
                        "The body content must render inside the open tip.");

                    Assert.AreEqual(TeachingTipPlacementMode.Bottom, tip.ActualPlacement,
                        "The tip must record the resolved placement when it opens.");
                    Path? topBeak = tip.Template.FindName("TopBeak", tip) as Path;
                    Assert.IsNotNull(topBeak, "TopBeak must exist in the TeachingTip template.");
                    Assert.AreEqual(Visibility.Visible, topBeak.Visibility,
                        "A tip popped below its target must show the beak on its top edge.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_IsOpenFalse_ClosesPopupAndRaisesClosed()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 640, Height = 480 };
                Button target = new() { Content = "Anchor" };
                Controls.TeachingTip tip = new()
                {
                    Title = "Closable",
                    Target = target,
                };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true }),
                        "IsOpen=true must open the host popup before the close scenario.");

                    bool closedRaised = false;
                    tip.Closed += (_, _) => closedRaised = true;

                    tip.IsOpen = false;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: false }),
                        "IsOpen=false must close the host popup.");

                    // Popup.Closed is raised asynchronously once the fade-out completes, so
                    // sample the flag instead of asserting immediately.
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => closedRaised),
                        "Closing the tip must raise Closed after the popup closes.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_IsLightDismissEnabled_MapsToPopupStaysOpen()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 640, Height = 480 };
                Button target = new() { Content = "Anchor" };
                Controls.TeachingTip tip = new()
                {
                    Title = "Dismissable",
                    Target = target,
                    IsLightDismissEnabled = true,
                };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true }),
                        "IsOpen=true must open the host popup before light dismiss is verified.");

                    Popup? popup = tip.HostPopup;
                    Assert.IsNotNull(popup, "Opening the tip must lazily create the host popup.");
                    Assert.IsFalse(popup.StaysOpen,
                        "IsLightDismissEnabled=true must map to a light-dismiss popup (StaysOpen=false).");

                    tip.IsLightDismissEnabled = false;
                    Assert.IsTrue(popup.StaysOpen,
                        "Disabling light dismiss while open must restore StaysOpen=true.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_CloseButton_RaisesCloseButtonClickAndCloses()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 640, Height = 480 };
                Button target = new() { Content = "Anchor" };
                Controls.TeachingTip tip = new()
                {
                    Title = "Close me",
                    Target = target,
                };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => tip.HostPopup is { IsOpen: true } && tip.Template?.FindName("PART_CloseButton", tip) is ButtonBase),
                        "The tip must open and apply its template before the close button is clicked.");

                    bool closeClickRaised = false;
                    bool closedRaised = false;
                    tip.CloseButtonClick += (_, _) => closeClickRaised = true;
                    tip.Closed += (_, _) => closedRaised = true;

                    ButtonBase? closeButton = tip.Template.FindName("PART_CloseButton", tip) as ButtonBase;
                    Assert.IsNotNull(closeButton, "PART_CloseButton must exist in the TeachingTip template.");
                    closeButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

                    Assert.IsTrue(closeClickRaised, "Clicking the close button must raise CloseButtonClick.");
                    Assert.IsFalse(tip.IsOpen, "Clicking the close button must set IsOpen=false.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: false }),
                        "Clicking the close button must close the host popup.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => closedRaised),
                        "Clicking the close button must raise Closed once the popup has closed.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_ActionButton_RaisesActionButtonClickAndInvokesCommand()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 640, Height = 480 };
                Button target = new() { Content = "Anchor" };
                TeachingTipRecordingCommand command = new();
                Controls.TeachingTip tip = new()
                {
                    Title = "Act on me",
                    Target = target,
                    ActionButtonContent = "Update now",
                    ActionButtonCommand = command,
                    ActionButtonCommandParameter = "payload",
                };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => tip.HostPopup is { IsOpen: true } && tip.Template?.FindName("PART_ActionButton", tip) is ButtonBase),
                        "The tip must open and apply its template before the action button is clicked.");

                    bool actionClickRaised = false;
                    tip.ActionButtonClick += (_, _) => actionClickRaised = true;

                    ButtonBase? actionButton = tip.Template.FindName("PART_ActionButton", tip) as ButtonBase;
                    Assert.IsNotNull(actionButton, "PART_ActionButton must exist in the TeachingTip template.");
                    Assert.AreEqual(Visibility.Visible, actionButton.Visibility,
                        "The action button must be visible when ActionButtonContent is set.");
                    actionButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

                    Assert.IsTrue(actionClickRaised, "Clicking the action button must raise ActionButtonClick.");
                    Assert.AreEqual(1, command.ExecuteCount, "Clicking the action button must execute ActionButtonCommand once.");
                    Assert.AreEqual("payload", command.LastParameter,
                        "ActionButtonCommand must receive ActionButtonCommandParameter.");
                    Assert.IsTrue(tip.IsOpen, "Invoking the action button must not close the tip.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_NoTarget_CentersPopupAndHidesBeak()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 640, Height = 480, Content = new Grid() };
                Controls.TeachingTip tip = new()
                {
                    Title = "Untargeted",
                    Subtitle = "Centered over the window content",
                };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true }),
                        "An untargeted tip must still open its host popup.");

                    Popup? popup = tip.HostPopup;
                    Assert.IsNotNull(popup, "Opening the tip must lazily create the host popup.");
                    Assert.AreEqual(PlacementMode.Center, popup.Placement,
                        "An untargeted tip must center its popup.");
                    Assert.AreSame(window.Content, popup.PlacementTarget,
                        "An untargeted tip must anchor to the active window's content.");
                    Assert.AreEqual(TeachingTipPlacementMode.Center, tip.ActualPlacement,
                        "An untargeted tip must resolve ActualPlacement to Center.");

                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => tip.Template?.FindName("TopBeak", tip) is Path),
                        "The tip template must apply inside the popup.");
                    foreach (string beakName in new[] { "TopBeak", "BottomBeak", "LeftBeak", "RightBeak" })
                    {
                        Path? beak = tip.Template.FindName(beakName, tip) as Path;
                        Assert.IsNotNull(beak, string.Format("{0} must exist in the TeachingTip template.", beakName));
                        Assert.AreEqual(Visibility.Collapsed, beak.Visibility,
                            string.Format("{0} must be collapsed for an untargeted (Center) tip.", beakName));
                    }
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_PreferredPlacement_MapsToPopupPlacement()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 640, Height = 480 };
                Button target = new() { Content = "Anchor" };
                Controls.TeachingTip tip = new()
                {
                    Title = "Placed",
                    Target = target,
                };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true }),
                        "IsOpen=true must open the host popup before placement mapping is verified.");

                    Popup? popup = tip.HostPopup;
                    Assert.IsNotNull(popup, "Opening the tip must lazily create the host popup.");
                    Assert.AreEqual(PlacementMode.Bottom, popup.Placement, "Auto must map to popup Bottom placement.");

                    tip.PreferredPlacement = TeachingTipPlacementMode.Top;
                    Assert.AreEqual(PlacementMode.Top, popup.Placement, "Top must map to popup Top placement.");
                    Assert.AreEqual(TeachingTipPlacementMode.Top, tip.ActualPlacement, "Top must resolve ActualPlacement to Top.");
                    DrainDispatcher(window.Dispatcher);
                    Path? bottomBeak = tip.Template.FindName("BottomBeak", tip) as Path;
                    Path? topBeak = tip.Template.FindName("TopBeak", tip) as Path;
                    Assert.IsNotNull(bottomBeak, "BottomBeak must exist in the TeachingTip template.");
                    Assert.IsNotNull(topBeak, "TopBeak must exist in the TeachingTip template.");
                    Assert.AreEqual(Visibility.Visible, bottomBeak.Visibility,
                        "A tip popped above its target must show the beak on its bottom edge.");
                    Assert.AreEqual(Visibility.Collapsed, topBeak.Visibility,
                        "The top beak must hide when the tip moves above its target.");

                    tip.PreferredPlacement = TeachingTipPlacementMode.Left;
                    Assert.AreEqual(PlacementMode.Left, popup.Placement, "Left must map to popup Left placement.");

                    tip.PreferredPlacement = TeachingTipPlacementMode.Right;
                    Assert.AreEqual(PlacementMode.Right, popup.Placement, "Right must map to popup Right placement.");

                    tip.PreferredPlacement = TeachingTipPlacementMode.Bottom;
                    Assert.AreEqual(PlacementMode.Bottom, popup.Placement, "Bottom must map to popup Bottom placement.");

                    tip.PreferredPlacement = TeachingTipPlacementMode.Center;
                    Assert.AreEqual(PlacementMode.Center, popup.Placement, "Center must map to popup Center placement.");

                    tip.PreferredPlacement = TeachingTipPlacementMode.Auto;
                    Assert.AreEqual(PlacementMode.Bottom, popup.Placement, "Auto currently maps to popup Bottom placement.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_ThemeCycle_SurfaceBrushesResolve()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                string[] brushKeys =
                [
                    "SolidBackgroundFillColorTertiaryBrush",
                    "SurfaceStrokeColorFlyoutBrush",
                    "TextFillColorPrimaryBrush",
                    "TextFillColorSecondaryBrush",
                ];

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, true);
                    foreach (string? key in brushKeys)
                    {
                        Assert.IsNotNull(app?.TryFindResource(key),
                            string.Format("Resource '{0}' must resolve in TeachingTip theme cycle step: {1}", key, theme));
                    }
                }
            });
        }

        private sealed class TeachingTipRecordingCommand : ICommand
        {
            public object? LastParameter { get; private set; }

            public int ExecuteCount { get; private set; }

            public bool CanExecute(object? parameter) { return true; }

            public void Execute(object? parameter)
            {
                LastParameter = parameter;
                ExecuteCount++;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S108:Nested blocks of code should not be left empty", Justification = "This is just test code.")]
            public event EventHandler? CanExecuteChanged { add { } remove { } }
        }
    }
}
