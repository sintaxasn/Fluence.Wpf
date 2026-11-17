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
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Fluence.Wpf.Helpers
{
    internal static class AcrylicSourceMap
    {
        private const double TransformTolerance = 0.000001;
        private static readonly Lock RegistrationLock = new();
        private static readonly List<WeakReference<AcrylicSourceRegistration>> Registrations = [];

        internal static IDisposable Register(FrameworkElement owner, FrameworkElement? source, FrameworkElement output)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(output);
            AcrylicSourceRegistration registration = new(owner, source, output);
            lock (RegistrationLock)
            {
                PruneRegistrations();
                Registrations.Add(new WeakReference<AcrylicSourceRegistration>(registration));
            }

            return registration;
        }

        internal static AcrylicSourceMapResult Evaluate(FrameworkElement owner, FrameworkElement? source,
            Size targetSize, double blurRadiusDip, HashSet<DependencyObject>? observedReferences = null)
        {
            ArgumentNullException.ThrowIfNull(owner);
            if (source is null || !owner.IsLoaded || !source.IsLoaded)
            {
                return Failure(AcrylicFallbackReason.UnavailableSource);
            }

            bool unsafeReferences = ScanReferences(source, owner, out bool hasReferencedHwndHost, observedReferences);
            if (Contains(source, owner) || Contains(owner, source) || WouldCloseCycle(owner, source)
                || unsafeReferences)
            {
                return Failure(AcrylicFallbackReason.CyclicSource);
            }

            HwndSource? ownerHwnd = PresentationSource.FromVisual(owner) is HwndSource ownerPresentation
                ? ownerPresentation : null;
            HwndSource? sourceHwnd = PresentationSource.FromVisual(source) is HwndSource sourcePresentation
                ? sourcePresentation : null;
            if (ownerHwnd is null || sourceHwnd is null)
            {
                return Failure(AcrylicFallbackReason.UnavailableSource);
            }

            if (!ReferenceEquals(ownerHwnd, sourceHwnd) || ownerHwnd.Handle != sourceHwnd.Handle)
            {
                return Failure(AcrylicFallbackReason.DifferentHwnd);
            }

            if (hasReferencedHwndHost)
            {
                return Failure(AcrylicFallbackReason.HwndHostBoundary);
            }

            if (!TryGetSourceBounds(source, out Rect visibleSource, out AcrylicFallbackReason clipFailure))
            {
                return Failure(clipFailure);
            }

            if (!TryGetAxisAlignedMap(owner, source, out Matrix ownerToSource))
            {
                return Failure(AcrylicFallbackReason.UnsupportedTransform);
            }

            Matrix toDevice = ownerHwnd.CompositionTarget.TransformToDevice;
            return CalculateGeometry(ownerToSource, visibleSource, targetSize, blurRadiusDip, toDevice);
        }

        internal static AcrylicSourceMapResult CalculateGeometry(Matrix ownerToSource, Rect visibleSource,
            Size targetSize, double blurRadiusDip, Matrix toDevice)
        {
            if (!IsFinitePositive(targetSize.Width) || !IsFinitePositive(targetSize.Height)
                || !IsFiniteNonnegative(blurRadiusDip) || !IsFiniteMatrix(toDevice)
                || !IsFiniteRect(visibleSource) || !IsFinitePositive(toDevice.M11)
                || !IsFinitePositive(toDevice.M22) || Math.Abs(toDevice.M12) > TransformTolerance
                || Math.Abs(toDevice.M21) > TransformTolerance || !IsFiniteMatrix(ownerToSource)
                || ownerToSource.M11 <= TransformTolerance || ownerToSource.M22 <= TransformTolerance
                || Math.Abs(ownerToSource.M12) > TransformTolerance
                || Math.Abs(ownerToSource.M21) > TransformTolerance || !ownerToSource.HasInverse)
            {
                return Failure(AcrylicFallbackReason.UnsupportedTransform);
            }

            Matrix sourceToOwner = ownerToSource;
            sourceToOwner.Invert();
            Rect targetRect = new(targetSize);
            Rect targetCrop = Rect.Intersect(targetRect, TransformRect(visibleSource, sourceToOwner));
            if (targetCrop.IsEmpty || targetCrop.Width <= 0 || targetCrop.Height <= 0)
            {
                return Failure(AcrylicFallbackReason.UnavailableSource);
            }

            int physicalPadX = CheckedPad(blurRadiusDip, toDevice.M11);
            int physicalPadY = CheckedPad(blurRadiusDip, toDevice.M22);
            if (physicalPadX < 0 || physicalPadY < 0)
            {
                return Failure(AcrylicFallbackReason.UnsupportedTransform);
            }

            Rect paddedTargetRect = targetCrop;
            paddedTargetRect.Inflate(physicalPadX / toDevice.M11, physicalPadY / toDevice.M22);
            Rect sourceRect = Rect.Intersect(TransformRect(paddedTargetRect, ownerToSource), visibleSource);
            return sourceRect.IsEmpty
                ? Failure(AcrylicFallbackReason.UnavailableSource)
                : new AcrylicSourceMapResult(AcrylicFallbackReason.None, sourceToOwner, ownerToSource,
                    targetRect, paddedTargetRect, sourceRect, targetCrop,
                    toDevice.M11, toDevice.M22, physicalPadX, physicalPadY);
        }

        private static int CheckedPad(double blurRadiusDip, double scale)
        {
            double value = Math.Ceiling(blurRadiusDip * scale);
            return value > int.MaxValue || double.IsInfinity(value) ? -1 : (int)value;
        }

        private static bool TryGetAxisAlignedMap(FrameworkElement owner, FrameworkElement source, out Matrix matrix)
        {
            matrix = Matrix.Identity;
            try
            {
                GeneralTransform transform = owner.TransformToVisual(source);
                return TryGetAffineAxisAlignedMatrix(transform, owner.ActualWidth, owner.ActualHeight, out matrix);
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        internal static bool TryGetAffineAxisAlignedMatrix(GeneralTransform transform, double width, double height,
            out Matrix matrix)
        {
            matrix = Matrix.Identity;
            return IsFinitePositive(width) && IsFinitePositive(height)
                && TryGetAffineMatrix(transform, out matrix) && IsFiniteMatrix(matrix)
                && matrix.M11 > TransformTolerance && matrix.M22 > TransformTolerance
                && Math.Abs(matrix.M12) <= TransformTolerance && Math.Abs(matrix.M21) <= TransformTolerance
                && matrix.HasInverse;
        }

        private static bool TryGetAffineMatrix(GeneralTransform transform, out Matrix matrix)
        {
            matrix = Matrix.Identity;
            if (transform is Transform affine)
            {
                matrix = affine.Value;
                return IsFiniteMatrix(matrix);
            }

            if (transform is not GeneralTransformGroup group)
            {
                return false;
            }

            foreach (GeneralTransform child in group.Children)
            {
                if (!TryGetAffineMatrix(child, out Matrix childMatrix))
                {
                    return false;
                }

                matrix.Append(childMatrix);
                if (!IsFiniteMatrix(matrix))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryGetSourceBounds(FrameworkElement source, out Rect visible, out AcrylicFallbackReason reason)
        {
            visible = new Rect(0, 0, source.ActualWidth, source.ActualHeight);
            reason = AcrylicFallbackReason.None;
            if (!IsFinitePositive(visible.Width) || !IsFinitePositive(visible.Height))
            {
                reason = AcrylicFallbackReason.UnavailableSource;
                return false;
            }

            for (DependencyObject? node = source; node is Visual visual; node = VisualTreeHelper.GetParent(node))
            {
                UIElement? element = node as UIElement;
                Geometry? declaredClip = element?.Clip;
                Geometry? effectiveClip = VisualTreeHelper.GetClip(visual);
                if (!IsPlainRectangle(declaredClip) || !IsPlainRectangle(effectiveClip))
                {
                    reason = AcrylicFallbackReason.UnsupportedClip;
                    return false;
                }

                if (declaredClip is null && effectiveClip is null && element?.ClipToBounds is not true)
                {
                    continue;
                }

                try
                {
                    GeneralTransform transform = visual.TransformToVisual(source);
                    if (declaredClip is RectangleGeometry declaredRectangle
                        && !ReferenceEquals(declaredClip, effectiveClip))
                    {
                        if (!TryTransformAxisAlignedRect(transform, declaredRectangle.Rect, out Rect clip))
                        {
                            reason = AcrylicFallbackReason.UnsupportedClip;
                            return false;
                        }

                        visible.Intersect(clip);
                    }

                    if (effectiveClip is RectangleGeometry rectangleClip)
                    {
                        if (!TryTransformAxisAlignedRect(transform, rectangleClip.Rect, out Rect clip))
                        {
                            reason = AcrylicFallbackReason.UnsupportedClip;
                            return false;
                        }

                        visible.Intersect(clip);
                    }

                    if (element?.ClipToBounds is true)
                    {
                        if (!TryTransformAxisAlignedRect(transform, new Rect(element.RenderSize), out Rect bounds))
                        {
                            reason = AcrylicFallbackReason.UnsupportedClip;
                            return false;
                        }

                        visible.Intersect(bounds);
                    }
                }
                catch (InvalidOperationException)
                {
                    reason = AcrylicFallbackReason.UnsupportedClip;
                    return false;
                }
            }

            if (visible.IsEmpty)
            {
                reason = AcrylicFallbackReason.UnavailableSource;
                return false;
            }

            return true;
        }

        private static bool IsPlainRectangle(Geometry? geometry)
        {
            return geometry is null || (geometry is RectangleGeometry rectangle
                && rectangle.Transform.Value.IsIdentity && rectangle.RadiusX == 0 && rectangle.RadiusY == 0);
        }

        private static bool ScanReferences(FrameworkElement source, FrameworkElement owner, out bool hasHwndHost,
            HashSet<DependencyObject>? observedReferences)
        {
            HashSet<FrameworkElement> unsafeTargets = [owner];
            foreach (AcrylicSourceRegistration registration in ActiveRegistrations())
            {
                if (registration.Owner.TryGetTarget(out FrameworkElement? registeredOwner)
                    && ReferenceEquals(registeredOwner.Dispatcher, source.Dispatcher))
                {
                    _ = unsafeTargets.Add(registeredOwner);
                }

                if (registration.Output.TryGetTarget(out FrameworkElement? output)
                    && ReferenceEquals(output.Dispatcher, source.Dispatcher))
                {
                    _ = unsafeTargets.Add(output);
                }
            }

            hasHwndHost = false;
            HashSet<DependencyObject> visited = [];
            bool unsafeReference = WalkReferences(source, unsafeTargets, [], visited, ref hasHwndHost);
            observedReferences?.UnionWith(visited);
            return unsafeReference;
        }

        private static bool WalkReferences(DependencyObject? node, HashSet<FrameworkElement> unsafeTargets,
            HashSet<DependencyObject> active, HashSet<DependencyObject> visited, ref bool hasHwndHost)
        {
            if (node is null || visited.Contains(node))
            {
                return false;
            }

            if (!active.Add(node) || (node is FrameworkElement element && unsafeTargets.Contains(element)))
            {
                return true;
            }

            try
            {
                hasHwndHost |= node is HwndHost;
                if (node is VisualBrush visualBrush
                    && WalkReferences(visualBrush.Visual, unsafeTargets, active, visited, ref hasHwndHost))
                {
                    return true;
                }

                if (node is DrawingBrush drawingBrush
                    && WalkReferences(drawingBrush.Drawing, unsafeTargets, active, visited, ref hasHwndHost))
                {
                    return true;
                }

                if (node is GeometryDrawing geometry)
                {
                    if (WalkReferences(geometry.Brush, unsafeTargets, active, visited, ref hasHwndHost)
                        || WalkReferences(geometry.Pen?.Brush, unsafeTargets, active, visited, ref hasHwndHost))
                    {
                        return true;
                    }
                }

                if (node is GlyphRunDrawing glyph
                    && WalkReferences(glyph.ForegroundBrush, unsafeTargets, active, visited, ref hasHwndHost))
                {
                    return true;
                }

                if (node is DrawingImage drawingImage
                    && WalkReferences(drawingImage.Drawing, unsafeTargets, active, visited, ref hasHwndHost))
                {
                    return true;
                }

                if (node is ImageBrush imageBrush
                    && WalkReferences(imageBrush.ImageSource, unsafeTargets, active, visited, ref hasHwndHost))
                {
                    return true;
                }

                if (node is ImageDrawing imageDrawing
                    && WalkReferences(imageDrawing.ImageSource, unsafeTargets, active, visited, ref hasHwndHost))
                {
                    return true;
                }

                if (node is DrawingGroup group)
                {
                    if (WalkReferences(group.OpacityMask, unsafeTargets, active, visited, ref hasHwndHost))
                    {
                        return true;
                    }

                    foreach (Drawing drawing in group.Children)
                    {
                        if (WalkReferences(drawing, unsafeTargets, active, visited, ref hasHwndHost))
                        {
                            return true;
                        }
                    }
                }

                if (node is Visual visual)
                {
                    if (visual is UIElement uiElement
                        && WalkReferences(uiElement.OpacityMask, unsafeTargets, active, visited, ref hasHwndHost))
                    {
                        return true;
                    }

                    if (visual is DrawingVisual drawingVisual
                        && WalkReferences(VisualTreeHelper.GetDrawing(drawingVisual), unsafeTargets, active, visited,
                            ref hasHwndHost))
                    {
                        return true;
                    }

                    if (node is Border border
                        && WalkReferences(border.Background, unsafeTargets, active, visited, ref hasHwndHost))
                    {
                        return true;
                    }

                    if (node is Panel panel
                        && WalkReferences(panel.Background, unsafeTargets, active, visited, ref hasHwndHost))
                    {
                        return true;
                    }

                    if (node is Control control
                        && WalkReferences(control.Background, unsafeTargets, active, visited, ref hasHwndHost))
                    {
                        return true;
                    }

                    if (node is Shape shape)
                    {
                        if (WalkReferences(shape.Fill, unsafeTargets, active, visited, ref hasHwndHost)
                            || WalkReferences(shape.Stroke, unsafeTargets, active, visited, ref hasHwndHost))
                        {
                            return true;
                        }
                    }

                    if (node is TextBlock text)
                    {
                        if (WalkReferences(text.Background, unsafeTargets, active, visited, ref hasHwndHost)
                            || WalkReferences(text.Foreground, unsafeTargets, active, visited, ref hasHwndHost))
                        {
                            return true;
                        }
                    }

                    if (node is Image image
                        && WalkReferences(image.Source, unsafeTargets, active, visited, ref hasHwndHost))
                    {
                        return true;
                    }

                    if (visual is UIElement
                        && WalkEffectiveBrushProperties(node, unsafeTargets, active, visited, ref hasHwndHost))
                    {
                        return true;
                    }

                    LocalValueEnumerator values = node.GetLocalValueEnumerator();
                    while (values.MoveNext())
                    {
                        if (values.Current.Value is Brush or DrawingImage
                            && WalkReferences(values.Current.Value as DependencyObject, unsafeTargets, active,
                                visited, ref hasHwndHost))
                        {
                            return true;
                        }
                    }

                    for (int index = 0; index < VisualTreeHelper.GetChildrenCount(visual); index++)
                    {
                        if (WalkReferences(VisualTreeHelper.GetChild(visual, index), unsafeTargets, active, visited,
                            ref hasHwndHost))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            finally
            {
                _ = visited.Add(node);
                _ = active.Remove(node);
            }
        }

        private static bool WalkEffectiveBrushProperties(DependencyObject node,
            HashSet<FrameworkElement> unsafeTargets, HashSet<DependencyObject> active,
            HashSet<DependencyObject> visited, ref bool hasHwndHost)
        {
            foreach (DependencyProperty dp in EffectiveBrushProperties(node))
            {
                if (node.GetValue(dp) is not Brush brush)
                {
                    continue;
                }

                if (WalkReferences(brush, unsafeTargets, active, visited, ref hasHwndHost))
                {
                    return true;
                }
            }

            return false;
        }

        internal static IEnumerable<DependencyProperty> EffectiveBrushProperties(DependencyObject node)
        {
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(node))
            {
                if (typeof(Brush).IsAssignableFrom(property.PropertyType)
                    && DependencyPropertyDescriptor.FromProperty(property)?.DependencyProperty is DependencyProperty dp)
                {
                    yield return dp;
                }
            }
        }

        private static bool WouldCloseCycle(FrameworkElement owner, FrameworkElement source)
        {
            HashSet<FrameworkElement> visited = [];
            return ReachesOwner(source, owner, visited);
        }

        private static bool ReachesOwner(FrameworkElement source, FrameworkElement target, HashSet<FrameworkElement> visited)
        {
            foreach (AcrylicSourceRegistration registration in ActiveRegistrations())
            {
                if (!registration.Owner.TryGetTarget(out FrameworkElement? nextOwner)
                    || !ReferenceEquals(nextOwner.Dispatcher, source.Dispatcher)
                    || !Contains(source, nextOwner) || !visited.Add(nextOwner))
                {
                    continue;
                }

                if (ReferenceEquals(nextOwner, target)
                    || (registration.Source is not null
                        && registration.Source.TryGetTarget(out FrameworkElement? nextSource)
                        && ReachesOwner(nextSource, target, visited)))
                {
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<AcrylicSourceRegistration> ActiveRegistrations()
        {
            List<AcrylicSourceRegistration> active = [];
            lock (RegistrationLock)
            {
                PruneRegistrations();
                foreach (WeakReference<AcrylicSourceRegistration> weak in Registrations)
                {
                    if (weak.TryGetTarget(out AcrylicSourceRegistration? registration)
                        && !registration.IsDisposed)
                    {
                        active.Add(registration);
                    }
                }
            }

            return active;
        }

        private static void PruneRegistrations()
        {
            _ = Registrations.RemoveAll(static weak => !weak.TryGetTarget(out AcrylicSourceRegistration? registration)
                || registration.IsDisposed);
        }

        private static bool Contains(DependencyObject root, DependencyObject node)
        {
            for (DependencyObject? current = node; current is not null; current = VisualTreeHelper.GetParent(current))
            {
                if (ReferenceEquals(current, root))
                {
                    return true;
                }
            }

            return false;
        }

        private static Rect TransformRect(Rect rectangle, Matrix matrix)
        {
            return new Rect(matrix.Transform(rectangle.TopLeft), matrix.Transform(rectangle.BottomRight));
        }

        private static bool TryTransformAxisAlignedRect(GeneralTransform transform, Rect rectangle, out Rect result)
        {
            result = Rect.Empty;
            if (!transform.TryTransform(rectangle.TopLeft, out Point topLeft)
                || !transform.TryTransform(rectangle.TopRight, out Point topRight)
                || !transform.TryTransform(rectangle.BottomLeft, out Point bottomLeft)
                || !transform.TryTransform(rectangle.BottomRight, out Point bottomRight)
                || !IsFinite(topLeft.X) || !IsFinite(topLeft.Y)
                || !IsFinite(bottomRight.X) || !IsFinite(bottomRight.Y)
                || Math.Abs(topLeft.Y - topRight.Y) > TransformTolerance
                || Math.Abs(bottomLeft.Y - bottomRight.Y) > TransformTolerance
                || Math.Abs(topLeft.X - bottomLeft.X) > TransformTolerance
                || Math.Abs(topRight.X - bottomRight.X) > TransformTolerance)
            {
                return false;
            }

            result = new Rect(topLeft, bottomRight);
            return IsFiniteRect(result);
        }

        private static bool IsFiniteRect(Rect rectangle)
        {
            return IsFinite(rectangle.Left) && IsFinite(rectangle.Top)
                && IsFinite(rectangle.Right) && IsFinite(rectangle.Bottom);
        }

        private static bool IsFiniteMatrix(Matrix matrix)
        {
            return IsFinite(matrix.M11) && IsFinite(matrix.M12) && IsFinite(matrix.M21)
                && IsFinite(matrix.M22) && IsFinite(matrix.OffsetX) && IsFinite(matrix.OffsetY);
        }

        private static bool IsFinitePositive(double value)
        {
            return IsFinite(value) && value > 0;
        }

        private static bool IsFiniteNonnegative(double value)
        {
            return IsFinite(value) && value >= 0;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static AcrylicSourceMapResult Failure(AcrylicFallbackReason reason)
        {
            return new AcrylicSourceMapResult(reason, Matrix.Identity, Matrix.Identity,
                Rect.Empty, Rect.Empty, Rect.Empty, Rect.Empty, 1, 1, 0, 0);
        }
    }
}
