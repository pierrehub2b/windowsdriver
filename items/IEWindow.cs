/*
Licensed to the Apache Software Foundation (ASF) under one
or more contributor license agreements.  See the NOTICE file
distributed with this work for additional information
regarding copyright ownership.  The ASF licenses this file
to you under the Apache License, Version 2.0 (the
"License"); you may not use this file except in compliance
with the License.  You may obtain a copy of the License at

  http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing,
software distributed under the License is distributed on an
"AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
KIND, either express or implied.  See the License for the
specific language governing permissions and limitations
under the License.
 */

using FlaUI.Core.AutomationElements;
using FlaUI.Core.AutomationElements.Infrastructure;
using FlaUI.Core.Definitions;
using FlaUI.Core.EventHandlers;
using FlaUI.Core.Identifiers;
using System;
using System.Collections.Generic;
using windowsdriver.actions;

namespace windowsdriver.items
{
    class IEWindow
    {
        public int Pid { get; set; }
        public IntPtr Handle { get; set; }

        private readonly Window window;

        private int tabsCount = 0;

        private readonly string id;

        private readonly List<IETab> tabs = new List<IETab>();

        public IEWindow(Window win, ActionIEWindow ie)
        {
            window = win;
            id = Guid.NewGuid().ToString();

            Pid = window.Properties.ProcessId;
            Handle = window.Properties.NativeWindowHandle;
            
            EventId closeEventId = new EventId(20017, "WindowClosedEvent");

            IAutomationEventHandler closeEvent = null;
            closeEvent = window.RegisterEvent(closeEventId, TreeScope.Element, (w, tp) =>
            {
                w.RemoveAutomationEventHandler(closeEventId, closeEvent);
                Array.ForEach(tabs.ToArray(), r => RemoveTab(r));
                ie.RemoveWindow(id);
            });

            AutomationElement tab = window.FindFirst(TreeScope.Descendants, window.ConditionFactory.ByControlType(ControlType.Tab));
            AddTab(tab.FindFirstChild(tab.ConditionFactory.ByControlType(ControlType.TabItem)).AsTabItem(), this, id);

            var eventHandler = tab.RegisterStructureChangedEvent(TreeScope.Children, (element, type, arg3) =>
            {
                if (tabsCount != tab.AsTab().TabItems.Length)
                {
                    tabsCount = tab.AsTab().TabItems.Length;

                    if (element.ControlType.Equals(ControlType.TabItem))
                    {
                        if (type.Equals(StructureChangeType.ChildAdded))
                        {
                            AddTab(element.AsTabItem(), this, id);
                        }
                        else if (type.Equals(StructureChangeType.ChildRemoved))
                        {
                            RemoveTab(tab.FindAllChildren(tab.ConditionFactory.ByControlType(ControlType.TabItem)));
                        }
                    }
                }
            });
        }

        internal void AddTab(TabItem item, IEWindow window, string windowId)
        {
            foreach (IETab tab in tabs)
            {
                if (tab.EqualsTab(item))
                {
                    return;
                }
            }
            tabs.Add(new IETab(item, window, windowId));
        }

        internal void RemoveTab(AutomationElement[] listTabs)
        {
            foreach (IETab tab in tabs)
            {
                if (Array.FindIndex(listTabs, e => tab.EqualsTab(e)) < 0)
                {
                    RemoveTab(tab);
                    break;
                }
            }
        }

        private void RemoveTab(IETab tab)
        {
            tabs.Remove(tab);
        }

        public bool EqualsWindowId(string id)
        {
            return this.id.Equals(id);
        }

        public void ToFront()
        {
            window.SetForeground();
            window.FocusNative();
        }

        public void Close()
        {
            window.Close();
        }
    }
}