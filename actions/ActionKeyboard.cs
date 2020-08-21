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
    private readonly string keyPattern = @"\$key\(([^)]*)\)";

    internal void SendKeysData(string data, bool keyDown)
    {
        data = Base64Decode(data);
        if (data.StartsWith("$KEY-", StringComparison.OrdinalIgnoreCase))
        {
            string key = data.Substring(5).ToUpper();
            if (key.Equals("BACK_SPACE"))
            {
                Keyboard.Type(VirtualKeyShort.BACK);
            }
            else
            {
                SendKeys.SendWait("{" + key + "}");
            }
        }
        else
        {
            if (keyDown)
            {
                Keyboard.Type(data.ToLowerInvariant());
            }
            else
            {
                foreach (char c in data)
                {
                    Keyboard.Type(c);
                    Thread.Sleep(10);
                }

                //SendKeys.SendWait(data);
                //Thread.Sleep(data.Length*10);
            }
        }
     }

    internal void Clear(AtsElement element)
    {
        if(element != null)
        {
            element.TextClear();
        }
    }

    internal void FocusElement(AtsElement element)
    {
        if (element != null)
        {
            element.ElementFocus();
        }
    }

    internal void AddressBar(string url)
    {
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_L);
        Keyboard.Type(url);
        Keyboard.Type(VirtualKeyShort.RETURN);
    }

    internal void RootKeys(string keys)
    {
        string[] tokens = Regex.Split(keys, keyPattern);
        foreach (string token in tokens)
        {
            if(token.Length > 0)    
            {
                try
                {
                    SendKeys.SendWait("{" + token.ToUpper() + "}");
                }
                catch
                {
                    Keyboard.Type(token);
                }
            }
        }
    }

    internal void Down(string code)
    {
        if ("33".Equals(code) || "57353".Equals(code))//ctrl key
        {
            Keyboard.Press(VirtualKeyShort.CONTROL);
        }
        else if ("46".Equals(code) || "57352".Equals(code))//shift key
        {
            Keyboard.Press(VirtualKeyShort.SHIFT);
        }
        else if ("57354".Equals(code))//alt key
        {
            Keyboard.Press(VirtualKeyShort.ALT);
        }
    }

    internal void Release(string code)
    {
        if ("33".Equals(code) || "57353".Equals(code))//ctrl key
        {
            Keyboard.Release(VirtualKeyShort.CONTROL);
        }
        else if ("46".Equals(code) || "57352".Equals(code))//shift key
        {
            Keyboard.Release(VirtualKeyShort.SHIFT);
        }
        else if ("57354".Equals(code))//alt key
        {
            Keyboard.Release(VirtualKeyShort.ALT);
        }
    }

    private static string Base64Decode(string base64EncodedData)
    {
        var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
        return Encoding.UTF8.GetString(base64EncodedBytes);
    }
}