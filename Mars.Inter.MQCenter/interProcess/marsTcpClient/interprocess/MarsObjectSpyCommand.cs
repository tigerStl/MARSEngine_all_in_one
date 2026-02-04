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

#if !_MarsToolsImport
using Mars.message.windowsWrapper.SystemUtil;
#endif
using System.Diagnostics;

namespace Mars.message.Inter.MQCenter.interProcess
{
    public class MarsObjectSpyCommand
    {
        
        public string spyType;
        
        public string targetWnd;
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
                }catch(Exception e)
                {
                    continue;
                }
            }
            return IntPtr.Zero;
        }

        public static  List<MarsSpiedObjectInfo> getCurrentAllObjectsOfContainer(ref bool isOk, ref string strError,
            QueryObjectRequst objReqInfo= null,
            bool isShowHighlight = false,
            bool isTypePthInclude = false)
        {
            simpleLog.MarsLoggerSimple.logBegin("getCurrentAllObjectsOfContainer",$"RequestInfo:{objReqInfo.typeOfGenerateSteps}");
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
                    if (c==null)
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


        public static List<MarsSpiedObjectInfo> getCurrentAllObjects(QueryObjectRequst objReqInfo=null,
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
                    if ((activeHwnd!=IntPtr.Zero)&&(!itm.Handle.Equals(activeHwnd)))
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
                        itm.Invoke(new Action(() => {
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
                
                if ((activeHwnd!=IntPtr.Zero) && (windowsHdlLst.Count > 0))
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
                            objNew.buildChildren(rootPeg:objNew);
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
                var lstOfObjs = getCurrentAllObjects();

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
        public const string cnst_swfnamePath     = "SwfName Path";
        public const string cnst_swfname         = "swfname";
        public const string cnst_text            = "Text";
        public const string cnst_index           = "Index";
        public const string cnst_type            = "swfType";
        public const string cnst_typePath        = "swfType Path";
        public const string cnst_appliedApp      = "Application Short Name";
        public const string cnst_isPegwindow     = "isPegwindow";                                                    

        public const string cnst_defaultPegwindowHint = "Set Pegwindow here";
        public const string cnst_defaultAppNameHint   = "Set Application Short Name";
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
                    ||((strTypeOfInherited.IndexOf(".ValueListDropDownUnsafe;")>=0))
                    ||((strTypeOfInherited.IndexOf(".Infragistics.Win.DropDownManager")>=0))
                    ||(strTypeOfInherited.IndexOf("DropDownForm;") >= 0))
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
                    || (strTypeOfInherited.IndexOf(".UltraToolbarsDockArea;")>=0)
                    ) return strTargetType = MarsObjectTypeMappings.cnst_swf_ToolBar;
                if ((strTypeOfInherited.IndexOf(".TabManager;") >= 0)
                    || (itm is winform.TabControl)
                    ) return strTargetType = MarsObjectTypeMappings.cnst_swf_Tab;
                if ((itm is winform.ButtonBase) 
                    || (strTypeOfInherited.IndexOf("ButtonUIElement") >=0))
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
                    ||(itm is winform.TreeView)
                   )
                {
                    return strTargetType = MarsObjectTypeMappings.cnst_swf_Tree;
                }else
                //if (strTypeOfInherited.IndexOf(".DropDownManager+DropDownForm") >= 0)
                {
                    simpleLog.MarsLoggerSimple.Info("getMarsObject", $"used to debug object and type|{strTypeOfInherited}");
                }
                ///这里需要添加类别和 mars类别映射的json文件
                if (engineMappingObj != null)
                {
                    strTargetType = engineMappingObj.getObjectTypeByTypePath(strTypeOfInherited, ref isOk,ref strError);
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
            var memberChecked = ReflectorForCSharp.GetMember(c, "Checked",ref isNotExist);
            if ((isNotExist)||(memberChecked==null))
            {
                simpleLog.MarsLoggerSimple.Error("GetCheckboxValue",strError = $"no checked member in |{c.GetType().FullName}|");
                isOk = false;
            }
            isOk = true;
            return memberChecked.ToString();
        }
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
        public virtual string toMarsObjString()
        {
            return string.IsNullOrEmpty(objectName)?$"swfname path:={objectNamePath}":$"SwfName:={objectName}";
        }

        public string getQuickAccess()
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
                strRslt = string.IsNullOrEmpty(objectName) ? strRslt
                    : $"{strRslt}\r\nswfName:={objectName}";
            if (!string.IsNullOrEmpty(objectNamePath))
                strRslt = string.IsNullOrEmpty(objectNamePath) ? strRslt
                    : $"{strRslt}\r\nswfName Path:={objectNamePath}";
            if (!string.IsNullOrEmpty(objectType))
                strRslt = string.IsNullOrEmpty(objectType) ? strRslt
                    : $"{strRslt}\r\nswfType:={objectType}";
            return strRslt ;
        }
    }

    public partial class MarsSpiedObjectInfo: MarsSpiedObjectBasicInfo
    {
        [ScriptIgnore]
        [DataMember(IsRequired = false)]
        public Object referenceToObj;
        [ScriptIgnore]
        [DataMember(IsRequired = false)]
        public List<MarsSpiedObjectInfo> children;

        public MarsSpiedObjectInfo()
        {
            this.obj_uuid = Guid.NewGuid().ToString(); 
        }

        public void buildChildren(bool isShowHighlight=false, MarsSpiedObjectBasicInfo rootPeg=null)
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
            catch (Exception e)
            {
                return null;
            }
        }
        private void loadFromControl(winform.Control itm, string parentNamePath, string strParentTypePath)
        {
            if (itm == null) return;
                        
            this.objectName = itm.Name;
            this.objectNamePath = string.IsNullOrEmpty(parentNamePath)?itm.Name:$"{itm.Name};{parentNamePath}";
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
                itm.Invoke(new Action(() => {
                    var screenPt = itm.PointToScreen(new System.Drawing.Point(-1,-1));
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
            if ((!string.IsNullOrEmpty(this.objectName))&&(!string.IsNullOrEmpty(this.Text)))
                return $"{this.objectName} [{this.Text}]";
            if (!string.IsNullOrEmpty(this.objectName))
            {
                return this.objectName;
            }
            if (!string.IsNullOrEmpty(this.Text))
                return this.Text;
            return $"({this.x},{this.y})-zorder:{this.zorder}";
        }

        
    }
}
