using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Exposes <see cref="NavigationViewItem"/> to UI Automation as a selectable list item.
    /// </summary>
    public class NavigationViewItemAutomationPeer : FrameworkElementAutomationPeer, ISelectionItemProvider, IInvokeProvider
    {
        /// <summary>Initializes a new instance.</summary>
        public NavigationViewItemAutomationPeer(NavigationViewItem owner) : base(owner) { }

        private NavigationViewItem NavigationViewItem
        {
            get { return (NavigationViewItem)Owner; }
        }

        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return "NavigationViewItem";
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ListItem;
        }

        /// <inheritdoc />
        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.SelectionItem || patternInterface == PatternInterface.Invoke)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        bool ISelectionItemProvider.IsSelected
        {
            get { return NavigationViewItem.IsSelected; }
        }

        IRawElementProviderSimple ISelectionItemProvider.SelectionContainer
        {
            get
            {
                var nav = ItemsControl.ItemsControlFromItemContainer(NavigationViewItem) as NavigationView;
                if (nav == null)
                {
                    return null;
                }

                var peer = CreatePeerForElement(nav);
                return peer != null ? ProviderFromPeer(peer) : null;
            }
        }

        void ISelectionItemProvider.Select()
        {
            var nav = ItemsControl.ItemsControlFromItemContainer(NavigationViewItem) as NavigationView;
            if (nav != null)
            {
                var data = nav.ItemContainerGenerator.ItemFromContainer(NavigationViewItem);
                nav.SelectedItem = (data != System.Windows.DependencyProperty.UnsetValue && data != null) ? data : NavigationViewItem;
            }
        }

        void ISelectionItemProvider.AddToSelection()
        {
            ((ISelectionItemProvider)this).Select();
        }

        void ISelectionItemProvider.RemoveFromSelection()
        {
        }

        void IInvokeProvider.Invoke()
        {
            ((ISelectionItemProvider)this).Select();
        }
    }
}
