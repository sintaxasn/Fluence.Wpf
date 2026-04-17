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

        private ToggleSwitch ToggleSwitch
        {
            get { return (ToggleSwitch)Owner; }
        }

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
            if (patternInterface == PatternInterface.Toggle)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        ToggleState IToggleProvider.ToggleState
        {
            get
            {
                bool? isChecked = ToggleSwitch.IsChecked;
                if (isChecked == true)
                {
                    return ToggleState.On;
                }

                return isChecked == false ? ToggleState.Off : ToggleState.Indeterminate;
            }
        }

        void IToggleProvider.Toggle()
        {
            bool? current = ToggleSwitch.IsChecked;
            ToggleSwitch.IsChecked = current != true;
        }
    }
}
