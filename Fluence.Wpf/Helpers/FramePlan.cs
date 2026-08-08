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

namespace Fluence.Wpf.Helpers
{
    /// <summary>
    /// Carries the resolved window-border frame instructions computed by
    /// <see cref="Controls.WindowPolicy.BuildFramePlan"/>: the <c>DynamicResource</c> key for the
    /// WPF-template border brush (<see cref="TemplateBorderBrushResourceKey"/>) and the DWM border
    /// color (<see cref="DwmBorderColor"/>). The WPF-template border's thickness is not part of
    /// this plan; it is driven entirely by the FluenceWindow control template
    /// (<c>Themes/Controls/FluenceWindow.xaml</c>), which sets it to 1 by default and 0 while
    /// maximized via a <c>WindowState</c> trigger.
    /// </summary>
    /// <param name="templateBorderBrushResourceKey">The <c>DynamicResource</c> key for the border brush.</param>
    /// <param name="dwmBorderColor">The COLORREF (BGR, 24-bit) value for the DWM border color.</param>
    internal sealed class FramePlan(
        string templateBorderBrushResourceKey,
        int dwmBorderColor)
    {
        /// <summary>
        /// Gets the <c>DynamicResource</c> key for the border brush to apply to the template
        /// border element. Always <c>"CardStrokeColorDefaultSolidBrush"</c>: the window frame
        /// uses the subtle system stroke in every state, never an accent color.
        /// </summary>
        internal string TemplateBorderBrushResourceKey { get; } = templateBorderBrushResourceKey;

        /// <summary>
        /// Gets the COLORREF (BGR, 24-bit) value to write to <c>DWMWA_BORDER_COLOR</c>. Always
        /// <see cref="Native.NativeConstants.DWMWA_COLOR_DEFAULT"/>, which tells DWM to restore
        /// its own border. A caller must check <see cref="WindowCapabilities.SupportsBorderColor"/>
        /// before writing this value to the DWM attribute; the plan records the sentinel
        /// regardless so the caller does not need a separate null check.
        /// </summary>
        internal int DwmBorderColor { get; } = dwmBorderColor;
    }
}
