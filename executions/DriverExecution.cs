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
using System.Diagnostics;
using System.IO;
using System.Management;
using windowsdriver;

class DriverExecution : AtsExecution
{
    private const int errorCode = -4;

    private enum DriverType
    {
        Capabilities = 0,
        Application = 1,
        CloseWindows = 2,
        Close = 3
    };

    public DriverExecution(int t, string[] commandsData, DesktopData[] caps, DesktopManager desktop) : base()
    {
        DriverType type = (DriverType)t;

        if (type == DriverType.Capabilities)
        {
            response.Data = caps;
        }
        else if (type == DriverType.Application)
        {
            if (commandsData.Length > 0)
            {
                string applicationPath = commandsData[0];
                try
                {
                    FileVersionInfo versInfo = FileVersionInfo.GetVersionInfo(applicationPath);

                    response.Data = new DesktopData[3];
                    response.Data[0] = new DesktopData("ApplicationVersion", string.Format("{0}", versInfo.ProductVersion));
                    response.Data[1] = new DesktopData("ApplicationBuildVersion", string.Format("{0}", versInfo.ProductBuildPart));
                    response.Data[2] = new DesktopData("ApplicationName", string.Format("{0}", versInfo.ProductName));

                }
                catch (FileNotFoundException)
                {
                    response.setError(errorCode, "file path is not valid or not found : " + applicationPath);
                }
            }
            else
            {
                response.setError(errorCode, "no application path data");
            }
        }
        else if (type == DriverType.CloseWindows)
        {
            _ = int.TryParse(commandsData[0], out int pid);
            _ = int.TryParse(commandsData[1], out int handle);

            if (pid > 0)
            {
                List<DesktopWindow> wins = desktop.GetOrderedWindowsByPid(pid);
                foreach (DesktopWindow win in wins)
                {
                    win.Close();
                }
            }
            else
            {
                response.setError(errorCode, "pid must be greater than 0");
            }

            DesktopWindow winapp = desktop.GetWindowByHandle(handle);
            if(winapp != null)
            {
                winapp.Close();
            }
        }
        else if (type == DriverType.Close)
        {
            response.type = -1;
        }
        else
        {
            response.setError(errorCode, "unknown driver command");
        }
    }

    private void KillProcessAndChildren(int pid)
    {
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
        ManagementObjectCollection moc = searcher.Get();
        foreach (ManagementObject mo in moc)
        {
            KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
        }
        try
        {
            Process proc = Process.GetProcessById(pid);
            proc.Kill();
        }
        catch { }

        searcher.Dispose();
    }
}