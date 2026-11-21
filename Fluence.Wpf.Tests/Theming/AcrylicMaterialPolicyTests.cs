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
using Fluence.Wpf.Helpers;
using Xunit;

namespace Fluence.Wpf.Tests.Theming
{
    public sealed class AcrylicMaterialPolicyTests
    {
        private static readonly Color SystemWindow = Color.FromArgb(64, 171, 205, 239);

        [Fact]
        public void NormalState_UsesMaterial_AndKeepsWindowActivationOutOfPolicy()
        {
            AcrylicPolicySnapshot snapshot = Ready();
            AcrylicPolicyResult result = Evaluate(snapshot);
            Assert.Equal(AcrylicFallbackReason.None, result.Reason);
            Assert.True(result.UseMaterial);
            Assert.Equal(Color.FromRgb(17, 34, 51), result.OpaqueFallback);
            // There is intentionally no IsWindowActive input: deactivation cannot select fallback.
            Assert.Null(typeof(AcrylicPolicySnapshot).GetProperty("IsWindowActive"));
            Assert.Null(typeof(AcrylicPolicySnapshot).GetProperty("ReducedMotion"));
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public void EitherHighContrastSignal_UsesLiveOpaqueSystemWindowColor(bool os, bool app)
        {
            AcrylicPolicySnapshot snapshot = Ready();
            snapshot.OsHighContrast = os;
            snapshot.AppHighContrast = app;
            snapshot.TransparencyEnabled = false;
            snapshot.PowerSaving = true;
            AcrylicMaterialRecipe recipe = Recipe();
            recipe.AlwaysUseFallback = true;
            AcrylicPolicyResult result = AcrylicMaterialPolicy.Evaluate(snapshot, recipe, SystemWindow);
            Assert.Equal(AcrylicFallbackReason.HighContrast, result.Reason);
            Assert.False(result.UseMaterial);
            Assert.Equal(Color.FromRgb(171, 205, 239), result.OpaqueFallback);
        }

        [Theory]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(11)]
        [InlineData(12)]
        public void SourceFailure_ReturnsItsExactReason_AndOpaqueRecipeRgb(int reasonValue)
        {
            AcrylicFallbackReason reason = (AcrylicFallbackReason)reasonValue;
            AcrylicPolicySnapshot snapshot = Ready();
            snapshot.SourceFailure = reason;
            AssertOrdinaryFallback(reason, snapshot);
        }

        [Fact]
        public void IndependentFallbackBranches_ReturnNamedReasons_AndOpaqueRecipeRgb()
        {
            AcrylicPolicySnapshot design = Ready(); design.IsDesignMode = true;
            AssertOrdinaryFallback(AcrylicFallbackReason.DesignMode, design);

            AcrylicPolicySnapshot transparency = Ready(); transparency.TransparencyEnabled = false;
            AssertOrdinaryFallback(AcrylicFallbackReason.TransparencyOff, transparency);

            AcrylicPolicySnapshot saving = Ready(); saving.PowerSaving = true;
            AssertOrdinaryFallback(AcrylicFallbackReason.PowerSaving, saving);

            AcrylicPolicySnapshot unknown = Ready(); unknown.PowerSaving = null;
            AssertOrdinaryFallback(AcrylicFallbackReason.UnknownPower, unknown);

            AcrylicPolicySnapshot graphics = Ready(); graphics.GraphicsSupported = false;
            AssertOrdinaryFallback(AcrylicFallbackReason.UnsupportedGraphics, graphics);

            AcrylicPolicySnapshot blend = Ready(); blend.BlendAvailable = false;
            AssertOrdinaryFallback(AcrylicFallbackReason.BlendUnavailable, blend);

            AcrylicPolicySnapshot device = Ready(); device.DeviceLost = true;
            AssertOrdinaryFallback(AcrylicFallbackReason.DeviceLost, device);

            AcrylicPolicySnapshot budget = Ready(); budget.BudgetExceeded = true;
            AssertOrdinaryFallback(AcrylicFallbackReason.BudgetExceeded, budget);

            AcrylicMaterialRecipe always = Recipe(); always.AlwaysUseFallback = true;
            AcrylicPolicyResult result = AcrylicMaterialPolicy.Evaluate(Ready(), always, SystemWindow);
            Assert.Equal(AcrylicFallbackReason.Always, result.Reason);
            Assert.Equal(Color.FromRgb(17, 34, 51), result.OpaqueFallback);
        }

        [Fact]
        public void Precedence_IsDesignThenAlwaysThenTransparencyThenPowerThenSourceThenGraphics()
        {
            AcrylicPolicySnapshot state = Ready();
            state.IsDesignMode = true;
            state.TransparencyEnabled = false;
            state.PowerSaving = true;
            state.SourceFailure = AcrylicFallbackReason.CyclicSource;
            state.GraphicsSupported = false;
            AcrylicMaterialRecipe recipe = Recipe(); recipe.AlwaysUseFallback = true;
            Assert.Equal(AcrylicFallbackReason.DesignMode, AcrylicMaterialPolicy.Evaluate(state, recipe, SystemWindow).Reason);
            state.IsDesignMode = false;
            Assert.Equal(AcrylicFallbackReason.Always, AcrylicMaterialPolicy.Evaluate(state, recipe, SystemWindow).Reason);
            recipe.AlwaysUseFallback = false;
            Assert.Equal(AcrylicFallbackReason.TransparencyOff, AcrylicMaterialPolicy.Evaluate(state, recipe, SystemWindow).Reason);
            state.TransparencyEnabled = true;
            Assert.Equal(AcrylicFallbackReason.PowerSaving, AcrylicMaterialPolicy.Evaluate(state, recipe, SystemWindow).Reason);
            state.PowerSaving = false;
            Assert.Equal(AcrylicFallbackReason.CyclicSource, AcrylicMaterialPolicy.Evaluate(state, recipe, SystemWindow).Reason);
            state.SourceFailure = AcrylicFallbackReason.None;
            Assert.Equal(AcrylicFallbackReason.UnsupportedGraphics, AcrylicMaterialPolicy.Evaluate(state, recipe, SystemWindow).Reason);
        }

        private static AcrylicPolicySnapshot Ready()
        {
            return new AcrylicPolicySnapshot { PowerSaving = false };
        }

        private static AcrylicMaterialRecipe Recipe()
        {
            return new AcrylicMaterialRecipe
            {
                FallbackColor = Color.FromArgb(64, 17, 34, 51),
            };
        }

        private static AcrylicPolicyResult Evaluate(AcrylicPolicySnapshot snapshot)
        {
            return AcrylicMaterialPolicy.Evaluate(snapshot, Recipe(), SystemWindow);
        }

        private static void AssertOrdinaryFallback(AcrylicFallbackReason reason, AcrylicPolicySnapshot snapshot)
        {
            AcrylicPolicyResult result = Evaluate(snapshot);
            Assert.Equal(reason, result.Reason);
            Assert.False(result.UseMaterial);
            Assert.Equal(Color.FromRgb(17, 34, 51), result.OpaqueFallback);
        }
    }
}
