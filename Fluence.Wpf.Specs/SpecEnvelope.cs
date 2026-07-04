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

namespace Fluence.Wpf.Specs
{
    /// <summary>
    /// The stable outer wrapper around a serialized spec tree. The envelope contract never changes;
    /// only the payload's schema evolves, gated by <see cref="SchemaVersion"/>. This is what lets an
    /// opaque spec blob cross hosts whose Fluence versions differ, failing loudly instead of
    /// misreading.
    /// </summary>
    /// <param name="schemaVersion">The payload schema version.</param>
    /// <param name="specsAssemblyVersion">The producing Fluence.Wpf.Specs assembly version, for diagnostics.</param>
    /// <param name="payload">The serialized spec tree.</param>
    [DataContract(Namespace = SpecContracts.Namespace)]
    internal sealed class SpecEnvelope(int schemaVersion, string? specsAssemblyVersion, byte[] payload)
    {
        /// <summary>
        /// Gets or sets the payload schema version.
        /// </summary>
        [DataMember(Order = 0)]
        internal int SchemaVersion { get; set; } = schemaVersion;

        /// <summary>
        /// Gets or sets the producing Fluence.Wpf.Specs assembly version, for diagnostics.
        /// </summary>
        [DataMember(Order = 1, EmitDefaultValue = false)]
        internal string? SpecsAssemblyVersion { get; set; } = specsAssemblyVersion;

        /// <summary>
        /// Gets or sets the serialized spec tree.
        /// </summary>
        [DataMember(Order = 2)]
        internal byte[]? Payload { get; set; } = payload;
    }
}
