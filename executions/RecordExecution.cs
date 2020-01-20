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
using System.Drawing;
using System.IO;

class RecordExecution : AtsExecution
{
    private enum RecordType
    {
        Stop = 0,
        Screenshot = 1,
        Start = 2,
        Create = 3,
        Image = 4,
        Value = 5,
        Data = 6,
        Status = 7,
        Element = 8,
        Position = 9,
        Download = 10,
        ImageMobile = 11,
        CreateMobile = 12,
        ScreenshotMobile = 13
    };

    public RecordExecution(int type, string[] commandsData, VisualRecorder recorder) : base()
    {
        RecordType recordType = (RecordType)type;


        if (recordType == RecordType.Stop)
        {
            recorder.Stop();
        }
        else if (recordType == RecordType.Download)
        {
            response.type = 1;
            response.atsvFilePath = recorder.GetDownloadFile();
        }

        if (commandsData.Length > 0)
        {
            if (recordType == RecordType.Screenshot)
            {
                if(int.TryParse(commandsData[0], out int x) &&
                int.TryParse(commandsData[1], out int y) &&
                int.TryParse(commandsData[2], out int w) &&
                int.TryParse(commandsData[3], out int h))
                {
                    response.Image = recorder.ScreenCapture(x, y, w, h);
                }
            }
            else if (recordType == RecordType.ScreenshotMobile)
            {
                response.Image = VisualAction.GetScreenshot(commandsData[0]);
            }
            else if (recordType == RecordType.Start)
            {
                string tempFolder = Path.GetTempPath() + "\\ats_recorder";
                long freeSpace = 0;
                try
                {
                    DriveInfo drive = new DriveInfo(new FileInfo(tempFolder).Directory.Root.FullName);
                    freeSpace = drive.AvailableFreeSpace;
                }
                catch { }

                if (freeSpace > 100000000)
                {
                    try
                    {
                        if (Directory.Exists(tempFolder))
                        {
                            DirectoryInfo folder = new DirectoryInfo(tempFolder);
                            foreach (FileInfo file in folder.EnumerateFiles())
                            {
                                file.Delete();
                            }
                        }
                        else
                        {
                            Directory.CreateDirectory(tempFolder);
                        }
                    }
                    catch { }


                    string id = commandsData[0];
                    string fullName = commandsData[1];
                    string description = commandsData[2];
                    string author = commandsData[3];
                    string groups = commandsData[4];
                    string prereq = commandsData[5];

                    response.atsvFilePath = tempFolder + "\\" + fullName;

                    _ = int.TryParse(commandsData[6], out int videoQuality);

                    recorder.Start(tempFolder, id, fullName, description, author, groups, prereq, videoQuality, commandsData[7]);
                }
                else
                {
                    response.ErrorCode = -50;
                    response.ErrorMessage = "Not enough space available on disk : " + (freeSpace / 1024 / 1024) + " Mo";
                }
            }
            else if (recordType == RecordType.Create)
            {
                string actionType = commandsData[0];

                _ = int.TryParse(commandsData[1], out int line);

                _ = long.TryParse(commandsData[2], out long timeLine);

                string channelName = commandsData[3];

                double[] channelDimmension = new double[] { 0, 0, 1, 1 };
                _ = double.TryParse(commandsData[4], out channelDimmension[0]);
                _ = double.TryParse(commandsData[5], out channelDimmension[1]);
                _ = double.TryParse(commandsData[6], out channelDimmension[2]);
                _ = double.TryParse(commandsData[7], out channelDimmension[3]);

                recorder.Create(actionType, line, timeLine, channelName, channelDimmension);
            }
            else if (recordType == RecordType.CreateMobile)
            {
                string actionType = commandsData[0];

                _ = int.TryParse(commandsData[1], out int line);

                _ = long.TryParse(commandsData[2], out long timeLine);

                string channelName = commandsData[3];

                double[] channelDimmension = new double[] { 0, 0, 1, 1 };
                _ = double.TryParse(commandsData[4], out channelDimmension[0]);
                _ = double.TryParse(commandsData[5], out channelDimmension[1]);
                _ = double.TryParse(commandsData[6], out channelDimmension[2]);
                _ = double.TryParse(commandsData[7], out channelDimmension[3]);

                recorder.CreateMobile(actionType, line, timeLine, channelName, channelDimmension, commandsData[8]);
            }
            else if (recordType == RecordType.Image)
            {
                double[] screenRect = new double[] { 0, 0, 1, 1 };
                _ = double.TryParse(commandsData[0], out screenRect[0]);
                _ = double.TryParse(commandsData[1], out screenRect[1]);
                _ = double.TryParse(commandsData[2], out screenRect[2]);
                _ = double.TryParse(commandsData[3], out screenRect[3]);

                _ = bool.TryParse(commandsData[4], out bool isRef);

                recorder.AddImage(screenRect, isRef);
            }
            else if (recordType == RecordType.ImageMobile)
            {
                double[] screenRect = new double[] { 0, 0, 1, 1 };
                _ = double.TryParse(commandsData[0], out screenRect[0]);
                _ = double.TryParse(commandsData[1], out screenRect[1]);
                _ = double.TryParse(commandsData[2], out screenRect[2]);
                _ = double.TryParse(commandsData[3], out screenRect[3]);

                _ = bool.TryParse(commandsData[4], out bool isRef);
                recorder.AddImage(commandsData[5], screenRect, isRef);
            }
            else if (recordType == RecordType.Value)
            {
                recorder.AddValue(commandsData[0]);
            }
            else if (recordType == RecordType.Data)
            {
                recorder.AddData(commandsData[0], commandsData[1]);
            }
            else if (recordType == RecordType.Status)
            {
                _ = int.TryParse(commandsData[0], out int error);
                _ = long.TryParse(commandsData[1], out long duration);

                recorder.Status(error, duration);
            }
            else if (recordType == RecordType.Element)
            {
                double[] elementBound = new double[] { 0, 0, 0, 0 };
                _ = double.TryParse(commandsData[0], out elementBound[0]);
                _ = double.TryParse(commandsData[1], out elementBound[1]);
                _ = double.TryParse(commandsData[2], out elementBound[2]);
                _ = double.TryParse(commandsData[3], out elementBound[3]);

                _ = long.TryParse(commandsData[4], out long searchDuration);

                _ = int.TryParse(commandsData[5], out int numElements);

                string criterias = "";
                if (commandsData.Length > 6)
                {
                    criterias = commandsData[6];
                }

                string tag = "*";
                if (commandsData.Length > 7)
                {
                    tag = commandsData[7];
                }

                recorder.AddElement(elementBound, searchDuration, numElements, criterias, tag);
            }
            else if (recordType == RecordType.Position)
            {
                recorder.AddPosition(commandsData[0], commandsData[1], commandsData[2], commandsData[3]);
            }
        }
    }
}