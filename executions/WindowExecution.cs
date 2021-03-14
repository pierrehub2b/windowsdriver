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
using System.IO;
using System.Net;
using System.Threading;
using windowsdriver;

class WindowExecution : AtsExecution
{
    //-------------------------------------------------
    // com.ats.executor.ActionStatus -> status code
    //-------------------------------------------------
    private const int UNREACHABLE_GOTO_URL = -11;
    private const int WINDOW_NOT_FOUND = -14;
    //-------------------------------------------------

    public enum WindowType
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
        State = 10
    };
    
    private readonly Executor executor;

    public WindowExecution(WindowType type, string[] commandsData, ActionKeyboard keyboard, VisualRecorder recorder, DesktopManager desktop) : base()
    {
        DesktopWindow window = null;

        if (type == WindowType.Close || type == WindowType.Handle || type == WindowType.State || type == WindowType.Keys)
        {
            _ = int.TryParse(commandsData[0], out int handle);
            window = desktop.GetWindowByHandle(handle);
            if (window != null)
            {
                if (type == WindowType.State)
                {
                    executor = new StateExecutor(window, response, commandsData[1]);
                }
                else if (type == WindowType.Keys)
                {
                    executor = new KeysExecutor(window, response, keyboard, commandsData[1]);
                }
                else if (type == WindowType.Close)
                {
                    executor = new CloseExecutor(window, response);
                }
                else if (type == WindowType.Handle)
                {
                    executor = new HandleExecutor(window, response);
                }
            }
        }
        else if (type == WindowType.ToFront)
        {
            if (commandsData.Length > 1)
            {
                _ = int.TryParse(commandsData[0], out int handle);
                _ = int.TryParse(commandsData[1], out int pid);

                window = desktop.GetWindowByHandle(handle);
                recorder.CurrentPid = pid;
            }
            else
            {
                _ = int.TryParse(commandsData[0], out int pid);
                recorder.CurrentPid = pid;

                List<DesktopWindow> windows = desktop.GetOrderedWindowsByPid(pid);
                if (windows.Count > 0)
                {
                    window = windows[0];
                }
            }

            if (window != null)
            {
                window.ToFront();
            }
        }
        else if (type == WindowType.Url)
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
        else if (type == WindowType.Title)
        {
            if (commandsData[0].Equals("jx")){
                window = desktop.GetJxWindowPid(commandsData[1]);
            }
            else
            {
                window = desktop.GetWindowPid(commandsData[0]);
            }
        }
        else if (type == WindowType.List)
        {
            _ = int.TryParse(commandsData[0], out int pid);
            executor = new ListExecutor(window, response, desktop.GetOrderedWindowsByPid(pid).ToArray());
        }
        else if (type == WindowType.Switch)
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
            }
        }
        else if (type == WindowType.Move || type == WindowType.Resize)
        {
            _ = int.TryParse(commandsData[0], out int handle);
            _ = int.TryParse(commandsData[1], out int value1);
            _ = int.TryParse(commandsData[2], out int value2);

            window = desktop.GetWindowByHandle(handle);
            if(window != null)
            {
                if (type == WindowType.Move)
                {
                    executor = new MoveExecutor(window, response, value1, value2);
                }
                else
                {
                    executor = new ResizeExecutor(window, response, value1, value2);
                }
            }
        }

        if(executor == null)
        {
            executor = new EmptyExecutor(window, response);
        }
    }

    private abstract class Executor
    {
        protected readonly DesktopWindow window;
        protected readonly DesktopResponse response;
        
        public Executor(DesktopWindow window, DesktopResponse response)
        {
            this.window = window;
            this.response = response;
        }
        public abstract void Run();
    }

    private class EmptyExecutor : Executor
    {
        public EmptyExecutor(DesktopWindow window, DesktopResponse response) : base(window, response) { }
        public override void Run() {
            if (window != null)
            {
                response.Windows = new DesktopWindow[] { window };
            }
            else
            {
                response.setError(WINDOW_NOT_FOUND, "window not found");
            }
        }
    }

    private class HandleExecutor : Executor
    {
        public HandleExecutor(DesktopWindow window, DesktopResponse response) : base(window, response) { }
        public override void Run()
        {
            response.Windows = new DesktopWindow[] { window };
        }
    }

    private class ListExecutor : Executor
    {
        protected DesktopWindow[] windows;

        public ListExecutor(DesktopWindow window, DesktopResponse response, DesktopWindow[] windows) : base(window, response)
        {
            this.windows = windows;
        }

        public override void Run()
        {
            response.Windows = windows;
        }
    }

    private class MoveExecutor : Executor
    {
        protected int x = 0;
        protected int y = 0;

        public MoveExecutor(DesktopWindow window, DesktopResponse response, int value1, int value2) : base(window, response)
        {
            x = value1;
            y = value2;
        }

        public override void Run()
        {
            window.Move(x, y);
        }
    }

    private class ResizeExecutor : Executor
    {
        protected int w = 0;
        protected int h = 0;

        public ResizeExecutor(DesktopWindow window, DesktopResponse response, int value1, int value2) : base(window, response)
        {
            w = value1;
            h = value2;
        }

        public override void Run()
        {
            if (w > 0 && h > 0)
            {
                window.Resize(w, h);
            }
        }
    }

    private class StateExecutor : Executor
    {
        protected string state;

        public StateExecutor(DesktopWindow window, DesktopResponse response, string state) : base(window, response)
        {
            this.state = state;
        }

        public override void Run()
        {
            window.ChangeState(state);
            Dispose();
        }

        public void Dispose()
        {
            state = null;
        }
    }
    private class CloseExecutor : Executor
    {
        public CloseExecutor(DesktopWindow window, DesktopResponse response) : base(window, response) { }
        public override void Run()
        {
            window.Close();
        }
    }

    private class KeysExecutor : Executor
    {
        protected ActionKeyboard keyboard;
        protected string keys;

        public KeysExecutor(DesktopWindow window, DesktopResponse response, ActionKeyboard keyboard, string keys) : base(window, response)
        {
            this.keyboard = keyboard;
            this.keys = keys;
        }

        public override void Run()
        {
            if (window != null)
            {
                window.SetMouseFocus();
                Thread.Sleep(200);
            }
            keyboard.RootKeys(keys);

            Dispose();
        }

        public void Dispose()
        {
            keyboard = null;
            keys = null;
        }
    }

    public override bool Run(HttpListenerContext context)
    {
        executor.Run();
        return base.Run(context);
    }
}