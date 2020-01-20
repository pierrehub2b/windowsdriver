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
using windowsdriver;
using windowsdriver.items;

class ElementExecution : AtsExecution
{
    private enum ElementType
    {
        Childs = 0,
        Parents = 1,
        Find = 2,
        Attributes = 3,
        Select = 4,
        FromPoint = 5,
        Script = 6,
        Root = 7
    };

    private readonly Executor executor;

    public ElementExecution(int type, string[] commandsData, DesktopManager desktop) : base()
    {
        ElementType elemType = (ElementType)type;

        if (elemType == ElementType.Find)
        {
            if (commandsData.Length > 1)
            {
                string tag = commandsData[1];
                string[] criterias = new List<string>(commandsData).GetRange(2, commandsData.Length - 2).ToArray();

                _ = int.TryParse(commandsData[0], out int handle);
                if (handle > 0)
                {
                    executor = new FindExecutor(response, desktop, handle, tag, criterias);
                }
                else
                {
                    executor = new DesktopExecutor(response, desktop, tag, criterias);
                }
                return;
            }
        }
        else if (elemType == ElementType.FromPoint)
        {
            executor = new FromPointExecutor(response, desktop);
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
                else if (elemType == ElementType.Root)
                {
                    executor = new ChildsExecutor(response, element, "*", new string[0]);
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
                else if (elemType == ElementType.Script)
                {
                    if (commandsData.Length > 1)
                    {
                        executor = new ScriptExecutor(response, element, commandsData[1]);
                        return;
                    }
                }
                else if (commandsData.Length > 1)
                {
                    if (elemType == ElementType.Childs)
                    {
                        element.TryExpand();
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

    private class DesktopExecutor : Executor
    {
        readonly DesktopManager desktop;
        readonly string tag;
        readonly string[] criterias;

        public DesktopExecutor(DesktopResponse response, DesktopManager desktop, string tag, string[] criterias) : base(response)
        {
            this.desktop = desktop;
            this.tag = tag;
            this.criterias = criterias;
        }

        public override void Run()
        {
            response.Elements = desktop.GetElements(tag, criterias);
        }
    }

    private class FindExecutor : Executor
    {
        private readonly int handle;
        private readonly string tag;
        private readonly string[] attributes;
        private readonly DesktopManager desktop;

        public FindExecutor(DesktopResponse response, DesktopManager desktop, int handle, string tag, string[] attributes) : base(response)
        {
            this.handle = handle;
            this.tag = tag;
            this.attributes = attributes;
            this.desktop = desktop;
        }

        public override void Run()
        {
            DesktopWindow window = desktop.GetWindowByHandle(handle);
            if (window != null)
            {
                window.Focus();
                response.Elements = window.GetElements(tag, attributes);
            }
            else
            {
                response.Elements = new AtsElement[0];
            }
        }
    }

    private class FromPointExecutor : Executor
    {
        private readonly DesktopManager desktop;

        public FromPointExecutor(DesktopResponse response, DesktopManager desk) : base(response) {
            desktop = desk;
        }

        public override void Run()
        {
            AtsElement elem = desktop.GetElementFromPoint(Mouse.Position);
            if(elem != null)
            {
                response.Elements = new AtsElement[1] { elem };
            }
            else
            {
                response.Elements = new AtsElement[0];
            }
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
            response.Elements = element.GetParents();
            Dispose();
        }

        public void Dispose()
        {
            element = null;
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
            response.Elements = element.GetElements(tag, attributes);
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
                response.Data = element.GetProperty(propertyName);
            }

            Dispose();
        }
    }

    private class ScriptExecutor : ElementExecutor
    {
        private readonly string script;

        public ScriptExecutor(DesktopResponse response, AtsElement element, string script) : base(response, element)
        {
            this.script = script;
        }

        public override void Run()
        {
            element.LoadProperties();

            if (script == null || script.Length == 0)
            {
                response.Data = new DesktopData[0];
            }
            else
            {
                response.Data = element.ExecuteScript(script);
            }

            Dispose();
        }
    }

    private class SelectExecutor : ElementExecutor
    {
        private readonly bool regexp = false;
        private readonly string type;
        private readonly string value;

        public SelectExecutor(DesktopResponse response, AtsElement element, string type, string value) : base(response, element)
        {
            this.type = type;
            this.value = value;
        }

        public SelectExecutor(DesktopResponse response, AtsElement element, string type, string value, string regexp) : base(response, element)
        {
            this.type = type;
            this.value = value;
            _ = bool.TryParse(regexp, out this.regexp);
        }

        public override void Run()
        {
            if ("index".Equals(type))
            {
                _ = int.TryParse(value, out int index);
                element.SelectIndex(index);
            }
            else if ("value".Equals(type))
            {
                element.SelectValue(value);
            }
            else
            {
                element.SelectText(value, regexp);
            }
        }
    }
}