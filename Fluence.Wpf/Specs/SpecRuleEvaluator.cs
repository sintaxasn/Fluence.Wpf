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
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;

namespace Fluence.Wpf.Specs
{
    /// <summary>
    /// Evaluates declarative validation rules against the live values of materialized input
    /// elements when the dialog commits on a non-cancel button. Mirrors the module's
    /// Test-FluenceInput semantics: evaluation stops at the first failure and its message is
    /// surfaced in the dialog's validation InfoBar.
    /// </summary>
    internal static class SpecRuleEvaluator
    {
        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Evaluates every rule-bearing pair and returns the first failure message, or null when
        /// all rules pass.
        /// </summary>
        /// <param name="pairs">The (spec, element) pairs of the materialized dialog.</param>
        /// <returns>The first failure message, or null.</returns>
        public static string? Evaluate(IReadOnlyList<KeyValuePair<SpecNode, FrameworkElement>> pairs)
        {
            foreach (KeyValuePair<SpecNode, FrameworkElement> pair in pairs)
            {
                SpecNode node = pair.Key;
                if (node.Rules.Count == 0 || node.Name is not string name || string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }
                Dictionary<string, object?> probe = new(StringComparer.OrdinalIgnoreCase);
                SpecMaterializer.HarvestValue(node, pair.Value, probe);
                _ = probe.TryGetValue(name, out object? value);
                string? failure = EvaluateRules(node, name, value);
                if (failure is not null)
                {
                    return failure;
                }
            }
            return null;
        }

        private static string? EvaluateRules(SpecNode node, string name, object? value)
        {
            string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            foreach (SpecRule rule in node.Rules)
            {
                string? failure = rule switch
                {
                    NotEmptyRule => string.IsNullOrWhiteSpace(text) ? "'" + name + "' is required." : null,
                    PatternRule pattern => EvaluatePattern(name, text, pattern),
                    LengthRule length => EvaluateLength(name, text, length),
                    RangeRule range => EvaluateRange(name, value, text, range),
                    _ => "'" + name + "' has an unrecognized rule of type '" + rule.GetType().FullName + "'; Fluence.Wpf and Fluence.Wpf.Specs must be a matched pair.",
                };
                if (failure is not null)
                {
                    return rule.ErrorMessage ?? failure;
                }
            }
            return null;
        }

        private static string? EvaluatePattern(string name, string text, PatternRule rule)
        {
            if (string.IsNullOrWhiteSpace(rule.Pattern) || string.IsNullOrWhiteSpace(text))
            {
                return null;
            }
            try
            {
                return Regex.IsMatch(text, rule.Pattern, RegexOptions.None, PatternTimeout)
                    ? null
                    : $"'{name}' does not match the required format.";
            }
            catch (RegexMatchTimeoutException)
            {
                return $"'{name}' could not be validated; the pattern timed out.";
            }
        }

        private static string? EvaluateLength(string name, string text, LengthRule rule)
        {
            return rule.MinLength is { } minLength && text.Length < minLength
                ? "'" + name + "' requires at least " + minLength.ToString(CultureInfo.InvariantCulture) + " characters."
                : rule.MaxLength is { } maxLength && text.Length > maxLength
                ? "'" + name + "' allows at most " + maxLength.ToString(CultureInfo.InvariantCulture) + " characters."
                : null;
        }

        private static string? EvaluateRange(string name, object? value, string text, RangeRule rule)
        {
            double number;
            if (value is double direct)
            {
                number = direct;
            }
            else if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
            {
                return "'" + name + "' must be a number.";
            }
            return rule.Minimum is { } minimum && number < minimum
                ? "'" + name + "' must be at least " + minimum.ToString(CultureInfo.InvariantCulture) + "."
                : rule.Maximum is { } maximum && number > maximum
                ? "'" + name + "' must be at most " + maximum.ToString(CultureInfo.InvariantCulture) + "."
                : null;
        }
    }
}
