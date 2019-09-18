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

using System.Runtime.InteropServices;
using System.Threading;

class ActionMouse
{
    [DllImport("user32.dll")]
    private static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

    private const int MOUSEEVENTF_LEFTDOWN = 0x02;
    private const int MOUSEEVENTF_LEFTUP = 0x04;
    private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const int MOUSEEVENTF_RIGHTUP = 0x0010;
    private const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const int MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const int MOUSEEVENTF_WHEEL = 0x0800;
    private const int WM_MOUSEMOVE = 0x200;

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    internal void mouseMove(int x, int y)
    {
        SetCursorPos(x, y);
    }

    internal void click()
    {
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
    }
    internal void middleClick()
    {
        mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0);
        Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);
    }
    internal void rightClick()
    {
        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
        Thread.Sleep(50);
        mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
    }

    internal void doubleClick()
    {
        click();
        Thread.Sleep(150);
        click();
    }

    internal void down()
    {
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
    }

    internal void release()
    {
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
    }

    internal void wheel(int delta)
    {
        mouse_event(MOUSEEVENTF_WHEEL, 0, 0, -delta, 0);
    }
}