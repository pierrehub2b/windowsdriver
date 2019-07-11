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
using System.Runtime.Serialization;
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

    public virtual void dispose()
    {
        this.Element = null;
    }
    
    public AtsElement(AutomationElement elem)
    {
        this.Id = Guid.NewGuid().ToString();
        this.Element = elem;

        try
        {
            this.Tag = ElementProperty.getSimpleControlName(elem.Current.ControlType.ProgrammaticName);
            this.Password = elem.Current.IsPassword;

            updateVisual(elem);
        }
        catch (ElementNotAvailableException ex)
        {
            throw ex;
        }
    }

    public void updateVisual(AutomationElement elem)
    {
        this.Visible = !elem.Current.IsOffscreen;

        System.Windows.Rect bounding = elem.Current.BoundingRectangle;
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

    internal string getText()
    {
        object pattern;
        if (Element.TryGetCurrentPattern(TextPattern.Pattern, out pattern))
        {
            return (pattern as TextPattern).DocumentRange.GetText(-1);
        }
        else if (Element.TryGetCurrentPattern(ValuePattern.Pattern, out pattern))
        {
            try
            {
                return (pattern as ValuePattern).Current.Value;
            }
            catch (Exception) { }
        }
        return null;
    }

    internal List<AtsElement> getElements(string tag, string[] attributes)
    {
        if (!"*".Equals(tag))
        {
            tag = string.Format("ControlType.{0}", tag);
        }

        List<AtsElement> listElements = new List<AtsElement>();
        addChild(listElements, Element, tag, attributes);

        return listElements;
    }

    private void addChild(List<AtsElement> listChild, AutomationElement parent, string tag, string[] attributes)
    {
        if (parent.Current.ControlType == ControlType.Document)
        {
            object documentUrl;
            if (parent.TryGetCurrentPattern(ValuePattern.Pattern, out documentUrl))
            {
                if ((documentUrl as ValuePattern).Current.Value.StartsWith("http"))
                {
                    return;
                }
            }
        }

        if ("*".Equals(tag) || parent.Current.ControlType.ProgrammaticName.Equals(tag))
        {
            AtsElement el = CachedElement.getCachedElement(parent);

            if (attributes.Length > 0)
            {
                List<DesktopData> attr = new List<DesktopData>();
                foreach (string a in attributes)
                {
                    DesktopData prop = el.getProperty(a);
                    if (prop != null)
                    {
                        attr.Add(prop);
                    }
                    else
                    {
                        break;
                    }
                }

                if (attr.Count == attributes.Length)
                {
                    el.Attributes = attr.ToArray();
                    listChild.Add(el);
                }
            }
            else
            {
                listChild.Add(el);
            }
        }

        var walker = TreeWalker.RawViewWalker;

        var current = walker.GetFirstChild(parent);
        while (current != null)
        {
            addChild(listChild, current, tag, attributes);
            current = walker.GetNextSibling(current);
        }
    }

    internal List<DesktopData> getProperties()
    {
        List<DesktopData> properties = new List<DesktopData>();

        string txt = getText();
        if (txt != null)
        {
            properties.Add(new DesktopData("Text", txt));
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

        var walker = TreeWalker.RawViewWalker;
        var current = walker.GetParent(Element);

        while (current != null && current.Current.ClassName != "#32769")
        {
            parents.Add(CachedElement.getCachedElement(current));
            current = walker.GetParent(current);
        }

        if(parents.Count > 0)
        {
            parents.RemoveAt(parents.Count - 1);
        }

        return parents;
    }
}