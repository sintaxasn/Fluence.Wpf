using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Exposes <see cref="NumberBox"/> to UI Automation as a spinner with range value.
    /// </summary>
    /// <remarks>Initializes a new instance.</remarks>
    public class NumberBoxAutomationPeer(NumberBox owner) : FrameworkElementAutomationPeer(owner), IRangeValueProvider
    {
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

        /// <inheritdoc />
        public virtual double Value => NumberBox.Value;

        /// <inheritdoc />
        public virtual double Minimum => NumberBox.Minimum;

        /// <inheritdoc />
        public virtual double Maximum => NumberBox.Maximum;

        /// <inheritdoc />
        public virtual double SmallChange => NumberBox.SmallChange;

        /// <inheritdoc />
        public virtual double LargeChange => NumberBox.SmallChange;

        /// <inheritdoc />
        public virtual bool IsReadOnly => !NumberBox.IsEnabled;

        /// <inheritdoc />
        public virtual void SetValue(double value)
        {
            NumberBox.Value = value;
        }
    }
}
