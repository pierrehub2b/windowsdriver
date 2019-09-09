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

using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Windows.Automation;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Windows.Forms;

[DataContract(Name = "com.ats.executor.drivers.desktop.DesktopWindow")]
public class DesktopWindow : AtsElement
{
    [DllImport("User32.dll")]
    private static extern Int32 SetForegroundWindow(int hWnd);

    [DllImport("user32.dll")]
    private static extern int ShowWindow(int hWnd, uint Msg);

    [DllImport("user32.dll")]
    internal static extern bool SendMessage(int hWnd, Int32 msg, Int32 wParam, Int32 lParam);
    static Int32 WM_SYSCOMMAND = 0x0112;
    static Int32 SC_RESTORE = 0xF120;
    
    [DataMember(Name = "pid")]
    public int Pid { get; set; }

    [DataMember(Name = "handle")]
    public int Handle { get; set; }

    private WindowPattern windowPattern;
    private TransformPattern transformPattern;

    private const string MAXIMIZE = "maximize";
    private const string REDUCE = "reduce";
    private const string RESTORE = "restore";
    private const string CLOSE = "close";
    
    public DesktopWindow(AutomationElement elem) : base(elem, "Window")
    {
        try
        {
            Pid = elem.Current.ProcessId;
            Handle = elem.Current.NativeWindowHandle;

            object pattern;
            if (elem.TryGetCurrentPattern(TransformPattern.Pattern, out pattern))
            {
                transformPattern = (TransformPattern)pattern;
            }

            if (elem.TryGetCurrentPattern(WindowPattern.Pattern, out pattern))
            {
                windowPattern = (WindowPattern)pattern;
                try
                {
                    windowPattern.SetWindowVisualState(WindowVisualState.Normal);
                }
                catch (InvalidOperationException){}
            }
            return;
        }
        catch (ElementNotAvailableException) { }

        throw new InvalidOperationException("Element is not a desktop window");
    }

    public override void dispose()
    {
        base.dispose();
        windowPattern = null;
        transformPattern = null;
    }

    internal void resize(int w, int h)
    {
        waitIdle();
        //try
        //{
            if (transformPattern.Current.CanResize)
            {
                transformPattern.Resize(w, h);
            }

        //}
        //catch (InvalidOperationException) { }
    }

    internal void move(int x, int y)
    {
        try
        {
            transformPattern.Move(x, y);
        }
        catch (InvalidOperationException) { }
        waitIdle();
    }

    internal void close()
    {
        try
        {
            windowPattern.Close();
        }
        catch (InvalidOperationException) { }
        catch (ElementNotAvailableException) { }

        dispose();
    }

    internal void waitIdle()
    {
        try
        {
            windowPattern.WaitForInputIdle(5000);
        }
        catch (ArgumentOutOfRangeException) { }
    }

    internal void toFront()
    {
        SetForegroundWindow(Handle);
        SendMessage(Handle, WM_SYSCOMMAND, SC_RESTORE, 0);
        windowPattern.SetWindowVisualState(WindowVisualState.Normal);
    }

    internal void state(string value)
    {
        if (windowPattern != null)
        {
            switch (value)
            {
                case MAXIMIZE:
                    if (windowPattern.Current.CanMaximize && !windowPattern.Current.IsModal)
                    {
                        windowPattern.SetWindowVisualState(WindowVisualState.Maximized);
                    }
                    break;
                case REDUCE:
                    if (windowPattern.Current.CanMinimize && !windowPattern.Current.IsModal)
                    {
                        windowPattern.SetWindowVisualState(WindowVisualState.Minimized);
                    }
                    break;
                case RESTORE:
                    if (!windowPattern.Current.IsModal)
                    {
                        windowPattern.SetWindowVisualState(WindowVisualState.Normal);
                        toFront();
                    }
                    break;
                case CLOSE:
                    close();
                    break;
            }
        }
    }

    //-----------------------------------------------------------------------------------------------------------------------------------------------
        private static List<int> GetChildProcesses(int parentId){

        List<int> result = new List<int>();
        result.Add(parentId);

        var query = "Select * From Win32_Process Where ParentProcessId = " + parentId;

        ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
        ManagementObjectCollection processList = searcher.Get();

        foreach (ManagementBaseObject proc in processList)
        {
            result.Add(Convert.ToInt32(proc.GetPropertyValue("ProcessId")));
        }

        return result;
    }

    public static List<DesktopWindow> getOrderedWindowsByPid(int pid)
    {
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
            List<int> pids = GetChildProcesses(pid);

            List<DesktopWindow> windowsList = new List<DesktopWindow>();
            AutomationElement elementNode = TreeWalker.RawViewWalker.GetFirstChild(AutomationElement.RootElement);

            while (elementNode != null)
            {
                //try
                //{
                    if (pids.IndexOf(elementNode.Current.ProcessId) != -1)
                    {
                        if (elementNode.Current.ControlType == ControlType.Window)
                        {
                            windowsList.Insert(0, CachedElement.getCachedWindow(elementNode));
                        }
                        else if (elementNode.Current.ControlType == ControlType.Pane)
                        {
                            windowsList.Add(CachedElement.getCachedWindow(elementNode));
                        }
                    }
                //}
                //catch (InvalidOperationException e) {
                //    Console.WriteLine(e.Message);
                //}
                //catch (ElementNotAvailableException) { }

                elementNode = TreeWalker.ControlViewWalker.GetNextSibling(elementNode);
            }

            return windowsList;
        }
        else
        {
            throw new Exception(string.Format("Pid {0} does not exists", pid));
        }
    }

    public static DesktopWindow getTopWindowByPid(int pid)
    {
        if (pid > 0)
        {
            AutomationElement elementNode = TreeWalker.RawViewWalker.GetFirstChild(AutomationElement.RootElement);
            while (elementNode != null)
            {
                //try
                //{
                    if (elementNode.Current.ProcessId == pid)
                    {
                        return new DesktopWindow(elementNode);
                    }
                //}
                //catch (ElementNotAvailableException) { }
                //catch (InvalidOperationException) { }

                elementNode = TreeWalker.ControlViewWalker.GetNextSibling(elementNode);
            }
        }
        return null;
    }

    public static DesktopWindow getWindowByHandle(int handle)
    {
        if (handle > 0)
        {
            AutomationElement window = AutomationElement.FromHandle(new IntPtr(handle));
            //AutomationElement window = AutomationElement.RootElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.NativeWindowHandleProperty, handle));

            if (window != null)
            {
               return new DesktopWindow(window);
            }
        }
        return null;
    }

    public static DesktopWindow getWindowPid(string title)
    {
        Condition propCondition = new PropertyCondition(AutomationElement.NameProperty, title, PropertyConditionFlags.IgnoreCase);

        AutomationElement winNode = TreeWalker.RawViewWalker.GetFirstChild(AutomationElement.RootElement);

        while (winNode != null)
        {
            //try
            //{
                if (winNode.Current.Name.IndexOf(title) >= 0)
                {
                    return CachedElement.getCachedWindow(winNode); 
                }
            //}
            //catch (InvalidOperationException) { }
            //catch (ElementNotAvailableException) { }
                        
            AutomationElement textChild = winNode.FindFirst(TreeScope.Element | TreeScope.Children, propCondition);

            if(textChild != null)
            {
                return CachedElement.getCachedWindow(winNode);
            }
                       
            winNode = TreeWalker.ControlViewWalker.GetNextSibling(winNode);
        }
        return null;
    }
}