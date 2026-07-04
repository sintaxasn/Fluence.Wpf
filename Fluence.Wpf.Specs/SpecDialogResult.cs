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
using System.Runtime.Serialization;

namespace Fluence.Wpf.Specs
{
    /// <summary>
    /// The outcome of a shown dialog spec: which button closed it and the harvested values of every
    /// named input element. Values are primitives (string, bool, double, DateTime?, TimeSpan?).
    /// </summary>
    [DataContract(Namespace = SpecContracts.Namespace)]
    public sealed class SpecDialogResult
    {
        [DataMember(Name = "Values", Order = 1, EmitDefaultValue = false)]
        private Dictionary<string, object?>? ValuesCore { get; set; }

        /// <summary>
        /// Gets or sets the identity of the button that closed the dialog: the button's Name, or its
        /// Text when no name was set, or "Timeout"/"Closed" for non-button dismissal in later phases.
        /// </summary>
        [DataMember(Order = 0, EmitDefaultValue = false)]
        public string? Button { get; set; }

        /// <summary>
        /// Gets the harvested values keyed by input element Name (case-insensitive until the result
        /// crosses a serialization boundary, where the comparer is not preserved).
        /// </summary>
        public IDictionary<string, object?> Values => ValuesCore ??= new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }
}
