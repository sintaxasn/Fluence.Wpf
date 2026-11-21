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
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace Fluence.Wpf.Tests.Control
{
    // Independent test-only scalar implementation of the specified Direct2D
    // nonseparable equations. No production helper, shader bytecode, or renderer
    // method is called here. The pinned literals live in blend-goldens.json.
    internal static class AcrylicBlendOracle
    {
        internal enum BlendMode { Luminosity = 0, Color = 1 }

        [StructLayout(LayoutKind.Auto)]
        internal readonly struct Result
        {
            internal Result(Color straight, byte redPremul, byte greenPremul, byte bluePremul)
            {
                Straight = straight;
                RedPremul = redPremul;
                GreenPremul = greenPremul;
                BluePremul = bluePremul;
            }

            internal Color Straight { get; }
            internal byte RedPremul { get; }
            internal byte GreenPremul { get; }
            internal byte BluePremul { get; }
        }

        internal static Result Blend(Color backdrop, Color foreground, BlendMode mode)
        {
            double[] backdropRgb = Rgb(backdrop);
            double[] foregroundRgb = Rgb(foreground);
            double backdropAlpha = backdrop.A / 255.0;
            double foregroundAlpha = foreground.A / 255.0;
            double[] shifted = mode is BlendMode.Luminosity
                ? ChangeLuminance(backdropRgb, Luminance(foregroundRgb))
                : ChangeLuminance(foregroundRgb, Luminance(backdropRgb));

            double alpha = foregroundAlpha + (backdropAlpha * (1.0 - foregroundAlpha));
            double[] premul = new double[3];
            for (int channel = 0; channel < 3; channel++)
            {
                premul[channel] = (shifted[channel] * foregroundAlpha * backdropAlpha)
                    + (foregroundRgb[channel] * foregroundAlpha * (1.0 - backdropAlpha))
                    + (backdropRgb[channel] * backdropAlpha * (1.0 - foregroundAlpha));
            }

            Color straight = alpha < 0.000001
                ? Color.FromArgb(0, 0, 0, 0)
                : Color.FromArgb(Byte(alpha), Byte(premul[0] / alpha),
                    Byte(premul[1] / alpha), Byte(premul[2] / alpha));
            return new Result(straight, Byte(premul[0]), Byte(premul[1]), Byte(premul[2]));
        }

        internal static Color OverOpaque(Result input, Color background)
        {
            double remaining = 1.0 - (input.Straight.A / 255.0);
            return Color.FromRgb(
                Byte((input.RedPremul / 255.0) + (background.R / 255.0 * remaining)),
                Byte((input.GreenPremul / 255.0) + (background.G / 255.0 * remaining)),
                Byte((input.BluePremul / 255.0) + (background.B / 255.0 * remaining)));
        }

        private static double[] Rgb(Color color)
        {
            return [color.R / 255.0, color.G / 255.0, color.B / 255.0];
        }

        private static double Luminance(double[] color)
        {
            return (0.30 * color[0]) + (0.59 * color[1]) + (0.11 * color[2]);
        }

        private static double[] ChangeLuminance(double[] color, double target)
        {
            double difference = target - Luminance(color);
            double[] result = [color[0] + difference, color[1] + difference, color[2] + difference];

            double low = Math.Min(result[0], Math.Min(result[1], result[2]));
            double high = Math.Max(result[0], Math.Max(result[1], result[2]));
            double luminance = Luminance(result);
            if (low < 0.0)
            {
                double scale = luminance / Math.Max(luminance - low, 0.000001);
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = luminance + ((result[i] - luminance) * scale);
                }
            }

            if (high > 1.0)
            {
                double scale = (1.0 - luminance) / Math.Max(high - luminance, 0.000001);
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = luminance + ((result[i] - luminance) * scale);
                }
            }

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = Math.Max(0.0, Math.Min(1.0, result[i]));
            }

            return result;
        }

        private static byte Byte(double unit)
        {
            return (byte)Math.Round(Math.Max(0.0, Math.Min(1.0, unit)) * 255.0,
                MidpointRounding.AwayFromZero);
        }
    }
}
