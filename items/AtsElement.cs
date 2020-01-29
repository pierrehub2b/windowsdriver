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
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using windowsdriver.items;

[DataContract(Name = "com.ats.element.AtsElement")]
public class AtsElement
{
    private const int UNDEFINED_SIZE = 9898;

    [DataMember(Name = "id")]
    public string Id;

    [DataMember(Name = "tag")]
    public string Tag;

    [DataMember(Name = "clickable")]
    public bool Clickable = true;

    [DataMember(Name = "x")]
    public double X = 0;

    [DataMember(Name = "y")]
    public double Y = 0;

    [DataMember(Name = "width")]
    public double Width = UNDEFINED_SIZE;

    [DataMember(Name = "height")]
    public double Height = UNDEFINED_SIZE;

    [DataMember(Name = "visible")]
    public Boolean Visible = true;

    [DataMember(Name = "password")]
    public Boolean Password = false;

    [DataMember(Name = "attributes")]
    public DesktopData[] Attributes;

    public AutomationElement Element;
    public Boolean Enabled = true;

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
        Tag = tag;
        Element = elem;

        UpdateBounding(elem);

        CachedElements.Instance.Add(Id, this);
    }

    public AtsElement(AutomationElement elem, string tag, DesktopData[] attr) : this(elem, tag)
    {
        Attributes = attr;
    }

    public AtsElement(string tag, AutomationElement elem) : this(elem, tag)
    {
        try
        {
            Visible = !elem.IsOffscreen;
        }
        catch { }

        try
        {
            Password = elem.Properties.IsPassword;
        }
        catch { }

        try
        {
            Enabled = elem.IsEnabled;
        }
        catch { }
    }
    
    public AtsElement(string tag, AutomationElement elem, string[] attributes) : this(tag, elem)
    {
        Attributes = Properties.AddProperties(attributes, Password, Visible, Enabled, ref Element);
    }

    public AtsElement(AutomationElement elem) : this(GetTag(elem), elem)
    {
    }

    private void UpdateBounding(AutomationElement elem)
    {
        try
        {
            Rectangle rec = elem.BoundingRectangle;
            X = rec.X;
            Y = rec.Y;
            Width = rec.Width;
            Height = rec.Height;
        }
        catch { }
    }

    internal bool TryExpand()
    {
        if (Element.Patterns.ExpandCollapse.IsSupported)
        {
            Element.Patterns.ExpandCollapse.Pattern.Expand();
            return true;
        }
        return false;
    }

    //-----------------------------------------------------------------------------------------
    // Select
    //-----------------------------------------------------------------------------------------

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
                if (listItems.Length > index)
                {
                    SelectListItem(listItems[index]);
                }
            }
            else
            {
                //Element.Click();
                AutomationElement[] dropDownLists = Element.Automation.GetDesktop().FindAllChildren(Element.ConditionFactory.ByControlType(ControlType.List));

                if (dropDownLists.Length > 0)
                {
                    dropDownLists[0].AsListBox().Select(index);
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
            if (list != null)
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
                try {
                    AutomationElement listItem = Element.Automation.GetDesktop().FindFirst(TreeScope.Children,
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

                    return;
                }
                catch {}
                
                //last chance 

                AutomationElement[] dropDownLists = Element.Automation.GetDesktop().FindAllChildren(Element.ConditionFactory.ByControlType(ControlType.List));

                if (dropDownLists.Length > 0)
                {
                    AutomationElement listBox = dropDownLists[0];
                    AutomationElement[] items = listBox.FindAllChildren(listBox.ConditionFactory.ByControlType(ControlType.ListItem));

                    int loop = 0;

                    if (regexp)
                    {
                        Regex regex = new Regex(@text);
                        foreach (AutomationElement item in items)
                        {
                            string name = item.Name;
                            if (regex.IsMatch(name))
                            {
                                listBox.AsListBox().Select(loop);
                                break;
                            }

                            loop++;
                        }
                    }
                    else
                    {
                        
                        foreach (AutomationElement item in items)
                        {
                            string name = item.Name;
                            if (name.Equals(text))
                            {
                                listBox.AsListBox().Select(loop);
                                break;
                            }
                            loop++;
                        }
                    }
                }
            }
        }

        if (expandCollapse)
        {
            Element.Patterns.ExpandCollapse.Pattern.Collapse();
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
            Element.Click();
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
            else
            {
                Element.Click();
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

    //-----------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------

    public static string GetTag(AutomationElement elem)
    {
        try
        {
            return elem.ControlType.ToString();
        }
        catch {
            return "*";
        }
    }

    internal void Focus()
    {
        Element.Focus();
        Element.FocusNative();
    }

    //-----------------------------------------------------------------------------------------------------

    public virtual AtsElement[] GetElements(string tag, string[] attributes)
    {
        List<AtsElement> listElements = new List<AtsElement> { this };

        //---------------------------------------------------------------------
        // try to find a modal window
        //---------------------------------------------------------------------

        AutomationElement rootElement = Element;
        AutomationElement[] children = Element.FindAllChildren();

        for(int i= children.Length-1; i>=0; i--)
        {
            AutomationElement child = children[i];
            if (child.Patterns.Window.IsSupported && child.Patterns.Window.Pattern.IsModal)
            {
                rootElement = child;
                break;
            }
        }

        //---------------------------------------------------------------------
        //---------------------------------------------------------------------

        Task<AtsElement[]> task;
        
        int len = attributes.Length;

        if (len > 0)
        {
            string[] newAttributes = new string[len];
            AndCondition searchCondition = new AndCondition();

            for (int i = 0; i < len; i++)
            {
                string[] attributeData = attributes[i].Split('\t');
                if (attributeData.Length == 2)
                {
                    MethodInfo byMethod = rootElement.ConditionFactory.GetType().GetMethod("By" + attributeData[0]);
                    if (byMethod != null)
                    {
                        searchCondition = searchCondition.And((PropertyCondition)byMethod.Invoke(rootElement.ConditionFactory, new[] { attributeData[1] }));
                    }
                }
                newAttributes[i] = attributeData[0];
            }

            if ("*".Equals(tag) || string.IsNullOrEmpty(tag))
            {
                task = Task.Run(() =>
                {
                    Array.ForEach(rootElement.FindAllDescendants(searchCondition), e => listElements.Add(new AtsElement("*", e, newAttributes)));
                    return listElements.ToArray();
                });
            }
            else
            {
                task = Task.Run(() =>
                {
                    Array.ForEach(Array.FindAll(rootElement.FindAllDescendants(searchCondition), e => tag.Equals(GetTag(e), StringComparison.OrdinalIgnoreCase)), e => listElements.Add(new AtsElement(tag, e, newAttributes)));
                    return listElements.ToArray();
                });
            }
        }
        else
        {
            if ("*".Equals(tag) || string.IsNullOrEmpty(tag))
            {
                task = Task.Run(() =>
                {
                    Array.ForEach(rootElement.FindAllDescendants(), e => listElements.Add(new AtsElement(e)));
                    return listElements.ToArray();
                });
            }
            else
            {
                task = Task.Run(() =>
                {
                    Array.ForEach(Array.FindAll(rootElement.FindAllDescendants(), e => tag.Equals(GetTag(e), StringComparison.OrdinalIgnoreCase)), e => listElements.Add(new AtsElement(tag, e)));
                    return listElements.ToArray();
                });
            }
        }

        task.Wait(20000);
        return task.Result;
    }
    
    //-----------------------------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------------------------

    private static readonly string[] propertiesBase = new string[] {
                Properties.Name,
                Properties.NativeWindowHandle,
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
                Properties.IsVisible,
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
            Attributes = Properties.AddProperties(propertiesGrid, Password, Visible, Enabled, ref Element);
        }
        else
        {
            Attributes = Properties.AddProperties(propertiesBase, Password, Visible, Enabled, ref Element);
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
            AtsElement parentElement = new AtsElement(parent);
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
        public const string NativeWindowHandle = "NativeWindowHandle";
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
        public const string IsVisible = "IsVisible";
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

        public static DesktopData[] AddProperties(string[] attributes, bool isPassword, bool isVisible, bool isEnabled, ref AutomationElement element)
        {
            List<DesktopData> result = new List<DesktopData>();

            FrameworkAutomationElementBase.IProperties propertyValues = element.Properties;
            FrameworkAutomationElementBase.IFrameworkPatterns patternValues = element.Patterns;

            for(int i=0; i< attributes.Length; i++)
            {
                AddProperty(attributes[i], isPassword, isVisible, isEnabled, element, propertyValues, patternValues, ref result);
            }

            return result.ToArray();
        }

        public static void AddProperty(string propertyName, bool isPassword, bool isVisible, bool isEnabled, AutomationElement element, FrameworkAutomationElementBase.IProperties propertyValues, FrameworkAutomationElementBase.IFrameworkPatterns patternValues, ref List<DesktopData> properties)
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
                        StringBuilder sb = new StringBuilder().Append(rect.X).Append(",").Append(rect.Y).Append(",").Append(rect.Width).Append(",").Append(rect.Height);
                        properties.Add(new DesktopData(BoundingRectangle, sb.ToString()));
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

                case NativeWindowHandle:
                    if (propertyValues.NativeWindowHandle.IsSupported)
                    {
                        properties.Add(new DesktopData(propertyName, propertyValues.NativeWindowHandle.ToString()));
                    }
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
                    properties.Add(new DesktopData(propertyName, isEnabled));
                    break;
                case IsPassword:
                    properties.Add(new DesktopData(propertyName, isPassword));
                    break;
                case IsVisible:
                    properties.Add(new DesktopData(propertyName, isVisible));
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

        private static void CheckProperty(string name, AutomationProperty<string> property, ref List<DesktopData> properties)
        {
            try
            {
                properties.Add(new DesktopData(name, property.ValueOrDefault));
            }
            catch { }
            /*if (property.IsSupported)
            {
                properties.Add(new DesktopData(name, property.ValueOrDefault));
            }*/
        }
    }
}