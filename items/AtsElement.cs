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
using FlaUI.Core.AutomationElements;
using FlaUI.Core.AutomationElements.Infrastructure;
using FlaUI.Core.Shapes;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
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

    //private readonly string innerText = "";

    public virtual void Dispose()
    {
        this.Element = null;
    }

    public AtsElement(AutomationElement elem, string tag)
    {
        this.Id = Guid.NewGuid().ToString();
        this.Element = elem;
        this.Tag = tag;

        UpdateBounding(elem);
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

        if (elem.Properties.IsPassword.IsSupported)
        {
            this.Password = elem.Properties.IsPassword.Value;
        }
        else
        {
            this.Password = false;
        }

        if (elem.Properties.ControlType.IsSupported)
        {
            this.Tag = elem.Properties.ControlType.ValueOrDefault.ToString();
        }

        if (string.IsNullOrEmpty(this.Tag) && elem.Properties.ClassName.IsSupported)
        {
            this.Tag = elem.Properties.ClassName;
        }

        if (this.Tag == null)
        {
            this.Tag = "*";
        }

        UpdateBounding(elem);
    }

    private void UpdateBounding(AutomationElement elem)
    {
        Rectangle rec = new Rectangle(0.0, 0.0, 9999999.0, 9999999.0);
        if (elem.Properties.BoundingRectangle.IsSupported)
        {
            rec = elem.Properties.BoundingRectangle;
        }

        this.X = rec.X;
        this.Y = rec.Y;
        this.Width = rec.Width;
        this.Height = rec.Height;
    }

    internal bool IsTag(string value)
    {
        return Tag.Equals(value);
    }

    internal void SelectIndex(ActionMouse mouse, int index)
    {
        bool expandCollapse = ExpandElement(mouse);

        if (Element.Patterns.Selection.IsSupported)
        {
            ComboBox combo = Element.AsComboBox();
            ComboBoxItem[] items = combo.Items;

            if (items.Length > index)
            {
                ComboBoxItem item = items[index];
                if (item.Patterns.ScrollItem.IsSupported)
                {
                    item.Patterns.ScrollItem.Pattern.ScrollIntoView();
                }
                item.FocusNative();
                item.Click();
            }
        }

        if (expandCollapse)
        {
            Element.Patterns.ExpandCollapse.Pattern.Collapse();
        }
    }

    internal void SelectText(ActionMouse mouse, string text, bool regexp)
    {
        bool expandCollapse = ExpandElement(mouse);

        if (Element.Patterns.Selection.IsSupported)
        {
            ComboBox combo = Element.AsComboBox();
            ComboBoxItem[] items = combo.Items;

            if (regexp)
            {
                Regex regex = new Regex(@text);
                foreach (ComboBoxItem item in items)
                {
                    if (regex.IsMatch(item.Text))
                    {
                        SelectComboItem(item);
                        break;
                    }
                }
            }
            else
            {
                foreach (ComboBoxItem item in items)
                {
                    if (item.Text.Equals(text))
                    {
                        SelectComboItem(item);
                        break;
                    }
                }
            }
        }
        else if (Element.Patterns.Value.IsSupported)
        {
            Element.Patterns.Value.Pattern.SetValue(text);
        }

        if (expandCollapse)
        {
            Element.Patterns.ExpandCollapse.Pattern.Collapse();
        }
    }

    private bool ExpandElement(ActionMouse mouse)
    {
        Element.FocusNative();

        AutomationElement dropDown = Element.FindFirstChild(c => c.ByControlType(FlaUI.Core.Definitions.ControlType.Button));
        if (dropDown != null)
        {
            Point pt = dropDown.GetClickablePoint();
            mouse.mouseMove(Convert.ToInt32(pt.X), Convert.ToInt32(pt.Y));
            mouse.click();
        }

        if (Element.Patterns.ExpandCollapse.IsSupported)
        {
            Element.Patterns.ExpandCollapse.Pattern.Expand();
            return true;
        }
        return false;
    }

    private void SelectComboItem(ComboBoxItem item)
    {
        if (item.Patterns.ScrollItem.IsSupported)
        {
            item.Patterns.ScrollItem.Pattern.ScrollIntoView();
        }
        item.FocusNative();
        item.Click();
    }

    //-----------------------------------------------------------------------------------------------------

    internal List<AtsElement> GetElements(string tag, string[] attributes)
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
                listElements.Add(CachedElement.CreateCachedElement(element));
            }
        }
        else
        {
            foreach (AutomationElement element in uiElements)
            {
                AtsElement atsElement = new AtsElement(element);
                if (atsElement.IsTag(tag))
                {
                    CachedElement.AddCachedElement(atsElement);
                    listElements.Add(atsElement);
                }
            }
        }

        if (attributes.Length > 0)
        {
            Parallel.ForEach<AtsElement>(listElements, elem =>
            {
                elem.LoadProperties(attributes);
            });
        }

        return listElements;
    }

    //-----------------------------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------------------------

    private static readonly string[] propertiesBase = new string[] {
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
                Properties.DateTime };

    private static readonly string[] propertiesGrid = new List<string>(propertiesBase) { Properties.RowCount, Properties.ColumnCount }.ToArray();

    internal void LoadProperties()
    {
        if ("Grid".Equals(Tag))
        {
            Attributes = Properties.AddProperties(propertiesGrid, Password, Element);
        }
        else
        {
            Attributes = Properties.AddProperties(propertiesBase, Password, Element);
        }
    }

    internal DesktopData GetProperty(string name)
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

    public void LoadProperties(string[] attributes)
    {
        this.Attributes = Properties.AddProperties(attributes, Password, Element);
    }

    /*public void AddInnerText(string value)
    {
        //TODO inner text
    }*/

    //-----------------------------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------------------------

    internal List<AtsElement> GetParents()
    {
        LoadProperties();

        List<AtsElement> parents = new List<AtsElement>();

        AutomationElement parent = Element.Parent;
        while (parent != null)
        {
            AtsElement parentElement = CachedElement.CreateCachedElement(parent);
            parentElement.LoadProperties();

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
        public const string ColumnCount = "ColumnCount";
        public const string RowCount = "RowCount";

        public static DesktopData[] AddProperties(string[] attributes, bool isPassword, AutomationElement element)
        {
            List<DesktopData> result = new List<DesktopData>();

            Parallel.ForEach<string>(attributes, a =>
            {
                AddProperty(a, isPassword, element, result);
            });

            return result.ToArray();
        }

        public static void AddProperty(string propertyName, bool isPassword, AutomationElement element, List<DesktopData> properties)
        {
            AutomationElementPropertyValues propertyValues = element.Properties;
            AutomationElementPatternValuesBase patternValues = element.Patterns;

            switch (propertyName)
            {
                case Name:
                    CheckProperty(propertyName, propertyValues.Name, properties);
                    break;
                case AutomationId:
                    CheckProperty(propertyName, propertyValues.AutomationId, properties);
                    break;
                case ClassName:
                    CheckProperty(propertyName, propertyValues.ClassName, properties);
                    break;
                case HelpText:
                    CheckProperty(propertyName, propertyValues.HelpText, properties);
                    break;
                case ItemStatus:
                    CheckProperty(propertyName, propertyValues.ItemStatus, properties);
                    break;
                case ItemType:
                    CheckProperty(propertyName, propertyValues.ItemType, properties);
                    break;
                case AriaRole:
                    CheckProperty(propertyName, propertyValues.AriaRole, properties);
                    break;
                case AriaProperties:
                    CheckProperty(propertyName, propertyValues.AriaProperties, properties);
                    break;
                case AcceleratorKey:
                    CheckProperty(propertyName, propertyValues.AcceleratorKey, properties);
                    break;
                case AccessKey:
                    CheckProperty(propertyName, propertyValues.AccessKey, properties);
                    break;
                case IsEnabled:
                    properties.Add(new DesktopData(propertyName, isPassword));
                    break;
                case IsPassword:
                    CheckProperty(propertyName, propertyValues.IsPassword, properties);
                    break;
                case Text:
                    if (patternValues.Text.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patternValues.Text.Pattern.DocumentRange.GetText(999999999)));
                    }
                    break;
                case Value:
                    if (isPassword)
                    {
                        properties.Add(new DesktopData(propertyName, "#PASSWORD_VALUE#"));
                    }
                    else if (patternValues.Value.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patternValues.Value.Pattern.Value.ValueOrDefault));
                    }
                    break;
                case IsReadOnly:
                    if (patternValues.Value.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patternValues.Value.Pattern.IsReadOnly));
                    }
                    break;
                case SelectedItems:
                    if (patternValues.Selection.IsSupported)
                    {
                        List<string> items = new List<string>();
                        foreach (AutomationElement item in patternValues.Selection.Pattern.Selection.ValueOrDefault)
                        {
                            if (item.Patterns.SelectionItem.IsSupported && item.Properties.Name.IsSupported)
                            {
                                items.Add(item.Name);
                            }
                        }
                        properties.Add(new DesktopData(propertyName, String.Join(",", items)));
                    }
                    break;
                case IsSelected:
                    if (patternValues.SelectionItem.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patternValues.SelectionItem.Pattern.IsSelected));
                    }
                    break;
                case Toggle:
                    if (patternValues.Toggle.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patternValues.Toggle.Pattern.ToggleState.ToString()));
                    }
                    break;
                case RangeValue:
                    if (patternValues.RangeValue.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, Convert.ToInt32(patternValues.RangeValue.Pattern.Value)));
                    }
                    break;
                case HorizontalScrollPercent:
                    if (patternValues.Scroll.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, Convert.ToInt32(patternValues.Scroll.Pattern.HorizontalScrollPercent.ValueOrDefault)));
                    }
                    break;
                case VerticalScrollPercent:
                    if (patternValues.Scroll.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, Convert.ToInt32(patternValues.Scroll.Pattern.VerticalScrollPercent.ValueOrDefault)));
                    }
                    break;
                case FillColor:
                    if (patternValues.Styles.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patternValues.Styles.Pattern.FillColor.ValueOrDefault));
                    }
                    break;
                case FillPatternColor:
                    if (patternValues.Styles.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patternValues.Styles.Pattern.FillPatternColor.ValueOrDefault));
                    }
                    break;
                case FillPatternStyle:
                    if (patternValues.Styles.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patternValues.Styles.Pattern.FillPatternStyle.ValueOrDefault));
                    }
                    break;
                case AnnotationTypeName:
                    if (patternValues.Annotation.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patternValues.Annotation.Pattern.AnnotationTypeName));
                    }
                    break;
                case Author:
                    if (patternValues.Annotation.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patternValues.Annotation.Pattern.Author));
                    }
                    break;
                case DateTime:
                    if (patternValues.Annotation.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patternValues.Annotation.Pattern.DateTime));
                    }
                    break;
                case RowCount:
                    properties.Add(new DesktopData(propertyName, element.AsGrid().RowCount));
                    break;
                case ColumnCount:
                    properties.Add(new DesktopData(propertyName, element.AsGrid().ColumnCount));
                    break;
            }
        }

        private static void CheckProperty(string name, AutomationProperty<bool> property, List<DesktopData> properties)
        {
            if (property.IsSupported)
            {
                properties.Add(new DesktopData(name, property.ValueOrDefault));
            }
        }

        private static void CheckProperty(string name, AutomationProperty<string> property, List<DesktopData> properties)
        {
            if (property.IsSupported)
            {
                properties.Add(new DesktopData(name, property.ValueOrDefault));
            }
        }
    }
}