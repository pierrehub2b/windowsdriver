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
using FlaUI.Core.EventHandlers;
using System.Collections.Generic;

namespace windowsdriver.desktop
{
    class WindowHandle
    {

        //private static readonly int WinCloseEventId = System.Windows.Automation.WindowPattern.WindowClosedEvent.Id;
        private const int WinCloseEventId = 20017;

        public int Pid;
        public int Handle;
        public AutomationElement Win;

        public WindowHandle(int pid, AutomationElement win, List<WindowHandle> list)
        {
            Pid = pid;
            Win = win;
            Handle = win.Properties.NativeWindowHandle.Value.ToInt32();

            AutomationEventHandlerBase closeEvent = null;
            closeEvent = win.RegisterAutomationEvent(new FlaUI.Core.Identifiers.EventId(WinCloseEventId, "WindowClosedEvent"), TreeScope.Element, (removed, evType) =>
            {
                if (!removed.IsAvailable)
                {
                    list.Remove(this);
                    Win = null;
                    try
                    {
                        closeEvent.Dispose();
                    }
                    catch { }
                }
            });
        }
    }
}