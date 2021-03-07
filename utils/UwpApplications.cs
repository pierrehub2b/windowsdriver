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

namespace windowsdriver.utils
{
    class UwpApplications
    {
        public static string getApplicationId(string package)
        {
            if (package.Contains("*"))
            {
                return getApplication(package);
            }
            else
            {
                string appId = getApplication(package);
                if (appId != null)
                {
                    return appId;
                }
            }
            return getApplication("*" + package + "*");
        }

        private static string getApplication(string package)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.StartInfo.FileName = "powershell.exe";
            p.StartInfo.Arguments = @"Get-AppxPackage -Name " + package;
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            string[] data = output.Split('\n');
            string fullName = null;
            string name = null;
            string publisherId = null;
            foreach (string line in data)
            {
                string[] dataLine = line.Split(':');
                if (dataLine.Length > 1) {
                    if (dataLine[0].StartsWith("PackageFamilyName"))
                    {
                        fullName = dataLine[1].Trim();
                    }else if (dataLine[0].StartsWith("Name"))
                    {
                        name = dataLine[1].Trim();
                    }
                    else if (dataLine[0].StartsWith("PublisherId"))
                    {
                        publisherId = dataLine[1].Trim();
                    }
                }
            }

            if(fullName != null && fullName.Length > 0)
            {
                return fullName;
            }
            else if((name != null && name.Length > 0) && (publisherId != null && publisherId.Length > 0))
            {
                return name + "_" + publisherId;
            }

            return null;
        }
    }
}
