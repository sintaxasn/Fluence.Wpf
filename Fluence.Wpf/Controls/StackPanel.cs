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
using System.Windows.Controls;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Stack panel with uniform spacing between children.
    /// </summary>
    public class StackPanel : System.Windows.Controls.StackPanel
    {
        /// <summary>
        /// Identifies the <see cref="Spacing"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SpacingProperty =
            DependencyProperty.Register(
                nameof(Spacing),
                typeof(double),
                typeof(StackPanel),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// Gets or sets the uniform spacing between children.
        /// </summary>
        public double Spacing
        {
            get { return (double)GetValue(SpacingProperty); }
            set { SetValue(SpacingProperty, value); }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var orientation = Orientation;
            var spacing = Spacing;
            var children = InternalChildren;
            var count = children.Count;

            if (count == 0)
            {
                return new Size(0, 0);
            }

            double totalMain = 0;
            double maxCross = 0;

            if (orientation == Orientation.Vertical)
            {
                var remainingHeight = constraint.Height;
                for (var i = 0; i < count; i++)
                {
                    var child = children[i];
                    if (child == null)
                    {
                        continue;
                    }

                    child.Measure(new Size(constraint.Width, remainingHeight));
                    var size = child.DesiredSize;
                    totalMain += size.Height;
                    if (i < count - 1)
                    {
                        totalMain += spacing;
                    }

                    remainingHeight = Math.Max(0, remainingHeight - size.Height - (i < count - 1 ? spacing : 0));
                    maxCross = Math.Max(maxCross, size.Width);
                }

                return new Size(maxCross, totalMain);
            }

            var remainingWidth = constraint.Width;
            for (var i = 0; i < count; i++)
            {
                var child = children[i];
                if (child == null)
                {
                    continue;
                }

                child.Measure(new Size(remainingWidth, constraint.Height));
                var size = child.DesiredSize;
                totalMain += size.Width;
                if (i < count - 1)
                {
                    totalMain += spacing;
                }

                remainingWidth = Math.Max(0, remainingWidth - size.Width - (i < count - 1 ? spacing : 0));
                maxCross = Math.Max(maxCross, size.Height);
            }

            return new Size(totalMain, maxCross);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            var orientation = Orientation;
            var spacing = Spacing;
            var children = InternalChildren;
            var count = children.Count;

            if (orientation == Orientation.Vertical)
            {
                var y = 0.0;
                for (var i = 0; i < count; i++)
                {
                    var child = children[i];
                    if (child == null)
                    {
                        continue;
                    }

                    var height = child.DesiredSize.Height;
                    child.Arrange(new Rect(0, y, arrangeSize.Width, height));
                    y += height;
                    if (i < count - 1)
                    {
                        y += spacing;
                    }
                }

                return arrangeSize;
            }

            var x = 0.0;
            for (var i = 0; i < count; i++)
            {
                var child = children[i];
                if (child == null)
                {
                    continue;
                }

                var width = child.DesiredSize.Width;
                child.Arrange(new Rect(x, 0, width, arrangeSize.Height));
                x += width;
                if (i < count - 1)
                {
                    x += spacing;
                }
            }

            return arrangeSize;
        }
    }
}
