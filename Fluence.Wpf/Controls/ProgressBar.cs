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
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Fluent-styled progress bar with standard, indeterminate, and step modes.
    /// </summary>
    [TemplatePart(Name = PART_Track, Type = typeof(System.Windows.Controls.Border))]
    [TemplatePart(Name = PART_Fill, Type = typeof(System.Windows.Controls.Border))]
    [TemplatePart(Name = PART_IndeterminateBar, Type = typeof(System.Windows.Controls.Border))]
    [TemplatePart(Name = PART_IndeterminateBar2, Type = typeof(System.Windows.Controls.Border))]
    [TemplatePart(Name = PART_IndeterminateTranslate, Type = typeof(TranslateTransform))]
    [TemplatePart(Name = PART_IndeterminateTranslate2, Type = typeof(TranslateTransform))]
    public class ProgressBar : System.Windows.Controls.ProgressBar
    {
        private const string PART_Track = "PART_Track";
        private const string PART_Fill = "PART_Fill";
        private const string PART_IndeterminateBar = "PART_IndeterminateBar";
        private const string PART_IndeterminateBar2 = "PART_IndeterminateBar2";
        private const string PART_IndeterminateTranslate = "PART_IndeterminateTranslate";
        private const string PART_IndeterminateTranslate2 = "PART_IndeterminateTranslate2";

        private System.Windows.Controls.Border _track;
        private System.Windows.Controls.Border _fill;
        private System.Windows.Controls.Border _indeterminateBar;
        private System.Windows.Controls.Border _indeterminateBar2;
        private TranslateTransform _indeterminateTranslate;
        private TranslateTransform _indeterminateTranslate2;

        static ProgressBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ProgressBar),
                new FrameworkPropertyMetadata(typeof(ProgressBar)));
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(ProgressBar),
                new FrameworkPropertyMetadata(new CornerRadius(2), OnLayoutPropertyChanged));

        /// <summary>
        /// Gets or sets the corner radius of the progress bar track and fill.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ProgressMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ProgressModeProperty =
            DependencyProperty.Register(
                nameof(ProgressMode),
                typeof(ProgressBarMode),
                typeof(ProgressBar),
                new FrameworkPropertyMetadata(ProgressBarMode.Standard, OnProgressModeChanged));

        /// <summary>
        /// Gets or sets the progress mode (Standard, Indeterminate, or StepProgress).
        /// </summary>
        public ProgressBarMode ProgressMode
        {
            get => (ProgressBarMode)GetValue(ProgressModeProperty);
            set => SetValue(ProgressModeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Steps"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StepsProperty =
            DependencyProperty.Register(
                nameof(Steps),
                typeof(int),
                typeof(ProgressBar),
                new FrameworkPropertyMetadata(0, OnLayoutPropertyChanged));

        /// <summary>
        /// Gets or sets the total number of steps in StepProgress mode.
        /// </summary>
        public int Steps
        {
            get => (int)GetValue(StepsProperty);
            set => SetValue(StepsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CurrentStep"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CurrentStepProperty =
            DependencyProperty.Register(
                nameof(CurrentStep),
                typeof(int),
                typeof(ProgressBar),
                new FrameworkPropertyMetadata(0, OnAnimatedLayoutPropertyChanged));

        /// <summary>
        /// Gets or sets the current step in StepProgress mode.
        /// </summary>
        public int CurrentStep
        {
            get => (int)GetValue(CurrentStepProperty);
            set => SetValue(CurrentStepProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowStepMarkers"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowStepMarkersProperty =
            DependencyProperty.Register(
                nameof(ShowStepMarkers),
                typeof(bool),
                typeof(ProgressBar),
                new FrameworkPropertyMetadata(true, OnLayoutPropertyChanged));

        /// <summary>
        /// Gets or sets whether step markers are shown in StepProgress mode.
        /// </summary>
        public bool ShowStepMarkers
        {
            get => (bool)GetValue(ShowStepMarkersProperty);
            set => SetValue(ShowStepMarkersProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TrackHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TrackHeightProperty =
            DependencyProperty.Register(
                nameof(TrackHeight),
                typeof(double),
                typeof(ProgressBar),
                new FrameworkPropertyMetadata(4.0, OnLayoutPropertyChanged));

        /// <summary>
        /// Gets or sets the height of the progress bar track.
        /// </summary>
        public double TrackHeight
        {
            get => (double)GetValue(TrackHeightProperty);
            set => SetValue(TrackHeightProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressBar"/> class and subscribes to size changes for layout updates.
        /// </summary>
        public ProgressBar()
        {
            SizeChanged += OnSizeChanged;
        }

        private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProgressBar bar = (ProgressBar)d;
            bar.UpdateFillWidth(false);
            bar.RefreshIndeterminateLayout();
        }

        private static void OnAnimatedLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProgressBar bar = (ProgressBar)d;
            bar.UpdateFillWidth();
            bar.RefreshIndeterminateLayout();
        }

        private static void OnProgressModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ProgressBar bar = (ProgressBar)d;
            bar.ApplyProgressMode();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateFillWidth(false);
            RefreshIndeterminateLayout();
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            StopIndeterminate();

            _track?.SizeChanged -= OnTrackSizeChanged;

            _track = GetTemplateChild(PART_Track) as System.Windows.Controls.Border;
            _fill = GetTemplateChild(PART_Fill) as System.Windows.Controls.Border;
            _indeterminateBar = GetTemplateChild(PART_IndeterminateBar) as System.Windows.Controls.Border;
            _indeterminateBar2 = GetTemplateChild(PART_IndeterminateBar2) as System.Windows.Controls.Border;
            _indeterminateTranslate = GetTemplateChild(PART_IndeterminateTranslate) as TranslateTransform;
            _indeterminateTranslate2 = GetTemplateChild(PART_IndeterminateTranslate2) as TranslateTransform;

            _track?.SizeChanged += OnTrackSizeChanged;

            ApplyProgressMode();
            UpdateFillWidth(false);
            RefreshIndeterminateLayout();
        }

        private void OnTrackSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateFillWidth(false);
            RefreshIndeterminateLayout();
        }

        /// <inheritdoc />
        protected override void OnValueChanged(double oldValue, double newValue)
        {
            base.OnValueChanged(oldValue, newValue);
            UpdateFillWidth();
        }

        /// <inheritdoc />
        protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
        {
            base.OnMinimumChanged(oldMinimum, newMinimum);
            UpdateFillWidth(false);
        }

        /// <inheritdoc />
        protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
        {
            base.OnMaximumChanged(oldMaximum, newMaximum);
            UpdateFillWidth(false);
        }

        private void ApplyProgressMode()
        {
            if (_fill == null || _indeterminateBar == null)
            {
                return;
            }

            if (ProgressMode == ProgressBarMode.Indeterminate)
            {
                _fill.Visibility = Visibility.Collapsed;
                _indeterminateBar.Visibility = Visibility.Visible;
                _indeterminateBar2?.Visibility = Visibility.Visible;
                RefreshIndeterminateLayout();
            }
            else
            {
                StopIndeterminate();
                _fill.Visibility = Visibility.Visible;
                _indeterminateBar.Visibility = Visibility.Collapsed;
                _indeterminateBar2?.Visibility = Visibility.Collapsed;

                ApplyFillBrushForMode();

                // PART_Track can report 0 width during template application. Queue one
                // layout-priority update so determinate fills render correctly after the
                // first measure/arrange pass.
                Dispatcher.BeginInvoke(
                    new Action(() => UpdateFillWidth(false)),
                    System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        private void ApplyFillBrushForMode()
        {
            if (_fill == null)
            {
                return;
            }

            switch (ProgressMode)
            {
                case ProgressBarMode.Error:
                    _fill.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, "SystemFillColorCriticalBrush");
                    break;
                case ProgressBarMode.Paused:
                    _fill.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, "SystemFillColorCautionBrush");
                    break;
                default:
                    _fill.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, "AccentFillColorDefaultBrush");
                    break;
            }
        }

        private void RefreshIndeterminateLayout()
        {
            if (_track == null || _indeterminateBar == null || ProgressMode != ProgressBarMode.Indeterminate)
            {
                return;
            }

            double trackWidth = _track.ActualWidth;
            if (trackWidth <= 0 || double.IsNaN(trackWidth))
            {
                return;
            }

            _indeterminateBar.Width = trackWidth * 0.4;
            _indeterminateBar2?.Width = trackWidth * 0.55;

            StartIndeterminate(trackWidth);
        }

        private void StartIndeterminate(double trackWidth)
        {
            StopIndeterminate();

            if (_indeterminateTranslate == null || _indeterminateBar == null)
            {
                return;
            }

            // WinUI 3 canonical timing: both bars on a 2.0 s repeat cycle.
            // Bar 1 travels 0 to 1.5 s then holds; bar 2 is delayed 0.75 s.
            // (Authority: WinUI_XAML/Controls/ProgressBar.xaml Indeterminate VSM)
            StartTranslateAnimation(
                _indeterminateTranslate,
                -_indeterminateBar.Width,
                trackWidth,
                TimeSpan.FromSeconds(2.0),
                TimeSpan.Zero);

            if (_indeterminateTranslate2 != null && _indeterminateBar2 != null)
            {
                StartTranslateAnimation(
                    _indeterminateTranslate2,
                    -_indeterminateBar2.Width,
                    trackWidth,
                    TimeSpan.FromSeconds(2.0),
                    TimeSpan.FromMilliseconds(750));
            }
        }

        private static void StartTranslateAnimation(
            TranslateTransform target,
            double from,
            double to,
            TimeSpan duration,
            TimeSpan beginTime)
        {
            target.X = from;

            DoubleAnimation animation = new()
            {
                From = from,
                To = to,
                Duration = new Duration(duration),
                BeginTime = beginTime,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            target.BeginAnimation(TranslateTransform.XProperty, animation, HandoffBehavior.SnapshotAndReplace);
        }

        private void StopIndeterminate()
        {
            if (_indeterminateTranslate != null)
            {
                _indeterminateTranslate.BeginAnimation(TranslateTransform.XProperty, null);
                _indeterminateTranslate.X = 0;
            }

            if (_indeterminateTranslate2 != null)
            {
                _indeterminateTranslate2.BeginAnimation(TranslateTransform.XProperty, null);
                _indeterminateTranslate2.X = 0;
            }
        }

        private void UpdateFillWidth(bool animate = true)
        {
            if (_track == null || _fill == null || ProgressMode == ProgressBarMode.Indeterminate)
            {
                return;
            }

            double trackWidth = _track.ActualWidth;
            if (trackWidth <= 0 || double.IsNaN(trackWidth))
            {
                return;
            }

            double ratio;
            if (ProgressMode == ProgressBarMode.StepProgress && Steps > 0)
            {
                int step = Math.Max(0, Math.Min(CurrentStep, Steps));
                ratio = step / (double)Steps;
            }
            else
            {
                double min = Minimum;
                double max = Maximum;
                if (Math.Abs(max - min) < double.Epsilon)
                {
                    ratio = 0;
                }
                else
                {
                    ratio = (Value - min) / (max - min);
                    ratio = Math.Max(0, Math.Min(1, ratio));
                }
            }

            double targetWidth = trackWidth * ratio;
            if (!animate)
            {
                _fill.BeginAnimation(WidthProperty, null);
                _fill.Width = targetWidth;
                return;
            }

            double fromWidth = _fill.ActualWidth;
            if (double.IsNaN(fromWidth) || fromWidth <= 0)
            {
                fromWidth = _fill.Width;
            }

            _fill.BeginAnimation(WidthProperty, null);
            _fill.Width = targetWidth;

            DoubleAnimation animation = new()
            {
                From = fromWidth,
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(280),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            _fill.BeginAnimation(WidthProperty, animation, HandoffBehavior.SnapshotAndReplace);
        }
    }
}
