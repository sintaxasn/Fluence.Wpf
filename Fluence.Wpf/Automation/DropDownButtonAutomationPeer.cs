using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Exposes <see cref="DropDownButton"/> to UI Automation with the ExpandCollapse pattern.
    /// </summary>
    public class DropDownButtonAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
    {
        /// <summary>Initializes a new instance.</summary>
        public DropDownButtonAutomationPeer(DropDownButton owner) : base(owner) { }

        private DropDownButton DropDownButton
        {
            get { return (DropDownButton)Owner; }
        }

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
            if (patternInterface == PatternInterface.ExpandCollapse)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
        {
            get
            {
                return DropDownButton.IsChecked == true
                    ? ExpandCollapseState.Expanded
                    : ExpandCollapseState.Collapsed;
            }
        }

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
