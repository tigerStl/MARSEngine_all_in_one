using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

using winform = System.Windows.Forms;
using System.Web.Script.Serialization;
using System.Runtime.Serialization;
#if !_MarsToolsImport
using MarsUFTAddins.IMars.tiger;
#endif
using System.Security.Permissions;
//using System.Text.Json.Serialization;
using System.Drawing.Imaging;
using System.Drawing;
using System.Dynamic;
using System.IO;
using Mars.message.Inter.MQCenter.objectTypeMapping;
using Mars.message.Inter.MQCenter.objectSpy;
using System.Runtime.Remoting;
using System.Windows.Controls;
using System.Runtime.InteropServices.WindowsRuntime;
using Mars.Inter.MQCenter.interProcess;
using Mars.Inter.MQCenter.interProcess.Wpf.ObjectSpy.Support;
using Mars.message.Inter.MQCenter.simpleLog;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.Security.Policy;





#if !_MarsToolsImport
using Mars.message.windowsWrapper.SystemUtil;
#endif
using System.Diagnostics;

namespace Mars.message.Inter.MQCenter.interProcess
{
    public class MarsObjectSpyCommand
    {
        public const string cnst_commandName_normal = "normal";
        public const string cnst_commandName_wpfhyberidFramework = "wpfhyberidFramework";
        /// <summary>
        /// 取值为Normal或者WpfHyberidFramework
        /// </summary>
        public string spyType { get; set; } = cnst_commandName_normal;

        public string targetWnd { get; set; }
    }


    /// <summary>
    /// 该类用于针对winform的application
    /// </summary>
    /// 
#if !_MarsToolsImport
    public class MarsWinformSpy
    {
        public const string cnst_applicationType_winform = "Mars_dotNet";

        public static string getTypePath(Type t)
        {
            string rslt = t.FullName;
            Type parentType = t.BaseType;
            while (parentType != null)
            {
                rslt = $"{rslt};{parentType.FullName}";
                string namespaceName = parentType.Namespace;
                if (namespaceName != null && (namespaceName.StartsWith("System") || namespaceName.StartsWith("Microsoft")))
                {
                    // Found a parent type in "System" or "Microsoft" namespace
                    break;
                }
                parentType = parentType.BaseType;
            }
            return rslt;
        }

        private static IntPtr GetFirstControlHandle(List<IntPtr> src)
        {
            foreach (var hdl in src)
            {
                try
                {
                    System.Windows.Forms.Control.FromHandle(hdl);
                    return hdl;
                }
                catch (Exception)
                {
                    continue;
                }
            }
            return IntPtr.Zero;
        }

        public static List<MarsSpiedObjectInfo> getCurrentAllObjectsOfContainer(ref bool isOk, ref string strError,
            QueryObjectRequst objReqInfo = null,
            bool isShowHighlight = false,
            bool isTypePthInclude = false)
        {
            simpleLog.MarsLoggerSimple.logBegin("getCurrentAllObjectsOfContainer", $"RequestInfo:{objReqInfo.typeOfGenerateSteps}");
            if (objReqInfo.typeOfGenerateSteps != 1)
            {
                strError = $"typeOfGenerateSteps should be 0 but it is |{objReqInfo.typeOfGenerateSteps}|";
                isOk = false;
                simpleLog.MarsLoggerSimple.Error("getCurrentAllObjectsOfContainer", strError);
                return new List<MarsSpiedObjectInfo>();
            }
            UIPermission uIPermission = new UIPermission(UIPermissionWindow.AllWindows);
            try
            {
                IntPtr activeHwnd = IntPtr.Zero;
                System.Windows.Forms.Control c = null;
                if ((objReqInfo != null) && (objReqInfo.currentHandle != 0))
                {
                    c = System.Windows.Forms.Control.FromHandle((IntPtr)objReqInfo.currentHandle);
                    if (c == null)
                    {
                        strError = "can't get control from handle";
                        isOk = false;
                        return new List<MarsSpiedObjectInfo>();
                    }
                }
                if (c == null)
                {
                    strError = "no control handle or handle can't be casted to control";
                    isOk = false;
                    return new List<MarsSpiedObjectInfo>();
                }

                var objNew = new MarsSpiedObjectInfo()
                {
                    x = c.Left,
                    y = c.Top,
                    w = c.Width,
                    h = c.Height,
                    relatedX = c.Left,
                    relatedY = c.Top,
                    referenceToObj = c,
                    objectName = c.Name,
                    objectNamePath = c.Name,
                    Text = c.Text,
                    objectType = c.GetType().FullName,
                    objectTypePath = c.GetType().FullName,
                    isVisible = c.Visible,
                    isChildWindow = false,
                    isOwnedWindow = false,
                    hwnd = c.Handle.ToInt64(),
                    Pegwindow = null
                };

                List<MarsSpiedObjectInfo> lstOfObjs = new List<MarsSpiedObjectInfo>();
                if (objNew.children == null)
                    objNew.children = new List<MarsSpiedObjectInfo>();
                objNew.children.Clear();
                objNew.buildChildren(isShowHighlight, rootPeg: objNew);
                lstOfObjs.Add(objNew);
                lstOfObjs.AddRange(objNew.children);
                isOk = true;
                return lstOfObjs;

            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("getCurrentAllObjects", e.Message, e.StackTrace);
                return null;
            }
        }


        public static List<MarsSpiedObjectInfo> getCurrentAllObjects(QueryObjectRequst objReqInfo = null,
            bool isShowHighlight = false,
            bool isTypePthInclude = false)
        {
            UIPermission uIPermission = new UIPermission(UIPermissionWindow.AllWindows);
            try
            {
                IntPtr activeHwnd = IntPtr.Zero;
                if ((objReqInfo != null) && (objReqInfo.currentHandle != 0))
                {
                    activeHwnd = MarsWindowsAPIs.GetAncestor(new IntPtr(objReqInfo.currentHandle),
                        MarsWindowsAPIs.GetAncestorFlags.GetRoot);
                    simpleLog.MarsLoggerSimple.Info("getCurrentAllObjects", $"has handle of re-query|and top wind handle|{activeHwnd}");
                }

                //var windowsHdlLst = MarsWindowsAPIsExtend.EnumerateProcessWindowHandles(Process.GetCurrentProcess().Id);
                var formCllection = winform.Application.OpenForms;
                var windowsHdlLst = MarsWindowsAPIsExtend.GetWindows(Process.GetCurrentProcess().Id);
                simpleLog.MarsLoggerSimple.Info("getCurrentAllObjects", $"process windows|{windowsHdlLst.Count}|");
                //var activeHwnd = GetFirstControlHandle(windowsHdlLst);

                bool isFormInclude = false;
                List<IntPtr> unDealedTopWindow = new List<IntPtr>();

                if ((formCllection == null) && (!isFormInclude))
                    return null;

                List<MarsSpiedObjectInfo> lstOfObjs = new List<MarsSpiedObjectInfo>();
                /// 算法
                /// 1，获得所有的objects 和its parents
                /// 2，load to那个窗口

                for (int i = 0; i < (formCllection == null ? -1 : formCllection.Count); i++)
                {
                    winform.Form itm = formCllection[i];
                    if ((activeHwnd != IntPtr.Zero) && (!itm.Handle.Equals(activeHwnd)))
                    {
                        simpleLog.MarsLoggerSimple.Info("getCurrentAllObjects", $"activeWindow|{activeHwnd}|{itm.Handle}|i-{i}, ignore");
                        continue;
                    }
                    // ignore the tool window
                    if (itm is MarsObjSpyForm) continue;
                    if (itm is Mars.message.Utility.visualObjects.objectSpyer.HighlightWindow) continue;
                    if (itm.GetType() == typeof(objectSpy.MarsObjSpyForm)) continue;
                    if (itm.GetType() == typeof(Mars.message.Utility.visualObjects.objectSpyer.HighlightWindow)) continue;
                    if (windowsHdlLst.Any(p => p == itm.Handle))
                    {
                        windowsHdlLst.Remove(itm.Handle);
                    }

                    MarsSpiedObjectInfo objNew = null;
                    MarsObjectOp objOp = new MarsObjectOp();
                    objNew = new MarsSpiedObjectInfo()
                    {
                        x = itm.Left,
                        y = itm.Top,
                        w = itm.Width,
                        h = itm.Height,
                        relatedX = itm.Left,
                        relatedY = itm.Top,
                        referenceToObj = itm,
                        objectName = itm.Name,
                        objectNamePath = itm.Name,
                        Text = itm.Text,
                        objectType = itm.GetType().FullName,
                        objectTypePath = itm.GetType().FullName,
                        isVisible = itm.Visible,
                        isChildWindow = itm.IsMdiChild,
                        isOwnedWindow = itm.OwnedForms == null ? false : itm.OwnedForms.Length > 0,
                        Pegwindow = null
                    };

                    if (isTypePthInclude)
                    {
                        objNew.objectNamePath = getTypePath(itm.GetType());
                    }

                    objNew.controlMarsType = objOp.getMarsObject(itm);
                    try
                    {
                        itm.Invoke(new Action(() =>
                        {
                            var screenPt = itm.PointToScreen(new System.Drawing.Point(-1, -1));
                            objNew.x = screenPt.X;
                            objNew.y = screenPt.Y;
                        }));
                        objNew.snapshotFileNameWithPath = objNew.saveAreaToFile(
                            new Rectangle(objNew.x - 1,
                            objNew.y - 1, objNew.w + 2, objNew.h + 2)
                        );
                    }
                    catch (Exception e)
                    {
#if !_MarsToolsImport
                        simpleLog.MarsLoggerSimple.Error("loadFromControl", e.Message, e);
#endif
                    }
                    lstOfObjs.Add(objNew);

                    simpleLog.MarsLoggerSimple.Info("\t", objNew.ToString());
                    // build its child
                    objNew.buildChildren(isShowHighlight, rootPeg: objNew);
                }

                if ((activeHwnd != IntPtr.Zero) && (windowsHdlLst.Count > 0))
                {
                    simpleLog.MarsLoggerSimple.Info("\t", $"there are {windowsHdlLst.Count} more windows's handles");
                    windowsHdlLst.ForEach(p =>
                    {
                        try
                        {
                            System.Windows.Forms.Control cntrl = System.Windows.Forms.Control.FromHandle(p);

                            if (cntrl == null) return;
                            if (cntrl is Mars.message.Utility.visualObjects.objectSpyer.HighlightWindow) return;
                            if (cntrl.GetType() == typeof(objectSpy.MarsObjSpyForm)) return;
                            if (cntrl.GetType() == typeof(Mars.message.Utility.visualObjects.objectSpyer.HighlightWindow)) return;
                            MarsSpiedObjectInfo objNew = null;
                            lstOfObjs.Add(objNew = new MarsSpiedObjectInfo()
                            {
                                x = cntrl.Left,
                                y = cntrl.Top,
                                w = cntrl.Width,
                                h = cntrl.Height,
                                relatedX = cntrl.Left,
                                relatedY = cntrl.Top,
                                referenceToObj = cntrl,
                                objectName = cntrl.Name,
                                objectNamePath = cntrl.Name,
                                Text = cntrl.Text,
                                objectType = cntrl.GetType().FullName,
                                objectTypePath = cntrl.GetType().FullName,
                            });
                            objNew.buildChildren(rootPeg: objNew);
                        }
                        catch (Exception e)
                        {
                            simpleLog.MarsLoggerSimple.Error("LoadObjectsToSpyForm", e.Message, e);
                        }
                    });
                }

                return lstOfObjs;
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("getCurrentAllObjects", e.Message, e.StackTrace);
                return null;
            }
        }


        internal void LoadObjectsToSpyForm(MarsObjectSpyCommand spyCmmd)
        {
            simpleLog.MarsLoggerSimple.logBegin("LoadObjectsToSpyForm");
            //UIPermission uIPermission = new UIPermission(UIPermissionWindow.AllWindows);            
            try
            {
                List < MarsSpiedObjectInfo > lstOfObjs = null;
                if ((spyCmmd.spyType ?? "").Equals(MarsObjectSpyCommand.cnst_commandName_wpfhyberidFramework, StringComparison.OrdinalIgnoreCase))
                {
                    /// 获得.net framework control 嵌入在其他对象的，如wpf时候的情况
                    /// 
                    //lstOfObjs =  
                }
                else
                    lstOfObjs = getCurrentAllObjects();

                objectSpy.MarsObjSpyForm spyFrm = objectSpy.MarsObjSpyForm.getInstance(lstOfObjs);
                spyFrm.targetControlWndId = spyCmmd.targetWnd;
                if (spyFrm.Modal) return;
                objectSpy.MarsObjSpyForm.showModuleInThread();
            }
            catch (Exception e)
            {
                simpleLog.MarsLoggerSimple.Error("LoadObjectsToSpyForm", e.Message, e);
            }
        }
    }
#endif
    [DataContract]
    public partial class MarsSpyGeneratedQuickAccess
    {
        public const string cnst_objectHappyName = "Object Happy Name";
        public const string cnst_objectPegWindow = "Pegwindow";
        public const string cnst_swfnamePath = "SwfName Path";
        public const string cnst_swfname = "swfname";
        public const string cnst_text = "Text";
        public const string cnst_index = "Index";
        public const string cnst_type = "swfType";
        public const string cnst_typePath = "swfType Path";
        public const string cnst_appliedApp = "Application Short Name";
        public const string cnst_isPegwindow = "isPegwindow";

        public const string cnst_defaultPegwindowHint = "Set Pegwindow here";
        public const string cnst_defaultAppNameHint = "Set Application Short Name";
        [DataMember(IsRequired = true)]
        public string PropertyName;
        [DataMember(IsRequired = true)]
        public string PropertyValue;
#if !_MarsToolsImport
        public bool gen_DataForObj(System.Windows.Forms.Control c, string properIdx)
        {
            if (string.IsNullOrEmpty(properIdx)) return false;
            if (cnst_swfname.Equals(properIdx, StringComparison.OrdinalIgnoreCase))
            {
                PropertyName = cnst_swfname;
                PropertyValue = c.Name;
                return true;
            }
            ReflectorForCSharp reflector = new ReflectorForCSharp();
            if (cnst_swfnamePath.Equals(properIdx, StringComparison.OrdinalIgnoreCase))
            {
                string namePath = reflector.getNamePath(c);
                PropertyName = cnst_swfnamePath;
                PropertyValue = namePath;
                return true;
            }
            if (cnst_text.Equals(properIdx, StringComparison.OrdinalIgnoreCase))
            {
                var v = ReflectorForCSharp.GetPropValue(c, "Text");
                if (v == null) return false;
                PropertyName = cnst_text;
                PropertyValue = v.ToString();
                return true;
            }
            if (cnst_type.Equals(properIdx, StringComparison.OrdinalIgnoreCase))
            {
                PropertyName = cnst_type;
                PropertyValue = c.GetType().FullName;
                return true;
            }
            return false;
        }
#endif
    }

    public class MarsObjectOp
    {

        public System.Windows.Forms.Control GetTopParent(System.Windows.Forms.Control c)
        {
            if (c == null) return null;
            if (c.Parent == null) return c;
            return GetTopParent(c.Parent);
        }

        public MarsSpiedObjInfoAI getControlPeg(System.Windows.Forms.Control c, ref bool isOk, ref string strError)
        {
            int iMark = new Random().Next();
            simpleLog.MarsLoggerSimple.logBegin("getControlPeg", $"{iMark}");
            if (c == null)
            {
                simpleLog.MarsLoggerSimple.Error("getControlPeg", strError = "control is null");
                return null;
            }
            ReflectorForCSharp reflector = new ReflectorForCSharp();
            string strNamePth = reflector.getNamePath(c);

            MarsSpiedObjInfoAI pegRslt = new MarsSpiedObjInfoAI();
            try
            {
                var pc = GetTopParent(c);
                if (pc == null)
                {
                    /// c is peg 
                    /// 
                    pegRslt.objectName = c.Name;
                    pegRslt.objectNamePath = strNamePth;
                    pegRslt.objectTypePath = c.GetType().FullName;
                    pegRslt.objectType = c.GetType().FullName;
                }
                else
                {
                    pegRslt.objectName = pc.Name;
                    pegRslt.objectNamePath = pc.Name;
                    pegRslt.objectTypePath = pc.GetType().FullName;
                    pegRslt.objectType = c.GetType().FullName;
                }
                isOk = true;
                return pegRslt;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.Info($"getControlPeg|{iMark}", pegRslt.ToString());
                simpleLog.MarsLoggerSimple.logEnd($"getControlPeg|{iMark}");
            }
        }

        public MarsSpiedObjInfoAI getControlQuick(System.Windows.Forms.Control c, ref bool isOk, ref string strError)
        {
            int iMark = new Random().Next();
            simpleLog.MarsLoggerSimple.logBegin("getControlQuick", $"{iMark}");
            if (c == null)
            {
                simpleLog.MarsLoggerSimple.Error($"getControlQuick|{iMark}", strError = "control is null");
                return null;
            }
            ReflectorForCSharp reflector = new ReflectorForCSharp();
            string strNamePth = ReflectorForCSharp.MarsGetParentsNames(c);// reflector.getNamePath(c);
            //ReflectorForCSharp.GetTypeAndItsAncestor
            MarsSpiedObjInfoAI objRslt = new MarsSpiedObjInfoAI();
            try
            {
                /// 
                objRslt.objectName = c.Name;
                objRslt.objectNamePath = strNamePth;
                objRslt.objectType = c.GetType().FullName;
                //objRslt.objectTypePath = 
                isOk = true;
                return objRslt;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd($"getControlQuick|{iMark}");
            }
        }

        public string getMarsObject(winform.Control itm)
        {
            if (itm == null) return null;
            string strError = "";
            bool isOk = false;
            var engineMappingObj = Mars.Inter.MQCenter.objectEngine.ObjectEngineConfigFileManagement.GetEngineObject();

            string strTargetType = "";
            string strTypeOfInherited = ReflectorForCSharp.GetObjectBaseType(itm.GetType(), true);
            try
            {
                if ((strTypeOfInherited.IndexOf("Infragistics.Win.UltraWinEditors.UltraComboEditor;") >= 0)
                    || (itm is winform.ComboBox)
                    || ((strTypeOfInherited.IndexOf(".ValueListDropDownUnsafe;") >= 0))
                    || ((strTypeOfInherited.IndexOf(".Infragistics.Win.DropDownManager") >= 0))
                    || (strTypeOfInherited.IndexOf("DropDownForm;") >= 0))
                {
                    return strTargetType = MarsObjectTypeMappings.cnst_swf_Combobox;
                }

                if ((strTypeOfInherited.IndexOf(".UltraGridBase;") >= 0)
                    || (strTypeOfInherited.IndexOf(".UltraGrid;") >= 0)
                    || (itm is winform.DataGrid)
                    ) return strTargetType = MarsObjectTypeMappings.cnst_swf_Table;

                if ((strTypeOfInherited.IndexOf("Windows;") >= 0)
                    || (itm is winform.Form)
                    || (itm is System.Windows.Forms.MdiClient)
                    ) return strTargetType = MarsObjectTypeMappings.cnst_swf_pegwindow;

                if ((strTypeOfInherited.IndexOf("Label;") >= 0)
                    || (itm is winform.Label)
                    ) return strTargetType = MarsObjectTypeMappings.cnst_swf_Label;
                if ((strTypeOfInherited.IndexOf("CheckBox;") >= 0)
                    || (itm is winform.CheckBox)
                    ) return strTargetType = MarsObjectTypeMappings.cnst_swf_Checkbox;
                if ((strTypeOfInherited.IndexOf(".UltraToolbarBase;") >= 0)
                    || (itm is winform.ToolBar)
                    || (strTypeOfInherited.IndexOf(".UltraToolbarsDockArea;") >= 0)
                    ) return strTargetType = MarsObjectTypeMappings.cnst_swf_ToolBar;
                if ((strTypeOfInherited.IndexOf(".TabManager;") >= 0)
                    || (itm is winform.TabControl)
                    ) return strTargetType = MarsObjectTypeMappings.cnst_swf_Tab;
                if ((itm is winform.ButtonBase)
                    || (strTypeOfInherited.IndexOf("ButtonUIElement") >= 0))
                {
                    return strTargetType = MarsObjectTypeMappings.cnst_swf_Button;
                }
                if (itm is winform.Label)
                {
                    return strTargetType = MarsObjectTypeMappings.cnst_swf_Label;
                }
                if ((itm is winform.TextBoxBase)
                    || (strTypeOfInherited.IndexOf(".TextEditorControlBase;") >= 0)
                    ) return strTargetType = MarsObjectTypeMappings.cnst_swf_edit;
                if ((strTypeOfInherited.IndexOf(".PopupMenuControl;") >= 0)
                    || (itm is winform.MainMenu)
                    || (itm is winform.MenuItem)
                    || (itm is winform.MenuStrip)
                    || (itm is winform.ContextMenu) || (itm is winform.ContextMenuStrip))
                {
                    return strTargetType = MarsObjectTypeMappings.cnst_swf_Menu;
                }
                if (itm is System.Windows.Forms.ListView)
                {
                    return strTargetType = MarsObjectTypeMappings.cnst_swf_ListView;
                }
                if ((strTypeOfInherited.IndexOf(".UltraTree") >= 0)
                    || (itm is winform.TreeView)
                   )
                {
                    return strTargetType = MarsObjectTypeMappings.cnst_swf_Tree;
                }
                else
                //if (strTypeOfInherited.IndexOf(".DropDownManager+DropDownForm") >= 0)
                {
                    simpleLog.MarsLoggerSimple.Info("getMarsObject", $"used to debug object and type|{strTypeOfInherited}");
                }
                ///这里需要添加类别和 mars类别映射的json文件
                if (engineMappingObj != null)
                {
                    strTargetType = engineMappingObj.getObjectTypeByTypePath(strTypeOfInherited, ref isOk, ref strError);
                    if (isOk) return strTargetType;
                }

                simpleLog.MarsLoggerSimple.Error("*****PLEASE NOTICE******", $"UNCHECKED object type|{strTypeOfInherited}");
                return strTargetType = MarsObjectTypeMappings.cnst_swf_object;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.Info("getMarsObject", $"{strTargetType}={strTypeOfInherited}");
            }
        }

        public static string GetCheckboxValue(System.Windows.Forms.Control c,
            ref bool isOk, ref string strError)
        {
            int iMark = new Random().Next();

            simpleLog.MarsLoggerSimple.logBegin($"{iMark}|GetCheckboxValue");
            string strTyp = c == null ? "NULL" : c.GetType().FullName;
            simpleLog.MarsLoggerSimple.Info("GetCheckboxValue", $"{iMark}|{strTyp}");
            if (c is System.Windows.Forms.CheckBox)
            {
                System.Windows.Forms.CheckBox chckBox = (System.Windows.Forms.CheckBox)c;
                isOk = true;
                return chckBox.Checked.ToString();
            }
            /// check type for infragisticsts
            /// 
            string allTypes = ReflectorForCSharp.GetObjectBaseType(c.GetType());
            ReflectorForCSharp reflect = new ReflectorForCSharp();
            bool isNotExist = false;
            var memberChecked = ReflectorForCSharp.GetMember(c, "Checked", ref isNotExist);
            if ((isNotExist) || (memberChecked == null))
            {
                simpleLog.MarsLoggerSimple.Error("GetCheckboxValue", strError = $"no checked member in |{c.GetType().FullName}|");
                isOk = false;
            }
            isOk = true;
            return memberChecked.ToString();
        }
    }
    [Serializable]
    public class MarsObjectColumnInfo
    {
        public string ColumnName { get; set; }
    }

    [Serializable]
    public class MarsSpiedObjInfoAI
    {
        public string objectName { get; set; }
        public string objectType { get; set; }
        public string objectNamePath { get; set; }
        public string objectTypePath { get; set; }
        public string marsNamePath { get; set; } = "";
        public string Text { get; set; } = "";
        public bool? isChildWindow { get; set; }
        public bool? isOwnedWindow { get; set; }

        // for table and combobox
        public List<string> ListItems { get; set; }
        public List<MarsObjectColumnInfo> DataTableColumns { get; set; }
        public Rectangle objectRect { get; set; }

        public virtual string toMarsObjString()
        {
            return string.IsNullOrEmpty(objectName) ? $"swfname path:={objectNamePath}" : $"SwfName:={objectName}";
        }

        public string getQuickAccess(bool isUseNameOnly = false)
        {
            string strRslt = "";

            if (isChildWindow != null)
            {
                strRslt = $"is Child Window:={isChildWindow.Value}";
            }
            if (isOwnedWindow != null)
            {
                strRslt = string.IsNullOrEmpty(strRslt) ? strRslt
                    : $"{strRslt}\r\nis Owned Window:={isOwnedWindow.Value}";
            }
            if (!string.IsNullOrEmpty(objectName))
                strRslt = string.IsNullOrEmpty(strRslt) ? $"swfName:={objectName}"
                    : $"{strRslt}\r\nswfName:={objectName}";
            if ((isUseNameOnly) && (!string.IsNullOrEmpty(strRslt)))
                return strRslt;
            if (!string.IsNullOrEmpty(objectNamePath))
                strRslt = string.IsNullOrEmpty(strRslt) ? $"swfName Path:={objectNamePath}"
                    : $"{strRslt}\r\nswfName Path:={objectNamePath}";
            //if (!string.IsNullOrEmpty(objectType))
            //    strRslt = string.IsNullOrEmpty(objectType) ? strRslt
            //        : $"{strRslt}\r\nswfType:={objectType}";
            return strRslt;
        }

        public override string ToString()
        {
            string info = $"objectName|{objectName}|objectType|{objectType}|objectNamePath{objectNamePath}|objectTypePath|{objectTypePath}|\r\nText|{Text}|rectangle|{objectRect}";
            if (DataTableColumns != null)
            {
                info += ("|\r\n" + string.Join(";", DataTableColumns));
            }
            return info;
        }

        public string ToStringWithPrefix(string strPrefix)
        {
            string info = $"objectName|{objectName}|objectType|{objectType}|objectNamePath{objectNamePath}|objectTypePath|{objectTypePath}|\r\n{strPrefix}Text|{Text}|rectangle|{objectRect}";
            if (DataTableColumns != null)
            {
                info += ("|\r\n" + strPrefix + string.Join(";", DataTableColumns));
            }
            return info;
        }
    }

    public partial class MarsSpiedObjectInfo : MarsSpiedObjectBasicInfo
    {
        [ScriptIgnore]
        [DataMember(IsRequired = false)]
        public Object referenceToObj;
        [ScriptIgnore]
        [DataMember(IsRequired = false)]
        public List<MarsSpiedObjectInfo> children;

        // 添加启用状态属性，用于WPF对象
        [DataMember(IsRequired = false)]
        public bool isEnabled { get; set; } = true;

        // 添加所有子节点和孙节点的总数属性
        [DataMember(IsRequired = false)]
        public int allChildrenCount { get; set; } = 0;

        // 添加Snoop风格的信息属性
        [DataMember(IsRequired = false)]
        public Dictionary<string, object> dependencyProperties { get; set; } = new Dictionary<string, object>();

        [DataMember(IsRequired = false)]
        public List<string> events { get; set; } = new List<string>();

        [DataMember(IsRequired = false)]
        public string style { get; set; } = "";

        [DataMember(IsRequired = false)]
        public string template { get; set; } = "";

        [DataMember(IsRequired = false)]
        public Dictionary<string, object> resources { get; set; } = new Dictionary<string, object>();

        [DataMember(IsRequired = false)]
        public List<string> bindings { get; set; } = new List<string>();

        [DataMember(IsRequired = false)]
        public List<string> triggers { get; set; } = new List<string>();

        [DataMember(IsRequired = false)]
        public string renderInfo { get; set; } = "";

        [DataMember(IsRequired = false)]
        public string layoutInfo { get; set; } = "";

        [DataMember(IsRequired = false)]
        public string inputInfo { get; set; } = "";

        [DataMember(IsRequired = false)]
        public string focusInfo { get; set; } = "";

        [DataMember(IsRequired = false)]
        public string visibilityInfo { get; set; } = "";

        [DataMember(IsRequired = false)]
        public string transformInfo { get; set; } = "";

        [DataMember(IsRequired = false)]
        public string animationInfo { get; set; } = "";

        [DataMember(IsRequired = false)]
        public string contextInfo { get; set; } = "";

        [DataMember(IsRequired = false)]
        public string debugInfo { get; set; } = "";

        #region MSAA 属性区
        [DataMember(IsRequired =false)]
        public string marsMSAARoleNamePath { get; set; } = "";

        [DataMember(IsRequired = false)]
        public string marsNamePath { get; set; } = "";

        [DataMember(IsRequired = false)]
        public string marsTypePath { get; set; } = "";

        #endregion

        public MarsSpiedObjectInfo()
        {
            this.obj_uuid = Guid.NewGuid().ToString();
        }

        public void buildChildren(bool isShowHighlight = false, MarsSpiedObjectBasicInfo rootPeg = null)
        {
            if (referenceToObj == null) return;
            if (!(referenceToObj is winform.Control)) return;
            winform.Control c = (winform.Control)referenceToObj;

            //if (isShowHighlight)
            //{
            //    HighLightForm tmpForm = new HighLightForm();
            //    tmpForm.Left = ix;
            //    tmpForm.Top = iy;
            //    tmpForm.Width = iw;
            //    tmpForm.Height = ih;
            //    tmpForm.targetFileName = strFileName;
            //    //tmpForm.Update();
            //    //tmpForm.Show();
            //    System.Threading.Thread.Sleep(1000);

            //    Application.Run(tmpForm);
            //}

            if ((c.Controls == null) || (c.Controls.Count == 0)) return;

            children = buildChildren(c.Controls, objectNamePath,
                objectTypePath, rootPeg);
        }

        private List<MarsSpiedObjectInfo> buildChildren(winform.Control.ControlCollection childrens,
            string parentNamePath,
            string strParentTypePath,
            MarsSpiedObjectBasicInfo rootPeg = null)
        {
            List<MarsSpiedObjectInfo> lstRslt = new List<MarsSpiedObjectInfo>();
            foreach (var itm in childrens)
            {
                if (itm == null) continue;
                if (!(itm is winform.Control)) continue;

                MarsSpiedObjectInfo spiedObj = new MarsSpiedObjectInfo();
                spiedObj.loadFromControl(itm as winform.Control, parentNamePath, strParentTypePath);
                if (spiedObj != null)
                {
                    if (rootPeg != null)
                    {
                        spiedObj.PegWindUUID = rootPeg.obj_uuid;
                        spiedObj.Pegwindow = rootPeg;
                    }
                }
#if !_MarsToolsImport
                //simpleLog.MarsLoggerSimple.Info("\t", spiedObj.ToString());
#endif
                lstRslt.Add(spiedObj);
#if !_MarsToolsImport
                //simpleLog.MarsLoggerSimple.Info("\t", spiedObj.ToString());
#endif

                if ((itm as winform.Control).HasChildren)
                    spiedObj.children = spiedObj.buildChildren((itm as winform.Control).Controls,
                        spiedObj.objectNamePath,
                        spiedObj.objectTypePath,
                        rootPeg);
            }
            return lstRslt;
        }

        internal string saveAreaToFile(Rectangle screenArea)
        {
            try
            {
                Bitmap screenshot = new Bitmap(screenArea.Width, screenArea.Height, PixelFormat.Format32bppArgb);
                var pth = Path.GetDirectoryName(typeof(MarsSpiedObjectInfo).Assembly.Location);
                string strImgPath = Path.Combine(pth, "tmpImg");
                if (!System.IO.Directory.Exists(strImgPath))
                {
                    System.IO.Directory.CreateDirectory(strImgPath);
                }
                Guid guid = Guid.NewGuid();
                string targetFileName = Path.Combine(strImgPath, guid.ToString() + ".jpeg");
                using (Graphics g = Graphics.FromImage(screenshot))
                {
                    g.CopyFromScreen(screenArea.X, screenArea.Y, 0, 0, screenArea.Size, CopyPixelOperation.SourceCopy);
                }

                screenshot.Save(targetFileName, ImageFormat.Jpeg);
                return targetFileName;
            }
            catch (Exception)
            {
                return null;
            }
        }
        private void loadFromControl(winform.Control itm, string parentNamePath, string strParentTypePath)
        {
            if (itm == null) return;

            this.objectName = itm.Name;
            this.objectNamePath = string.IsNullOrEmpty(parentNamePath) ? itm.Name : $"{itm.Name};{parentNamePath}";
            this.objectType = itm.GetType().FullName;
            this.objectTypePath = string.IsNullOrEmpty(strParentTypePath) ? this.objectType : $"{this.objectType};{strParentTypePath}";
            this.referenceToObj = itm;
            System.Drawing.Point pt = new System.Drawing.Point(itm.Left, itm.Top);

            MarsObjectOp objOp = new MarsObjectOp();

            this.relatedX = itm.Left;
            this.relatedY = itm.Top;
            this.Text = itm.Text;
            this.zorder = itm.TabIndex;
            this.h = itm.Height;
            this.w = itm.Width;
            this.isVisible = itm.Visible;
            this.controlMarsType = objOp.getMarsObject(itm);

            try
            {
                itm.Invoke(new Action(() =>
                {
                    var screenPt = itm.PointToScreen(new System.Drawing.Point(-1, -1));
                    this.x = screenPt.X;
                    this.y = screenPt.Y;
                    try
                    {
                        this.hwnd = itm.Handle.ToInt64();
                    }
                    catch (Exception)
                    {

                    }
                }));
                this.snapshotFileNameWithPath = saveAreaToFile(new Rectangle(this.x - 1, this.y - 1, this.w + 2, this.h + 2));
            }
            catch (Exception e)
            {
#if !_MarsToolsImport
                simpleLog.MarsLoggerSimple.Error("loadFromControl", e.Message, e);
#endif
            }
        }

        public override string ToString()
        {

            var json = new JavaScriptSerializer().Serialize(this);
            return json;
        }

        internal string getDisplayId()
        {
            string result = $"[{controlClassTypeFromAPI}][{this.x},{this.y},{this.w},{this.h}]-|t:{this.Text}|n:{this.objectName}|";
            if (this.controlClassTypeFromAPI == null)
                return result;
            if (this.controlClassTypeFromAPI.Equals("winforms", StringComparison.OrdinalIgnoreCase))
            {
                result = $"{result}{this.objectName}";
            }
            else if (this.controlClassTypeFromAPI.Equals("standard", StringComparison.OrdinalIgnoreCase))
            {
                result = $"{result}{this.controlId}";
            }
            return result;
            /*
            if ((!string.IsNullOrEmpty(this.objectName)) && (!string.IsNullOrEmpty(this.Text)))
                return $"{this.objectName} [{this.Text}]";
            if (!string.IsNullOrEmpty(this.objectName))
            {
                return this.objectName;
            }
            if (!string.IsNullOrEmpty(this.Text))
                return this.Text;
            return $"({this.x},{this.y})-zorder:{this.zorder}";
            */
        }


    }


    /// <summary>
    /// 标准Windows控件信息类
    /// </summary>
    public class StandardWindowInfo
    {
        public IntPtr Handle { get; set; }
        public string WindowClass { get; set; }
        public string WindowText { get; set; }
        public IntPtr ParentHandle { get; set; }
        public bool IsVisible { get; set; }
        public Rectangle Bounds { get; set; }
        public int ControlId { get; set; }
    }

    /// <summary>
    /// 标准Windows控件枚举器
    /// </summary>
    public static class StandardWindowsEnumerator
    {
        /// <summary>
        /// 获取指定窗口的所有标准子控件
        /// </summary>
        /// <param name="parentHandle">父窗口句柄</param>
        /// <returns>标准控件信息列表</returns>
        public static List<StandardWindowInfo> GetStandardChildWindows(IntPtr parentHandle)
        {
            var result = new List<StandardWindowInfo>();

            if (parentHandle == IntPtr.Zero)
                return result;

            // 使用EnumChildWindows枚举所有子窗口
            MarsWindowsAPIs.EnumChildWindows(parentHandle, (childHandle, lParam) =>
            {
                try
                {
                    var windowInfo = GetWindowInfo(childHandle);
                    if (windowInfo != null)
                    {
                        result.Add(windowInfo);
                    }
                }
                catch (Exception ex)
                {
                    simpleLog.MarsLoggerSimple.Error("GetStandardChildWindows", $"Error processing window {childHandle}: {ex.Message}");
                }
                return true; // 继续枚举
            }, IntPtr.Zero);

            return result;
        }

        /// <summary>
        /// 获取指定窗口的详细信息
        /// </summary>
        /// <param name="handle">窗口句柄</param>
        /// <returns>窗口信息</returns>
        private static StandardWindowInfo GetWindowInfo(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                return null;

            try
            {
                var windowInfo = new StandardWindowInfo
                {
                    Handle = handle,
                    ParentHandle = MarsWindowsAPIs.GetParent(handle)
                };

                // 获取窗口类名
                var className = new StringBuilder(256);
                MarsWindowsAPIs.GetClassName(handle, className, 255);
                windowInfo.WindowClass = className.ToString();

                // 获取窗口文本
                var windowText = new StringBuilder(256);
                MarsWindowsAPIs.GetWindowText(handle, windowText, 255);
                windowInfo.WindowText = windowText.ToString();

                // 获取窗口矩形
                MarsWindowsAPIs.RECT rect;
                if (MarsWindowsAPIs.GetWindowRect(handle, out rect))
                {
                    windowInfo.Bounds = new Rectangle(rect.Left, rect.Top,
                        rect.Right - rect.Left, rect.Bottom - rect.Top);
                }

                // 获取可见性
                windowInfo.IsVisible = MarsWindowsAPIs.IsWindowVisible(handle);

                // 获取控件ID
                windowInfo.ControlId = MarsWindowsAPIs.GetDlgCtrlID(handle);

                return windowInfo;
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("GetWindowInfo", $"Error getting window info for {handle}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 将标准窗口信息转换为MarsSpiedObjectInfo
        /// </summary>
        /// <param name="windowInfo">标准窗口信息</param>
        /// <returns>MarsSpiedObjectInfo对象</returns>
        public static MarsSpiedObjectInfo ConvertToMarsSpiedObjectInfo(StandardWindowInfo windowInfo)
        {
            if (windowInfo == null)
                return null;

            var marsInfo = new MarsSpiedObjectInfo
            {
                obj_uuid = Guid.NewGuid().ToString(),
                hwnd = windowInfo.Handle.ToInt64(),
                objectName = string.IsNullOrEmpty(windowInfo.WindowText) ?
                    $"Control_{windowInfo.ControlId}" : windowInfo.WindowText,
                objectType = windowInfo.WindowClass,
                Text = windowInfo.WindowText ?? "",
                isVisible = windowInfo.IsVisible,
                x = windowInfo.Bounds.X,
                y = windowInfo.Bounds.Y,
                w = windowInfo.Bounds.Width,
                h = windowInfo.Bounds.Height,
                controlMarsType = GetMarsControlType(windowInfo.WindowClass),
                children = new List<MarsSpiedObjectInfo>(),
                allChildrenCount = 0
            };

            // 设置父窗口信息
            if (windowInfo.ParentHandle != IntPtr.Zero)
            {
                marsInfo.PegWindUUID = windowInfo.ParentHandle.ToInt64().ToString();
            }

            return marsInfo;
        }

        /// <summary>
        /// 根据窗口类名获取Mars控件类型
        /// </summary>
        /// <param name="windowClass">窗口类名</param>
        /// <returns>Mars控件类型</returns>
        private static string GetMarsControlType(string windowClass)
        {
            if (string.IsNullOrEmpty(windowClass))
                return "Unknown";

            switch (windowClass.ToLower())
            {
                case "button":
                    return "swfbutton";
                case "edit":
                    return "swfedit";
                case "static":
                    return "swfstatic";
                case "listbox":
                    return "swflistbox";
                case "combobox":
                    return "swfcombobox";
                case "checkbox":
                    return "swfcheckbox";
                case "radiobutton":
                    return "swfradiobutton";
                case "scrollbar":
                    return "swfscrollbar";
                case "listview":
                    return "swflistview";
                case "treeview":
                    return "swftreeview";
                case "tabcontrol":
                    return "swftabcontrol";
                case "toolbar":
                    return "swftoolbar";
                case "statusbar":
                    return "swfstatusbar";
                case "progressbar":
                    return "swfprogressbar";
                case "slider":
                    return "swfslider";
                case "datetimepicker":
                    return "swfdatetimepicker";
                case "monthcalendar":
                    return "swfmonthcalendar";
                case "groupbox":
                    return "swfgroupbox";
                case "panel":
                    return "swfpanel";
                default:
                    return $"swf{windowClass.ToLower()}";
            }
        }

        /// <summary>
        /// 构建树状结构的MarsSpiedObjectInfo列表
        /// </summary>
        /// <param name="parentHandle">父窗口句柄</param>
        /// <returns>树状结构的MarsSpiedObjectInfo列表</returns>
        public static List<MarsSpiedObjectInfo> BuildStandardObjectsTree(IntPtr parentHandle)
        {
            var allWindows = GetStandardChildWindows(parentHandle);
            var marsObjects = new List<MarsSpiedObjectInfo>();
            var marsObjectDict = new Dictionary<IntPtr, MarsSpiedObjectInfo>();

            // 首先转换所有窗口为MarsSpiedObjectInfo
            foreach (var windowInfo in allWindows)
            {
                var marsInfo = ConvertToMarsSpiedObjectInfo(windowInfo);
                if (marsInfo != null)
                {
                    marsObjectDict[windowInfo.Handle] = marsInfo;
                }
            }

            // 构建父子关系
            foreach (var windowInfo in allWindows)
            {
                if (!marsObjectDict.ContainsKey(windowInfo.Handle))
                    continue;

                var marsInfo = marsObjectDict[windowInfo.Handle];

                if (windowInfo.ParentHandle == parentHandle)
                {
                    // 顶级窗口
                    marsObjects.Add(marsInfo);
                }
                else if (marsObjectDict.ContainsKey(windowInfo.ParentHandle))
                {
                    // 子窗口
                    var parentMarsInfo = marsObjectDict[windowInfo.ParentHandle];
                    if (parentMarsInfo.children == null)
                        parentMarsInfo.children = new List<MarsSpiedObjectInfo>();
                    parentMarsInfo.children.Add(marsInfo);
                }
            }

            // 计算所有子节点数量
            foreach (var marsInfo in marsObjectDict.Values)
            {
                marsInfo.allChildrenCount = CountAllChildren(marsInfo);
            }

            return marsObjects;
        }

        /// <summary>
        /// 递归计算所有子节点数量
        /// </summary>
        /// <param name="marsInfo">MarsSpiedObjectInfo对象</param>
        /// <returns>子节点总数</returns>
        private static int CountAllChildren(MarsSpiedObjectInfo marsInfo)
        {
            if (marsInfo?.children == null || marsInfo.children.Count == 0)
                return 0;

            int count = marsInfo.children.Count;
            foreach (var child in marsInfo.children)
            {
                count += CountAllChildren(child);
            }
            return count;
        }


        public static System.Drawing.Image GetControlImage(MarsSpiedObjectInfo objInfo)
        {
            if (objInfo == null) return null;
            
            // Handle different object types
            if (objInfo.referenceToObj is AutomationElement autoElm)
            {
                objInfo.hwnd = autoElm.Current.NativeWindowHandle;
                return CaptureImageByHwnd(objInfo);
            }
            else if (objInfo.referenceToObj is Accessibility.IAccessible accObj)
            {
                // For IAccessible objects, capture by location instead of hwnd
                return CaptureImageByLocation(accObj, objInfo);
            } else if (objInfo.referenceToObj is System.Windows.Forms.Control)
            {
                return CaptureImageByHwnd(objInfo); 
            }
            return null;
        }

        /// <summary>
        /// 通过窗口句柄捕获图像
        /// </summary>
        private static System.Drawing.Image CaptureImageByHwnd(MarsSpiedObjectInfo objInfo)
        {
            try
            {
                if (objInfo.hwnd == 0) return null;

                IntPtr hwnd = new IntPtr(objInfo.hwnd);
                int width = objInfo.w > 0 ? objInfo.w : 200;
                int height = objInfo.h > 0 ? objInfo.h : 200;

                Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (Graphics gfxBmp = Graphics.FromImage(bmp))
                {
                    IntPtr hdcBitmap = gfxBmp.GetHdc();
                    bool succeeded = MarsWindowsAPIs.PrintWindow(hwnd, hdcBitmap, 0);
                    gfxBmp.ReleaseHdc(hdcBitmap);

                    if (!succeeded)
                    {
                        // PrintWindow失败时，直接从屏幕上捕获objectRect区域的图像
                        try
                        {
                            // 使用Graphics.CopyFromScreen直接从屏幕捕获objectRect区域
                            using (Graphics screenGfx = Graphics.FromImage(bmp))
                            {
                                screenGfx.CopyFromScreen(
                                    objInfo.x, objInfo.y,  // 源位置（屏幕坐标）
                                    0, 0,                  // 目标位置（图像内坐标）
                                    new System.Drawing.Size(objInfo.w, objInfo.h), // 捕获区域大小
                                    CopyPixelOperation.SourceCopy
                                );
                            }
                        }
                        catch (Exception screenEx)
                        {
                            simpleLog.MarsLoggerSimple.Warning("CaptureImageByHwnd", 
                                $"Failed to capture from screen: {screenEx.Message}");
                            
                            // 如果屏幕捕获也失败，尝试BitBlt作为最后的备选方案
                            IntPtr hWndDC = MarsWindowsAPIs.GetWindowDC(hwnd);
                            using (Graphics g = Graphics.FromImage(bmp))
                            {
                                IntPtr hDC = g.GetHdc();
                                try
                                {
                                    // BitBlt等代码可补充
                                }
                                finally
                                {
                                    g.ReleaseHdc(hDC);
                                    MarsWindowsAPIs.ReleaseDC(hwnd, hWndDC);
                                }
                            }
                        }
                    }
                }
                return bmp;
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("CaptureImageByHwnd", $"Error capturing image by hwnd: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 通过位置捕获 IAccessible 对象的图像
        /// </summary>
        private static System.Drawing.Image CaptureImageByLocation(Accessibility.IAccessible accObj, MarsSpiedObjectInfo objInfo)
        {
            try
            {
                accObj.accLocation(out int left, out int top, out int width, out int height, 0);
                
                if (width <= 0 || height <= 0)
                {
                    simpleLog.MarsLoggerSimple.Warning("CaptureImageByLocation", "IAccessible object has zero or negative dimensions");
                    return null;
                }

                // 确保坐标在屏幕范围内
                left = Math.Max(0, left);
                top = Math.Max(0, top);
                
                // 创建指定大小的位图
                Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    // 使用 CopyFromScreen 直接从屏幕复制指定区域的像素
                    g.CopyFromScreen(left, top, 0, 0, new System.Drawing.Size(width, height));
                }
                
                simpleLog.MarsLoggerSimple.Info("CaptureImageByLocation", $"Captured IAccessible image at ({left},{top}) size {width}x{height}");
                return bmp;
            }
            catch (Exception ex)
            {
                simpleLog.MarsLoggerSimple.Error("CaptureImageByLocation", $"Error capturing image by location: {ex.Message}", ex);
                return null;
            }
        }
    }

}

