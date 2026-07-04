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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace Fluence.Wpf.Specs
{
    /// <summary>
    /// The root of a composed dialog: a window title, a vertical flow of content elements, and the
    /// button row. Serialize with <see cref="SpecSerialization"/>; materialize with the
    /// SpecMaterializer in Fluence.Wpf.dll (the two ship as a matched pair).
    /// </summary>
    [DataContract(Namespace = SpecContracts.Namespace)]
    public sealed class DialogSpec
    {
        [DataMember(Name = "Content", Order = 1, EmitDefaultValue = false)]
        private Collection<SpecNode>? ContentCore { get; set; }

        [DataMember(Name = "Buttons", Order = 2, EmitDefaultValue = false)]
        private Collection<ButtonSpec>? ButtonsCore { get; set; }

        /// <summary>
        /// Initializes a new, empty <see cref="DialogSpec"/>.
        /// </summary>
        public DialogSpec()
        {
        }

        /// <summary>
        /// Initializes a new <see cref="DialogSpec"/> from a property dictionary (the PowerShell
        /// hashtable construction idiom). Unknown keys fail fast.
        /// </summary>
        /// <param name="properties">Recognized keys: Title, Content (one or more SpecNode), Buttons (one or more ButtonSpec).</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="properties"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown for an unrecognized key or invalid item type.</exception>
        public DialogSpec(IDictionary properties)
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
                    case "TITLE":
                        Title = SpecValueConverter.ToText(entry.Value);
                        break;
                    case "CONTENT":
                        SpecValueConverter.FillNodes(Content, entry.Value, nameof(Content));
                        break;
                    case "BUTTONS":
                        FillButtons(entry.Value);
                        break;
                    default:
                        throw new ArgumentException($"Unknown property '{key}' for {nameof(DialogSpec)}. Valid properties: Title, Content, Buttons.", nameof(properties));
                }
            }
        }

        /// <summary>
        /// Gets or sets the dialog window title.
        /// </summary>
        [DataMember(Order = 0, EmitDefaultValue = false)]
        public string? Title { get; set; }

        /// <summary>
        /// Gets the content elements, rendered as a vertical flow in declaration order.
        /// </summary>
        public IList<SpecNode> Content => ContentCore ??= [];

        /// <summary>
        /// Gets the dialog buttons, rendered as a right-aligned row in declaration order. At least
        /// one button is required.
        /// </summary>
        public IList<ButtonSpec> Buttons => ButtonsCore ??= [];

        private void FillButtons(object? value)
        {
            if (value is null)
            {
                return;
            }
            if (value is ButtonSpec single)
            {
                Buttons.Add(single);
                return;
            }
            if (value is IEnumerable sequence and not string)
            {
                foreach (object? item in sequence)
                {
                    if (item is not ButtonSpec button)
                    {
                        throw new ArgumentException($"Value of type '{item?.GetType().FullName ?? "null"}' is not valid for {nameof(Buttons)}; expected one or more {nameof(ButtonSpec)} instances.", nameof(value));
                    }
                    Buttons.Add(button);
                }
                return;
            }
            throw new ArgumentException($"Value of type '{value.GetType().FullName}' is not valid for {nameof(Buttons)}; expected one or more {nameof(ButtonSpec)} instances.", nameof(value));
        }
    }
}
