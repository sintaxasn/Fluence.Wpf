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
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Fluence.Wpf.Helpers;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Control
{
    public sealed class AcrylicMaterialRendererTests
    {
        [Fact]
        public Task PackagedPs30BlendBytecodeLoadsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Uri uri = new("pack://application:,,,/Fluence.Wpf;component/Shaders/AcrylicBlend.ps");
                System.Windows.Resources.StreamResourceInfo? resource = Application.GetResourceStream(uri);
                Assert.NotNull(resource);
                Assert.True(resource.Stream.Length > 0);
            });
        }

        [Fact]
        public void ColorModeBlackAndHalfWhiteIsBlack()
        {
            AcrylicBlendOracle.Result result = AcrylicBlendOracle.Blend(
                Colors.Black, Color.FromArgb(128, 255, 255, 255), AcrylicBlendOracle.BlendMode.Color);
            Assert.Equal(Colors.Black, result.Straight);
            Assert.Equal((byte)0, result.RedPremul);
            Assert.Equal((byte)0, result.GreenPremul);
            Assert.Equal((byte)0, result.BluePremul);
        }

        [Fact]
        public void PartialAlphaRetainsPremultipliedChannels()
        {
            AcrylicBlendOracle.Result result = AcrylicBlendOracle.Blend(
                Color.FromArgb(64, 0, 0, 255), Color.FromArgb(192, 255, 0, 0), AcrylicBlendOracle.BlendMode.Color);
            Assert.Equal((byte)208, result.Straight.A);
            Assert.Equal((byte)161, result.RedPremul);
            Assert.Equal((byte)0, result.GreenPremul);
            Assert.Equal((byte)16, result.BluePremul);
        }

        [Fact]
        public void ZeroAlphaInputsProduceZeroRgb()
        {
            AcrylicBlendOracle.Result result = AcrylicBlendOracle.Blend(
                Colors.Transparent, Colors.Transparent, AcrylicBlendOracle.BlendMode.Luminosity);
            Assert.Equal(Color.FromArgb(0, 0, 0, 0), result.Straight);
            Assert.Equal((byte)0, result.RedPremul);
            Assert.Equal((byte)0, result.GreenPremul);
            Assert.Equal((byte)0, result.BluePremul);
        }

        [Fact]
        public void AllIndependentBlendGoldensMatchCpuOracle()
        {
            using Stream stream = typeof(AcrylicMaterialRendererTests).Assembly.GetManifestResourceStream(
                "Fluence.Wpf.Tests.Assets.Acrylic.blend-goldens.json")
                ?? throw new InvalidOperationException("The embedded acrylic blend goldens are missing.");
            DataContractJsonSerializer serializer = new(typeof(BlendGoldenDocument));
            BlendGoldenDocument document = (BlendGoldenDocument)serializer.ReadObject(stream)!;
            Assert.Equal(11, document.Cases.Length);
            foreach (BlendGoldenCase golden in document.Cases)
            {
                AcrylicBlendOracle.BlendMode mode = string.Equals(golden.Mode, "color", StringComparison.Ordinal)
                    ? AcrylicBlendOracle.BlendMode.Color : AcrylicBlendOracle.BlendMode.Luminosity;
                AcrylicBlendOracle.Result actual = AcrylicBlendOracle.Blend(
                    ParseArgb(golden.Background), ParseArgb(golden.Foreground), mode);
                if (string.Equals(golden.Mode, "luminosity-then-color", StringComparison.Ordinal))
                {
                    actual = AcrylicBlendOracle.Blend(actual.Straight,
                        ParseArgb(golden.SecondForeground), AcrylicBlendOracle.BlendMode.Color);
                }

                Color expected = ParseArgb(golden.Output);
                Color expectedPremul = ParseArgb("#FF" + golden.OutputPremul[1..]);
                AssertChannels(golden.Name + " straight", actual.Straight, expected, document.ToleranceRgbBytes);
                Assert.True(Math.Abs(actual.RedPremul - expectedPremul.R) <= document.ToleranceRgbBytes,
                    golden.Name + " premultiplied red");
                Assert.True(Math.Abs(actual.GreenPremul - expectedPremul.G) <= document.ToleranceRgbBytes,
                    golden.Name + " premultiplied green");
                Assert.True(Math.Abs(actual.BluePremul - expectedPremul.B) <= document.ToleranceRgbBytes,
                    golden.Name + " premultiplied blue");
            }
        }

        private static void AssertChannels(string name, Color actual, Color expected, int tolerance)
        {
            Assert.True(Math.Abs(actual.A - expected.A) <= tolerance, name + " alpha");
            Assert.True(Math.Abs(actual.R - expected.R) <= tolerance, name + " red");
            Assert.True(Math.Abs(actual.G - expected.G) <= tolerance, name + " green");
            Assert.True(Math.Abs(actual.B - expected.B) <= tolerance, name + " blue");
        }

        private static Color ParseArgb(string value)
        {
            Assert.Equal(9, value.Length);
            return Color.FromArgb(
                byte.Parse(value.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(value.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(value.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(value.Substring(7, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
        }

        [DataContract]
        private sealed class BlendGoldenDocument
        {
            [DataMember(Name = "toleranceRgbBytes")]
            public int ToleranceRgbBytes { get; set; }

            [DataMember(Name = "cases")]
            public BlendGoldenCase[] Cases { get; set; } = [];
        }

        [DataContract]
        private sealed class BlendGoldenCase
        {
            [DataMember(Name = "name")]
            public string Name { get; set; } = string.Empty;

            [DataMember(Name = "mode")]
            public string Mode { get; set; } = string.Empty;

            [DataMember(Name = "background")]
            public string Background { get; set; } = string.Empty;

            [DataMember(Name = "foreground")]
            public string Foreground { get; set; } = string.Empty;

            [DataMember(Name = "secondForeground")]
            public string SecondForeground { get; set; } = string.Empty;

            [DataMember(Name = "output")]
            public string Output { get; set; } = string.Empty;

            [DataMember(Name = "outputPremul")]
            public string OutputPremul { get; set; } = string.Empty;
        }

        [Fact]
        public Task MaterialPolicyBuildsPerConsumerRetainedOutputAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = WpfTestSta.EnsureApplication();
                Window window = new() { Width = 320, Height = 220, ShowInTaskbar = false, ShowActivated = false };
                Grid root = new();
                Border source = new()
                {
                    Width = 120,
                    Height = 120,
                    Background = Brushes.Blue,
                    HorizontalAlignment = HorizontalAlignment.Left,
                };
                Border owner = new()
                {
                    Width = 80,
                    Height = 80,
                    HorizontalAlignment = HorizontalAlignment.Right,
                };
                _ = root.Children.Add(source);
                _ = root.Children.Add(owner);
                try
                {
                    window.Content = root;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    AcrylicRecipeSnapshot recipe = AcrylicRecipeSnapshot.Create(new AcrylicMaterialRecipe());
                    AcrylicPolicyResult policy = new(AcrylicFallbackReason.None, Colors.Black);
                    using AcrylicMaterialRenderer renderer = new(owner);
                    (double Scale, double RadiusDip, bool SplitBlur)[] cases = [(1.0, 90.0, false), (1.5, 64.0, true)];
                    foreach ((double scale, double radiusDip, bool splitBlur) in cases)
                    {
                        AcrylicSourceMapResult map = AcrylicSourceMap.CalculateGeometry(
                            Matrix.Identity, new Rect(source.RenderSize), owner.RenderSize,
                            AcrylicMaterialRenderer.BlurRadiusDip, new Matrix(scale, 0, 0, scale, 0, 0));
                        renderer.Update(source, map, recipe, policy);
                        Assert.Same(renderer.OutputVisual, renderer.Output.Visual);
                        if (RenderCapability.IsPixelShaderVersionSupported(3, 0)
                            && (RenderCapability.Tier >> 16) >= 2
                            && RenderOptions.ProcessRenderMode is not System.Windows.Interop.RenderMode.SoftwareOnly)
                        {
                            Assert.Equal(AcrylicFallbackReason.None, renderer.Reason);
                            Assert.False(renderer.IsUsingFallback);
                            AssertBlurPasses(renderer.OutputVisual, radiusDip, splitBlur);
                        }
                        else
                        {
                            Assert.Equal(AcrylicFallbackReason.UnsupportedGraphics, renderer.Reason);
                            Assert.True(renderer.IsUsingFallback);
                        }
                    }
                }
                finally
                {
                    VisualTree.CloseWindowAndDrain(window);
                }
            });
        }

        private static void AssertBlurPasses(FrameworkElement outputVisual, double radiusDip, bool splitBlur)
        {
            Canvas output = Assert.IsType<Canvas>(outputVisual);
            Rectangle material = Assert.IsType<Rectangle>(output.Children[0]);
            Rectangle colorPass = Assert.IsType<Rectangle>(Assert.IsType<VisualBrush>(material.Fill).Visual);
            Rectangle luminosityPass = Assert.IsType<Rectangle>(Assert.IsType<VisualBrush>(colorPass.Fill).Visual);
            Visual? blurredSource = Assert.IsType<VisualBrush>(luminosityPass.Fill).Visual;
            if (splitBlur)
            {
                Rectangle secondPass = Assert.IsType<Rectangle>(blurredSource);
                AssertGaussianBlur(secondPass, radiusDip);
                blurredSource = Assert.IsType<VisualBrush>(secondPass.Fill).Visual;
            }

            Canvas paddedSource = Assert.IsType<Canvas>(blurredSource);
            AssertGaussianBlur(paddedSource, radiusDip);
        }

        private static void AssertGaussianBlur(UIElement element, double radiusDip)
        {
            BlurEffect blur = Assert.IsType<BlurEffect>(element.Effect);
            Assert.Equal(radiusDip, blur.Radius);
            Assert.Equal(KernelType.Gaussian, blur.KernelType);
        }
    }
}
