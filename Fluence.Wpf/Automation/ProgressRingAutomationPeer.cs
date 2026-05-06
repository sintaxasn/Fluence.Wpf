using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Exposes <see cref="ProgressRing"/> to UI Automation as a progress indicator.
    /// </summary>
    /// <remarks>Initializes a new instance.</remarks>
    public class ProgressRingAutomationPeer(ProgressRing owner) : FrameworkElementAutomationPeer(owner), IRangeValueProvider
    {
        private ProgressRing ProgressRing => (ProgressRing)Owner;

        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return "ProgressRing";
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ProgressBar;
        }

        /// <inheritdoc />
        public override object GetPattern(PatternInterface patternInterface)
        {
            return patternInterface != PatternInterface.RangeValue || ProgressRing.IsIndeterminate
                ? base.GetPattern(patternInterface)
                : this;
        }

        /// <inheritdoc />
        public virtual double Value => ProgressRing.Value;

        /// <inheritdoc />
        public virtual double Minimum => ProgressRing.Minimum;

        /// <inheritdoc />
        public virtual double Maximum => ProgressRing.Maximum;

        /// <inheritdoc />
        public virtual double SmallChange => 1;

        /// <inheritdoc />
        public virtual double LargeChange => 10;

        /// <inheritdoc />
        public virtual bool IsReadOnly => true;

        /// <inheritdoc />
        public virtual void SetValue(double value)
        {
        }
    }
}
