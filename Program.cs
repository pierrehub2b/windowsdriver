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

using FlaUI.Core.AutomationElements.Infrastructure;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using windowsdriver.actions;

// State object for reading client data asynchronously
public class StateObject
{
    public Socket workSocket = null;
    public const int BufferSize = 512;
    public byte[] buffer = new byte[BufferSize];
}

public static class CachedElement
{
    private static readonly ConcurrentDictionary<string, AtsElement> cached = new ConcurrentDictionary<string, AtsElement>();

    public static AtsElement GetCachedElementById(string id)
    {
        cached.TryGetValue(id, out AtsElement value);
        return value;
    }

    public static AtsElement CreateCachedElement(AutomationElement elem)
    {
        AtsElement newElement = new AtsElement(elem);
        cached.TryAdd(newElement.Id, newElement);
        return newElement;
    }

    public static void ClearElements()
    {
        /*foreach (var kvp in cached)
        {
            if (cached.TryRemove(kvp.Key, out AtsElement elem))
            {
                elem.Dispose();
            }
        }*/
    }

    public static void AddCachedElement(AtsElement elem)
    {
        cached.TryAdd(elem.Id, elem);
    }

    public static DesktopWindow GetCachedWindow(AutomationElement elem)
    {
        DesktopWindow window = new DesktopWindow(elem);
        cached.TryAdd(window.Id, window);
        return window;
    }

    /*public static void removeCachedElement(AtsElement element)
    {
        cached.TryRemove(element.Id, out element);
    }*/
}

public class DesktopDriver
{
    public const int DefaultPort = 9988;
    private static readonly DesktopData[] capabilities = GetCapabilities();

    private static readonly ActionKeyboard keyboard = new ActionKeyboard();
    private static readonly VisualRecorder recorder = new VisualRecorder();
    private static readonly ActionIEWindow ie = new ActionIEWindow();

   public static int Main(String[] args)
    {
        UIA3Automation uia3 = new UIA3Automation();
        var eventHandler = uia3.GetDesktop().RegisterStructureChangedEvent(TreeScope.Children, (element, type, arg3) =>
        {
            if (type.Equals(StructureChangeType.ChildAdded) && element.Properties.ClassName.IsSupported && "IEFrame".Equals(element.ClassName))
            {
                ie.AddWindow(element.AsWindow());
            }
        });

        int defaultPort = DefaultPort;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].IndexOf("--port=") == 0)
            {
                String[] dataPort = args[i].Split('=');
                if (dataPort.Length >= 2)
                {
                    int.TryParse(dataPort[1], out defaultPort);
                    if (defaultPort < 1025 || defaultPort > IPEndPoint.MaxPort)
                    {
                        defaultPort = DefaultPort;
                    }
                }
            }
        }

        Console.WriteLine("Starting ATS Windows Desktop Driver on port {0}", defaultPort);
        Console.WriteLine("Only local connections are allowed.");
        new WebServer(defaultPort, SendResponse).Run();

        return 0;
    }

    public static bool SendResponse(HttpListenerContext listener)
    {
        DesktopRequest req = null;

        string[] cmdType = listener.Request.RawUrl.Substring(1).Split('/');
        if (cmdType.Length > 1)
        {
            int.TryParse(cmdType[0], out int t0);
            int.TryParse(cmdType[1], out int t1);

            string postData = "";
            using (var reader = new StreamReader(listener.Request.InputStream, listener.Request.ContentEncoding))
            {
                postData = reader.ReadToEnd();
            }
            req = new DesktopRequest(t0, t1, postData.Split('\n'), keyboard, recorder, capabilities, ie);
        }
        else
        {
            req = new DesktopRequest(-2, listener.Request.UserAgent.Equals("AtsDesktopDriver"), "wrong number of url parameters");
        }

        return req.execute(listener);
    }

    //-----------------------------------------------------------------------------------------------------------------------------------------------------------------

    private static DesktopData[] GetCapabilities()
    {
        List<DesktopData> osData = new List<DesktopData>
        {
            new DesktopData("MachineName", Environment.MachineName),
            new DesktopData("DriverVersion", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()),
            new DesktopData("DotNetVersion", GetFrameworkVersion().ToString()),
            new DesktopData("ScreenResolution", Screen.PrimaryScreen.Bounds.Width.ToString() + "x" + Screen.PrimaryScreen.Bounds.Height.ToString()),
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

        ManagementObject cpu = new ManagementObjectSearcher("select * from Win32_Processor").Get().Cast<ManagementObject>().First();
        osData.Add(new DesktopData("CpuSocket", (string)cpu["SocketDesignation"]));
        osData.Add(new DesktopData("CpuName", (string)cpu["Caption"]));
        osData.Add(new DesktopData("CpuArchitecture", "" + (ushort)cpu["Architecture"]));
        osData.Add(new DesktopData("CpuMaxClockSpeed", (uint)cpu["MaxClockSpeed"] + " Mhz"));
        osData.Add(new DesktopData("CpuCores", "" + (uint)cpu["NumberOfCores"]));

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