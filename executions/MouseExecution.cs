﻿/*
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

using System.Net;

class MouseExecution : AtsExecution
{
    private static int errorCode = -5;

    private MouseType type;
    private enum MouseType
    {
        Move = 0,
        Click = 1,
        RightClick = 2,
        MiddleClick = 3,
        DoubleClick = 4,
        Down = 5,
        Release = 6,
        Wheel = 7
    };

    private ActionMouse action;

    private int[] move;
    private int wheelDelta = 0;

    public MouseExecution(int type, string[] commandsData, ActionMouse action) : base()
    {
        this.action = action;
        this.type = (MouseType)type;

        if (commandsData.Length > 1)
        {
            move = new int[] { 0, 0 };
            int.TryParse(commandsData[0], out move[0]);
            int.TryParse(commandsData[1], out move[1]);
        }
        else if (commandsData.Length > 0)
        {
            int.TryParse(commandsData[0], out wheelDelta);
        }
    }

    public override bool Run(HttpListenerContext context)
    {
        switch (type)
        {
            case MouseType.Move:

                if (move != null)
                {
                    action.mouseMove(move[0], move[1]);
                }
                else
                {
                    response.setError(errorCode, "move data command error");
                }
                break;

            case MouseType.Click:

                action.click();
                break;

            case MouseType.DoubleClick:

                action.doubleClick();
                break;

            case MouseType.RightClick:

                action.rightClick();
                break;

            case MouseType.MiddleClick:

                action.middleClick();
                break;

            case MouseType.Down:

                action.down();
                break;

            case MouseType.Release:

                action.release();
                break;

            case MouseType.Wheel:

                action.wheel(wheelDelta);
                break;

            default:
                response.setError(errorCode, "unknown mouse command");
                break;
        }

        return base.Run(context);
    }
}