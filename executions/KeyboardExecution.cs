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

using System.Net;

class KeyboardExecution : AtsExecution
{
    private static readonly int errorCode = -6;
    private readonly KeyType type;

    private enum KeyType
    {
        Clear = 0,
        Enter = 1,
        Down = 2,
        Release = 3
    };

    private readonly ActionKeyboard action;
    private readonly string data;

    public KeyboardExecution(int type, string[] commandsData, ActionKeyboard action) : base()
    {
        this.action = action;
        this.type = (KeyType)type;

        if (commandsData.Length > 0)
        {
            this.data = commandsData[0];
        }
    }

    public override bool Run(HttpListenerContext context)
    {
        if (type == KeyType.Clear)
        {
            action.Clear();
        }
        else if (data != null)
        {
            if (type == KeyType.Enter)
            {
                action.SendKeysData(data);
            }
            else if (type == KeyType.Down)
            {
                action.Down(data);
            }
            else if (type == KeyType.Release)
            {
                action.Release(data);
            }
            else
            {
                response.setError(errorCode, "unknown text command");
            }
        }
        else
        {
            response.setError(errorCode, "enter text data command error");
        }

        return base.Run(context);
    }
}