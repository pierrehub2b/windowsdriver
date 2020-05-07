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
using FlaUI.Core.Input;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        Root = 7,
        LoadTree = 8,
        ListItems = 9
    };

    private readonly Executor executor;

    public ElementExecution(int type, string[] commandsData, DesktopManager desktop) : base()
    {
        ElementType elemType = (ElementType)type;

        if (elemType == ElementType.Find)
        {
            if (commandsData.Length > 1)
            {
                _ = int.TryParse(commandsData[0], out int handle);
                executor = new FindExecutor(response, desktop, handle, commandsData[1], new List<string>(commandsData).GetRange(2, commandsData.Length - 2).ToArray());
                return;
            }
        }
        else if (elemType == ElementType.LoadTree)
        {
            _ = int.TryParse(commandsData[0], out int handle);
            executor = new LoadTreeExecutor(response, desktop, handle);
            return;
        }
        else if (elemType == ElementType.FromPoint)
        {
            executor = new FromPointExecutor(response, desktop);
            return;
        }
        else
        {
            AtsElement element = CachedElements.Instance.GetElementById(commandsData[0]);
            if (element == null)
            {
                response.setError(-73, "cached element not found");
            }
            else
            {
                if (elemType == ElementType.Parents)
                {
                    executor = new ElementExecutor(response, element, desktop);
                    return;
                }
                else if (elemType == ElementType.Root)
                {
                    executor = new ChildsExecutor(response, element, "*", new string[0], desktop);
                    return;
                }
                else if (elemType == ElementType.Attributes)
                {
                    if (commandsData.Length > 1)
                    {
                        executor = new AttributesExecutor(response, element, commandsData[1], desktop);
                    }
                    else
                    {
                        executor = new AttributesExecutor(response, element, null, desktop);
                    }
                    return;
                }
                else if (elemType == ElementType.Script)
                {
                    if (commandsData.Length > 1)
                    {
                        executor = new ScriptExecutor(response, element, commandsData[1], desktop);
                        return;
                    }
                }
                else if (elemType == ElementType.ListItems)
                {
                    executor = new ListItemsExecutor(response, element, desktop);
                    return;
                }
                else if (commandsData.Length > 1)
                {
                    if (elemType == ElementType.Childs)
                    {
                        executor = new ChildsExecutor(response, element, commandsData[1], new List<string>(commandsData).GetRange(2, commandsData.Length - 2).ToArray(), desktop);
                        return;
                    }

                    if (elemType == ElementType.Select && commandsData.Length > 2)
                    {
                        if (commandsData.Length > 3)
                        {
                            executor = new SelectExecutor(response, element, commandsData[1], commandsData[2], commandsData[3], desktop);
                            return;
                        }
                        else
                        {
                            executor = new SelectExecutor(response, element, commandsData[1], commandsData[2], desktop);
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

        public DesktopExecutor(DesktopResponse response, DesktopManager desktop) : base(response)
        {
            this.desktop = desktop;
        }

        public DesktopExecutor(DesktopResponse response, DesktopManager desktop, string tag, string[] criterias) : base(response)
        {
            this.desktop = desktop;
            this.tag = tag;
            this.criterias = criterias;
        }

        public override void Run()
        {
            if (string.IsNullOrEmpty(tag))
            {
                response.Elements = new AtsElement[1] { desktop.DesktopElement };
            }
            else
            {
                response.Elements = desktop.GetElements(tag, criterias);
            }
        }
    }

    private class LoadTreeExecutor : Executor
    {
        private readonly int handle;
        private readonly DesktopManager desktop;

        public LoadTreeExecutor(DesktopResponse response, DesktopManager desktop, int handle) : base(response)
        {
            this.handle = handle;
            this.desktop = desktop;
        }

        public override void Run()
        {
            DesktopWindow window = desktop.GetWindowByHandle(handle);
            if (window != null)
            {
                window.Focus();
                Task<AtsElement[]> task = Task.Run(() =>
                {
                    return window.GetElementsTree(desktop);
                });

                task.Wait(TimeSpan.FromSeconds(40));
                response.Elements = task.Result;
            }
            else
            {
                response.Elements = new AtsElement[0];
            }
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
                response.Elements = window.GetElements(tag, attributes, null, desktop);
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
        protected DesktopManager desktop;

        public ElementExecutor(DesktopResponse response, AtsElement element, DesktopManager desktop) : base(response)
        {
            this.element = element;
            this.desktop = desktop;
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

        public ChildsExecutor(DesktopResponse response, AtsElement element, string tag, string[] attributes, DesktopManager desktop) : base(response, element, desktop)
        {
            this.tag = tag;
            this.attributes = attributes;
        }

        public override void Run()
        {
            response.Elements = element.GetElements(tag, attributes, element.Element, desktop);
        }
    }

    private class ListItemsExecutor : ElementExecutor
    {
        private AtsElement[] items;

        public ListItemsExecutor(DesktopResponse response, AtsElement element, DesktopManager desktop) : base(response, element, desktop)
        {
            this.items = element.GetListItems(desktop);
        }

        public override void Run()
        {
            if(items.Length == 0)
            {
                element.TryExpand();
                items = element.GetListItems(desktop);
            }
            
            foreach(AtsElement it in items)
            {
                it.LoadListItemAttributes();
            }
            response.Elements = items;
            Keyboard.Type('\t');
        }
    }

    private class AttributesExecutor : ElementExecutor
    {
        private readonly string propertyName;

        public AttributesExecutor(DesktopResponse response, AtsElement element, string propertyName, DesktopManager desktop) : base(response, element, desktop)
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

        public ScriptExecutor(DesktopResponse response, AtsElement element, string script, DesktopManager desktop) : base(response, element, desktop)
        {
            this.script = script;
        }

        public override void Run()
        {
            element.LoadProperties();
            response.Data = element.ExecuteScript(script);
            Dispose();
        }
    }

    private class SelectExecutor : ElementExecutor
    {
        private readonly bool regexp = false;
        private readonly string type;
        private readonly string value;

        public SelectExecutor(DesktopResponse response, AtsElement element, string type, string value, DesktopManager desktop) : base(response, element, desktop)
        {
            this.type = type;
            this.value = value;
        }

        public SelectExecutor(DesktopResponse response, AtsElement element, string type, string value, string regexp, DesktopManager desktop) : base(response, element, desktop)
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
                element.SelectItem(index, desktop);
            }
            else
            {
                bool byValue = "value".Equals(type);
                if (regexp)
                {
                    Regex rx = new Regex(@value);
                    if (byValue)
                    {
                        element.SelectItem((AutomationElement e) => { return e.Patterns.Value.IsSupported && rx.IsMatch(e.Patterns.Value.ToString());}, desktop);
                    }
                    else
                    {
                        element.SelectItem((AutomationElement e) => { return rx.IsMatch(e.Name); }, desktop);
                    }
                }
                else
                {
                    if (byValue)
                    {
                        element.SelectItem((AutomationElement e) => { return e.Patterns.Value.IsSupported && e.Patterns.Value.ToString() == value; }, desktop);
                    }
                    else
                    {
                        element.SelectItem((AutomationElement e) => { return e.Name == value; }, desktop);
                    }
                }
            }
        }
    }
}