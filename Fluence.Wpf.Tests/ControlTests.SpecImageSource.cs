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
using System.IO;
using System.Windows.Media;
using Fluence.Wpf.Specs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Defense-in-depth coverage for <see cref="SpecMaterializer.LoadImageSourceFromPath"/> and
    /// <see cref="SpecMaterializer.LoadImageSourceFromBase64"/>: remote schemes must be rejected
    /// before WPF ever issues a request, and oversized Base64 payloads must be rejected before
    /// decode. Legit local/UNC/pack sources and normal-sized images must keep working unchanged.
    /// </summary>
    public partial class ControlTests
    {
        /// <summary>A standard 1x1 transparent PNG, 68 bytes decoded.</summary>
        private const string TinyPngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

        [TestMethod]
        public void LoadImageSourceFromPath_RemoteScheme_ThrowsBeforeAnyRequest()
        {
            RunOnStaThread(static () =>
            {
                _ = EnsureApplication();

                InvalidOperationException httpException = Assert.ThrowsExactly<InvalidOperationException>(
                    static () => _ = SpecMaterializer.LoadImageSourceFromPath("http://example.invalid/x.png"));
                StringAssert.Contains(httpException.Message, "http", StringComparison.OrdinalIgnoreCase);

                InvalidOperationException httpsException = Assert.ThrowsExactly<InvalidOperationException>(
                    static () => _ = SpecMaterializer.LoadImageSourceFromPath("https://example.invalid/x.png"));
                StringAssert.Contains(httpsException.Message, "https", StringComparison.OrdinalIgnoreCase);

                InvalidOperationException ftpException = Assert.ThrowsExactly<InvalidOperationException>(
                    static () => _ = SpecMaterializer.LoadImageSourceFromPath("ftp://example.invalid/x.png"));
                StringAssert.Contains(ftpException.Message, "ftp", StringComparison.OrdinalIgnoreCase);
            });
        }

        [TestMethod]
        public void LoadImageSourceFromPath_PackScheme_IsNotSchemeRejected()
        {
            RunOnStaThread(static () =>
            {
                _ = EnsureApplication();

                try
                {
                    // A pack application-resource URI must clear the scheme allow-list. The target
                    // resource is a XAML file, not an image, so a decode-side failure (or success) is
                    // fine here; the only outcome the fix must never produce is the scheme rejection.
                    _ = SpecMaterializer.LoadImageSourceFromPath("pack://application:,,,/Fluence.Wpf;component/Themes/Icons/FluenceIcons.xaml");
                }
                catch (InvalidOperationException exception)
                {
                    Assert.Fail($"pack:// must not be rejected by the scheme allow-list: {exception.Message}");
                }
                catch (Exception exception)
                {
                    Assert.IsNotInstanceOfType<InvalidOperationException>(exception, "only the scheme rejection is under test here");
                }
            });
        }

        [TestMethod]
        public void LoadImageSourceFromPath_LocalFile_StillLoadsFrozenBitmap()
        {
            RunOnStaThread(static () =>
            {
                _ = EnsureApplication();

                string path = Path.Combine(Path.GetTempPath(), "FluenceSpecImageSource_" + Guid.NewGuid().ToString("N") + ".png");
                File.WriteAllBytes(path, Convert.FromBase64String(TinyPngBase64));
                try
                {
                    ImageSource source = SpecMaterializer.LoadImageSourceFromPath(path);
                    Assert.IsNotNull(source, "a real local file path must still load");
                    Assert.IsTrue(source.IsFrozen, "the loaded bitmap must be frozen");
                }
                finally
                {
                    File.Delete(path);
                }
            });
        }

        [TestMethod]
        public void LoadImageSourceFromBase64_TinyImage_StillLoadsFrozenBitmap()
        {
            RunOnStaThread(static () =>
            {
                _ = EnsureApplication();

                ImageSource source = SpecMaterializer.LoadImageSourceFromBase64(TinyPngBase64);
                Assert.IsNotNull(source, "a normal-sized Base64 image must still load");
                Assert.IsTrue(source.IsFrozen, "the loaded bitmap must be frozen");
            });
        }

        [TestMethod]
        public void LoadImageSourceFromBase64_OversizedPayload_ThrowsBeforeDecode()
        {
            RunOnStaThread(static () =>
            {
                _ = EnsureApplication();

                byte[] oversized = new byte[SpecMaterializer.MaxImageBytes + 1];
                string base64 = Convert.ToBase64String(oversized);

                InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(
                    () => _ = SpecMaterializer.LoadImageSourceFromBase64(base64));
                StringAssert.Contains(exception.Message, SpecMaterializer.MaxImageBytes.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
            });
        }
    }
}
