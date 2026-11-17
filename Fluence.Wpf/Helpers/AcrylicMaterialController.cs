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
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace Fluence.Wpf.Helpers
{
    internal sealed class AcrylicMaterialController : IAcrylicBudgetParticipant
    {
        private static readonly DependencyProperty DirectControllerProperty = DependencyProperty.RegisterAttached(
            "DirectController", typeof(AcrylicMaterialController), typeof(AcrylicMaterialController));
        private readonly FrameworkElement _owner;
        private readonly Func<FrameworkElement?> _resolveSource;
        private readonly Action? _stateChanged;
        private readonly AcrylicMaterialRenderer _renderer;
        private readonly AcrylicMaterialRecipe _defaults = new();
        private AcrylicMaterialRecipe? _recipe;
        private bool _recipeAttached;
        private FrameworkElement? _lastCandidate;
        private IDisposable? _registration;
        private AcrylicEnvironmentMonitor.Subscription? _monitor;
        private HwndSource? _hwnd;
        private Window? _window;
        private bool _windowClosed;
        private BrushPropertyObserver? _propertyObserver;
        private DependencyProperty? _observedProperty;
        private bool _propertyAssigned;
        private bool _queued;
        private bool _refreshing;
        private bool _hasRendered;
        private AcrylicSourceMapResult? _lastMap;
        private AcrylicRecipeSnapshot _lastRecipe;
        private AcrylicPolicyResult? _lastPolicy;
        private readonly List<GeometryWatcher> _geometryWatchers = [];
        private AcrylicSourceReferenceWatcher? _referenceWatcher;

        internal AcrylicMaterialController(FrameworkElement owner, Func<FrameworkElement?> resolveSource,
            AcrylicMaterialRecipe? recipe, Action? stateChanged = null)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _owner.Dispatcher.VerifyAccess();
            _resolveSource = resolveSource ?? throw new ArgumentNullException(nameof(resolveSource));
            _recipe = recipe;
            _stateChanged = stateChanged;
            _renderer = new AcrylicMaterialRenderer(owner);
            owner.Loaded += OnLoaded;
            owner.Unloaded += OnUnloaded;
            owner.LayoutUpdated += OnLayoutUpdated;
            if (owner.IsLoaded)
            {
                AttachRecipe();
                Refresh();
            }
        }

        internal VisualBrush Output => _renderer.Output;
        internal FrameworkElement OutputVisual => _renderer.OutputVisual;
        internal AcrylicFallbackReason Reason => _renderer.Reason;
        internal bool IsUsingFallback => _renderer.IsUsingFallback;
        internal bool IsDisposed { get; private set; }
        internal FrameworkElement? CurrentSource { get; private set; }

        internal void AttachDirectProperty(DependencyProperty property)
        {
            _owner.Dispatcher.VerifyAccess();
            if (_owner.GetValue(DirectControllerProperty) is AcrylicMaterialController previous)
            {
                previous.Release();
            }

            _owner.SetValue(DirectControllerProperty, this);
            _observedProperty = property;
            _propertyObserver = new BrushPropertyObserver(this, _owner, property);
        }

        internal void SetRecipe(AcrylicMaterialRecipe? recipe)
        {
            _owner.Dispatcher.VerifyAccess();
            if (IsDisposed || ReferenceEquals(_recipe, recipe))
            {
                return;
            }

            DetachRecipe();
            _recipe = recipe;
            AttachRecipe();
            Refresh();
        }

        internal void InvalidateSource()
        {
            _owner.Dispatcher.VerifyAccess();
            if (!IsDisposed)
            {
                Refresh();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _owner.Dispatcher.VerifyAccess();
            _ = sender;
            _ = e;
            if (IsDisposed)
            {
                return;
            }

            _windowClosed = false;
            AttachRecipe();
            Refresh();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _owner.Dispatcher.VerifyAccess();
            _ = sender;
            _ = e;
            Suspend();
        }

        private void OnLayoutUpdated(object? sender, EventArgs e)
        {
            _owner.Dispatcher.VerifyAccess();
            _ = sender;
            _ = e;
            QueueRefresh();
        }

        private void OnSourceUnloaded(object sender, RoutedEventArgs e)
        {
            _owner.Dispatcher.VerifyAccess();
            _ = sender;
            _ = e;
            DetachSource();
            Refresh();
        }

        private void OnRecipeChanged(object? sender, EventArgs e)
        {
            _owner.Dispatcher.VerifyAccess();
            _ = sender;
            _ = e;
            Refresh();
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            _owner.Dispatcher.VerifyAccess();
            _ = sender;
            _ = e;
            _windowClosed = true;
            Suspend();
        }

        private void QueueRefresh()
        {
            _owner.Dispatcher.VerifyAccess();
            if (_queued || IsDisposed || !_owner.IsLoaded)
            {
                return;
            }

            _queued = true;
            _ = _owner.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                _owner.Dispatcher.VerifyAccess();
                _queued = false;
                if (!IsDisposed && _owner.IsLoaded)
                {
                    Refresh();
                }
            }));
        }

        void IAcrylicBudgetParticipant.RequestBudgetRefresh()
        {
            Dispatcher dispatcher = _owner.Dispatcher;
            if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            {
                return;
            }

            try
            {
                _ = dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                {
                    dispatcher.VerifyAccess();
                    if (!IsDisposed && _owner.IsLoaded)
                    {
                        QueueRefresh();
                    }
                }));
            }
            catch (InvalidOperationException) when (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            {
                // A different UI dispatcher can shut down after the initial check.
            }
        }

        private void Refresh()
        {
            _owner.Dispatcher.VerifyAccess();
            if (_refreshing)
            {
                QueueRefresh();
                return;
            }

            _refreshing = true;
            try
            {
                RefreshCore();
            }
            finally
            {
                _refreshing = false;
            }
        }

        private void RefreshCore()
        {
            _owner.Dispatcher.VerifyAccess();
            if (IsDisposed || _windowClosed)
            {
                return;
            }

            bool designMode = DesignerProperties.GetIsInDesignMode(_owner);
            HwndSource? currentHwnd = !designMode && _owner.IsLoaded
                ? PresentationSource.FromVisual(_owner) as HwndSource : null;
            if (!ReferenceEquals(_hwnd, currentHwnd))
            {
                DropMonitor();
                _hwnd = currentHwnd;
                if (currentHwnd is not null)
                {
                    _monitor = AcrylicEnvironmentMonitor.Subscribe(currentHwnd, QueueRefresh);
                    if (Window.GetWindow(_owner) is Window window)
                    {
                        _window = window;
                        window.Closed += OnWindowClosed;
                    }
                }
            }

            AcrylicMaterialRecipe recipe = _recipe ?? _defaults;
            AcrylicRecipeSnapshot values = AcrylicRecipeSnapshot.Create(recipe);
            FrameworkElement? candidate = !designMode && _owner.IsLoaded ? _resolveSource() : null;
            UpdateGeometryWatchers(candidate);
            if (!ReferenceEquals(candidate, CurrentSource))
            {
                DetachSource();
            }

            Size size = new(_owner.ActualWidth, _owner.ActualHeight);
            HashSet<DependencyObject> references = [];
            AcrylicSourceMapResult map = AcrylicSourceMap.Evaluate(_owner, candidate, size,
                AcrylicMaterialRenderer.BlurRadiusDip, references);
            UpdateReferenceWatcher(references);
            if (map.Reason is not AcrylicFallbackReason.None)
            {
                DetachSource();
            }
            else if (CurrentSource is null && candidate is not null)
            {
                CurrentSource = candidate;
                candidate.Unloaded += OnSourceUnloaded;
                candidate.LayoutUpdated += OnLayoutUpdated;
                _registration = AcrylicSourceMap.Register(_owner, candidate, _renderer.OutputVisual);
            }

            AcrylicPolicySnapshot snapshot = _monitor?.CreateSnapshot() ?? new AcrylicPolicySnapshot
            {
                IsDesignMode = designMode,
                OsHighContrast = SystemParameters.HighContrast,
                PowerSaving = false,
                GraphicsSupported = false,
            };
            snapshot.IsDesignMode = designMode;
            snapshot.SourceFailure = map.Reason;
            AcrylicPolicyResult policy = AcrylicMaterialPolicy.Evaluate(snapshot, recipe, SystemColors.WindowColor);
            if (policy.UseMaterial && map.Reason is AcrylicFallbackReason.None
                && !AcrylicMaterialBudget.TryReserve(this, map))
            {
                policy = new AcrylicPolicyResult(AcrylicFallbackReason.BudgetExceeded, policy.OpaqueFallback);
            }
            if (_hasRendered && _lastPolicy is AcrylicPolicyResult previousPolicy
                && ReferenceEquals(candidate, _lastCandidate)
                && MapsEqual(map, _lastMap) && values.Equals(_lastRecipe)
                && policy.Reason == previousPolicy.Reason
                && policy.OpaqueFallback == previousPolicy.OpaqueFallback)
            {
                return;
            }

            bool wasFallback = _renderer.IsUsingFallback;
            AcrylicFallbackReason wasReason = _renderer.Reason;
            try
            {
                _renderer.Update(CurrentSource, map, values, policy);
            }
            catch
            {
                AcrylicMaterialBudget.Release(this);
                throw;
            }

            if (_renderer.IsUsingFallback)
            {
                AcrylicMaterialBudget.Release(this);
            }
            _lastMap = map;
            _lastCandidate = candidate;
            _lastRecipe = values;
            _lastPolicy = map.Reason is AcrylicFallbackReason.None
                && policy.Reason is AcrylicFallbackReason.None
                && _renderer.IsUsingFallback
                ? new AcrylicPolicyResult(_renderer.Reason, policy.OpaqueFallback) : policy;
            _hasRendered = true;
            if (wasFallback != _renderer.IsUsingFallback || wasReason != _renderer.Reason)
            {
                _stateChanged?.Invoke();
            }
        }

        private static bool MapsEqual(AcrylicSourceMapResult left, AcrylicSourceMapResult? right)
        {
            return right is not null && left.Reason == right.Reason
                && left.SourceToOwner == right.SourceToOwner && left.OwnerToSource == right.OwnerToSource
                && left.TargetRect == right.TargetRect && left.PaddedTargetRect == right.PaddedTargetRect
                && left.SourceRect == right.SourceRect && left.TargetCrop == right.TargetCrop
                && left.DeviceScaleX == right.DeviceScaleX && left.DeviceScaleY == right.DeviceScaleY
                && left.PhysicalPadX == right.PhysicalPadX && left.PhysicalPadY == right.PhysicalPadY;
        }

        private void Suspend()
        {
            _owner.Dispatcher.VerifyAccess();
            if (IsDisposed)
            {
                return;
            }

            DetachSource();
            ClearReferenceWatcher();
            ClearGeometryWatchers();
            DropMonitor();
            DetachRecipe();
            _hasRendered = false;
            _lastCandidate = null;
            AcrylicMaterialRecipe recipe = _recipe ?? _defaults;
            AcrylicSourceMapResult map = AcrylicSourceMap.Evaluate(_owner, source: null, new Size(1, 1), 0);
            AcrylicPolicySnapshot snapshot = new()
            {
                IsDesignMode = DesignerProperties.GetIsInDesignMode(_owner),
                OsHighContrast = SystemParameters.HighContrast,
                SourceFailure = AcrylicFallbackReason.UnavailableSource,
                PowerSaving = false,
            };
            AcrylicPolicyResult policy = AcrylicMaterialPolicy.Evaluate(snapshot, recipe, SystemColors.WindowColor);
            try
            {
                _renderer.Update(source: null, map, AcrylicRecipeSnapshot.Create(recipe), policy);
            }
            finally
            {
                AcrylicMaterialBudget.Release(this);
            }
            _stateChanged?.Invoke();
        }

        private void DetachSource()
        {
            _owner.Dispatcher.VerifyAccess();
            _registration?.Dispose();
            _registration = null;
            if (CurrentSource is FrameworkElement source)
            {
                source.Unloaded -= OnSourceUnloaded;
                source.LayoutUpdated -= OnLayoutUpdated;
                CurrentSource = null;
            }
        }

        private void UpdateGeometryWatchers(FrameworkElement? candidate)
        {
            List<UIElement> chain = [];
            if (candidate is not null)
            {
                AppendVisualChain(candidate, chain);
                AppendVisualChain(_owner, chain);
            }

            if (chain.Count == _geometryWatchers.Count)
            {
                bool same = true;
                for (int index = 0; index < chain.Count; index++)
                {
                    if (!ReferenceEquals(chain[index], _geometryWatchers[index].Element))
                    {
                        same = false;
                        break;
                    }
                }

                if (same)
                {
                    return;
                }
            }

            ClearGeometryWatchers();
            foreach (UIElement element in chain)
            {
                _geometryWatchers.Add(new GeometryWatcher(this, element));
            }
        }

        private static void AppendVisualChain(Visual start, List<UIElement> chain)
        {
            for (DependencyObject? node = start; node is Visual visual; node = VisualTreeHelper.GetParent(visual))
            {
                if (node is UIElement element && !chain.Contains(element))
                {
                    chain.Add(element);
                }
            }
        }

        private void ClearGeometryWatchers()
        {
            foreach (GeometryWatcher watcher in _geometryWatchers)
            {
                watcher.Dispose();
            }

            _geometryWatchers.Clear();
        }

        private void UpdateReferenceWatcher(HashSet<DependencyObject> references)
        {
            if (_referenceWatcher?.Matches(references) is true)
            {
                return;
            }

            ClearReferenceWatcher();
            if (references.Count is not 0)
            {
                _referenceWatcher = AcrylicSourceReferenceWatcher.Observe(references, Refresh);
            }
        }

        private void ClearReferenceWatcher()
        {
            _referenceWatcher?.Dispose();
            _referenceWatcher = null;
        }

        private void AttachRecipe()
        {
            _owner.Dispatcher.VerifyAccess();
            if (!_recipeAttached && _owner.IsLoaded && _recipe?.IsFrozen is false)
            {
                _recipe.Changed += OnRecipeChanged;
                _recipeAttached = true;
            }
        }

        private void DetachRecipe()
        {
            _owner.Dispatcher.VerifyAccess();
            if (_recipeAttached && _recipe is not null)
            {
                _recipe.Changed -= OnRecipeChanged;
                _recipeAttached = false;
            }
        }

        private void DropMonitor()
        {
            _owner.Dispatcher.VerifyAccess();
            if (_window is Window window)
            {
                window.Closed -= OnWindowClosed;
                _window = null;
            }

            using AcrylicEnvironmentMonitor.Subscription? previous = _monitor;
            _monitor = null;
            _hwnd = null;
        }

        private void OnObservedPropertyChanged()
        {
            _owner.Dispatcher.VerifyAccess();
            if (IsDisposed)
            {
                return;
            }

            if (_observedProperty is DependencyProperty property
                && ReferenceEquals(_owner.GetValue(property), Output))
            {
                _propertyAssigned = true;
            }
            else if (_propertyAssigned)
            {
                Release();
            }
        }

        internal void Release()
        {
            _owner.Dispatcher.VerifyAccess();
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            _owner.Loaded -= OnLoaded;
            _owner.Unloaded -= OnUnloaded;
            _owner.LayoutUpdated -= OnLayoutUpdated;
            _propertyObserver?.Clear();
            _propertyObserver = null;
            _observedProperty = null;
            DetachSource();
            ClearReferenceWatcher();
            ClearGeometryWatchers();
            DropMonitor();
            DetachRecipe();
            if (ReferenceEquals(_owner.GetValue(DirectControllerProperty), this))
            {
                _owner.ClearValue(DirectControllerProperty);
            }

            try
            {
                _renderer.Dispose();
            }
            finally
            {
                AcrylicMaterialBudget.Release(this);
            }
        }

        private sealed class BrushPropertyObserver : DependencyObject
        {
            private static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
                "Value", typeof(Brush), typeof(BrushPropertyObserver), new PropertyMetadata(defaultValue: null, OnChanged));
            private readonly AcrylicMaterialController _controller;

            internal BrushPropertyObserver(AcrylicMaterialController controller, FrameworkElement owner,
                DependencyProperty property)
            {
                _controller = controller;
                Observe(owner, property);
            }

            private void Observe(FrameworkElement owner, DependencyProperty property)
            {
                _ = BindingOperations.SetBinding(this, ValueProperty, new Binding
                {
                    Source = owner,
                    Path = new PropertyPath(property),
                    Mode = BindingMode.OneWay,
                });
            }

            private static void OnChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
            {
                _ = args;
                BrushPropertyObserver observer = (BrushPropertyObserver)dependencyObject;
                observer.Dispatcher.VerifyAccess();
                observer._controller.OnObservedPropertyChanged();
            }

            internal void Clear()
            {
                BindingOperations.ClearBinding(this, ValueProperty);
            }
        }

        private sealed class GeometryWatcher : IDisposable
        {
            private readonly AcrylicMaterialController _controller;
            private readonly PropertyObserver _transformObserver;
            private readonly PropertyObserver _clipObserver;
            private Transform? _transform;
            private Geometry? _clip;
            private bool _disposed;

            internal GeometryWatcher(AcrylicMaterialController controller, UIElement element)
            {
                _controller = controller;
                Element = element;
                _transformObserver = PropertyObserver.Observe(element, UIElement.RenderTransformProperty, OnPropertyChanged);
                _clipObserver = PropertyObserver.Observe(element, UIElement.ClipProperty, OnPropertyChanged);
                ObserveFreezables();
            }

            internal UIElement Element { get; }

            private void OnPropertyChanged()
            {
                _controller._owner.Dispatcher.VerifyAccess();
                if (!_disposed)
                {
                    ObserveFreezables();
                    _controller.QueueRefresh();
                }
            }

            private void ObserveFreezables()
            {
                Transform nextTransform = Element.RenderTransform;
                if (!ReferenceEquals(_transform, nextTransform))
                {
                    if (_transform is not null)
                    {
                        WeakEventManager<Freezable, EventArgs>.RemoveHandler(_transform, nameof(Freezable.Changed), OnFreezableChanged);
                    }

                    _transform = nextTransform;
                    if (!nextTransform.IsFrozen)
                    {
                        WeakEventManager<Freezable, EventArgs>.AddHandler(nextTransform, nameof(Freezable.Changed), OnFreezableChanged);
                    }
                }

                Geometry? nextClip = Element.Clip;
                if (!ReferenceEquals(_clip, nextClip))
                {
                    if (_clip is not null)
                    {
                        WeakEventManager<Freezable, EventArgs>.RemoveHandler(_clip, nameof(Freezable.Changed), OnFreezableChanged);
                    }

                    _clip = nextClip;
                    if (nextClip?.IsFrozen is false)
                    {
                        WeakEventManager<Freezable, EventArgs>.AddHandler(nextClip, nameof(Freezable.Changed), OnFreezableChanged);
                    }
                }
            }

            private void OnFreezableChanged(object? sender, EventArgs e)
            {
                _controller._owner.Dispatcher.VerifyAccess();
                _ = sender;
                _ = e;
                if (!_disposed)
                {
                    _controller.QueueRefresh();
                }
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _transformObserver.Clear();
                _clipObserver.Clear();
                if (_transform is not null)
                {
                    WeakEventManager<Freezable, EventArgs>.RemoveHandler(_transform, nameof(Freezable.Changed), OnFreezableChanged);
                }

                if (_clip is not null)
                {
                    WeakEventManager<Freezable, EventArgs>.RemoveHandler(_clip, nameof(Freezable.Changed), OnFreezableChanged);
                }
            }
        }

        private sealed class PropertyObserver : DependencyObject
        {
            private static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
                "Value", typeof(object), typeof(PropertyObserver), new PropertyMetadata(defaultValue: null, OnChanged));
            private readonly Action _changed;

            private PropertyObserver(Action changed)
            {
                _changed = changed;
            }

            internal static PropertyObserver Observe(UIElement source, DependencyProperty property, Action changed)
            {
                PropertyObserver observer = new(changed);
                _ = BindingOperations.SetBinding(observer, ValueProperty, new Binding
                {
                    Source = source,
                    Path = new PropertyPath(property),
                    Mode = BindingMode.OneWay,
                });
                return observer;
            }

            private static void OnChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
            {
                _ = args;
                ((PropertyObserver)dependencyObject)._changed();
            }

            internal void Clear()
            {
                BindingOperations.ClearBinding(this, ValueProperty);
            }
        }
    }
}
