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
using FlaUI.UIA3;
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;
using windowsdriver.items;
using windowsdriver.desktop;
using FlaUI.Core.Conditions;
using System.Threading;
using System.Diagnostics;
using FlaUI.Core;

namespace windowsdriver
{
    public class DesktopManager
    {
        public readonly DesktopElement DesktopElement;

        private readonly List<WindowHandle> handles = new List<WindowHandle>();
        private readonly List<PopupHandle> popups = new List<PopupHandle>();

        private readonly UIA3Automation uia3 = new UIA3Automation();
        private readonly AutomationElement desktop;
               
        public readonly int DesktopWidth;
        public readonly int DesktopHeight;

        public readonly Rectangle DesktopRect;

        public readonly PropertyCondition OnScreenProperty;
        private readonly AndCondition TopModalCondition;

        public DesktopManager()
        {
            DesktopRect = SystemInformation.VirtualScreen;
            DesktopWidth = DesktopRect.Width;
            DesktopHeight = DesktopRect.Height;

            uia3.ConnectionTimeout = new TimeSpan(0, 0, 30);
            uia3.TransactionTimeout = new TimeSpan(0, 1, 0);

            desktop = uia3.GetDesktop();
            OnScreenProperty = new PropertyCondition(uia3.PropertyLibrary.Element.IsOffscreen, false);
            TopModalCondition = desktop.ConditionFactory.ByControlType(ControlType.Pane).And(desktop.ConditionFactory.ByClassName("Alternate Modal Top Most"));

            DesktopElement = new DesktopElement(desktop, DesktopRect);

            AutomationElement[] children = desktop.FindAllChildren();
            foreach (AutomationElement child in children)
            {
                if (child.Properties.ProcessId.IsSupported && child.Patterns.Window.IsSupported && child.Properties.NativeWindowHandle.IsSupported)
                {
                    AddHandle(child.Properties.ProcessId, child);
                }
            }
                       
            var eventHandler = desktop.RegisterStructureChangedEvent(TreeScope.Children, (element, type, arg) =>
            {
                if (type.Equals(StructureChangeType.ChildAdded))
                {
                    try
                    {
                        int pid = element.Properties.ProcessId;
                        string className = element.Properties.ClassName;

                        if (className != "SysShadow")
                        {
                            if (!element.Patterns.Window.IsSupported)
                            {
                                popups.Add(new PopupHandle(pid, element, popups));
                            }
                            else if (element.Properties.NativeWindowHandle.IsSupported)
                            {
                                AddHandle(pid, element);
                            }
                        }
                    }
                    catch {}
                }
            });
        }
        
        internal DesktopWindow getAppMainWindow(FlaUI.Core.Application app)
        {
            Window win = app.GetMainWindow(uia3, TimeSpan.FromSeconds(10));
            return new DesktopWindow(win, this);
        }

        public void Clean()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public bool ContainsPoint(int x, int y)
        {
            return DesktopRect.Contains(x, y);
        }

        private void AddHandle(int pid, AutomationElement win)
        {
            handles.Add(new WindowHandle(pid, win, handles));
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
            List<WindowHandle> pids = handles.FindAll(w => w.Pid == pid);

            if (index < pids.Count)
            {
                return new DesktopWindow(pids[index].Win, this); 
            }

            return null;
        }
        public DesktopWindow GetJxWindowPid(string title)
        {

            AutomationElement[] windows = desktop.FindAllChildren();

            //-------------------------------------------------------------------------------------------------
            // Try to find JXBrowser main window
            //-------------------------------------------------------------------------------------------------

            for (int i = 0; i < windows.Length; i++)
            {
                AutomationElement window = windows[i];
                if (window.Properties.AutomationId.IsSupported && window.Properties.Name.IsSupported
                    && window.AutomationId.Equals("JavaFX1")) {

                    string windowName = window.Properties.Name;
                    if (windowName.ToLower().Contains(title.ToLower()))
                    {
                        return new DesktopWindow(window, this);
                    }
                }
            }
            return null;
        }
        
        public DesktopWindow GetWindowPid(string title)
        {
            AutomationElement[] windows = desktop.FindAllChildren();

            //-------------------------------------------------------------------------------------------------
            // try to find standard window
            //-------------------------------------------------------------------------------------------------

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
                    try
                    {
                        if (windowChild.Properties.Name.IsSupported && windowChild.Name.IndexOf(title, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return new DesktopWindow(windowChild, this);
                        }
                    }
                    catch { }
                }
            }

            return null;
        }

        public DesktopWindow GetWindowByHandle(int handle)
        {
            if (handle > 0)
            {
                //AutomationElement window = handles.Find(w => w.Handle == handle).Win;
                foreach(WindowHandle winh in handles.ToArray())
                {
                    if(winh.Handle == handle)
                    {
                        return new DesktopWindow(winh.Win, this);
                    }
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
            handles.FindAll(w => w.Pid == pid).ForEach(e => windowsList.Add(new DesktopWindow(e.Win, this)));

            return windowsList;
        }

        public DesktopWindow getWindowByProcess(Process proc)
        {
            int maxTry = 5;
            DesktopWindow window = null;
            int mainWindowHandle = proc.MainWindowHandle.ToInt32();
            if (mainWindowHandle > 0)
            {
                while (window == null && maxTry > 0)
                {
                    System.Threading.Thread.Sleep(100);
                    window = GetWindowByHandle(mainWindowHandle);
                    maxTry--;
                }
            }
            else
            {
                window = GetWindowPid(proc.ProcessName);
            }
            return window;
        }

        public AutomationElement GetFirstModalPane()
        {
            AutomationElement modalPane = desktop.FindFirstChild(TopModalCondition);
            int maxTry = 20;
            while (modalPane == null && maxTry > 0)
            {
                modalPane = desktop.FindFirstChild(TopModalCondition);
                Thread.Sleep(200);
                maxTry--;
            }
            return modalPane;
        }

        public Queue<AtsElement> GetElements(string tag, string[] attributes)
        {
            return DesktopElement.GetElements(tag, attributes, desktop, this);
        }

        public Stack<AutomationElement> GetPopupDescendants(int pid)
        {
            Stack<AutomationElement> list = new Stack<AutomationElement>();
            popups.FindAll(p => p.Pid == pid).ForEach(e =>
            {
                foreach (AutomationElement elem in e.GetElements())
                {
                    list.Push(elem);
                }
            });
            return list;
        }

        public AtsElement[] GetPopupListItems()
        {
            PopupHandle item = popups.Find(p => p.GetListItemsLength() > 0);
            if(item != null)
            {
                return item.GetListItems();
            }
            return new AtsElement[0];
        }

        public AutomationElement[] GetPopupListItemElements()
        {
            PopupHandle item = popups.Find(p => p.GetListItemsLength() > 0);
            if (item != null)
            {
                return item.GetListItemElements();
            }
            return new AutomationElement[0];
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
