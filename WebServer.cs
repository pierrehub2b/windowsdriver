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

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Threading;
using windowsdriver;

public class WebServer
{
    private const string ATS_USER_AGENT = "AtsDesktopDriver";

    private Boolean isRunning = true;

    private readonly HttpListener listener;

    private readonly ActionKeyboard keyboard = new ActionKeyboard();
    private readonly DesktopManager desktop = new DesktopManager();
    private readonly VisualRecorder recorder = new VisualRecorder();

    private readonly DesktopData[] capabilities;

    public WebServer(int port)
    {
        this.capabilities = GetCapabilities(desktop);
        this.listener = new HttpListener();

        if (!HttpListener.IsSupported)
            throw new NotSupportedException(
                "Needs Windows XP SP2, Server 2003 or later.");

        listener.Prefixes.Add("http://localhost:" + port + "/");
        listener.Start();
    }

    private bool SendResponse(HttpListenerContext listener)
    {
        DesktopRequest req;

        string[] cmdType = listener.Request.RawUrl.Substring(1).Split('/');
        if (cmdType.Length > 1)
        {
            _ = int.TryParse(cmdType[0], out int t0);
            _ = int.TryParse(cmdType[1], out int t1);

            string postData = "";
            using (var reader = new StreamReader(listener.Request.InputStream, listener.Request.ContentEncoding))
            {
                postData = reader.ReadToEnd();
            }
            req = new DesktopRequest(t0, t1, postData.Split('\n'), keyboard, recorder, capabilities, desktop);
        }
        else
        {
            req = new DesktopRequest(-2, listener.Request.UserAgent.Equals(ATS_USER_AGENT), "wrong number of url parameters");
        }

        return req.Execute(listener);
    }
    
    public void Run()
    {
        while (isRunning)
        {
            _ = ThreadPool.QueueUserWorkItem((c) =>
              {
                  var ctx = c as HttpListenerContext;
                  bool atsAgent = ctx.Request.UserAgent.Equals(ATS_USER_AGENT);
                  try
                  {
                      SendResponse(ctx);
                  }
                  catch (Exception e)
                  {
                      DesktopRequest req = new DesktopRequest(-99, atsAgent, "error -> " + e.StackTrace.ToString());
                      isRunning = req.Execute(ctx);
                  }

              }, listener.GetContext());
        }
    }

    public void Stop()
    {
        listener.Stop();
        listener.Close();
    }

    //-----------------------------------------------------------------------------------------------------------------------------------------------------------------

    private static DesktopData[] GetCapabilities(DesktopManager desktop)
    {
        List<DesktopData> osData = new List<DesktopData>
        {
            new DesktopData("MachineName", Environment.MachineName),
            new DesktopData("DriverVersion", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()),
            new DesktopData("DotNetVersion", GetFrameworkVersion().ToString()),
            new DesktopData("ScreenWidth", desktop.DesktopWidth),
            new DesktopData("ScreenHeight", desktop.DesktopHeight),
            new DesktopData("VirtualWidth", desktop.DesktopWidth),
            new DesktopData("VirtualHeight", desktop.DesktopHeight),
            new DesktopData("Version", Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "").ToString())
        };

        string driveLetter = Path.GetPathRoot(Environment.CurrentDirectory);
        DriveInfo dinf = new DriveInfo(driveLetter);
        if (dinf.IsReady)
        {
            osData.Add(new DesktopData("DriveLetter", driveLetter));
            osData.Add(new DesktopData("DiskTotalSize", dinf.TotalSize / 1024 / 1024 + " Mo"));
            osData.Add(new DesktopData("DiskFreeSpace", dinf.AvailableFreeSpace / 1024 / 1024 + " Mo"));
        }

        ManagementObject os = new ManagementObjectSearcher("select * from Win32_OperatingSystem").Get().Cast<ManagementObject>().First();
        osData.Add(new DesktopData("BuildNumber", (string)os["BuildNumber"]));
        osData.Add(new DesktopData("Name", (string)os["Caption"]));
        osData.Add(new DesktopData("CountryCode", (string)os["CountryCode"]));
        os.Dispose();

        ManagementObject cpu = new ManagementObjectSearcher("select * from Win32_Processor").Get().Cast<ManagementObject>().First();
        osData.Add(new DesktopData("CpuSocket", (string)cpu["SocketDesignation"]));
        osData.Add(new DesktopData("CpuName", (string)cpu["Caption"]));
        osData.Add(new DesktopData("CpuArchitecture", "" + (ushort)cpu["Architecture"]));
        osData.Add(new DesktopData("CpuMaxClockSpeed", (uint)cpu["MaxClockSpeed"] + " Mhz"));
        osData.Add(new DesktopData("CpuCores", "" + (uint)cpu["NumberOfCores"]));
        cpu.Dispose();

        return osData.ToArray();
    }

    private static Version GetFrameworkVersion()
    {
        using (RegistryKey ndpKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
        {
            if (ndpKey != null)
            {
                int value = (int)(ndpKey.GetValue("Release") ?? 0);
                if (value >= 528040)
                    return new Version(4, 8, 0);

                if (value >= 461808)
                    return new Version(4, 7, 2);

                if (value >= 461308)
                    return new Version(4, 7, 1);

                if (value >= 460798)
                    return new Version(4, 7, 0);

                if (value >= 394802)
                    return new Version(4, 6, 2);

                if (value >= 394254)
                    return new Version(4, 6, 1);

                if (value >= 393295)
                    return new Version(4, 6, 0);

                if (value >= 379893)
                    return new Version(4, 5, 2);

                if (value >= 378675)
                    return new Version(4, 5, 1);

                if (value >= 378389)
                    return new Version(4, 5, 0);
            }
        }

        return new Version(0, 0, 0);
    }
}