using System.Windows.Automation.Peers;
using Fluence.Wpf.Controls;

namespace Fluence.Wpf.Automation
{
    /// <summary>
    /// Exposes <see cref="InfoBar"/> to UI Automation as a status bar element.
    /// </summary>
    public class InfoBarAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>Initializes a new instance.</summary>
        public InfoBarAutomationPeer(InfoBar owner) : base(owner) { }

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
            var infoBar = (InfoBar)Owner;
            string title = infoBar.Title;
            if (!string.IsNullOrEmpty(title))
            {
                return title;
            }

            return base.GetNameCore();
        }
    }
}
