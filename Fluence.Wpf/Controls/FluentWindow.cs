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
using System.ComponentModel;
using System.Windows;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Deprecated alias for <see cref="FluenceWindow"/>. New code should use <see cref="FluenceWindow"/> directly.
    /// </summary>
    /// <remarks>
    /// This type is retained for binary and source compatibility with earlier 0.x releases and simply inherits
    /// the <see cref="FluenceWindow"/> chrome, backdrop, caption-button, and title-bar behavior. It will be removed
    /// in a future major version.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use Fluence.Wpf.Controls.FluenceWindow instead. FluentWindow is retained as an alias for 0.x source compatibility and will be removed in a future major version.", error: false)]
    public class FluentWindow : FluenceWindow
    {
        static FluentWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FluentWindow),
                new FrameworkPropertyMetadata(typeof(FluenceWindow)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentWindow"/> class. Equivalent to constructing a <see cref="FluenceWindow"/>.
        /// </summary>
        public FluentWindow()
        {
        }
    }
}
