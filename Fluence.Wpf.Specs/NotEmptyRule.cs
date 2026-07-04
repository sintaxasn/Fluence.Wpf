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
    /// Requires a non-null, non-whitespace value before the dialog can commit.
    /// </summary>
    [DataContract(Namespace = SpecContracts.Namespace)]
    public sealed class NotEmptyRule : SpecRule
    {
        /// <summary>
        /// Initializes a new, empty <see cref="NotEmptyRule"/>.
        /// </summary>
        public NotEmptyRule()
        {
        }

        /// <summary>
        /// Initializes a new <see cref="NotEmptyRule"/> from a property dictionary (the PowerShell
        /// hashtable construction idiom). Unknown keys fail fast.
        /// </summary>
        /// <param name="properties">Recognized keys: ErrorMessage.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="properties"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown for an unrecognized key.</exception>
        public NotEmptyRule(IDictionary properties)
        {
            if (properties is null)
            {
                throw new ArgumentNullException(nameof(properties));
            }
            foreach (DictionaryEntry entry in properties)
            {
                string key = SpecValueConverter.ToPropertyKey(entry.Key);
                ErrorMessage = string.Equals(key, nameof(ErrorMessage), StringComparison.OrdinalIgnoreCase)
                    ? SpecValueConverter.ToText(entry.Value)
                    : throw new ArgumentException($"Unknown property '{key}' for {nameof(NotEmptyRule)}. Valid properties: ErrorMessage.", nameof(properties));
            }
        }
    }
}
