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
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.Shapes;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

[DataContract(Name = "com.ats.element.AtsElement")]
public class AtsElement
{
    [DataMember(Name = "id")]
    public string Id;

    [DataMember(Name = "tag")]
    public string Tag;

    [DataMember(Name = "clickable")]
    public bool Clickable;

    [DataMember(Name = "x")]
    public double X;

    [DataMember(Name = "y")]
    public double Y;

    [DataMember(Name = "width")]
    public double Width;

    [DataMember(Name = "height")]
    public double Height;

    [DataMember(Name = "visible")]
    public Boolean Visible;

    [DataMember(Name = "password")]
    public Boolean Password;

    [DataMember(Name = "attributes")]
    public DesktopData[] Attributes;

    public AutomationElement Element;

    //private readonly string innerText = "";

    public virtual void Dispose()
    {
        Element = null;
        Attributes = null;
    }

    ~AtsElement()
    {
        Dispose();
    }

    public AtsElement(AutomationElement elem, string tag)
    {
        Id = Guid.NewGuid().ToString();
        Element = elem;
        Tag = tag;
        Attributes = new DesktopData[0];

        UpdateBounding(elem);
    }

    public AtsElement(string tag, AutomationElement elem, string[] attributes) : this(tag, elem)
    {
        Attributes = Properties.AddProperties(attributes, Password, ref Element);
    }
   
    public AtsElement(AutomationElement elem) : this(GetTag(elem), elem)
    {
    }

    public AtsElement(AutomationElement elem, bool clic) : this(elem)
    {
        Clickable = clic;
    }
    
    public AtsElement(string tag, AutomationElement elem)
    {
        Id = Guid.NewGuid().ToString();
        Tag = tag;
        Element = elem;
        
        if (elem.Properties.IsOffscreen.IsSupported)
        {
            Visible = !elem.Properties.IsOffscreen.Value;
        }
        else
        {
            Visible = true;
        }

        if (elem.Properties.IsPassword.IsSupported)
        {
            Password = elem.Properties.IsPassword.Value;
        }
        else
        {
            Password = false;
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

        X = rec.X;
        Y = rec.Y;
        Width = rec.Width;
        Height = rec.Height;
    }

    internal void SelectIndex(int index)
    {
        bool expandCollapse = ExpandElement();

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
        else
        {
            AutomationElement list = Element.FindFirstChild(Element.ConditionFactory.ByControlType(ControlType.List));
            if (list != null)
            {
                AutomationElement[] listItems = list.FindAllChildren();
                if(listItems.Length > index)
                {
                    SelectListItem(listItems[index]);
                }
            }
            else
            {
                AutomationElement listItem = Element.Automation.GetDesktop().FindFirst(
                    TreeScope.Children, 
                    new AndCondition(
                        Element.ConditionFactory.ByControlType(ControlType.Pane), 
                        Element.ConditionFactory.ByClassName(Element.ClassName)));
                
                if(listItem != null)
                {
                    AutomationElement[] items = listItem.FindAllChildren();
                    if(items.Length > index)
                    {
                        ClickListItem(items[index]);
                    }
                }
            }
        }

        if (expandCollapse)
        {
            Element.Patterns.ExpandCollapse.Pattern.Collapse();
        }
    }

    internal void SelectText(string text, bool regexp)
    {
        bool expandCollapse = ExpandElement();

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
                        SelectListItem(item);
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
                        SelectListItem(item);
                        break;
                    }
                }
            }
        }
        else
        {
            AutomationElement list = Element.FindFirstChild(Element.ConditionFactory.ByControlType(ControlType.List));
            if(list != null)
            {
                AutomationElement[] listItems = list.FindAllChildren();
                if (regexp)
                {
                    Regex regex = new Regex(@text);
                    foreach (AutomationElement item in listItems)
                    {
                        if (regex.IsMatch(item.Name))
                        {
                            SelectListItem(item);
                            break;
                        }
                    }
                }
                else
                {
                    foreach (AutomationElement item in listItems)
                    {
                        if (item.Name.Equals(text))
                        {
                            SelectListItem(item);
                            break;
                        }
                    }
                }
            }
            else
            {
                AutomationElement listItem = Element.Automation.GetDesktop().FindFirst(
                    TreeScope.Children,
                    new AndCondition(Element.ConditionFactory.ByControlType(ControlType.Pane),
                    Element.ConditionFactory.ByClassName(Element.ClassName)));

                if (listItem != null)
                {
                    AutomationElement[] items = listItem.FindAllChildren();
                    if (regexp)
                    {
                        Regex regex = new Regex(@text);
                        foreach (AutomationElement item in items)
                        {
                            if (regex.IsMatch(item.Name))
                            {
                                ClickListItem(item);
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (AutomationElement item in items)
                        {
                            if (item.Name.Equals(text))
                            {
                                ClickListItem(item);
                                break;
                            }
                        }
                    }
                }
            }
        }
        /*else if (Element.Patterns.Value.IsSupported)
        {
            Element.Patterns.Value.Pattern.SetValue(text);
        }*/

        if (expandCollapse)
        {
            
        }
    }

    internal void SelectValue(string value)
    {
        bool expandCollapse = ExpandElement();

        if (Element.Patterns.Value.IsSupported)
        {
            Element.Patterns.Value.Pattern.SetValue(value);
        }

        if (expandCollapse)
        {
            Element.Patterns.ExpandCollapse.Pattern.Collapse();
        }
    }

    private bool ExpandElement()
    {
        Element.FocusNative();
 
        if (Element.Patterns.ExpandCollapse.IsSupported)
        {
            Element.Patterns.ExpandCollapse.Pattern.Expand();
            return true;
        }
        else
        {
            AutomationElement dropDown = Element.FindFirstChild(
                        new OrCondition(
                            Element.ConditionFactory.ByControlType(ControlType.Button),
                            Element.ConditionFactory.ByControlType(ControlType.SplitButton)));

            if (dropDown != null)
            {
                Mouse.Position = dropDown.GetClickablePoint();
                Mouse.LeftClick();
            }
        }

        return false;
    }

    private void ClickListItem(AutomationElement item)
    {
        item.FocusNative();

        if (item.Patterns.Invoke.IsSupported)
        {
            item.Patterns.Invoke.Pattern.Invoke();
        }

        Rectangle rect = item.BoundingRectangle;

        Mouse.Position = new Point(rect.X + (rect.Width / 2), rect.Y + (rect.Height / 2));
        Mouse.LeftClick();
    }

    private void SelectListItem(AutomationElement item)
    {
        if (item.Patterns.ScrollItem.IsSupported)
        {
            item.Patterns.ScrollItem.Pattern.ScrollIntoView();
        }
        item.FocusNative();
        item.Click();
    }

    public static string GetTag(AutomationElement elem)
    {
        string tag;
        if (elem.Properties.ControlType.IsSupported)
        {
            tag = elem.Properties.ControlType.ValueOrDefault.ToString();
            if (!string.IsNullOrEmpty(tag))
            {
                return tag;
            }
        }

        if (elem.Properties.ClassName.IsSupported)
        {
            tag = elem.Properties.ClassName;
            if (!string.IsNullOrEmpty(tag))
            {
                return tag;
            }
        }
        return "*";
    }
    
    internal void Focus()
    {
        Element.Focus();
        Element.FocusNative();
    }
    
    //-----------------------------------------------------------------------------------------------------

    internal AtsElement[] GetElements(string tag, string[] attributes)
    {
        List<AtsElement> listElements = new List<AtsElement>
        {
            this
        };

        AutomationElement[] uiElements = Element.FindAllDescendants();

        if ("*".Equals(tag) || tag.Length == 0)
        {
            if (attributes.Length > 0)
            {
                foreach (AutomationElement element in uiElements)
                {
                    AtsElement atsElement = new AtsElement("*", element, attributes);
                    CachedElement.AddCachedElement(atsElement);
                    listElements.Add(atsElement);
                }
            }
            else
            {
                foreach (AutomationElement element in uiElements)
                {
                    //bool clickable = element.TryGetClickablePoint(out Point pt);
                    listElements.Add(CachedElement.CreateCachedElement(element, true));
                }
            }
        }
        else
        {
            if (attributes.Length > 0)
            {
                foreach (AutomationElement element in uiElements)
                {
                    string elemTag = GetTag(element);
                    if (elemTag.Equals(tag))
                    {
                        AtsElement atsElement = new AtsElement(elemTag, element, attributes);
                        CachedElement.AddCachedElement(atsElement);
                        listElements.Add(atsElement);
                    }
                }
            }
            else
            {
                foreach (AutomationElement element in uiElements)
                {
                    string elemTag = GetTag(element);
                    if (elemTag.Equals(tag))
                    {
                        AtsElement atsElement = new AtsElement(elemTag, element);
                        CachedElement.AddCachedElement(atsElement);
                        listElements.Add(atsElement);
                    }
                }
            }
        }

        Array.Clear(uiElements, 0, uiElements.Length);

        return listElements.ToArray();
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
                Properties.DateTime,
                Properties.ChildId,
                Properties.Description,
                Properties.Help,
                Properties.BoundingX,
                Properties.BoundingY,
                Properties.BoundingWidth,
                Properties.BoundingHeight,
                Properties.BoundingRectangle};

    private static readonly string[] propertiesGrid = new List<string>(propertiesBase) { Properties.RowCount, Properties.ColumnCount }.ToArray();

    internal void LoadProperties()
    {
        if ("Grid".Equals(Tag))
        {
            Attributes = Properties.AddProperties(propertiesGrid, Password, ref Element);
        }
        else
        {
            Attributes = Properties.AddProperties(propertiesBase, Password, ref Element);
        }
    }

    internal DesktopData[] GetProperty(string name)
    {
        foreach (DesktopData data in Attributes)
        {
            if (data.Name.Equals(name))
            {
                return new DesktopData[] { data };
            }
        }
        return new DesktopData[0];
    }

    internal DesktopData[] ExecuteScript(string script)
    {
        if (script.StartsWith("Invoke()", StringComparison.OrdinalIgnoreCase) && Element.Patterns.Invoke.IsSupported)
        {
            Element.Patterns.Invoke.Pattern.Invoke();
        }
        return new DesktopData[0];
    }

    /*public void AddInnerText(string value)
    {
        //TODO inner text
    }*/

    //-----------------------------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------------------------

    internal AtsElement[] GetParents()
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

        return parents.ToArray();
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
        public const string ChildId = "ChildId";
        public const string Description = "Description";
        public const string Help = "Help";
        public const string BoundingX = "BoundingX";
        public const string BoundingY = "BoundingY";
        public const string BoundingWidth = "BoundingWidth";
        public const string BoundingHeight = "BoundingHeight";
        public const string BoundingRectangle = "BoundingRectangle";

        public static DesktopData[] AddProperties(string[] attributes, bool isPassword, ref AutomationElement element)
        {
            List<DesktopData> result = new List<DesktopData>();

            AutomationElementPropertyValues propertyValues = element.Properties;
            AutomationElementPatternValuesBase patternValues = element.Patterns;

            foreach (string a in attributes)
            {
                AddProperty(a, isPassword, element, propertyValues, patternValues, ref result);
            }

            return result.ToArray();
        }

        public static void AddProperty(string propertyName, bool isPassword, AutomationElement element, AutomationElementPropertyValues propertyValues, AutomationElementPatternValuesBase patternValues, ref List<DesktopData> properties)
        {
            switch (propertyName)
            {
                case BoundingX:
                case BoundingY:
                case BoundingWidth:
                case BoundingHeight:
                case BoundingRectangle:
                    Rectangle rect = element.BoundingRectangle;
                    if (BoundingRectangle.Equals(propertyName))
                    {
                        properties.Add(new DesktopData(BoundingRectangle, rect.X + "," + rect.Y + "," + rect.Width + "," + rect.Height));
                    }
                    else if (BoundingX.Equals(propertyName))
                    {
                        properties.Add(new DesktopData(BoundingX, rect.X));
                    }
                    else if (BoundingY.Equals(propertyName))
                    {
                        properties.Add(new DesktopData(BoundingY, rect.Y));
                    }
                    else if (BoundingWidth.Equals(propertyName))
                    {
                        properties.Add(new DesktopData(BoundingWidth, rect.Width));
                    }
                    else if (BoundingHeight.Equals(propertyName))
                    {
                        properties.Add(new DesktopData(BoundingHeight, rect.Height));
                    }
                    break;
                case Name:
                    string value = "";
                    if (propertyValues.Name.IsSupported)
                    {
                        value = propertyValues.Name.Value;
                    }

                    if (value == null && patternValues.LegacyIAccessible.IsSupported)
                    {
                        value = patternValues.LegacyIAccessible.Pattern.Name.Value;
                    }
                    properties.Add(new DesktopData(propertyName, value));
                    break;
                case AutomationId:
                    CheckProperty(propertyName, propertyValues.AutomationId, ref properties);
                    break;
                case ClassName:
                    CheckProperty(propertyName, propertyValues.ClassName, ref properties);
                    break;
                case HelpText:
                    CheckProperty(propertyName, propertyValues.HelpText, ref properties);
                    break;
                case ItemStatus:
                    CheckProperty(propertyName, propertyValues.ItemStatus, ref properties);
                    break;
                case ItemType:
                    CheckProperty(propertyName, propertyValues.ItemType, ref properties);
                    break;
                case AriaRole:
                    CheckProperty(propertyName, propertyValues.AriaRole, ref properties);
                    break;
                case AriaProperties:
                    CheckProperty(propertyName, propertyValues.AriaProperties, ref properties);
                    break;
                case AcceleratorKey:
                    CheckProperty(propertyName, propertyValues.AcceleratorKey, ref properties);
                    break;
                case AccessKey:
                    CheckProperty(propertyName, propertyValues.AccessKey, ref properties);
                    break;
                case IsEnabled:
                    properties.Add(new DesktopData(propertyName, isPassword));
                    break;
                case IsPassword:
                    CheckProperty(propertyName, propertyValues.IsPassword, ref properties);
                    break;
                case RowCount:
                    properties.Add(new DesktopData(propertyName, element.AsGrid().RowCount));
                    break;
                case ColumnCount:
                    properties.Add(new DesktopData(propertyName, element.AsGrid().ColumnCount));
                    break;
                case Text:
                    if (patternValues.Text2.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patternValues.Text2.Pattern.DocumentRange.GetText(999999999)));
                    }
                    else if (patternValues.Text.IsSupported)
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
                    else if (patternValues.LegacyIAccessible.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patternValues.LegacyIAccessible.Pattern.Value.ValueOrDefault));
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
                    if (patternValues.Annotation.IsSupported && patternValues.Annotation.Pattern.AnnotationTypeName.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patternValues.Annotation.Pattern.AnnotationTypeName));
                    }
                    break;
                case Author:
                    if (patternValues.Annotation.IsSupported && patternValues.Annotation.Pattern.Author.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patternValues.Annotation.Pattern.Author));
                    }
                    break;
                case DateTime:
                    if (patternValues.Annotation.IsSupported && patternValues.Annotation.Pattern.DateTime.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patternValues.Annotation.Pattern.DateTime));
                    }
                    break;
                case ChildId:
                    if (patternValues.LegacyIAccessible.IsSupported && patternValues.LegacyIAccessible.Pattern.ChildId.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patternValues.LegacyIAccessible.Pattern.ChildId));
                    }
                    break;
                case Description:
                    if (patternValues.LegacyIAccessible.IsSupported && patternValues.LegacyIAccessible.Pattern.Description.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patternValues.LegacyIAccessible.Pattern.Description));
                    }
                    break;
                case Help:
                    if (patternValues.LegacyIAccessible.IsSupported && patternValues.LegacyIAccessible.Pattern.Help.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, patternValues.LegacyIAccessible.Pattern.Help));
                    }
                    break;
            }
        }

        private static void CheckProperty(string name, AutomationProperty<bool> property, ref List<DesktopData> properties)
        {
            if (property.IsSupported)
            {
                properties.Add(new DesktopData(name, property.ValueOrDefault));
            }
        }

        private static void CheckProperty(string name, AutomationProperty<string> property, ref List<DesktopData> properties)
        {
            if (property.IsSupported)
            {
                properties.Add(new DesktopData(name, property.ValueOrDefault));
            }
        }
    }
}