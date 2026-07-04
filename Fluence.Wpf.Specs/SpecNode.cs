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
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace Fluence.Wpf.Specs
{
    /// <summary>
    /// Base type for every serializable dialog-spec element. A spec is a strict tree: sharing one
    /// node instance in two places (or forming a cycle) is rejected by <see cref="SpecTreeValidator"/>
    /// because the wire format duplicates shared nodes and throws on cycles.
    /// </summary>
    [DataContract(Namespace = SpecContracts.Namespace)]
    public abstract class SpecNode
    {
        [DataMember(Name = "Rules", Order = 5, EmitDefaultValue = false)]
        private Collection<SpecRule>? RulesCore { get; set; }

        /// <summary>
        /// Gets or sets the element name. For value-bearing (input) elements this is the key under
        /// which the harvested value is returned; it must be unique within the dialog. Optional for
        /// purely presentational elements.
        /// </summary>
        [DataMember(Order = 0, EmitDefaultValue = false)]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the outer margin as a WPF thickness string: one value ("8"), two values
        /// ("8,4"), or four values ("8,4,8,4"), parsed culture-invariantly.
        /// </summary>
        [DataMember(Order = 1, EmitDefaultValue = false)]
        public string? Margin { get; set; }

        /// <summary>
        /// Gets or sets whether the element is enabled. Null leaves the control default untouched.
        /// </summary>
        [DataMember(Order = 2, EmitDefaultValue = false)]
        public bool? IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a fixed width in device-independent pixels. Null leaves layout to the control.
        /// </summary>
        [DataMember(Order = 3, EmitDefaultValue = false)]
        public double? Width { get; set; }

        /// <summary>
        /// Gets or sets a minimum width in device-independent pixels. Null leaves layout to the control.
        /// </summary>
        [DataMember(Order = 4, EmitDefaultValue = false)]
        public double? MinWidth { get; set; }

        /// <summary>
        /// Gets the declarative validation rules evaluated when the dialog commits on a non-cancel
        /// button. Rules are only meaningful on named, value-bearing elements.
        /// </summary>
        public IList<SpecRule> Rules => RulesCore ??= [];

        /// <summary>
        /// Gets whether any rules have been attached (without materializing the lazy collection).
        /// </summary>
        internal bool HasRules => RulesCore?.Count > 0;

        /// <summary>
        /// Gets whether this element harvests a value into the dialog result. Overridden by
        /// generated input specs.
        /// </summary>
        internal virtual bool IsValueBearing => false;

        /// <summary>
        /// Enumerates the child spec nodes of this element. Overridden by generated container specs.
        /// </summary>
        internal virtual IEnumerable<SpecNode> GetChildren()
        {
            yield break;
        }
    }
}
