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
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Fluence.Wpf.Helpers
{
    // Mode 0 changes the backdrop's luminosity. Mode 1 changes its color.
    // Input is the WPF effect target at s0; Foreground is s1.
    internal sealed class AcrylicBlendEffect : ShaderEffect
    {
        internal static readonly DependencyProperty InputProperty =
            RegisterPixelShaderSamplerProperty(nameof(Input), typeof(AcrylicBlendEffect), 0);

        internal static readonly DependencyProperty ForegroundProperty =
            RegisterPixelShaderSamplerProperty(nameof(Foreground), typeof(AcrylicBlendEffect), 1);

        internal static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
            nameof(Mode), typeof(double), typeof(AcrylicBlendEffect),
            new UIPropertyMetadata(0.0, PixelShaderConstantCallback(0)));

        internal AcrylicBlendEffect(Brush foreground, double mode)
        {
            PixelShader = new PixelShader
            {
                UriSource = new Uri($"/{typeof(AcrylicBlendEffect).Assembly.GetName().Name};component/Shaders/AcrylicBlend.ps", UriKind.Relative),
            };
            Foreground = foreground;
            Mode = mode;
            UpdateShaderValue(InputProperty);
            UpdateShaderValue(ForegroundProperty);
            UpdateShaderValue(ModeProperty);
        }

        internal Brush Input
        {
            get => (Brush)GetValue(InputProperty);
            set => SetValue(InputProperty, value);
        }

        internal Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        internal double Mode
        {
            get => (double)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }
    }
}
