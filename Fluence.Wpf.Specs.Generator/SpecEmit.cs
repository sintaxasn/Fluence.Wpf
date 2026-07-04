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
using System.Text;

namespace Fluence.Wpf.Specs.Generator
{
    /// <summary>Shared constants and helpers for the emitted source files.</summary>
    internal static class SpecEmit
    {
        /// <summary>
        /// Appends one emitted source line composed from parts, then a newline. Centralizing the
        /// concatenation keeps the emitters free of string-concatenation-in-Append call sites.
        /// </summary>
        /// <param name="builder">The target builder.</param>
        /// <param name="parts">The line fragments, appended in order.</param>
        public static void Line(StringBuilder builder, params string[] parts)
        {
            foreach (string part in parts)
            {
                _ = builder.Append(part);
            }
            _ = builder.AppendLine();
        }

        /// <summary>Ordinal string equality, centralizing the comparison the analyzers require.</summary>
        /// <param name="value">The value under test.</param>
        /// <param name="candidate">The candidate to compare against.</param>
        /// <returns>True when the strings are ordinal-equal.</returns>
        public static bool Is(string? value, string candidate)
        {
            return string.Equals(value, candidate, StringComparison.Ordinal);
        }

        /// <summary>The generator identity stamped into GeneratedCode attributes.</summary>
        public const string ToolName = "Fluence.Wpf.Specs.Generator";

        /// <summary>The generator version stamped into GeneratedCode attributes.</summary>
        public const string ToolVersion = "1.0.0";

        private static readonly string[] PreambleLines =
        [
            "/*",
            " * Copyright 2026 Dan Cunningham",
            " *",
            " * Redistribution and use in source and binary forms, with or without",
            " * modification, are permitted provided that the following conditions are met:",
            " *",
            " * 1. Redistributions of source code must retain the above copyright notice,",
            " *    this list of conditions and the following disclaimer.",
            " * 2. Redistributions in binary form must reproduce the above copyright notice,",
            " *    this list of conditions and the following disclaimer in the documentation",
            " *    and/or other materials provided with the distribution.",
            " * 3. Neither the name of the copyright holder nor the names of its contributors",
            " *    may be used to endorse or promote products derived from this software",
            " *    without specific prior written permission.",
            " *",
            " * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS \"AS IS\"",
            " * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE",
            " * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE",
            " * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE",
            " * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR",
            " * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF",
            " * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS",
            " * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN",
            " * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)",
            " * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF",
            " * THE POSSIBILITY OF SUCH DAMAGE.",
            " */",
            "",
            "// <auto-generated/>",
            "// Generated by Fluence.Wpf.Specs.Generator from SpecSurface.xml. Do not edit; edit the manifest.",
            "#nullable enable",
            "",
        ];

        /// <summary>
        /// The preamble of every emitted file: the BSD license header, the auto-generated marker
        /// (which exempts the file from style analyzers), and the nullable context. Joined with LF
        /// to match the repository line-ending policy.
        /// </summary>
        public static readonly string FilePreamble = string.Join("\n", PreambleLines);

        /// <summary>The manifest file name both generators look for among AdditionalFiles.</summary>
        public const string ManifestFileName = "SpecSurface.xml";

        /// <summary>Escapes text for inclusion in an emitted XML documentation comment.</summary>
        /// <param name="value">The raw documentation text.</param>
        /// <returns>The escaped text.</returns>
        public static string EscapeXml(string value)
        {
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        /// <summary>Maps a scalar manifest member type token to the emitted DTO property type.</summary>
        /// <param name="manifestType">The manifest type token (not stringList/child/childList/enum).</param>
        /// <returns>The C# property type text, or null when the token is not a scalar.</returns>
        public static string? GetScalarDtoType(string manifestType)
        {
            return manifestType switch
            {
                "text" => "string?",
                "uri" => "string?",
                "thickness" => "string?",
                "boolOpt" => "bool?",
                "intOpt" => "int?",
                "doubleOpt" => "double?",
                "dateOpt" => "global::System.DateTime?",
                "timeOpt" => "global::System.TimeSpan?",
                _ => null,
            };
        }

        /// <summary>Maps a scalar manifest member type token to its SpecValueConverter call.</summary>
        /// <param name="manifestType">The manifest type token.</param>
        /// <returns>The converter method name, or null when the token is not a scalar.</returns>
        public static string? GetScalarConverter(string manifestType)
        {
            return manifestType switch
            {
                "text" => "ToText",
                "uri" => "ToText",
                "thickness" => "ToText",
                "boolOpt" => "ToNullableBoolean",
                "intOpt" => "ToNullableInt32",
                "doubleOpt" => "ToNullableDouble",
                "dateOpt" => "ToNullableDateTime",
                "timeOpt" => "ToNullableTimeSpan",
                _ => null,
            };
        }
    }
}
