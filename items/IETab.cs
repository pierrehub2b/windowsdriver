using FlaUI.Core.AutomationElements;
using FlaUI.Core.AutomationElements.Infrastructure;
using FlaUI.Core.Definitions;

namespace windowsdriver.items
{
    class IETab
    {
        private readonly TabItem item;
        private readonly IEWindow window;
        private readonly string windowId;

        public IETab(TabItem item, IEWindow window, string windowId)
        {
            this.item = item;
            this.window = window;
            this.windowId = windowId;
        }

        public bool EqualsTab(AutomationElement item)
        {
            return this.item.Equals(item);
        }

        public bool EqualsWindow(string id)
        {
            return this.windowId.Equals(id);
        }

        public void ToFront()
        {
            window.ToFront();
            item.Click();
        }

        public void Close()
        {
            ToFront();
            AutomationElement closeButton = item.FindFirstChild(item.ConditionFactory.ByControlType(ControlType.Button));
            if(closeButton != null)
            {
                closeButton.Click();
            }
        }
    }
}