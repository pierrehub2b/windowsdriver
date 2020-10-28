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
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;

[DataContract(Name = "com.ats.recorder.VisualAction")]
public class VisualAction
{
    protected readonly List<byte[]> imagesList;

    public VisualAction()
    {
        this.imagesList = new List<byte[]>();
        this.Error = 0;
    }

    public static byte[] GetScreenshot(string uri)
    {
        return GetScreenshotStream(uri);
    }

    public static byte[] GetScreenshotStream(string uriString)
    {
        var uri = new Uri(uriString);
        String hostname = uri.Host;
        int port = uri.Port;

        try
        {
            var client = new TcpClient(hostname, port);

            var dataString = "hires";
            byte[] data = Encoding.ASCII.GetBytes(dataString);

            var headerString = "POST /screenshot HTTP/1.1\r\nUser-Agent: Windows Driver\r\nDate: " + DateTime.Now + "\r\nContent-Type: " + "text/plain" + "\r\nContent-Length: " + data.Length + "\r\n\r\n" + dataString;
            byte[] header = Encoding.ASCII.GetBytes(headerString);

            NetworkStream stream = client.GetStream();
            stream.Write(header, 0, header.Length);

            MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            stream.Close();
            client.Close();

            return ParseStream(memoryStream.ToArray());
        }
        catch (ArgumentNullException e)
        {
            Console.WriteLine("ArgumentNullException: {0}", e);
        }
        catch (SocketException e)
        {
            Console.WriteLine("SocketException: {0}", e);
        }

        return new byte[0];
    }

    private static byte[] ParseStream(byte[] stream)
    {
        string str = Encoding.ASCII.GetString(stream, 0, stream.Length);
        var stringArray = Regex.Split(str, "\r\n\r\n");
        var headerBytes = Encoding.ASCII.GetBytes(stringArray[0] + "\r\n\r\n");

        var screenshot = new byte[stream.Length - headerBytes.Length];
        Array.Copy(stream, headerBytes.Length, screenshot, 0, screenshot.Length);
        return screenshot;
    }

    /* public static byte[] GetScreenshotStream(string uri)
    {
        HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
        httpWebRequest.ContentType = "application/json";
        httpWebRequest.Method = "POST";

        // Set the content length of the string being posted.
        byte[] b = Encoding.ASCII.GetBytes("hires");
        httpWebRequest.ContentLength = b.Length;

        Stream newStream = httpWebRequest.GetRequestStream();

        newStream.Write(b, 0, b.Length);

        byte[] buffer = new byte[4096];
        using (Stream responseStream = httpWebRequest.GetResponse().GetResponseStream())
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                int count = 0;

                do
                {
                    count = responseStream.Read(buffer, 0, buffer.Length);
                    memoryStream.Write(buffer, 0, count);
                } while (count != 0);

                return memoryStream.ToArray();
            }
        }
    } */

    public static Bitmap GetScreenshotImage(string uri)
    {
        Bitmap bmp;
        using (var ms = new MemoryStream(GetScreenshotStream(uri)))
        {
            bmp = new Bitmap(ms);
        }
        return bmp;
    }

    public VisualAction(VisualRecorder recorder, bool stop, string type, int line, string script, long timeLine, string channelName, double[] channelBound, string imageType, PerformanceCounter cpu, PerformanceCounter ram, float netSent, float netReceived) : this()
    {
        this.Type = type;
        this.Line = line;
        this.Script = script;
        this.TimeLine = timeLine;
        this.ChannelName = channelName;
        this.ChannelBound = new TestBound(channelBound);
        this.imagesList.Add(recorder.ScreenCapture(channelBound));
        this.ImageType = imageType;
        this.ImageRef = 0;
        this.Stop = stop;
    }

    public VisualAction(VisualRecorder recorder, bool stop, string type, int line, string script, long timeLine, string channelName, double[] channelBound, string imageType, PerformanceCounter cpu, PerformanceCounter ram, float netSent, float netReceived, string url) : this()
    {
        this.Type = type;
        this.Line = line;
        this.Script = script;
        this.TimeLine = timeLine;
        this.ChannelName = channelName;
        this.imagesList.Add(recorder.ScreenCapture(channelBound, GetScreenshotImage(url)));
        this.ChannelBound = new TestBound(channelBound);
        this.ImageType = imageType;
        this.ImageRef = 0;
        this.Stop = stop;
    }

    public VisualAction(VisualActionSync action) : this()
    {
        this.ChannelBound = action.ChannelBound;
        this.ChannelName = action.ChannelName;
        this.Data = action.Data;
        this.Duration = action.Duration;
        this.Element = action.Element;
        this.Error = action.Error;
        this.ImageRef = action.ImageRef;
        this.imagesList = action.imagesList;
        this.ImageType = action.ImageType;
        this.Index = action.Index;
        this.Line = action.Line;
        this.Script = action.Script;
        this.TimeLine = action.TimeLine;
        this.Type = action.Type;
        this.Value = action.Value;
        this.Stop = action.Stop;
    }

    public virtual void AddImage(VisualRecorder recorder, double[] channelBound, bool isRef)
    {}

    public virtual void AddImage(VisualRecorder recorder, string url, double[] channelBound, bool isRef)
    {}

    [DataMember(Name = "channelName")]
    public string ChannelName;

    [DataMember(Name = "data")]
    public string Data;

    [DataMember(Name = "element")]
    public VisualElement Element;

    [DataMember(Name = "images")]
    public byte[][] Images
    {
        get { return imagesList.ToArray(); }
        set { }
    }

    [DataMember(Name = "imageType")]
    public string ImageType;

    [DataMember(Name = "index")]
    public int Index;

    [DataMember(Name = "error")]
    public int Error;

    [DataMember(Name = "stop")]
    public bool Stop;

    [DataMember(Name = "duration")]
    public long Duration;

    [DataMember(Name = "line")]
    public int Line;

    [DataMember(Name = "script")]
    public string Script;

    [DataMember(Name = "timeLine")]
    public long TimeLine;

    [DataMember(Name = "type")]
    public string Type;

    [DataMember(Name = "value")]
    public string Value;

    [DataMember(Name = "channelBound")]
    public TestBound ChannelBound;

    [DataMember(Name = "imageRef")]
    public int ImageRef;
}