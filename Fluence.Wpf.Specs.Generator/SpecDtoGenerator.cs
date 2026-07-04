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
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Fluence.Wpf.Specs.Generator
{
    /// <summary>
    /// Emits the serializable spec DTO classes and mirrored enums into the Fluence.Wpf.Specs
    /// compilation from the SpecSurface.xml manifest. Inactive in every other compilation.
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class SpecDtoGenerator : IIncrementalGenerator
    {
        /// <inheritdoc />
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValueProvider<string?> assemblyName = context.CompilationProvider.Select(static (compilation, _) => compilation.AssemblyName);
            IncrementalValueProvider<ImmutableArray<string>> manifests = context.AdditionalTextsProvider
                .Where(static text => string.Equals(Path.GetFileName(text.Path), SpecEmit.ManifestFileName, StringComparison.OrdinalIgnoreCase))
                .Select(static (text, cancellationToken) => text.GetText(cancellationToken)?.ToString() ?? string.Empty)
                .Collect();
            context.RegisterSourceOutput(assemblyName.Combine(manifests), static (productionContext, source) => Execute(productionContext, source.Left, source.Right));
        }

        private static void Execute(SourceProductionContext context, string? assemblyName, ImmutableArray<string> manifests)
        {
            if (!string.Equals(assemblyName, "Fluence.Wpf.Specs", StringComparison.Ordinal))
            {
                return;
            }
            if (manifests.Length == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ManifestInvalid, Location.None, "no SpecSurface.xml AdditionalFiles entry is wired into the project"));
                return;
            }
            List<string> errors = [];
            SpecSurfaceModel model = SpecSurfaceModel.Parse(manifests[0], errors);
            foreach (string error in errors)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ManifestInvalid, Location.None, error));
            }
            if (errors.Count > 0)
            {
                return;
            }
            context.AddSource("SpecEnums.g.cs", SourceText.From(EmitEnums(model), Encoding.UTF8));
            foreach (SpecControlModel control in model.Controls)
            {
                context.AddSource(control.Name + "Spec.g.cs", SourceText.From(EmitControl(control), Encoding.UTF8));
            }
        }

        private static string EmitEnums(SpecSurfaceModel model)
        {
            StringBuilder builder = new();
            _ = builder.Append(SpecEmit.FilePreamble);
            _ = builder.AppendLine();
            _ = builder.AppendLine("namespace Fluence.Wpf.Specs");
            _ = builder.AppendLine("{");
            bool first = true;
            foreach (SpecEnumModel enumModel in model.Enums)
            {
                if (!first)
                {
                    _ = builder.AppendLine();
                }
                first = false;
                SpecEmit.Line(builder, "    /// <summary>", SpecEmit.EscapeXml(enumModel.Doc), "</summary>");
                SpecEmit.Line(builder, "    [global::System.CodeDom.Compiler.GeneratedCode(\"", SpecEmit.ToolName, "\", \"", SpecEmit.ToolVersion, "\")]");
                SpecEmit.Line(builder, "    public enum ", enumModel.Name);
                _ = builder.AppendLine("    {");
                for (int index = 0; index < enumModel.Values.Count; index++)
                {
                    SpecEnumValueModel value = enumModel.Values[index];
                    SpecEmit.Line(builder, "        /// <summary>", SpecEmit.EscapeXml(value.Doc), "</summary>");
                    SpecEmit.Line(builder, "        ", value.Name, " = ", index.ToString(CultureInfo.InvariantCulture), ",");
                }
                _ = builder.AppendLine("    }");
            }
            _ = builder.AppendLine("}");
            return builder.ToString();
        }

        private static string EmitControl(SpecControlModel control)
        {
            string className = control.Name + "Spec";
            string validKeys = "Name, Margin, IsEnabled, Width, MinWidth, Rules" + string.Concat(control.Members.Select(static member => ", " + member.Name));
            StringBuilder builder = new();
            _ = builder.Append(SpecEmit.FilePreamble);
            _ = builder.AppendLine();
            _ = builder.AppendLine("namespace Fluence.Wpf.Specs");
            _ = builder.AppendLine("{");
            SpecEmit.Line(builder, "    /// <summary>", SpecEmit.EscapeXml(control.Doc), "</summary>");
            SpecEmit.Line(builder, "    [global::System.CodeDom.Compiler.GeneratedCode(\"", SpecEmit.ToolName, "\", \"", SpecEmit.ToolVersion, "\")]");
            _ = builder.AppendLine("    [global::System.Runtime.Serialization.DataContract(Namespace = SpecContracts.Namespace)]");
            SpecEmit.Line(builder, "    public sealed class ", className, " : SpecNode");
            _ = builder.AppendLine("    {");
            SpecEmit.Line(builder, "        /// <summary>Initializes a new, empty <see cref=\"", className, "\"/>.</summary>");
            SpecEmit.Line(builder, "        public ", className, "()");
            _ = builder.AppendLine("        {");
            _ = builder.AppendLine("        }");
            _ = builder.AppendLine();
            SpecEmit.Line(builder, "        /// <summary>Initializes a new <see cref=\"", className, "\"/> from a property dictionary (the PowerShell hashtable construction idiom). Unknown keys fail fast.</summary>");
            SpecEmit.Line(builder, "        /// <param name=\"properties\">Recognized keys: ", validKeys, ".</param>");
            _ = builder.AppendLine("        /// <exception cref=\"global::System.ArgumentNullException\">Thrown when <paramref name=\"properties\"/> is null.</exception>");
            _ = builder.AppendLine("        /// <exception cref=\"global::System.ArgumentException\">Thrown for an unrecognized key or invalid value.</exception>");
            SpecEmit.Line(builder, "        public ", className, "(global::System.Collections.IDictionary properties)");
            _ = builder.AppendLine("        {");
            _ = builder.AppendLine("            if (properties is null)");
            _ = builder.AppendLine("            {");
            _ = builder.AppendLine("                throw new global::System.ArgumentNullException(nameof(properties));");
            _ = builder.AppendLine("            }");
            _ = builder.AppendLine("            foreach (global::System.Collections.DictionaryEntry entry in properties)");
            _ = builder.AppendLine("            {");
            _ = builder.AppendLine("                string key = SpecValueConverter.ToPropertyKey(entry.Key);");
            _ = builder.AppendLine("                switch (key.ToUpperInvariant())");
            _ = builder.AppendLine("                {");
            AppendCtorCase(builder, "NAME", "Name = SpecValueConverter.ToText(entry.Value);");
            AppendCtorCase(builder, "MARGIN", "Margin = SpecValueConverter.ToText(entry.Value);");
            AppendCtorCase(builder, "ISENABLED", "IsEnabled = SpecValueConverter.ToNullableBoolean(entry.Value);");
            AppendCtorCase(builder, "WIDTH", "Width = SpecValueConverter.ToNullableDouble(entry.Value);");
            AppendCtorCase(builder, "MINWIDTH", "MinWidth = SpecValueConverter.ToNullableDouble(entry.Value);");
            AppendCtorCase(builder, "RULES", "SpecValueConverter.FillRules(Rules, entry.Value, nameof(Rules));");
            foreach (SpecMemberModel member in control.Members)
            {
                AppendCtorCase(builder, member.Name.ToUpperInvariant(), GetCtorAssignment(member));
            }
            _ = builder.AppendLine("                    default:");
            SpecEmit.Line(builder, "                        throw new global::System.ArgumentException(\"Unknown property '\" + key + \"' for ", className, ". Valid properties: ", validKeys, ".\", nameof(properties));");
            _ = builder.AppendLine("                }");
            _ = builder.AppendLine("            }");
            _ = builder.AppendLine("        }");
            foreach (SpecMemberModel member in control.Members)
            {
                AppendMember(builder, control, member);
            }
            if (control.IsValueBearing)
            {
                _ = builder.AppendLine();
                _ = builder.AppendLine("        /// <inheritdoc />");
                _ = builder.AppendLine("        internal override bool IsValueBearing => true;");
            }
            AppendGetChildren(builder, control);
            _ = builder.AppendLine("    }");
            _ = builder.AppendLine("}");
            return builder.ToString();
        }

        private static void AppendCtorCase(StringBuilder builder, string upperKey, string assignment)
        {
            SpecEmit.Line(builder, "                    case \"", upperKey, "\":");
            SpecEmit.Line(builder, "                        ", assignment);
            _ = builder.AppendLine("                        break;");
        }

        private static string GetCtorAssignment(SpecMemberModel member)
        {
            return member.EnumName is string enumName
                ? member.Name + " = SpecValueConverter.ToNullableEnum<" + enumName + ">(entry.Value, nameof(" + member.Name + "));"
                : member.Type switch
                {
                    "stringList" => "SpecValueConverter.FillStrings(" + member.Name + ", entry.Value);",
                    "childList" => "SpecValueConverter.FillNodes(" + member.Name + ", entry.Value, nameof(" + member.Name + "));",
                    "child" => member.Name + " = SpecValueConverter.ToNullableNode(entry.Value, nameof(" + member.Name + "));",
                    _ => member.Name + " = SpecValueConverter." + (SpecEmit.GetScalarConverter(member.Type) ?? "ToText") + "(entry.Value);",
                };
        }

        private static void AppendMember(StringBuilder builder, SpecControlModel control, SpecMemberModel member)
        {
            _ = builder.AppendLine();
            string order = GetOrder(control, member);
            if (SpecEmit.Is(member.Type, "stringList") || SpecEmit.Is(member.Type, "childList"))
            {
                string itemType = SpecEmit.Is(member.Type, "stringList") ? "string" : "SpecNode";
                SpecEmit.Line(builder, "        [global::System.Runtime.Serialization.DataMember(Name = \"", member.Name, "\", Order = ", order, ", EmitDefaultValue = false)]");
                SpecEmit.Line(builder, "        private global::System.Collections.ObjectModel.Collection<", itemType, ">? ", member.Name, "Core { get; set; }");
                _ = builder.AppendLine();
                SpecEmit.Line(builder, "        /// <summary>", SpecEmit.EscapeXml(member.Doc), "</summary>");
                SpecEmit.Line(builder, "        public global::System.Collections.Generic.IList<", itemType, "> ", member.Name, " => ", member.Name, "Core ??= new global::System.Collections.ObjectModel.Collection<", itemType, ">();");
                return;
            }
            string propertyType = SpecEmit.Is(member.Type, "child")
                ? "SpecNode?"
                : member.EnumName is string enumName
                ? enumName + "?"
                : SpecEmit.GetScalarDtoType(member.Type) ?? "string?";
            SpecEmit.Line(builder, "        /// <summary>", SpecEmit.EscapeXml(member.Doc), "</summary>");
            SpecEmit.Line(builder, "        [global::System.Runtime.Serialization.DataMember(Order = ", order, ", EmitDefaultValue = false)]");
            SpecEmit.Line(builder, "        public ", propertyType, " ", member.Name, " { get; set; }");
        }

        private static string GetOrder(SpecControlModel control, SpecMemberModel member)
        {
            return (10 + control.Members.IndexOf(member)).ToString(CultureInfo.InvariantCulture);
        }

        private static void AppendGetChildren(StringBuilder builder, SpecControlModel control)
        {
            SpecMemberModel? childList = control.Members.Find(static member => SpecEmit.Is(member.Type, "childList"));
            SpecMemberModel? child = control.Members.Find(static member => SpecEmit.Is(member.Type, "child"));
            if (childList is not null)
            {
                _ = builder.AppendLine();
                _ = builder.AppendLine("        /// <inheritdoc />");
                _ = builder.AppendLine("        internal override global::System.Collections.Generic.IEnumerable<SpecNode> GetChildren()");
                _ = builder.AppendLine("        {");
                SpecEmit.Line(builder, "            return ", childList.Name, ";");
                _ = builder.AppendLine("        }");
                return;
            }
            if (child is not null)
            {
                _ = builder.AppendLine();
                _ = builder.AppendLine("        /// <inheritdoc />");
                _ = builder.AppendLine("        internal override global::System.Collections.Generic.IEnumerable<SpecNode> GetChildren()");
                _ = builder.AppendLine("        {");
                SpecEmit.Line(builder, "            if (", child.Name, " is not null)");
                _ = builder.AppendLine("            {");
                SpecEmit.Line(builder, "                yield return ", child.Name, ";");
                _ = builder.AppendLine("            }");
                _ = builder.AppendLine("        }");
            }
        }
    }
}
