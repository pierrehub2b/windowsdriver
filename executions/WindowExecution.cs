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
using System.Windows.Automation;

class WindowExecution : AtsExecution
{
    private static int errorCode = -7;

    private WindowType type;
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

    private DesktopWindow window;
    private ActionKeyboard keyboard;

    private int pid = -1;
    private int[] bounds;
    private string keys;
    private string state;

    private string folderPath;

    private List<DesktopWindow> ieWindows;

    public WindowExecution(int type, string[] commandsData, ActionKeyboard keyboard, List<DesktopWindow> ieWindows, VisualRecorder recorder) : base()
    {
        int handle = -1;
        this.type = (WindowType)type;
        this.keyboard = keyboard;
        this.ieWindows = ieWindows;

        if (this.type == WindowType.Close || this.type == WindowType.Handle || this.type == WindowType.ToFront || this.type == WindowType.State || this.type == WindowType.Keys)
        {
            int.TryParse(commandsData[0], out handle);
            window = DesktopWindow.getWindowByHandle(handle);

            if (this.type == WindowType.State)
            {
                this.state = commandsData[1];
            }
            else if (this.type == WindowType.Keys)
            {
                this.keys = commandsData[1];
            }
            else if (this.type == WindowType.ToFront)
            {
                int.TryParse(commandsData[1], out pid);
                recorder.CurrentPid = pid;
            }
        }
        else if (this.type == WindowType.Url)
        {
            int.TryParse(commandsData[0], out handle);
            window = DesktopWindow.getWindowByHandle(handle);

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
            catch (Exception) {
                response.setError(errorCode, "directory path not valid : " + fname);
            }

        }
        else if (this.type == WindowType.Title)
        {
            window = DesktopWindow.getWindowPid(commandsData[0]);
        }
        else if (this.type == WindowType.List)
        {
            int.TryParse(commandsData[0], out pid);
        }
        else if (this.type == WindowType.Move || this.type == WindowType.Resize)
        {
            bounds = new int[] { 0, 0 };

            int.TryParse(commandsData[0], out handle);
            int.TryParse(commandsData[1], out bounds[0]);
            int.TryParse(commandsData[2], out bounds[1]);

            window = DesktopWindow.getWindowByHandle(handle);
        }
    }

    public override bool Run(HttpListenerContext context)
    {
        switch (type)
        {
            case WindowType.Close:

                if (window != null)
                {
                    window.close();
                }
                break;

            case WindowType.State:
                if (window != null)
                {
                    window.state(state);
                }
                break;
            case WindowType.List:

                try
                {
                    List<DesktopWindow> wins = DesktopWindow.getOrderedWindowsByPid(pid);

                    if (ieWindows.Count > 0 && ieWindows[0].Pid == pid)
                    {
                        List<DesktopWindow> reorderedList = new List<DesktopWindow>();
                        foreach (DesktopWindow ieWin in ieWindows)
                        {
                            DesktopWindow reordered = wins.Find(w => w.Handle == ieWin.Handle);
                            if (reordered != null)
                            {
                                reorderedList.Add(reordered);
                            }
                        }
                        response.Windows = reorderedList.ToArray();
                    }
                    else
                    {
                        //wins.Reverse();
                        response.Windows = wins.ToArray();
                    }
                    
                }
                catch (Exception e)
                {
                    response.setError(errorCode, e.Message);
                }
                break;

            case WindowType.Move:

                if (window != null)
                {
                    window.move(bounds[0], bounds[1]);
                }
                else
                {
                    response.setError(errorCode, "window not found");
                }
                break;

            case WindowType.Resize:

                if (window != null)
                {
                    window.resize(bounds[0], bounds[1]);
                }
                else
                {
                    response.setError(errorCode, "window not found");
                }
                break;

            case WindowType.Url:

                if (folderPath != null && window != null)
                {
                    try
                    {
                        window.toFront();
                        keyboard.addressBar(folderPath);
                    }
                    catch (ElementNotAvailableException) {
                        response.setError(errorCode, "address bar not found");
                    }
                }
                else
                {
                    //response.setError(errorCode, "directory not found");
                }
                break;

            case WindowType.Switch:

                //not yet implemented
                break;

            case WindowType.ToFront:

                if (window != null)
                {
                    try
                    {
                        window.toFront();
                    }
                    catch (ElementNotAvailableException) { }
                }
                else
                {
                    response.setError(errorCode, "unable to find top window with pid = " + pid);
                }
                break;

            case WindowType.Keys:

                if (window != null)
                {
                    try
                    {
                        window.toFront();
                    }
                    catch (ElementNotAvailableException) { }
                }
                keyboard.rootKeys(keys.ToLower());
                
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