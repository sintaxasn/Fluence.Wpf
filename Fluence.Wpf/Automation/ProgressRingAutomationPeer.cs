using System.Windows.Automation;
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

        private ProgressRing ProgressRing
        {
            get { return (ProgressRing)Owner; }
        }

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
            if (patternInterface == PatternInterface.RangeValue && !ProgressRing.IsIndeterminate)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        double IRangeValueProvider.Value
        {
            get { return ProgressRing.Value; }
        }

        double IRangeValueProvider.Minimum
        {
            get { return ProgressRing.Minimum; }
        }

        double IRangeValueProvider.Maximum
        {
            get { return ProgressRing.Maximum; }
        }

        double IRangeValueProvider.SmallChange
        {
            get { return 1; }
        }

        double IRangeValueProvider.LargeChange
        {
            get { return 10; }
        }

        bool IRangeValueProvider.IsReadOnly
        {
            get { return true; }
        }

        void IRangeValueProvider.SetValue(double value)
        {
        }
    }
}
