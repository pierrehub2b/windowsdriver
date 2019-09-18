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
using System.Net;
using System.Threading;

public class WebServer
{
    public Boolean isRunning = true;

    private readonly HttpListener listener;
    private readonly Func<HttpListenerContext, bool> _responderMethod;


    public WebServer(int port, Func<HttpListenerContext, bool> method)
    {
        this.listener = new HttpListener();

        if (!HttpListener.IsSupported)
            throw new NotSupportedException(
                "Needs Windows XP SP2, Server 2003 or later.");

        listener.Prefixes.Add("http://localhost:" + port + "/");

        _responderMethod = method;
        listener.Start();
    }

    public void Run()
    {
        while (isRunning)
        {
            try
            {
                while (listener.IsListening)
                {
                    ThreadPool.QueueUserWorkItem((c) =>
                    {
                        var ctx = c as HttpListenerContext;
                        bool atsAgent = ctx.Request.UserAgent.Equals("AtsDesktopDriver");
                        try
                        {
                            _responderMethod(ctx);
                        }
                        catch (Exception e)
                        {
                            DesktopRequest req = new DesktopRequest(-99, atsAgent, "error -> " + e.StackTrace.ToString());
                            isRunning = req.execute(ctx);
                        }

                    }, listener.GetContext());
                }
            }
            catch { } // suppress any exceptions
        }
    }

    public void Stop()
    {
        listener.Stop();
        listener.Close();
    }
}