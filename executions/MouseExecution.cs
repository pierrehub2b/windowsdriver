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

using FlaUI.Core.Input;
using System;
using System.Drawing;
using System.Net;
using windowsdriver;

class MouseExecution : AtsExecution
{
    private const int errorCode = -5;

    private readonly MouseType type;
    private readonly DesktopManager desktop;

    private enum MouseType
    {
        Move = 0,
        Click = 1,
        RightClick = 2,
        MiddleClick = 3,
        DoubleClick = 4,
        Down = 5,
        Release = 6,
        Wheel = 7,
        Drag = 8
    };

    private readonly int data0 = 0;
    private readonly int data1 = 0;

    public MouseExecution(int type, string[] commandsData, DesktopManager desktop) : base()
    {
        this.type = (MouseType)type;
        this.desktop = desktop;

        if (commandsData.Length > 0)
        {
            _ = int.TryParse(commandsData[0], out data0);
            if (commandsData.Length > 1)
            {
                _ = int.TryParse(commandsData[1], out data1);
            }
        }
    }

    public override bool Run(HttpListenerContext context)
    {
        switch (type)
        {
            case MouseType.Move:

                Mouse.Position = new Point(data0, data1);
                break;
                
            case MouseType.Drag:

                int dragOffsetX = 20;
                int dragOffsetY = 10;

                AtsElement current = desktop.GetElementFromPoint(Mouse.Position);
                if(current != null)
                {
                    dragOffsetX = Convert.ToInt32(current.Width / 2);
                    dragOffsetY = Convert.ToInt32(current.Height / 2);
                }
                
                Mouse.Down(MouseButton.Left);
                System.Threading.Thread.Sleep(200);
                Mouse.MoveBy(dragOffsetX, dragOffsetY);
                
                break;

            case MouseType.Click:

                Mouse.Click(MouseButton.Left);
                break;

            case MouseType.DoubleClick:

                Mouse.DoubleClick(MouseButton.Left);
                break;

            case MouseType.RightClick:

                Mouse.Click(MouseButton.Right);
                break;

            case MouseType.MiddleClick:

                Mouse.Click(MouseButton.Middle);
                break;

            case MouseType.Down:

                Mouse.Down(MouseButton.Left);
                break;

            case MouseType.Release:

                Mouse.Up(MouseButton.Left);
                break;

            case MouseType.Wheel:

                Mouse.Scroll(-data0 / 100);
                break;

            default:
                response.setError(errorCode, "unknown mouse command");
                break;
        }

        return base.Run(context);
    }
}