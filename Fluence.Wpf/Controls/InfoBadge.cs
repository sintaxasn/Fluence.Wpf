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

using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Fluence.Wpf.Automation;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A small badge overlay that displays a numeric value, icon, or dot indicator.
    /// Typically attached to a <see cref="NavigationViewItem"/> or other control.
    /// </summary>
    /// <remarks>
    /// <see cref="System.Windows.Controls.ContentControl.Content"/> is driven by the badge itself:
    /// it carries the value's text, the icon element, or nothing at all, depending on which display
    /// kind <see cref="Value"/> and <see cref="IconSource"/> resolve to. WinUI's InfoBadge has no
    /// content surface at all (<c language="text">InfoBadge.idl</c> declares only
    /// <c language="csharp">Value</c>, <c language="csharp">IconSource</c> and
    /// <c language="csharp">TemplateSettings</c>); here it is an artifact of the WPF base class, so
    /// a value assigned to it directly is overwritten the next time either property changes. Set
    /// <see cref="Value"/> or <see cref="IconSource"/> instead.
    /// </remarks>
    [TemplateVisualState(GroupName = "DisplayKindStates", Name = "Dot")]
    [TemplateVisualState(GroupName = "DisplayKindStates", Name = "Icon")]
    [TemplateVisualState(GroupName = "DisplayKindStates", Name = "FontIcon")]
    [TemplateVisualState(GroupName = "DisplayKindStates", Name = "Value")]
    public class InfoBadge : ContentControl
    {
        /// <summary>
        /// Initializes static members of the InfoBadge class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the InfoBadge control uses its custom style by
        /// default. It is called automatically by the .NET runtime before any static members are accessed or any
        /// instances are created.</remarks>
        static InfoBadge()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(InfoBadge),
                new FrameworkPropertyMetadata(typeof(InfoBadge)));
        }

        /// <summary>
        /// Identifies the <see cref="Value"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(int),
                typeof(InfoBadge),
                new FrameworkPropertyMetadata(-1, OnValueChanged));

        /// <summary>
        /// Gets or sets the numeric value displayed. Set to -1 to show a dot instead.
        /// </summary>
        public int Value
        {
            get => (int)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BadgeStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BadgeStyleProperty =
            DependencyProperty.Register(
                nameof(BadgeStyle),
                typeof(InfoBadgeStyle),
                typeof(InfoBadge),
                new FrameworkPropertyMetadata(InfoBadgeStyle.Attention));

        /// <summary>
        /// Gets or sets the severity style of the badge.
        /// </summary>
        public InfoBadgeStyle BadgeStyle
        {
            get => (InfoBadgeStyle)GetValue(BadgeStyleProperty);
            set => SetValue(BadgeStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IconSource"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconSourceProperty =
            DependencyProperty.Register(
                nameof(IconSource),
                typeof(object),
                typeof(InfoBadge),
                new FrameworkPropertyMetadata(defaultValue: null, OnIconSourceChanged));

        /// <summary>
        /// Gets or sets an icon element to display inside the badge (overrides Value text).
        /// </summary>
        public object IconSource
        {
            get => GetValue(IconSourceProperty);
            set => SetValue(IconSourceProperty, value);
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new InfoBadgeAutomationPeer(this);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(InfoBadge),
                new FrameworkPropertyMetadata(new CornerRadius(8)));

        /// <summary>
        /// Gets or sets the corner radius of the badge. Left alone it follows the badge's own
        /// height, staying a capsule at any size; set locally it is honoured as given, which is
        /// what WinUI's own <c language="text">InfoBadge.cpp</c> does with a local value.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <inheritdoc />
        /// <remarks>
        /// A badge is never narrower than it is tall: WinUI squares it up when the natural width
        /// comes out under the height (<c language="text">InfoBadge.cpp</c>
        /// <c language="csharp">MeasureOverride</c>), which is what turns a single digit value into
        /// a circle rather than a squashed oval. With the 4 dip minimum width WinUI's own metrics
        /// carry, nothing else would hold that shape.
        /// </remarks>
        protected override Size MeasureOverride(Size constraint)
        {
            Size desired = base.MeasureOverride(constraint);
            return desired.Width < desired.Height ? new Size(desired.Height, desired.Height) : desired;
        }

        /// <inheritdoc />
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateCornerRadius();
        }

        /// <summary>
        /// Keeps the badge a capsule at whatever height it ends up: the radius is half the measured
        /// height, which is what WinUI recomputes on every size change
        /// (<c language="text">InfoBadge.cpp</c> <c language="csharp">OnSizeChanged</c>). A radius
        /// the consumer set locally wins, as it does there.
        /// </summary>
        private void UpdateCornerRadius()
        {
            if (ReadLocalValue(CornerRadiusProperty) != DependencyProperty.UnsetValue)
            {
                return;
            }

            double radius = ActualHeight / 2;
            SetCurrentValue(CornerRadiusProperty, new CornerRadius(radius));
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateDisplayKind(useTransitions: false);
        }

        private static void OnIconSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((InfoBadge)d).UpdateDisplayKind();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((InfoBadge)d).UpdateDisplayKind();
        }

        /// <summary>
        /// Resolves what the badge shows and which <c language="xaml">DisplayKindStates</c> state it
        /// is in, in WinUI's own order of precedence: a value wins, then an icon, then the dot
        /// (<c language="text">InfoBadge.cpp</c> <c language="csharp">OnPropertyChanged</c>, which
        /// tests <c language="csharp">Value() &gt;= 0</c> first). A badge carrying both a value and
        /// an icon therefore shows the value, and clearing the value falls back to the icon rather
        /// than leaving the badge empty.
        /// </summary>
        /// <param name="useTransitions">Whether to play the state transition.</param>
        private void UpdateDisplayKind(bool useTransitions = true)
        {
            string state;
            if (Value >= 0)
            {
                Content = Value.ToString(CultureInfo.CurrentCulture);
                state = "Value";
            }
            else if (IconSource is not null)
            {
                Content = IconSource;

                // WinUI splits the icon states by icon type, because a font glyph and a drawn icon
                // take different insets (IconInfoBadgeFontIconMargin against IconInfoBadgeIconMargin).
                state = IconSource is FontIcon ? "FontIcon" : "Icon";
            }
            else
            {
                Content = null;
                state = "Dot";
            }

            _ = VisualStateManager.GoToState(this, state, useTransitions);
        }
    }
}
