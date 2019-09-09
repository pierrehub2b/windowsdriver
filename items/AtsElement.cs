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
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows.Automation;

[DataContract(Name = "com.ats.element.AtsElement")]
public class AtsElement
{
    [DataMember(Name = "id")]
    public string Id { get; set; }

    [DataMember(Name = "tag")]
    public string Tag { get; set; }

    [DataMember(Name = "x")]
    public double X { get; set; }

    [DataMember(Name = "y")]
    public double Y { get; set; }

    [DataMember(Name = "width")]
    public double Width { get; set; }

    [DataMember(Name = "height")]
    public double Height { get; set; }

    [DataMember(Name = "visible")]
    public Boolean Visible { get; set; }

    [DataMember(Name = "password")]
    public Boolean Password { get; set; }

    [DataMember(Name = "attributes")]
    public DesktopData[] Attributes { get; set; }

    public AutomationElement Element { get; set; }

    private string value = null;
    private string innerText = "";

    private static readonly ParallelOptions pOptions = new ParallelOptions() { MaxDegreeOfParallelism = 10};

    public static IDictionary<int, string> ControlTypes = new Dictionary<int, string>();
    static AtsElement()
    {
        FieldInfo[] properties = typeof(ControlType).GetFields();
        foreach (FieldInfo field in properties)
        {
            ControlType value = (ControlType)field.GetValue(null);
            ControlTypes.Add(value.Id, field.Name);
        }
    }
    public static int GetTagKey(string value)
    {
        foreach (KeyValuePair<int, string> pair in ControlTypes)
        {
            if (pair.Value.Equals(value))
            {
                return pair.Key;
            }
        }
        return 0;
    }

    public virtual void dispose()
    {
        this.Element = null;
    }

    public AtsElement(AutomationElement elem, string tag)
    {
        this.Id = Guid.NewGuid().ToString();
        this.Element = elem;
        this.Tag = tag;

        updateBounding();
    }

    public AtsElement(AutomationElement elem)
    {
        this.Id = Guid.NewGuid().ToString();
        this.Element = elem;
        this.Password = elem.Current.IsPassword;
        this.Visible = !elem.Current.IsOffscreen;

        updateBounding();

        string tag = "*";
        if (elem.Current.ControlType == null)
        {
            tag = elem.Current.ClassName;
        }
        else
        {
            ControlTypes.TryGetValue(elem.Current.ControlType.Id, out tag);
        }
        this.Tag = tag;
    }

    private void updateBounding()
    {
        System.Windows.Rect bounding = Element.Current.BoundingRectangle;
        if (bounding.IsEmpty)
        {
            this.X = 0;
            this.Y = 0;
            this.Width = 0;
            this.Height = 0;
        }
        else
        {
            this.X = bounding.X;
            this.Y = bounding.Y;
            this.Width = bounding.Width;
            this.Height = bounding.Height;
        }
    }

    internal string getElementText(AutomationElement elem)
    {
        object pattern;
        if (elem.TryGetCurrentPattern(TextPattern.Pattern, out pattern))
        {
            return (pattern as TextPattern).DocumentRange.GetText(-1);
        }
        else if (elem.TryGetCurrentPattern(ValuePattern.Pattern, out pattern))
        {
            try
            {
                return (pattern as ValuePattern).Current.Value;
            }
            catch (Exception) { }
        }
        return null;
    }

    internal string getText()
    {
        return getElementText(Element);
    }

    public void addInnerText(string value)
    {
        if(value.Length > 0)
        {
            innerText += value + "\t";
        }
    }

    public void loadAttributes(string[] attributes)
    {
        List<DesktopData> attr = new List<DesktopData>();

        Parallel.ForEach<string>(attributes, a =>
        {
            DesktopData data = getProperty(a);
            if (data != null)
            {
                attr.Add(data);
            }
        });

        this.Attributes = attr.ToArray();
    }

    internal List<AtsElement> getElements(string tag, string[] attributes)
    {
        List<AtsElement> listElements = new List<AtsElement>();
        Attributes = new DesktopData[0];
        listElements.Add(this);
        
        addChild(listElements, Element, tag, attributes, this);

        if (attributes.Length > 0)
        {
            Parallel.ForEach<AtsElement>(listElements, elem =>
            {
                elem.loadAttributes(attributes);
            });
        }

        return listElements;
    }

    private void addChild(List<AtsElement> listChild, AutomationElement parent, string tag, string[] attributes, AtsElement currentElement)
    {
        if ("*".Equals(tag) || (parent.Current.ControlType == null && parent.Current.ClassName.Equals(tag)) || (parent.Current.ControlType != null && parent.Current.ControlType.Id == GetTagKey(tag)))
        {
            currentElement = CachedElement.getCachedElement(parent);
            listChild.Add(currentElement);
        }

        object valuePattern;
        if (true == parent.TryGetCurrentPattern(ValuePatternIdentifiers.Pattern, out valuePattern))
        {
            value = ((ValuePattern)valuePattern).Current.Value;
        }

        List<AutomationElement> children = parent.FindAll(TreeScope.Children, Condition.TrueCondition).Cast<AutomationElement>().ToList();
        Parallel.ForEach<AutomationElement>(children, pOptions, elem =>
        {
            currentElement.addInnerText(elem.Current.Name);
            addChild(listChild, elem, tag, attributes, currentElement);
        });
    }

    internal List<DesktopData> getProperties()
    {
        List<DesktopData> properties = new List<DesktopData>();
        if(innerText.Length > 0)
        {
            properties.Add(new DesktopData("InnerText", innerText.Substring(0, innerText.Length-1)));
        }
        
        string txt = getText();
        if (txt != null)
        {
            properties.Add(new DesktopData("Text", txt));
        }

        if(value != null)
        {
            properties.Add(new DesktopData("Value", value));
        }

        AutomationProperty[] list = Array.FindAll(Element.GetSupportedProperties(), ElementProperty.notRequiredProperties);
        foreach (AutomationProperty prop in list)
        {
            try
            {
                object propertyValue = Element.GetCurrentPropertyValue(prop);
                if (propertyValue != null)
                {
                    properties.Add(new DesktopData(ElementProperty.getSimplePropertyName(prop.ProgrammaticName), propertyValue.ToString()));
                }
            }
            catch (InvalidOperationException) { }
        }
        return properties;
    }

    internal DesktopData getProperty(string name)
    {
        if ("Text".Equals(name))
        {
            return new DesktopData("Text", getText());
        }
        else if ("InnerText".Equals(name))
        {
            string result = innerText;
            if (result.Length > 0)
            {
                return new DesktopData("InnerText", result.Substring(0, result.Length - 1));
            }
            return new DesktopData("InnerText", result);
        }

        AutomationProperty prop = Array.Find(Element.GetSupportedProperties(), p => p.ProgrammaticName.EndsWith("." + name + "Property"));

        if (prop != null)
        {
            try
            {
                object propertyValue = Element.GetCurrentPropertyValue(prop);
                if (propertyValue != null)
                {
                    return new DesktopData(name, propertyValue.ToString());
                }
            }
            catch (ElementNotAvailableException)
            {
                CachedElement.removeCachedElement(this);
            }
        }

        return null;
    }

    internal List<AtsElement> getParents()
    {
        List<AtsElement> parents = new List<AtsElement>();

        TreeWalker walker = TreeWalker.RawViewWalker;
        AutomationElement parent = walker.GetParent(Element);

        while (parent != null && parent.Current.ClassName != "#32769")
        {
            AtsElement parentElement = CachedElement.getCachedElement(parent);

            AutomationElementCollection siblings = parent.FindAll(TreeScope.Children, Condition.TrueCondition);
            foreach(AutomationElement sibling in siblings)
            {
                parentElement.addInnerText(sibling.Current.Name);
            }

            parents.Insert(0, parentElement);
            parent = walker.GetParent(parent);
        }

        //TODO get full innertext

        return parents;
    }
}