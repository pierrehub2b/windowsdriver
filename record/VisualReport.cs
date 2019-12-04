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

        if (!string.IsNullOrEmpty(id)) 
        {
            this.Id = id;
        }

        if (!string.IsNullOrEmpty(author))
        {
            this.Author = author;
        }

        if (!string.IsNullOrEmpty(description))
        {
            this.Description = description;
        }

        if (!string.IsNullOrEmpty(groups))
        {
            this.Groups = groups;
        }

        if (!string.IsNullOrEmpty(prereq))
        {
            this.Prerequisite = prereq;
        }
    }

    [DataMember(Name = "author")]
    public string Author;

    [DataMember(Name = "description")]
    public string Description;

    [DataMember(Name = "started")]
    public string Started;

    [DataMember(Name = "groups")]
    public string Groups;

    [DataMember(Name = "id")]
    public string Id;

    [DataMember(Name = "name")]
    public string Name;

    [DataMember(Name = "prerequisite")]
    public string Prerequisite;

    [DataMember(Name = "quality")]
    public int Quality;

    [DataMember(Name = "cpuSpeed")]
    public long CpuSpeed;

    [DataMember(Name = "totalMemory")]
    public long TotalMemory;

    [DataMember(Name = "cpuCount")]
    public int CpuCount;

    [DataMember(Name = "osInfo")]
    public string OsInfo;
}