using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Exposes <see cref="ToggleSwitch"/> to UI Automation with the Toggle pattern.
    /// </summary>
    /// <remarks>Initializes a new instance.</remarks>
    public class ToggleSwitchAutomationPeer(ToggleSwitch owner) : FrameworkElementAutomationPeer(owner), IToggleProvider
    {
        private ToggleSwitch ToggleSwitch => (ToggleSwitch)Owner;

        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return "ToggleSwitch";
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Button;
        }

        /// <inheritdoc />
        public override object GetPattern(PatternInterface patternInterface)
        {
            return patternInterface != PatternInterface.Toggle
                ? base.GetPattern(patternInterface)
                : this;
        }

        /// <inheritdoc />
        public virtual ToggleState ToggleState => ToggleSwitch.IsChecked is bool isChecked
            ? !isChecked ? ToggleState.Off : ToggleState.On
            : ToggleState.Indeterminate;

        /// <inheritdoc />
        public virtual void Toggle()
        {
            bool? current = ToggleSwitch.IsChecked;
            ToggleSwitch.IsChecked = current != true;
        }
    }
}
