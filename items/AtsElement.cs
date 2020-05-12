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
using FlaUI.Core.WindowsAPI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using windowsdriver;
using windowsdriver.items;

[DataContract(Name = "com.ats.element.AtsElement")]
public class AtsElement
{
    [DataMember(Name = "id")]
    public string Id;

    [DataMember(Name = "tag")]
    public string Tag = "*";

    [DataMember(Name = "clickable")]
    public bool Clickable = true;

    [DataMember(Name = "x")]
    public double X = 0;

    [DataMember(Name = "y")]
    public double Y = 0;

    [DataMember(Name = "width")]
    public double Width = -1;

    [DataMember(Name = "height")]
    public double Height = -1;

    [DataMember(Name = "visible")]
    public Boolean Visible = false;

    [DataMember(Name = "password")]
    public Boolean Password = false;

    [DataMember(Name = "numChildren")]
    public int NumChildren = 0;

    [DataMember(Name = "attributes")]
    public DesktopData[] Attributes;

    [DataMember(Name = "children")]
    public AtsElement[] Children;

    public AutomationElement Element;

    //private readonly string innerText = "";

    public virtual void Dispose()
    {
        Element = null;
        Attributes = null;
        Children = null;
    }

    ~AtsElement()
    {
        Dispose();
    }

    private bool IsElementVisible(AutomationElement elem)
    {
        try
        {
            Rectangle rec = elem.Properties.BoundingRectangle;

            AccessibilityState state = elem.Patterns.LegacyIAccessible.Pattern.State.Value;
                if (state.HasFlag(AccessibilityState.STATE_SYSTEM_INVISIBLE))
                {
                    return false;
                }
                               
                X = rec.X;
                Y = rec.Y;
                Width = rec.Width;
                Height = rec.Height;

                return true;
        }
        catch (Exception) { }

        return false;
    }

    public AtsElement(DesktopManager desktop, AutomationElement elem) : this(elem)
    {
        if (Visible)
        {
            AutomationElement[] childs = elem.FindAllChildren(desktop.NotOffScreenProperty);
            NumChildren = childs.Length;
            if (NumChildren > 0)
            {
                Children = new AtsElement[NumChildren];
                for (int i = 0; i < NumChildren; i++)
                {
                    Children[i] = new AtsElement(desktop, childs.ElementAt(i));
                }

                Array.Clear(childs, 0, childs.Length);
                return;
            }
        }

        Children = new AtsElement[0];
    }

    public AtsElement(AutomationElement elem)
    {
        Element = elem;
        Attributes = new DesktopData[0];

        if (IsElementVisible(Element))
        {
            Visible = true;
            Id = Guid.NewGuid().ToString();
            Tag = GetTag(Element);
            Password = IsPassword(Element);
            CachedElements.Instance.Add(Id, this);
        }
    }

    public AtsElement(AutomationElement elem, string[] attributes) : this(elem)
    {
        Attributes = Properties.AddProperties(attributes, Password, Visible, elem);
    }

    public AtsElement[] GetListItems(DesktopManager desktop)
    {
        AutomationElement[] listItems = Element.FindAllDescendants(Element.ConditionFactory.ByControlType(ControlType.ListItem));

        int len = listItems.Length;
        if (len > 0)
        {
            AtsElement[] result = new AtsElement[len];
            for (int i = 0; i < len; i++)
            {
                result[i] = new AtsElement(listItems[i]);
            }
            return result;
        }
        return desktop.GetPopupListItems();
    }

    public AutomationElement[] GetListItemElements(DesktopManager desktop)
    {
        AutomationElement[] listItems = Element.FindAllDescendants(Element.ConditionFactory.ByControlType(ControlType.ListItem));

        int len = listItems.Length;
        if (len > 0)
        {
            return listItems;
        }
        return desktop.GetPopupListItemElements();
    }

    public void LoadListItemAttributes()
    {
        Attributes = new DesktopData[2];
        Attributes[0] = new DesktopData("text");
        Attributes[1] = new DesktopData("value");

        try
        {
            Attributes[0].SetValue(TruncString(Element.Name, 52));
            Attributes[1].SetValue(TruncString(Element.Patterns.LegacyIAccessible.Pattern.Value, 52));
        }
        catch { }
    }

    private string TruncString(string myStr, int THRESHOLD)
    {
        if (myStr.Length > THRESHOLD)
            return myStr.Substring(0, THRESHOLD) + "...";
        return myStr;
    }

    //-----------------------------------------------------------------------------------------
    // Select
    //-----------------------------------------------------------------------------------------

    private AutomationElement SelectFirstItem(DesktopManager desktop)
    {
        ExpandElement();
        Thread.Sleep(100);
        Keyboard.Type(new[] { VirtualKeyShort.PRIOR, VirtualKeyShort.HOME });

        return GetSelectedItem(null, desktop);
    }

    private AutomationElement GetSelectedItem(AutomationElement current, DesktopManager desktop)
    {
        Thread.Sleep(136);

        AutomationElement[] items = GetListItemElements(desktop);
        foreach (AutomationElement item in items)
        {
            try
            {
                AccessibilityState state = item.Patterns.LegacyIAccessible.Pattern.State.Value;
                if (state.HasFlag(AccessibilityState.STATE_SYSTEM_SELECTED))
                {
                    if (item.Equals(current))
                    {
                        return null;
                    }
                    return item;
                }
            }
            catch { }
        }
        return null;
    }

    internal void SelectItem(int index, DesktopManager desktop)
    {
        AutomationElement currentItem = SelectFirstItem(desktop);

        int currentIndex = 0;
        while (currentItem != null)
        {
            if (currentIndex == index)
            {
                Keyboard.Type(VirtualKeyShort.ENTER);
                break;
            }

            Keyboard.Type(VirtualKeyShort.DOWN);
            currentItem = GetSelectedItem(currentItem, desktop);

            currentIndex++;
        }
    }

    internal void SelectItem(Predicate<AutomationElement> predicate, DesktopManager desktop)
    {
        AutomationElement currentItem = SelectFirstItem(desktop);

        while (currentItem != null)
        {
            if (predicate(currentItem))
            {
                Keyboard.Type(VirtualKeyShort.ENTER);
                break;
            }

            Keyboard.Type(VirtualKeyShort.DOWN);
            currentItem = GetSelectedItem(currentItem, desktop);
        }
    }

    internal bool TryExpand()
    {
        Element.FocusNative();
        Thread.Sleep(100);

        try
        {
            if (Element.Patterns.ExpandCollapse.IsSupported)
            {
                Element.Patterns.ExpandCollapse.Pattern.Expand();
                Thread.Sleep(140);
                return true;
            }
        }
        catch { }

        return false;
    }

    private bool ExpandElement()
    {
        Element.FocusNative();
        Thread.Sleep(100);

        if (Element.Patterns.ExpandCollapse.IsSupported)
        {
            Element.Patterns.ExpandCollapse.Pattern.Expand();
            Thread.Sleep(140);
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

    //-----------------------------------------------------------------------------------------
    // Script
    //-----------------------------------------------------------------------------------------

    private static readonly ExecuteFunction FocusFunction = new ExecuteFunction("Focus");
    private static readonly ExecuteFunction InvokeFunction = new ExecuteFunction("Invoke");
    private static readonly ExecuteFunction SelectTextFunction = new ExecuteFunction("SelectText");
    private static readonly ExecuteFunction SelectIndexFunction = new ExecuteFunction("SelectIndex");
    private static readonly ExecuteFunction SelectTextItemFunction = new ExecuteFunction("SelectTextItem");
    private static readonly ExecuteFunction SelectDataItemFunction = new ExecuteFunction("SelectDataItem");

    private class ExecuteFunction
    {
        private string value;
        private readonly string name;
        private readonly int nameLength = 0;

        public ExecuteFunction(string n)
        {
            name = n + "(";
            nameLength = n.Length + 1;
        }

        public bool Match(string data)
        {
            if (data.StartsWith(name, StringComparison.OrdinalIgnoreCase))
            {
                int closeParenthesis = data.LastIndexOf(")");
                if (closeParenthesis >= nameLength)
                {
                    value = data.Substring(nameLength, closeParenthesis - nameLength);
                    return true;
                }
            }
            return false;
        }

        public string ValueAsString()
        {
            return value;
        }

        public int ValueAsInt()
        {
            _ = int.TryParse(value, out int index);
            return index;
        }
    }

    internal DesktopData[] ExecuteScript(string script)
    {
        if (script != null)
        {
            if (InvokeFunction.Match(script))
            {
                Invoke();
            }
            else if (FocusFunction.Match(script))
            {
                Focus();
            }
            else if (SelectTextFunction.Match(script))
            {
                SelectText(SelectTextFunction.ValueAsString());
            }
            else if (SelectIndexFunction.Match(script))
            {
                SelectText(SelectTextFunction.ValueAsInt());
            }
            else if (SelectTextItemFunction.Match(script))
            {
                string text = SelectTextItemFunction.ValueAsString();

                AutomationElement windowParent = Element.Parent;
                while (windowParent != null)
                {
                    if (windowParent.Properties.ControlType.IsSupported && windowParent.ControlType.Equals(ControlType.Window))
                    {
                        ExpandElement();
                        AutomationElement[] listItems = windowParent.FindAllDescendants(windowParent.ConditionFactory.ByControlType(ControlType.ListItem));

                        foreach (AutomationElement item in listItems)
                        {
                            ListBoxItem listItem = item.AsListBoxItem();
                            if (listItem.Name.Equals(text))
                            {
                                listItem.ScrollIntoView();

                                Rectangle rect = listItem.BoundingRectangle;
                                Mouse.Position = new Point(rect.X + (rect.Width / 2), rect.Y + (rect.Height / 2));
                                Mouse.Click();

                                break;
                            }
                        }
                        break;
                    }
                    windowParent = windowParent.Parent;
                }
            }
            else if (SelectDataItemFunction.Match(script))
            {
                string text = SelectDataItemFunction.ValueAsString();
                AutomationElement[] rows = Element.FindAllChildren();

                foreach (AutomationElement row in rows)
                {
                    AutomationElement[] cells = row.FindAllDescendants(row.ConditionFactory.ByControlType(ControlType.DataItem));
                    if (cells.Length > 0)
                    {
                        AutomationElement cell = cells[1];
                        cell.Focus();
                        cell.Click();
                        Thread.Sleep(50);
                        if (cell.Name.Contains(text))
                        {

                            break;
                        }
                    }
                }
            }
        }

        return new DesktopData[0];
    }

    public virtual void Focus()
    {
        Element.Focus();
        Element.FocusNative();
    }

    public virtual void Invoke()
    {
        if (Element.Patterns.Invoke.IsSupported)
        {
            Element.Patterns.Invoke.Pattern.Invoke();
        }
    }

    public virtual void SelectText(string value)
    {
        ExpandElement();
        Element.AsComboBox().Select(value);
        Thread.Sleep(140);
        Element.AsComboBox().SelectedItem.Click();
    }

    public virtual void SelectText(int index)
    {
        ExpandElement();
        Element.AsComboBox().Select(index);
        Thread.Sleep(140);
        Element.AsComboBox().SelectedItem.Click();
    }

    //-----------------------------------------------------------------------------------------
    // Text
    //-----------------------------------------------------------------------------------------

    public void ElementFocus()
    {
        Element.FocusNative();
    }

    public void TextClear()
    {
        Element.FocusNative();

        try
        {
            Element.AsTextBox().Text = "";
            Keyboard.Type(new[] { VirtualKeyShort.SPACE, VirtualKeyShort.BACK });
            return;
        }
        catch { }

        try
        {
            Keyboard.TypeSimultaneously(new[] { VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A });
            Keyboard.Type(new[] { VirtualKeyShort.SPACE, VirtualKeyShort.BACK });
        }
        catch { }
               
        /*if (Element.Properties.IsKeyboardFocusable.IsSupported && Element.Properties.IsKeyboardFocusable)
        {
            
            AccessibilityState state = Element.Patterns.LegacyIAccessible.Pattern.State.Value;
            if (state.HasFlag(AccessibilityState.STATE_SYSTEM_FOCUSED))
            {
                try
                {
                    Keyboard.Type(new[] { VirtualKeyShort.SPACE, VirtualKeyShort.BACK });
                }
                catch { }
            }
        }*/
    }

    //-----------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------

    public static string GetTag(AutomationElement elem)
    {
        try
        {
            return elem.Properties.ControlType.ToString();
        }
        catch { }
        return "Unknown";
    }

    public static bool IsPassword(AutomationElement elem)
    {
        try
        {
            return elem.Properties.IsPassword;
        }
        catch { }
        return false;
    }

    //-----------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------

    public virtual AtsElement[] GetElementsTree(DesktopManager desktop)
    {
        return new AtsElement[0];
    }

    //-----------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------

    public virtual Queue<AtsElement> GetElements(string tag, string[] attributes, AutomationElement root, DesktopManager desktop)
    {
        return GetElements(tag, attributes, root, desktop, new Stack<AutomationElement>());
    }
    
    private static PropertyCondition NotOffScreenProperty;

    public virtual Queue<AtsElement> GetElements(string tag, string[] attributes, AutomationElement root, DesktopManager desktop, Stack<AutomationElement> elements)
    {
        NotOffScreenProperty = desktop.NotOffScreenProperty;

        if ("*".Equals(tag) || string.IsNullOrEmpty(tag))
        {
            LoadDescendants(elements, root);
        }
        else
        {
            LoadDescendantsByControleType(elements, root, tag);
        }
        
        Queue<AtsElement> listElements = new Queue<AtsElement> { };
        int len = attributes.Length;
        
        if (len > 0)
        {
            string[] newAttributes = new string[len];
            for (int i = 0; i < len; i++)
            {
                string[] attributeData = attributes[i].Split('\t');
                newAttributes[i] = attributeData[0];
            }

            while (elements.Count > 0)
            {
                AddAtsElement(listElements, new AtsElement(elements.Pop(), newAttributes));
            }
        }
        else
        {
            while (elements.Count > 0)
            {
                AddAtsElement(listElements, new AtsElement(elements.Pop()));
            }
        }

        return listElements;
    }

    private static void AddAtsElement(Queue<AtsElement> list, AtsElement element)
    {
        if (element.Visible)
        {
            list.Enqueue(element);
        }
    }

    public static void LoadDescendants(PropertyCondition property, Stack<AutomationElement> items, AutomationElement root)
    {
        NotOffScreenProperty = property;
        ChildWalker(items, root);
    }

    public static void LoadDescendants(Stack<AutomationElement> items, AutomationElement root)
    {
        ChildWalker(items, root);
    }

    private static void LoadDescendantsByControleType(Stack<AutomationElement> items, AutomationElement root, String tag)
    {
        if(tag.Equals("Unknown"))
        {
            UndefinedChildWalker(items, root);
        }
        else
        {
            ControlType type;
            try
            {
                type = (ControlType)Enum.Parse(typeof(ControlType), tag);
            }
            catch
            {
                return;
            }

            ChildWalker(items, root, type);
        }
    }

    private static void ChildWalker(Stack<AutomationElement> list, AutomationElement parent, ControlType type)
    {
        AutomationElement[] children = parent.FindAllChildren(NotOffScreenProperty);
        for (int i = 0; i < children.Length; i++)
        {
            try
            {
                if (children[i].ControlType.Equals(type))
                {
                    list.Push(children[i]);
                }
            }
            catch { }
                       
            ChildWalker(list, children[i], type);
        }
        Array.Clear(children, 0, children.Length);
    }

    private static void UndefinedChildWalker(Stack<AutomationElement> list, AutomationElement parent)
    {
        AutomationElement[] children = parent.FindAllChildren(NotOffScreenProperty);
        for (int i = 0; i < children.Length; i++)
        {
            if (!children[i].Properties.ControlType.IsSupported)
            {
                list.Push(children[i]);
            }
            UndefinedChildWalker(list, children[i]);
        }
        Array.Clear(children, 0, children.Length);
    }

    static void ChildWalker(Stack<AutomationElement> list, AutomationElement parent)
    {
        AutomationElement[] children = parent.FindAllChildren(NotOffScreenProperty);
        for (int i = 0; i < children.Length; i++)
        {
            list.Push(children[i]);
            ChildWalker(list, children[i]);
        }
        Array.Clear(children, 0, children.Length);
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
            Attributes = Properties.AddProperties(propertiesGrid, Password, Visible, Element);
        }
        else
        {
            Attributes = Properties.AddProperties(propertiesBase, Password, Visible, Element);
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

    /*public void AddInnerText(string value)
    {
        //TODO inner text
    }*/

    //-----------------------------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------------------------

    internal AtsElement[] GetParents()
    {
        LoadProperties();
        Stack<AtsElement> parents = new Stack<AtsElement>();

        AutomationElement parent = Element.Parent;
        while (parent != null)
        {
            AtsElement parentElement = new AtsElement(parent);
            parentElement.LoadProperties();

            parents.Push(parentElement);
            parent = parent.Parent;
        }

        parents.Pop();// remove Windows desktop
        parents.Pop();// remove main windows of application

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

        public static DesktopData[] AddProperties(string[] attributes, bool isPassword, bool isVisible, AutomationElement element)
        {
            List<DesktopData> result = new List<DesktopData>();

            FrameworkAutomationElementBase.IProperties propertyValues = element.Properties;
            FrameworkAutomationElementBase.IFrameworkPatterns patternValues = element.Patterns;

            for (int i = 0; i < attributes.Length; i++)
            {
                AddProperty(attributes[i], isPassword, isVisible, element, propertyValues, patternValues, ref result);
            }

            return result.ToArray();
        }

        public static void AddProperty(string propertyName, bool isPassword, bool isVisible, AutomationElement element, FrameworkAutomationElementBase.IProperties propertyValues, FrameworkAutomationElementBase.IFrameworkPatterns patternValues, ref List<DesktopData> properties)
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
                    properties.Add(new DesktopData(propertyName, element.Properties.IsEnabled));
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