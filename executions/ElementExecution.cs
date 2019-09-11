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
using System.Collections.Generic;
using System.Net;

class ElementExecution : AtsExecution
{
    private readonly ElementType elemType;
    private enum ElementType
    {
        Childs = 0,
        Parents = 1,
        Find = 2,
        Attributes = 3,
        Select = 4
    };

    private readonly string tag = "*";
    private readonly string propertyName;
    private readonly int handle = -1;
    private readonly string[] attributes;

    private readonly int index = -1;

    private readonly AtsElement element;

    public ElementExecution(int type, string[] commandsData) :base()
    {
        elemType = (ElementType)type;

        if (elemType == ElementType.Find)
        {
            int.TryParse(commandsData[0], out this.handle);
            tag = commandsData[1];
            attributes = new List<string>(commandsData).GetRange(2, commandsData.Length - 2).ToArray();
        }
        else
        {
            element = CachedElement.getCachedElementById(commandsData[0]);
            if (elemType == ElementType.Childs)
            {
                tag = commandsData[1];
                attributes = new List<string>(commandsData).GetRange(2, commandsData.Length - 2).ToArray();
            }
            else if (elemType == ElementType.Attributes && commandsData.Length > 1)
            {
                propertyName = commandsData[1];
            }
            else if (elemType == ElementType.Select && commandsData.Length > 2)
            {
                if ("index".Equals(commandsData[1])){
                    int.TryParse(commandsData[2], out this.index);
                }
                else
                {
                    propertyName = commandsData[2];
                }
            }
        }
    }

    public override bool Run(HttpListenerContext context)
    {
        if (elemType == ElementType.Find)
        {
            if (handle > 0)
            {
                List<AtsElement> elements = new List<AtsElement>();
                DesktopWindow window = DesktopWindow.GetWindowByHandle(handle);
                if (window != null)
                {
                    elements.AddRange(window.GetElements(tag, attributes));
                }

                response.Elements = elements.ToArray();
            }
            else
            {
                response.setError(-72, "get all elements error with handle = " + handle);
            }
        }
        else if (elemType == ElementType.Childs)
        {
            if (element != null)
            {
                response.Elements = element.GetElements(tag, attributes).ToArray();
            }
            else
            {
                response.setError(-73, "cached element not found");
            }
        }
        else if (elemType == ElementType.Attributes)
        {
            if (element != null)
            {
                element.LoadProperties();

                if (propertyName == null)
                {
                    response.Data = element.Attributes;
                }
                else
                {
                    response.Data = new DesktopData[] { element.GetProperty(propertyName) };
                }
            }
            else
            {
                response.setError(-8, "cached element not found");
            }
        }
        else if (elemType == ElementType.Parents)
        {
            if (element != null)
            {
                response.Elements = element.GetParents().ToArray();
            }
            else
            {
                response.setError(-8, "cached element not found");
            }
        }
        else if (elemType == ElementType.Select)
        {
            if (element != null)
            {
                try
                {
                    if (index > -1)
                    {
                        element.SelectIndex(index);
                    }
                    else
                    {
                        element.SelectText(propertyName);
                    }
                }
                catch (Exception) { }
            }
            else
            {
                response.setError(-8, "cached element not found");
            }
        }

        return base.Run(context);
    }
}