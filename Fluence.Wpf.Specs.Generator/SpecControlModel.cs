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

using System.Collections.Generic;

namespace Fluence.Wpf.Specs.Generator
{
    /// <summary>A curated control declared by the manifest.</summary>
    internal sealed class SpecControlModel
    {
        /// <summary>Gets or sets the control name (the spec class is named &lt;Name&gt;Spec).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the full CLR control type name.</summary>
        public string Clr { get; set; } = string.Empty;

        /// <summary>Gets or sets the XML documentation text.</summary>
        public string Doc { get; set; } = string.Empty;

        /// <summary>Gets or sets the harvested value member name, or null for non-input controls.</summary>
        public string? ValueMember { get; set; }

        /// <summary>Gets or sets the harvest kind: String, Boolean, Double, Date, Time, or RadioGroup.</summary>
        public string? ValueKind { get; set; }

        /// <summary>Gets the curated members in declaration order.</summary>
        public List<SpecMemberModel> Members { get; } = [];

        /// <summary>Gets whether this control harvests a value under its own Name key.</summary>
        public bool IsValueBearing => ValueKind is not null && !SpecEmit.Is(ValueKind, "RadioGroup");
    }
}
