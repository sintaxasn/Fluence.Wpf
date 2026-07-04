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
using System.Collections;
using System.Runtime.Serialization;

namespace Fluence.Wpf.Specs
{
    /// <summary>
    /// Requires a numeric value to fall inside an inclusive range.
    /// </summary>
    [DataContract(Namespace = SpecContracts.Namespace)]
    public sealed class RangeRule : SpecRule
    {
        /// <summary>
        /// Initializes a new, empty <see cref="RangeRule"/>.
        /// </summary>
        public RangeRule()
        {
        }

        /// <summary>
        /// Initializes a new <see cref="RangeRule"/> from a property dictionary (the PowerShell
        /// hashtable construction idiom). Unknown keys fail fast.
        /// </summary>
        /// <param name="properties">Recognized keys: Minimum, Maximum, ErrorMessage.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="properties"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown for an unrecognized key.</exception>
        public RangeRule(IDictionary properties)
        {
            if (properties is null)
            {
                throw new ArgumentNullException(nameof(properties));
            }
            foreach (DictionaryEntry entry in properties)
            {
                string key = SpecValueConverter.ToPropertyKey(entry.Key);
                switch (key.ToUpperInvariant())
                {
                    case "MINIMUM":
                        Minimum = SpecValueConverter.ToNullableDouble(entry.Value);
                        break;
                    case "MAXIMUM":
                        Maximum = SpecValueConverter.ToNullableDouble(entry.Value);
                        break;
                    case "ERRORMESSAGE":
                        ErrorMessage = SpecValueConverter.ToText(entry.Value);
                        break;
                    default:
                        throw new ArgumentException($"Unknown property '{key}' for {nameof(RangeRule)}. Valid properties: Minimum, Maximum, ErrorMessage.", nameof(properties));
                }
            }
        }

        /// <summary>
        /// Gets or sets the inclusive minimum value. Null means no minimum.
        /// </summary>
        [DataMember(Order = 1, EmitDefaultValue = false)]
        public double? Minimum { get; set; }

        /// <summary>
        /// Gets or sets the inclusive maximum value. Null means no maximum.
        /// </summary>
        [DataMember(Order = 2, EmitDefaultValue = false)]
        public double? Maximum { get; set; }
    }
}
