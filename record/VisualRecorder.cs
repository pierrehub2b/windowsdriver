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

using DotAmf.Data;
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
    private static extern bool DeleteDC(IntPtr hDC);

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

    private ReportSummary summary;

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

    public byte[] ScreenCapture(int x, int y, int w, int h, Bitmap img)
    {
        return ScreenCapture(x, y, w, h, maxQualityEncoder, maxQualityEncoderParameters, img);
    }

    public byte[] ScreenCapture(int x, int y, int w, int h)
    {
        return ScreenCapture(x, y, w, h, maxQualityEncoder, maxQualityEncoderParameters);
    }

    public byte[] ScreenCapture(double[] bound)
    {
        return ScreenCapture((int)bound[0], (int)bound[1], (int)bound[2], (int)bound[3], animationEncoder, animationEncoderParameters);
    }

    public byte[] ScreenCapture(TestBound bounds)
    {
        return ScreenCapture((int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height, animationEncoder, animationEncoderParameters);
    }

    public byte[] ScreenCapture(double[] bound, Bitmap img)
    {
        return ScreenCapture((int)bound[0], (int)bound[1], (int)bound[2], (int)bound[3], animationEncoder, animationEncoderParameters, img);
    }

    public static byte[] ScreenCapture(int x, int y, int w, int h, ImageCodecInfo encoder, EncoderParameters encoderParameters)
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

    public static byte[] ScreenCapture(int x, int y, int w, int h, ImageCodecInfo encoder, EncoderParameters encoderParameters, Bitmap img)
    {
        IntPtr hdcSrc = GetDC(GetDesktopWindow());
        IntPtr hdcDest = CreateCompatibleDC(hdcSrc);
        IntPtr hBitmap = CreateCompatibleBitmap(hdcSrc, w, h);

        Bitmap bitmap;
        var cropedImg = img.Clone(new Rectangle(x, y, w, h), img.PixelFormat);
        using (bitmap = cropedImg)
        {
            DeleteObject(hBitmap);
            GC.Collect();

            MemoryStream imageStream;
            using (imageStream = new MemoryStream())
            {
                bitmap.Save(imageStream, encoder, encoderParameters);
                return imageStream.ToArray();
            }
        }
    }

    internal void Stop()
    {
        Flush();

        if (visualStream != null)
        {
            visualStream.Flush();
            
            AmfSerializer.WriteObject(visualStream, summary);
            visualStream.Flush();

            visualStream.Close();
        }

        visualStream = null;
        AmfSerializer = null;
    }

    internal void Start(string folderPath, string id, string fullName, string description, string author, string groups, string prereq, int videoQuality, string started)
    {
        frameIndex = -1;
        startTime = DateTime.Now;
        imageType = "jpeg";

        if (videoQuality > 0)
        {
            if (AmfSerializer == null)
            {
                AmfSerializer = new DataContractAmfSerializer(typeof(VisualAction), new[] {typeof(ReportSummary), typeof(ReportSummaryError), typeof(VisualElement), typeof(VisualReport), typeof(TestBound)});
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
                catch { }
            }
        }
    }

    internal void Summary(bool passed, int actions, string suiteName, string testName,  string data)
    {
        summary = new ReportSummary(passed, actions, suiteName, testName, data);
    }

    internal void Summary(bool passed, int actions, string suiteName, string testName, string data, string errorScript, int errorLine, string errorMessage)
    {
        summary = new ReportSummary(passed, actions, suiteName, testName, data, errorScript, errorLine, errorMessage);
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

    internal void Create(string actionType, int actionLine, long timeLine, string channelName, double[] channelBound, bool sync, bool stop)
    {
        currentAction.AddImage(this, channelBound, false);

        Flush();
        if(sync)
        {
            currentAction = new VisualActionSync(this, stop, actionType, actionLine, timeLine, channelName, channelBound, imageType, null, null, 0.0F, 0.0F);
        } else
        {
            currentAction = new VisualAction(this, stop, actionType, actionLine, timeLine, channelName, channelBound, imageType, null, null, 0.0F, 0.0F);
        }
        
    }

    internal void CreateMobile(string actionType, int actionLine, long timeLine, string channelName, double[] channelBound, string url, bool sync, bool stop)
    {
        currentAction.AddImage(this, url, channelBound, false);

        Flush(); 
        if(sync) {
            currentAction = new VisualActionSync(this, stop, actionType, actionLine, timeLine, channelName, channelBound, imageType, null, null, 0.0F, 0.0F, url);
        } else
        {
            currentAction = new VisualAction(this, stop, actionType, actionLine, timeLine, channelName, channelBound, imageType, null, null, 0.0F, 0.0F, url);
        }
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
            if(currentAction is VisualActionSync)
            {
                currentAction = new VisualAction(currentAction as VisualActionSync);
            }
            AmfSerializer.WriteObject(visualStream, currentAction);
            visualStream.Flush();
        }
    }
}