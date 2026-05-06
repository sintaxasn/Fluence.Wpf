using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Exposes <see cref="DropDownButton"/> to UI Automation with the ExpandCollapse pattern.
    /// </summary>
    /// <remarks>Initializes a new instance.</remarks>
    public class DropDownButtonAutomationPeer(DropDownButton owner) : FrameworkElementAutomationPeer(owner), IExpandCollapseProvider
    {
        private DropDownButton DropDownButton => (DropDownButton)Owner;

        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return "DropDownButton";
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.SplitButton;
        }

        /// <inheritdoc />
        public override object GetPattern(PatternInterface patternInterface)
        {
            return patternInterface != PatternInterface.ExpandCollapse
                ? base.GetPattern(patternInterface)
                : this;
        }

        ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState => DropDownButton.IsChecked == true
                    ? ExpandCollapseState.Expanded
                    : ExpandCollapseState.Collapsed;

        void IExpandCollapseProvider.Expand()
        {
            DropDownButton.IsChecked = true;
        }

        void IExpandCollapseProvider.Collapse()
        {
            DropDownButton.IsChecked = false;
        }
    }
}
