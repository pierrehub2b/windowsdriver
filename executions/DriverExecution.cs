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

using FlaUI.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text.RegularExpressions;
using windowsdriver;
using windowsdriver.utils;
using static System.Management.ManagementObjectCollection;

class DriverExecution : AtsExecution
{
    private const int errorCode = -4;
    private const string UWP_PROTOCOLE = "uwp";
    private const string PROC_PROTOCOLE = "proc";
    private const string PROCESS_PROTOCOLE = "process";

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
                int protocoleSplitIndex = commandsData[0].IndexOf("://");
                if(protocoleSplitIndex > 0)
                {
                    string applicationProtocol = commandsData[0].Substring(0, protocoleSplitIndex).ToLower();
                    string applicationPath = commandsData[0].Substring(protocoleSplitIndex + 3);

                    if (applicationProtocol.Equals(UWP_PROTOCOLE))
                    {
                       string[] uwpUrlData = applicationPath.Split('/');
                        if(uwpUrlData.Length > 1)
                        {
                            string packageName = uwpUrlData[0];
                            string windowName = uwpUrlData[1];

                            if (windowName.Length > 0)
                            {
                                string appId = "App";
                                int exclamPos = packageName.IndexOf("!");
                                if (exclamPos > -1)
                                {
                                    appId = packageName.Substring(exclamPos + 1);
                                    packageName = packageName.Substring(0, exclamPos);
                                }

                                if (!packageName.Contains("_"))
                                {
                                    packageName = UwpApplications.getApplicationId(packageName);
                                }

                                if (packageName != null)
                                {
                                    try
                                    {
                                        /*Process.Start(string.Format("shell:AppsFolder\\{0}_{1}!{2}", groupId, publisherId, appId));
                                        Process uwpProcess = null;
                                        int maxTry = 20;
                                        while (uwpProcess == null && maxTry > 0)
                                        {
                                            System.Threading.Thread.Sleep(300);
                                            uwpProcess = getUwpProcess(groupId, publisherId);
                                            maxTry--;
                                        }*/

                                        Application app = Application.LaunchStoreApp(packageName + "!" + appId);
                                        app.WaitWhileBusy(TimeSpan.FromSeconds(7));
                                        app.WaitWhileMainHandleIsMissing(TimeSpan.FromSeconds(7));

                                        Process uwpProcess = Process.GetProcessById(app.ProcessId);

                                        if (uwpProcess != null)
                                        {
                                            int maxTry = 5;
                                            DesktopWindow window = null;
                                            while (window == null && maxTry > 0)
                                            {
                                                System.Threading.Thread.Sleep(100);
                                                window = desktop.GetWindowPid(windowName);
                                                maxTry--;
                                            }

                                            if (window != null)
                                            {
                                                window.UpdateApplicationData(uwpProcess);
                                                response.Windows = new DesktopWindow[] { window };
                                            }
                                            else
                                            {
                                                response.setError(errorCode, "window with name '" + windowName + "' not found");
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        response.setError(errorCode, "cannot start UWP application : " + e.Message);
                                    }
                                }
                                else
                                {
                                    response.setError(errorCode, "malformed uwp url (package name not found) : " + applicationPath);
                                }
                            }
                            else
                            {
                                response.setError(errorCode, "malformed uwp url (missing window name) : " + applicationPath);
                            }
                        }
                        else
                        {
                            response.setError(errorCode, "malformed uwp url (sould be 'uwp://[UwpApplicationName]/[window name])' : " + applicationPath);
                        }
                    }
                    else if (applicationProtocol.Equals(PROCESS_PROTOCOLE) || applicationProtocol.Equals(PROC_PROTOCOLE))
                    {
                        Process appProcess = null;
                        int maxTry = 20;
                        while (appProcess == null && maxTry > 0)
                        {
                            System.Threading.Thread.Sleep(300);
                            appProcess = getProcessByInfo(applicationPath);
                            maxTry--;
                        }

                        if (appProcess != null)
                        {
                            DesktopWindow window = desktop.getWindowByProcess(appProcess);
                            if (window != null)
                            {
                                window.UpdateApplicationData(appProcess);
                                response.Windows = new DesktopWindow[] { window };
                            }
                            else
                            {
                                response.setError(errorCode, "unable to find window with process : " + appProcess.ProcessName + " (" + appProcess.Id + ")");
                            }
                        }
                        else
                        {
                            response.setError(errorCode, "unable to find process matching : " + applicationPath);
                        }
                    }
                    else
                    {
                        applicationPath = Uri.UnescapeDataString(Regex.Replace(applicationPath, @"^/", ""));
                        if (File.Exists(applicationPath))
                        {
                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            if (commandsData.Length > 1)
                            {
                                int newLen = commandsData.Length - 1;
                                string[] args = new string[newLen];
                                Array.Copy(commandsData, 1, args, 0, newLen);
                                startInfo.Arguments = String.Join(" ", args);
                            }
                            startInfo.FileName = applicationPath;

                            /*Process proc = Process.Start(start);
                            proc.WaitForInputIdle();
                            System.Threading.Thread.Sleep(2000);*/

                            try
                            {
                                Application app = Application.AttachOrLaunch(startInfo);
                                app.WaitWhileBusy(TimeSpan.FromSeconds(7));
                                app.WaitWhileMainHandleIsMissing(TimeSpan.FromSeconds(7));

                                Process appProc = Process.GetProcessById(app.ProcessId);

                                if (!app.HasExited)
                                {
                                    //DesktopWindow window = desktop.getWindowByProcess(appProc);
                                    DesktopWindow window = desktop.getAppMainWindow(app);
                                    if (window != null)
                                    {
                                        window.UpdateApplicationData(appProc);
                                        response.Windows = new DesktopWindow[] { window };
                                    }
                                    else
                                    {
                                        response.setError(errorCode, "unable to find window for application : " + applicationPath);
                                    }
                                }
                                else
                                {
                                    response.setError(errorCode, "the process has exited, you may try another way to start this application (UWP ?)");
                                }
                            }
                            catch (Exception e)
                            {
                                response.setError(errorCode, "cannot start application : " + e.Message);
                            }
                        }
                        else
                        {
                            response.setError(errorCode, "application file not found : " + applicationPath);
                        }
                    }
                }
                else
                {
                    response.setError(errorCode, "malformed application url : " + commandsData[0]);
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
    
    private Process getUwpProcess(string groupId, string publisherId)
    {
        ManagementObjectSearcher searcher = new ManagementObjectSearcher(string.Format("select ProcessID,CommandLine from Win32_Process where CommandLine like '%{0}%{1}%'", groupId, publisherId));
        ManagementObjectEnumerator enu = searcher.Get().GetEnumerator();

        if (enu.MoveNext())
        {
            int processId = Int32.Parse(enu.Current["ProcessID"].ToString());
            return Process.GetProcessById(processId);
        }
        return null;
    }

    private Process getProcessByInfo(string info)
    {
        Regex infoRegex = new Regex(info);

        ManagementObjectSearcher processes = new ManagementObjectSearcher("select ProcessID,Caption,ExecutablePath from Win32_Process");
        foreach (ManagementObject o in processes.Get())
        {
            Object exec = o["ExecutablePath"];
            if (exec != null)
            {
                string procName = Regex.Replace(o["Caption"].ToString(), @".exe$", "");
                string executablePath = exec.ToString();

                if (string.Equals(info, procName, StringComparison.CurrentCultureIgnoreCase) || infoRegex.IsMatch(executablePath))
                {
                    int.TryParse(o["ProcessID"].ToString(), out int procId);
                    return Process.GetProcessById(procId);
                }
            }
        }
        return null;
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