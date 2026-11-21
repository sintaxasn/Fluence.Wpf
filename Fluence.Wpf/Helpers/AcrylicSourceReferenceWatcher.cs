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
using System.Windows;
using System.Windows.Data;

namespace Fluence.Wpf.Helpers
{
    // Watches only the reference graph visited by AcrylicSourceMap. Binding observes effective
    // styled/triggered DP values; Freezable.Changed includes nested drawing/brush mutations.
    internal sealed class AcrylicSourceReferenceWatcher : IDisposable
    {
        private readonly Action _changed;
        private readonly HashSet<DependencyObject> _nodes;
        private readonly List<PropertyProbe> _properties = [];
        private readonly List<Freezable> _freezables = [];
        private bool _ready;
        private bool _disposed;

        private AcrylicSourceReferenceWatcher(HashSet<DependencyObject> nodes, Action changed)
        {
            _changed = changed;
            _nodes = nodes;
        }

        internal static AcrylicSourceReferenceWatcher Observe(HashSet<DependencyObject> nodes, Action changed)
        {
            AcrylicSourceReferenceWatcher watcher = new(nodes, changed);
            watcher.Attach();
            return watcher;
        }

        private void Attach()
        {
            foreach (DependencyObject node in _nodes)
            {
                if (node is UIElement)
                {
                    HashSet<DependencyProperty> properties = [UIElement.OpacityMaskProperty];
                    properties.UnionWith(AcrylicSourceMap.EffectiveBrushProperties(node));
                    foreach (DependencyProperty property in properties)
                    {
                        _properties.Add(PropertyProbe.AttachTo(this, node, property));
                    }
                }

                if (node is Freezable freezable && !freezable.IsFrozen)
                {
                    FreezableChangedEventManager.AddHandler(freezable, OnFreezableChanged);
                    _freezables.Add(freezable);
                }
            }

            _ready = true;
        }

        internal bool Matches(HashSet<DependencyObject> nodes)
        {
            return _nodes.SetEquals(nodes);
        }

        private void OnFreezableChanged(object? sender, EventArgs e)
        {
            _ = sender;
            _ = e;
            OnChanged();
        }

        private void OnChanged()
        {
            if (_ready && !_disposed)
            {
                _changed();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            foreach (PropertyProbe property in _properties)
            {
                property.Clear();
            }

            _properties.Clear();
            foreach (Freezable freezable in _freezables)
            {
                FreezableChangedEventManager.RemoveHandler(freezable, OnFreezableChanged);
            }

            _freezables.Clear();
            _nodes.Clear();
        }

        private sealed class PropertyProbe : DependencyObject
        {
            private static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
                "Value", typeof(object), typeof(PropertyProbe), new PropertyMetadata(defaultValue: null, OnValueChanged));
            private readonly AcrylicSourceReferenceWatcher _watcher;

            private PropertyProbe(AcrylicSourceReferenceWatcher watcher)
            {
                _watcher = watcher;
            }

            internal static PropertyProbe AttachTo(AcrylicSourceReferenceWatcher watcher, DependencyObject node,
                DependencyProperty property)
            {
                PropertyProbe probe = new(watcher);
                probe.Bind(node, property);
                return probe;
            }

            private void Bind(DependencyObject node, DependencyProperty property)
            {
                _ = BindingOperations.SetBinding(this, ValueProperty, new Binding
                {
                    Source = node,
                    Path = new PropertyPath(property),
                    Mode = BindingMode.OneWay,
                });
            }

            private static void OnValueChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
            {
                _ = args;
                ((PropertyProbe)dependencyObject)._watcher.OnChanged();
            }

            internal void Clear()
            {
                BindingOperations.ClearBinding(this, ValueProperty);
            }
        }
    }
}
