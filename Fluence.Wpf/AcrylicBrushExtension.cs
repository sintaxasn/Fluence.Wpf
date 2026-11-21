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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using Fluence.Wpf.Helpers;

namespace Fluence.Wpf
{
    /// <summary>
    /// Provides one in-app acrylic visual brush for a concrete supported XAML target.
    /// </summary>
    public sealed class AcrylicBrushExtension : MarkupExtension
    {
        /// <summary>
        /// Initializes a new acrylic brush markup extension.
        /// </summary>
        public AcrylicBrushExtension()
        {
        }

        /// <summary>
        /// Initializes a new acrylic brush markup extension with a backdrop source name.
        /// </summary>
        /// <param name="backdropSourceName">The name of a source in the target's namescope.</param>
        public AcrylicBrushExtension(string backdropSourceName)
        {
            BackdropSourceName = backdropSourceName;
        }

        /// <summary>
        /// Gets or sets the required name of the same-window backdrop source.
        /// </summary>
        public string BackdropSourceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the fixed recipe values used by this brush instance.
        /// </summary>
        public AcrylicMaterialRecipe? Recipe { get; set; }

        /// <summary>
        /// Creates a distinct visual brush for a concrete Border or supported Shape fill.
        /// </summary>
        /// <param name="serviceProvider">The XAML service provider for this use.</param>
        /// <returns>A per-use <see cref="VisualBrush"/>.</returns>
        /// <exception cref="ArgumentNullException">The XAML service provider is missing.</exception>
        /// <exception cref="InvalidOperationException">The backdrop source name is empty.</exception>
        /// <exception cref="NotSupportedException">The XAML target does not have a concrete supported brush property.</exception>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);
            if (string.IsNullOrWhiteSpace(BackdropSourceName))
            {
                throw new InvalidOperationException("AcrylicBrush requires a nonempty BackdropSourceName.");
            }

            IProvideValueTarget? target = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
            if (target?.TargetObject is not FrameworkElement owner
                || target.TargetProperty is not DependencyProperty property
                || !((owner is Border && property == Border.BackgroundProperty)
                    || ((owner is Rectangle or Ellipse) && property == Shape.FillProperty)))
            {
                throw new NotSupportedException(
                    "AcrylicBrush requires a concrete Border.Background, Rectangle.Fill, or Ellipse.Fill target. "
                    + "Style setters, shared extension resources, and nonconcrete deferred contexts are unsupported.");
            }

            owner.Dispatcher.VerifyAccess();
            string sourceName = BackdropSourceName;
            AcrylicMaterialController controller = new(owner, () => ResolveSource(owner, sourceName), Recipe);
            controller.AttachDirectProperty(property);
            return controller.Output;
        }

        private static FrameworkElement? ResolveSource(FrameworkElement owner, string sourceName)
        {
            if (owner.FindName(sourceName) is FrameworkElement local)
            {
                return local;
            }

            if (owner.TemplatedParent is FrameworkElement parent
                && parent.FindName(sourceName) is FrameworkElement templateSource)
            {
                return templateSource;
            }

            // A gallery sample can move a compiled visual under another control's namescope.
            // Its original page namescope remains on a loaded visual ancestor.
            for (DependencyObject? node = VisualTreeHelper.GetParent(owner); node is Visual visual;
                node = VisualTreeHelper.GetParent(visual))
            {
                if (node is FrameworkElement ancestor
                    && ancestor.FindName(sourceName) is FrameworkElement source)
                {
                    return source;
                }
            }

            return null;
        }
    }
}
