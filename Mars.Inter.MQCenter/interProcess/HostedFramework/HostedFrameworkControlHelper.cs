using Mars.Inter.MQCenter.DataLayer.network.ErrorCheckData;
using Mars.Inter.MQCenter.interProcess.FrameworkOp;
using Mars.Inter.MQCenter.ThirdPartComponent.DevExpress;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.Utility;
using Mars.message.windowsWrapper.SystemUtil;
using MarsUFTAddins.IMars.tiger;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Markup.Localizer;

namespace Mars.Inter.MQCenter.interProcess.HostedFramework
{

    public class SearchAndClickData
    {
        public string MouseAction { get; set; }
        public List<string> DataToCompare { get; set; }

        public string sourceSettings { get; set; }

        /// <summary>
        /// 格式：LEFT_CLICK;[DATA1:DATA2]
        /// </summary>
        /// <param name="dataSettings"></param>
        /// <returns></returns>
        public static SearchAndClickData GetInstance(string dataSettings)
        {
            if (string.IsNullOrEmpty(dataSettings)) return null;
            string[] actAndData = dataSettings.Split(new string[]{ ";" }, StringSplitOptions.RemoveEmptyEntries);
            if (actAndData.Length < 2) { return null; }
            SearchAndClickData rslt = new SearchAndClickData();
            rslt.MouseAction = actAndData[0];
            string input = actAndData[1];
            // 去除前后的方括号
            string trimmed = input.Trim('[', ']');
            // 按冒号分割
            List<string> result = trimmed.Split(':').ToList();
            // result = { "a", "b", "c" }
            rslt.DataToCompare = result;
            rslt.sourceSettings = dataSettings;  
            return rslt;
        }
    }


    public class HostedFrameworkControlGlobalVar
    {
        public static IntPtr Current_Root_Hwnd { get; set; } = IntPtr.Zero;
        public static System.Windows.Forms.Control Currernt_Root_Control { get; set; } = null;
        public static int current_RootObjectType { get; set; } = 1; // 1: framework, 2: wpf
    }

    public class HostedFrameworkControlKeywordHelper
    {

        public const string cnst_hostclass_forwpf = "System.Windows.Forms.Integration.WinFormsAdapter";

        public static IntPtr GetIntegration_WinFormsAdapter_Handle(List<IntPtr> hwnds)
        {
            foreach (var h in hwnds)
            {
                if (h.Equals(IntPtr.Zero)) continue;
                try
                {
                    System.Windows.Forms.Control cntrl = System.Windows.Forms.Control.FromHandle(h);
                    if (cntrl==null) continue;
                    
                    string fullType = cntrl.GetType().FullName;
                    if (fullType.IndexOf(cnst_hostclass_forwpf) >= 0)
                    {
                        return h;
                    }
                }
                catch
                {
                    continue;
                }
            }
            return IntPtr.Zero;
        }


        private static bool FindObjects(string strKeyword, Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            List<System.Windows.Forms.Control> matchedControls,
            ref string strError, ref string strAdv, ref string strStack
            )
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("FindObjects", $"{iMark}|{strKeyword}|{MarsWindowsAPIsExtend.Dic2String(objPegProperties)}|{MarsWindowsAPIsExtend.Dic2String(objProperties)}");
            ///1，获得hwnd，判断是否和HostedFrameworkControlGlobalVar中的Current_Root_Hwnd一致，如果不一致，则获得新的对象
            ///
            string tmpHwndStr = objPegProperties[MarsConstants.cnst_pegProperty_hwnd_fromuiaa];
            IntPtr targetHwnd = IntPtr.Zero;
            try
            {
                targetHwnd = (IntPtr)Convert.ToInt64(tmpHwndStr);
            }
            catch
            {
                targetHwnd = IntPtr.Zero;
            }
            if (targetHwnd.Equals(IntPtr.Zero))
            {
                strError = "HostedFrameworkControlKeywordHelper::FindObjects failed, invalid hwnd value: " + tmpHwndStr;
                strAdv = "Please ensure that target window has main window";
                strStack = Environment.StackTrace;

                MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.FindObjects", $"{iMark}|{strKeyword}|{strError}\r\n{strAdv}\r\n{strStack}");
                return false;
            }
            if (!HostedFrameworkControlGlobalVar.Current_Root_Hwnd.Equals(targetHwnd))
            {
                /// 从中寻找intergrated对象

                var lstHwnd = MarsWindowsAPIsExtend.GetChildWindows(targetHwnd);
                /// build 对象
                /// 
                lstHwnd.Insert(0, targetHwnd);
                IntPtr winFormsAdapterHwnd = GetIntegration_WinFormsAdapter_Handle(lstHwnd);
                if (winFormsAdapterHwnd.Equals(IntPtr.Zero))
                {
                    strError = "No System.Windows.Forms.Integration.WinFormsAdapter instance is found";
                    strAdv = "Please ensure that .net framework controls are hosted in WPF";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.FindObjects", $"{iMark}|{strKeyword}|{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }
                System.Windows.Forms.Control targetControl = null;
                try
                {
                    targetControl = System.Windows.Forms.Control.FromHandle(winFormsAdapterHwnd);
                }
                catch (Exception e)
                {
                    strError = "HostedFrameworkControlKeywordHelper::FindObjects failed, can not get Control from hwnd: " + tmpHwndStr;
                    strAdv = "Please ensure that target window has main window";
                    strStack = e.ToString();
                    MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.FindObjects", $"{iMark}|{strKeyword}|{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }
                HostedFrameworkControlGlobalVar.Currernt_Root_Control = targetControl;
            }
            /// 2, 遍历HostedFrameworkControlGlobalVar.Currernt_Root_Control所有的子对象，以及孙对象，找到符合条件的对象
            /// 
            List<object> childCntrls = new List<object>();
            MarsFrameworkHelper.MarsRecursiveGetAllChildren(HostedFrameworkControlGlobalVar.Currernt_Root_Control, childCntrls, false);
            MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.FindObjects", $"{iMark}| Total child controls count: {childCntrls.Count}");

             //= new List<System.Windows.Forms.Control>();
            bool isMatched = false;
            foreach (var o in childCntrls)
            {
                System.Windows.Forms.Control ctrl = o as System.Windows.Forms.Control;
                if (ctrl == null) continue;
                isMatched = false;
                bool isNotExists = false;
                bool isSkip = false;
                foreach (var k in objProperties.Keys)
                {
                    string v = objProperties[k];
                    string v_FromControl = string.Empty;
                    isSkip = false;
                    switch (k.ToLower())
                    {
                        case "name":
                        case "swfname":
                            v_FromControl = ctrl.Name;
                            break;
                        case "text":
                        case "catpion":
                            /// 通过反射获得text属性
                            /// 
                            var tmpO = ReflectorForCSharp.GetMember(ctrl, "Text", ref isNotExists);
                            if (!isNotExists)
                            {
                                strError = $"no such property or member |Text| from object|{ctrl.GetType().FullName}";
                                strAdv = "Please ensure the property or member is correct";
                                strStack = Environment.StackTrace;
                                MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.FindObjects", $"{iMark}|{strKeyword}|{strError}\r\n{strAdv}\r\n{strStack}");
                                return false;
                            }
                            break;
                        case "catalog":
                            isSkip = true;
                            break;
                        case "swftype":
                        case "type":
                            v_FromControl = o.GetType().FullName;
                            break;
                        default:
                            strError = $"HostedFrameworkControlKeywordHelper::FindObjects failed, unsupported property key: {k}";
                            strAdv = "Please ensure the property key is correct";
                            strStack = Environment.StackTrace;
                            MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.FindObjects", $"{iMark}|{strKeyword}|{strError}\r\n{strAdv}\r\n{strStack}");
                            return false;
                    }
                    if (isSkip) continue;
                    if ((string.Compare(v, v_FromControl, StringComparison.OrdinalIgnoreCase) == 0)
                        || (MarsWindowsAPIsExtend.RegularTest(v, v_FromControl))
                    )
                    {
                        isMatched = true;
                    }
                    else
                    {
                        isMatched = false;
                        break;
                    }

                }
                if (isMatched)
                {
                    matchedControls.Add(ctrl);
                }
            }
            MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.FindObjects", $"{iMark}| find object|{matchedControls.Count}/{childCntrls.Count}");
            if (matchedControls.Count != 1)
            {
                strError = "PressKey failed, matched controls count is not 1, actual count: " + matchedControls.Count;
                strAdv = "Please ensure that the object properties are correct and unique";
                strStack = Environment.StackTrace;
                MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.FindObjects", $"{iMark}|{strKeyword}|{strError}\r\n{strAdv}\r\n{strStack}");
                return false;
            }
            MarsLoggerSimple.logEnd("HostedFrameworkControlKeywordHelper.FindObjects", $"{iMark}|{strKeyword}|FindObjects success");
            return true;
        }

        internal static bool PressKey(string strParaMeter, string strData, string strobjType, string strAttachInfo, string strPegName,
            string strObjName, Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj, ref string strError, ref string strDataReturn,
            ref string strStack, ref string strAdv, ref string strSnapshotForShouldBeFile, bool isInnerCall, int waitingTime)
        {
            int iMark = new Random().Next(100000);
            MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.PressKey", $"{iMark}|PressKey enter, strObjName:{strObjName}, strPegName:{strPegName}");
            try
            {
                if (!objPegProperties.ContainsKey(MarsConstants.cnst_pegProperty_hwnd_fromuiaa))
                {
                    strError = "HostedFrameworkControlKeywordHelper::PressKey failed, missing peg property: " + MarsConstants.cnst_pegProperty_hwnd_fromuiaa;
                    strAdv = "Please ensure that target window has main window";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.PressKey", $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }

                /// 算法：
                /// 1，获得hwnd，判断是否和HostedFrameworkControlGlobalVar中的Current_Root_Hwnd一致，如果不一致，则获得新的对象
                /// 

                //var dataAndAction = SearchAndClickData.GetInstance(strData);
                //if (dataAndAction == null)
                //{
                //    strError = $"Data format is wrong|{strData}";
                //    strAdv = "please Ensure that data format is MouseAction;[data1:data2]";
                //    strStack = Environment.StackTrace;
                //    MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.PressKey", $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                //    return false;
                //}
                List<System.Windows.Forms.Control> matchedControls = new List<System.Windows.Forms.Control>();
                if (!FindObjects("PressKey",objProperties, objPegProperties, matchedControls, ref strError, ref strAdv, ref strStack))
                {
                    MarsLoggerSimple.Error("PressKey", $"{iMark}|{strError}|{strAdv}|{strStack}");
                    return false;
                }
                /// 3, 对找到的对象, 判断是devexpress还是Infragistics，进行search
                /// 
                string strTypes = GetBaseTypesUntilSystemWindowsFormsControl(matchedControls[0].GetType());
                string matchedText = "";
                object matchedNode = null;

                matchedControls[0].Focus();
                System.Threading.Thread.Sleep( 200 );
                System.Windows.Forms.SendKeys.SendWait(strData);
                System.Threading.Thread.Sleep(200);
                /// 使用postmessage实现
                IntPtr targetHandle = matchedControls[0].Handle;
                if (targetHandle == IntPtr.Zero)
                {
                    strError = "PressKey failed, target control handle is zero.";
                    strAdv = "Ensure the control is created and visible before sending keyboard messages.";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.PressKey", $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }

                const int keyDownLParam = 0x00000001;
                const int keyUpLParam = unchecked((int)0xC0000001);
                int vkLeft = (int)System.Windows.Forms.Keys.Left;

                MarsWindowsAPIs.PostMessage(targetHandle, (uint)WM.KEYDOWN, vkLeft, keyDownLParam);
                MarsWindowsAPIs.PostMessage(targetHandle, (uint)WM.KEYUP, vkLeft, keyUpLParam);

                return true;
                //if (strTypes.IndexOf("DevExpress", StringComparison.OrdinalIgnoreCase) >= 0)
                //{
                //    MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.PressKey", $"{iMark}| Found DevExpress control, type:{matchedControls[0].GetType().FullName}");
                //    if (strobjType.ToLower().Equals("swftreeview", StringComparison.OrdinalIgnoreCase))
                //    {
                //        (int x, int y) centerPoint = default;
                //        MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.PressKey", $"{iMark}| Detected TreeList control, will use TreeList helper");
                //        bool isOk = DevExpressTreeListOpHelper.SearchAndClick(matchedControls[0], strParaMeter, dataAndAction,
                //            out matchedText, out matchedNode,
                //            ref strError, ref strAdv, ref strStack, ref centerPoint,
                //            true);
                //        if (isOk)
                //        {
                //            strDataReturn = $"{dataAndAction.MouseAction};{centerPoint.x}:{centerPoint.y}";
                //            return true;
                //        }
                //        else return false;
                //    }
                //    strError = $"unsupported typ|{strobjType}";
                //    strAdv = "Please ensure that only DevExpress.treelist is set";
                //    strStack = Environment.StackTrace;
                //    MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.PressKey", $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                //    return false;
                //}
                //else
                //{
                //    strError = "PressKey failed, unsupported control type: " + matchedControls[0].GetType().FullName;
                //    strAdv = "Please ensure that the control is DevExpress or Infragistics type";
                //    strStack = Environment.StackTrace;
                //    MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.PressKey", $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                //    return false;
                //}
            }
            finally
            {
                MarsLoggerSimple.logEnd("HostedFrameworkControlKeywordHelper.SearchAndClick", $"{iMark}|{iMark}|SearchAndClick leave");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="strParaMeter">如果是空，或者central，表示点击在中间；如果是-x:-y，任何有-的表示从右或者bottom开始计数，其他情况按照左上为原点模式</param>
        /// <param name="strData">如果parameter pos的信息，同时data有paramter的格式就按照上面的说明</param>
        /// <param name="strobjType"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strPegName"></param>
        /// <param name="strObjName"></param>
        /// <param name="objProperties"></param>
        /// <param name="objPegProperties"></param>
        /// <param name="errorCheckObj"></param>
        /// <param name="strError"></param>
        /// <param name="strDataReturn"></param>
        /// <param name="strStack"></param>
        /// <param name="strAdv"></param>
        /// <param name="strSnapshotForShouldBeFile"></param>
        /// <param name="isInnerCall"></param>
        /// <param name="waitingTime"></param>
        /// <returns></returns>
        internal static bool ClickButton(string strParaMeter, string strData, string strobjType, string strAttachInfo, string strPegName,
            string strObjName, Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj, ref string strError, ref string strDataReturn,
            ref string strStack, ref string strAdv, ref string strSnapshotForShouldBeFile, bool isInnerCall, int waitingTime)
        {
            int iMark = new Random().Next(100000);
            MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.ClickButton", $"{iMark}|ClickButton enter, strObjName:{strObjName}, strPegName:{strPegName}");
            try
            {
                if (!objPegProperties.ContainsKey(MarsConstants.cnst_pegProperty_hwnd_fromuiaa))
                {
                    strError = "HostedFrameworkControlKeywordHelper::ClickButton failed, missing peg property: " + MarsConstants.cnst_pegProperty_hwnd_fromuiaa;
                    strAdv = "Please ensure that target window has main window";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.ClickButton", $"{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }

                /// 算法：
                /// 1，获得hwnd，判断是否和HostedFrameworkControlGlobalVar中的Current_Root_Hwnd一致，如果不一致，则获得新的对象
                
                List<System.Windows.Forms.Control> matchedControls = new List<System.Windows.Forms.Control>();
                if (!FindObjects("ClickButton", objProperties, objPegProperties, matchedControls, ref strError, ref strAdv, ref strStack))
                {
                    MarsLoggerSimple.Error("ClickButton", $"{iMark}|{strError}|{strAdv}|{strStack}");
                    return false;
                }

                /// 3, 对找到的对象, 判断是devexpress还是标准Windows Forms控件，进行click
                /// 
                string strTypes = GetBaseTypesUntilSystemWindowsFormsControl(matchedControls[0].GetType());
                string matchedText = "";
                object matchedNode = null;
                
                // Check if it's DevExpress or standard Windows Forms Control
                bool isDevExpress = strTypes.IndexOf("DevExpress", StringComparison.OrdinalIgnoreCase) >= 0;
                bool isStandardControl = matchedControls[0] is System.Windows.Forms.Control;
                
                if (isDevExpress || isStandardControl)
                {
                    MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.ClickButton", $"{iMark}| Found {(isDevExpress ? "DevExpress" : "standard")} control, type:{matchedControls[0].GetType().FullName}");
                    // Use DevExpressButtonOpHelper for button click (works for both DevExpress and standard controls)
                    bool isOk = DevExpressButtonOpHelper.ClickButton(matchedControls[0], strParaMeter,
                        ref strError, ref strAdv, ref strStack);
                    if (isOk)
                    {
                        return true;
                    }
                    else return false;
                }
                else
                {
                    strError = "ClickButton failed, unsupported control type: " + matchedControls[0].GetType().FullName;
                    strAdv = "Please ensure that the control is DevExpress or standard Windows Forms Control";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.ClickButton", $"{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }
            }
            finally
            {
                MarsLoggerSimple.logEnd("HostedFrameworkControlKeywordHelper.ClickButton", $"{iMark}|SearchAndClick leave");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="strobjType"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strPegName"></param>
        /// <param name="strObjName"></param>
        /// <param name="objProperties"></param>
        /// <param name="objPegProperties"></param>
        /// <param name="errorCheckObj"></param>
        /// <param name="strError"></param>
        /// <param name="strDataReturn"></param>
        /// <param name="strStack"></param>
        /// <param name="strAdv"></param>
        /// <param name="strSnapshotForShouldBeFile"></param>
        /// <param name="isInnerCall"></param>
        /// <param name="waitingTime"></param>
        /// <returns></returns>
        internal static bool FillEdit(string strParaMeter, string strData, string strobjType, string strAttachInfo, string strPegName,
            string strObjName, Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj, ref string strError, ref string strDataReturn,
            ref string strStack, ref string strAdv, ref string strSnapshotForShouldBeFile, bool isInnerCall, int waitingTime)
        {
            int iMark = new Random().Next(100000);
            MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.FillEdit", $"{iMark}|FillEdit enter, strObjName:{strObjName}, strPegName:{strPegName}");
            try
            {
                if (!objPegProperties.ContainsKey(MarsConstants.cnst_pegProperty_hwnd_fromuiaa))
                {
                    strError = "HostedFrameworkControlKeywordHelper::FillEdit failed, missing peg property: " + MarsConstants.cnst_pegProperty_hwnd_fromuiaa;
                    strAdv = "Please ensure that target window has main window";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.FillEdit", $"{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }

                List<System.Windows.Forms.Control> matchedControls = new List<System.Windows.Forms.Control>();
                if (!FindObjects("FillEdit", objProperties, objPegProperties, matchedControls, ref strError, ref strAdv, ref strStack))
                {
                    MarsLoggerSimple.Error("FillEdit", $"{iMark}|{strError}|{strAdv}|{strStack}");
                    return false;
                }

                var targetControl = matchedControls[0];
                System.Drawing.Point centerPoint = System.Drawing.Point.Empty;
                // Ensure we calculate the click point on the UI thread.
                Action getCenterAction = () =>
                {
                    var rect = targetControl.RectangleToScreen(targetControl.ClientRectangle);
                    centerPoint = new System.Drawing.Point(
                        rect.Left + rect.Width / 2,
                        rect.Top + rect.Height / 2);
                    targetControl.Focus();
                };

                if (targetControl.InvokeRequired)
                {
                    targetControl.Invoke((System.Windows.Forms.MethodInvoker)(() => { getCenterAction(); }));
                }
                else
                {
                    getCenterAction();
                }

                if (centerPoint.IsEmpty)
                {
                    strError = "FillEdit failed, target control center point is empty.";
                    strAdv = "Please ensure the control is created and visible.";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.FillEdit", $"{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }

                // Click to focus, then clear existing text and input new data.
                MarsWindowsAPIsExtend.LeftMouseClick(centerPoint.X, centerPoint.Y);
                Thread.Sleep(100);
                System.Windows.Forms.SendKeys.SendWait("{HOME}");
                System.Windows.Forms.SendKeys.SendWait("{DEL 20}");
                Thread.Sleep(50);
                System.Windows.Forms.SendKeys.SendWait(strData);
                strDataReturn = strData;
                return true;
            }
            catch (Exception ex)
            {
                strError = $"FillEdit failed: {ex.Message}";
                strAdv = "Please ensure the target control is ready and focused";
                strStack = ex.ToString();
                MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.FillEdit", $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("HostedFrameworkControlKeywordHelper.FillEdit", $"{iMark}|FillEdit leave");
            }
        }

        internal static bool SelectDropDown(string strParaMeter, string strData, string strobjType, string strAttachInfo, string strPegName,
            string strObjName, Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj, ref string strError, ref string strDataReturn,
            ref string strStack, ref string strAdv, ref string strSnapshotForShouldBeFile, bool isInnerCall, int waitingTime)
        {
            int iMark = new Random().Next(100000);
            MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.SelectDropDown", $"{iMark}|SelectDropDown enter, strObjName:{strObjName}, strPegName:{strPegName}");
            try
            {
                if (!objPegProperties.ContainsKey(MarsConstants.cnst_pegProperty_hwnd_fromuiaa))
                {
                    strError = "HostedFrameworkControlKeywordHelper::SelectDropDown failed, missing peg property: " + MarsConstants.cnst_pegProperty_hwnd_fromuiaa;
                    strAdv = "Please ensure that target window has main window";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.SelectDropDown", $"{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }

                List<System.Windows.Forms.Control> matchedControls = new List<System.Windows.Forms.Control>();
                if (!FindObjects("SelectDropDown", objProperties, objPegProperties, matchedControls, ref strError, ref strAdv, ref strStack))
                {
                    MarsLoggerSimple.Error("SelectDropDown", $"{iMark}|{strError}|{strAdv}|{strStack}");
                    return false;
                }

                var targetControl = matchedControls[0];
                string typePath = GetBaseTypesUntilSystemWindowsFormsControl(targetControl.GetType());
                bool isDevExpress = typePath.IndexOf("DevExpress", StringComparison.OrdinalIgnoreCase) >= 0;
                bool isStandardControl = targetControl is System.Windows.Forms.Control;
                if (!isDevExpress && !isStandardControl)
                {
                    strError = $"SelectDropDown failed, unsupported control type: {targetControl.GetType().FullName}";
                    strAdv = "Please ensure that the control is DevExpress or standard Windows Forms Control";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.SelectDropDown", $"{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }

                // Step 1: click right border (x+width-10, y+10)
                var clickPoint = targetControl.PointToScreen(new System.Drawing.Point(
                    Math.Max(1, targetControl.Width - 10),
                    Math.Min(targetControl.Height - 1, 10)));
                MarsWindowsAPIsExtend.LeftMouseClick(clickPoint.X, clickPoint.Y);
                //Thread.Sleep(waitingTime > 0 ? waitingTime : 300);
                Thread.Sleep(1*1000);

                // Step 2 & 3: find dropdown list item by data (Regular test) and click its rectangle
                if (!TrySelectDropDownItemByReflector(targetControl, clickPoint, strData, ref strDataReturn, ref strError, ref strAdv, ref strStack))
                {
                    MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.SelectDropDown", $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }

                return true;
            }
            finally
            {
                MarsLoggerSimple.logEnd("HostedFrameworkControlKeywordHelper.SelectDropDown", $"{iMark}|SelectDropDown leave");
            }
        }

        private static bool TrySelectDropDownItemByReflector(System.Windows.Forms.Control targetControl, System.Drawing.Point clickPoint,
            string strData, ref string strDataReturn, ref string strError, ref string strAdv, ref string strStack)
        {
            int iMark = new Random().Next(100000);
            MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.TrySelectDropDownItemByReflector",
                $"{iMark}|enter|data:{strData}|click:{clickPoint.X},{clickPoint.Y}|ctrl:{targetControl?.GetType().FullName}");
            MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.TrySelectDropDownItemByReflector",
                $"{iMark}|targetControl class:{targetControl?.GetType().FullName}");
            int processId = 0;
            MarsWindowsAPIs.GetWindowThreadProcessId(targetControl.Handle, out processId);
            if (processId <= 0)
            {
                strError = "SelectDropDown failed, can't get target process id";
                strAdv = "Please ensure the target control handle is valid";
                strStack = Environment.StackTrace;
                MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.TrySelectDropDownItemByReflector",
                    $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                return false;
            }
            MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.TrySelectDropDownItemByReflector", $"{iMark}|pid:{processId}");

            var bottomCenter = targetControl.PointToScreen(new System.Drawing.Point(
                targetControl.Width / 2,
                Math.Max(0, targetControl.Height - 1)));
            var popupPoint = new System.Drawing.Point(bottomCenter.X, bottomCenter.Y + 16);
            var hdl = MarsWindowsAPIs.WindowFromPoint(new System.Drawing.Point(popupPoint.X, popupPoint.Y));
            MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.TrySelectDropDownItemByReflector", $"{iMark}|----hdl----|{hdl}");
            var c = System.Windows.Forms.Control.FromHandle(hdl);
            //if (c!=null)
            //    MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.TrySelectDropDownItemByReflector", $"{iMark}|----control----|{c.GetType().FullName}|{GetBaseTypesUntilSystemWindowsFormsControl(c.GetType())}");
            //else
            //    MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.TrySelectDropDownItemByReflector", $"{iMark}|----control----|NULL");

            if (c == null)
            {
                strError = "SelectDropDown failed, popup control is null";
                strAdv = "make sure that droplist has been popuped";
                strStack = Environment.StackTrace;
                MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.TrySelectDropDownItemByReflector",
                    $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                return false;
            }

            string typePath = GetBaseTypesUntilSystemWindowsFormsControl(c.GetType());
            if (typePath.IndexOf("DevExpress.XtraEditors.Popup.SimplePopupListBox", StringComparison.OrdinalIgnoreCase) < 0)
            {
                strError = "For devexpress combobox, only support DevExpress.XtraEditors.Popup.SimplePopupListBox and its derived types.";
                strAdv = "make sure that droplist has been popuped";
                strStack = Environment.StackTrace;
                MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.TrySelectDropDownItemByReflector",
                    $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                return false;
            }
            MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.TrySelectDropDownItemByReflector",
                $"{iMark}|popup control class:{c.GetType().FullName}\r\n{typePath}");

            bool isMatched = false;
            string matchedText = string.Empty;
            string allItems = string.Empty;
            System.Drawing.Rectangle matchedRect = System.Drawing.Rectangle.Empty;
            int clickX = 0;
            int clickY = 0;
            string localError = null;
            string localAdv = null;
            string localStack = null;

            Action findItemAction = () =>
            {
                int iMark2 = new Random().Next(100000);
                MarsLoggerSimple.logBegin("findItemAction", $"{iMark2}|{c.GetType().FullName}");
                var propertiesObj = c.GetType().GetProperty("Properties")?.GetValue(c);
                var itemsObj = propertiesObj?.GetType().GetProperty("Items")?.GetValue(propertiesObj);
                if (itemsObj == null)
                {
                    localError = "SelectDropDown failed, cannot get items from popup list";
                    localAdv = "make sure that droplist has been popuped";
                    localStack = Environment.StackTrace;
                    MarsLoggerSimple.Error("findItemAction", $"{iMark2}|{localError}|{localAdv}|\r\n{localStack}");
                    return;
                }
                var getItemTextCoreMethod = c.GetType().GetMethod("GetItemTextCore",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                    null, new[] { typeof(int) }, null);

                int count = 0;
                var countProp = itemsObj.GetType().GetProperty("Count");
                if (countProp != null)
                {
                    count = Convert.ToInt32(countProp.GetValue(itemsObj));
                }
                else if (itemsObj is System.Collections.ICollection collection)
                {
                    count = collection.Count;
                }
                MarsLoggerSimple.Info("findItemAction", $"{iMark2}|find {count} items");
                for (int i = 0; i < count; i++)
                {
                    object item = null;
                    if (itemsObj is System.Collections.IList list)
                    {
                        item = list[i];
                    }
                    else
                    {
                        var indexer = itemsObj.GetType().GetProperty("Item");
                        if (indexer != null)
                        {
                            item = indexer.GetValue(itemsObj, new object[] { i });
                        }
                    }
                    if (item == null) continue;

                    string text = null;
                    if (getItemTextCoreMethod != null)
                    {
                        text = getItemTextCoreMethod.Invoke(c, new object[] { i }) as string;
                    }
                    if (string.IsNullOrEmpty(text))
                    {
                        text = item.ToString();
                    }
                    MarsLoggerSimple.Info("findItemAction", $"{iMark2}|item[{i}] class:{item.GetType().FullName}|text:{text}");
                    allItems = string.IsNullOrEmpty(allItems) ? text : $"{allItems};{text}";

                    if (!MarsWindowsAPIsExtend.RegularTest(strData, text) &&
                        !text.Equals(strData, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    ScrollItemIntoView(c, i, item);
                    var rectMethod = c.GetType().GetMethod("GetItemRectangle", new[] { typeof(int) });
                    if (rectMethod != null)
                    {
                        matchedRect = (System.Drawing.Rectangle)rectMethod.Invoke(c, new object[] { i });
                    }

                    if (matchedRect.IsEmpty || matchedRect.Width <= 2 || matchedRect.Height <= 2)
                    {
                        localError = $"SelectDropDown failed, can not get rectangle for item [{text}]";
                        localAdv = "make sure that droplist has been popuped";
                        localStack = Environment.StackTrace;
                        return;
                    }

                    var screenRect = c.RectangleToScreen(matchedRect);
                    clickX = screenRect.X + screenRect.Width / 2;
                    clickY = screenRect.Y + screenRect.Height / 2;
                    isMatched = true;
                    matchedText = text;
                    return;
                }
            };

            if (c.InvokeRequired)
            {
                c.Invoke((System.Windows.Forms.MethodInvoker)(() => { findItemAction(); }));
            }
            else
            {
                findItemAction();
            }

            if (!string.IsNullOrEmpty(localError))
            {
                strError = localError;
                strAdv = localAdv;
                strStack = localStack;
                MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.TrySelectDropDownItemByReflector",
                    $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                return false;
            }

            if (!isMatched)
            {
                strError = $"No dropdown item matched [{strData}] in [{allItems}]";
                strAdv = "Please ensure the dropdown list contains the target item";
                strStack = Environment.StackTrace;
                MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.TrySelectDropDownItemByReflector",
                    $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                return false;
            }

            MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.TrySelectDropDownItemByReflector",
                $"{iMark}|matched item:{matchedText}|rect:{matchedRect}");
            MarsWindowsAPIsExtend.LeftMouseClick(clickX, clickY);
            strDataReturn = matchedText;
            return true;
        }

        private static void ScrollItemIntoView(System.Windows.Forms.Control listCtrl, int index, object item)
        {
            int iMark = new Random().Next(100000);
            if (listCtrl == null)
            {
                MarsLoggerSimple.Warning("HostedFrameworkControlKeywordHelper.ScrollItemIntoView",
                    $"{iMark}|listCtrl is null|index:{index}");
                return;
            }
            var ctrlType = listCtrl.GetType();
            MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.ScrollItemIntoView",
                $"{iMark}|enter|type:{ctrlType.FullName}|index:{index}|item:{item}");

            var makeVisible = ctrlType.GetMethods().FirstOrDefault(m =>
                m.Name.Equals("MakeVisible", StringComparison.OrdinalIgnoreCase) && m.GetParameters().Length == 1);
            if (makeVisible != null)
            {
                var paramType = makeVisible.GetParameters()[0].ParameterType;
                object param = null;
                if (paramType == typeof(int))
                {
                    param = index;
                }
                else if (item != null && paramType.IsInstanceOfType(item))
                {
                    param = item;
                }
                else if (item != null && paramType == typeof(object))
                {
                    param = item;
                }
                else if (paramType == typeof(string) && item != null)
                {
                    param = item.ToString();
                }

                if (param != null)
                {
                    MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.ScrollItemIntoView",
                        $"{iMark}|invoke MakeVisible|paramType:{param.GetType().FullName}");
                    makeVisible.Invoke(listCtrl, new object[] { param });
                    return;
                }
            }

            var topIndexProp = ctrlType.GetProperty("TopIndex");
            if (topIndexProp != null && topIndexProp.CanWrite)
            {
                MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.ScrollItemIntoView",
                    $"{iMark}|set TopIndex|value:{index}");
                topIndexProp.SetValue(listCtrl, index);
                return;
            }
            MarsLoggerSimple.Warning("HostedFrameworkControlKeywordHelper.ScrollItemIntoView",
                $"{iMark}|no scroll method|type:{ctrlType.FullName}");
        }

        private static bool TrySelectDropDownItemByWinForms(AutomationElement popupElement, string strData,
            ref string strDataReturn, ref string strError, ref string strAdv, ref string strStack, int iMark)
        {
            if (popupElement == null) return false;
            int hwnd = 0;
            try { hwnd = popupElement.Current.NativeWindowHandle; } catch { hwnd = 0; }
            if (hwnd == 0) return false;

            try
            {
                var popupCtrl = System.Windows.Forms.Control.FromHandle((IntPtr)hwnd);
                if (popupCtrl == null) return false;

                MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.TrySelectDropDownItemByUIA",
                    $"{iMark}|popup ctrl:{popupCtrl.GetType().FullName}");

                if (popupCtrl is System.Windows.Forms.ListBox listBox)
                {
                    string allItems = "";
                    for (int i = 0; i < listBox.Items.Count; i++)
                    {
                        var itm = listBox.Items[i];
                        if (itm == null) continue;
                        string text = itm.ToString();
                        allItems = string.IsNullOrEmpty(allItems) ? text : $"{allItems};{text}";
                        if (!MarsWindowsAPIsExtend.RegularTest(strData, text) &&
                            !text.Equals(strData, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var rect = listBox.GetItemRectangle(i);
                        if (rect.IsEmpty || rect.Width <= 2 || rect.Height <= 2)
                        {
                            listBox.SelectedIndex = i;
                        }
                        else
                        {
                            var screenRect = listBox.RectangleToScreen(rect);
                            MarsWindowsAPIsExtend.LeftMouseClick(screenRect.X + screenRect.Width / 2,
                                screenRect.Y + screenRect.Height / 2);
                        }
                        strDataReturn = text;
                        return true;
                    }

                    strError = $"No dropdown item matched [{strData}] in [{allItems}]";
                    strAdv = "Please ensure the dropdown list contains the target item";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.TrySelectDropDownItemByUIA",
                        $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("HostedFrameworkControlKeywordHelper.TrySelectDropDownItemByUIA",
                    $"{iMark}|TrySelectDropDownItemByWinForms failed|{ex.Message}");
            }

            return false;
        }

        private static IEnumerable<AutomationElement> EnumerateElementsByDepth(AutomationElement root, int maxDepth)
        {
            if (root == null || maxDepth < 0) yield break;
            var queue = new Queue<(AutomationElement element, int depth)>();
            queue.Enqueue((root, 0));
            while (queue.Count > 0)
            {
                var (element, depth) = queue.Dequeue();
                if (element == null || depth >= maxDepth) continue;
                AutomationElement child = null;
                try
                {
                    child = TreeWalker.ControlViewWalker.GetFirstChild(element);
                }
                catch
                {
                    child = null;
                }
                while (child != null)
                {
                    yield return child;
                    queue.Enqueue((child, depth + 1));
                    try
                    {
                        child = TreeWalker.ControlViewWalker.GetNextSibling(child);
                    }
                    catch
                    {
                        break;
                    }
                }
            }
        }



        internal static bool SearchAndClick(string strParaMeter, string strData, string strobjType, string strAttachInfo, string strPegName,
            string strObjName, Dictionary<string, string> objProperties, Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj, ref string strError, ref string strDataReturn,
            ref string strStack, ref string strAdv, ref string strSnapshotForShouldBeFile, bool isInnerCall, int waitingTime)
        {
            int iMark = new Random().Next(100000);
            MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.SearchAndClick", $"{iMark}|SearchAndClick enter, strObjName:{strObjName}, strPegName:{strPegName}");
            try
            {
                if (!objPegProperties.ContainsKey(MarsConstants.cnst_pegProperty_hwnd_fromuiaa))
                {
                    strError = "HostedFrameworkControlKeywordHelper::SearchAndClick failed, missing peg property: " + MarsConstants.cnst_pegProperty_hwnd_fromuiaa;
                    strAdv = "Please ensure that target window has main window";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.SearchAndClick", $"{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }

                /// 算法：
                /// 1，获得hwnd，判断是否和HostedFrameworkControlGlobalVar中的Current_Root_Hwnd一致，如果不一致，则获得新的对象
                /// 

                var dataAndAction = SearchAndClickData.GetInstance(strData);
                if (dataAndAction == null)
                {
                    strError = $"Data format is wrong|{strData}";
                    strAdv = "please Ensure that data format is MouseAction;[data1:data2]";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.SearchAndClick", $"{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }

                List<System.Windows.Forms.Control> matchedControls = new List<System.Windows.Forms.Control>();
                if (!FindObjects("PressKey", objProperties, objPegProperties, matchedControls, ref strError, ref strAdv, ref strStack))
                {
                    MarsLoggerSimple.Error("PressKey", $"{iMark}|{strError}|{strAdv}|{strStack}");
                    return false;
                }

                /// 3, 对找到的对象, 判断是devexpress还是Infragistics，进行search
                /// 
                string strTypes = GetBaseTypesUntilSystemWindowsFormsControl(matchedControls[0].GetType());
                string matchedText = "";
                object matchedNode = null;
                if (strTypes.IndexOf("DevExpress", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.SearchAndClick", $"{iMark}| Found DevExpress control, type:{matchedControls[0].GetType().FullName}");
                    if (strobjType.ToLower().Equals("swftreeview", StringComparison.OrdinalIgnoreCase))
                    {
                        (int x, int y, int w, int h) centerPoint = default;
                        MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.SearchAndClick", $"{iMark}| Detected TreeList control, will use TreeList helper");
                        bool isOk = DevExpressTreeListOpHelper.SearchAndClick(matchedControls[0], strParaMeter, dataAndAction,
                            out matchedText, out matchedNode,                             
                            ref strError, ref strAdv, ref strStack, ref centerPoint,
                            true);
                        if (isOk)
                        {
                            strDataReturn = $"{dataAndAction.MouseAction};{centerPoint.x}:{centerPoint.y}:{centerPoint.w}:{centerPoint.h}";
                            return true;
                        }
                        else return false;
                    }
                    strError = $"unsupported typ|{strobjType}";
                    strAdv = "Please ensure that only DevExpress.treelist is set";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.SearchAndClick", $"{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }

                //else if (strTypes.IndexOf("Infragistics", StringComparison.OrdinalIgnoreCase) >= 0)
                //{
                //    MarsLoggerSimple.Info("HostedFrameworkControlKeywordHelper.SearchAndClick", $"{iMark}| Found Infragistics control, type:{matchedControls[0].GetType().FullName}");
                //    return HostedFrameworkControlInfragisticsHelper.SearchAndClickOnInfragisticsControl(strParaMeter, strData, strobjType, strAttachInfo, strPegName,
                //        strObjName, objProperties, objPegProperties,
                //        errorCheckObj, ref strError, ref strDataReturn,
                //        ref strStack, ref strAdv, ref strSnapshotForShouldBeFile, isInnerCall, waitingTime,
                //        matchedControls[0]);
                //}
                else
                {
                    strError = "SearchAndClick failed, unsupported control type: " + matchedControls[0].GetType().FullName;
                    strAdv = "Please ensure that the control is DevExpress or Infragistics type";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error("HostedFrameworkControlKeywordHelper.SearchAndClick", $"{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }
            }
            finally
            {
                MarsLoggerSimple.logEnd("HostedFrameworkControlKeywordHelper.SearchAndClick", $"{iMark}|SearchAndClick leave");
            }
        }

        private static string GetBaseTypesUntilSystemWindowsFormsControl(Type t)
        {
            StringBuilder sb = new StringBuilder();
            Type currentType = t;
            while (currentType != null)
            {
                sb.AppendLine(currentType.FullName);
                if (currentType.FullName.StartsWith("System"))
                {
                    break;
                }
                currentType = currentType.BaseType;
            }
            return sb.ToString();
        }
    }
}
