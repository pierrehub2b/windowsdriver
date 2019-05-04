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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Automation;
using System.Management;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using Microsoft.Win32;

// State object for reading client data asynchronously
public class StateObject
{
    public Socket workSocket = null;
    public const int BufferSize = 512;
    public byte[] buffer = new byte[BufferSize];
}

public static class ElementProperty
{
    public static readonly Predicate<AutomationProperty> notRequiredProperties = new Predicate<AutomationProperty>(p => p != AutomationElementIdentifiers.ControlTypeProperty && p != AutomationElementIdentifiers.ProcessIdProperty && p != AutomationElementIdentifiers.RuntimeIdProperty && p != AutomationElementIdentifiers.BoundingRectangleProperty);
    private static readonly Regex propertyNameEnd = new Regex("Property$");
    private static readonly Regex controlTypeStart = new Regex("^ControlType.");

    internal static string getSimplePropertyName(string name)
    {
        string prop = propertyNameEnd.Replace(name, string.Empty);
        int dotPos = prop.LastIndexOf(".");
        if (dotPos > -1)
        {
            return prop.Substring(dotPos + 1);
        }
        return prop;
    }

    internal static string getSimpleControlName(string name)
    {
        return controlTypeStart.Replace(name, string.Empty);
    }
}

public static class CachedElement
{
    private static ConcurrentDictionary<string, AtsElement> cached = new ConcurrentDictionary<string, AtsElement>();

    public static AtsElement getCachedElementById(string id)
    {
        AtsElement value = null;
        cached.TryGetValue(id, out value);
        return value;
    }

    public static AtsElement getCachedElement(AutomationElement elem)
    {
        /*string key = AtsElement.getElementId(elem);

        AtsElement found;
        if (cached.TryGetValue(key, out found))
        {
            try
            {
                found.updateVisual(elem);
            }
            catch (ElementNotAvailableException)
            {
                removeCachedElement(found);
                return null;
            }

            return found;
        }
        else
        {
            AtsElement newElement = new AtsElement(elem);
            cached.TryAdd(key, newElement);
            return newElement;
        }*/


        AtsElement newElement = new AtsElement(elem);
        cached.TryAdd(newElement.Id, newElement);
        return newElement;

    }

    public static DesktopWindow getCachedWindow(AutomationElement elem)
    {
        DesktopWindow window = new DesktopWindow(elem);
        cached.TryAdd(window.Id, window);
        return window;
    }

    public static void removeCachedElement(AtsElement element)
    {
        cached.TryRemove(element.Id, out element);
    }

    public static void clearCachedElements()
    {
        foreach (var kvp in cached)
        {
            AtsElement elem = null;
            if (cached.TryRemove(kvp.Key, out elem))
            {
                elem.dispose();
            }
        }
    }
}

public class DesktopDriver
{
    public const int DefaultPort = 9988;
    private static DesktopData[] capabilities = GetCapabilities();

    private static ManualResetEvent allDone = new ManualResetEvent(false);
    private static ActionKeyboard keyboard = new ActionKeyboard();
    private static ActionMouse mouse = new ActionMouse();
    private static VisualRecorder recorder = new VisualRecorder();

    public static int Main(String[] args)
    {
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
            int t0 = -1;
            int.TryParse(cmdType[0], out t0);

            int t1 = -1;
            int.TryParse(cmdType[1], out t1);

            string postData = "";
            using (var reader = new StreamReader(listener.Request.InputStream, listener.Request.ContentEncoding))
            {
                postData = reader.ReadToEnd();
            }
            req = new DesktopRequest(t0, t1, postData.Split('\n'), mouse, keyboard, recorder, capabilities, ieWindows);
        }
        else
        {
            req = new DesktopRequest(-2, listener.Request.UserAgent.Equals("AtsDesktopDriver"), "wrong number of url parameters");
        }

        return req.execute(listener);
    }

    //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
    // IE specific management
    //-----------------------------------------------------------------------------------------------------------------------------------------------------------------

    private static List<DesktopWindow> ieWindows = new List<DesktopWindow>();
    private static AutomationEventHandler openHandler = new AutomationEventHandler(OnWindowOpen);
    private static void OnWindowOpen(object el, AutomationEventArgs e)
    {
        AutomationElement win;
        try
        {
            win = el as AutomationElement;
        }
        catch (ElementNotAvailableException)
        {
            return;
        }

        if (e.EventId == WindowPattern.WindowOpenedEvent && win != null && "IEFrame".Equals(win.Current.ClassName))
        {
            object winPattern;
            if (win.TryGetCurrentPattern(WindowPattern.Pattern, out winPattern))
            {
                ieWindows.Add(new DesktopWindow(win));
                Automation.AddAutomationEventHandler(WindowPattern.WindowClosedEvent, win, TreeScope.Subtree, (sender2, e2) =>
                {
                    foreach (DesktopWindow ieWin in ieWindows)
                    {
                        if (ieWin.Handle == win.Current.NativeWindowHandle)
                        {
                            ieWindows.Remove(ieWin);
                            break;
                        }
                    }
                });
            }
        }
    }

    //-----------------------------------------------------------------------------------------------------------------------------------------------------------------

    private static DesktopData[] GetCapabilities()
    {
        List<DesktopData> osData = new List<DesktopData>();
        osData.Add(new DesktopData("MachineName", Environment.MachineName));
        osData.Add(new DesktopData("DriverVersion", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()));
        osData.Add(new DesktopData("DotNetVersion", Environment.Version.ToString()));
        osData.Add(new DesktopData("ScreenResolution", Screen.PrimaryScreen.Bounds.Width.ToString() + "x" + Screen.PrimaryScreen.Bounds.Height.ToString()));
        osData.Add(new DesktopData("Version", Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", "").ToString()));

        string driveLetter = Path.GetPathRoot(Environment.CurrentDirectory);
        DriveInfo dinf = new DriveInfo(driveLetter);
        if (dinf.IsReady)
        {
            osData.Add(new DesktopData("DriveLetter", driveLetter));
            osData.Add(new DesktopData("DiskTotalSize", dinf.TotalSize/1024/1024 + " Mo"));
            osData.Add(new DesktopData("DiskFreeSpace", dinf.AvailableFreeSpace/1024/1024 + " Mo"));
        }

        var os = new ManagementObjectSearcher("select * from Win32_OperatingSystem").Get().Cast<ManagementObject>().First();
        osData.Add(new DesktopData("BuildNumber", (string)os["BuildNumber"]));
        osData.Add(new DesktopData("Name", (string)os["Caption"]));
        osData.Add(new DesktopData("CountryCode", (string)os["CountryCode"]));

        var cpu = new ManagementObjectSearcher("select * from Win32_Processor").Get().Cast<ManagementObject>().First();
        osData.Add(new DesktopData("CpuSocket", (string)cpu["SocketDesignation"]));
        osData.Add(new DesktopData("CpuName", (string)cpu["Caption"]));
        osData.Add(new DesktopData("CpuArchitecture", "" + (ushort)cpu["Architecture"]));
        osData.Add(new DesktopData("CpuMaxClockSpeed", (uint)cpu["MaxClockSpeed"] + " Mhz"));
        osData.Add(new DesktopData("CpuCores", "" + (uint)cpu["NumberOfCores"]));

        return osData.ToArray();
    }
}