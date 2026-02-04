using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Accessibility;

namespace Mars.Inter.MQCenter.MSAASupport
{
    public sealed class MARSAccessibleConstans
    {

        public const int ROLE_SYSTEM_TITLEBAR = 0x1;
        public const int ROLE_SYSTEM_MENUBAR = 0x2;
        public const int ROLE_SYSTEM_SCROLLBAR = 0x3;
        public const int ROLE_SYSTEM_GRIP = 0x4;
        public const int ROLE_SYSTEM_SOUND = 0x5;
        public const int ROLE_SYSTEM_CURSOR = 0x6;
        public const int ROLE_SYSTEM_CARET = 0x7;
        public const int ROLE_SYSTEM_ALERT = 0x8;  
        public const int ROLE_SYSTEM_WINDOW = 0x9;
        public const int ROLE_SYSTEM_CLIENT = 0x0A;
        public const int ROLE_SYSTEM_MENUPOPUP = 0xb;
        public const int ROLE_SYSTEM_MENUITEM = 0xc;
        public const int ROLE_SYSTEM_TOOLTIP = 0xd;
        public const int ROLE_SYSTEM_APPLICATION = 0xe;
        public const int ROLE_SYSTEM_DOCUMENT = 0xf;
        public const int ROLE_SYSTEM_PANE = 0x10;
        public const int ROLE_SYSTEM_CHART = 0x11;
        public const int ROLE_SYSTEM_DIALOG = 0x12;
        public const int ROLE_SYSTEM_BORDER = 0x13;
        public const int ROLE_SYSTEM_GROUPING = 0x14;
        public const int ROLE_SYSTEM_SEPARATOR = 0x15;
        public const int ROLE_SYSTEM_TOOLBAR = 0x16;
        public const int ROLE_SYSTEM_STATUSBAR = 0x17;
        public const int ROLE_SYSTEM_TABLE = 0x18;  // propertyGridView
        public const int ROLE_SYSTEM_COLUMNHEADER = 0x19;
        public const int ROLE_SYSTEM_ROWHEADER = 0x1A;
        public const int ROLE_SYSTEM_COLUMN = 0x1b;
        public const int ROLE_SYSTEM_ROW = 0x1c;
        public const int ROLE_SYSTEM_CELL = 0x1d;
        public const int ROLE_SYSTEM_LINK = 0x1e;
        public const int ROLE_SYSTEM_HELPBALLOON = 0x1f;
        public const int ROLE_SYSTEM_CHARACTER = 0x20;
        public const int ROLE_SYSTEM_LIST = 0x21;
        public const int ROLE_SYSTEM_LISTITEM = 0x22;
        public const int ROLE_SYSTEM_OUTLINE = 0x23;  // treeview
        public const int ROLE_SYSTEM_OUTLINEITEM = 0x24;
        public const int ROLE_SYSTEM_PAGETAB = 0x25;
        public const int ROLE_SYSTEM_PROPERTYPAGE = 0x26;
        public const int ROLE_SYSTEM_INDICATOR = 0x27;
        public const int ROLE_SYSTEM_GRAPHIC = 0x28;
        public const int ROLE_SYSTEM_STATICTEXT = 0X29;
        public const int ROLE_SYSTEM_TEXT = 0x2a;
        public const int ROLE_SYSTEM_PUSHBUTTON = 0x2b;
        public const int ROLE_SYSTEM_CHECKBUTTON = 0x2c;
        public const int ROLE_SYSTEM_RADIOBUTTON = 0x2d;
        public const int ROLE_SYSTEM_COMBOBOX = 0x2e;
        public const int ROLE_SYSTEM_DROPLIST = 0x2f;
        public const int ROLE_SYSTEM_PROGRESSBAR = 0x30;
        public const int ROLE_SYSTEM_DIAL = 0x31;
        public const int ROLE_SYSTEM_HOTKEYFIELD = 0x32;
        public const int ROLE_SYSTEM_DIAGRAM = 0x35;
        public const int ROLE_SYSTEM_ANIMATION = 0x36;
        public const int ROLE_SYSTEM_EQUATION = 0x37;
        public const int ROLE_SYSTEM_BUTTONDROPDOWN = 0x38;
        public const int ROLE_SYSTEM_BUTTONMENU = 0x39;
        public const int ROLE_SYSTEM_BUTTONDROPDOWNGRID = 0x3a;
        public const int ROLE_SYSTEM_WHITESPACE = 0x3b;
        public const int ROLE_SYSTEM_CLOCK = 0x3d;
        public const int ROLE_SYSTEM_SPLITBUTTON = 0x3e;
        public const int ROLE_SYSTEM_IPADDRESS = 0x3f;
        public const int ROLE_SYSTEM_OUTLINEBUTTON = 0x40;

        /// <summary>
        /// Accessibility role:  System Slider
        /// </summary>
        public const int ROLE_SYSTEM_SLIDER = 0x33;

        /// <summary>
        /// Accessibility role:  System Spin Button
        /// </summary>
        public const int ROLE_SYSTEM_SPINBUTTON = 0x34;

        /// <summary>
        /// Accessibility role:  System Page Tab List (Tab Control)
        /// </summary>
        public const int ROLE_SYSTEM_PAGETABLIST = 0x3C;

        public const int BS_MULTILINE = 0x2000;
    }

    public interface IAccessibleProvider
    {
        /// <summary>
        /// 根据窗口句柄获取无障碍对象实例（IAccessible）
        /// </summary>
        object GetAccessibleObject(IntPtr hwnd);
    }

    // Accessible2 接口定义
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("E89F726E-C4F4-4C19-BB19-B647D7FA8478")]
    public interface IAccessible2
    {
        // IAccessible 基础方法
        void get_accParent([Out, MarshalAs(UnmanagedType.Interface)] out object ppdispParent);
        void get_accChildCount(out long pcountChildren);
        void get_accChild([In, MarshalAs(UnmanagedType.Struct)] object varChild, [Out, MarshalAs(UnmanagedType.Interface)] out object ppdispChild);
        void get_accName([In, MarshalAs(UnmanagedType.Struct)] object varChild, [Out, MarshalAs(UnmanagedType.LPWStr)] out string pszName);
        void get_accValue([In, MarshalAs(UnmanagedType.Struct)] object varChild, [Out, MarshalAs(UnmanagedType.LPWStr)] out string pszValue);
        void get_accDescription([In, MarshalAs(UnmanagedType.Struct)] object varChild, [Out, MarshalAs(UnmanagedType.LPWStr)] out string pszDescription);
        void get_accRole([In, MarshalAs(UnmanagedType.Struct)] object varChild, [Out, MarshalAs(UnmanagedType.Struct)] out object pvarRole);
        void get_accState([In, MarshalAs(UnmanagedType.Struct)] object varChild, [Out, MarshalAs(UnmanagedType.Struct)] out object pvarState);
        void get_accHelp([In, MarshalAs(UnmanagedType.Struct)] object varChild, [Out, MarshalAs(UnmanagedType.LPWStr)] out string pszHelp);
        void get_accHelpTopic([Out, MarshalAs(UnmanagedType.LPWStr)] string pszHelpFile, [In, MarshalAs(UnmanagedType.Struct)] object varChild, [Out] out long pidTopic);
        void get_accKeyboardShortcut([In, MarshalAs(UnmanagedType.Struct)] object varChild, [Out, MarshalAs(UnmanagedType.LPWStr)] out string pszKeyboardShortcut);
        void get_accFocus([Out, MarshalAs(UnmanagedType.Struct)] out object pvarChild);
        void get_accSelection([Out, MarshalAs(UnmanagedType.Struct)] out object pvarChildren);
        void get_accDefaultAction([In, MarshalAs(UnmanagedType.Struct)] object varChild, [Out, MarshalAs(UnmanagedType.LPWStr)] out string pszDefaultAction);
        void accSelect(long flagsSelect, [In, MarshalAs(UnmanagedType.Struct)] object varChild);
        void accLocation([Out] out long pxLeft, [Out] out long pyTop, [Out] out long pcxWidth, [Out] out long pcyHeight, [In, MarshalAs(UnmanagedType.Struct)] object varChild);
        void accNavigate(long navDir, [In, MarshalAs(UnmanagedType.Struct)] object varStart, [Out, MarshalAs(UnmanagedType.Interface)] out object pvarEndUpAt);
        void accHitTest(long xLeft, long yTop, [Out, MarshalAs(UnmanagedType.Interface)] out object pvarChild);
        void accDoDefaultAction([In, MarshalAs(UnmanagedType.Struct)] object varChild);
        void put_accName([In, MarshalAs(UnmanagedType.Struct)] object varChild, [In, MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void put_accValue([In, MarshalAs(UnmanagedType.Struct)] object varChild, [In, MarshalAs(UnmanagedType.LPWStr)] string pszValue);

        // Accessible2 特有方法
        void get_nRelations(out long nRelations);
        void get_relation(long relationIndex, [Out, MarshalAs(UnmanagedType.Interface)] out IAccessible2 ppTarget);
        void get_relations(long maxRelations, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IAccessible2[] ppTargets, out long nRelations);
        void role(out long role);
        void get_locale(out string locale);
        void get_attributes(out string attributes);
        void get_groupPosition(out long groupLevel, out long similarItemsInGroup, out long positionInGroup);
        void get_states(out long states);
        void get_extendedRole(out string role);
        void get_localizedExtendedRole(out string localizedRole);
        void get_nExtendedStates(out long nExtendedStates);
        void get_extendedStates(long maxExtendedStates, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] string[] extendedStates, out long nExtendedStates);
        void get_localizedExtendedStates(long maxLocalizedExtendedStates, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] string[] localizedExtendedStates, out long nLocalizedExtendedStates);
        void get_uniqueID(out long uniqueID);
        void get_windowHandle(out IntPtr windowHandle);
        void get_indexInParent(out long indexInParent);
        void get_relationTargetsOfType(string type, long maxTargets, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IAccessible2[] targets, out long nTargets);
        void get_selections(out IAccessible2 selections);
        void scrollTo(IAccessible2 scrollType);
        void scrollToPoint(IAccessible2 coordinateType, long x, long y);
    }



    // IAccessibleTable 接口定义
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("35AD8075-C20C-4fb4-B094-F4F7275DD469")]
    public interface IAccessibleTable
    {
        void get_accessibleAt(long row, long column, [Out, MarshalAs(UnmanagedType.Interface)] out IAccessible accessible);
        void get_caption([Out, MarshalAs(UnmanagedType.Interface)] out IAccessible accessible);
        void get_columnDescription(long column, [Out, MarshalAs(UnmanagedType.LPWStr)] out string description);
        void get_columnExtentAt(long row, long column, out long nColumnsSpanned);
        void get_columnHeader([Out, MarshalAs(UnmanagedType.Interface)] out IAccessible accessible, out long startingRowIndex);
        void get_columnIndex(long cellIndex, out long columnIndex);
        void get_nColumns(out long columnCount);
        void get_nRows(out long rowCount);
        void get_nSelectedCells(out long cellCount);
        void get_nSelectedChildren(out long childCount);
        void get_rowDescription(long row, [Out, MarshalAs(UnmanagedType.LPWStr)] out string description);
        void get_rowExtentAt(long row, long column, out long nRowsSpanned);
        void get_rowHeader([Out, MarshalAs(UnmanagedType.Interface)] out IAccessible accessible, out long startingColumnIndex);
        void get_rowIndex(long cellIndex, out long rowIndex);
        void get_selectedCells([Out, MarshalAs(UnmanagedType.Interface)] out IAccessible accessible, out long nSelectedCells);
        void get_selectedChildren(long maxChildren, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IAccessible[] children, out long nChildren);
        void get_summary([Out, MarshalAs(UnmanagedType.Interface)] out IAccessible accessible);
        void get_isColumnSelected(long column, out bool isSelected);
        void get_isRowSelected(long row, out bool isSelected);
        void get_isSelected(long row, long column, out bool isSelected);
        void selectRow(long row);
        void selectColumn(long column);
        void unselectRow(long row);
        void unselectColumn(long column);
        void get_modelChange([Out, MarshalAs(UnmanagedType.Struct)] out object modelChange);
    }

}
