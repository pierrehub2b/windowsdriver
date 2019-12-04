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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

public class VisualRecorder
{
    [DllImport("user32.dll", EntryPoint = "GetDesktopWindow")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll", EntryPoint = "GetDC")]
    private static extern IntPtr GetDC(IntPtr ptr);

    [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll", EntryPoint = "SelectObject")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr bmp);

    [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleBitmap")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll", EntryPoint = "BitBlt")]
    private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest, IntPtr hdcSource, int xSrc, int ySrc, int RasterOp);

    [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
    private static extern IntPtr DeleteObject(IntPtr hDc);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteDC(IntPtr hDC);

    private const int SRCCOPY = 0x00CC0020;

    private int frameIndex = 0;
    private BufferedStream visualStream;
    private VisualAction currentAction;

    private ImageCodecInfo animationEncoder;
    private EncoderParameters animationEncoderParameters;

    private readonly ImageCodecInfo maxQualityEncoder;
    private readonly EncoderParameters maxQualityEncoderParameters;

    private string imageType;

    private DataContractAmfSerializer AmfSerializer;

    private DateTime startTime;

    private string AtsvFilePath;

    //private PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
    //private PerformanceCounter ramCounter;

    //private PerformanceCounter networkBytesSent = new PerformanceCounter("Network Interface", "Bytes Sent/sec", true);
    //private PerformanceCounter networkBytesReceived = new PerformanceCounter("Network Interface", "Bytes Received/sec", true);

    private int currentPid = -1;

    public VisualRecorder()
    {
        maxQualityEncoder = VisualRecorder.GetEncoder(ImageFormat.Png);
        maxQualityEncoderParameters = new EncoderParameters(1);
        maxQualityEncoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 100L);
    }

    public int CurrentPid
    {
        get { return currentPid; }
        set
        {
            if (value != currentPid)
            {
                currentPid = value;
            }
        }
    }

    public static ImageCodecInfo GetEncoder(ImageFormat format)
    {
        ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
        foreach (ImageCodecInfo codec in codecs)
        {
            if (codec.FormatID == format.Guid)
            {
                return codec;
            }
        }
        return null;
    }

    public byte[] Capture(int x, int y, int w, int h)
    {
        return Capture(x, y, w, h, maxQualityEncoder, maxQualityEncoderParameters);
    }

    public byte[] Capture(double[] bound)
    {
        return Capture((int)bound[0], (int)bound[1], (int)bound[2], (int)bound[3], animationEncoder, animationEncoderParameters);
    }

    public byte[] Capture(int x, int y, int w, int h, Bitmap img)
    {
        return Capture(x, y, w, h, maxQualityEncoder, maxQualityEncoderParameters, img);
    }

    public byte[] Capture(double[] bound, Bitmap img)
    {
        return Capture((int)bound[0], (int)bound[1], (int)bound[2], (int)bound[3], animationEncoder, animationEncoderParameters, img);
    }

    public byte[] Capture(int x, int y, int w, int h, ImageCodecInfo encoder, EncoderParameters encoderParameters)
    {
        IntPtr hdcSrc = GetDC(GetDesktopWindow());
        IntPtr hdcDest = CreateCompatibleDC(hdcSrc);
        IntPtr hBitmap = CreateCompatibleBitmap(hdcSrc, w, h);

        if (hBitmap != IntPtr.Zero)
        {
            IntPtr hOld = (IntPtr)SelectObject(hdcDest, hBitmap);

            BitBlt(hdcDest, 0, 0, w, h, hdcSrc, x, y, SRCCOPY);
            SelectObject(hdcDest, hOld);

            DeleteDC(hdcDest);

            Bitmap bitmap;
            using (bitmap = Image.FromHbitmap(hBitmap))
            {
                DeleteObject(hBitmap);
                GC.Collect();

                MemoryStream imageStream;
                using (imageStream = new MemoryStream())
                {
                    bitmap.Save(imageStream, encoder, encoderParameters);
                }
                return imageStream.ToArray();
            }
        }
        return null;
    }

    public byte[] Capture(int x, int y, int w, int h, ImageCodecInfo encoder, EncoderParameters encoderParameters, Bitmap img)
    {
        IntPtr hdcSrc = GetDC(GetDesktopWindow());
        IntPtr hdcDest = CreateCompatibleDC(hdcSrc);
        IntPtr hBitmap = CreateCompatibleBitmap(hdcSrc, w, h);

        if (hBitmap != IntPtr.Zero)
        {
            IntPtr hOld = (IntPtr)SelectObject(hdcDest, hBitmap);

            BitBlt(hdcDest, 0, 0, w, h, hdcSrc, x, y, SRCCOPY);
            SelectObject(hdcDest, hOld);

            DeleteDC(hdcDest);

            Bitmap bitmap;
            using (bitmap = img)
            {
                DeleteObject(hBitmap);
                GC.Collect();

                MemoryStream imageStream;
                using (imageStream = new MemoryStream())
                {
                    bitmap.Save(imageStream, encoder, encoderParameters);
                }
                return imageStream.ToArray();
            }
        }
        return null;
    }

    internal void Stop()
    {
        //cpuCounter.Close();
        //ramCounter.Close();

        Flush();

        if (visualStream != null)
        {
            visualStream.Flush();
            visualStream.Close();
        }

        visualStream = null;
        AmfSerializer = null;

        //DeleteDC(hDC);

    }

    internal void Start(string folderPath, string id, string fullName, string description, string author, string groups, string prereq, int videoQuality, string started)
    {
        //hDC = GetDC(GetDesktopWindow());
        //hMemDC = CreateCompatibleDC(hDC);

        frameIndex = -1;
        startTime = DateTime.Now;
        imageType = "jpeg";

        if (videoQuality > 0)
        {
            if (AmfSerializer == null)
            {
                AmfSerializer = new DataContractAmfSerializer(typeof(VisualAction), new[] { typeof(VisualElement), typeof(VisualReport), typeof(TestBound) });
            }

            if (videoQuality == 4) // max quality level
            {
                imageType = "png";
                animationEncoder = maxQualityEncoder;
                animationEncoderParameters = maxQualityEncoderParameters;
            }
            else
            {
                animationEncoder = GetEncoder(ImageFormat.Jpeg);
                animationEncoderParameters = new EncoderParameters(2);

                if (videoQuality == 3) // quality level
                {
                    animationEncoderParameters.Param[1] = new EncoderParameter(Encoder.Quality, 70L);
                    animationEncoderParameters.Param[0] = new EncoderParameter(Encoder.Compression, 70L);
                }
                else if (videoQuality == 2)// speed level
                {
                    animationEncoderParameters.Param[1] = new EncoderParameter(Encoder.Quality, 35L);
                    animationEncoderParameters.Param[0] = new EncoderParameter(Encoder.Compression, 20L);
                }
                else // size level
                {
                    animationEncoderParameters.Param[1] = new EncoderParameter(Encoder.Quality, 10L);
                    animationEncoderParameters.Param[0] = new EncoderParameter(Encoder.Compression, 100L);
                }
            }

            if (visualStream == null)
            {
                AtsvFilePath = folderPath + "\\" + fullName + ".tmp";
                try
                {
                    visualStream = new BufferedStream(new FileStream(AtsvFilePath, FileMode.Create));
                    currentAction = new VisualReport(id, fullName, description, author, groups, prereq, videoQuality, started);
                }
                finally { }
            }
        }
    }

    public string GetDownloadFile()
    {
        if (visualStream != null)
        {
            visualStream.Flush();
            visualStream.Close();
        }

        visualStream = null;
        AmfSerializer = null;

        return AtsvFilePath;
    }

    internal void Create(string actionType, int actionLine, long timeLine, string channelName, double[] channelBound)
    {
        Flush();
        currentAction = new VisualAction(this, actionType, actionLine, timeLine, channelName, channelBound, imageType, null, null, 0.0F, 0.0F);
    }

    internal void CreateMobile(string actionType, int actionLine, long timeLine, string channelName, double[] channelBound, string url)
    {
        Flush();
        currentAction = new VisualAction(this, actionType, actionLine, timeLine, channelName, channelBound, imageType, null, null, 0.0F, 0.0F, url);
    }

    internal void AddImage(double[] screenRect, bool isRef)
    {
        currentAction.AddImage(this, screenRect, isRef);
    }

    internal void AddImage(string url, double[] screenRect, bool isRef)
    {
        currentAction.AddImage(this, url, screenRect, isRef);
    }

    internal void AddValue(string v)
    {
        if (!string.IsNullOrEmpty(v))
        {
            currentAction.Value = v;
        }
    }

    internal void AddData(string v1, string v2)
    {
        AddValue(v1);
        if (!string.IsNullOrEmpty(v2))
        {
            currentAction.Data = v2;
        }
    }

    internal void Status(int error, long duration)
    {
        currentAction.Duration = duration;
        currentAction.Error = error;
    }

    internal void AddElement(double[] bound, long duration, int found, string criterias, string tag)
    {
        currentAction.Element = new VisualElement(tag, criterias, found, bound, duration);
    }

    internal void AddPosition(string hpos, string hposValue, string vpos, string vposValue)
    {
        if (currentAction.Element != null)
        {
            currentAction.Element.UpdatePosition(hpos, hposValue, vpos, vposValue);
        }
    }

    internal void Flush()
    {
        currentAction.Index = frameIndex;
        frameIndex++;

        if (visualStream != null)
        {
            AmfSerializer.WriteObject(visualStream, currentAction);
        }
    }

    //-------------------------------------------------------------------------------------------------------------------

    /*private static string GetInstanceNameForProcessId(int processId)
    {
        var process = Process.GetProcessById(processId);
        string processName = Path.GetFileNameWithoutExtension(process.ProcessName);

        PerformanceCounterCategory cat = new PerformanceCounterCategory("Process");
        string[] instances = cat.GetInstanceNames().Where(inst => inst.StartsWith(processName)).ToArray();

        foreach (string instance in instances)
        {
            using (PerformanceCounter cnt = new PerformanceCounter("Process",
                "ID Process", instance, true))
            {
                int val = (int)cnt.RawValue;
                if (val == processId)
                {
                    return instance;
                }
            }
        }
        return null;
    }

    private string GetInstanceName(int processId)
    {
        string instanceName = Process.GetProcessById(processId).ProcessName;
        bool found = false;
        if (!string.IsNullOrEmpty(instanceName))
        {
            Process[] processes = Process.GetProcessesByName(instanceName);
            if (processes.Length > 0)
            {
                int i = 0;
                foreach (Process p in processes)
                {
                    instanceName = string.Format("{0}#{1}", p.ProcessName, i);
                    if (PerformanceCounterCategory.CounterExists("ID Process", "Process"))
                    {
                        PerformanceCounter counter = new PerformanceCounter("Process", "ID Process", instanceName);

                        if (processId == counter.RawValue)
                        {
                            found = true;
                            break;
                        }
                    }
                    i++;
                }
            }
        }

        if (!found)
            instanceName = string.Empty;

        return instanceName;
    }
    
    private string GetPerformanceCounterProcessName(int pid)
    {
        return GetPerformanceCounterProcessName(pid, Process.GetProcessById(pid).ProcessName);
    }

    private string GetPerformanceCounterProcessName(int pid, string processName)
    {
        int nameIndex = 1;
        string value = processName;
        string counterName = processName + "#" + nameIndex;
        PerformanceCounter pc = new PerformanceCounter("Process", "ID Process", counterName, true);

        while (true)
        {
            try
            {
                if (pid == (int)pc.NextValue())
                {
                    value = counterName;
                    break;
                }
                else
                {
                    nameIndex++;
                    counterName = processName + "#" + nameIndex;
                    pc = new PerformanceCounter("Process", "ID Process", counterName, true);
                }
            }
            catch (SystemException)
            {
                return null;
            }
        }
        return value;
    }*/
}