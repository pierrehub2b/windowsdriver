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
using FlaUI.Core.Definitions;
using FlaUI.Core.Identifiers;
using FlaUI.UIA3;
using FlaUI.UIA3.EventHandlers;
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;
using windowsdriver.items;

namespace windowsdriver
{
    public class DesktopManager
    {
        public readonly DesktopElement DesktopElement;

        private readonly int WinCloseEventId;

        private readonly ListMap<int, AutomationElement> handles = new ListMap<int, AutomationElement>();
        private readonly ListMap<int, AutomationElement> popups = new ListMap<int, AutomationElement>();

        private readonly UIA3Automation uia3 = new UIA3Automation();
        private readonly AutomationElement desktop;
               
        public readonly int DesktopWidth;
        public readonly int DesktopHeight;

        public readonly Rectangle DesktopRect;

        public DesktopManager()
        {
            WinCloseEventId = System.Windows.Automation.WindowPattern.WindowClosedEvent.Id;

            DesktopRect = SystemInformation.VirtualScreen;
            DesktopWidth = DesktopRect.Width;
            DesktopHeight = DesktopRect.Height;

            uia3.ConnectionTimeout = new TimeSpan(0, 0, 10);
            uia3.TransactionTimeout = new TimeSpan(0, 1, 0);
            desktop = uia3.GetDesktop();

            DesktopElement = new DesktopElement(desktop, DesktopRect);

            AutomationElement[] children = desktop.FindAllChildren();
            foreach (AutomationElement child in children)
            {
                if(child.Properties.ProcessId.IsSupported)
                {
                    int pid = child.Properties.ProcessId;

                    if (!child.Patterns.Window.IsSupported)
                    {
                        if (!IsDesktopComponent(child.ClassName))
                        {
                            AddPopup(pid, child);
                        }
                    }
                    else if (child.Properties.NativeWindowHandle.IsSupported)
                    {
                        AddHandle(pid, child.AsWindow());
                    }
                }
            }
                       
            var eventHandler = desktop.RegisterStructureChangedEvent(TreeScope.Children, (element, type, arg3) =>
            {
                if (type.Equals(StructureChangeType.ChildAdded))
                {
                    if(element.Properties.ProcessId.IsSupported && element.Properties.ClassName.IsSupported)
                    {
                        int pid = element.Properties.ProcessId;
                        string className = element.Properties.ClassName;
                        if (className != "SysShadow")
                        {
                            if (!element.Patterns.Window.IsSupported)
                            {
                                AddPopup(pid, element);
                            }
                            else if (element.Properties.NativeWindowHandle.IsSupported)
                            {
                                AddHandle(pid, element.AsWindow());
                            }
                        }
                    }
                }
            });
        }

        public bool ContainsPoint(int x, int y)
        {
            return DesktopRect.Contains(x, y);
        }

        private void AddHandle(int pid, Window win)
        {
            handles.Add(pid, win);

            UIA3AutomationEventHandler closeEvent = null;
            closeEvent = (UIA3AutomationEventHandler)win.RegisterAutomationEvent(new EventId(WinCloseEventId, "WindowClosedEvent"), TreeScope.Element, (removed, evType) =>
            {
                closeEvent.Dispose();
                handles.Remove(removed);
            });
        }

        private void AddPopup(int pid, AutomationElement popup)
        {
            popups.Add(pid, popup);

            UIA3StructureChangedEventHandler closeEvent = null;
            closeEvent = (UIA3StructureChangedEventHandler)popup.RegisterStructureChangedEvent(TreeScope.Element, (removed, id, obj) =>
            {
                closeEvent.Dispose();
                popups.Remove(removed);
            });
        }

        private class ListMap<T, V> : List<KeyValuePair<T, V>>
        {
            public void Add(T key, V value)
            {
                if (!Contains(value))
                {
                    Add(new KeyValuePair<T, V>(key, value));
                }
            }

            public V[] Get(T key)
            {
                return FindAll(p => p.Key.Equals(key)).ConvertAll(p => p.Value).ToArray();
            }

            public bool Contains(V value)
            {
                foreach(KeyValuePair<T, V> kv in this)
                {
                    if (kv.Value.Equals(value))
                    {
                        return true;
                    }
                }
                return false;
            }

            public void Remove(V value)
            {
                Remove(Find(p => p.Value.Equals(value)));
            }
        }
        
        //-------------------------------------------------------------------------------------------------------------
        //-------------------------------------------------------------------------------------------------------------

        public AtsElement GetElementFromPoint(Point pt)
        {
            AutomationElement elem = uia3.FromPoint(pt);
            if(elem != null){
                return new AtsElement(elem);
            }
            return null;
        }

        //-------------------------------------------------------------------------------------------------------------
        //-------------------------------------------------------------------------------------------------------------

        public DesktopWindow GetWindowIndexByPid(int pid, int index)
        {
            List<KeyValuePair<int, AutomationElement>> pids = handles.FindAll(w => w.Key == pid);

            if (index > pids.Count)
            {
                return new DesktopWindow(pids[index].Value, this); 
            }

            return null;
        }

        public DesktopWindow GetWindowPid(string title)
        {
            AutomationElement[] windows = desktop.FindAllChildren();

            for (int i = 0; i < windows.Length; i++)
            {
                AutomationElement window = windows[i];
                if (window.Properties.Name.IsSupported && window.Name.IndexOf(title, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return new DesktopWindow(window, this);
                }
            }

            //-------------------------------------------------------------------------------------------------
            // second chance to find the window
            //-------------------------------------------------------------------------------------------------

            for (int i = 0; i < windows.Length; i++)
            {
                AutomationElement[] windowChildren = windows[i].FindAllChildren();
                for (int j = 0; j < windowChildren.Length; j++)
                {
                    AutomationElement windowChild = windowChildren[j];
                    if (windowChild.Properties.Name.IsSupported && windowChild.Name.IndexOf(title, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return new DesktopWindow(windowChild, this);
                    }
                }
            }

            return null;
        }

        public DesktopWindow GetWindowByHandle(int handle)
        {
            if (handle > 0)
            {
                AutomationElement window = handles.Find(e => e.Value.AsWindow().Properties.NativeWindowHandle == new IntPtr(handle)).Value;
                if (window != null)
                {
                    return new DesktopWindow(window, this);
                }
            }
            else
            {
                return DesktopElement;
            }
            return null;
        }

        public List<DesktopWindow> GetOrderedWindowsByPid(int pid)
        {
            List<DesktopWindow> windowsList = new List<DesktopWindow>();
            handles.FindAll(w => w.Key == pid).ForEach(e => windowsList.Add(new DesktopWindow(e.Value, this)));

            return windowsList;
        }

        public AtsElement[] GetElements(string tag, string[] attributes)
        {
            return DesktopElement.GetElements(tag, attributes, desktop, this);
        }

        public AutomationElement[] GetPopupDescendants(int pid)
        {
            List<AutomationElement> list = new List<AutomationElement>();
            foreach (AutomationElement p in popups.Get(pid))
            {
                list.Add(p);
                list.AddRange(p.FindAllDescendants());
            }
            return list.ToArray();
        }

        public AutomationElement[] GetDialogChildren(int pid)
        {
            return desktop.FindAllChildren(desktop.ConditionFactory.ByControlType(ControlType.Window).And(desktop.ConditionFactory.ByProcessId(pid)));
        }

        //----------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------

        public static bool IsDesktopComponent(string className)
        {
            return className.StartsWith("Shell_")
                    || className.StartsWith("TaskList")
                    || className == "SysListView32"
                    || className == "Progman"
                    || className == "NotifyIconOverflowWindow"
                    || className == "Windows.UI.Core.CoreWindow";
        }
    }
}
