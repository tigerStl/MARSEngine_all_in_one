
using Mars.message.AutoTestingDriver.ErrorMessage;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.windowsWrapper.SystemUtil;
using MarsUFTAddins.IMars.tiger;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Mars.message.Inter.MQCenter.MarsObjectIdentifier
{
    internal delegate bool MarsObjectPropertiesFetching(System.Windows.Forms.Control targetObject, string strProperty, ref string strPropertyValue);
    internal class MarsformIndentifier
    {
        internal const string UN_KNOW_VALUE = "Unknow";
        public const string CNST_GROUPED_CELL = "GROUPED CELL";
        protected static Dictionary<string, MarsObjectPropertiesFetching> ObjectSupportedProperties = new Dictionary<string, MarsObjectPropertiesFetching>
        {
            {"IS OWNED WINDOW",  Mars_IsOwnedWindow },
            {"IS CHILD WINDOW",  Mars_IsChildWindow },
            {"TEXT"           ,  Mars_ControlText   },
            {"REGEXPWNDTITLE" ,  Mars_ControlText   },
            {"NAME"           ,  Mars_ObjectName    },
            {"SWFNAME"        ,  Mars_ObjectName    },
            {"SWFTYPENAME"    ,  Mars_ObjectTypeName},
            {"SWFTYPE"        ,  Mars_ObjectTypeName},
            {"SWFNAME PATH"   ,  Mars_NamePath      },  //回溯parent同时获得其swfname
            {"SWFTYPE PATH"   ,  Mars_TypePath      },
            {"OBJECT CLASS"   ,  Mars_ObjectClass   },
            {"VISIBLE"        ,  Mars_ControlVisible},
            {CNST_GROUPED_CELL,  Mars_ObjectClass   },
        };

        internal static List<string> marsGridCellObjectType = new List<string>() { "opicsCellColIndex", "cellRowIdx" };
        private static bool IsTypeShouldSkipCheckOnObjectChecking(string strObjProType)
        {
            if (string.IsNullOrEmpty(strObjProType)) return true;
            if (IsGridCellPro(strObjProType)) return true;

            return false;
        }

        public static bool IsGridCellPro(string strPro)
        {
            foreach (var itm in marsGridCellObjectType)
            {
                if (string.Compare(strPro, itm, true) == 0) return true;
            }
            return false;
        }

        public static bool ContainsGridCellProper(Dictionary<string, string> srcProps,
            Dictionary<string, string> targetProps)
        {
            if ((srcProps == null) || (targetProps == null)) return false;
            bool isFind = false;
            foreach (var itm in srcProps.Keys)
            {
                if (string.IsNullOrEmpty(itm)) continue;
                if (IsTypeShouldSkipCheckOnObjectChecking(itm))
                {
                    targetProps.Add(itm, srcProps[itm]);
                    isFind = true;
                }
            }
            return isFind;
        }


        private static bool Mars_IsOwnedWindow(Control targetObject, string strProperty, ref string strPropertyValue)
        {
            MarsLoggerSimple.Info("\t", "Mars_IsOwnedWindow begins...");
            bool isTrue = false;
            try
            {
                strPropertyValue = "false"; //always return ffalse ;
                if (targetObject.InvokeRequired)
                {
                    Form f = null;
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                    {
                        f = targetObject as Form;

                        //qtp上注释：Indicates whether the object's window has an owner window.
                        //就是说是否有parent?
                        if (f != null)
                        {
                            //strPropertyValue = f.IsMdiContainer + "";
                            isTrue = f.Owner != null;
                        }
                        else
                        {
                            MarsLoggerSimple.Error("Mars_IsOwnedWindow", string.Format("invoke,object is not form, it is :[{0}]", f.GetType()));
                        }

                    }));
                    strPropertyValue = isTrue + "";
                    //MarsLoggerSimple.Info("Mars_IsOwnedWindow", string.Format("Forma name [{0}] is true returns :[{1}]", f.Name, isTrue));
                    return true;
                }
                else
                {
                    Form f = targetObject as Form;
                    //qtp上注释：Indicates whether the object's window has an owner window.
                    //就是说是否有parent?
                    if (f != null)
                    {
                        //strPropertyValue = f.IsMdiContainer + "";
                        isTrue = f.Owner != null;
                    }
                    else
                    {
                        MarsLoggerSimple.Error("Mars_IsOwnedWindow", string.Format("object is not form, it is :[{0}]", f.GetType()));
                    }

                    strPropertyValue = isTrue + "";
                    MarsLoggerSimple.Info("Mars_IsOwnedWindow", string.Format("Forma name [{0}] is true returns :[{1}]", targetObject.Name, isTrue));
                    return true;
                }

            }
            catch (Exception e)
            {
                MarsLoggerSimple.Error("\t", string.Format("Exception:[{0}]", e.Message), e);
                return isTrue = false;
            }
            finally
            {
                //strProperty = string.Format("{0}", isTrue);
                MarsLoggerSimple.Info("\t", string.Format("reutrns [{0}] mmm [{1}]", isTrue, strPropertyValue));
            }
        }

        private static bool Mars_TypePath(Control targetObject, string strProperty, ref string strPropertyValue)
        {
            strPropertyValue = GetParentTypePath(targetObject);// ReflectorForCSharp.GetObjectBaseType(targetObject.GetType());
            return true;
        }

        public static string GetParentTypePath(Control c)
        {
            while ((c != null) && (c.Parent != null))
            {
                return string.Format("{0};{1}", GetParentTypePath(c.Parent as Control), c.GetType().ToString());
            }
            return "";
        }

        public static bool Mars_NamePath(Control targetObject, string strProperty, ref string strPropertyValue)
        {
            strPropertyValue = "";
            if (targetObject == null) return false;
            if (targetObject.Parent != null)
            {
                //opics 的对象 swfname path 中包括对象本身的名称，并且最后有“;”
                strPropertyValue = ReflectorForCSharp.MarsGetParentsNames(targetObject.Parent);
            }
            else
                strPropertyValue = "";
            return true;
        }

        public static string MarsGetParentsNames(Control targetObject)
        {
             
            while (targetObject != null)
            {
                return targetObject.Name + ";" + MarsGetParentsNames(targetObject.Parent);
            }
            return "";
        }

        private static bool Mars_ObjectName(Control targetObject, string strProperty, ref string strPropertyValue)
        {
            strPropertyValue = UN_KNOW_VALUE;
            if (targetObject == null) return false;
            strPropertyValue = targetObject.Name;
            return true;
        }

        private static bool Mars_ObjectTypeName(Control targetObject, string strProperty, ref string strPropertyValue)
        {
            strPropertyValue = UN_KNOW_VALUE;
            if (targetObject == null) return false;
            strPropertyValue = targetObject.GetType().ToString();
            return true;
        }

        private static bool Mars_ControlVisible(Control targetObject, string strProperty, ref string strPropertyValue)
        {
            strPropertyValue = UN_KNOW_VALUE;
            if (targetObject == null) return false;
            strPropertyValue = targetObject.Visible?"True":"False";
            return true;
        }

        private static bool Mars_ObjectClass(Control targetObject, string strProperty, ref string strPropertyValue)
        {
            strPropertyValue = UN_KNOW_VALUE;
            if (targetObject == null) return false;
            StringBuilder sName = new StringBuilder(256);
            windowsWrapper.SystemUtil.MarsWindowsAPIs.GetClassName(targetObject.Handle, sName, 255);
            strPropertyValue = sName.ToString();
            return true;
        }

        private static bool Mars_ControlText(Control targetObject, string strProperty, ref string strPropertyValue)
        {
            strPropertyValue = UN_KNOW_VALUE;
            if (targetObject == null) return false;
            string strValueTmp = "";
            if (targetObject.InvokeRequired)
            {

                targetObject.Invoke(new Action(() =>                
                //System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                {
                    strValueTmp = targetObject.Text;
                    if (string.IsNullOrEmpty(strValueTmp))
                    {
                        bool isNotExist = false;
                        //object oTtileForSummitWindow = ReflectorForCSharp.GetMember(targetObject, "WindowTitle", ref isNotExist);
                        //if (!isNotExist)
                        //{
                        //    strValueTmp = oTtileForSummitWindow == null ? "" : oTtileForSummitWindow.ToString();
                        //}
                        //else
                        {
                            if (targetObject is System.Windows.Forms.Form)
                            {

                            }
                            int iLen = windowsWrapper.SystemUtil.MarsWindowsAPIs.GetWindowTextLength(targetObject.Handle);
                            StringBuilder sb = new StringBuilder(256);
                            iLen = windowsWrapper.SystemUtil.MarsWindowsAPIs.GetWindowText(targetObject.Handle, sb, 255);
                            strValueTmp = sb.ToString();
                        }
                    }

                }));
                strPropertyValue = strValueTmp;
            }
            else
            {
                strPropertyValue = targetObject.Text;
                if (string.IsNullOrEmpty(strPropertyValue))
                {
                    //bool isNotExist = false;
                    //object oTtileForSummitWindow = ReflectorForCSharp.GetMember(targetObject, "WindowTitle", ref isNotExist);
                    //if (!isNotExist)
                    //{
                    //    strPropertyValue = oTtileForSummitWindow == null ? "" : oTtileForSummitWindow.ToString();
                    //}
                    //else
                    {
                        int iLen = windowsWrapper.SystemUtil.MarsWindowsAPIs.GetWindowTextLength(targetObject.Handle);
                        StringBuilder sb = new StringBuilder(256);
                        iLen = windowsWrapper.SystemUtil.MarsWindowsAPIs.GetWindowText(targetObject.Handle, sb, 255);
                        strPropertyValue = sb.ToString();
                    }
                }
            }

            MarsLoggerSimple.Info("Mars_ControlText", string.Format("Property:[{0}] value:[{1}] width:[{2}]", strProperty, strPropertyValue, targetObject.Bounds));

            return true;
        }

        /**
         * 返回表示是否处理成功。通过rpoperty value获得数据
         * */
        protected static bool Mars_IsChildWindow(System.Windows.Forms.Control targetObject, string strProperty, ref string strPropertyValue)
        {
            strPropertyValue = UN_KNOW_VALUE;
            if (targetObject == null) return false;

            System.Windows.Forms.Form f = targetObject as Form;
            //strPropertyValue = ((f.Parent!=null)||f.IsMdiChild || ((f.OwnedForms != null) && (f.OwnedForms.Length >= 1)))+"";
            strPropertyValue = ((f.Parent != null) || f.IsMdiChild) + "";// || ((f.OwnedForms != null) && (f.OwnedForms.Length >= 1))) + "";
            MarsLoggerSimple.Info("\t", string.Format("IsMDIChild:[{0}] parent:[{1}] parentForm:[{2}] OwnedForms Length:[{3}]",
                f.IsMdiChild, f.Parent, f.ParentForm, f.OwnedForms.Length));
            if (f != null)
            {
                /**
                 * 20190701修改
                 * */
                //return f.IsMdiChild||f.Parent!=null;
                return true;
            }
            return true;
        }

        protected string objectName;
        protected IntPtr assignedHandle;
        protected Dictionary<string, string> objectPropertyAndItsValues = null;
        protected bool isControlvisible;
        public bool IsControlVisible
        {
            get
            {
                return isControlvisible;
            }
            set
            {
                isControlvisible = value;
            }
        }

        public string ObjectName
        {
            get => objectName;
            set => objectName = value;
        }

        public Dictionary<string, string> ObjectPropertyAndItsValues
        {
            get => objectPropertyAndItsValues;
            set => objectPropertyAndItsValues = value;
        }

        public IntPtr WindowHandle
        {
            get
            {
                return assignedHandle;
            }
            set
            {
                assignedHandle = value;
            }
        }

        public object AssignedForm
        {
            get;
            set;
        }

        private static string FixedSwfnamePath(string strObjectName, string strSrc)
        {
            if (strSrc == null) return null;
            if (strSrc.EndsWith(";"))
            {
                return strObjectName + ";" + strSrc.Substring(0, strSrc.Length - 1);
            }
            return strObjectName + ";" + strSrc;
        }
        /// <summary>
        /// common objects
        /// </summary>
        /// <param name="o"></param>
        /// <param name="objProperties"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        internal static MarsformIndentifier FetchControlInfomation(Control objContorl, Dictionary<string, string> objProperties,
            string strPegName, string strObjName,
            ref bool isOk,
            ref string strError,
            ref string strAdv,
            ref string strStack)
        {
            MarsLoggerSimple.logBegin("MarsformIndentifier.FetchControlInfomation", string.Format("Try to get object information by properties:[{0}]", string.Join(";", objProperties.Keys.ToArray())));
            if (objContorl == null)
            {
                MarsLoggerSimple.Error("\tFetchControlInfomation", strError = "Passed null to a function");
                StackFrame stck = (new StackFrame());
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Contact Marquis";
                isOk = false;
                return null;
            }
            MarsformIndentifier objResult = new MarsformIndentifier();

            isOk = false;
            objResult.assignedHandle = objContorl.Handle;
            objResult.isControlvisible = objContorl.Visible;
            string sIdx = null;
            int iIdx = -1, iLocationPos = -1;
            string sLocation = null;

            if (objProperties.Keys.Count == 0)
            {
                MarsLoggerSimple.Warnning("\tFetchControlInfomation", "No object ids is passed, is a pegwindow?");
                isOk = true;
                return objResult;
            }
            List<string> lstkey = objProperties.Keys.ToList();
            for (int i = 0; i < lstkey.Count; i++)
            { // (string proOrg in objProperties.Keys) {
                try
                {
                    string proOrg = lstkey[i];
                    string strProSupport = proOrg == null ? "" : proOrg.ToUpper();
                    MarsLoggerSimple.Info("\t", string.Format("try to get property:[{0}]", strProSupport));

                    if ((string.Compare("index", proOrg, true) == 0))
                    {
                        sIdx = objProperties[proOrg];
                        if (!int.TryParse(sIdx, out iIdx))
                            iIdx = 0;
                        continue;
                    }
                    if (string.Compare("location", proOrg, true) == 0)
                    {
                        sLocation = objProperties[proOrg];
                        if (!int.TryParse(sIdx, out iLocationPos))
                            iLocationPos = 0;
                        continue;
                    }
                    if (IsTypeShouldSkipCheckOnObjectChecking(proOrg)) continue;

                    if (!ObjectSupportedProperties.ContainsKey(strProSupport))
                    {
                        // no index deal with
                        MarsLoggerSimple.Error("\t", strError = string.Format("Unsupported properties:[{0}]-[{1}]", proOrg, objProperties[proOrg]));
                        strError = $"Object property [{proOrg}] and its value [{objProperties[proOrg]}] are not supported ";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Make sure object identification is correct. Use Object Spy to identify the problem";
                        isOk = false;
                        return null;
                    }

                    //if (string.Compare("text", proOrg, true) == 0)
                    //{
                    //    char[] ac = new char[] { '^', '{', '.' };
                    //    if (!ac.Contains(objProperties[proOrg][0]))
                    //    {
                    //        objProperties[proOrg] = "^" + objProperties[proOrg];
                    //    }
                    //}

                    string strProValue = "";
                    isOk = ObjectSupportedProperties[strProSupport](objContorl, proOrg, ref strProValue);
                    if (!isOk)
                    {
                        MarsLoggerSimple.Error("\t", strError = string.Format("Can't get [{0}]'s value from object, value requires :[{1}]", proOrg, objProperties[proOrg]));
                        strError = $"Object property [{proOrg}] doesn't support";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Make sure object identification is correct. Use Object Spy to identify the problem";
                        return null;
                    }

                    /// for test
                    /// 
                    if (strProSupport.Equals("swftypename", StringComparison.OrdinalIgnoreCase))
                    {
                        string tmpType = "";
                        Mars_ObjectTypeName(objContorl, proOrg, ref tmpType);
                        simpleLog.MarsLoggerSimple.Info("\t", $"when swftypename matchs|name path is|{tmpType}");
                    }

                    MarsLoggerSimple.Info("\t", string.Format("key property:[{0}] value to test:[{1}] control value:[{2}]", proOrg, objProperties[proOrg], strProValue));
                    //判断是否是正则表达式
                    if ((objProperties[proOrg].Contains(".*")) || (objProperties[proOrg].StartsWith("^"))
                        || objProperties[proOrg].EndsWith("$") || objProperties[proOrg].Contains("|")
                        || objProperties[proOrg].Contains("\\.")
                        )
                    {
                        if (!MarsWindowsAPIsExtend.RegularTest(objProperties[proOrg], strProValue))
                        {
                            isOk = false;
                            if (string.Compare("swfname path", proOrg, true) == 0)
                            {

                                string strProvalueEx = FixedSwfnamePath(objContorl.Name, strProValue);
                                MarsLoggerSimple.Info("\t", string.Format("regex go to swfname path {0}", strProvalueEx));
                                //opics的swfName Path 包括本身的名称，最后无";"结尾
                                isOk = MarsWindowsAPIsExtend.RegularTest(objProperties[proOrg], strProvalueEx);
                            }
                            if (!isOk)
                            {
                                MarsLoggerSimple.Error("\t", strError = string.Format("regular express property [{0}] requires [{1}] but [{2}] returns", proOrg, objProperties[proOrg], strProValue));
                                strError = $"Object [{strObjName}] property [{objProperties[proOrg]}] dose not Match";
                                strStack = MarsErrorStacks.StackTraceDump();
                                strAdv = "Make sure object identification is correct. Use Object Spy to identify the problem";
                                return null;
                            }
                        }
                    }
                    else
                    {
                        if (string.Compare(objProperties[proOrg], strProValue) != 0)
                        {
                            isOk = false;
                            if (string.Compare("swfname path", proOrg, true) == 0)
                            {
                                string strProvalueEx = FixedSwfnamePath(objContorl.Name, strProValue);
                                MarsLoggerSimple.Info("\t", string.Format("go to swfname path {0}", strProvalueEx));
                                //opics的swfName Path 包括本身的名称，最后以";"结尾
                                isOk = string.Compare(objProperties[proOrg], strProvalueEx) == 0;
                            }
                            if (!isOk)
                            {
                                MarsLoggerSimple.Error("\t", strError = string.Format("property [{0}] Comparison requires [{1}] but [{2}] returns", proOrg, objProperties[proOrg], strProValue));
                                StackFrame stck = (new StackFrame());
                                strStack = MarsErrorStacks.StackTraceDump();
                                strAdv = "Make sure window identification is correct. Use Object Spy to identify and correct";
                                return null;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    MarsLoggerSimple.Error("\t", e.Message, e);
                }
            }

            if (!isOk)
            {
                ///empty id
                /// 
                MarsLoggerSimple.Error("\t", strError = "object identification is NULL");
                strStack = MarsErrorStacks.StackTraceDump();
                strAdv = "Make sure window identification is correct. Use Object Spy to identify";
                return null;
            }

            ///check visible later
            //objResult.IsControlVisible = MarsWindowsAPIs.IsWindowVisible(objContorl.Handle);
            //if (!objResult.IsControlVisible)
            //{
            //    MarsLoggerSimple.Error("\t",strError = "Object is not visible." );
            //    isOk = false;
            //    return null;
            //}
            return objResult;
        }

        internal static MarsformIndentifier FetchPegwindowInformation(Control objContorl, Dictionary<string, string> objPegProperties,
            string strPegName, string strObjName,
            ref bool isOk, ref string strError,
            ref string strAdv, ref string strStack)
        {
            simpleLog.MarsLoggerSimple.logBegin("FetchPegwindowInformation");
            string strProperValue = "";
            Dictionary<string, string> tmpProAndValue = new Dictionary<string, string>();
            string objName = "";
            try
            {
                List<string> lstKeys = objPegProperties.Keys.ToList();
                for (int i = 0; i < lstKeys.Count; i++)
                {
                    var key = lstKeys[i];
                    MarsLoggerSimple.Info("\t", string.Format("Current key:[{0}], total count:[{1}]", key, lstKeys.Count));
                    if (objPegProperties[key] == null) continue;
                    //}
                    //foreach (var key in objPegProperties.Keys)
                    //{
                    if (string.IsNullOrEmpty(key)) continue;
                    if (string.Compare("index", key, true) == 0)
                    {
                        tmpProAndValue.Add("INDEX", objPegProperties[key]);
                        continue;
                    }
                    else
                    {
                        if (!ObjectSupportedProperties.ContainsKey(key.ToUpper()))
                        {
                            strError = $"Keyword doesn't support [{key}] ";// string.Format("Non supported property checking:[{0}]", key);
                            strStack = MarsErrorStacks.StackTraceDump();
                            strAdv = "Mars supports Infragistics, WinForm and WPF controls";
                            isOk = false;
                            return null;
                        }
                    }
                    try
                    {
                        if (string.Compare("is owned window", key, true) == 0)
                        {
                            MarsLoggerSimple.Info("\t", string.Format("is owned window begins, function:[{0}]", ObjectSupportedProperties[key.ToUpper()]));
                        }
                        bool isGetData = ObjectSupportedProperties[key.ToUpper()](objContorl, key.ToUpper(), ref strProperValue);
                        if (string.Compare("is owned window", key, true) == 0)
                        {
                            MarsLoggerSimple.Info("\t", string.Format("is owned window end [{0}]-prepervalue:[{1}]", isGetData, strProperValue));
                        }
                        if (!isGetData)
                        {
                            strError = $"Object property [{key}] is NULL";// string.Format("Can't get property:[{0}] for Type:[{1}]", key, objContorl.GetType().ToString());
                            strStack = $"Can't get property:[{key}] for Type:[{objContorl.GetType().ToString()}]\r\n{MarsErrorStacks.StackTraceDump()}";
                            strAdv = "Contact Marquis";
                            isOk = false;
                            return null;
                        }
                    }
                    catch (Exception e)
                    {
                        MarsLoggerSimple.Error("\t", string.Format("exception:[{0}]", e.Message, e));
                    }
                    #region just folder
                    //if (string.Compare("text", key, true) == 0)
                    //{
                    //    char[] ac = new char[] { '^', '{', '.' };
                    //    if (!ac.Contains(objPegProperties[key][0]))
                    //    {
                    //        objPegProperties[key] = "^" + objPegProperties[key];
                    //    }
                    //}
                    #endregion
                    ///采用正则表达式比较
                    /// 
                    if (MarsWindowsAPIsExtend.RegularTest(objPegProperties[key], strProperValue) || string.Compare(objPegProperties[key], strProperValue, true) == 0)
                    {
                        MarsLoggerSimple.Info("\t", string.Format("try to get:[{0}]-[{1}] returns [{2}] matched", key, objPegProperties[key], strProperValue));
                        tmpProAndValue.Add(key.ToUpper(), strProperValue);
                    }
                    else
                    {
                        MarsLoggerSimple.Info("\t", string.Format("try to get:[{0}]-[{1}] returns [{2}] not matched", key, objPegProperties[key], strProperValue));
                        strError = $"Object [{strObjName}] is not found in Pegwindow [{strPegName}]";
                        strStack = MarsErrorStacks.StackTraceDump();
                        strAdv = "Make sure Object exists and is visibale; Make sure Object identification is correct. Use Object Spy to identify the problem";
                        isOk = false;
                        return null;
                    }
                }
                if (!(tmpProAndValue.ContainsKey("NAME") || (tmpProAndValue.ContainsKey("SWFNAME"))))
                {
                    objName = objContorl.Name;
                    tmpProAndValue.Add("SWFNAME", objName);
                }

                if (tmpProAndValue.ContainsKey("SWFNAME"))
                    objName = tmpProAndValue["SWFNAME"];
                else
                {
                    objName = tmpProAndValue["NAME"];
                }

                MarsformIndentifier objResult = new MarsformIndentifier();
                objResult.objectPropertyAndItsValues = tmpProAndValue;
                objResult.objectName = objName;
                objResult.assignedHandle = objContorl.Handle;
                isOk = true;
                return objResult;
            }
            catch (Exception e)
            {
                MarsLoggerSimple.Error("FetchPegwindowInformation", strError = string.Format("Exception:[{0}]\r\n\t{1}", e.Message, e.StackTrace));
#if !_ForClickOnce
                System.Diagnostics.EventLog.WriteEntry("Application", strError = string.Format("Exception:[{0}]\r\n\t{1}", e.Message, e.StackTrace));
#else 
                MarsLoggerSimple.Error("FetchPegwindowInformation", e.Message, e.StackTrace);
#endif
                strError = $"Error While finding parent window for a control [{strPegName}].[{strObjName}]";
                StackFrame stck = (new StackFrame());
                strStack = $"{e.Message}\r\n{e.StackTrace}";
                strAdv = "Unidentified error. If this continues, contact Marquis";
                isOk = false;
                return null;
            }
            finally
            {
                simpleLog.MarsLoggerSimple.logEnd("FetchPegwindowInformation");
            }
        }
    }


}
