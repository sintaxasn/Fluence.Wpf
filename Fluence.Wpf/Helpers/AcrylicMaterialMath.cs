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

namespace Fluence.Wpf.Helpers
{
    internal static class AcrylicMaterialMath
    {
        // The byte formulas and midpoint behavior follow AcrylicBrush.cpp and
        // ColorConversion.cpp at microsoft/microsoft-ui-xaml commit
        // 874579df106e6bfe60990727684781b3b1541a26.
        internal static byte RoundByte(double value)
        {
            if (!IsFinite(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            double clampedValue = Math.Max(0.0, Math.Min(255.0, value));
            return (byte)Math.Round(clampedValue, MidpointRounding.AwayFromZero);
        }

        internal static double GetTintOpacityModifier(double saturation, double value)
        {
            ValidateUnit(saturation, nameof(saturation));
            ValidateUnit(value, nameof(value));

            double suppression = value <= 0.5
                ? (0.5 - value) * 0.1
                : (value - 0.5) * 0.9;
            return 0.9 - (suppression * Math.Max(1.0 - (2.0 * saturation), 0.0));
        }

        internal static byte GetTintAlpha(
            byte originalAlpha,
            double tintOpacity,
            double saturation,
            double value,
            double? tintLuminosityOpacity)
        {
            ValidateUnit(tintOpacity, nameof(tintOpacity));
            ValidateUnit(saturation, nameof(saturation));
            ValidateUnit(value, nameof(value));
            ValidateOptionalFinite(tintLuminosityOpacity, nameof(tintLuminosityOpacity));

            double modifier = tintLuminosityOpacity is not null
                ? 1.0
                : GetTintOpacityModifier(saturation, value);
            return RoundByte(originalAlpha * tintOpacity * modifier);
        }

        internal static byte GetLuminosityAlpha(
            byte originalAlpha,
            double tintOpacity,
            double? tintLuminosityOpacity)
        {
            ValidateUnit(tintOpacity, nameof(tintOpacity));
            ValidateOptionalFinite(tintLuminosityOpacity, nameof(tintLuminosityOpacity));

            byte roundedTintAlpha = RoundByte(originalAlpha * tintOpacity);
            double luminosityOpacity = tintLuminosityOpacity is double explicitLuminosityOpacity
                ? Clamp(explicitLuminosityOpacity, 0.0, 1.0)
                : Math.Min(0.15 + (0.88 * (roundedTintAlpha / 255.0)), 1.0);
            return RoundByte(luminosityOpacity * 255.0);
        }

        internal static Color GetEffectiveTintColor(
            Color tintColor,
            double tintOpacity,
            double? tintLuminosityOpacity)
        {
            (double _, double saturation, double value) = HsvColorHelper.RgbToHsv(tintColor);
            byte alpha = GetTintAlpha(tintColor.A, tintOpacity, saturation, value, tintLuminosityOpacity);
            return Color.FromArgb(alpha, tintColor.R, tintColor.G, tintColor.B);
        }

        internal static Color GetEffectiveLuminosityColor(
            Color tintColor,
            double tintOpacity,
            double? tintLuminosityOpacity)
        {
            byte alpha = GetLuminosityAlpha(tintColor.A, tintOpacity, tintLuminosityOpacity);
            if (tintLuminosityOpacity is not null)
            {
                return Color.FromArgb(alpha, tintColor.R, tintColor.G, tintColor.B);
            }

            (double hue, double saturation, double value) = HsvColorHelper.RgbToHsv(tintColor);
            double clampedValue = Clamp(value, 0.125, 0.965);
            return HsvToRgb(hue, saturation, clampedValue, alpha);
        }

        private static Color HsvToRgb(double hue, double saturation, double value, byte alpha)
        {
            double normalizedHue = hue % 360.0;
            if (normalizedHue < 0.0)
            {
                normalizedHue += 360.0;
            }

            double chroma = value * saturation;
            double hueSection = normalizedHue / 60.0;
            double secondary = chroma * (1.0 - Math.Abs((hueSection % 2.0) - 1.0));
            double red;
            double green;
            double blue;

            if (hueSection < 1.0)
            {
                red = chroma;
                green = secondary;
                blue = 0.0;
            }
            else if (hueSection < 2.0)
            {
                red = secondary;
                green = chroma;
                blue = 0.0;
            }
            else if (hueSection < 3.0)
            {
                red = 0.0;
                green = chroma;
                blue = secondary;
            }
            else if (hueSection < 4.0)
            {
                red = 0.0;
                green = secondary;
                blue = chroma;
            }
            else if (hueSection < 5.0)
            {
                red = secondary;
                green = 0.0;
                blue = chroma;
            }
            else
            {
                red = chroma;
                green = 0.0;
                blue = secondary;
            }

            double minimum = value - chroma;
            return Color.FromArgb(
                alpha,
                RoundByte((red + minimum) * 255.0),
                RoundByte((green + minimum) * 255.0),
                RoundByte((blue + minimum) * 255.0));
        }

        private static void ValidateUnit(double value, string parameterName)
        {
            if (!IsFinite(value) || value < 0.0 || value > 1.0)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        private static void ValidateOptionalFinite(double? value, string parameterName)
        {
            if (value is double finiteValue && !IsFinite(finiteValue))
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        private static double Clamp(double value, double minimum, double maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
