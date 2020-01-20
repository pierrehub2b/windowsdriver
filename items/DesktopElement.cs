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

using FlaUI.Core.AutomationElements;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace windowsdriver.items
{
    [DataContract(Name = "com.ats.executor.drivers.desktop.DesktopElement")]
    public class DesktopElement : AtsElement
    {
        public DesktopElement(AutomationElement elem) : base(elem){
            Tag = "Desktop";
            X = -5;
            Y = 5;
            Width = SystemInformation.VirtualScreen.Width + 10;
            Height = SystemInformation.VirtualScreen.Height + 10;
        }

        public override AtsElement[] GetElements(string tag, string[] attributes)
        {
            List<AtsElement> listElements = new List<AtsElement>
            {
                this
            };

            int len = attributes.Length;
            AutomationElement[] uiElements;

            uiElements = Element.FindAllChildren();
            foreach (AutomationElement child in uiElements)
            {
                    listElements.Add(CachedElement.CreateCachedElement(child, true));
                    if (IsDesktopComponent(child.ClassName))
                    {
                        foreach (AutomationElement subChild in child.FindAllDescendants())
                        {
                            listElements.Add(CachedElement.CreateCachedElement(subChild, true));
                        }
                    }
            }

            Array.Clear(uiElements, 0, len);
            return listElements.ToArray();
        }
        
        public static bool IsDesktopComponent(string className)
        {
            return className.StartsWith("Shell_")
                    || className.StartsWith("TaskList")
                    || className == "Progman"
                    || className == "NotifyIconOverflowWindow"
                    || className == "Windows.UI.Core.CoreWindow";
        }
    }
}
