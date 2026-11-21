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
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Threading;

namespace Fluence.Wpf.Helpers
{
    internal static class AcrylicMaterialBudget
    {
        internal const long MaxSurfacePaddedPixels = 2_146_176;
        internal const long MaxAggregatePaddedPixels = 2_172_672;
        internal const int MaxActiveSurfaces = 2;

        private static readonly Lock Gate = new();
        private static readonly List<Entry> Entries = [];

        internal static bool TryReserve(IAcrylicBudgetParticipant participant, AcrylicSourceMapResult map)
        {
            ArgumentNullException.ThrowIfNull(participant);
            ArgumentNullException.ThrowIfNull(map);
            if (!TryCountPaddedPixels(map, out long requested))
            {
                Release(participant);
                return false;
            }

            List<IAcrylicBudgetParticipant> wake = [];
            bool accepted;
            lock (Gate)
            {
                bool freedByCollection = SweepDead();
                Entry? current = Find(participant);
                long oldPixels = current?.Pixels ?? 0;
                long allocatedElsewhere = 0;
                int activeElsewhere = 0;
                foreach (Entry entry in Entries)
                {
                    if (ReferenceEquals(entry, current) || entry.Pixels == 0)
                    {
                        continue;
                    }

                    allocatedElsewhere += entry.Pixels;
                    activeElsewhere++;
                }

                accepted = requested <= MaxSurfacePaddedPixels
                    && requested <= MaxAggregatePaddedPixels - allocatedElsewhere
                    && activeElsewhere < MaxActiveSurfaces;
                if (current is null)
                {
                    current = new Entry(participant);
                    Entries.Add(current);
                }

                current.Pixels = accepted ? requested : 0;
                current.Waiting = !accepted;
                if (freedByCollection || oldPixels > current.Pixels)
                {
                    wake = GatherWaiters(participant);
                }
            }

            Wake(wake);
            return accepted;
        }

        internal static void Release(IAcrylicBudgetParticipant participant)
        {
            ArgumentNullException.ThrowIfNull(participant);
            List<IAcrylicBudgetParticipant> wake = [];
            lock (Gate)
            {
                bool freedByCollection = SweepDead();
                Entry? current = Find(participant);
                bool freed = current?.Pixels > 0;
                if (current is not null)
                {
                    _ = Entries.Remove(current);
                }

                if (freed || freedByCollection)
                {
                    wake = GatherWaiters(participant);
                }
            }

            Wake(wake);
        }

        internal static bool TryCountPaddedPixels(AcrylicSourceMapResult map, out long pixels)
        {
            pixels = 0;
            if (map.Reason is not AcrylicFallbackReason.None
                || !IsFinitePositive(map.PaddedTargetRect.Width)
                || !IsFinitePositive(map.PaddedTargetRect.Height)
                || !IsFinitePositive(map.DeviceScaleX)
                || !IsFinitePositive(map.DeviceScaleY))
            {
                return false;
            }

            double width = Math.Ceiling(map.PaddedTargetRect.Width * map.DeviceScaleX);
            double height = Math.Ceiling(map.PaddedTargetRect.Height * map.DeviceScaleY);
            if (!IsFinitePositive(width) || !IsFinitePositive(height)
                || width > MaxSurfacePaddedPixels || height > MaxSurfacePaddedPixels)
            {
                return false;
            }

            try
            {
                pixels = checked((long)width * (long)height);
                return pixels <= MaxSurfacePaddedPixels;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        private static bool IsFinitePositive(double value)
        {
            return value > 0 && !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static Entry? Find(IAcrylicBudgetParticipant participant)
        {
            foreach (Entry entry in Entries)
            {
                if (entry.Participant.TryGetTarget(out IAcrylicBudgetParticipant? target)
                    && ReferenceEquals(target, participant))
                {
                    return entry;
                }
            }

            return null;
        }

        private static bool SweepDead()
        {
            bool freed = false;
            for (int index = Entries.Count - 1; index >= 0; index--)
            {
                if (!Entries[index].Participant.TryGetTarget(out _))
                {
                    freed |= Entries[index].Pixels > 0;
                    Entries.RemoveAt(index);
                }
            }

            return freed;
        }

        private static List<IAcrylicBudgetParticipant> GatherWaiters(IAcrylicBudgetParticipant except)
        {
            List<IAcrylicBudgetParticipant> waiters = [];
            foreach (Entry entry in Entries)
            {
                if (entry.Waiting && entry.Participant.TryGetTarget(out IAcrylicBudgetParticipant? target)
                    && !ReferenceEquals(target, except))
                {
                    waiters.Add(target);
                }
            }

            return waiters;
        }

        private static void Wake(List<IAcrylicBudgetParticipant> waiters)
        {
            foreach (IAcrylicBudgetParticipant waiter in waiters)
            {
                waiter.RequestBudgetRefresh();
            }
        }

        private sealed class Entry(IAcrylicBudgetParticipant participant)
        {
            internal readonly WeakReference<IAcrylicBudgetParticipant> Participant = new(participant);
            internal long Pixels;
            internal bool Waiting;
        }
    }
}
