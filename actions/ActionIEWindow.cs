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

using System.Collections.Generic;
using FlaUI.Core.AutomationElements;
using windowsdriver.items;

namespace windowsdriver.actions
{
    class ActionIEWindow
    {
        private readonly List<IEWindow> windows = new List<IEWindow>();

        private int currentWindow = 0;

        internal void AddWindow(Window window)
        {
            windows.Add(new IEWindow(window, this));
        }
               
        internal void RemoveWindow(string windowId)
        {
            foreach (IEWindow win in windows)
            {
                if (win.EqualsWindowId(windowId))
                {
                    windows.Remove(win);
                    break;
                }
            }
        }

        internal bool SetWindowToFront(int index)
        {
            if(windows.Count > index)
            {
                windows[index].ToFront();
                currentWindow = index;
                return true;
            }
            return false;
        }

        internal bool CloseWindow()
        {
            if (windows.Count > currentWindow)
            {
                windows[currentWindow].Close();
                return true;
            }
            return false;
        }
    }
}