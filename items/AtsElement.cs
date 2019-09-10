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

using FlaUI.Core;
using FlaUI.Core.AutomationElements.Infrastructure;
using FlaUI.Core.AutomationElements;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

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

    private readonly string innerText = "";

    public virtual void dispose()
    {
        this.Element = null;
    }

    public AtsElement(AutomationElement elem, string tag)
    {
        this.Id = Guid.NewGuid().ToString();
        this.Element = elem;
        this.Tag = tag;

        updateBounding(elem);
    }

    public AtsElement(AutomationElement elem)
    {
        this.Id = Guid.NewGuid().ToString();
        this.Element = elem;

        if (elem.Properties.IsOffscreen.IsSupported)
        {
            this.Visible = !elem.Properties.IsOffscreen.Value;
        }
        else
        {
            this.Visible = true;
        }

        this.Tag = elem.Properties.ControlType.ValueOrDefault.ToString();
        if (this.Tag.Length == 0)
        {
            this.Tag = elem.Properties.ClassName;
        }

        updateBounding(elem);
    }

    private void updateBounding(AutomationElement elem)
    {
        this.X = elem.BoundingRectangle.X;
        this.Y = elem.BoundingRectangle.Y;
        this.Width = elem.BoundingRectangle.Width;
        this.Height = elem.BoundingRectangle.Height;
    }

    internal bool isTag(string value)
    {
        return Tag.Equals(value);
    }

    public void SelectIndex(int index)
    {
        ComboBox combo = Element.AsComboBox();

        if(combo != null)
        {
            combo.Expand();
            combo.Select(index);
            if (combo.IsEditable)
            {
                combo.EditableText = Element.AsComboBox().SelectedItem.Text;
            }
            combo.Collapse();
        }
    }

    public void SelectText(string text)
    {
        ComboBox combo = Element.AsComboBox();
        if (combo != null)
        {
            combo.Expand();
            combo.Select(text);
            if (combo.IsEditable)
            {
                combo.EditableText = Element.AsComboBox().SelectedItem.Text;
            }
            combo.Collapse();
        }
    }

    //-----------------------------------------------------------------------------------------------------

    internal List<AtsElement> getElements(string tag, string[] attributes)
    {
        List<AtsElement> listElements = new List<AtsElement>
        {
            this
        };

        AutomationElement[] uiElements = Element.FindAllDescendants();
        if ("*".Equals(tag) || tag.Length == 0)
        {
            foreach (AutomationElement element in uiElements)
            {
                listElements.Add(CachedElement.createCachedElement(element));
            }
        }
        else
        {
            foreach (AutomationElement element in uiElements)
            {
                AtsElement atsElement = new AtsElement(element);
                if (atsElement.isTag(tag))
                {
                    CachedElement.addCachedElement(atsElement);
                    listElements.Add(atsElement);
                }
            }
        }

        if (attributes.Length > 0)
        {
            Parallel.ForEach<AtsElement>(listElements, elem =>
            {
                elem.loadProperties(attributes);
            });
        }

        return listElements;
    }

    //-----------------------------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------------------------

    internal void loadProperties()
    {
        if (Attributes == null)
        {
            Attributes = Properties.addProperties(new string[] {
                Properties.Name,
                Properties.AutomationId,
                Properties.ClassName,
                Properties.HelpText,
                Properties.ItemStatus,
                Properties.ItemType,
                Properties.AriaRole,
                Properties.AriaProperties,
                Properties.AcceleratorKey,
                Properties.AccessKey,
                Properties.IsEnabled,
                Properties.IsPassword,
                Properties.Text,
                Properties.Value,
                Properties.IsReadOnly,
                Properties.SelectedItems,
                Properties.IsSelected,
                Properties.Toggle,
                Properties.RangeValue,
                Properties.HorizontalScrollPercent,
                Properties.VerticalScrollPercent,
                Properties.FillColor,
                Properties.FillPatternColor,
                Properties.FillPatternStyle,
                Properties.AnnotationTypeName,
                Properties.Author,
                Properties.DateTime
            }, Element.Properties, Element.Patterns);
        }
    }
       
    internal DesktopData getProperty(string name)
    {
        foreach (DesktopData data in Attributes)
        {
            if (data.name.Equals(name))
            {
                return data;
            }
        }
        return null;
    }

    public void loadProperties(string[] attributes)
    {
        this.Attributes = Properties.addProperties(attributes, Element.Properties, Element.Patterns);
    }

    public void addInnerText(string value)
    {
        //TODO inner text
    }

    //-----------------------------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------------------------

    internal List<AtsElement> getParents()
    {
        loadProperties();

        List<AtsElement> parents = new List<AtsElement>();

        AutomationElement parent = Element.Parent;
        while (parent != null)
        {
            AtsElement parentElement = CachedElement.createCachedElement(parent);
            parentElement.loadProperties();

            parents.Insert(0, parentElement);
            parent = parent.Parent;
        }
        
        //TODO get full innertext

        return parents;
    }

    private static class Properties
    {
        public const string Name = "Name";
        public const string AutomationId = "AutomationId";
        public const string ClassName = "ClassName";
        public const string HelpText = "HelpText";
        public const string ItemStatus = "ItemStatus";
        public const string ItemType = "ItemType";
        public const string AriaRole = "AriaRole";
        public const string AriaProperties = "AriaProperties";
        public const string AcceleratorKey = "AcceleratorKey";
        public const string AccessKey = "AccessKey";
        public const string IsEnabled = "IsEnabled";
        public const string IsPassword = "IsPassword";
        public const string Text = "Text";
        public const string Value = "Value";
        public const string IsReadOnly = "IsReadOnly";
        public const string IsSelected = "IsSelected";
        public const string SelectedItems = "SelectedItems";
        public const string Toggle = "Toggle";
        public const string RangeValue = "RangeValue";
        public const string HorizontalScrollPercent = "HorizontalScrollPercent";
        public const string VerticalScrollPercent = "VerticalScrollPercent";
        public const string FillColor = "FillColor";
        public const string FillPatternColor = "FillPatternColor";
        public const string FillPatternStyle = "FillPatternStyle";
        public const string AnnotationTypeName = "AnnotationTypeName";
        public const string Author = "Author";
        public const string DateTime = "DateTime";

        private static void addProperty(string propertyName, AutomationElementPropertyValues propertyValues, AutomationElementPatternValuesBase patterns, List<DesktopData> properties)
        {
            switch (propertyName)
            {
                case Name:
                    checkProperty(propertyName, propertyValues.Name, properties);
                    break;
                case AutomationId:
                    checkProperty(propertyName, propertyValues.AutomationId, properties);
                    break;
                case ClassName:
                    checkProperty(propertyName, propertyValues.ClassName, properties);
                    break;
                case HelpText:
                    checkProperty(propertyName, propertyValues.HelpText, properties);
                    break;
                case ItemStatus:
                    checkProperty(propertyName, propertyValues.ItemStatus, properties);
                    break;
                case ItemType:
                    checkProperty(propertyName, propertyValues.ItemType, properties);
                    break;
                case AriaRole:
                    checkProperty(propertyName, propertyValues.AriaRole, properties);
                    break;
                case AriaProperties:
                    checkProperty(propertyName, propertyValues.AriaProperties, properties);
                    break;
                case AcceleratorKey:
                    checkProperty(propertyName, propertyValues.AcceleratorKey, properties);
                    break;
                case AccessKey:
                    checkProperty(propertyName, propertyValues.AccessKey, properties);
                    break;
                case IsEnabled:
                    checkProperty(propertyName, propertyValues.IsEnabled, properties);
                    break;
                case IsPassword:
                    checkProperty(propertyName, propertyValues.IsPassword, properties);
                    break;
                case Text:
                    if (patterns.Text.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patterns.Text.Pattern.DocumentRange.GetText(999999999)));
                    }
                    break;
                case Value:
                    if (patterns.Value.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patterns.Value.Pattern.Value.ValueOrDefault));
                    }
                    break;
                case IsReadOnly:
                    if (patterns.Value.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patterns.Value.Pattern.IsReadOnly));
                    }
                    break;
                case SelectedItems:
                    if (patterns.Selection.IsSupported)
                    {
                        List<string> items = new List<string>();
                        foreach(AutomationElement item in patterns.Selection.Pattern.Selection.ValueOrDefault)
                        {
                            items.Add(item.Name);
                        }
                        properties.Add(new DesktopData(propertyName, String.Join(",", items)));
                    }
                    break;
                case IsSelected:
                    if (patterns.SelectionItem.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patterns.SelectionItem.Pattern.IsSelected));
                    }
                    break;
                case Toggle:
                    if (patterns.Toggle.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patterns.Toggle.Pattern.ToggleState.ToString()));
                    }
                    break;
                case RangeValue:
                    if (patterns.RangeValue.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, Convert.ToInt32(patterns.RangeValue.Pattern.Value)));
                    }
                    break;
                case HorizontalScrollPercent:
                    if (patterns.Scroll.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, Convert.ToInt32(patterns.Scroll.Pattern.HorizontalScrollPercent.ValueOrDefault)));
                    }
                    break;
                case VerticalScrollPercent:
                    if (patterns.Scroll.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, Convert.ToInt32(patterns.Scroll.Pattern.VerticalScrollPercent.ValueOrDefault)));
                    }
                    break;
                case FillColor:
                    if (patterns.Styles.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patterns.Styles.Pattern.FillColor.ValueOrDefault));
                    }
                    break;
                case FillPatternColor:
                    if (patterns.Styles.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patterns.Styles.Pattern.FillPatternColor.ValueOrDefault));
                    }
                    break;
                case FillPatternStyle:
                    if (patterns.Styles.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patterns.Styles.Pattern.FillPatternStyle.ValueOrDefault));
                    }
                    break;
                case AnnotationTypeName:
                    if (patterns.Annotation.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patterns.Annotation.Pattern.AnnotationTypeName));
                    }
                    break;
                case Author:
                    if (patterns.Annotation.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patterns.Annotation.Pattern.Author));
                    }
                    break;
                case DateTime:
                    if (patterns.Annotation.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patterns.Annotation.Pattern.DateTime));
                    }
                    break;
            }
        }

        public static DesktopData[] addProperties(string[] attributes, AutomationElementPropertyValues properties, AutomationElementPatternValuesBase patterns)
        {
            List<DesktopData> result = new List<DesktopData>();

            Parallel.ForEach<string>(attributes, a =>
            {
                addProperty(a, properties, patterns, result);
            });

            return result.ToArray();
        }

        private static void checkProperty(string name, AutomationProperty<bool> property, List<DesktopData> properties)
        {
            if (property.IsSupported)
            {
                properties.Add(new DesktopData(name, property.ValueOrDefault));
            }
        }

        private static void checkProperty(string name, AutomationProperty<string> property, List<DesktopData> properties)
        {
            if (property.IsSupported)
            {
                properties.Add(new DesktopData(name, property.ValueOrDefault));
            }
        }
    }
}