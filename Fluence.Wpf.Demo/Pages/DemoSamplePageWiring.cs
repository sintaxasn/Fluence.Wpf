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
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    internal readonly struct DemoSampleSource
    {
        public DemoSampleSource(int slot, string xamlSource, string cSharpSource)
        {
            if (slot <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), "Slot must be greater than zero.");
            }

            Slot = slot;
            XamlSource = xamlSource ?? throw new ArgumentNullException(nameof(xamlSource));
            CSharpSource = cSharpSource ?? throw new ArgumentNullException(nameof(cSharpSource));
        }

        public int Slot { get; }

        public string XamlSource { get; }

        public string CSharpSource { get; }
    }

    internal static class DemoSamplePageWiring
    {
        internal static void Apply(DependencyObject root, params DemoSampleSource[] sources)
        {
            if (root is null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (sources is null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            List<DemoSampleControl> samples = [];
            CollectDemoSampleControls(root, samples);

            Dictionary<int, DemoSampleSource> sourceBySlot = CreateSourceMap(sources, samples.Count);
            if (sourceBySlot.Count != samples.Count)
            {
                throw new InvalidOperationException(
                    "Every DemoSampleControl in the page must have exactly one DemoSampleSource registration.");
            }

            ApplyContentSlots(root, samples);

            for (int i = 0; i < samples.Count; i++)
            {
                int slot = i + 1;
                DemoSampleSource source = sourceBySlot[slot];
                DemoSampleControl sample = samples[i];
                sample.XamlSource = source.XamlSource;
                sample.CSharpSource = source.CSharpSource;
            }
        }

        private static Dictionary<int, DemoSampleSource> CreateSourceMap(
            IEnumerable<DemoSampleSource> sources,
            int sampleCount)
        {
            Dictionary<int, DemoSampleSource> sourceBySlot = [];
            foreach (DemoSampleSource source in sources)
            {
                if (source.Slot <= 0)
                {
                    throw new InvalidOperationException("DemoSampleSource slot must be greater than zero.");
                }

                if (source.Slot > sampleCount)
                {
                    throw new InvalidOperationException(
                        "DemoSampleSource slot " + source.Slot.ToString(CultureInfo.InvariantCulture) +
                        " does not match a DemoSampleControl.");
                }

                if (sourceBySlot.ContainsKey(source.Slot))
                {
                    throw new InvalidOperationException(
                        "Duplicate DemoSampleSource slot " + source.Slot.ToString(CultureInfo.InvariantCulture) + ".");
                }

                sourceBySlot.Add(source.Slot, source);
            }

            return sourceBySlot;
        }

        private static void ApplyContentSlots(DependencyObject root, IList<DemoSampleControl> samples)
        {
            Dictionary<int, ContentControl> demoSlots = [];
            Dictionary<int, ContentControl> outputSlots = [];
            Dictionary<int, ContentControl> rightRailSlots = [];
            CollectContentSlots(root, demoSlots, outputSlots, rightRailSlots);
            ValidateSlotTargets(samples.Count, demoSlots, "DemoContentHost");
            ValidateSlotTargets(samples.Count, outputSlots, "OutputContentHost");
            ValidateSlotTargets(samples.Count, rightRailSlots, "RightRailContentHost");

            for (int i = 0; i < samples.Count; i++)
            {
                int slotIndex = i + 1;
                DemoSampleControl sample = samples[i];
                if (demoSlots.TryGetValue(slotIndex, out ContentControl? demoSlot))
                {
                    sample.DemoContent = TakeSlotContent(demoSlot);
                }

                if (outputSlots.TryGetValue(slotIndex, out ContentControl? outputSlot))
                {
                    sample.OutputContent = TakeSlotContent(outputSlot);
                }

                if (rightRailSlots.TryGetValue(slotIndex, out ContentControl? rightRailSlot))
                {
                    sample.RightRailContent = TakeSlotContent(rightRailSlot);
                }
            }
        }

        private static void ValidateSlotTargets(
            int sampleCount,
            IDictionary<int, ContentControl> slots,
            string suffix)
        {
            foreach (int slot in slots.Keys)
            {
                if (slot > sampleCount)
                {
                    throw new InvalidOperationException(
                        "DemoSampleSlot" + slot.ToString("00", CultureInfo.InvariantCulture) +
                        suffix + " does not match a DemoSampleControl.");
                }
            }
        }

        private static object? TakeSlotContent(ContentControl slot)
        {
            object? content = slot.Content;
            slot.Content = null;
            return content;
        }

        private static void CollectContentSlots(
            DependencyObject current,
            IDictionary<int, ContentControl> demoSlots,
            IDictionary<int, ContentControl> outputSlots,
            IDictionary<int, ContentControl> rightRailSlots)
        {
            if (current is ContentControl contentControl && !string.IsNullOrWhiteSpace(contentControl.Name))
            {
                AddSlotIfMatched(contentControl, "DemoContentHost", demoSlots);
                AddSlotIfMatched(contentControl, "OutputContentHost", outputSlots);
                AddSlotIfMatched(contentControl, "RightRailContentHost", rightRailSlots);
            }

            foreach (object child in LogicalTreeHelper.GetChildren(current))
            {
                if (child is DependencyObject childObject)
                {
                    CollectContentSlots(childObject, demoSlots, outputSlots, rightRailSlots);
                }
            }
        }

        private static void AddSlotIfMatched(
            ContentControl slot,
            string suffix,
            IDictionary<int, ContentControl> slots)
        {
            const string prefix = "DemoSampleSlot";
            string name = slot.Name;
            if (!name.StartsWith(prefix, StringComparison.Ordinal) ||
                !name.EndsWith(suffix, StringComparison.Ordinal))
            {
                return;
            }

            string indexText = name.Substring(prefix.Length, name.Length - prefix.Length - suffix.Length);
            if (!int.TryParse(indexText, NumberStyles.None, CultureInfo.InvariantCulture, out int index))
            {
                throw new InvalidOperationException("Invalid demo sample slot name: " + name + ".");
            }

            if (index <= 0)
            {
                throw new InvalidOperationException("Demo sample slot index must be greater than zero: " + name + ".");
            }

            if (slots.ContainsKey(index))
            {
                throw new InvalidOperationException("Duplicate demo sample slot name for " + name + ".");
            }

            slots.Add(index, slot);
        }

        private static void CollectDemoSampleControls(DependencyObject current, ICollection<DemoSampleControl> samples)
        {
            if (current is DemoSampleControl sample)
            {
                samples.Add(sample);
                return;
            }

            foreach (object child in LogicalTreeHelper.GetChildren(current))
            {
                if (child is DependencyObject childObject)
                {
                    CollectDemoSampleControls(childObject, samples);
                }
            }
        }
    }
}
