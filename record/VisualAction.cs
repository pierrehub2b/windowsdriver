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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;

[DataContract(Name = "com.ats.recorder.VisualAction")]
public class VisualAction
{
    private readonly List<byte[]> imagesList;
    private int mobileWidth = 1;
    private int mobileHeight = 1;

    public VisualAction()
    {
        this.imagesList = new List<byte[]>();
        this.Error = 0;
    }

    public byte[] GetScreenshot(string uri)
    {
        HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
        httpWebRequest.ContentType = "application/json";
        httpWebRequest.Method = "POST";

        using (StreamWriter writer = new StreamWriter(httpWebRequest.GetRequestStream()))
        {
            writer.WriteLine("screenshot");
        }


        WebResponse response = httpWebRequest.GetResponse();
        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
        {
            var dataReceived = reader.ReadToEnd();
            string s = JObject.Parse(dataReceived)["imgdata"].ToString();
            byte[] bytes = Convert.FromBase64String(s);
            using (var streamImg = new MemoryStream(bytes, 0, bytes.Length))
            {
                Image image = Image.FromStream(streamImg);
                mobileWidth = image.Width;
                mobileHeight = image.Height;
                return ImageToByteArray(image);
            }
        }
    }

    public byte[] ImageToByteArray(Image image)
    {
        using (var ms = new MemoryStream())
        {
            image.Save(ms, image.RawFormat);
            return ms.ToArray();
        }
    }


    public VisualAction(VisualRecorder recorder, string type, int line, long timeLine, string channelName, double[] channelBound, string imageType, PerformanceCounter cpu, PerformanceCounter ram, float netSent, float netReceived) : this()
    {
        this.Type = type;
        this.Line = line;
        this.TimeLine = timeLine;
        this.ChannelName = channelName;
        this.ChannelBound = new TestBound(channelBound);
        this.imagesList.Add(recorder.Capture(channelBound));
        this.ImageType = imageType;
        this.ImageRef = 0;
        /*try
        {
            this.Cpu = (int)cpu.NextValue() / Environment.ProcessorCount;
            this.Ram = ram.RawValue / 1024;
            this.NetSent = Convert.ToDouble(netSent);
            this.NetReceived = Convert.ToDouble(netReceived);
        }
        catch { }*/

    }

    public VisualAction(string type, int line, long timeLine, string channelName, double[] channelBound, string imageType, PerformanceCounter cpu, PerformanceCounter ram, float netSent, float netReceived, string url) : this()
    {
        this.Type = type;
        this.Line = line;
        this.TimeLine = timeLine;
        this.ChannelName = channelName;
        this.imagesList.Add(GetScreenshot(url));

        double ratioWidth = channelBound[2] / mobileWidth;
        double ratioHeight = channelBound[3] / mobileHeight;

        channelBound[0] = channelBound[0] * ratioWidth;
        channelBound[1] = channelBound[1] * ratioHeight;
        channelBound[2] = channelBound[2] * ratioWidth;
        channelBound[3] = channelBound[3] * ratioHeight;

        this.ChannelBound = new TestBound(channelBound);
        this.ImageType = imageType;
        this.ImageRef = 0;
        /*try
        {
            this.Cpu = (int)cpu.NextValue() / Environment.ProcessorCount;
            this.Ram = ram.RawValue / 1024;
            this.NetSent = Convert.ToDouble(netSent);
            this.NetReceived = Convert.ToDouble(netReceived);
        }
        catch { }*/

    }

    public void AddImage(VisualRecorder recorder, double[] channelBound, bool isRef)
    {
        byte[] cap = recorder.Capture(channelBound);

        /*if (!Equality(cap, imagesList.Last())) {
            imagesList.Add(cap);
        }*/

        if (isRef)
        {
            imagesList.Clear();
        }

        imagesList.Add(cap);
    }
    public void AddImage(VisualRecorder recorder, string url, bool isRef)
    {
        byte[] data = GetScreenshot(url);
        if (isRef)
        {
            imagesList.Clear();
        }
        imagesList.Add(data);
    }


    public bool Equality(byte[] a1, byte[] b1)
    {
        if (a1.Length != b1.Length)
        {
            return false;
        }

        for (int i = 0; i < a1.Length; i++)
        {
            if (a1[i] != b1[i])
            {
                return false;
            }
        }
        return true;
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