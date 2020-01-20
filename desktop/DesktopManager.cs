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
using System.Collections.Generic;
using windowsdriver.items;

namespace windowsdriver
{
    class DesktopManager
    {
        private readonly Dictionary<int, DesktopChild> handles = new Dictionary<int, DesktopChild>();
        private readonly UIA3Automation uia3 = new UIA3Automation();
        private readonly AutomationElement desktop;
        private readonly DesktopElement desktopElement;

        public DesktopManager()
        {
            desktop = uia3.GetDesktop();
            desktopElement = new DesktopElement(desktop);

            AutomationElement[] children = desktop.FindAllChildren();
            foreach (AutomationElement child in children)
            {
                if (child.Properties.NativeWindowHandle.IsSupported && child.Properties.ProcessId.IsSupported)
                {
                    AddHandle(child.Properties.NativeWindowHandle.Value.ToInt32(), child.Properties.ProcessId, child.ControlType, child.ClassName);
                }
            }

            var eventHandler = desktop.RegisterStructureChangedEvent(FlaUI.Core.Definitions.TreeScope.Children, (element, type, arg3) =>
            {
                if (element.Properties.NativeWindowHandle.IsSupported && element.Properties.ProcessId.IsSupported && element.Properties.ClassName.IsSupported)
                {
                    int nativeHandle = element.Properties.NativeWindowHandle.Value.ToInt32();
                    if (type.Equals(StructureChangeType.ChildAdded))
                    {
                        AddHandle(nativeHandle, element.Properties.ProcessId, element.ControlType, element.ClassName);
                                               
                        UIA3AutomationEventHandler closeEvent = null;
                        closeEvent = (UIA3AutomationEventHandler)element.RegisterAutomationEvent(new EventId(20017, "WindowClosedEvent"), FlaUI.Core.Definitions.TreeScope.Element, (w, evType) =>
                        {
                            closeEvent.Dispose();
                            RemoveHandle(nativeHandle);
                        });
                    }
                }
            });
        }

        private void AddHandle(int key, int pid, ControlType type, string className)
        {
            if (!handles.ContainsKey(key))
            {
                handles.Add(key, new DesktopChild(pid, type == ControlType.Pane || type == ControlType.Window, className));
            }
        }

        private void RemoveHandle(int key)
        {
            if (handles.ContainsKey(key))
            {
                handles.Remove(key);
            }
        }

        //-----------------------------------------------------------------------------------------------------------
        //-----------------------------------------------------------------------------------------------------------

        public class DesktopChild
        {
            public int Pid;
            public string ClassName;
            public bool PaneOrWindow;
            public bool LoadChildren;

            public DesktopChild(int pid, bool paneOrWindow, string className)
            {
                Pid = pid;
                ClassName = className;
                PaneOrWindow = paneOrWindow;
                LoadChildren = DesktopElement.IsDesktopComponent(className);
            }
        }

        //-------------------------------------------------------------------------------------------------------------
        //-------------------------------------------------------------------------------------------------------------

        public DesktopWindow GetWindowIndexByPid(int pid, int index)
        {
            List<int> wins = new List<int>();
            foreach (KeyValuePair<int, DesktopChild> pair in handles)
            {
                if(pair.Value.Pid == pid)
                {
                    wins.Add(pair.Key);
                }
            }

            if (index > wins.Count)
            {
                return new DesktopWindow(uia3.FromHandle(new IntPtr(wins[index])));
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
                    return CachedElement.GetCachedWindow(window);
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
                        return CachedElement.GetCachedWindow(windowChild);
                    }
                }
            }

            return null;
        }

        public DesktopWindow GetWindowByHandle(int handle)
        {
            if (handle > 0)
            {
                AutomationElement window = uia3.FromHandle(new IntPtr(handle));
                if (window != null)
                {
                    return new DesktopWindow(window);
                }
            }
            return null;
        }

        public List<DesktopWindow> GetOrderedWindowsByPid(int pid)
        {
            List<DesktopWindow> windowsList = new List<DesktopWindow>();

            foreach (KeyValuePair<int, DesktopChild> pair in handles)
            {
                if (pair.Value.Pid == pid && pair.Value.PaneOrWindow)
                {
                    windowsList.Add(CachedElement.GetCachedWindow(uia3.FromHandle(new IntPtr(pair.Key))));
                }
            }

            return windowsList;
        }

        public AtsElement[] GetElements(string tag, string[] attributes)
        {
            return desktopElement.GetElements(tag, attributes);
        }
    }
}
