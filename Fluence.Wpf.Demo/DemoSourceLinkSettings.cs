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

namespace Fluence.Wpf.Demo
{
    public static class DemoSourceLinkSettings
    {
        public const string RepositoryUrl = "https://github.com/sintaxasn/Fluence.Wpf";
        public const string RepositoryBranch = "main";
        public const string SampleRootPath = "Fluence.Wpf.Demo/Samples";

        public static bool IsOnlineMode { get; set; }

        public static Uri GetSourceUri(string samplePath)
        {
            return IsOnlineMode ? GetGitHubSourceUri(samplePath) : GetLocalSourceUri(samplePath);
        }

        public static Uri GetLocalSourceUri(string samplePath)
        {
            return new Uri("pack://siteoforigin:,,,/Samples/" + NormalizeSamplePath(samplePath), UriKind.Absolute);
        }

        public static Uri GetGitHubSourceUri(string samplePath)
        {
            return new Uri(
                RepositoryUrl + "/blob/" + RepositoryBranch + "/" + SampleRootPath + "/" + NormalizeSamplePath(samplePath),
                UriKind.Absolute);
        }

        private static string NormalizeSamplePath(string samplePath)
        {
            if (samplePath == null)
            {
                throw new ArgumentNullException("samplePath");
            }

            var normalized = samplePath.Replace('\\', '/').Trim('/');
            if (normalized.Length == 0 || normalized.IndexOf("..", StringComparison.Ordinal) >= 0)
            {
                throw new ArgumentException("Sample paths must be relative paths inside the Samples directory.", "samplePath");
            }

            return normalized;
        }
    }
}
