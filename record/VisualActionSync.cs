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

using System.Diagnostics;
using System.Windows.Media;

public class VisualActionSync : VisualAction
{

    public VisualActionSync(VisualRecorder recorder, bool stop, string type, int line, string script, long timeLine, string channelName, double[] channelBound, string imageType, PerformanceCounter cpu, PerformanceCounter ram, float netSent, float netReceived, string url) 
        : base(recorder, stop, type, line, script, timeLine,  channelName,  channelBound,  imageType,  cpu,  ram,  netSent,  netReceived,  url)
    {}

    public VisualActionSync(VisualRecorder recorder, bool stop, string type, int line, string script, long timeLine, string channelName, double[] channelBound, string imageType, PerformanceCounter cpu, PerformanceCounter ram, float netSent, float netReceived)
        : base(recorder, stop, type, line, script, timeLine, channelName, channelBound, imageType, cpu, ram, netSent, netReceived)
    { }

    public override void AddImage(VisualRecorder recorder, double[] channelBound, bool isRef)
    {
        byte[] cap = recorder.ScreenCapture(channelBound);
        if (isRef)
        {
            imagesList.Clear();
        }

        imagesList.Add(cap);
    }

    public override void AddImage(VisualRecorder recorder, string url, double[] channelBound, bool isRef)
    {
        byte[] cap = recorder.ScreenCapture(channelBound, GetScreenshotImage(url));
        if (isRef)
        {
            imagesList.Clear();
        }
        imagesList.Add(cap);
    }
}