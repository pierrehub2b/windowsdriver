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

using System.Runtime.Serialization;

[DataContract(Name = "com.ats.recorder.VisualReport")]
public class VisualReport : VisualAction
{
    public VisualReport() : base() { }

    public VisualReport(string id, string package, string description, string author, string groups, string prereq, int quality, string started) : this()
    {
        this.Type = "startVisualReport";
        this.Line = -1;
        this.Name = package;
        this.Quality = quality;
        this.Started = started;
        this.CpuSpeed = HardwareInfo.GetCpuSpeedInGHz();
        this.CpuCount = HardwareInfo.GetCpuCount();
        this.TotalMemory = HardwareInfo.GetPhysicalMemory();
        this.OsInfo = HardwareInfo.GetOSInformation();

        if (id != string.Empty)
        {
            this.Id = id;
        }

        if (author != string.Empty)
        {
            this.Author = author;
        }

        if (description != string.Empty)
        {
            this.Description = description;
        }

        if (groups != string.Empty)
        {
            this.Groups = groups;
        }

        if (prereq != string.Empty)
        {
            this.Prerequisite = prereq;
        }
    }

    [DataMember(Name = "author")]
    public string Author { get; set; }

    [DataMember(Name = "description")]
    public string Description { get; set; }

    [DataMember(Name = "started")]
    public string Started { get; set; }

    [DataMember(Name = "groups")]
    public string Groups { get; set; }

    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }

    [DataMember(Name = "prerequisite")]
    public string Prerequisite { get; set; }

    [DataMember(Name = "quality")]
    public int Quality { get; set; }

    [DataMember(Name = "cpuSpeed")]
    public long CpuSpeed { get; set; }

    [DataMember(Name = "totalMemory")]
    public long TotalMemory { get; set; }

    [DataMember(Name = "cpuCount")]
    public int CpuCount { get; set; }

    [DataMember(Name = "osInfo")]
    public string OsInfo { get; set; }
}