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
using System.Drawing;
using System.Runtime.Serialization;

namespace windowsdriver.items
{
    [DataContract(Name = "com.ats.executor.drivers.desktop.DesktopElement")]
    public class DesktopElement : AtsElement
    {
        public DesktopElement(AutomationElement elem, int desktopWidth, int desktopHeight) : base(elem){
            Tag = "Desktop";
            X = -5;
            Y = 5;
            Width = desktopWidth + 10;
            Height = desktopHeight + 10;
        }

        public override AtsElement[] GetElements(string tag, string[] attributes)
        {
            Attributes = new DesktopData[0];
            List<AtsElement> listElements = new List<AtsElement>
            {
                this
            };

            AutomationElement[] desktopChildren = Element.FindAllChildren();
            List<AutomationElement> desktopElements = new List<AutomationElement>();

            int len = attributes.Length;

            foreach (AutomationElement child in desktopChildren)
            {
                AddDesktopElement(desktopElements, child);
                if (IsDesktopComponent(child.ClassName))
                {
                    foreach (AutomationElement subChild in child.FindAllDescendants())
                    {
                        AddDesktopElement(desktopElements, subChild);
                    }
                }
            }

            if ("*".Equals(tag) || string.IsNullOrEmpty(tag))
            {
                if (len > 0)
                {
                    string[] newAttributes = new string[len];

                    for (int i = 0; i < len; i++)
                    {
                        string[] attributeData = attributes[i].Split('\t');
                        newAttributes[i] = attributeData[0];

                        /*if (attributeData.Length == 2)
                        {
                            string propertyValue = attributeData[1];
                        }*/
                    }

                    for (int i = 0; i < desktopElements.Count; i++)
                    {
                        listElements.Add(new AtsElement("*", desktopElements[i], newAttributes));
                    }
                }
                else
                {
                    for (int i = 0; i < desktopElements.Count; i++)
                    {
                        listElements.Add(new AtsElement(desktopElements[i]));
                    }
                }
            }
            else
            {
                if (len > 0)
                {
                    string[] newAttributes = new string[len];

                    for (int i = 0; i < len; i++)
                    {
                        string[] attributeData = attributes[i].Split('\t');
                        newAttributes[i] = attributeData[0];

                        /*if (attributeData.Length == 2)
                        {
                            string propertyValue = attributeData[1];
                        }*/

                    }

                    for (int i = 0; i < desktopElements.Count; i++)
                    {
                        AutomationElement elem = desktopElements[i];
                        if (tag.Equals(GetTag(elem), StringComparison.OrdinalIgnoreCase))
                        {
                            listElements.Add(new AtsElement(tag, elem, newAttributes));
                        }
                    }

                }
                else
                {
                    for (int i = 0; i < desktopElements.Count; i++)
                    {
                        AutomationElement elem = desktopElements[i];
                        if (tag.Equals(GetTag(elem), StringComparison.OrdinalIgnoreCase))
                        {
                            listElements.Add(new AtsElement(elem));
                        }
                    }
                }
            }

            return listElements.ToArray();
        }

        private void AddDesktopElement(List<AutomationElement> listElements, AutomationElement elem)
        {
            Rectangle rect = elem.BoundingRectangle;
            if (rect != null && (rect.X > -rect.Width && rect.Y > -rect.Height && rect.X < Width && rect.Y < Height))
            {
                listElements.Add(elem);
            }
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
