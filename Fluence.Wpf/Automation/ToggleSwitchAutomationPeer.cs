using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Exposes <see cref="ToggleSwitch"/> to UI Automation with the Toggle pattern.
    /// </summary>
    public class ToggleSwitchAutomationPeer : FrameworkElementAutomationPeer, IToggleProvider
    {
        /// <summary>Initializes a new instance.</summary>
        public ToggleSwitchAutomationPeer(ToggleSwitch owner) : base(owner) { }

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

        ToggleState IToggleProvider.ToggleState => ToggleSwitch.IsChecked is bool isChecked
            ? !isChecked ? ToggleState.Off : ToggleState.On
            : ToggleState.Indeterminate;

        void IToggleProvider.Toggle()
        {
            bool? current = ToggleSwitch.IsChecked;
            ToggleSwitch.IsChecked = current != true;
        }
    }
}
