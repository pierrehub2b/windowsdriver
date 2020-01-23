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
using System.Runtime.Serialization;
using windowsdriver;
using windowsdriver.items;

struct DesktopRequest
{
    private enum CommandType
    {
        Driver = 0,
        Record = 1,
        Window = 2,
        Element = 3,
        Keyboard = 4,
        Mouse = 5
    };

    private readonly AtsExecution execution;

    public DesktopRequest(int errorCode, bool atsAgent, string message)
    {
        execution = new AtsExecution(errorCode, atsAgent, message);
    }

    public DesktopRequest(int cmdType, int cmdSubType, string[] cmdData, ActionKeyboard keyboard, VisualRecorder recorder, DesktopData[] capabilities, DesktopManager desktop)
    {
        CommandType type = (CommandType)cmdType;

        if (type == CommandType.Driver)
        {
            execution = new DriverExecution(cmdSubType, cmdData, capabilities, desktop);
        }
        else if (type == CommandType.Mouse)
        {
            execution = new MouseExecution(cmdSubType, cmdData);
        }
        else if (type == CommandType.Keyboard)
        {
            execution = new KeyboardExecution(cmdSubType, cmdData, keyboard);
        }
        else if (type == CommandType.Window)
        {
            execution = new WindowExecution(cmdSubType, cmdData, keyboard, recorder, desktop);
        }
        else if (type == CommandType.Record)
        {
            execution = new RecordExecution(cmdSubType, cmdData, recorder);
        }
        else if (type == CommandType.Element)
        {
            execution = new ElementExecution(cmdSubType, cmdData, desktop);
        }
        else
        {
            execution = new AtsExecution(-3, true, "unkown command type");
        }
    }

    public bool Execute(HttpListenerContext context)
    {
        return execution.Run(context);
    }
}

[DataContract(Name = "com.ats.executor.drivers.desktop.DesktopResponse")]
public class DesktopResponse
{
    public int type = 0;
    public string atsvFilePath;

    public DesktopResponse() { }

    public DesktopResponse(int error, bool atsAgent, string message)
    {
        setError(error, message);
        if (!atsAgent)
        {
            type = -2;
        }
    }

    public void setError(int code, string message)
    {
        ErrorCode = code;
        ErrorMessage = message;
    }

    [DataMember(Name = "windows", IsRequired = false)]
    public DesktopWindow[] Windows;

    [DataMember(Name = "image", IsRequired = false)]
    public byte[] Image;

    [DataMember(Name = "elements", IsRequired = false)]
    public AtsElement[] Elements;

    [DataMember(Name = "data", IsRequired = false)]
    public DesktopData[] Data;

    [DataMember(Name = "errorCode", IsRequired = true)]
    public int ErrorCode;

    [DataMember(Name = "errorMessage", IsRequired = false)]
    public string ErrorMessage;
}

[DataContract(Name = "com.ats.executor.TestBound")]
public class TestBound
{
    public TestBound() { }

    public TestBound(double[] elementBound)
    {
        this.X = elementBound[0];
        this.Y = elementBound[1];
        this.Width = elementBound[2];
        this.Height = elementBound[3];
    }

    [DataMember(Name = "height")]
    public double Height;

    [DataMember(Name = "width")]
    public double Width;

    [DataMember(Name = "x")]
    public double X;

    [DataMember(Name = "y")]
    public double Y;
}