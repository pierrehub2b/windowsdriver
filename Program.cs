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

using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows.Forms;

public static class DesktopDriver
{
    public static readonly string[] dlls = {"Interop.UIAutomationClient", "FlaUI.Core", "FlaUI.UIA3", "DotAmf"};
    public const int DefaultPort = 9988;

    private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        string assemblyName = new AssemblyName(args.Name).Name;
        if (Array.Exists(dlls, element => element == assemblyName))
        {
            return Assembly.LoadFrom(Path.Combine(Application.StartupPath, assemblyName + ".dll"));
        }
        throw new Exception();
    }

    public static int Main(String[] args)
    {
         AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

         int defaultPort = DefaultPort;
         for (int i = 0; i < args.Length; i++)
         {
             if (args[i].IndexOf("--port=") == 0)
             {
                 String[] dataPort = args[i].Split('=');
                 if (dataPort.Length >= 2)
                 {
                     _ = int.TryParse(dataPort[1], out defaultPort);
                     if (defaultPort < 1025 || defaultPort > IPEndPoint.MaxPort)
                     {
                         defaultPort = DefaultPort;
                     }
                 }
             }
         }

         Console.WriteLine("Starting ATS Windows Desktop Driver {0} on port {1}", Assembly.GetExecutingAssembly().GetName().Version.ToString(), defaultPort);
         Console.WriteLine("Only local connections are allowed.");
         new WebServer(defaultPort).Run();

        return 0;
    }
}