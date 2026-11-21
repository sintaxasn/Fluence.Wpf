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

using System.Windows.Media;

namespace Fluence.Wpf.Helpers
{
    internal static class AcrylicMaterialPolicy
    {
        internal static AcrylicPolicyResult Evaluate(
            AcrylicPolicySnapshot snapshot, AcrylicMaterialRecipe recipe, Color systemWindowColor)
        {
            if (snapshot.OsHighContrast || snapshot.AppHighContrast)
            {
                return new AcrylicPolicyResult(AcrylicFallbackReason.HighContrast, systemWindowColor);
            }

            Color fallback = recipe.FallbackColor;
            if (snapshot.IsDesignMode)
            {
                return new AcrylicPolicyResult(AcrylicFallbackReason.DesignMode, fallback);
            }
            if (recipe.AlwaysUseFallback)
            {
                return new AcrylicPolicyResult(AcrylicFallbackReason.Always, fallback);
            }
            if (!snapshot.TransparencyEnabled)
            {
                return new AcrylicPolicyResult(AcrylicFallbackReason.TransparencyOff, fallback);
            }
            if (snapshot.PowerSaving is true)
            {
                return new AcrylicPolicyResult(AcrylicFallbackReason.PowerSaving, fallback);
            }
            if (snapshot.PowerSaving is null)
            {
                return new AcrylicPolicyResult(AcrylicFallbackReason.UnknownPower, fallback);
            }
            if (snapshot.SourceFailure is not AcrylicFallbackReason.None)
            {
                return new AcrylicPolicyResult(snapshot.SourceFailure, fallback);
            }
            if (!snapshot.GraphicsSupported)
            {
                return new AcrylicPolicyResult(AcrylicFallbackReason.UnsupportedGraphics, fallback);
            }
            if (!snapshot.BlendAvailable)
            {
                return new AcrylicPolicyResult(AcrylicFallbackReason.BlendUnavailable, fallback);
            }
            AcrylicFallbackReason finalReason = AcrylicFallbackReason.None;
            if (snapshot.DeviceLost)
            {
                finalReason = AcrylicFallbackReason.DeviceLost;
            }
            else if (snapshot.BudgetExceeded)
            {
                finalReason = AcrylicFallbackReason.BudgetExceeded;
            }

            return new AcrylicPolicyResult(finalReason, fallback);
        }
    }
}
