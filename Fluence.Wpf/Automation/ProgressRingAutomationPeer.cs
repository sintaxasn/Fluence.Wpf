using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Exposes <see cref="ProgressRing"/> to UI Automation as a progress indicator.
    /// </summary>
    public class ProgressRingAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
    {
        /// <summary>Initializes a new instance.</summary>
        public ProgressRingAutomationPeer(ProgressRing owner) : base(owner) { }

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

        double IRangeValueProvider.Value => ProgressRing.Value;

        double IRangeValueProvider.Minimum => ProgressRing.Minimum;

        double IRangeValueProvider.Maximum => ProgressRing.Maximum;

        double IRangeValueProvider.SmallChange => 1;

        double IRangeValueProvider.LargeChange => 10;

        bool IRangeValueProvider.IsReadOnly => true;

        void IRangeValueProvider.SetValue(double value)
        {
        }
    }
}
