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

using FlaUI.Core.AutomationElements.Infrastructure;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading.Tasks;

[DataContract(Name = "com.ats.executor.drivers.desktop.DesktopWindow")]
public class DesktopWindow : AtsElement
{
    [DataMember(Name = "pid")]
    public int Pid;

    [DataMember(Name = "handle")]
    public int Handle;

    private const string MAXIMIZE = "maximize";
    private const string REDUCE = "reduce";
    private const string RESTORE = "restore";
    private const string CLOSE = "close";

    private readonly bool canMove = false;
    private readonly bool canResize = false;
    private readonly bool isWindow = false;

    private bool isMaximized = false;

    public DesktopWindow(AutomationElement elem) : base(elem, "Window")
    {
        elem.Properties.ProcessId.TryGetValue(out int pid);
        this.Pid = pid;

        elem.Properties.NativeWindowHandle.TryGetValue(out IntPtr handle);
        this.Handle = handle.ToInt32();

        if (elem.Patterns.Transform.IsSupported)
        {
            this.canMove = elem.Patterns.Transform.Pattern.CanMove;
            this.canResize = elem.Patterns.Transform.Pattern.CanResize;
        }

        this.isWindow = elem.Patterns.Window.IsSupported;

        CachedElement.AddCachedElement(this);
    }

    internal void Resize(int w, int h)
    {
        WaitIdle();
        if (canResize)
        {
            Element.Patterns.Transform.Pattern.Resize(w, h);
        }
    }

    internal void Move(int x, int y)
    {
        WaitIdle();
        if (canMove)
        {
            Element.Patterns.Transform.Pattern.Move(x, y);
        }
    }

    internal void Close()
    {
        if (isWindow)
        {
            Element.AsWindow().Close();
        }
        Dispose();
    }

    internal void WaitIdle()
    {
        //TODO if needed
    }

    internal new void Focus()
    {
        Element.SetForeground();
        base.Focus();
    }

    internal void ToFront()
    {
        if (isWindow)
        {
            double w = Element.AsWindow().ActualWidth;
            double h = Element.AsWindow().ActualHeight;
                       
            if (isMaximized)
            {
                Element.Patterns.Window.Pattern.SetWindowVisualState(WindowVisualState.Maximized);
            }
            else
            {
                Element.Patterns.Window.Pattern.SetWindowVisualState(WindowVisualState.Normal);
            }

            if (Element.AsWindow().ActualWidth != w || Element.AsWindow().ActualHeight != h)
            {
                Resize(Convert.ToInt32(w), Convert.ToInt32(h));
            }

            Element.AsWindow().SetForeground();
            Element.AsWindow().Focus();
            Element.AsWindow().FocusNative();
        }
    }

    internal void ChangeState(string value)
    {
        if (isWindow)
        {
            switch (value)
            {
                case MAXIMIZE:
                    if (Element.Patterns.Window.Pattern.CanMaximize)
                    {
                        Element.Patterns.Window.Pattern.SetWindowVisualState(WindowVisualState.Maximized);
                        isMaximized = true;
                    }
                    break;
                case REDUCE:
                    if (Element.Patterns.Window.Pattern.CanMinimize)
                    {
                        Element.Patterns.Window.Pattern.SetWindowVisualState(WindowVisualState.Minimized);
                        isMaximized = false;
                    }
                    break;
                case RESTORE:
                    Element.Patterns.Window.Pattern.SetWindowVisualState(WindowVisualState.Normal);
                    isMaximized = false;
                    break;
                case CLOSE:
                    Close();
                    break;
            }
        }
    }

    public static List<DesktopWindow> GetOrderedWindowsByPid(int pid)
    {
        List<DesktopWindow> windowsList = new List<DesktopWindow>();

        bool procExists = false;
        Process[] procs = Process.GetProcesses();
        foreach (Process proc in procs)
        {
            if (proc.Id == pid)
            {
                procExists = true;
                break;
            }
        }

        if (procExists)
        {
            UIA3Automation ui3 = new UIA3Automation();
            AutomationElement[] winChildren = ui3.GetDesktop().FindAllChildren(w => w.ByProcessId(pid));

            foreach (AutomationElement win in winChildren)
            {
                if (win.ControlType == ControlType.Window)
                {
                    windowsList.Insert(0, CachedElement.GetCachedWindow(win));
                }
                else if (win.ControlType == ControlType.Pane)
                {
                    windowsList.Add(CachedElement.GetCachedWindow(win));
                }
            }

            ui3.Dispose();
        }

        return windowsList;
    }

    /*public static DesktopWindow getTopWindowByPid(int pid)
    {
        if (pid > 0)
        {
            AutomationElement window = new UIA3Automation().GetDesktop().FindFirstChild(w => w.ByProcessId(pid));
            if(window != null)
            {
                return new DesktopWindow(window);
            }
        }
        return null;
    }*/

    public static DesktopWindow GetWindowByHandle(int handle)
    {
        if (handle > 0)
        {
            UIA3Automation ui3 = new UIA3Automation();
            AutomationElement window = ui3.FromHandle(new IntPtr(handle));
            ui3.Dispose();

            if (window != null)
            {
                return new DesktopWindow(window);
            }
        }
        return null;
    }

    public static DesktopWindow GetWindowPid(string title)
    {
        UIA3Automation uia3 = new UIA3Automation();
        AutomationElement[] windows = uia3.GetDesktop().FindAllChildren();

        uia3.Dispose();

        foreach (AutomationElement window in windows)
        {
            if (window.Properties.Name.IsSupported && window.Name.IndexOf(title, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return CachedElement.GetCachedWindow(window);
            }
        }

        //-------------------------------------------------------------------------------------------------
        // second chance to find the window
        //-------------------------------------------------------------------------------------------------

        foreach (AutomationElement window in windows)
        {
            AutomationElement[] windowChildren = window.FindAllChildren();
            foreach (AutomationElement windowChild in windows)
            {
                if (windowChild.Properties.Name.IsSupported && windowChild.Name.IndexOf(title, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return CachedElement.GetCachedWindow(windowChild);
                }
            }
        }

        return null;
    }
}