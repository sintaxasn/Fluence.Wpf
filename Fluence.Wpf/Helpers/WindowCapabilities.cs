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

using Fluence.Wpf.Native;

namespace Fluence.Wpf.Helpers
{
    internal sealed class WindowCapabilities(
        bool supportsSystemBackdropType,
        bool supportsMicaEffect,
        bool supportsRoundedCorners,
        bool supportsCaptionColor,
        bool supportsBorderColor = false,
        bool backdropCompositionAvailable = true)
    {
        internal bool SupportsSystemBackdropType { get; private set; } = supportsSystemBackdropType;

        internal bool SupportsMicaEffect { get; private set; } = supportsMicaEffect;

        internal bool SupportsRoundedCorners { get; private set; } = supportsRoundedCorners;

        internal bool SupportsCaptionColor { get; private set; } = supportsCaptionColor;

        internal bool SupportsBorderColor { get; private set; } = supportsBorderColor;

        /// <summary>
        /// Indicates whether DWM will actually composite a Mica or Acrylic backdrop in this
        /// session. False when DWM composition is disabled, when the user has turned off
        /// "Transparency effects" in Settings, or when the primary display adapter is a Microsoft
        /// basic or synthetic adapter (a Hyper-V VM, an RDP session, or the Basic Display Adapter)
        /// on which DWM cannot composite a backdrop. When false, callers must resolve the effective
        /// backdrop to <see cref="BackdropType.None"/> so the window renders opaque instead of
        /// showing an uncomposited surface.
        /// </summary>
        internal bool BackdropCompositionAvailable { get; private set; } = backdropCompositionAvailable;

        internal static WindowCapabilities Current => new(
            OsVersionHelper.SupportsSystemBackdropType,
            OsVersionHelper.SupportsMicaEffect,
            OsVersionHelper.SupportsRoundedCorners,
            OsVersionHelper.SupportsCaptionColor,
            OsVersionHelper.SupportsBorderColor,
            IsBackdropCompositionAvailable());

        private static bool IsBackdropCompositionAvailable()
        {
            return NativeMethods.IsCompositionEnabled()
                && RegistryHelper.IsTransparencyEnabled()
                && !NativeMethods.IsPrimaryDisplayAdapterMicrosoftBasic();
        }
    }
}
