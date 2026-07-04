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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Fluence.Wpf.Specs
{
    /// <summary>
    /// Turns serializable dialog specs (from the WPF-free Fluence.Wpf.Specs assembly) into live
    /// Fluence control trees and harvests their values back. The per-control create, apply, and
    /// harvest table is generated from SpecSurface.xml; this hand-written half owns the public
    /// surface, the common-property application, and thickness parsing. Fluence.Wpf.dll and
    /// Fluence.Wpf.Specs.dll ship as a matched pair: a spec type without a materializer entry
    /// throws with an actionable message.
    /// </summary>
    /// <remarks>
    /// All materialization must happen on an STA thread whose Application resources have been
    /// seeded by <see cref="ApplicationThemeManager.Apply"/> (directly or via the PowerShell
    /// module's initializer); the created controls resolve their brushes from the theme slots.
    /// </remarks>
    public static partial class SpecMaterializer
    {
        [ThreadStatic]
        private static List<KeyValuePair<SpecNode, FrameworkElement>>? CapturedPairs;

        /// <summary>
        /// Validates a dialog spec and materializes it into a ready-to-show dialog window.
        /// </summary>
        /// <param name="spec">The dialog spec to materialize.</param>
        /// <returns>The dialog window; call <see cref="SpecDialogWindow.ShowAndCollect"/> to run it modally.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="spec"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the spec fails structural validation.</exception>
        public static SpecDialogWindow Materialize(DialogSpec spec)
        {
            if (spec is null)
            {
                throw new ArgumentNullException(nameof(spec));
            }
            SpecTreeValidator.Validate(spec);
            return new SpecDialogWindow(spec);
        }

        private static partial FrameworkElement CreateElementCore(SpecNode node);

        private static partial void HarvestValueCore(SpecNode node, FrameworkElement element, IDictionary<string, object?> values);

        /// <summary>
        /// Creates the element tree for one spec node, recording every (node, element) pair created
        /// during the recursion into <paramref name="pairs"/> for later validation and harvesting.
        /// </summary>
        /// <param name="node">The spec node to materialize.</param>
        /// <param name="pairs">Receives one entry per created element, in creation order.</param>
        /// <returns>The created root element for the node.</returns>
        internal static FrameworkElement CreateElementTracked(SpecNode node, List<KeyValuePair<SpecNode, FrameworkElement>> pairs)
        {
            CapturedPairs = pairs;
            try
            {
                return CreateElementCore(node);
            }
            finally
            {
                CapturedPairs = null;
            }
        }

        /// <summary>
        /// Harvests the current value of one materialized element into the values dictionary,
        /// keyed by the spec's Name (or GroupName for radio buttons).
        /// </summary>
        /// <param name="node">The spec node.</param>
        /// <param name="element">The element created for the node.</param>
        /// <param name="values">The dictionary receiving the harvested value.</param>
        internal static void HarvestValue(SpecNode node, FrameworkElement element, IDictionary<string, object?> values)
        {
            HarvestValueCore(node, element, values);
        }

        /// <summary>
        /// Parses a WPF thickness string: one value ("8"), two values ("8,4" as horizontal,vertical),
        /// or four values ("8,4,8,4" as left,top,right,bottom), culture-invariant.
        /// </summary>
        /// <param name="value">The thickness string.</param>
        /// <returns>The parsed thickness.</returns>
        /// <exception cref="FormatException">Thrown when the string is not a valid thickness.</exception>
        internal static Thickness ParseThickness(string value)
        {
            string[] parts = value.Split(',');
            double[] numbers = new double[parts.Length];
            for (int index = 0; index < parts.Length; index++)
            {
                if (!double.TryParse(parts[index].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out numbers[index]))
                {
                    throw new FormatException($"'{value}' is not a valid thickness; expected 'all', 'horizontal,vertical', or 'left,top,right,bottom' numbers.");
                }
            }
            return numbers.Length switch
            {
                1 => new Thickness(numbers[0]),
                2 => new Thickness(numbers[0], numbers[1], numbers[0], numbers[1]),
                4 => new Thickness(numbers[0], numbers[1], numbers[2], numbers[3]),
                _ => throw new FormatException($"'{value}' is not a valid thickness; expected 1, 2, or 4 comma-separated numbers."),
            };
        }

        /// <summary>
        /// Parses a WPF corner-radius string: one value ("8") or four values ("8,4,8,4" as
        /// topLeft,topRight,bottomRight,bottomLeft), culture-invariant.
        /// </summary>
        /// <param name="value">The corner-radius string.</param>
        /// <returns>The parsed corner radius.</returns>
        /// <exception cref="FormatException">Thrown when the string is not a valid corner radius.</exception>
        internal static CornerRadius ParseCornerRadius(string value)
        {
            string[] parts = value.Split(',');
            double[] numbers = new double[parts.Length];
            for (int index = 0; index < parts.Length; index++)
            {
                if (!double.TryParse(parts[index].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out numbers[index]))
                {
                    throw new FormatException($"'{value}' is not a valid corner radius; expected 'all' or 'topLeft,topRight,bottomRight,bottomLeft' numbers.");
                }
            }
            return numbers.Length switch
            {
                1 => new CornerRadius(numbers[0]),
                4 => new CornerRadius(numbers[0], numbers[1], numbers[2], numbers[3]),
                _ => throw new FormatException($"'{value}' is not a valid corner radius; expected 1 or 4 comma-separated numbers."),
            };
        }

        /// <summary>
        /// Loads a frozen bitmap from a file path or URI. An absolute non-file URI (a pack or
        /// http(s) URI) passes through as-is; anything else resolves to a full local path, which
        /// must exist. The bitmap is decoded eagerly (no stream retention) and frozen so the
        /// materialized dialog can cross thread boundaries safely.
        /// </summary>
        /// <param name="path">The image file path, or an absolute URI.</param>
        /// <returns>The frozen image source.</returns>
        /// <exception cref="FileNotFoundException">Thrown when a local path does not exist.</exception>
        internal static ImageSource LoadImageSourceFromPath(string path)
        {
            Uri uri;
            if (Uri.TryCreate(path, UriKind.Absolute, out Uri? absolute) && !absolute.IsFile)
            {
                uri = absolute;
            }
            else
            {
                string fullPath = absolute?.IsFile == true ? absolute.LocalPath : Path.GetFullPath(path);
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"Image file not found for spec Source '{fullPath}'.", fullPath);
                }
                uri = new Uri(fullPath);
            }
            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = uri;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        /// <summary>
        /// Loads a frozen bitmap from Base64-encoded image bytes. The bitmap is decoded eagerly
        /// (no stream retention) and frozen so the materialized dialog can cross thread
        /// boundaries safely.
        /// </summary>
        /// <param name="base64">The Base64-encoded image data.</param>
        /// <returns>The frozen image source.</returns>
        /// <exception cref="FormatException">Thrown when the text is not valid Base64.</exception>
        internal static ImageSource LoadImageSourceFromBase64(string base64)
        {
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(base64);
            }
            catch (FormatException exception)
            {
                throw new FormatException("SourceBase64 is not valid Base64 image data.", exception);
            }
            using MemoryStream stream = new(bytes);
            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        private static void ApplyCommonProperties(FrameworkElement element, SpecNode node)
        {
            CapturedPairs?.Add(new KeyValuePair<SpecNode, FrameworkElement>(node, element));
            if (node.Margin is not null)
            {
                element.Margin = ParseThickness(node.Margin);
            }
            if (node.IsEnabled is { } isEnabled)
            {
                element.IsEnabled = isEnabled;
            }
            if (node.Width is { } width)
            {
                element.Width = width;
            }
            if (node.MinWidth is { } minWidth)
            {
                element.MinWidth = minWidth;
            }
        }
    }
}
