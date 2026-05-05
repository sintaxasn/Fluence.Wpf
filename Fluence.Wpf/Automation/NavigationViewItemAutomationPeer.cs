/*
 * Copyright 2026 Dan Cunningham
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */
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

        private NavigationViewItem NavigationViewItem => (NavigationViewItem)Owner;

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

        bool ISelectionItemProvider.IsSelected => NavigationViewItem.IsSelected;

        IRawElementProviderSimple ISelectionItemProvider.SelectionContainer
        {
            get
            {
                if (ItemsControl.ItemsControlFromItemContainer(NavigationViewItem) is not NavigationView nav)
                {
                    return null;
                }

                AutomationPeer peer = CreatePeerForElement(nav);
                return peer != null ? ProviderFromPeer(peer) : null;
            }
        }

        void ISelectionItemProvider.Select()
        {
            if (ItemsControl.ItemsControlFromItemContainer(NavigationViewItem) is NavigationView nav)
            {
                nav.SelectItemFromContainer(NavigationViewItem);
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
            if (ItemsControl.ItemsControlFromItemContainer(NavigationViewItem) is NavigationView nav)
            {
                nav.InvokeItem(NavigationViewItem);
            }
        }
    }
}
