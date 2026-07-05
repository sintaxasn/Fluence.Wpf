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
using System.Runtime.CompilerServices;

namespace Fluence.Wpf.Specs
{
    /// <summary>
    /// Fail-fast structural validation for dialog specs, run before serialization and
    /// materialization. The wire format serializes a strict tree (object references are not
    /// preserved), so shared node instances and cycles are rejected here with clear messages
    /// instead of silently duplicating or throwing deep inside the serializer.
    /// </summary>
    public static class SpecTreeValidator
    {
        /// <summary>
        /// Validates a dialog spec's structure: at least one button with non-empty text, no shared
        /// or cyclic node instances, unique names across value-bearing elements, and no rules on
        /// unnamed elements.
        /// </summary>
        /// <param name="spec">The dialog spec to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="spec"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the spec violates a structural constraint.</exception>
        public static void Validate(DialogSpec spec)
        {
            if (spec is null)
            {
                throw new ArgumentNullException(nameof(spec));
            }
            if (spec.Buttons.Count == 0)
            {
                throw new InvalidOperationException("A dialog spec requires at least one button.");
            }
            foreach (ButtonSpec button in spec.Buttons)
            {
                if (string.IsNullOrWhiteSpace(button.Text))
                {
                    throw new InvalidOperationException("Every dialog button requires a non-empty Text.");
                }
            }
            HashSet<SpecNode> visited = new(ReferenceComparer.Instance);
            HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> radioGroups = new(StringComparer.OrdinalIgnoreCase);
            foreach (SpecNode node in spec.Content)
            {
                Visit(node, visited, names, radioGroups);
            }
        }

        private static void Visit(SpecNode node, HashSet<SpecNode> visited, HashSet<string> names, HashSet<string> radioGroups)
        {
            if (node is null)
            {
                throw new InvalidOperationException("A dialog spec content entry is null.");
            }
            if (!visited.Add(node))
            {
                throw new InvalidOperationException($"The spec node '{node.GetType().Name}'{DescribeName(node)} appears more than once in the tree. Specs serialize as a strict tree: shared instances and cycles are not supported; construct a separate instance per placement.");
            }
            if (node.HasRules && string.IsNullOrWhiteSpace(node.Name))
            {
                throw new InvalidOperationException($"The spec node '{node.GetType().Name}' has validation rules but no Name; rules apply to named input elements only.");
            }
            if (node is RadioButtonSpec radioButton)
            {
                if (!string.IsNullOrWhiteSpace(radioButton.GroupName))
                {
                    if (names.Contains(radioButton.GroupName!))
                    {
                        throw new InvalidOperationException($"The RadioButton GroupName '{radioButton.GroupName}' collides with an input Name; radio groups harvest under their GroupName, so it must not match another element's Name.");
                    }
                    _ = radioGroups.Add(radioButton.GroupName!);
                }
            }
            else if (node.IsValueBearing && !string.IsNullOrWhiteSpace(node.Name))
            {
                if (radioGroups.Contains(node.Name!))
                {
                    throw new InvalidOperationException($"The input Name '{node.Name}' collides with a RadioButton GroupName; radio groups harvest under their GroupName, so it must not match another element's Name.");
                }
                if (!names.Add(node.Name!))
                {
                    throw new InvalidOperationException($"Duplicate input name '{node.Name}'. Every value-bearing element needs a unique Name because names key the result values.");
                }
            }
            foreach (SpecNode child in node.GetChildren())
            {
                Visit(child, visited, names, radioGroups);
            }
        }

        private static string DescribeName(SpecNode node)
        {
            return string.IsNullOrWhiteSpace(node.Name) ? string.Empty : $" (Name: '{node.Name}')";
        }

        private sealed class ReferenceComparer : IEqualityComparer<SpecNode>
        {
            public static ReferenceComparer Instance { get; } = new();

            public bool Equals(SpecNode? x, SpecNode? y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(SpecNode obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
