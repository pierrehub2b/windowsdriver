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

[DataContract(Name = "com.ats.recorder.ReportSummaryError")]
public class ReportSummaryError
{
    public ReportSummaryError() : base() { }

    public ReportSummaryError(string name, int line, string message) : this()
    {
        this.ScriptName = name + ":" + line;
        this.Line = line;
        this.Message = message;
    }

    [DataMember(Name = "line")]
    public int Line;

    [DataMember(Name = "scriptName")]
    public string ScriptName;

    [DataMember(Name = "message")]
    public string Message;
}