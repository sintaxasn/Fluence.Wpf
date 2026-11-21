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
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Fluence.Wpf.Helpers
{
    internal sealed class AcrylicMaterialRenderer : IDisposable
    {
        // WinUI uses a Gaussian standard deviation of 30 DIPs. WPF's radius is three times
        // the standard deviation.
        internal const double BlurRadiusDip = 90.0;
        private const double WpfMaxBlurRadiusPx = 100.0;
        // Gaussian variances add. Round 90 / sqrt(2) to 64 because WPF uses an integer radius.
        private const double SplitBlurRadiusDip = 64.0;
        private const int NoiseTileSizeDip = 256;
        private const double NoiseNodeOpacity = 0.02;

        private readonly FrameworkElement _owner;
        private readonly Canvas _outputVisual;
        private FrameworkElement? _previousSource;
        private AcrylicSourceMapResult? _previousMap;
        private AcrylicRecipeSnapshot _previousRecipe;
        private Color _previousFallback;
        private Size _previousSourceSize;
        private bool _hasMaterial;
        private bool _disposed;

        internal AcrylicMaterialRenderer(FrameworkElement owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _outputVisual = new Canvas
            {
                Width = 1.0,
                Height = 1.0,
                Background = new SolidColorBrush(Color.FromRgb(249, 249, 249)),
                ClipToBounds = true,
            };
            Output = new VisualBrush(_outputVisual)
            {
                AutoLayoutContent = false,
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewbox = new Rect(0, 0, 1, 1),
                Stretch = Stretch.Fill,
            };
            Reason = AcrylicFallbackReason.UnavailableSource;
        }

        internal VisualBrush Output { get; }
        internal FrameworkElement OutputVisual => _outputVisual;
        internal AcrylicFallbackReason Reason { get; private set; }
        internal bool IsUsingFallback => Reason is not AcrylicFallbackReason.None;

        internal void Update(FrameworkElement? source, AcrylicSourceMapResult map,
            AcrylicRecipeSnapshot recipe, AcrylicPolicyResult policy)
        {
            ObjectDisposedException.ThrowIf(_disposed, typeof(AcrylicMaterialRenderer));

            ArgumentNullException.ThrowIfNull(map);
            ArgumentNullException.ThrowIfNull(policy);

            AcrylicFallbackReason reason = policy.UseMaterial ? map.Reason : policy.Reason;
            if (reason is not AcrylicFallbackReason.None || source is null)
            {
                SetFallback(source is null && reason is AcrylicFallbackReason.None
                    ? AcrylicFallbackReason.UnavailableSource : reason,
                    policy.OpaqueFallback, map.TargetRect.Size);
                return;
            }

            if (!SupportsHardware())
            {
                SetFallback(AcrylicFallbackReason.UnsupportedGraphics, policy.OpaqueFallback, map.TargetRect.Size);
                return;
            }

            if (_hasMaterial && ReferenceEquals(source, _previousSource)
                && SameMap(map, _previousMap) && recipe.Equals(_previousRecipe)
                && policy.OpaqueFallback.Equals(_previousFallback)
                && source.RenderSize.Equals(_previousSourceSize))
            {
                return;
            }

            try
            {
                BuildMaterial(source, map, recipe, policy.OpaqueFallback);
                _previousSource = source;
                _previousMap = map;
                _previousRecipe = recipe;
                _previousFallback = policy.OpaqueFallback;
                _previousSourceSize = source.RenderSize;
                _hasMaterial = true;
                Reason = AcrylicFallbackReason.None;
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException
                or NotSupportedException or ArgumentException or COMException)
            {
                SetFallback(AcrylicFallbackReason.BlendUnavailable, policy.OpaqueFallback, map.TargetRect.Size);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _outputVisual.Children.Clear();
            Output.Visual = null;
            _previousSource = null;
            _previousMap = null;
            _hasMaterial = false;
        }

        private bool SupportsHardware()
        {
            return RenderOptions.ProcessRenderMode is not RenderMode.SoftwareOnly
                && (RenderCapability.Tier >> 16) >= 2
                && RenderCapability.IsPixelShaderVersionSupported(3, 0)
                && PresentationSource.FromVisual(_owner) is HwndSource hwndSource
                && hwndSource.CompositionTarget is HwndTarget target
                && target.RenderMode is not RenderMode.SoftwareOnly;
        }

        private void BuildMaterial(FrameworkElement source, AcrylicSourceMapResult map,
            AcrylicRecipeSnapshot recipe, Color fallback)
        {
            Color tint = AcrylicMaterialMath.GetEffectiveTintColor(
                recipe.TintColor, recipe.TintOpacity, recipe.TintLuminosityOpacity);
            _outputVisual.Children.Clear();
            _outputVisual.Background = new SolidColorBrush(fallback);
            _outputVisual.Width = map.TargetRect.Width;
            _outputVisual.Height = map.TargetRect.Height;
            Output.Viewbox = map.TargetRect;

            Rectangle material = new()
            {
                Width = map.TargetCrop.Width,
                Height = map.TargetCrop.Height,
            };
            if (tint.A is byte.MaxValue)
            {
                material.Fill = new SolidColorBrush(tint);
            }
            else
            {
                VerifyBlendResource();
                Size paddedSize = map.PaddedTargetRect.Size;
                Visual blurredSource = BuildBlurredSource(source, map, fallback);

                Color luminosity = AcrylicMaterialMath.GetEffectiveLuminosityColor(
                    recipe.TintColor, recipe.TintOpacity, recipe.TintLuminosityOpacity);
                Rectangle luminosityPass = new()
                {
                    Width = paddedSize.Width,
                    Height = paddedSize.Height,
                    Fill = ForVisual(blurredSource, new Rect(paddedSize)),
                    Effect = new AcrylicBlendEffect(CreateColorSampler(luminosity), 0.0),
                };
                ArrangeFinite(luminosityPass, paddedSize);

                Rectangle colorPass = new()
                {
                    Width = paddedSize.Width,
                    Height = paddedSize.Height,
                    Fill = ForVisual(luminosityPass, new Rect(paddedSize)),
                    Effect = new AcrylicBlendEffect(CreateColorSampler(tint), 1.0),
                };
                ArrangeFinite(colorPass, paddedSize);
                Rect cropInPadded = new(
                    map.TargetCrop.X - map.PaddedTargetRect.X,
                    map.TargetCrop.Y - map.PaddedTargetRect.Y,
                    map.TargetCrop.Width, map.TargetCrop.Height);
                material.Fill = ForVisual(colorPass, cropInPadded);
            }

            Canvas.SetLeft(material, map.TargetCrop.X);
            Canvas.SetTop(material, map.TargetCrop.Y);
            _ = _outputVisual.Children.Add(material);

            Rectangle noise = new()
            {
                Width = map.TargetCrop.Width,
                Height = map.TargetCrop.Height,
                Fill = CreateNoiseBrush(),
                Opacity = NoiseNodeOpacity,
            };
            // Match the linear sampling of the desktop WinUI grain as its tile scales.
            RenderOptions.SetBitmapScalingMode(noise, BitmapScalingMode.Linear);
            Canvas.SetLeft(noise, map.TargetCrop.X);
            Canvas.SetTop(noise, map.TargetCrop.Y);
            _ = _outputVisual.Children.Add(noise);
            ArrangeFinite(_outputVisual, map.TargetRect.Size);
        }

        private static Visual BuildBlurredSource(FrameworkElement source, AcrylicSourceMapResult map, Color fallback)
        {
            Size paddedSize = map.PaddedTargetRect.Size;
            // WPF scales its blur radius by the smaller axis before applying its device-pixel cap.
            bool splitBlur = BlurRadiusDip * Math.Min(map.DeviceScaleX, map.DeviceScaleY) > WpfMaxBlurRadiusPx;
            Canvas paddedSource = BuildPaddedSource(source, map, fallback);
            paddedSource.Effect = CreateGaussianBlur(splitBlur ? SplitBlurRadiusDip : BlurRadiusDip);
            ArrangeFinite(paddedSource, paddedSize);
            if (!splitBlur)
            {
                return paddedSource;
            }

            // Limit this to two passes to bound rendering work. At higher scales each pass can still hit the cap.
            Rectangle secondPass = new()
            {
                Width = paddedSize.Width,
                Height = paddedSize.Height,
                Fill = ForVisual(paddedSource, new Rect(paddedSize)),
                Effect = CreateGaussianBlur(SplitBlurRadiusDip),
            };
            ArrangeFinite(secondPass, paddedSize);
            return secondPass;
        }

        private static BlurEffect CreateGaussianBlur(double radiusDip)
        {
            return new BlurEffect
            {
                Radius = radiusDip,
                KernelType = KernelType.Gaussian,
                RenderingBias = RenderingBias.Quality,
            };
        }

        private static Canvas BuildPaddedSource(FrameworkElement source,
            AcrylicSourceMapResult map, Color fallback)
        {
            Rect padded = map.PaddedTargetRect;
            Rect clipped = map.SourceRect;
            Point desiredTopLeft = map.OwnerToSource.Transform(padded.TopLeft);
            Point desiredBottomRight = map.OwnerToSource.Transform(padded.BottomRight);
            Rect desired = new(desiredTopLeft, desiredBottomRight);
            double[] xs = [desired.Left, clipped.Left, clipped.Right, desired.Right];
            double[] ys = [desired.Top, clipped.Top, clipped.Bottom, desired.Bottom];
            double edgeX = Math.Min(clipped.Width, 1.0 / (map.DeviceScaleX * map.SourceToOwner.M11));
            double edgeY = Math.Min(clipped.Height, 1.0 / (map.DeviceScaleY * map.SourceToOwner.M22));
            Canvas result = new()
            {
                Width = padded.Width,
                Height = padded.Height,
                Background = new SolidColorBrush(fallback),
                ClipToBounds = true,
            };

            for (int y = 0; y < 3; y++)
            {
                if (ys[y + 1] <= ys[y])
                {
                    continue;
                }

                for (int x = 0; x < 3; x++)
                {
                    if (xs[x + 1] <= xs[x])
                    {
                        continue;
                    }

                    double sampleX = x is 0 ? clipped.Left : x is 2 ? clipped.Right - edgeX : xs[x];
                    double sampleY = y is 0 ? clipped.Top : y is 2 ? clipped.Bottom - edgeY : ys[y];
                    double sampleWidth = x is 1 ? xs[x + 1] - xs[x] : edgeX;
                    double sampleHeight = y is 1 ? ys[y + 1] - ys[y] : edgeY;
                    Rect sample = new(sampleX, sampleY, sampleWidth, sampleHeight);
                    Point upperLeft = map.SourceToOwner.Transform(new Point(xs[x], ys[y]));
                    Point lowerRight = map.SourceToOwner.Transform(new Point(xs[x + 1], ys[y + 1]));
                    Rectangle tile = new()
                    {
                        Width = lowerRight.X - upperLeft.X,
                        Height = lowerRight.Y - upperLeft.Y,
                        Fill = ForVisual(source, sample),
                    };
                    Canvas.SetLeft(tile, upperLeft.X - padded.X);
                    Canvas.SetTop(tile, upperLeft.Y - padded.Y);
                    _ = result.Children.Add(tile);
                }
            }

            return result;
        }

        private static VisualBrush ForVisual(Visual visual, Rect viewbox)
        {
            return new VisualBrush(visual)
            {
                AutoLayoutContent = false,
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewbox = viewbox,
                Stretch = Stretch.Fill,
            };
        }

        private static ImageBrush CreateNoiseBrush()
        {
            Uri uri = new($"pack://application:,,,/{typeof(AcrylicMaterialRenderer).Assembly.GetName().Name};component/Shaders/AcrylicNoise.png");
            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = uri;
            bitmap.EndInit();
            bitmap.Freeze();
            return new ImageBrush(bitmap)
            {
                TileMode = TileMode.Tile,
                ViewportUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(0, 0, NoiseTileSizeDip, NoiseTileSizeDip),
                Stretch = Stretch.Fill,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top,
            };
        }

        private static ImageBrush CreateColorSampler(Color color)
        {
            byte[] pixel =
            [
                (byte)(((color.B * color.A) + 127) / 255),
                (byte)(((color.G * color.A) + 127) / 255),
                (byte)(((color.R * color.A) + 127) / 255),
                color.A,
            ];
            BitmapSource bitmap = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Pbgra32,
                palette: null, pixel, 4);
            bitmap.Freeze();
            ImageBrush brush = new(bitmap) { Stretch = Stretch.Fill };
            brush.Freeze();
            return brush;
        }

        private static void VerifyBlendResource()
        {
            Uri uri = new($"/{typeof(AcrylicMaterialRenderer).Assembly.GetName().Name};component/Shaders/AcrylicBlend.ps", UriKind.Relative);
            System.Windows.Resources.StreamResourceInfo resource = Application.GetResourceStream(uri)
                ?? throw new IOException("The packaged acrylic blend shader is unavailable.");
            resource.Stream.Close();
        }

        private void SetFallback(AcrylicFallbackReason reason, Color color, Size targetSize)
        {
            if (!_hasMaterial && Reason is not AcrylicFallbackReason.None && Reason == reason
                && _previousFallback.Equals(color)
                && _outputVisual.Width.Equals(targetSize.Width)
                && _outputVisual.Height.Equals(targetSize.Height))
            {
                return;
            }

            _outputVisual.Children.Clear();
            _outputVisual.Background = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
            _outputVisual.Width = IsFinitePositive(targetSize.Width) ? targetSize.Width : 1.0;
            _outputVisual.Height = IsFinitePositive(targetSize.Height) ? targetSize.Height : 1.0;
            Output.Viewbox = new Rect(0, 0, _outputVisual.Width, _outputVisual.Height);
            ArrangeFinite(_outputVisual, new Size(_outputVisual.Width, _outputVisual.Height));
            Reason = reason;
            _previousFallback = color;
            _previousSource = null;
            _previousMap = null;
            _hasMaterial = false;
        }

        private static bool SameMap(AcrylicSourceMapResult candidate, AcrylicSourceMapResult? previous)
        {
            return previous is not null
                && candidate.Reason == previous.Reason
                && candidate.SourceToOwner.Equals(previous.SourceToOwner)
                && candidate.OwnerToSource.Equals(previous.OwnerToSource)
                && candidate.TargetRect.Equals(previous.TargetRect)
                && candidate.PaddedTargetRect.Equals(previous.PaddedTargetRect)
                && candidate.SourceRect.Equals(previous.SourceRect)
                && candidate.TargetCrop.Equals(previous.TargetCrop)
                && candidate.DeviceScaleX.Equals(previous.DeviceScaleX)
                && candidate.DeviceScaleY.Equals(previous.DeviceScaleY)
                && candidate.PhysicalPadX == previous.PhysicalPadX
                && candidate.PhysicalPadY == previous.PhysicalPadY;
        }

        private static void ArrangeFinite(FrameworkElement element, Size size)
        {
            element.Measure(size);
            element.Arrange(new Rect(size));
            element.UpdateLayout();
        }

        private static bool IsFinitePositive(double number)
        {
            return number > 0 && !double.IsInfinity(number) && !double.IsNaN(number);
        }
    }
}
