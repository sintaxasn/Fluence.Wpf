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
using System.Windows.Media;

namespace Fluence.Wpf.Helpers
{
    internal static class HsvColorHelper
    {
        public static (double Hue, double Saturation, double Value) RgbToHsv(Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double hue = 0;
            double saturation = 0;
            double value = max;

            if (delta > 0)
            {
                if (max == r)
                {
                    hue = 60 * (((g - b) / delta) % 6);
                }
                else if (max == g)
                {
                    hue = 60 * (((b - r) / delta) + 2);
                }
                else
                {
                    hue = 60 * (((r - g) / delta) + 4);
                }

                if (hue < 0)
                {
                    hue += 360;
                }

                saturation = max > 0 ? delta / max : 0;
            }

            return (hue, saturation, value);
        }

        public static Color HsvToRgb(double hue, double saturation, double value)
        {
            hue = hue % 360;
            if (hue < 0) hue += 360;

            saturation = Math.Max(0, Math.Min(1, saturation));
            value = Math.Max(0, Math.Min(1, value));

            double c = value * saturation;
            double x = c * (1 - Math.Abs((hue / 60) % 2 - 1));
            double m = value - c;

            double r, g, b;

            if (hue < 60)
            {
                r = c; g = x; b = 0;
            }
            else if (hue < 120)
            {
                r = x; g = c; b = 0;
            }
            else if (hue < 180)
            {
                r = 0; g = c; b = x;
            }
            else if (hue < 240)
            {
                r = 0; g = x; b = c;
            }
            else if (hue < 300)
            {
                r = x; g = 0; b = c;
            }
            else
            {
                r = c; g = 0; b = x;
            }

            return Color.FromRgb(
                (byte)Math.Round((r + m) * 255),
                (byte)Math.Round((g + m) * 255),
                (byte)Math.Round((b + m) * 255));
        }

        public static Color GetLightVariant(Color baseColor, int level)
        {
            var (hue, saturation, value) = RgbToHsv(baseColor);

            double saturationReduction;
            double valueIncrease;

            switch (level)
            {
                case 1:
                    saturationReduction = 0.12;
                    valueIncrease = 0.10;
                    break;
                case 2:
                    saturationReduction = 0.24;
                    valueIncrease = 0.18;
                    break;
                case 3:
                    saturationReduction = 0.36;
                    valueIncrease = 0.26;
                    break;
                default:
                    saturationReduction = 0;
                    valueIncrease = 0;
                    break;
            }

            double newSaturation = Math.Max(saturation - saturationReduction, 0);
            double newValue = Math.Min(value + valueIncrease, 1);

            return HsvToRgb(hue, newSaturation, newValue);
        }

        public static Color GetDarkVariant(Color baseColor, int level)
        {
            var (hue, saturation, value) = RgbToHsv(baseColor);

            double saturationIncrease;
            double valueDecrease;

            switch (level)
            {
                case 1:
                    saturationIncrease = 0.08;
                    valueDecrease = 0.12;
                    break;
                case 2:
                    saturationIncrease = 0.12;
                    valueDecrease = 0.20;
                    break;
                case 3:
                    saturationIncrease = 0.16;
                    valueDecrease = 0.28;
                    break;
                default:
                    saturationIncrease = 0;
                    valueDecrease = 0;
                    break;
            }

            double newSaturation = Math.Min(saturation + saturationIncrease, 1);
            double newValue = Math.Max(value - valueDecrease, 0);

            return HsvToRgb(hue, newSaturation, newValue);
        }

        public static Color Lighten(Color color, double amount)
        {
            var (hue, saturation, value) = RgbToHsv(color);
            double newValue = Math.Min(value + amount, 1);
            return HsvToRgb(hue, saturation, newValue);
        }

        public static Color Darken(Color color, double amount)
        {
            var (hue, saturation, value) = RgbToHsv(color);
            double newValue = Math.Max(value - amount, 0);
            return HsvToRgb(hue, saturation, newValue);
        }

        public static Color WithAlpha(Color color, byte alpha)
        {
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }
    }
}
