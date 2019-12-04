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
using System.Collections.Generic;
using System.IO;
using System.Net;

class WindowExecution : AtsExecution
{
    private static readonly int errorCode = -7;

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
        State = 10
    };

    private readonly DesktopWindow window;
    private readonly ActionKeyboard keyboard;

    private readonly int pid = -1;
    private readonly int[] bounds;
    private readonly string keys;
    private readonly string state;

    private readonly string folderPath;

    public WindowExecution(int type, string[] commandsData, ActionKeyboard keyboard, VisualRecorder recorder) : base()
    {
        this.type = (WindowType)type;
        this.keyboard = keyboard;

        if (this.type == WindowType.Close || this.type == WindowType.Handle || this.type == WindowType.State || this.type == WindowType.Keys)
        {
            int.TryParse(commandsData[0], out int handle);
            window = DesktopWindow.GetWindowByHandle(handle);

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
                int.TryParse(commandsData[0], out int handle);
                int.TryParse(commandsData[1], out pid);

                window = DesktopWindow.GetWindowByHandle(handle);
                recorder.CurrentPid = pid;
            }
            else
            {
                int.TryParse(commandsData[0], out pid);
                recorder.CurrentPid = pid;

                List<DesktopWindow> windows = DesktopWindow.GetOrderedWindowsByPid(pid);
                if (windows.Count > 0)
                {
                    window = windows[0];
                }
            }
        }
        else if (this.type == WindowType.Url)
        {
            int.TryParse(commandsData[0], out int handle);
            window = DesktopWindow.GetWindowByHandle(handle);

            string fname = Environment.ExpandEnvironmentVariables(commandsData[1]);

            try
            {
                if (Directory.Exists(@fname))
                {
                    folderPath = Path.GetFullPath(@fname);
                }
                else
                {
                    response.setError(errorCode, "directory not found : " + fname);
                }
            }
            catch (Exception)
            {
                response.setError(errorCode, "directory path not valid : " + fname);
            }

        }
        else if (this.type == WindowType.Title)
        {
            window = DesktopWindow.GetWindowPid(commandsData[0]);
        }
        else if (this.type == WindowType.List)
        {
            int.TryParse(commandsData[0], out pid);
        }
        else if (this.type == WindowType.Switch)
        {
            int.TryParse(commandsData[0], out int handle);
            window = DesktopWindow.GetWindowByHandle(handle);
        }
        else if (this.type == WindowType.Move || this.type == WindowType.Resize)
        {
            bounds = new int[] { 0, 0 };

            int.TryParse(commandsData[0], out int handle);
            int.TryParse(commandsData[1], out bounds[0]);
            int.TryParse(commandsData[2], out bounds[1]);

            window = DesktopWindow.GetWindowByHandle(handle);
        }
    }

    private DesktopWindow[] GetWindowsList()
    {
        List<DesktopWindow> wins = DesktopWindow.GetOrderedWindowsByPid(pid);

        /*if (ieWindows != null && ieWindows.Count > 0 && ieWindows[0].Pid == pid)
        {
            List<DesktopWindow> reorderedList = new List<DesktopWindow>();
            foreach (WindowRef ieWin in ieWindows)
            {
                DesktopWindow reordered = wins.Find(w => w.Handle == ieWin.Handle.ToInt32());
                if (reordered != null)
                {
                    reorderedList.Add(reordered);
                }
            }
            return reorderedList.ToArray();
        }*/

        return wins.ToArray();
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

                response.Windows = GetWindowsList();
                break;

            case WindowType.Move:

                if (window != null)
                {
                    window.Move(bounds[0], bounds[1]);
                }
                else
                {
                    response.setError(errorCode, "window not found");
                }
                break;

            case WindowType.Resize:

                if (window != null)
                {
                    window.Resize(bounds[0], bounds[1]);
                }
                else
                {
                    response.setError(errorCode, "window not found");
                }
                break;

            case WindowType.Url:

                if (folderPath != null && window != null)
                {
                    window.ToFront();
                    keyboard.AddressBar(folderPath);
                }

                break;

            case WindowType.Switch:

                try
                {
                    window.ToFront();
                    response.Windows = new DesktopWindow[] { window };

                }
                catch (Exception e)
                {
                    response.setError(errorCode, e.Message);
                }
                break;

            case WindowType.ToFront:

                if (window != null)
                {
                    window.ToFront();
                }
                else
                {
                    response.setError(errorCode, "unable to find top window with pid = " + pid);
                }
                break;

            case WindowType.Keys:

                if (window != null)
                {
                    window.ToFront();
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
                    response.setError(errorCode, "window not found");
                }
                break;
        }

        return base.Run(context);
    }
}