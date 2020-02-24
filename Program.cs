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

using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

public static class DesktopDriver
{
    public const int DefaultPort = 9988;

    public static int Main(String[] args)
    {

        /*AutomationElement[] elements = new UIA3Automation().GetDesktop().FindAllChildren();
        Console.WriteLine("elements -> " + elements.Length);

        List<AtsElement> listElements = new List<AtsElement> { };
        Stopwatch sw = Stopwatch.StartNew();

        List<AtsElement> treeList = new List<AtsElement>();
        for (int i = 0; i < elements.Length; i++)
        {
            treeList.Add(new AtsElement(elements.ElementAt(i)));
        }

        foreach (AtsElement e in treeList)
        {
            e.addToFlatList(listElements);
        }
        
        sw.Stop();

        Console.WriteLine("executed in -> " + sw.ElapsedMilliseconds);
        Console.WriteLine("visible elements -> " + listElements.Count);*/
                              
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

        Console.WriteLine("Starting ATS Windows Desktop Driver on port {0}", defaultPort);
        Console.WriteLine("Only local connections are allowed.");
        new WebServer(defaultPort).Run();

        return 0;
    }
}