using FlaUI.Core.AutomationElements;
using FlaUI.Core.AutomationElements.Infrastructure;
using FlaUI.Core.Definitions;
using FlaUI.Core.EventHandlers;
using FlaUI.Core.Identifiers;
using System;
using System.Collections.Generic;

namespace windowsdriver.items
{
    class IEWindow
    {
        public int Pid { get; set; }
        public IntPtr Handle { get; set; }

        private readonly Window window;
        private readonly List<TabItem> openedTabs = new List<TabItem>();

        private int tabsCount = 0;

        public IEWindow(Window win, List<IEWindow> ieWindows)
        {
            window = win;

            Pid = window.Properties.ProcessId;
            Handle = window.Properties.NativeWindowHandle;
            
            EventId closeEventId = new EventId(20017, "WindowClosedEvent");

            IAutomationEventHandler closeEvent = null;
            closeEvent = window.RegisterEvent(closeEventId, TreeScope.Element, (w, tp) =>
            {
                w.RemoveAutomationEventHandler(closeEventId, closeEvent);
                ieWindows.Remove(this);
            });

            AutomationElement tab = window.FindFirst(TreeScope.Descendants, window.ConditionFactory.ByControlType(ControlType.Tab));
            AddTab(tab.FindFirstChild(tab.ConditionFactory.ByControlType(ControlType.TabItem)).AsTabItem());

            var eventHandler = tab.RegisterStructureChangedEvent(TreeScope.Children, (element, type, arg3) =>
            {
                if (tabsCount != tab.AsTab().TabItems.Length)
                {
                    tabsCount = tab.AsTab().TabItems.Length;

                    if (element.ControlType.Equals(ControlType.TabItem))
                    {
                        if (type.Equals(StructureChangeType.ChildAdded))
                        {
                            AddTab(element.AsTabItem());
                        }
                        else if (type.Equals(StructureChangeType.ChildRemoved))
                        {
                            RemoveTab(tab.FindAllChildren(tab.ConditionFactory.ByControlType(ControlType.TabItem)));
                        }
                    }
                }
            });
        }

        private void AddTab(TabItem tab)
        {
            if (!openedTabs.Contains(tab))
            {
                openedTabs.Add(tab);
            }
        }

        private void RemoveTab(AutomationElement[] tabItems)
        {
            foreach (TabItem item in openedTabs)
            {
                if (Array.FindIndex(tabItems, i => i.Equals(item)) < 0)
                {
                    openedTabs.Remove(item);
                    break;
                }
            }
        }

        public int CheckToFront(int index)
        {
            if(index >= openedTabs.Count)
            {
                return index - openedTabs.Count;
            }

            window.SetForeground();
            window.FocusNative();

            openedTabs[index].Click();

            return -1;
        }
        
        public static int SetWindowToFront(int index, List<IEWindow> ieWindows)
        {
            int winIndex = 0;
            foreach(IEWindow win in ieWindows)
            {
                index = win.CheckToFront(index);
                if(index == -1)
                {
                    return winIndex;
                }
                winIndex++;
            }
            return -1;
        }
    }
}