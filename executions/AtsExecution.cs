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

using DotAmf.Serialization;
using System;
using System.IO;
using System.Net;

class AtsExecution
{
    private static DataContractAmfSerializer AmfSerializer = new DataContractAmfSerializer(typeof(DesktopResponse), new[] { typeof(DesktopData), typeof(AtsElement), typeof(DesktopWindow) });
    protected DesktopResponse response;

    public AtsExecution()
    {
        response = new DesktopResponse();
    }

    public AtsExecution(int error, bool atsAgent, string message)
    {
        response = new DesktopResponse(error, atsAgent, message);
    }
    
    public virtual bool Run(HttpListenerContext context)
    {
        bool serverRun = true;
        if (response.type == 0)
        {
            context.Response.ContentType = "application/x-amf";
            AmfSerializer.WriteObject(context.Response.OutputStream, response);
        }
        else if(response.atsvFilePath != null)
        {
            try
            {
                string fileName = response.atsvFilePath;
                Stream input = new FileStream(fileName, FileMode.Open);

                context.Response.ContentType = System.Net.Mime.MediaTypeNames.Application.Octet;
                context.Response.ContentLength64 = input.Length;
                context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                context.Response.AddHeader("Last-Modified", File.GetLastWriteTime(fileName).ToString("r"));

                byte[] buffer = new byte[1024 * 64];
                int nbytes;
                while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                    context.Response.OutputStream.Write(buffer, 0, nbytes);

                input.Close();
                context.Response.OutputStream.Flush();
                context.Response.StatusCode = (int)HttpStatusCode.OK;

            }
            catch (Exception e) {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.StatusDescription = e.Message;
            }
        }
        else if (response.type == -1)
        {
            serverRun = false;
        }
        else if (response.type == -2)
        {
            context.Response.ContentType = "text/plain";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(response.ErrorMessage);
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
        }
        /*else
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.StatusDescription = "Recorded file path is null !";
        }*/

        context.Response.Close();
        return serverRun;
    }
}