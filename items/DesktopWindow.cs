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
using System;
using System.Runtime.Serialization;
using System.Threading;

[DataContract(Name = "com.ats.executor.drivers.desktop.DesktopWindow")]
public class DesktopWindow : AtsElement
{
    [DataMember(Name = "pid")]
    public int Pid;

    [DataMember(Name = "handle")]
    public int Handle;

    public readonly bool isIE = false;

    private const string MAXIMIZE = "maximize";
    private const string REDUCE = "reduce";
    private const string RESTORE = "restore";
    private const string CLOSE = "close";

    private readonly bool canMove = false;
    private readonly bool canResize = false;
    protected bool isWindow = false;
    
    private bool isMaximized = false;

    public DesktopWindow(AutomationElement elem) : base(elem, "Window")
    {
        elem.Properties.ProcessId.TryGetValue(out Pid);

        elem.Properties.NativeWindowHandle.TryGetValue(out IntPtr handle);
        Handle = handle.ToInt32();

        if (elem.Patterns.Transform.IsSupported)
        {
            canMove = elem.Patterns.Transform.Pattern.CanMove;
            canResize = elem.Patterns.Transform.Pattern.CanResize;
        }

        if (elem.Patterns.Window.IsSupported)
        {
            isWindow = true;
            elem.Properties.ClassName.TryGetValue(out string className);
            isIE = "IEFrame".Equals(className);
        }

        CachedElement.AddCachedElement(this);
    }

    public virtual void Resize(int w, int h)
    {
        WaitIdle();
        if (canResize)
        {
            Element.Patterns.Transform.Pattern.Resize(w, h);
        }
    }

    public virtual void Move(int x, int y)
    {
        WaitIdle();
        if (canMove)
        {
            Element.Patterns.Transform.Pattern.Move(x, y);
        }
    }

    public virtual void Close()
    {
        if (isWindow)
        {
            if (isIE)
            {
                AutomationElement[] tabs = Element.FindAll(TreeScope.Descendants, Element.ConditionFactory.ByControlType(ControlType.TabItem));
                if (tabs.Length > 1)
                {
                    for (int i = tabs.Length - 1; i > 0; i--)
                    {
                        AutomationElement tab = tabs[i];
                        tab.Patterns.SelectionItem.Pattern.Select();
                        
                        int maxTry = 10;
                        while (maxTry > 0)
                        {
                            AutomationElement closeButton = tab.FindFirstChild(tab.ConditionFactory.ByControlType(ControlType.Button));
                            if (closeButton == null)
                            {
                                Thread.Sleep(350);
                                maxTry--;
                            }
                            else { 
                                closeButton.WaitUntilClickable(TimeSpan.FromMilliseconds(350.00));
                                closeButton.AsButton().Invoke();
                                maxTry = 0;
                            }
                        }
                    }
                }
            }
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
}