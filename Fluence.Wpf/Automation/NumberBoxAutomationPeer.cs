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

        private NumberBox NumberBox => (NumberBox)Owner;

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
            return patternInterface != PatternInterface.RangeValue
                ? base.GetPattern(patternInterface)
                : this;
        }

        double IRangeValueProvider.Value => NumberBox.Value;

        double IRangeValueProvider.Minimum => NumberBox.Minimum;

        double IRangeValueProvider.Maximum => NumberBox.Maximum;

        double IRangeValueProvider.SmallChange => NumberBox.SmallChange;

        double IRangeValueProvider.LargeChange => NumberBox.SmallChange;

        bool IRangeValueProvider.IsReadOnly => !NumberBox.IsEnabled;

        void IRangeValueProvider.SetValue(double value)
        {
            NumberBox.Value = value;
        }
    }
}
