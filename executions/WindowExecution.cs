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
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Threading;
using windowsdriver;
using static System.Management.ManagementObjectCollection;

class WindowExecution : AtsExecution
{
    //-------------------------------------------------
    // com.ats.executor.ActionStatus -> status code
    //-------------------------------------------------
    private const int UNREACHABLE_GOTO_URL = -11;
    private const int WINDOW_NOT_FOUND = -14;
    //-------------------------------------------------

    private readonly WindowType type;
    private enum WindowType
    {
        Title = 0,
        Handle = 1,
        List = 2,
        Move = 3,
        Resize = 4,
        ToFront = 5,
        Switch = 6,
        Close = 7,
        Url = 8,
        Keys = 9,
        State = 10,
        Uwp = 11
    };

    private readonly DesktopWindow window;
    private readonly ActionKeyboard keyboard;
    private readonly DesktopManager desktop;

    private readonly int pid = -1;
    private readonly int[] bounds;
    private readonly string keys;
    private readonly string state;

    public WindowExecution(int type, string[] commandsData, ActionKeyboard keyboard, VisualRecorder recorder, DesktopManager desktop) : base()
    {
        this.type = (WindowType)type;
        this.keyboard = keyboard;
        this.desktop = desktop;

        if (this.type == WindowType.Close || this.type == WindowType.Handle || this.type == WindowType.State || this.type == WindowType.Keys)
        {
            _ = int.TryParse(commandsData[0], out int handle);
            window = desktop.GetWindowByHandle(handle);

            if (this.type == WindowType.State)
            {
                this.state = commandsData[1];
            }
            else if (this.type == WindowType.Keys)
            {
                this.keys = commandsData[1];
            }
        }
        else if (this.type == WindowType.ToFront)
        {
            if (commandsData.Length > 1)
            {
                _ = int.TryParse(commandsData[0], out int handle);
                _ = int.TryParse(commandsData[1], out pid);

                window = desktop.GetWindowByHandle(handle);
                recorder.CurrentPid = pid;
            }
            else
            {
                _ = int.TryParse(commandsData[0], out pid);
                recorder.CurrentPid = pid;

                List<DesktopWindow> windows = desktop.GetOrderedWindowsByPid(pid);
                if (windows.Count > 0)
                {
                    window = windows[0];
                }
            }
        }
        else if (this.type == WindowType.Url)
        {
            _ = int.TryParse(commandsData[0], out int handle);
            window = desktop.GetWindowByHandle(handle);

            if (window.isIE)
            {
                window.ToFront();

                AutomationElement win = window.Element;
                AutomationElement addressBar = win.FindFirst(TreeScope.Descendants, win.ConditionFactory.ByControlType(ControlType.Pane).And(win.ConditionFactory.ByClassName("Address Band Root")));
                if (addressBar != null)
                {
                    addressBar.Focus();
                    AutomationElement edit = addressBar.FindFirstChild(addressBar.ConditionFactory.ByControlType(ControlType.Edit));

                    if(edit != null)
                    {
                        try
                        {
                            edit.Focus();
                            edit.Click();
                            if (edit.Patterns.Value.IsSupported)
                            {
                                edit.Patterns.Value.Pattern.SetValue(commandsData[1]);
                            }
                            //edit.AsTextBox().Enter(commandsData[1]);
                            Keyboard.Type(VirtualKeyShort.ENTER);
                        }
                        catch { }
                    }
                }
            }
            else
            {
                string fname = Environment.ExpandEnvironmentVariables(commandsData[1]);
                try
                {
                    if (Directory.Exists(@fname))
                    {
                        window.ToFront();
                        keyboard.AddressBar(Path.GetFullPath(@fname));
                    }
                    else
                    {
                        response.setError(UNREACHABLE_GOTO_URL, "directory not found : " + fname);
                    }
                }
                catch (Exception)
                {
                    response.setError(UNREACHABLE_GOTO_URL, "directory path not valid : " + fname);
                }
            }
        }
        else if (this.type == WindowType.Uwp)
        {

            ManagementObjectSearcher searcher = new ManagementObjectSearcher(string.Format("select ProcessID,CommandLine from Win32_Process where CommandLine like '%{0}%{1}%'", commandsData[0], commandsData[1]));
            ManagementObjectEnumerator enu = searcher.Get().GetEnumerator();

            if (enu.MoveNext())
            {
                string commandLine = enu.Current["CommandLine"].ToString();
                int processId = Int32.Parse(enu.Current["ProcessID"].ToString());

                Process uwpProcess = Process.GetProcessById(processId);
                ProcessModule module = uwpProcess.MainModule;

                window = desktop.GetWindowPid(commandsData[2]);
                window.Pid = processId;

                //window = desktop.GetWindowByHandle(windowHandle.ToInt32());
            }
        }
        else if (this.type == WindowType.Title)
        {
            if (commandsData[0].Equals("jx")){
                window = desktop.GetJxWindowPid(commandsData[1]);
            }
            else
            {
                window = desktop.GetWindowPid(commandsData[0]);
            }
        }
        else if (this.type == WindowType.List)
        {
            _ = int.TryParse(commandsData[0], out pid);
        }
        else if (this.type == WindowType.Switch)
        {
            if (commandsData.Length > 1)
            {
                _ = int.TryParse(commandsData[0], out int pid);
                _ = int.TryParse(commandsData[1], out int index);
                window = desktop.GetWindowIndexByPid(pid, index);

                if(window == null)
                {
                    _ = int.TryParse(commandsData[2], out int handle);
                    window = desktop.GetWindowByHandle(handle);
                }
            }
            else
            { 
                _ = int.TryParse(commandsData[0], out int handle);
                window = desktop.GetWindowByHandle(handle);
            }

            if (window != null)
            {
                window.ToFront();
                response.Windows = new DesktopWindow[] { window };
            }
            else
            {
                response.setError(WINDOW_NOT_FOUND, "window not found");
            }
        }
        else if (this.type == WindowType.Move || this.type == WindowType.Resize)
        {
            bounds = new int[] { 0, 0 };

            _ = int.TryParse(commandsData[0], out int handle);
            _ = int.TryParse(commandsData[1], out bounds[0]);
            _ = int.TryParse(commandsData[2], out bounds[1]);

            window = desktop.GetWindowByHandle(handle);
        }
    }

    public override bool Run(HttpListenerContext context)
    {
        switch (type)
        {
            case WindowType.Close:

                if (window != null)
                {
                    window.Close();
                }
                break;

            case WindowType.State:
                if (window != null)
                {
                    window.ChangeState(state);
                }
                break;
            case WindowType.List:

                response.Windows = desktop.GetOrderedWindowsByPid(pid).ToArray();
                break;

            case WindowType.Move:

                if (window != null)
                {
                    window.Move(bounds[0], bounds[1]);
                }
                else
                {
                    response.setError(WINDOW_NOT_FOUND, "window not found");
                }
                break;

            case WindowType.Resize:

                if (window != null)
                {
                    if(bounds[0] > 0 && bounds[1] > 0)
                    {
                        window.Resize(bounds[0], bounds[1]);
                    }
                }
                else
                {
                    response.setError(WINDOW_NOT_FOUND, "window not found");
                }
                break;

            case WindowType.Url:
                break;
            case WindowType.Switch:
                break;
            case WindowType.ToFront:

                if (window != null)
                {
                    window.ToFront();
                }
                else
                {
                    response.setError(WINDOW_NOT_FOUND, "unable to find top window with pid = " + pid);
                }
                break;

            case WindowType.Keys:

                if (window != null)
                {
                    window.SetMouseFocus();
                    Thread.Sleep(200);
                }
                keyboard.RootKeys(keys.ToLower());

                break;

            default:
                if (window != null)
                {
                    response.Windows = new DesktopWindow[] { window };
                }
                else
                {
                    response.setError(WINDOW_NOT_FOUND, "window not found");
                }
                break;
        }

        return base.Run(context);
    }
}