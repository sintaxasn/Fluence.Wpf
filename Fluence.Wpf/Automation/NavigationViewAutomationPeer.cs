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
            if (patternInterface == PatternInterface.Selection)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        bool ISelectionProvider.CanSelectMultiple => false;

        bool ISelectionProvider.IsSelectionRequired => false;

        IRawElementProviderSimple[] ISelectionProvider.GetSelection()
        {
            object selected = NavigationView.SelectedItem;
            if (selected == null)
            {
                return System.Array.Empty<IRawElementProviderSimple>();
            }

            if (NavigationView.ItemContainerGenerator.ContainerFromItem(selected) is not NavigationViewItem container)
            {
                return System.Array.Empty<IRawElementProviderSimple>();
            }

            AutomationPeer peer = CreatePeerForElement(container);
            if (peer == null)
            {
                return System.Array.Empty<IRawElementProviderSimple>();
            }

            return new[] { ProviderFromPeer(peer) };
        }
    }
}
