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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using windowsdriver;

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

    protected bool isWindow = false;
    
    private bool isMaximized = false;

    private DesktopManager desktop;

    public DesktopWindow(AutomationElement elem, Rectangle deskRect) : base(elem)
    {
        Tag = "Desktop";
    }

    public DesktopWindow(AutomationElement elem, DesktopManager desktop) : base(elem)
    {
        Tag = "Window";
        this.desktop = desktop;

        elem.Properties.ProcessId.TryGetValue(out Pid);

        elem.Properties.NativeWindowHandle.TryGetValue(out IntPtr handle);
        Handle = handle.ToInt32();

        if (elem.Patterns.Window.IsSupported)
        {
            isWindow = true;
            elem.Properties.ClassName.TryGetValue(out string className);
            isIE = "IEFrame".Equals(className);
        }
    }

    private bool CanMoveResize()
    {
        try
        {
            return Element.Patterns.Transform.IsSupported && Element.Patterns.Transform.Pattern.CanMove && Element.Patterns.Transform.Pattern.CanResize;
        }
        catch { }

        return false;
    }
       
    public override AtsElement[] GetElementsTree(DesktopManager desktop)
    {
        Stack<AutomationElement> popupChildren = desktop.GetPopupDescendants(Element.Properties.ProcessId);

        Queue<AtsElement> listElements = new Queue<AtsElement> { };

        //---------------------------------------------------------------------
        // try to find a modal window
        //---------------------------------------------------------------------

        AutomationElement[] children = Element.FindAllChildren(Element.ConditionFactory.ByControlType(ControlType.Window));
        for (int i = 0; i < children.Length; i++)
        {
            AutomationElement child = children[i];
            if (child.Patterns.Window.IsSupported && child.Patterns.Window.Pattern.IsModal)
            {
                foreach (AutomationElement popup in popupChildren)
                {
                    listElements.Enqueue(new AtsElement(desktop, popup));
                }

                listElements.Enqueue(new AtsElement(desktop, child));
                return listElements.ToArray();
            }
        }

        foreach (AutomationElement child in popupChildren.Concat(Element.FindAllChildren(desktop.NotOffScreenProperty)))
        {
            if(child.Properties.ClassName.IsSupported && (child.ClassName.Equals("Intermediate D3D Window") || child.ClassName.Equals("MozillaCompositorWindowClass")))
            { // Main Google Chrome app window or main Firefox app window
                continue;
            }
            listElements.Enqueue(new AtsElement(desktop, child));
        }

        return listElements.ToArray();
    }

    public override Queue<AtsElement> GetElements(string tag, string[] attributes, AutomationElement root, DesktopManager desktop)
    {
        //---------------------------------------------------------------------
        // try to find a modal window
        //---------------------------------------------------------------------

        AutomationElement[] children = Element.FindAllChildren(Element.ConditionFactory.ByControlType(ControlType.Window));
        for (int i = 0; i < children.Length; i++)
        {
            AutomationElement child = children[i];
            if (child.Patterns.Window.IsSupported && child.Patterns.Window.Pattern.IsModal)
            {
                return base.GetElements(tag, attributes, child, desktop);
            }
        }

        return base.GetElements(tag, attributes, Element, desktop, desktop.GetPopupDescendants(Element.Properties.ProcessId));
    }

    public virtual void Resize(int w, int h)
    {
        if(WaitIdle())
        {
            Element.Patterns.Transform.Pattern.Resize(w, h);
        }
    }

    public virtual void Move(int x, int y)
    {
        if (WaitIdle())
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

            try
            {
                CloseModalPopups();
                Element.AsWindow().Close();
                Thread.Sleep(500);

                if (CloseModalPopups())
                {
                    Process[] process = Process.GetProcesses();
                    foreach (Process proc in process)
                    {
                        if (proc.Id == Pid)
                        {
                            proc.Kill();
                            break;
                        }
                    }
                }
            }
            catch (Exception e) {
                var verif = e.Message;
            }
        }

        Dispose();
    }

    private bool CloseModalPopups()
    {
        bool findModal = false;

        AutomationElement[] dialogs = desktop.GetDialogChildren(Pid);
        for (int i = 0; i < dialogs.Length; i++)
        {
            AutomationElement dialog = dialogs[i];
            if (!dialog.Equals(Element))
            {
                dialog.AsWindow().Close();
                findModal = true;
            }
        }
               
        dialogs = Element.FindAllChildren(Element.ConditionFactory.ByControlType(ControlType.Window));
        for (int i = 0; i < dialogs.Length; i++)
        {
            AutomationElement dialog = dialogs[i];
            if (dialog.Patterns.Window.IsSupported && dialog.Patterns.Window.Pattern.IsModal)
            {
                dialog.AsWindow().Close();
                findModal = true;
            }
        }

        return findModal;
    }

    internal bool WaitIdle()
    {
        //TODO if needed
        return CanMoveResize();
    }

    public override void Focus()
    {
        Element.SetForeground();
        base.Focus();
    }

    public virtual void ToFront()
    {
        if (isWindow)
        {
            if (!HasModalChild() && Element.Patterns.Window.IsSupported && Element.Patterns.Window.Pattern.WindowVisualState.IsSupported)
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
            }
            Element.AsWindow().SetForeground();
            Element.AsWindow().Focus();
            Element.AsWindow().FocusNative();
        }
    }

    public virtual void SetMouseFocus()
    {
        ToFront();
        Element.Click();
    }

    private bool HasModalChild()
    {
        AutomationElement[] children = Element.FindAllChildren(Element.ConditionFactory.ByControlType(ControlType.Window));
        for (int i = 0; i < children.Length; i++)
        {
            AutomationElement child = children[i];
            if (child.Patterns.Window.IsSupported && child.Patterns.Window.Pattern.IsModal)
            {
                return true;
            }
        }
        return false;
    }

    public virtual void ChangeState(string value)
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