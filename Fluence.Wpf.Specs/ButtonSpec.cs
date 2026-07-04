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
    /// One dialog button. The clicked button's <see cref="Name"/> (or <see cref="Text"/> when no
    /// name is set) becomes the dialog result's Button value. Mirrors the module's 'Fluence.Button'
    /// object shape.
    /// </summary>
    [DataContract(Namespace = SpecContracts.Namespace)]
    public sealed class ButtonSpec
    {
        /// <summary>
        /// Initializes a new, empty <see cref="ButtonSpec"/>.
        /// </summary>
        public ButtonSpec()
        {
        }

        /// <summary>
        /// Initializes a new <see cref="ButtonSpec"/> from a property dictionary (the PowerShell
        /// hashtable construction idiom). Unknown keys fail fast.
        /// </summary>
        /// <param name="properties">Recognized keys: Name, Text, IsDefault, IsCancel.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="properties"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown for an unrecognized key.</exception>
        public ButtonSpec(IDictionary properties)
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
                    case "NAME":
                        Name = SpecValueConverter.ToText(entry.Value);
                        break;
                    case "TEXT":
                        Text = SpecValueConverter.ToText(entry.Value);
                        break;
                    case "ISDEFAULT":
                        IsDefault = SpecValueConverter.ToBoolean(entry.Value);
                        break;
                    case "ISCANCEL":
                        IsCancel = SpecValueConverter.ToBoolean(entry.Value);
                        break;
                    default:
                        throw new ArgumentException($"Unknown property '{key}' for {nameof(ButtonSpec)}. Valid properties: Name, Text, IsDefault, IsCancel.", nameof(properties));
                }
            }
        }

        /// <summary>
        /// Gets or sets the result key reported when this button closes the dialog. Defaults to
        /// <see cref="Text"/> when null.
        /// </summary>
        [DataMember(Order = 0, EmitDefaultValue = false)]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the visible button label. Underscores mark keyboard accelerators. Required.
        /// </summary>
        [DataMember(Order = 1, EmitDefaultValue = false)]
        public string? Text { get; set; }

        /// <summary>
        /// Gets or sets whether this button is the accented default (activated by Enter).
        /// </summary>
        [DataMember(Order = 2, EmitDefaultValue = false)]
        public bool IsDefault { get; set; }

        /// <summary>
        /// Gets or sets whether this button cancels the dialog (activated by Esc); cancel buttons
        /// skip validation rules.
        /// </summary>
        [DataMember(Order = 3, EmitDefaultValue = false)]
        public bool IsCancel { get; set; }
    }
}
