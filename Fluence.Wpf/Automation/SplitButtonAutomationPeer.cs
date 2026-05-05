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
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Exposes <see cref="SplitButton"/> to UI Automation with the Invoke pattern
    /// (primary half) and the ExpandCollapse pattern (flyout half).
    /// </summary>
    public class SplitButtonAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider, IExpandCollapseProvider
    {
        /// <summary>Initializes a new instance.</summary>
        public SplitButtonAutomationPeer(SplitButton owner) : base(owner) { }

        private SplitButton SplitButton => (SplitButton)Owner;

        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return "SplitButton";
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.SplitButton;
        }

        /// <inheritdoc />
        public override object GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Invoke)
            {
                return this;
            }

            if (patternInterface == PatternInterface.ExpandCollapse)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        void IInvokeProvider.Invoke()
        {
            // Route Invoke to the primary half by raising SplitButton.Click and executing Command.
            var button = SplitButton;
            var command = button.Command;

            button.RaiseEvent(new RoutedEventArgs(SplitButton.ClickEvent, button));

            if (command == null)
            {
                return;
            }

            var parameter = button.CommandParameter;
            var target = button.CommandTarget;

            var routedCommand = command as System.Windows.Input.RoutedCommand;
            if (routedCommand != null)
            {
                if (routedCommand.CanExecute(parameter, target))
                {
                    routedCommand.Execute(parameter, target);
                }
            }
            else if (command.CanExecute(parameter))
            {
                command.Execute(parameter);
            }
        }

        ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState => SplitButton.IsFlyoutOpen
                    ? ExpandCollapseState.Expanded
                    : ExpandCollapseState.Collapsed;

        void IExpandCollapseProvider.Expand()
        {
            // The read-only IsFlyoutOpen reflects the secondary ToggleButton, which is
            // part of the template. Automation clients opening a SplitButton without a
            // visual tree see no-op behavior; with a template applied, the overridden
            // PropertyChanged wiring flips the popup via the secondary button.
            var owner = SplitButton;
            var toggle = owner.Template == null ? null
                : owner.Template.FindName("PART_SecondaryButton", owner) as System.Windows.Controls.Primitives.ToggleButton;
            if (toggle != null)
            {
                toggle.IsChecked = true;
            }
        }

        void IExpandCollapseProvider.Collapse()
        {
            var owner = SplitButton;
            var toggle = owner.Template == null ? null
                : owner.Template.FindName("PART_SecondaryButton", owner) as System.Windows.Controls.Primitives.ToggleButton;
            if (toggle != null)
            {
                toggle.IsChecked = false;
            }
        }
    }
}
