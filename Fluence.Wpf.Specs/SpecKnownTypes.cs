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
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Fluence.Wpf.Specs
{
    /// <summary>
    /// The closed set of polymorphic types the spec serializer accepts: every public, concrete
    /// data-contract type in this assembly (dialog root, buttons, results, validation rules, and
    /// all generated control specs), discovered once by reflection and ordered deterministically.
    /// The serializer deliberately mirrors PSADT's frozen-allow-list posture: open-ended object
    /// graphs cannot pass.
    /// </summary>
    public static class SpecKnownTypes
    {
        /// <summary>
        /// Gets every known spec type in deterministic (ordinal full-name) order.
        /// </summary>
        public static ReadOnlyCollection<Type> All { get; } = BuildAll();

        private static ReadOnlyCollection<Type> BuildAll()
        {
            Type[] types =
            [
                .. typeof(SpecKnownTypes).Assembly
                    .GetTypes()
                    .Where(static type => type.IsPublic && !type.IsAbstract && Attribute.IsDefined(type, typeof(DataContractAttribute)))
                    .OrderBy(static type => type.FullName, StringComparer.Ordinal),
            ];
            return new ReadOnlyCollection<Type>(types);
        }
    }
}
