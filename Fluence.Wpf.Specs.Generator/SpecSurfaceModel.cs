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
using System.Xml;
using System.Xml.Linq;

namespace Fluence.Wpf.Specs.Generator
{
    /// <summary>The parsed SpecSurface.xml manifest.</summary>
    internal sealed class SpecSurfaceModel
    {
        /// <summary>Gets the mirrored enums.</summary>
        public List<SpecEnumModel> Enums { get; } = [];

        /// <summary>Gets the curated controls.</summary>
        public List<SpecControlModel> Controls { get; } = [];

        /// <summary>
        /// Parses the manifest text, collecting structural errors instead of throwing (so the
        /// generator can report them as FLSPEC005 diagnostics).
        /// </summary>
        /// <param name="xml">The manifest file text.</param>
        /// <param name="errors">Receives one message per structural problem.</param>
        /// <returns>The parsed model (possibly partial when errors are present).</returns>
        public static SpecSurfaceModel Parse(string xml, IList<string> errors)
        {
            SpecSurfaceModel model = new();
            XDocument document;
            try
            {
                document = XDocument.Parse(xml);
            }
            catch (XmlException exception)
            {
                errors.Add("XML parse failure: " + exception.Message);
                return model;
            }
            XElement? root = document.Root;
            if (root is null || !SpecEmit.Is(root.Name.LocalName, "SpecSurface"))
            {
                errors.Add("The document root must be <SpecSurface>.");
                return model;
            }
            foreach (XElement element in root.Elements("Enum"))
            {
                SpecEnumModel enumModel = new()
                {
                    Name = RequireAttribute(element, "name", errors),
                    Clr = RequireAttribute(element, "clr", errors),
                    Doc = RequireAttribute(element, "doc", errors),
                };
                foreach (XElement valueElement in element.Elements("Value"))
                {
                    enumModel.Values.Add(new SpecEnumValueModel
                    {
                        Name = RequireAttribute(valueElement, "name", errors),
                        Doc = RequireAttribute(valueElement, "doc", errors),
                    });
                }
                if (enumModel.Values.Count == 0)
                {
                    errors.Add("Enum '" + enumModel.Name + "' declares no values.");
                }
                model.Enums.Add(enumModel);
            }
            foreach (XElement element in root.Elements("Control"))
            {
                SpecControlModel controlModel = new()
                {
                    Name = RequireAttribute(element, "name", errors),
                    Clr = RequireAttribute(element, "clr", errors),
                    Doc = RequireAttribute(element, "doc", errors),
                    ValueMember = (string?)element.Attribute("valueMember"),
                    ValueKind = (string?)element.Attribute("valueKind"),
                };
                if ((controlModel.ValueMember is null) != (controlModel.ValueKind is null))
                {
                    errors.Add("Control '" + controlModel.Name + "' must declare valueMember and valueKind together.");
                }
                foreach (XElement memberElement in element.Elements("Member"))
                {
                    controlModel.Members.Add(new SpecMemberModel
                    {
                        Name = RequireAttribute(memberElement, "name", errors),
                        Type = RequireAttribute(memberElement, "type", errors),
                        Doc = RequireAttribute(memberElement, "doc", errors),
                    });
                }
                model.Controls.Add(controlModel);
            }
            return model;
        }

        private static string RequireAttribute(XElement element, string name, IList<string> errors)
        {
            string? value = (string?)element.Attribute(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add("<" + element.Name.LocalName + "> is missing the required '" + name + "' attribute.");
                return string.Empty;
            }
            return value!;
        }
    }
}
