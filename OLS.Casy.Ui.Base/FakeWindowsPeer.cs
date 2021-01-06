using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation.Peers;

namespace OLS.Casy.Ui.Base
{
    public class FakeWindowsPeer : WindowAutomationPeer
    {
        public FakeWindowsPeer(Window window)
            : base(window)
        {
        }
        protected override List<AutomationPeer> GetChildrenCore()
        {
            return null;
        }
    }
}
