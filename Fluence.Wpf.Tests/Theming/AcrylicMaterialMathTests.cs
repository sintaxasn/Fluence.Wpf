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
using System.Windows.Media;
using Fluence.Wpf.Helpers;
using Xunit;

namespace Fluence.Wpf.Tests.Theming
{
    /// <summary>
    /// Checks pinned WinUI AcrylicBrush byte math with independent literal oracles.
    /// The source commit is 874579df106e6bfe60990727684781b3b1541a26.
    /// </summary>
    public sealed class AcrylicMaterialMathTests
    {
        [Theory]
        [InlineData(0.0, 0.0, 173)]
        [InlineData(0.0, 0.125, 176)]
        [InlineData(0.0, 0.5, 184)]
        [InlineData(0.0, 0.965, 98)]
        [InlineData(0.0, 1.0, 92)]
        [InlineData(0.25, 0.0, 179)]
        [InlineData(0.25, 0.125, 180)]
        [InlineData(0.25, 0.5, 184)]
        [InlineData(0.25, 0.965, 141)]
        [InlineData(0.25, 1.0, 138)]
        [InlineData(0.5, 0.0, 184)]
        [InlineData(0.5, 0.125, 184)]
        [InlineData(0.5, 0.5, 184)]
        [InlineData(0.5, 0.965, 184)]
        [InlineData(0.5, 1.0, 184)]
        [InlineData(1.0, 0.0, 184)]
        [InlineData(1.0, 0.125, 184)]
        [InlineData(1.0, 0.5, 184)]
        [InlineData(1.0, 0.965, 184)]
        [InlineData(1.0, 1.0, 184)]
        public void AutomaticTintAlpha_MatchesPinnedSourceGolden(double saturation, double value, byte expectedAlpha)
        {
            Assert.Equal(expected: expectedAlpha, actual: AcrylicMaterialMath.GetTintAlpha(originalAlpha: 204, tintOpacity: 1.0, saturation: saturation, value: value, tintLuminosityOpacity: null));
            Assert.Equal(expected: (byte)218, actual: AcrylicMaterialMath.GetLuminosityAlpha(originalAlpha: 204, tintOpacity: 1.0, tintLuminosityOpacity: null));
        }

        [Theory]
        [InlineData(255, 255, 255, 246, 246, 246)]
        [InlineData(0, 0, 0, 32, 32, 32)]
        [InlineData(255, 0, 0, 246, 0, 0)]
        public void AutomaticLuminosityRgb_ClampsValueAtBlackAndWhiteEndpoints(byte red, byte green, byte blue, byte expectedRed, byte expectedGreen, byte expectedBlue)
        {
            Color tint = Color.FromArgb(204, red, green, blue);
            Color expected = Color.FromArgb(218, expectedRed, expectedGreen, expectedBlue);
            Assert.Equal(expected: expected, actual: AcrylicMaterialMath.GetEffectiveLuminosityColor(tintColor: tint, tintOpacity: 1.0, tintLuminosityOpacity: null));
        }

        [Theory]
        [InlineData(0.0, 1.0, 0.45)]
        [InlineData(0.0, 0.0, 0.85)]
        [InlineData(0.0, 0.5, 0.9)]
        [InlineData(0.25, 1.0, 0.675)]
        [InlineData(0.5, 1.0, 0.9)]
        [InlineData(1.0, 0.0, 0.9)]
        public void TintOpacityModifier_MatchesPiecewiseSourceFormula(double saturation, double value, double expected)
        {
            Assert.Equal(expected: expected, actual: AcrylicMaterialMath.GetTintOpacityModifier(saturation: saturation, value: value), precision: 12);
        }

        [Fact]
        public void AutomaticTintScalesAlphaAndPreservesOriginalRgb()
        {
            Color tint = Color.FromArgb(204, 255, 255, 255);
            Assert.Equal(expected: Color.FromArgb(92, 255, 255, 255), actual: AcrylicMaterialMath.GetEffectiveTintColor(tintColor: tint, tintOpacity: 1.0, tintLuminosityOpacity: null));
        }
        [Fact]
        public void ExplicitLuminosityBypassesModifierAndPreservesTintRgb()
        {
            Color tint = Color.FromArgb(204, 17, 139, 241);
            Assert.Equal(expected: Color.FromArgb(204, 17, 139, 241), actual: AcrylicMaterialMath.GetEffectiveTintColor(tintColor: tint, tintOpacity: 1.0, tintLuminosityOpacity: 0.96));
            Assert.Equal(expected: Color.FromArgb(245, 17, 139, 241), actual: AcrylicMaterialMath.GetEffectiveLuminosityColor(tintColor: tint, tintOpacity: 1.0, tintLuminosityOpacity: 0.96));
        }

        [Theory]
        [InlineData(-0.25, 0)]
        [InlineData(1.25, 255)]
        public void ExplicitLuminosityClampsToUnitInterval(double opacity, byte expectedAlpha)
        {
            Color tint = Color.FromArgb(204, 17, 139, 241);
            Assert.Equal(expected: expectedAlpha, actual: AcrylicMaterialMath.GetLuminosityAlpha(originalAlpha: tint.A, tintOpacity: 1.0, tintLuminosityOpacity: opacity));
        }

        [Theory]
        [InlineData(2.5, 3)]
        [InlineData(3.5, 4)]
        [InlineData(10.5, 11)]
        public void RoundByteUsesHalfAwayFromZero(double value, byte expected)
        {
            Assert.Equal(expected: expected, actual: AcrylicMaterialMath.RoundByte(value: value));
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void RoundByteRejectsNonFiniteInput(double value)
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => AcrylicMaterialMath.RoundByte(value: value));
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void TintMathRejectsNonFiniteUnitInputs(double value)
        {
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => AcrylicMaterialMath.GetTintAlpha(originalAlpha: 204, tintOpacity: value, saturation: 0.25, value: 0.5, tintLuminosityOpacity: null));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => AcrylicMaterialMath.GetTintAlpha(originalAlpha: 204, tintOpacity: 1.0, saturation: value, value: 0.5, tintLuminosityOpacity: null));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => AcrylicMaterialMath.GetTintAlpha(originalAlpha: 204, tintOpacity: 1.0, saturation: 0.25, value: value, tintLuminosityOpacity: null));
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => AcrylicMaterialMath.GetLuminosityAlpha(originalAlpha: 204, tintOpacity: 1.0, tintLuminosityOpacity: value));
        }

        [Fact]
        public void TransparentTintProducesZeroEffectiveTintAlpha()
        {
            Color tint = Color.FromArgb(0, 15, 83, 211);
            Assert.Equal(expected: (byte)0, actual: AcrylicMaterialMath.GetEffectiveTintColor(tintColor: tint, tintOpacity: 1.0, tintLuminosityOpacity: null).A);
        }

        [Fact]
        public void OpaqueTintShortcutCanBeDetectedFromEffectiveAlpha()
        {
            Color tint = Color.FromArgb(255, 1, 2, 3);
            Assert.Equal(expected: (byte)255, actual: AcrylicMaterialMath.GetEffectiveTintColor(tintColor: tint, tintOpacity: 1.0, tintLuminosityOpacity: 1.0).A);
        }
    }
}
