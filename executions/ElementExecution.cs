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
using FlaUI.UIA3;
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
        Select = 4,
        FromPoint = 5
    };

    private readonly Executor executor;

    public ElementExecution(int type, string[] commandsData) : base()
    {
        elemType = (ElementType)type;

        if (elemType == ElementType.Find)
        {
            if (commandsData.Length > 1)
            {
                int.TryParse(commandsData[0], out int handle);
                if (handle > 0)
                {
                    executor = new FindExecutor(response, handle, commandsData[1], new List<string>(commandsData).GetRange(2, commandsData.Length - 2).ToArray());
                    return;
                }
                else
                {
                    response.setError(-72, "invalid handle value");
                }
            }
        }
        else if (elemType == ElementType.FromPoint)
        {
            executor = new FromPointExecutor(response);
            return;
        }
        else
        {
            AtsElement element = CachedElement.GetCachedElementById(commandsData[0]);
            if (element == null)
            {
                response.setError(-73, "cached element not found");
            }
            else
            {
                if (elemType == ElementType.Parents)
                {
                    executor = new ElementExecutor(response, element);
                    return;
                }
                else if (elemType == ElementType.Attributes)
                {
                    if (commandsData.Length > 1)
                    {
                        executor = new AttributesExecutor(response, element, commandsData[1]);
                    }
                    else
                    {
                        executor = new AttributesExecutor(response, element, null);
                    }
                    return;
                }
                else if (commandsData.Length > 1)
                {
                    if (elemType == ElementType.Childs)
                    {
                        executor = new ChildsExecutor(response, element, commandsData[1], new List<string>(commandsData).GetRange(2, commandsData.Length - 2).ToArray());
                        return;
                    }

                    if (elemType == ElementType.Select && commandsData.Length > 2)
                    {
                        if (commandsData.Length > 3)
                        {
                            executor = new SelectExecutor(response, element, commandsData[1], commandsData[2], commandsData[3]);
                            return;
                        }
                        else
                        {
                            executor = new SelectExecutor(response, element, commandsData[1], commandsData[2]);
                            return;
                        }
                    }
                }
            }
        }
        executor = new EmptyExecutor(response);
    }

    public override bool Run(HttpListenerContext context)
    {
        executor.Run();
        return base.Run(context);
    }

    private abstract class Executor
    {
        protected readonly DesktopResponse response;
        public Executor(DesktopResponse response)
        {
            this.response = response;
        }
        public abstract void Run();
    }

    private class EmptyExecutor : Executor
    {
        public EmptyExecutor(DesktopResponse response) : base(response) { }
        public override void Run() { }
    }

    private class FindExecutor : Executor
    {
        private readonly int handle;
        private readonly string tag;
        private readonly string[] attributes;

        public FindExecutor(DesktopResponse response, int handle, string tag, string[] attributes) : base(response)
        {
            this.handle = handle;
            this.tag = tag;
            this.attributes = attributes;
        }

        public override void Run()
        {
            List<AtsElement> elements = new List<AtsElement>();
            DesktopWindow window = DesktopWindow.GetWindowByHandle(handle);
            if (window != null)
            {
                elements.AddRange(window.GetElements(tag, attributes));
            }

            response.Elements = elements.ToArray();
        }
    }
    private class FromPointExecutor : Executor
    {
        public FromPointExecutor(DesktopResponse response) : base(response) { }

        public override void Run()
        {
            UIA3Automation ui3 = new UIA3Automation();
            AtsElement elem = new AtsElement(ui3.FromPoint(Mouse.Position));

            response.Elements = new AtsElement[1] { elem };
            ui3.Dispose();
        }
    }

    private class ElementExecutor : Executor
    {
        protected AtsElement element;

        public ElementExecutor(DesktopResponse response, AtsElement element) : base(response)
        {
            this.element = element;
        }

        public override void Run()
        {
            response.Elements = element.GetParents().ToArray();
        }
    }

    private class ChildsExecutor : ElementExecutor
    {
        private readonly string tag;
        private readonly string[] attributes;

        public ChildsExecutor(DesktopResponse response, AtsElement element, string tag, string[] attributes) : base(response, element)
        {
            this.tag = tag;
            this.attributes = attributes;
        }

        public override void Run()
        {
            response.Elements = element.GetElements(tag, attributes).ToArray();
        }
    }

    private class AttributesExecutor : ElementExecutor
    {
        private readonly string propertyName;

        public AttributesExecutor(DesktopResponse response, AtsElement element, string propertyName) : base(response, element)
        {
            this.propertyName = propertyName;
        }

        public override void Run()
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
    }

    private class SelectExecutor : ElementExecutor
    {
        private readonly bool regexp;
        private readonly string type;
        private readonly string value;

        public SelectExecutor(DesktopResponse response, AtsElement element, string type, string value) : base(response, element)
        {
            this.type = type;
            this.value = value;
            this.regexp = false;
        }

        public SelectExecutor(DesktopResponse response, AtsElement element, string type, string value, string regexp) : base(response, element)
        {
            this.type = type;
            this.value = value;
            bool.TryParse(regexp, out this.regexp);
        }

        public override void Run()
        {
            if ("index".Equals(type))
            {
                int.TryParse(value, out int index);
                element.SelectIndex(index);
            }
            else
            {
                element.SelectText(value, regexp);
            }
        }
    }
}