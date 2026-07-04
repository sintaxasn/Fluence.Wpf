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

using Microsoft.CodeAnalysis;

namespace Fluence.Wpf.Specs.Generator
{
    /// <summary>
    /// The FLSPEC diagnostics reported when SpecSurface.xml drifts from the compiled Fluence.Wpf
    /// control surface. All are errors: drift must fail the build, never surface at runtime.
    /// </summary>
    internal static class DiagnosticDescriptors
    {
        private const string Category = "SpecSurface";

        /// <summary>FLSPEC001: a manifest control's CLR type does not exist in the compilation.</summary>
        public static readonly DiagnosticDescriptor ControlNotFound = new(
            "FLSPEC001",
            "Spec-surface control type not found",
            "SpecSurface.xml declares control '{0}' with CLR type '{1}', which does not exist in this compilation",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>FLSPEC002: a manifest member does not exist on the control type or its bases.</summary>
        public static readonly DiagnosticDescriptor MemberNotFound = new(
            "FLSPEC002",
            "Spec-surface member not found",
            "SpecSurface.xml declares member '{0}' on control '{1}', but no such property exists on '{2}' or its base types",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>FLSPEC003: a manifest member's declared type is incompatible with the control property.</summary>
        public static readonly DiagnosticDescriptor MemberTypeIncompatible = new(
            "FLSPEC003",
            "Spec-surface member type incompatible",
            "SpecSurface.xml declares member '{0}' on control '{1}' as '{2}', which is incompatible with the property type '{3}'",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>FLSPEC004: a mirrored enum's values drift from the CLR enum.</summary>
        public static readonly DiagnosticDescriptor EnumDrift = new(
            "FLSPEC004",
            "Spec-surface enum drift",
            "SpecSurface.xml enum '{0}' does not match CLR enum '{1}': {2}",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>FLSPEC005: the manifest is missing, unreadable, or structurally invalid.</summary>
        public static readonly DiagnosticDescriptor ManifestInvalid = new(
            "FLSPEC005",
            "Spec-surface manifest invalid",
            "SpecSurface.xml is missing or invalid: {0}",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
