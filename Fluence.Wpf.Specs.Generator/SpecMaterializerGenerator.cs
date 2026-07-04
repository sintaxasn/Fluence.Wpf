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
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Fluence.Wpf.Specs.Generator
{
    /// <summary>
    /// Emits the generated half of Fluence.Wpf's SpecMaterializer (the per-control create, apply,
    /// harvest, and enum-mapping table) from SpecSurface.xml, and validates every manifest control,
    /// member, and enum value against the real compiled symbols. Drift is a build error
    /// (FLSPEC001-FLSPEC004). Inactive in every compilation other than Fluence.Wpf.
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class SpecMaterializerGenerator : IIncrementalGenerator
    {
        /// <inheritdoc />
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValueProvider<ImmutableArray<string>> manifests = context.AdditionalTextsProvider
                .Where(static text => string.Equals(Path.GetFileName(text.Path), SpecEmit.ManifestFileName, StringComparison.OrdinalIgnoreCase))
                .Select(static (text, cancellationToken) => text.GetText(cancellationToken)?.ToString() ?? string.Empty)
                .Collect();
            context.RegisterSourceOutput(context.CompilationProvider.Combine(manifests), static (productionContext, source) => Execute(productionContext, source.Left, source.Right));
        }

        private static void Execute(SourceProductionContext context, Compilation compilation, ImmutableArray<string> manifests)
        {
            if (!string.Equals(compilation.AssemblyName, "Fluence.Wpf", StringComparison.Ordinal))
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
            bool hasDrift = false;
            foreach (SpecEnumModel enumModel in model.Enums)
            {
                hasDrift |= !ValidateEnum(context, compilation, enumModel);
            }
            Dictionary<string, string> valueMemberTypes = new(StringComparer.Ordinal);
            foreach (SpecControlModel control in model.Controls)
            {
                hasDrift |= !ValidateControl(context, compilation, model, control, valueMemberTypes);
            }
            if (hasDrift)
            {
                return;
            }
            context.AddSource("SpecMaterializer.g.cs", SourceText.From(EmitMaterializer(model, valueMemberTypes), Encoding.UTF8));
        }

        private static bool ValidateEnum(SourceProductionContext context, Compilation compilation, SpecEnumModel enumModel)
        {
            INamedTypeSymbol? clrEnum = compilation.GetTypeByMetadataName(enumModel.Clr);
            if (clrEnum is null || clrEnum.TypeKind != TypeKind.Enum)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.EnumDrift, Location.None, enumModel.Name, enumModel.Clr, "the CLR enum type was not found"));
                return false;
            }
            HashSet<string> clrNames = new(clrEnum.GetMembers().OfType<IFieldSymbol>().Where(static field => field.ConstantValue is not null).Select(static field => field.Name), StringComparer.Ordinal);
            HashSet<string> manifestNames = new(enumModel.Values.Select(static value => value.Name), StringComparer.Ordinal);
            List<string> problems = [];
            foreach (string name in manifestNames.Where(name => !clrNames.Contains(name)))
            {
                problems.Add("manifest value '" + name + "' does not exist on the CLR enum");
            }
            foreach (string name in clrNames.Where(name => !manifestNames.Contains(name)))
            {
                problems.Add("CLR value '" + name + "' is not mirrored in the manifest");
            }
            if (problems.Count > 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.EnumDrift, Location.None, enumModel.Name, enumModel.Clr, string.Join("; ", problems)));
                return false;
            }
            return true;
        }

        private static bool ValidateControl(SourceProductionContext context, Compilation compilation, SpecSurfaceModel model, SpecControlModel control, Dictionary<string, string> valueMemberTypes)
        {
            INamedTypeSymbol? controlType = compilation.GetTypeByMetadataName(control.Clr);
            if (controlType is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ControlNotFound, Location.None, control.Name, control.Clr));
                return false;
            }
            bool valid = true;
            foreach (SpecMemberModel member in control.Members)
            {
                IPropertySymbol? property = FindProperty(controlType, member.Name);
                if (property is null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MemberNotFound, Location.None, member.Name, control.Name, control.Clr));
                    valid = false;
                    continue;
                }
                string display = property.Type.ToDisplayString();
                if (!IsCompatible(model, member.Type, display))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MemberTypeIncompatible, Location.None, member.Name, control.Name, member.Type, display));
                    valid = false;
                }
            }
            if (control.ValueMember is string valueMember)
            {
                IPropertySymbol? property = FindProperty(controlType, valueMember);
                if (property is null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MemberNotFound, Location.None, valueMember, control.Name, control.Clr));
                    valid = false;
                }
                else
                {
                    valueMemberTypes[control.Name] = property.Type.ToDisplayString();
                }
            }
            return valid;
        }

        private static IPropertySymbol? FindProperty(INamedTypeSymbol type, string name)
        {
            for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
            {
                IPropertySymbol? property = current.GetMembers(name).OfType<IPropertySymbol>().FirstOrDefault();
                if (property is not null)
                {
                    return property;
                }
            }
            return null;
        }

        private static bool IsCompatible(SpecSurfaceModel model, string manifestType, string display)
        {
            if (manifestType.StartsWith("enum:", StringComparison.Ordinal))
            {
                string enumName = manifestType.Substring(5);
                string? clr = model.Enums.Find(entry => SpecEmit.Is(entry.Name, enumName))?.Clr;
                return clr is not null && (SpecEmit.Is(display, clr) || SpecEmit.Is(display, clr + "?"));
            }
            return manifestType switch
            {
                "text" => SpecEmit.Is(display, "string") || SpecEmit.Is(display, "string?") || SpecEmit.Is(display, "object") || SpecEmit.Is(display, "object?"),
                "uri" => SpecEmit.Is(display, "System.Uri") || SpecEmit.Is(display, "System.Uri?") || SpecEmit.Is(display, "string") || SpecEmit.Is(display, "string?"),
                "thickness" => SpecEmit.Is(display, "System.Windows.Thickness"),
                "boolOpt" => SpecEmit.Is(display, "bool") || SpecEmit.Is(display, "bool?"),
                "intOpt" => SpecEmit.Is(display, "int") || SpecEmit.Is(display, "int?"),
                "doubleOpt" => SpecEmit.Is(display, "double") || SpecEmit.Is(display, "double?"),
                "dateOpt" => SpecEmit.Is(display, "System.DateTime") || SpecEmit.Is(display, "System.DateTime?"),
                "timeOpt" => SpecEmit.Is(display, "System.TimeSpan") || SpecEmit.Is(display, "System.TimeSpan?"),
                "stringList" or "childList" or "child" => true,
                _ => false,
            };
        }

        private static string EmitMaterializer(SpecSurfaceModel model, Dictionary<string, string> valueMemberTypes)
        {
            StringBuilder builder = new();
            _ = builder.Append(SpecEmit.FilePreamble);
            _ = builder.AppendLine();
            _ = builder.AppendLine("namespace Fluence.Wpf.Specs");
            _ = builder.AppendLine("{");
            SpecEmit.Line(builder, "    [global::System.CodeDom.Compiler.GeneratedCode(\"", SpecEmit.ToolName, "\", \"", SpecEmit.ToolVersion, "\")]");
            _ = builder.AppendLine("    public static partial class SpecMaterializer");
            _ = builder.AppendLine("    {");
            EmitCreateCore(builder, model);
            EmitHarvestCore(builder, model, valueMemberTypes);
            foreach (SpecControlModel control in model.Controls)
            {
                EmitCreator(builder, control);
            }
            foreach (SpecEnumModel enumModel in model.Enums)
            {
                EmitEnumMapper(builder, enumModel);
            }
            _ = builder.AppendLine("    }");
            _ = builder.AppendLine("}");
            return builder.ToString();
        }

        private static void EmitCreateCore(StringBuilder builder, SpecSurfaceModel model)
        {
            _ = builder.AppendLine("        private static partial global::System.Windows.FrameworkElement CreateElementCore(global::Fluence.Wpf.Specs.SpecNode node)");
            _ = builder.AppendLine("        {");
            _ = builder.AppendLine("            return node switch");
            _ = builder.AppendLine("            {");
            foreach (SpecControlModel control in model.Controls)
            {
                SpecEmit.Line(builder, "                global::Fluence.Wpf.Specs.", control.Name, "Spec spec => Create", control.Name, "Element(spec),");
            }
            _ = builder.AppendLine("                _ => throw new global::System.NotSupportedException(\"Spec node type '\" + node.GetType().FullName + \"' has no materializer entry; Fluence.Wpf and Fluence.Wpf.Specs must be a matched pair.\"),");
            _ = builder.AppendLine("            };");
            _ = builder.AppendLine("        }");
        }

        private static void EmitHarvestCore(StringBuilder builder, SpecSurfaceModel model, Dictionary<string, string> valueMemberTypes)
        {
            _ = builder.AppendLine();
            _ = builder.AppendLine("        private static partial void HarvestValueCore(global::Fluence.Wpf.Specs.SpecNode node, global::System.Windows.FrameworkElement element, global::System.Collections.Generic.IDictionary<string, object?> values)");
            _ = builder.AppendLine("        {");
            _ = builder.AppendLine("            switch (node)");
            _ = builder.AppendLine("            {");
            foreach (SpecControlModel control in model.Controls.Where(static entry => entry.ValueKind is not null))
            {
                SpecEmit.Line(builder, "                case global::Fluence.Wpf.Specs.", control.Name, "Spec spec when element is global::", control.Clr, " control:");
                if (SpecEmit.Is(control.ValueKind, "RadioGroup"))
                {
                    _ = builder.AppendLine("                    if (control.IsChecked == true && spec.GroupName is not null)");
                    _ = builder.AppendLine("                    {");
                    _ = builder.AppendLine("                        values[spec.GroupName] = control.Content as string;");
                    _ = builder.AppendLine("                    }");
                }
                else
                {
                    _ = builder.AppendLine("                    if (spec.Name is not null)");
                    _ = builder.AppendLine("                    {");
                    SpecEmit.Line(builder, "                        values[spec.Name] = ", GetHarvestExpression(control, valueMemberTypes), ";");
                    _ = builder.AppendLine("                    }");
                }
                _ = builder.AppendLine("                    break;");
            }
            _ = builder.AppendLine("                default:");
            _ = builder.AppendLine("                    break;");
            _ = builder.AppendLine("            }");
            _ = builder.AppendLine("        }");
        }

        private static string GetHarvestExpression(SpecControlModel control, Dictionary<string, string> valueMemberTypes)
        {
            string access = "control." + control.ValueMember;
            string display = valueMemberTypes.TryGetValue(control.Name, out string? value) ? value : string.Empty;
            return control.ValueKind switch
            {
                "Boolean" => access + " == true",
                "String" when SpecEmit.Is(display, "object") => access + " as string",
                _ => access,
            };
        }

        private static void EmitCreator(StringBuilder builder, SpecControlModel control)
        {
            _ = builder.AppendLine();
            SpecEmit.Line(builder, "        private static global::System.Windows.FrameworkElement Create", control.Name, "Element(global::Fluence.Wpf.Specs.", control.Name, "Spec spec)");
            _ = builder.AppendLine("        {");
            SpecEmit.Line(builder, "            global::", control.Clr, " control = new global::", control.Clr, "();");
            _ = builder.AppendLine("            ApplyCommonProperties(control, spec);");
            foreach (SpecMemberModel member in control.Members)
            {
                EmitApply(builder, member);
            }
            _ = builder.AppendLine("            return control;");
            _ = builder.AppendLine("        }");
        }

        private static void EmitApply(StringBuilder builder, SpecMemberModel member)
        {
            string specAccess = "spec." + member.Name;
            string controlAccess = "control." + member.Name;
            if (member.EnumName is string enumName)
            {
                SpecEmit.Line(builder, "            if (", specAccess, ".HasValue)");
                _ = builder.AppendLine("            {");
                SpecEmit.Line(builder, "                ", controlAccess, " = Map", enumName, "(", specAccess, ".Value);");
                _ = builder.AppendLine("            }");
                return;
            }
            switch (member.Type)
            {
                case "text":
                    SpecEmit.Line(builder, "            if (", specAccess, " is not null)");
                    _ = builder.AppendLine("            {");
                    SpecEmit.Line(builder, "                ", controlAccess, " = ", specAccess, ";");
                    _ = builder.AppendLine("            }");
                    break;
                case "uri":
                    SpecEmit.Line(builder, "            if (", specAccess, " is not null)");
                    _ = builder.AppendLine("            {");
                    SpecEmit.Line(builder, "                ", controlAccess, " = new global::System.Uri(", specAccess, ", global::System.UriKind.RelativeOrAbsolute);");
                    _ = builder.AppendLine("            }");
                    break;
                case "thickness":
                    SpecEmit.Line(builder, "            if (", specAccess, " is not null)");
                    _ = builder.AppendLine("            {");
                    SpecEmit.Line(builder, "                ", controlAccess, " = ParseThickness(", specAccess, ");");
                    _ = builder.AppendLine("            }");
                    break;
                case "stringList":
                    SpecEmit.Line(builder, "            foreach (string item in ", specAccess, ")");
                    _ = builder.AppendLine("            {");
                    SpecEmit.Line(builder, "                _ = ", controlAccess, ".Add(item);");
                    _ = builder.AppendLine("            }");
                    break;
                case "childList":
                    SpecEmit.Line(builder, "            foreach (global::Fluence.Wpf.Specs.SpecNode childNode in ", specAccess, ")");
                    _ = builder.AppendLine("            {");
                    SpecEmit.Line(builder, "                _ = ", controlAccess, ".Add(CreateElementCore(childNode));");
                    _ = builder.AppendLine("            }");
                    break;
                case "child":
                    SpecEmit.Line(builder, "            if (", specAccess, " is not null)");
                    _ = builder.AppendLine("            {");
                    SpecEmit.Line(builder, "                ", controlAccess, " = CreateElementCore(", specAccess, ");");
                    _ = builder.AppendLine("            }");
                    break;
                default:
                    SpecEmit.Line(builder, "            if (", specAccess, ".HasValue)");
                    _ = builder.AppendLine("            {");
                    SpecEmit.Line(builder, "                ", controlAccess, " = ", specAccess, ".Value;");
                    _ = builder.AppendLine("            }");
                    break;
            }
        }

        private static void EmitEnumMapper(StringBuilder builder, SpecEnumModel enumModel)
        {
            _ = builder.AppendLine();
            SpecEmit.Line(builder, "        private static global::", enumModel.Clr, " Map", enumModel.Name, "(global::Fluence.Wpf.Specs.", enumModel.Name, " value)");
            _ = builder.AppendLine("        {");
            _ = builder.AppendLine("            switch (value)");
            _ = builder.AppendLine("            {");
            foreach (SpecEnumValueModel enumValue in enumModel.Values)
            {
                SpecEmit.Line(builder, "                case global::Fluence.Wpf.Specs.", enumModel.Name, ".", enumValue.Name, ":");
                SpecEmit.Line(builder, "                    return global::", enumModel.Clr, ".", enumValue.Name, ";");
            }
            _ = builder.AppendLine("                default:");
            SpecEmit.Line(builder, "                    throw new global::System.ArgumentOutOfRangeException(nameof(value), value, \"Unknown ", enumModel.Name, " value.\");");
            _ = builder.AppendLine("            }");
            _ = builder.AppendLine("        }");
        }
    }
}
