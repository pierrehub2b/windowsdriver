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

using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.Serialization;

[DataContract(Name = "com.ats.recorder.VisualAction")]
public class VisualAction
{
    private readonly List<byte[]> imagesList;

    public VisualAction()
    {
        this.imagesList = new List<byte[]>();
        this.Error = 0;
    }

    public static byte[] GetScreenshot(string uri)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            GetScreenshotStream(uri).CopyTo(ms);
            return ms.ToArray();
        }
    }

    public static Stream GetScreenshotStream(string uri)
    {
        HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
        httpWebRequest.ContentType = "application/json";
        httpWebRequest.Method = "POST";

        using (StreamWriter writer = new StreamWriter(httpWebRequest.GetRequestStream()))
        {
            writer.WriteLine("screenshot");
            writer.WriteLine("hires");
        }

        WebResponse response = httpWebRequest.GetResponse();

        return response.GetResponseStream();
    }

    public static Bitmap GetScreenshotImage(string uri)
    {
        return new Bitmap(GetScreenshotStream(uri));
    }

    public VisualAction(VisualRecorder recorder, string type, int line, long timeLine, string channelName, double[] channelBound, string imageType, PerformanceCounter cpu, PerformanceCounter ram, float netSent, float netReceived) : this()
    {
        this.Type = type;
        this.Line = line;
        this.TimeLine = timeLine;
        this.ChannelName = channelName;
        this.ChannelBound = new TestBound(channelBound);
        this.imagesList.Add(recorder.ScreenCapture(channelBound));
        this.ImageType = imageType;
        this.ImageRef = 0;
    }

    public VisualAction(VisualRecorder recorder, string type, int line, long timeLine, string channelName, double[] channelBound, string imageType, PerformanceCounter cpu, PerformanceCounter ram, float netSent, float netReceived, string url) : this()
    {
        this.Type = type;
        this.Line = line;
        this.TimeLine = timeLine;
        this.ChannelName = channelName;
        this.imagesList.Add(recorder.ScreenCapture(channelBound, GetScreenshotImage(url)));
        this.ChannelBound = new TestBound(channelBound);
        this.ImageType = imageType;
        this.ImageRef = 0;
    }

    public void AddImage(VisualRecorder recorder, double[] channelBound, bool isRef)
    {
        byte[] cap = recorder.ScreenCapture(channelBound);
        if (isRef)
        {
            imagesList.Clear();
        }

        imagesList.Add(cap);
    }
    
    public void AddImage(VisualRecorder recorder, string url, double[] channelBound, bool isRef)
    {
        if (isRef)
        {
            imagesList.Clear();
        }
        //imagesList.Add(recorder.ScreenCapture(channelBound, GetScreenshot(url, false)));
    }

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

    [DataMember(Name = "duration")]
    public long Duration;

    [DataMember(Name = "line")]
    public int Line;

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

    [DataMember(Name = "cpu")]
    public double Cpu;

    [DataMember(Name = "ram")]
    public double Ram;

    [DataMember(Name = "netSent")]
    public double NetSent;

    [DataMember(Name = "netReceived")]
    public double NetReceived;
}