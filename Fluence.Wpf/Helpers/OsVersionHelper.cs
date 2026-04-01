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
using Fluence.Wpf.Native;

namespace Fluence.Wpf.Helpers
{
    internal static class OsVersionHelper
    {
        private static readonly Version _osVersion;
        private static readonly int _osBuild;

        static OsVersionHelper()
        {
            try
            {
                _osVersion = NativeMethods.GetRealOsVersion();
            }
            catch
            {
                _osVersion = Environment.OSVersion.Version;
            }

            _osBuild = _osVersion.Build;
        }

        public static Version OsVersion => _osVersion;

        public static int OsBuild => _osBuild;

        public static bool IsWindows10 => _osBuild >= 10240;

        public static bool IsWindows10_1809 => _osBuild >= 17763;

        public static bool IsWindows11 => _osBuild >= 22000;

        public static bool IsWindows11_22H2 => _osBuild >= 22621;

        public static bool IsWindows11_23H2 => _osBuild >= 22631;

        public static bool SupportsBackdrop => IsWindows11;

        public static bool SupportsSystemBackdropType => IsWindows11_22H2;

        public static bool SupportsMicaEffect => IsWindows11 && !IsWindows11_22H2;

        public static bool SupportsRoundedCorners => IsWindows11;

        public static bool SupportsCaptionColor => IsWindows11;

        public static bool SupportsBorderColor => IsWindows11;
    }
}
