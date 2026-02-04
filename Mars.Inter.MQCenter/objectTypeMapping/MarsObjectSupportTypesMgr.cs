using Mars.message.AutoTestingDriver.ErrorMessage;
using MarsUFTAddins.IMars.tiger;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Mars.message.Inter.MQCenter.objectTypeMapping
{
    internal interface IMarsSupportedType
    {
        List<object> GetSupportedType(ref bool isOk, ref string strError);
    }

    public abstract class MarsObjBasicOpreator
    {
        public virtual string GetCurrentDisplayText(object sourceControl, ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            return null;
        }
    }

    public class MarsObjectSupportTypesMgr
    {
        private static Dictionary<string, IMarsSupportedType> MarsTypSupportMgr = new Dictionary<string, IMarsSupportedType>()
        {
            { "SWFBUTTON", new MarsSwfButtonSupportTypeMgr()},
            { "SWFCHECKBOX", new MarsSwfCheckboxSupportTypeMgr()},
            { "SWFCOMBOBOX", new MarsSwfComboboxSupportTypeMgr()},
            { "SWFEDIT", new MarsSwfEditSupportTypeMgr()},
            { "SWFLABEL", new MarsSwfLabelSupportTypeMgr()},
            { "SWFLIST" , new MarsSwfListSupportTypeMgr() },
            { "SWFRADIOBUTTON", new MarsSwfRadioButtonTypeMgr()},
            { "SWFTAB", new MarsSwFTabSupportTypeMgr()},
            { "SWFTABLE", new MarsSwfTableSupportTypeMgr()},
            { "SWFTOOLBAR", new MarsSwfToolBarSupportTypeMgr()},
            { "SWFTREEVIEW", new MarsSwfTreeviewSupportTypeMgr()},
            { "SWFSTATUSBAR", new MarsSwfStatusbarTypeMgr()},
            { "SWFWINDOW", new MarsSwfWindowSupportTypeMgr()},

        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strMarsType">could be swfEdit, swfTable and so on</param>
        /// <returns></returns>
        public static List<object> GetSupportedObjectType(string strMarsType, ref bool isOk, ref string strError)
        {
            string strKey = strMarsType == null ? "" : strMarsType.ToUpper();
            if (!MarsTypSupportMgr.ContainsKey(strKey))
            {
                isOk = false;
                strError = string.Format("No such object type is supported:[{0}]", strMarsType);
                return null;
            }
            return MarsTypSupportMgr[strKey].GetSupportedType(ref isOk, ref strError);
        }

        /// <summary>
        /// 依据当前的对象类型，获得支持的类型名称，如swfedit，swfcombo,swftable
        /// </summary>
        /// <param name="strTypes"></param>
        /// <param name="strObjTargetType"></param>
        /// <returns></returns>
        internal static bool searchMarsTypeByObjTyps(string strTypes, ref string strObjTargetType)
        {
            foreach(var itm in MarsTypSupportMgr.Keys)
            {
                if (itm == null) continue;
                IMarsSupportedType marsSupportedTypeInfo = MarsTypSupportMgr[itm];
                if (marsSupportedTypeInfo == null) continue;
                //marsSupportedTypeInfo.GetSupportedType();
            }
            return false;
        }
    }

    public class MarsSwfButtonSupportTypeMgr : IMarsSupportedType
    {
        private static List<object> SwfButtonSupportedType = new List<object>()
        {
            typeof(System.Windows.Forms.Button),
            typeof(System.Windows.Forms.ButtonBase),//注意：大部分的Button都从该类继承           
            "Summit.Framework.View.ButtonControl",
            "Summit.Framework.View.ImageButtonControl",
            "Infragistics.Win.Misc.UltraButton",
            "Summit.Framework.DesktopContent.ImageButton"
        };
        public List<object> GetSupportedType(ref bool isOk, ref string strError)
        {
            return SwfButtonSupportedType;
        }
    }
    public class MarsSwfRadioButtonTypeMgr : IMarsSupportedType
    {
        private static List<object> SwfRadiobuttonSupportedType = new List<object>()
        {
            typeof(System.Windows.Forms.RadioButton),
            typeof(System.Windows.Forms.ButtonBase),//注意：大部分的Button都从该类继承           
            
        };
        public List<object> GetSupportedType(ref bool isOk, ref string strError)
        {
            return SwfRadiobuttonSupportedType;
        }
    }

    public class MarsSwfLabelSupportTypeMgr : IMarsSupportedType
    {
        private static List<object> SwfLabelSupportedType = new List<object>()
        {
            typeof(System.Windows.Forms.Label)
        };
        public List<object> GetSupportedType(ref bool isOk, ref string strError)
        {
            return SwfLabelSupportedType;
        }
    }

    public class MarsSwfListSupportTypeMgr : IMarsSupportedType
    {
        private static List<object> SwfLabelSupportedType = new List<object>()
        {
            typeof(System.Windows.Forms.ListBox),
            typeof(System.Windows.Forms.ListControl),
            "Infragistics.Win.UltraWinTree.UltraTree",
            "Summit.Framework.View.DDownControl",
        };
        public List<object> GetSupportedType(ref bool isOk, ref string strError)
        {
            return SwfLabelSupportedType;
        }
    }



    public class MarsSwfEditSupportTypeMgr : MarsObjBasicOpreator, IMarsSupportedType
    {
        private static List<object> SwfEditSupportedType = new List<object>()
        {
            typeof(System.Windows.Forms.TextBox),
            typeof(System.Windows.Forms.TextBoxBase),//注意：大部分的edit都从该类继承，如textbox， maskTExtbox
            "Summit.Framework.View.TextControlBase",
            "Summit.Framework.View.DDownControl",
            "Summit.Framework.View.TextControl" ,
            "Infragistics.Win.UltraWinEditors.UltraTextEditor" ,
            "Infragistics.Win.UltraWinEditors.UltraCheckEditor",
            "Infragistics.Win.UltraWinEditors.UltraDateTimeEditor",
            "Infragistics.Win.UltraWinEditors.UltraNumericEditor",
            "Infragistics.Win.EmbeddableTextBoxWithUIPermissions"
        };
        public List<object> GetSupportedType(ref bool isOk, ref string strError)
        {
            return SwfEditSupportedType;
        }

        public override string GetCurrentDisplayText(object sourceControl, ref bool isOk, ref string strError, ref string strAdv, ref string strStack)
        {
            if (sourceControl == null)
            {
                strError = "Passing null object to a function";//"Source control is null";
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                isOk = false;
                return null;
            }
            bool isNotExist = false;
            object strData = ReflectorForCSharp.GetMember(sourceControl, "Text", ref isNotExist);
            if (isNotExist)
            {
                strError = "Object Property [Text] is NULL";// "no text member exists in target control";
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                isOk = false;
                return null;
            }

            if (strData == null)
            {
                isOk = true;
                return "";
            }
            isOk = true;
            return strData.ToString();
        }
    }

    public class MarsSwfToolBarSupportTypeMgr : IMarsSupportedType
    {
        private static List<object> SwfToolBarSupportedType = new List<object>
        {
            "Infragistics.Win.UltraWinToolbars.PopupMenuControlTrusted",
            "Infragistics.Win.UltraWinToolbars.UltraToolbarsDockArea",
        };

        public List<object> GetSupportedType(ref bool isOk, ref string strError)
        {
            return SwfToolBarSupportedType;
        }
    }

    public class MarsSwfCheckboxSupportTypeMgr : IMarsSupportedType
    {
        private static List<object> SwfCheckboxSupportType = new List<object>
        {
            typeof(System.Windows.Forms.CheckBox),
            "Summit.Framework.View.CheckBoxControl",
            "Infragistics.Win.CheckEditor",
        };

        public List<object> GetSupportedType(ref bool isOk, ref string strError)
        {
            return SwfCheckboxSupportType;
        }
    }


    public class MarsSwfComboboxSupportTypeMgr : IMarsSupportedType
    {
        private static List<object> SwfComboboxSupportType = new List<object>
        {
            typeof(System.Windows.Forms.ListControl),
            "Infragistics.Win.UltraWinEditors.UltraComboEditor",
            "Summit.Framework.View.DDownControl",

        };

        public List<object> GetSupportedType(ref bool isOk, ref string strError)
        {
            return SwfComboboxSupportType;
        }
    }

    public class MarsSwfTableSupportTypeMgr : IMarsSupportedType
    {
        private static List<object> SwfTableSupportType = new List<object>
        {
            typeof(System.Windows.Forms.DataGrid),
            "Summit.Framework.View.ManualSpreadsheetControl",
            "Summit.Framework.View.SpreadsheetControl",
            "Summit.Framework.View.SerializedDataSpreadsheetControl",
            "Summit.Framework.View.RTSpreadsheetControl",
            "Infragistics.Win.UltraWinGrid.UltraGrid",
        };

        public List<object> GetSupportedType(ref bool isOk, ref string strError)
        {
            return SwfTableSupportType;
        }
    }

    public class MarsSwFTabSupportTypeMgr : IMarsSupportedType
    {
        private static List<object> SwfTabSupportType = new List<object>
        {
            typeof(System.Windows.Forms.TabControl),
            //typeof(System.Windows.Forms.ListControl),
            "Infragistics.Win.UltraControlBase", //base
            "Infragistics.Win.UltraWinTabControl.UltraTabControlBase",
            "Summit.Framework.View.TabControlEx",
            "Summit.Framework.Desktop.PaneTabControl",

        };

        public List<object> GetSupportedType(ref bool isOk, ref string strError)
        {
            return SwfTabSupportType;
        }
    }

    public class MarsSwfStatusbarTypeMgr : IMarsSupportedType
    {
        private static List<object> SwfStatusbarSupportType = new List<object>
        {
            typeof(System.Windows.Forms.StatusBar),
            "Infragistics.Win.UltraWinStatusBar.UltraStatusBar",
        };

        public List<object> GetSupportedType(ref bool isOk, ref string strError)
        {
            return SwfStatusbarSupportType;
        }
    }

    public class MarsSwfTreeviewSupportTypeMgr : IMarsSupportedType
    {
        private static List<object> SwfTreeviewSupportType = new List<object>
        {
            typeof(System.Windows.Forms.TreeView),
            "Infragistics.Win.UltraWinTree.UltraTree",
            "Summit.Framework.View.TreeControl",
            //"System.Windows.Forms.Control; Infragistics.Win.UltraControlBase;
            "Infragistics.Win.UltraWinTree.UltraTree",
            "Summit.Framework.View.RawTreeControl",
            "Summit.Framework.DesktopContent.BaseTreeControl",
            "Summit.Framework.View.TreeControl"
        };

        public List<object> GetSupportedType(ref bool isOk, ref string strError)
        {
            return SwfTreeviewSupportType;
        }
    }

    public class MarsSwfWindowSupportTypeMgr : IMarsSupportedType
    {
        private static List<object> SwfWindowSupportType = new List<object>
        {
            typeof(System.Windows.Forms.Form),
            "System.Windows.Forms.Form",
            "Summit.Framework.Desktop.PaneLayout",
            "Summit.Framework.Desktop.ApplicationLayout"
        };

        public List<object> GetSupportedType(ref bool isOk, ref string strError)
        {
            return SwfWindowSupportType;
        }
    }

}
