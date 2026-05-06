using System.Windows.Automation.Peers;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Exposes <see cref="InfoBar"/> to UI Automation as a status bar element.
    /// </summary>
    /// <remarks>Initializes a new instance.</remarks>
    public class InfoBarAutomationPeer(InfoBar owner) : FrameworkElementAutomationPeer(owner)
    {

        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return "InfoBar";
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.StatusBar;
        }

        /// <inheritdoc />
        protected override string GetNameCore()
        {
            string title = ((InfoBar)Owner).Title;
            return string.IsNullOrWhiteSpace(title)
                ? base.GetNameCore()
                : title;
        }
    }
}
