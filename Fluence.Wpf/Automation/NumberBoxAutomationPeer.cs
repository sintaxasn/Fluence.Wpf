using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Exposes <see cref="NumberBox"/> to UI Automation as a spinner with range value.
    /// </summary>
    public class NumberBoxAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
    {
        /// <summary>Initializes a new instance.</summary>
        public NumberBoxAutomationPeer(NumberBox owner) : base(owner) { }

        private NumberBox NumberBox
        {
            get { return (NumberBox)Owner; }
        }

        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return "NumberBox";
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Spinner;
        }

        /// <inheritdoc />
        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.RangeValue)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        double IRangeValueProvider.Value
        {
            get { return NumberBox.Value; }
        }

        double IRangeValueProvider.Minimum
        {
            get { return NumberBox.Minimum; }
        }

        double IRangeValueProvider.Maximum
        {
            get { return NumberBox.Maximum; }
        }

        double IRangeValueProvider.SmallChange
        {
            get { return NumberBox.SmallChange; }
        }

        double IRangeValueProvider.LargeChange
        {
            get { return NumberBox.SmallChange; }
        }

        bool IRangeValueProvider.IsReadOnly
        {
            get { return !NumberBox.IsEnabled; }
        }

        void IRangeValueProvider.SetValue(double value)
        {
            NumberBox.Value = value;
        }
    }
}
