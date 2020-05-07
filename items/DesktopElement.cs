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
using FlaUI.Core.Definitions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;

namespace windowsdriver.items
{
    [DataContract(Name = "com.ats.executor.drivers.desktop.DesktopElement")]
    public class DesktopElement : DesktopWindow
    {
        public DesktopElement(AutomationElement elem, Rectangle deskRect) : base(elem, deskRect)
        {
            X = deskRect.X - 5;
            Y = deskRect.Y + 5;
            Width = deskRect.Width + 10;
            Height = deskRect.Height + 10;
        }

        public override AtsElement[] GetElementsTree(DesktopManager desktop)
        {
            List<AtsElement> listElements = new List<AtsElement>();
            
            foreach (AutomationElement child in Element.FindAllChildren())
            {
                if (!child.ClassName.Equals("ApolloRuntimeContentWindow"))
                {
                    if (DesktopManager.IsDesktopComponent(child.ClassName))
                    {
                        listElements.Add(new AtsElement(true, child));
                    }
                    else
                    {
                        listElements.Add(new AtsElement(child));
                    }
                }
            }

            return listElements.ToArray();
        }
        
        public override AtsElement[] GetElements(string tag, string[] attributes, AutomationElement root, DesktopManager desktop)
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
                desktopElements.Add(child);
                if (DesktopManager.IsDesktopComponent(child.ClassName))
                {
                    foreach (AutomationElement subChild in AtsElement.GetDescendents(child))
                    {
                        desktopElements.Add(subChild);
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
                        listElements.Add(new AtsElement(desktopElements[i], newAttributes));
                    }
                }
                else
                {
                    for (int i = 0; i < desktopElements.Count; i++)
                    {
                        listElements.Add(new AtsElement(true, desktopElements[i]));
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
                            listElements.Add(new AtsElement(elem, newAttributes));
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

            return listElements.FindAll(e => e.Visible == true).ToArray();
        }
        
        public override void Resize(int w, int h){ }
        public override void Move(int x, int y){}
        public override void Close() { }
        public override void Focus() { }
        public override void ChangeState(string value) { }

        public override void ToFront() {

            AutomationElement[] children = Element.FindAllChildren();

            foreach (AutomationElement child in children)
            {
                if (!DesktopManager.IsDesktopComponent(child.ClassName) && !child.ClassName.Equals("ApolloRuntimeContentWindow") && child.Patterns.Window.Pattern.WindowVisualState.IsSupported)
                {
                    child.Patterns.Window.Pattern.WindowVisualState.TryGetValue(out WindowVisualState state);
                    if (!state.Equals(WindowVisualState.Minimized) && child.Patterns.Window.Pattern.CanMinimize)
                    {
                        child.Patterns.Window.Pattern.SetWindowVisualState(WindowVisualState.Minimized);
                    }
                }
            }
        }
    }
}