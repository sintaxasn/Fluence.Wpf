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
using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Fluence.Wpf.Helpers
{
    /// <summary>
    /// Estimates the average color of the desktop wallpaper (or solid desktop background) and
    /// models how a Windows 11 Mica surface would tint toward it, so
    /// <c>NavigationViewContentBackground</c> can be recomputed at theme-apply time from the live
    /// desktop instead of a fixed neutral-wallpaper value. DWM's Mica compositor blends the
    /// wallpaper directly; a translucent WPF layer above <c>DwmExtendFrameIntoClientArea</c> does
    /// not composite correctly (see <c>KNOWN_ISSUES.md</c>, "DWM glass-blend distorts translucent
    /// overlays above Mica"), so the result here is folded into the existing opaque pre-blend
    /// instead of being applied as WPF-side translucency.
    /// </summary>
    internal static class WallpaperTintHelper
    {
        // ITU-R BT.601 (NTSC) luma weights. Used only to separate a wallpaper color into an
        // achromatic "brightness" component and a chromatic residual; any standard luma weighting
        // works for this purpose, this one was chosen because it is the most commonly cited.
        private const double LumaR = 0.299;
        private const double LumaG = 0.587;
        private const double LumaB = 0.114;

        // Fitted against the 8 measured WinUI 3 Gallery pane (raw Mica) rows below (wallpaper
        // colors extracted from F:\Images\WinUI_Light_*.png corner patches; White/Red/Orange/Yellow
        // desktops in both Light and Dark). Model: the tint is the wallpaper's chroma residual
        // (color minus its own luma) added to the theme's neutral base, scaled by a weight that
        // decays with how far the wallpaper's luma sits from the base's luma - closer luma tints
        // more strongly, matching the measured shape (Light: Yellow, close to the bright #F3F3F3
        // base, shifts blue by 49; Red, far darker, shifts only a few levels. Dark: Red, close to
        // the near-black #202020 base, lifts red by 14; Yellow, far brighter, shifts only a few
        // levels). An achromatic wallpaper has a zero chroma residual by construction, so the
        // model passes through to the exact base color regardless of the wallpaper's brightness -
        // this is what keeps a neutral desktop pinned to the existing #F9F9F9 / #272727 values.
        //
        // Measured pane (raw Mica) rows and the achieved fit, verified by direct execution of
        // EstimateMicaColor/ComputeContentBackground against these inputs (paneEstimate/
        // contentEstimate - measured, per channel; see WallpaperTintHelperTests for the assertions):
        //   Light base #F3F3F3 (243,243,243):
        //     White  (255,255,255) -> pane (243,243,243) err (0,0,0)     content (249,249,249) err (0,0,0)
        //     Red    (232,17,35)   -> pane (249,240,240) err (0,0,-1)    content (252,247,247) err (0,-1,-1)
        //     Orange (255,140,0)   -> pane (252,241,227) err (+3,-1,-6)  content (253,248,241) err (+1,-1,-3)
        //     Yellow (255,255,0)   -> pane (249,249,196) err (0,0,+2)    content (252,252,225) err (0,0,0)
        //   Dark base #202020 (32,32,32):
        //     White  (255,255,255) -> pane (32,32,32) err (0,0,0)       content (39,39,39) err (0,0,0)
        //     Red    (232,17,35)   -> pane (46,25,27) err (0,-1,0)      content (49,34,36) err (0,-1,0)
        //     Orange (255,140,0)   -> pane (37,31,23) err (+1,0,-3)     content (43,39,33) err (+1,0,-2)
        //     Yellow (255,255,0)   -> pane (32,32,24) err (-1,-1,-2)    content (39,39,34) err (-1,-1,-1)
        // The pane-row errors run up to +/-6 (Light Orange, blue channel); PreBlend's alpha
        // weighting (0.5 light / 0.298 dark) roughly halves that on the way to the content layer,
        // so every content-layer row stays within +/-3 of measured - well inside the +/-6
        // per-channel tolerance the fit spec requires on CONTENT rows. The pane-row checks in
        // WallpaperTintHelperTests use a looser bound (they are a diagnostic cross-check of this
        // intermediate step, not the spec's actual contract) so a sub-ULP difference in Math.Exp
        // between runtimes cannot flip a near-boundary pane assertion.
        private const double LightTintStrength = 0.25;
        private const double LightTintFalloff = 90.0;
        private const double DarkTintStrength = 0.14;
        private const double DarkTintFalloff = 130.0;

        // The canonical WinUI LayerFillColorDefault values this pre-blend approximates:
        // #80FFFFFF (light) and #4C3A3A3A (dark). PreBlendContent composites this layer, at its
        // real alpha, over the estimated Mica color using integer "over" math and a truncating
        // (not rounding) final conversion - truncation is what reproduces the committed neutral
        // values #F9F9F9 / #272727 exactly (0.298*58 + 0.702*32 = 39.75, which truncates to 39,
        // matching Theme.Dark.xaml, but would round to 40).
        private const byte LightLayerAlpha = 0x80;
        private const byte LightLayerChannel = 0xFF;
        private const byte DarkLayerAlpha = 0x4C;
        private const byte DarkLayerChannel = 0x3A;

        private static Color? _cachedColor;
        private static string? _cachedPath;
        private static DateTime _cachedWriteTimeUtc;
        private static bool _cacheIsSolidColor;
        private static bool _hasCache;

        /// <summary>
        /// Gets or sets the test seam. When non-null, <see cref="GetWallpaperAverageColor"/>
        /// returns this value without touching the file system, registry, or Win32 APIs. Reset to
        /// <see langword="null"/> in test cleanup. Mirrors the
        /// <see cref="MotionHelper.OverrideIsMotionEnabled"/> precedent.
        /// </summary>
        internal static Color? OverrideAverageColor { get; set; }

        /// <summary>
        /// Returns the average color of the current desktop wallpaper, or the solid desktop
        /// background color when no wallpaper file is set. Returns <see langword="null"/> on any
        /// failure so callers can fall back to a fixed neutral value.
        /// </summary>
        internal static Color? GetWallpaperAverageColor()
        {
            if (OverrideAverageColor is Color overrideColor)
            {
                return overrideColor;
            }

            // Best-effort by design: any failure reading the wallpaper path, the file, or the
            // registry fallback must degrade to null (keep the neutral XAML default), never throw
            // out of a theme-apply pipeline call.
            try
            {
                string? path = NativeMethods.GetDesktopWallpaperPath();
                if (path is null || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    return GetSolidBackgroundColorCached();
                }

                DateTime writeTimeUtc = File.GetLastWriteTimeUtc(path);
                if (_hasCache && !_cacheIsSolidColor
                    && string.Equals(_cachedPath, path, StringComparison.OrdinalIgnoreCase)
                    && _cachedWriteTimeUtc == writeTimeUtc)
                {
                    return _cachedColor;
                }

                Color? averaged = AverageBitmapPixels(path);
                _cachedPath = path;
                _cachedWriteTimeUtc = writeTimeUtc;
                _cachedColor = averaged;
                _cacheIsSolidColor = false;
                _hasCache = true;
                return averaged;
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                return null;
            }
        }

        /// <summary>
        /// Invalidates the cached wallpaper/background color so the next
        /// <see cref="GetWallpaperAverageColor"/> call re-reads the file system and registry.
        /// Called by <see cref="SystemThemeWatcher"/> when a wallpaper change broadcast arrives.
        /// </summary>
        internal static void InvalidateCache()
        {
            _hasCache = false;
            _cachedColor = null;
            _cachedPath = null;
            _cacheIsSolidColor = false;
        }

        /// <summary>
        /// Estimates the Mica pane color a Windows 11 desktop would show for
        /// <paramref name="wallpaperAverage"/>, per the fitted model documented above.
        /// </summary>
        /// <param name="wallpaperAverage">The average wallpaper (or solid background) color.</param>
        /// <param name="isDark">Whether to use the Dark theme's neutral base and tint constants.</param>
        internal static Color EstimateMicaColor(Color wallpaperAverage, bool isDark)
        {
            double baseGray = isDark ? 32.0 : 243.0; // SolidBackgroundFillColorBase R=G=B
            double y = (LumaR * wallpaperAverage.R) + (LumaG * wallpaperAverage.G) + (LumaB * wallpaperAverage.B);
            double diff = Math.Abs(y - baseGray);
            double strength = isDark ? DarkTintStrength : LightTintStrength;
            double falloff = isDark ? DarkTintFalloff : LightTintFalloff;
            double weight = strength * Math.Exp(-diff / falloff);

            double r = baseGray + (weight * (wallpaperAverage.R - y));
            double g = baseGray + (weight * (wallpaperAverage.G - y));
            double b = baseGray + (weight * (wallpaperAverage.B - y));

            return Color.FromRgb(ToByte(r), ToByte(g), ToByte(b));
        }

        /// <summary>
        /// Composites the theme's translucent content-layer color (<c>LayerFillColorDefault</c>)
        /// over <paramref name="micaEstimate"/> using integer "over" math, matching the pre-blend
        /// used for the neutral <c>NavigationViewContentBackground</c> fallback in
        /// <c>Theme.Light.xaml</c> / <c>Theme.Dark.xaml</c>. Always fully opaque.
        /// </summary>
        /// <param name="micaEstimate">The Mica pane color estimate to blend over.</param>
        /// <param name="isDark">Whether to use the Dark theme's layer color and alpha.</param>
        internal static Color PreBlendContent(Color micaEstimate, bool isDark)
        {
            byte layerAlpha = isDark ? DarkLayerAlpha : LightLayerAlpha;
            byte layerChannel = isDark ? DarkLayerChannel : LightLayerChannel;
            byte inverse = (byte)(255 - layerAlpha);

            byte r = OverBlend(layerChannel, layerAlpha, micaEstimate.R, inverse);
            byte g = OverBlend(layerChannel, layerAlpha, micaEstimate.G, inverse);
            byte b = OverBlend(layerChannel, layerAlpha, micaEstimate.B, inverse);

            return Color.FromArgb(0xFF, r, g, b);
        }

        /// <summary>
        /// Computes the final opaque <c>NavigationViewContentBackground</c> color for
        /// <paramref name="wallpaperAverage"/>: <see cref="EstimateMicaColor"/> followed by
        /// <see cref="PreBlendContent"/>.
        /// </summary>
        /// <param name="wallpaperAverage">The average wallpaper (or solid background) color.</param>
        /// <param name="isDark">Whether to compute the Dark theme's content color.</param>
        internal static Color ComputeContentBackground(Color wallpaperAverage, bool isDark)
        {
            return PreBlendContent(EstimateMicaColor(wallpaperAverage, isDark), isDark);
        }

        private static byte OverBlend(int fg, int alpha, int bg, int inverseAlpha)
        {
            // Truncating integer division (not rounded) - see the constants comment above for why
            // this specific rounding mode is required to reproduce the committed neutral values.
            return (byte)(((fg * alpha) + (bg * inverseAlpha)) / 255);
        }

        private static Color? GetSolidBackgroundColorCached()
        {
            if (_hasCache && _cacheIsSolidColor)
            {
                return _cachedColor;
            }

            // Cache the attempt, not just a success: a missing/malformed registry value is cached
            // as null too, so a failing lookup does not re-hit the registry on every single Apply
            // call. InvalidateCache() (driven by SystemThemeWatcher) clears this like any other
            // cached result.
            Color? color = RegistryHelper.TryGetDesktopBackgroundColor(out Color background) ? background : null;
            _cachedColor = color;
            _cachedPath = null;
            _cacheIsSolidColor = true;
            _hasCache = true;
            return color;
        }

        private static Color? AverageBitmapPixels(string path)
        {
            try
            {
                BitmapImage image = new();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.DecodePixelWidth = 32;
                image.UriSource = new Uri(path, UriKind.Absolute);
                image.EndInit();
                image.Freeze();

                FormatConvertedBitmap converted = new();
                converted.BeginInit();
                converted.Source = image;
                converted.DestinationFormat = PixelFormats.Bgra32;
                converted.EndInit();
                converted.Freeze();

                int width = converted.PixelWidth;
                int height = converted.PixelHeight;
                if (width <= 0 || height <= 0)
                {
                    return null;
                }

                int stride = width * 4;
                byte[] pixels = new byte[stride * height];
                converted.CopyPixels(pixels, stride, 0);

                long sumR = 0;
                long sumG = 0;
                long sumB = 0;
                for (int i = 0; i < pixels.Length; i += 4)
                {
                    // Bgra32 byte order: B, G, R, A.
                    sumB += pixels[i];
                    sumG += pixels[i + 1];
                    sumR += pixels[i + 2];
                }

                int pixelCount = width * height;
                return Color.FromRgb((byte)(sumR / pixelCount), (byte)(sumG / pixelCount), (byte)(sumB / pixelCount));
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                // Best-effort decode: a missing, locked, or corrupt wallpaper file, an unsupported
                // codec, or a headless/session-0 host with no imaging pipeline must degrade to
                // null, matching GetWallpaperAverageColor's overall failure contract.
                return null;
            }
        }

        private static byte ToByte(double value)
        {
            // Math.Clamp is unavailable on net472; clamp manually (Section 4.3 feasibility).
            double clamped = value < 0.0 ? 0.0 : value > 255.0 ? 255.0 : value;
            return (byte)clamped;
        }
    }
}
