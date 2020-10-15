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

[DataContract(Name = "com.ats.recorder.ReportSummary")]
public class ReportSummary
{
    public ReportSummary() : base() { }

    public ReportSummary(bool passed, int actions, string suiteName, string testName, string data) : this()
    {
        if (passed)
        {
            this.Status = 1;
        }
        else
        {
            this.Status = 0;
        }
        this.SuiteName = suiteName;
        this.TestName = testName;
        this.Actions = actions;
        this.Data = data;
    }

    public ReportSummary(bool passed, int actions, string suiteName, string testName, string data, string errorScript, int errorLine, string errorMessage) : this(passed, actions, suiteName, testName, data)
    {
        this.Error = new ReportSummaryError(errorScript, errorLine, errorMessage);
    }

    [DataMember(Name = "suiteName")]
    public string SuiteName;

    [DataMember(Name = "testName")]
    public string TestName;

    [DataMember(Name = "data")]
    public string Data;
    
    [DataMember(Name = "status")]
    public int Status;

    [DataMember(Name = "actions")]
    public int Actions;

    [DataMember(Name = "error")]
    public ReportSummaryError Error;
}