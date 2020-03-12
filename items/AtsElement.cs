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
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
            if (elem.Properties.BoundingRectangle.IsSupported)
            {
                AccessibilityState state = elem.Patterns.LegacyIAccessible.Pattern.State.Value;
                if(state.HasFlag(AccessibilityState.STATE_SYSTEM_INVISIBLE) || state.HasFlag(AccessibilityState.STATE_SYSTEM_OFFSCREEN))
                {
                    return false;
                }
                
                Rectangle rec = elem.Properties.BoundingRectangle;
                X = rec.X;
                Y = rec.Y;
                Width = rec.Width;
                Height = rec.Height;

                return true;
            }
        }
        catch (Exception) { }

        return false;
    }

    public AtsElement(bool loadChildren, AutomationElement elem):this(elem)
    {
        if (Visible)
        {
            AutomationElement[] childs = elem.FindAllChildren();
            NumChildren = childs.Length;
            if (NumChildren > 0)
            {
                Children = new AtsElement[NumChildren];
                for (int i = 0; i < NumChildren; i++)
                {
                    Children[i] = new AtsElement(true, childs.ElementAt(i));
                }
                return;
            }
        }

        Children = new AtsElement[0];
    }

    public AtsElement(AutomationElement elem)
    {
        Element = elem;

        Attributes = new DesktopData[0];
        Visible = IsElementVisible(Element);
        
        if (Visible)
        {
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
        Thread.Sleep(150);
        Keyboard.Type(new[] { VirtualKeyShort.PRIOR });
        Keyboard.Type(new[] { VirtualKeyShort.HOME });
        Thread.Sleep(150);

        return GetSelectedItem(null, desktop);
    }

    private AutomationElement GetSelectedItem(AutomationElement current, DesktopManager desktop)
    {
        AutomationElement[] items = GetListItemElements(desktop);
        foreach (AutomationElement item in items)
        {
            if (!item.Equals(current))
            {
                try
                {
                    AccessibilityState state = item.Patterns.LegacyIAccessible.Pattern.State.Value;
                    if (state.HasFlag(AccessibilityState.STATE_SYSTEM_SELECTED))
                    {
                        return item;
                    }
                }
                catch { }

            }
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
            Thread.Sleep(150);
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
            Thread.Sleep(150);
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
                Thread.Sleep(300);
                return true;
            }
        }
        catch {}

        return false;
    }

    private bool ExpandElement()
    {
        Element.FocusNative();
        Thread.Sleep(100);

        if (Element.Patterns.ExpandCollapse.IsSupported)
        {
            Element.Patterns.ExpandCollapse.Pattern.Expand();
            Thread.Sleep(300);
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

    private const string SelectText = "SelectText(";
    private const int SelectTextLength = 11;

    private const string SelectIndex = "SelectIndex(";
    private const int SelectIndexLength = 12;
    
    private const string SelectTextItem = "SelectTextItem(";
    private const int SelectTextItemLength = 15;

    private const string SelectDataItem = "SelectDataItem(";
    private const int SelectDataItemLength = 15;

    internal DesktopData[] ExecuteScript(string script)
    {
        if (script != null)
        {
            if (script.StartsWith("Invoke()", StringComparison.OrdinalIgnoreCase) && Element.Patterns.Invoke.IsSupported)
            {
                Element.Patterns.Invoke.Pattern.Invoke();
            }
            else if (script.StartsWith("Focus()", StringComparison.OrdinalIgnoreCase))
            {
                Focus();
            }
            else if (script.StartsWith(SelectText, StringComparison.OrdinalIgnoreCase))
            {
                int closeParenthesis = script.LastIndexOf(")");
                if(closeParenthesis > SelectTextLength)
                {
                    string text = script.Substring(SelectTextLength, closeParenthesis - SelectTextLength);

                    ExpandElement();
                    Element.AsComboBox().Select(text);
                    Thread.Sleep(200);
                    Element.AsComboBox().SelectedItem.Click();
                }
            }
            else if (script.StartsWith(SelectIndex, StringComparison.OrdinalIgnoreCase))
            {
                int closeParenthesis = script.LastIndexOf(")");
                if (closeParenthesis > SelectIndexLength)
                {
                    _ = int.TryParse(script.Substring(SelectIndexLength, closeParenthesis - SelectIndexLength), out int index);

                    ExpandElement();
                    Element.AsComboBox().Select(index);
                    Thread.Sleep(200);
                    Element.AsComboBox().SelectedItem.Click();
                }
            }
            else if (script.StartsWith(SelectTextItem, StringComparison.OrdinalIgnoreCase))
            {
                int closeParenthesis = script.LastIndexOf(")");
                if (closeParenthesis > SelectTextItemLength)
                {
                    string text = script.Substring(SelectTextItemLength, closeParenthesis - SelectTextItemLength);

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
                                if (item.Name.Equals(text))
                                {
                                    listItem.ScrollIntoView();

                                    Rectangle rect = listItem.BoundingRectangle;
                                    Mouse.Position = new Point(rect.X + (rect.Width / 2), rect.Y + (rect.Height / 2));
                                    Mouse.Click();
                                }
                            }
                            break;
                        }
                        windowParent = windowParent.Parent;
                    }
                }
            }
            else if (script.StartsWith(SelectDataItem, StringComparison.OrdinalIgnoreCase))
            {
                int closeParenthesis = script.LastIndexOf(")");
                if (closeParenthesis > SelectDataItemLength)
                {
                    string text = script.Substring(SelectDataItemLength, closeParenthesis - SelectDataItemLength);
                    AutomationElement[] rows = Element.FindAllChildren();

                    foreach(AutomationElement row in rows)
                    {
                        AutomationElement[] cells = row.FindAllDescendants(row.ConditionFactory.ByControlType(ControlType.DataItem));
                        if (cells.Length > 0)
                        {
                            AutomationElement cell = cells[1];
                            cell.Focus();
                            cell.Click();
                            Thread.Sleep(50);
                            if (cell.Name.Contains(text)){

                                break;
                            }

                        }
                    }

                }
            }
            else if (script.StartsWith("SelectRowIndex(", StringComparison.OrdinalIgnoreCase))
            {

            }           
        }

        return new DesktopData[0];
    }

    //-----------------------------------------------------------------------------------------
    // Text
    //-----------------------------------------------------------------------------------------

    public bool TextClear()
    {
        Element.FocusNative();

        if(Element.Properties.IsKeyboardFocusable.IsSupported && Element.Properties.IsKeyboardFocusable)
        {
            AccessibilityState state = Element.Patterns.LegacyIAccessible.Pattern.State.Value;
            if (state.HasFlag(AccessibilityState.STATE_SYSTEM_FOCUSED))
            {
                try
                {
                    Keyboard.Type(new[] { VirtualKeyShort.SPACE });
                    Element.AsTextBox().Text = "";
                    return true;
                }
                catch { }

                try
                {
                    Keyboard.TypeSimultaneously(new[] { VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A });
                    Keyboard.Type(new[] { VirtualKeyShort.DELETE });
                }
                catch { }
                return true;
            }
        }
        return false;
    }

    //-----------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------

    public static string GetTag(AutomationElement elem)
    {
        try
        {
            return elem.Properties.ControlType.ToString();
        }catch { }
        return "*";
    }

    public static bool IsPassword(AutomationElement elem)
    {
        if (elem.Properties.IsPassword.IsSupported)
        {
            return elem.Properties.IsPassword;
        }
        return false;
    }

    public virtual void Focus()
    {
        Element.Focus();
        Element.FocusNative();
    }
    
    //-----------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------
    
    public virtual AtsElement[] GetElementsTree(DesktopManager desktop)
    {
        return new AtsElement[0];
    }

    //-----------------------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------------------

    public virtual AtsElement[] GetElements(string tag, string[] attributes, AutomationElement root, DesktopManager desktop)
    {
        return GetElements(tag, attributes, root, desktop, new AutomationElement[0]);
    }

    public virtual AtsElement[] GetElements(string tag, string[] attributes, AutomationElement root, DesktopManager desktop, AutomationElement[] popups)
    {
        List<AtsElement> listElements = new List<AtsElement> { };
        
        int len = attributes.Length;

        Task<AtsElement[]> task = Task.Run(() =>
        {
            if (len > 0)
            {
                string[] newAttributes = new string[len];
                AndCondition searchCondition = new AndCondition();

                for (int i = 0; i < len; i++)
                {
                    string[] attributeData = attributes[i].Split('\t');
                    MethodInfo byMethod = root.ConditionFactory.GetType().GetMethod("By" + attributeData[0]);

                    if (byMethod != null)
                    {
                        if (attributeData.Length == 2)
                        {
                            searchCondition = searchCondition.And((PropertyCondition)byMethod.Invoke(root.ConditionFactory, new[] { attributeData[1] }));
                        }
                    }
                    newAttributes[i] = attributeData[0];
                }

                AutomationElement[] elements = root.FindAllDescendants(searchCondition).Concat(popups).ToArray();

                if ("*".Equals(tag) || string.IsNullOrEmpty(tag))
                {
                    for (int i = 0; i < elements.Length; i++)
                    {
                        AtsElement elem = new AtsElement(elements.ElementAt(i), newAttributes);
                        if (elem.Visible)
                        {
                            listElements.Add(elem);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < elements.Length; i++)
                    {
                        AutomationElement e = elements.ElementAt(i);
                        if (tag.Equals(GetTag(e), StringComparison.OrdinalIgnoreCase))
                        {
                            AtsElement elem = new AtsElement(e, newAttributes);
                            if (elem.Visible)
                            {
                                listElements.Add(elem);
                            }
                        }
                    }
                }
            }
            else
            {
                AutomationElement[] elements = root.FindAllDescendants().Concat(popups).ToArray();

                if ("*".Equals(tag) || string.IsNullOrEmpty(tag))
                {
                    for (int i = 0; i < elements.Length; i++)
                    {
                            AtsElement elem = new AtsElement(elements.ElementAt(i));
                            if (elem.Visible)
                            {
                                listElements.Add(elem);
                            }
                    }
                }
                else
                {
                    for (int i = 0; i < elements.Length; i++)
                    {
                        AutomationElement e = elements.ElementAt(i);
                        if (tag.Equals(GetTag(e), StringComparison.OrdinalIgnoreCase))
                        {
                            AtsElement elem = new AtsElement(e);
                            if (elem.Visible)
                            {
                                listElements.Add(elem);
                            }
                        }
                    }
                }
            }
            return listElements.ToArray();
        });
               
        task.Wait(40000);
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

        public static DesktopData[] AddProperties(string[] attributes, bool isPassword, bool isVisible, AutomationElement element)
        {
            List<DesktopData> result = new List<DesktopData>();

            FrameworkAutomationElementBase.IProperties propertyValues = element.Properties;
            FrameworkAutomationElementBase.IFrameworkPatterns patternValues = element.Patterns;

            for(int i=0; i< attributes.Length; i++)
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