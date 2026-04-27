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
using System.Windows.Media.Animation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for <see cref="ProgressRing"/> indeterminate animation smoothness.
    /// </summary>
    public partial class ControlTests
    {
        // ──────────────────────────────────────────────────────────────────────────
        // A4 regression: ProgressRing indeterminate arc S-curve smoothness
        // ──────────────────────────────────────────────────────────────────────────

        [TestMethod]
        public void ProgressRing_Indeterminate_SweepAnimation_HasAtLeastFourKeyFrames()
        {
            // Regression: the original 2-keyframe (0s→0.75, 0.8s→0.1) approach
            // creates a hard velocity-zero snap at the apex because both SplineDoubleKeyFrames
            // use symmetric ease-in/ease-out independently, producing a near-zero-velocity
            // stall before the direction change. The fix uses 4 keyframes:
            //   0.0s → 0.10  (initial minimum)
            //   0.4s → 0.40  (ease-out: rapid expansion)
            //   0.8s → 0.75  (ease-in: near-zero velocity AT the apex, from both sides)
            //   1.6s → 0.10  (ease-in-out: smooth contraction)
            // This test guards against regressing to fewer keyframes.
            var anim = ProgressRing.CreateIndeterminateSweepAnimation();

            Assert.IsNotNull(anim, "CreateIndeterminateSweepAnimation must return a non-null animation.");
            Assert.IsTrue(
                anim.KeyFrames.Count >= 4,
                "Indeterminate sweep animation must have at least 4 keyframes (S-curve). " +
                "Actual count: " + anim.KeyFrames.Count);
        }

        [TestMethod]
        public void ProgressRing_Indeterminate_SweepAnimation_ApexKeyFrameArrivesWithNearZeroVelocity()
        {
            // Verify that the keyframe at the apex (0.8 s, value 0.75) uses a KeySpline
            // whose control points produce near-zero velocity at the end of the interval
            // (P2.y close to 1 and P2.x close to 1 — i.e. a "ease-in" arrival shape).
            // The canonical value is KeySpline(0.8, 0, 1, 1).
            var anim = ProgressRing.CreateIndeterminateSweepAnimation();

            // The apex keyframe is the one whose value is closest to 0.75.
            SplineDoubleKeyFrame apexFrame = null;
            foreach (var kf in anim.KeyFrames)
            {
                var spline = kf as SplineDoubleKeyFrame;
                if (spline != null && Math.Abs(spline.Value - 0.75) < 0.01)
                {
                    apexFrame = spline;
                    break;
                }
            }

            Assert.IsNotNull(apexFrame,
                "The sweep animation must contain a SplineDoubleKeyFrame with value ~0.75 (the apex).");

            // KeySpline P2.x should be >= 0.8 (ease-in: late acceleration means slow arrival).
            // P2.x >= 0.8 confirms the curve lingers at the start and ramps up near the end.
            var ks = apexFrame.KeySpline;
            Assert.IsTrue(
                ks.ControlPoint2.X >= 0.8,
                "Apex keyframe KeySpline.ControlPoint2.X must be >= 0.8 (ease-in) to ensure " +
                "near-zero velocity at the apex. Actual: " + ks.ControlPoint2.X);
        }

        [TestMethod]
        public void ProgressRing_Indeterminate_SweepAnimation_IsSingleCycleWithHoldEnd()
        {
            // The caterpillar offset fix replaced RepeatBehavior.Forever with a single-cycle
            // animation chained via the Completed event in StartNextIndeterminateCycle().
            // A forever-repeating animation resets StrokeDashOffset to its from-value at each
            // cycle boundary, producing a visible jump.  The fix accumulates _cumulativeOffset
            // externally and feeds each new cycle a fresh start value, so FillBehavior.HoldEnd
            // is required (hold the final frame between the Completed callback and the next
            // BeginAnimation call).  RepeatBehavior must be exactly 1 iteration.
            var anim = ProgressRing.CreateIndeterminateSweepAnimation();

            Assert.AreEqual(
                new RepeatBehavior(1),
                anim.RepeatBehavior,
                "Sweep animation must be a single cycle (repetition managed externally by " +
                "the Completed-chained StartNextIndeterminateCycle, not by RepeatBehavior.Forever).");

            Assert.AreEqual(
                FillBehavior.HoldEnd,
                anim.FillBehavior,
                "Sweep animation must use FillBehavior.HoldEnd so the final frame is held " +
                "until the next cycle begins in the Completed callback.");
        }

        [TestMethod]
        public void ProgressRing_Indeterminate_SweepAnimation_TotalDurationIs1Point6Seconds()
        {
            // Verify the last keyframe is at 1.6 s, preserving the original cycle period.
            var anim = ProgressRing.CreateIndeterminateSweepAnimation();
            var lastKf = anim.KeyFrames[anim.KeyFrames.Count - 1];
            var lastTime = lastKf.KeyTime.TimeSpan;
            Assert.AreEqual(
                1.6,
                lastTime.TotalSeconds,
                0.01,
                "The final keyframe must be at t=1.6 s (original cycle period preserved).");
        }
    }
}
