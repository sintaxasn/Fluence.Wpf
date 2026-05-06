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
using System.Windows.Automation.Peers;
using Fluence.Wpf.Automation;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A toggle switch control with On/Off content.
    /// </summary>
    /// <remarks>Inspired by WInUI's ToggleSwitch.</remarks>
    public class ToggleSwitch : System.Windows.Controls.Primitives.ToggleButton
    {
        /// <summary>
        /// Initializes static members of the ToggleSwitch class and overrides the default style metadata.
        /// </summary>
        /// <remarks>This static constructor ensures that the ToggleSwitch control uses its custom default
        /// style by associating it with the appropriate style key. This is required for custom controls to apply their
        /// styles correctly in XAML.</remarks>
        static ToggleSwitch()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ToggleSwitch),
                new FrameworkPropertyMetadata(typeof(ToggleSwitch)));
        }

        /// <summary>
        /// Identifies the <see cref="OnContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OnContentProperty =
            DependencyProperty.Register(
                nameof(OnContent),
                typeof(object),
                typeof(ToggleSwitch),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the content displayed when the switch is on.
        /// </summary>
        public object OnContent
        {
            get => GetValue(OnContentProperty);
            set => SetValue(OnContentProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="OffContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OffContentProperty =
            DependencyProperty.Register(
                nameof(OffContent),
                typeof(object),
                typeof(ToggleSwitch),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the content displayed when the switch is off.
        /// </summary>
        public object OffContent
        {
            get => GetValue(OffContentProperty);
            set => SetValue(OffContentProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="OnContentTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OnContentTemplateProperty =
            DependencyProperty.Register(
                nameof(OnContentTemplate),
                typeof(DataTemplate),
                typeof(ToggleSwitch),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the data template for the on-state content.
        /// </summary>
        public DataTemplate OnContentTemplate
        {
            get => (DataTemplate)GetValue(OnContentTemplateProperty);
            set => SetValue(OnContentTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="OffContentTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OffContentTemplateProperty =
            DependencyProperty.Register(
                nameof(OffContentTemplate),
                typeof(DataTemplate),
                typeof(ToggleSwitch),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the data template for the off-state content.
        /// </summary>
        public DataTemplate OffContentTemplate
        {
            get => (DataTemplate)GetValue(OffContentTemplateProperty);
            set => SetValue(OffContentTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HeaderContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderContentProperty =
            DependencyProperty.Register(
                nameof(HeaderContent),
                typeof(object),
                typeof(ToggleSwitch),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the header content displayed above the switch.
        /// </summary>
        public object HeaderContent
        {
            get => GetValue(HeaderContentProperty);
            set => SetValue(HeaderContentProperty, value);
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ToggleSwitchAutomationPeer(this);
        }
    }
}
