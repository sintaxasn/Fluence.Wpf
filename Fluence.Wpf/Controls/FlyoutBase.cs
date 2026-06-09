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
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Provides data for the <see cref="FlyoutBase.Closing"/> event.
    /// </summary>
    public class FlyoutBaseClosingEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets a value indicating whether the close operation should be canceled.
        /// Set to <see langword="true"/> to keep the flyout open.
        /// </summary>
        public bool Cancel { get; set; }
    }

    /// <summary>
    /// Represents the base class for flyout controls that display lightweight UI in a
    /// light-dismiss <see cref="Popup"/> anchored to a placement target, mirroring the
    /// WinUI 3 <c>FlyoutBase</c> contract.
    /// </summary>
    /// <remarks>
    /// The popup is created lazily on the first <see cref="ShowAt"/> call with
    /// <see cref="Popup.StaysOpen"/> set to <see langword="false"/> (clicking outside the
    /// flyout dismisses it). Derived classes supply the popup child via
    /// <see cref="CreatePresenter"/>.
    /// </remarks>
    public abstract class FlyoutBase : DependencyObject
    {
        /// <summary>
        /// Identifies the AttachedFlyout attached property, which associates a flyout with an
        /// arbitrary element so it can later be opened via <see cref="ShowAttachedFlyout"/>.
        /// </summary>
        public static readonly DependencyProperty AttachedFlyoutProperty =
            DependencyProperty.RegisterAttached(
                "AttachedFlyout",
                typeof(FlyoutBase),
                typeof(FlyoutBase),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="Placement"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlacementProperty =
            DependencyProperty.Register(
                nameof(Placement),
                typeof(FlyoutPlacementMode),
                typeof(FlyoutBase),
                new PropertyMetadata(FlyoutPlacementMode.Top, OnPlacementChanged));

        /// <summary>
        /// Gets or sets where the flyout opens relative to its placement target.
        /// </summary>
        public FlyoutPlacementMode Placement
        {
            get => (FlyoutPlacementMode)GetValue(PlacementProperty);
            set => SetValue(PlacementProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShouldConstrainToRootBounds"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShouldConstrainToRootBoundsProperty =
            DependencyProperty.Register(
                nameof(ShouldConstrainToRootBounds),
                typeof(bool),
                typeof(FlyoutBase),
                new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the flyout should stay within the bounds of
        /// the XAML root. Accepted for WinUI signature compatibility; WPF popups are positioned
        /// by the OS, so the value is not currently enforced.
        /// </summary>
        public bool ShouldConstrainToRootBounds
        {
            get => (bool)GetValue(ShouldConstrainToRootBoundsProperty);
            set => SetValue(ShouldConstrainToRootBoundsProperty, value);
        }

        /// <summary>
        /// Occurs before the flyout opens.
        /// </summary>
        public event EventHandler? Opening;

        /// <summary>
        /// Occurs after the flyout has opened.
        /// </summary>
        public event EventHandler? Opened;

        /// <summary>
        /// Occurs before the flyout closes through <see cref="Hide"/>. Set
        /// <see cref="FlyoutBaseClosingEventArgs.Cancel"/> to <see langword="true"/> to keep the
        /// flyout open. Light-dismiss closes bypass this event and raise only
        /// <see cref="Closed"/>.
        /// </summary>
        public event EventHandler<FlyoutBaseClosingEventArgs>? Closing;

        /// <summary>
        /// Occurs after the flyout has closed, whether through <see cref="Hide"/> or
        /// light dismiss.
        /// </summary>
        public event EventHandler? Closed;

        /// <summary>
        /// Gets a value indicating whether the flyout is currently open.
        /// </summary>
        public bool IsOpen => HostPopup is not null && HostPopup.IsOpen;

        /// <summary>
        /// Gets the popup that hosts the presenter. Created lazily on the first
        /// <see cref="ShowAt"/> call. Internal so tests can verify popup configuration.
        /// </summary>
        internal Popup? HostPopup { get; private set; }

        /// <summary>
        /// Gets the cached presenter element returned by <see cref="CreatePresenter"/>.
        /// Internal so tests can verify the hosted content.
        /// </summary>
        internal FrameworkElement? Presenter { get; private set; }

        /// <summary>
        /// Gets the flyout attached to the specified element via
        /// <see cref="AttachedFlyoutProperty"/>.
        /// </summary>
        /// <param name="element">The element the flyout is attached to.</param>
        /// <returns>The attached flyout, or <see langword="null"/> when none is attached.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="element"/> is <c>null</c>.</exception>
        public static FlyoutBase? GetAttachedFlyout(FrameworkElement element)
        {
            return element is null
                ? throw new ArgumentNullException(nameof(element))
                : (FlyoutBase?)element.GetValue(AttachedFlyoutProperty);
        }

        /// <summary>
        /// Sets the flyout attached to the specified element via
        /// <see cref="AttachedFlyoutProperty"/>.
        /// </summary>
        /// <param name="element">The element to attach the flyout to.</param>
        /// <param name="value">The flyout to attach, or <see langword="null"/> to detach.</param>
        /// <exception cref="ArgumentNullException"><paramref name="element"/> is <c>null</c>.</exception>
        public static void SetAttachedFlyout(FrameworkElement element, FlyoutBase? value)
        {
            if (element is null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(AttachedFlyoutProperty, value);
        }

        /// <summary>
        /// Shows the flyout attached to the specified element via
        /// <see cref="AttachedFlyoutProperty"/>, anchored to that element. Does nothing when no
        /// flyout is attached.
        /// </summary>
        /// <param name="flyoutOwner">The element whose attached flyout should be shown.</param>
        /// <exception cref="ArgumentNullException"><paramref name="flyoutOwner"/> is <c>null</c>.</exception>
        public static void ShowAttachedFlyout(FrameworkElement flyoutOwner)
        {
            if (flyoutOwner is null)
            {
                throw new ArgumentNullException(nameof(flyoutOwner));
            }

            GetAttachedFlyout(flyoutOwner)?.ShowAt(flyoutOwner);
        }

        /// <summary>
        /// Shows the flyout placed relative to the specified element. Raises
        /// <see cref="Opening"/> before the popup opens and <see cref="Opened"/> after, then
        /// moves focus to the presenter.
        /// </summary>
        /// <param name="placementTarget">The element to anchor the flyout to.</param>
        /// <exception cref="ArgumentNullException"><paramref name="placementTarget"/> is <c>null</c>.</exception>
        public void ShowAt(FrameworkElement placementTarget)
        {
            if (placementTarget is null)
            {
                throw new ArgumentNullException(nameof(placementTarget));
            }

            Popup popup = EnsurePopup();
            popup.PlacementTarget = placementTarget;
            popup.Placement = MapPlacement(Placement);
            if (popup.IsOpen)
            {
                return;
            }

            Opening?.Invoke(this, EventArgs.Empty);
            popup.IsOpen = true;
            Opened?.Invoke(this, EventArgs.Empty);
            if (Presenter is not null)
            {
                _ = Presenter.Focus();
            }
        }

        /// <summary>
        /// Hides the flyout. Raises <see cref="Closing"/> first; the close is abandoned when a
        /// handler sets <see cref="FlyoutBaseClosingEventArgs.Cancel"/> to
        /// <see langword="true"/>. <see cref="Closed"/> is raised once the popup has closed.
        /// </summary>
        public void Hide()
        {
            if (HostPopup is null || !HostPopup.IsOpen)
            {
                return;
            }

            FlyoutBaseClosingEventArgs args = new();
            Closing?.Invoke(this, args);
            if (args.Cancel)
            {
                return;
            }

            HostPopup.IsOpen = false;
        }

        /// <summary>
        /// Creates the element that presents the flyout content as the popup child. Called once
        /// when the popup is created; implementations may cache and return the same instance.
        /// </summary>
        /// <returns>The presenter element hosted as the popup child.</returns>
        protected abstract FrameworkElement CreatePresenter();

        /// <summary>
        /// Maps the WinUI-style <see cref="FlyoutPlacementMode"/> to the WPF popup
        /// <see cref="PlacementMode"/>. <see cref="FlyoutPlacementMode.Full"/> and
        /// <see cref="FlyoutPlacementMode.Auto"/> map to bottom placement.
        /// </summary>
        /// <param name="placement">The requested flyout placement.</param>
        /// <returns>The equivalent popup placement.</returns>
        private static PlacementMode MapPlacement(FlyoutPlacementMode placement)
        {
            return placement switch
            {
                FlyoutPlacementMode.Top => PlacementMode.Top,
                FlyoutPlacementMode.Left => PlacementMode.Left,
                FlyoutPlacementMode.Right => PlacementMode.Right,
                FlyoutPlacementMode.Bottom or FlyoutPlacementMode.Full or FlyoutPlacementMode.Auto or _ =>
                    PlacementMode.Bottom,
            };
        }

        private static void OnPlacementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlyoutBase flyout && flyout.HostPopup is not null)
            {
                flyout.HostPopup.Placement = MapPlacement((FlyoutPlacementMode)e.NewValue);
            }
        }

        /// <summary>
        /// Creates the light-dismiss popup on first use and hosts the presenter returned by
        /// <see cref="CreatePresenter"/> as its child.
        /// </summary>
        /// <returns>The popup that hosts the presenter.</returns>
        private Popup EnsurePopup()
        {
            if (HostPopup is null)
            {
                Presenter = CreatePresenter();
                HostPopup = new Popup
                {
                    AllowsTransparency = true,
                    Child = Presenter,
                    PopupAnimation = PopupAnimation.Fade,
                    StaysOpen = false,
                };
                HostPopup.Closed += OnPopupClosed;
            }

            return HostPopup;
        }

        private void OnPopupClosed(object? sender, EventArgs e)
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }
    }
}
