using System.Windows.Automation;
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

        private NavigationView NavigationView
        {
            get { return (NavigationView)Owner; }
        }

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

        bool ISelectionProvider.CanSelectMultiple
        {
            get { return false; }
        }

        bool ISelectionProvider.IsSelectionRequired
        {
            get { return false; }
        }

        IRawElementProviderSimple[] ISelectionProvider.GetSelection()
        {
            var selected = NavigationView.SelectedItem;
            if (selected == null)
            {
                return new IRawElementProviderSimple[0];
            }

            var container = NavigationView.ItemContainerGenerator.ContainerFromItem(selected) as NavigationViewItem;
            if (container == null)
            {
                return new IRawElementProviderSimple[0];
            }

            var peer = CreatePeerForElement(container);
            if (peer == null)
            {
                return new IRawElementProviderSimple[0];
            }

            return new[] { ProviderFromPeer(peer) };
        }
    }
}
