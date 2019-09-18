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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;

class ActionKeyboard
{
    private static Regex keyRegexp = new Regex(@"\$key\((.*)\)");
    private InputSimulator simulator;

    public ActionKeyboard()
    {
        this.simulator = new InputSimulator();
    }

    internal void sendKeys(string data)
    {
        pasteText(Base64Decode(data));
    }

    internal void clear()
    {
        simulator.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
        simulator.Keyboard.KeyPress(VirtualKeyCode.VK_A);
        simulator.Keyboard.KeyUp(VirtualKeyCode.CONTROL);

        simulator.Keyboard.KeyPress(VirtualKeyCode.BACK);
    }

    internal void addressBar(string folder)
    {
        simulator.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
        simulator.Keyboard.KeyPress(VirtualKeyCode.VK_L);
        simulator.Keyboard.KeyUp(VirtualKeyCode.CONTROL);

        pasteText(folder);

        simulator.Keyboard.KeyDown(VirtualKeyCode.RETURN);
        simulator.Keyboard.KeyUp(VirtualKeyCode.RETURN);
    }

    internal void rootKeys(string keys)
    {
        bool isSpecialKey = false;
        foreach (Match match in keyRegexp.Matches(keys))
        {
            isSpecialKey = true;
            try
            {
                SendKeys.SendWait("{" + match.Groups[1].ToString().ToUpper() + "}");
            }
            catch (Exception) { }

            /*VirtualKeyCode code;
            if (Enum.TryParse<VirtualKeyCode>(match.Groups[1].ToString().ToUpper(), out code))
            {
                //simulator.Keyboard.KeyPress(code);
                try
                {
                    SendKeys.SendWait(code);
                }
                catch (Exception){}

            }*/
        }

        if (!isSpecialKey)
        {
            SendKeys.SendWait(keys);
        }
    }

    internal void down(string code)
    {
        if ("33".Equals(code))//ctrl key
        {
            simulator.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
        }
        else if ("46".Equals(code))//shift key
        {
            simulator.Keyboard.KeyDown(VirtualKeyCode.SHIFT);
        }
    }

    internal void release(string code)
    {
        if ("33".Equals(code))//ctrl key
        {
            simulator.Keyboard.KeyUp(VirtualKeyCode.CONTROL);
        }
        else if ("46".Equals(code))//shift key
        {
            simulator.Keyboard.KeyUp(VirtualKeyCode.SHIFT);
        }
    }

    private string Base64Decode(string base64EncodedData)
    {
        var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
        return Encoding.UTF8.GetString(base64EncodedBytes);
    }

    private void pasteText(string text)
    {
        if (text.Length > 0)
        {
            Thread thread = new Thread(() => Clipboard.SetText(text));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            simulator.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
            simulator.Keyboard.KeyPress(VirtualKeyCode.VK_V);
            simulator.Keyboard.KeyUp(VirtualKeyCode.CONTROL);
        }
    }
}