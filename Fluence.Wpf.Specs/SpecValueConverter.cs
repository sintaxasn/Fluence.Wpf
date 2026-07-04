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
using System.Globalization;

namespace Fluence.Wpf.Specs
{
    /// <summary>
    /// Culture-invariant value coercion helpers shared by the hand-written and generated
    /// IDictionary-style spec constructors. Centralizing coercion keeps the generated property
    /// switches tiny and consistent.
    /// </summary>
    internal static class SpecValueConverter
    {
        /// <summary>
        /// Unwraps a PowerShell PSObject wrapper to its base object, without referencing the
        /// PowerShell SDK. PowerShell auto-unwraps in its own operators and typed parameter
        /// binding, but raw object arrays reaching these converters keep the wrappers.
        /// </summary>
        /// <param name="value">The possibly wrapped value.</param>
        /// <returns>The unwrapped value.</returns>
        public static object? Unwrap(object? value)
        {
            if (value is null)
            {
                return null;
            }
            Type type = value.GetType();
            return string.Equals(type.FullName, "System.Management.Automation.PSObject", StringComparison.Ordinal)
                ? type.GetProperty("BaseObject")?.GetValue(value)
                : value;
        }

        /// <summary>
        /// Converts a dictionary key to its property-name string form.
        /// </summary>
        /// <param name="key">The raw dictionary key.</param>
        /// <returns>The key as an invariant string.</returns>
        public static string ToPropertyKey(object? key)
        {
            return Convert.ToString(Unwrap(key), CultureInfo.InvariantCulture) ?? string.Empty;
        }

        /// <summary>
        /// Converts a value to its invariant string form, or null.
        /// </summary>
        /// <param name="value">The raw value.</param>
        /// <returns>The invariant string, or null when the value is null.</returns>
        public static string? ToText(object? value)
        {
            value = Unwrap(value);
            return value is null ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts a value to Base64 text: a byte array is encoded, a string passes through
        /// as already-encoded Base64, and null stays null.
        /// </summary>
        /// <param name="value">The raw value (byte array, Base64 string, or null).</param>
        /// <returns>The Base64 text, or null when the value is null.</returns>
        public static string? ToBase64Text(object? value)
        {
            value = Unwrap(value);
            return value switch
            {
                null => null,
                byte[] bytes => Convert.ToBase64String(bytes),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture),
            };
        }

        /// <summary>
        /// Converts a value to a nullable boolean (accepting booleans and boolean-like strings).
        /// </summary>
        /// <param name="value">The raw value.</param>
        /// <returns>The boolean, or null when the value is null.</returns>
        public static bool? ToNullableBoolean(object? value)
        {
            value = Unwrap(value);
            return value is null ? null : Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts a value to a non-nullable boolean.
        /// </summary>
        /// <param name="value">The raw value.</param>
        /// <returns>The boolean; null converts to false.</returns>
        public static bool ToBoolean(object? value)
        {
            value = Unwrap(value);
            return value is not null && Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts a value to a nullable 32-bit integer.
        /// </summary>
        /// <param name="value">The raw value.</param>
        /// <returns>The integer, or null when the value is null.</returns>
        public static int? ToNullableInt32(object? value)
        {
            value = Unwrap(value);
            return value is null ? null : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts a value to a nullable double.
        /// </summary>
        /// <param name="value">The raw value.</param>
        /// <returns>The double, or null when the value is null.</returns>
        public static double? ToNullableDouble(object? value)
        {
            value = Unwrap(value);
            return value is null ? null : Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts a value to a nullable <see cref="DateTime"/>.
        /// </summary>
        /// <param name="value">The raw value.</param>
        /// <returns>The date, or null when the value is null.</returns>
        public static DateTime? ToNullableDateTime(object? value)
        {
            value = Unwrap(value);
            return value switch
            {
                null => null,
                DateTime dateTime => dateTime,
                _ => Convert.ToDateTime(value, CultureInfo.InvariantCulture),
            };
        }

        /// <summary>
        /// Converts a value to a nullable <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="value">The raw value.</param>
        /// <returns>The time span, or null when the value is null.</returns>
        public static TimeSpan? ToNullableTimeSpan(object? value)
        {
            value = Unwrap(value);
            return value switch
            {
                null => null,
                TimeSpan timeSpan => timeSpan,
                _ => TimeSpan.Parse(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty, CultureInfo.InvariantCulture),
            };
        }

        /// <summary>
        /// Converts a value to a mirrored spec enum, accepting enum values and case-insensitive names.
        /// </summary>
        /// <typeparam name="TEnum">The mirrored spec enum type.</typeparam>
        /// <param name="value">The raw value.</param>
        /// <param name="memberName">The spec member name, used in the failure message.</param>
        /// <returns>The parsed enum value.</returns>
        /// <exception cref="ArgumentException">Thrown when the value is not a valid enum name or value.</exception>
        public static TEnum ToEnum<TEnum>(object? value, string memberName)
            where TEnum : struct, Enum
        {
            value = Unwrap(value);
            if (value is TEnum direct)
            {
                return direct;
            }
            string? text = ToText(value);
            return text is not null && Enum.TryParse(text, ignoreCase: true, out TEnum parsed)
                ? parsed
                : throw new ArgumentException(FormattableString.Invariant($"Value '{value}' is not valid for {memberName}. Valid values: {string.Join(", ", Enum.GetNames(typeof(TEnum)))}."), memberName);
        }

        /// <summary>
        /// Converts a value to a nullable mirrored spec enum.
        /// </summary>
        /// <typeparam name="TEnum">The mirrored spec enum type.</typeparam>
        /// <param name="value">The raw value.</param>
        /// <param name="memberName">The spec member name, used in the failure message.</param>
        /// <returns>The parsed enum value, or null when the value is null.</returns>
        public static TEnum? ToNullableEnum<TEnum>(object? value, string memberName)
            where TEnum : struct, Enum
        {
            return value is null ? null : ToEnum<TEnum>(value, memberName);
        }

        /// <summary>
        /// Appends string items to a collection from a single string or any enumerable of values.
        /// </summary>
        /// <param name="target">The collection to fill.</param>
        /// <param name="value">A string, an enumerable of values, or null (no-op).</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> is null.</exception>
        public static void FillStrings(IList<string> target, object? value)
        {
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            value = Unwrap(value);
            if (value is null)
            {
                return;
            }
            if (value is string single)
            {
                target.Add(single);
                return;
            }
            if (value is IEnumerable sequence)
            {
                foreach (object? item in sequence)
                {
                    target.Add(ToText(item) ?? string.Empty);
                }
                return;
            }
            target.Add(ToText(value) ?? string.Empty);
        }

        /// <summary>
        /// Appends spec nodes to a collection from a single node or any enumerable of nodes.
        /// </summary>
        /// <param name="target">The collection to fill.</param>
        /// <param name="value">A <see cref="SpecNode"/>, an enumerable of nodes, or null (no-op).</param>
        /// <param name="memberName">The spec member name, used in the failure message.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when an item is not a <see cref="SpecNode"/>.</exception>
        public static void FillNodes(IList<SpecNode> target, object? value, string memberName)
        {
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            value = Unwrap(value);
            if (value is null)
            {
                return;
            }
            if (value is SpecNode single)
            {
                target.Add(single);
                return;
            }
            if (value is IEnumerable sequence and not string)
            {
                foreach (object? item in sequence)
                {
                    target.Add(ToNode(item, memberName));
                }
                return;
            }
            throw new ArgumentException($"Value of type '{value.GetType().FullName}' is not valid for {memberName}; expected one or more {nameof(SpecNode)} instances.", memberName);
        }

        /// <summary>
        /// Appends validation rules to a collection from a single rule or any enumerable of rules.
        /// </summary>
        /// <param name="target">The collection to fill.</param>
        /// <param name="value">A <see cref="SpecRule"/>, an enumerable of rules, or null (no-op).</param>
        /// <param name="memberName">The spec member name, used in the failure message.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when an item is not a <see cref="SpecRule"/>.</exception>
        public static void FillRules(IList<SpecRule> target, object? value, string memberName)
        {
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            value = Unwrap(value);
            if (value is null)
            {
                return;
            }
            if (value is SpecRule single)
            {
                target.Add(single);
                return;
            }
            if (value is IEnumerable sequence and not string)
            {
                foreach (object? rawItem in sequence)
                {
                    object? item = Unwrap(rawItem);
                    if (item is not SpecRule rule)
                    {
                        throw new ArgumentException($"Value of type '{item?.GetType().FullName ?? "null"}' is not valid for {memberName}; expected one or more {nameof(SpecRule)} instances.", memberName);
                    }
                    target.Add(rule);
                }
                return;
            }
            throw new ArgumentException($"Value of type '{value.GetType().FullName}' is not valid for {memberName}; expected one or more {nameof(SpecRule)} instances.", memberName);
        }

        /// <summary>
        /// Converts a value to a single <see cref="SpecNode"/>.
        /// </summary>
        /// <param name="value">The raw value.</param>
        /// <param name="memberName">The spec member name, used in the failure message.</param>
        /// <returns>The node, or null when the value is null.</returns>
        /// <exception cref="ArgumentException">Thrown when the value is not a <see cref="SpecNode"/>.</exception>
        public static SpecNode? ToNullableNode(object? value, string memberName)
        {
            value = Unwrap(value);
            return value is null ? null : ToNode(value, memberName);
        }

        private static SpecNode ToNode(object? value, string memberName)
        {
            return Unwrap(value) is SpecNode node
                ? node
                : throw new ArgumentException($"Value of type '{value?.GetType().FullName ?? "null"}' is not valid for {memberName}; expected a {nameof(SpecNode)}.", memberName);
        }
    }
}
