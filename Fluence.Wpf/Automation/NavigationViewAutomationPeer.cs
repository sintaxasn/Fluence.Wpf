using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Exposes <see cref="NavigationView"/> to UI Automation as a selection list.
    /// </summary>
    public class NavigationViewAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider
    {
        /// <summary>Initializes a new instance.</summary>
        public NavigationViewAutomationPeer(NavigationView owner) : base(owner) { }

        private NavigationView NavigationView => (NavigationView)Owner;

        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return "NavigationView";
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.List;
        }

        /// <inheritdoc />
        public override object GetPattern(PatternInterface patternInterface)
        {
            return patternInterface != PatternInterface.Selection
                ? base.GetPattern(patternInterface)
                : this;
        }

        bool ISelectionProvider.CanSelectMultiple => false;

        bool ISelectionProvider.IsSelectionRequired => false;

        IRawElementProviderSimple[] ISelectionProvider.GetSelection()
        {
            object selected = NavigationView.SelectedItem;
            return selected is not null && NavigationView.ItemContainerGenerator.ContainerFromItem(selected) is NavigationViewItem container
                ? [ProviderFromPeer(CreatePeerForElement(container))]
                : [];
        }
    }
}
