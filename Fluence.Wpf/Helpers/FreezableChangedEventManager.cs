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
using System.Windows;

namespace Fluence.Wpf.Helpers
{
    internal sealed class FreezableChangedEventManager : WeakEventManager
    {
        private FreezableChangedEventManager()
        {
        }

        private static FreezableChangedEventManager CurrentManager
        {
            get
            {
                Type managerType = typeof(FreezableChangedEventManager);
                if (GetCurrentManager(managerType) is not FreezableChangedEventManager manager)
                {
                    manager = new FreezableChangedEventManager();
                    SetCurrentManager(managerType, manager);
                }

                return manager;
            }
        }

        internal static void AddHandler(Freezable source, EventHandler<EventArgs> handler)
        {
            CurrentManager.ProtectedAddHandler(source, handler);
        }

        internal static void RemoveHandler(Freezable source, EventHandler<EventArgs> handler)
        {
            CurrentManager.ProtectedRemoveHandler(source, handler);
        }

        protected override ListenerList NewListenerList()
        {
            return new ListenerList<EventArgs>();
        }

        protected override void StartListening(object source)
        {
            ((Freezable)source).Changed += DeliverEvent;
        }

        protected override void StopListening(object source)
        {
            // Freeze clears Changed handlers. Its public event accessor rejects removal afterward.
            if (source is Freezable { IsFrozen: false } freezable)
            {
                freezable.Changed -= DeliverEvent;
            }
        }
    }
}
