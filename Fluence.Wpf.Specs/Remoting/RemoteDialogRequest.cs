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

using System.Runtime.Serialization;

namespace Fluence.Wpf.Specs.Remoting
{
    /// <summary>
    /// The wire request a remote-host controller sends to the standalone Fluence UI host to show one
    /// composed spec dialog. Carries the already-serialized spec envelope (Base64 form, produced by
    /// <see cref="SpecSerialization.SerializeToBase64"/>) plus the presentation options the in-process
    /// dialog path accepts, so both paths render identically.
    /// </summary>
    [DataContract(Namespace = SpecContracts.Namespace)]
    public sealed class RemoteDialogRequest
    {
        /// <summary>
        /// Gets or sets the Base64 spec envelope produced by
        /// <see cref="SpecSerialization.SerializeToBase64"/>. The host deserializes it with
        /// <see cref="SpecSerialization.DeserializeFromBase64"/> before materializing.
        /// </summary>
        [DataMember(Order = 0, EmitDefaultValue = false)]
        public string? SpecBase64 { get; set; }

        /// <summary>
        /// Gets or sets the requested theme name (Auto, Light, Dark, or HighContrast). Null applies Auto.
        /// </summary>
        [DataMember(Order = 1, EmitDefaultValue = false)]
        public string? Theme { get; set; }

        /// <summary>
        /// Gets or sets the requested backdrop name (Mica, Acrylic, Tabbed, None, or Auto). Null applies Mica.
        /// </summary>
        [DataMember(Order = 2, EmitDefaultValue = false)]
        public string? Backdrop { get; set; }

        /// <summary>
        /// Gets or sets the custom accent color as a parseable color string (for example "#FF0078D4").
        /// Null applies the system accent.
        /// </summary>
        [DataMember(Order = 3, EmitDefaultValue = false)]
        public string? AccentColorText { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the dialog window shows above other windows.
        /// </summary>
        [DataMember(Order = 4, EmitDefaultValue = false)]
        public bool Topmost { get; set; }

        /// <summary>
        /// Gets or sets the number of seconds after which the host closes the dialog by itself,
        /// yielding the same Cancelled result as a user dismissal. Null means no timeout.
        /// </summary>
        [DataMember(Order = 5, EmitDefaultValue = false)]
        public int? TimeoutSeconds { get; set; }
    }
}
