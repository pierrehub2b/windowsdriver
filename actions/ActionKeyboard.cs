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

using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

class ActionKeyboard
{
    private static readonly Regex keyRegexp = new Regex(@"\$key\((.*)\)");

    internal void SendKeysData(string data)
    {
        PasteText(Base64Decode(data));
    }

    internal void Clear()
    {
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Keyboard.Type(VirtualKeyShort.BACK);
    }

    internal void AddressBar(string folder)
    {
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_L);
        PasteText(folder);
        Keyboard.Type(VirtualKeyShort.RETURN);
    }

    internal void RootKeys(string keys)
    {
        bool isSpecialKey = false;
        foreach (Match match in keyRegexp.Matches(keys))
        {
            isSpecialKey = true;
            try
            {
                SendKeys.SendWait("{" + match.Groups[1].ToString().ToUpper() + "}");
            }
            catch { }
        }

        if (!isSpecialKey)
        {
            SendKeys.SendWait(keys);
        }
    }

    internal void Down(string code)
    {
        if ("33".Equals(code))//ctrl key
        {
            Keyboard.Pressing(VirtualKeyShort.CONTROL);
        }
        else if ("46".Equals(code))//shift key
        {
            Keyboard.Pressing(VirtualKeyShort.SHIFT);
        }
    }

    internal void Release(string code)
    {
        if ("33".Equals(code))//ctrl key
        {
            Keyboard.Release(VirtualKeyShort.CONTROL);
        }
        else if ("46".Equals(code))//shift key
        {
            Keyboard.Release(VirtualKeyShort.SHIFT);
        }
    }

    private string Base64Decode(string base64EncodedData)
    {
        var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
        return Encoding.UTF8.GetString(base64EncodedBytes);
    }

    private void PasteText(string text)
    {
        if (text.Length > 0)
        {
            if (text.StartsWith("$KEY-"))
            {
                SendKeys.SendWait("{" + text.Substring(5) + "}");
            }
            else
            {
                Thread thread = new Thread(() => Clipboard.SetText(text));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();

                Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_V);
            }
        }
    }
}