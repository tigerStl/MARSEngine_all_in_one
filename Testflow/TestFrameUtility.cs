
using Mars.message.Securities;
using MarsTestFrame.systemUtil;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using TestFlowClient.ClientAddins.AddinsMgr;
#if ExcelSupport
using TestFlowClient.Mars.Office.Support;
#endif
using TestFlowClient.Mars.TigerConfig;
using TestFlowClient.referenceSource;

namespace TestFlowClient
{
#if _TMP_DEBUGER_
    [Serializable]
    [ComVisible(true)]
    public class TestFrameUtility : MarshalByRefObject
#else
    public class TestFrameUtility
#endif
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hwnd, ref RECT lpRect);
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int cx, int cy, int wflags);
        const short SWP_NOMOVE = 0X2;
        const short SWP_NOSIZE = 1;
        const short SWP_NOZORDER = 0X4;
        const int SWP_SHOWWINDOW = 0x0040;
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool MoveWindow(IntPtr hwnd, int x, int y, int nw, int nh, bool isRepaint);


        private static MLogger Logger = null;// MLogger.GetLogger(typeof(TestFrameUtility));

        private static bool isBreakpointNow = false;
        /** test data **/
        public static int ID = 0;

        private static Thread ShellThread = null;

        public static void ChangeBreakPointNow(bool isValue)
        {
            isBreakpointNow = isValue;
        }


        public TestFrameUtility()
        {
#if _NO_C_DRIVER_WRITE
            string strFile = Environment.GetEnvironmentVariable("_MARS_SHELL_DIR_");
            try
            {
                if (!Directory.Exists(Path.Combine(strFile, "MARS")))
                {
                    Directory.CreateDirectory(Path.Combine(strFile, "MARS"));
                }
                strFile = Path.Combine(strFile, "MARS");
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Exception when create folders under Application Director:[{0}]\r\n{1}", strFile, e.Message));
            }
#else
            string strFile = typeof(TestFlowClientMainEntry).Assembly.Location;
#endif
            strFile = Path.GetDirectoryName(strFile);
            strFile = Path.Combine(strFile, ".\\log\\MarsTestClient.log");
            MLogger.LogFileCofigName = strFile;
            Logger = MLogger.GetLogger(typeof(TestFlowClientMainEntry));

            Logger.logBegin("NewInstance_Of_TestFrameUtility");
            isBreakpointNow = false;
#if _TMP_DEBUGER_
            Console.WriteLine("Current Domain Name:{0}\r\nCurrent Thread Code:{1}", AppDomain.CurrentDomain.FriendlyName, Thread.CurrentThread.GetHashCode().ToString());
#endif
            ID++;
            Logger.Info("TestFrameUtility", string.Format("ID value is [{0}]", ID));

            //if (ShellThread == null)
            //{
            //    ShellThread = new Thread(new ThreadStart(delegate() { }));
            //}
        }

        public bool StartProcess(object objPath, ref object strError)
        {
            string strPath = objPath == null ? "" : objPath.ToString();
            if (!System.IO.File.Exists(strPath))
            {
                strError = string.Format("no such file exists:[{0}]", strPath);
                return false;
            }

            try
            {
                string strDir = System.IO.Path.GetDirectoryName(strPath);
                Process p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = strPath,
                        WorkingDirectory = strDir,
                        Verb = "runas",
                        UseShellExecute = true,
                        LoadUserProfile = true

                    }
                };
                p.Start();
                return true;
            }
            catch (Exception e)
            {
                strError = string.Format("exception when run :[{0}]\r\n{1}", strPath, e.Message);
                return false;
            }
        }

        public bool checkKeywordIsskippable(object strKeyword)
        {
            if (strKeyword == null) return false;

            string[] arrSkippableKeywords = AppConfigReader.GetSkippableKeyWord();
            foreach (string strItm in arrSkippableKeywords)
            {
                if (strItm == null) continue;
                if (string.Compare(strKeyword.ToString(), strItm, true) == 0) return true;
            }
            return false;
        }

        public bool CheckErrorForFillTable()
        {
            //FillTableAutoCheckError
            //string strCheck
            return false;
        }

        public object ConvertYYYMMDD2Number(object dateInYYYYMMDD, ref object isOk, ref object strError)
        {
            Logger.logBegin("ConvertYYYMMDD2Number");
            Logger.Info("ConvertYYYMMDD2Number", string.Format("date:[{0}]", dateInYYYYMMDD));
            if (dateInYYYYMMDD == null)
            {
                isOk = false;
                strError = ("No date with yyyymmdd format is set, null.");
                return "";
            }
            string strDate = dateInYYYYMMDD.ToString();
            if (string.IsNullOrEmpty(strDate))
            {
                isOk = false;
                strError = ("No date with yyyymmdd format is set, IsNullOrEmpty.");
                return "";
            }
            DateTime d;
            try
            {
                bool bOk = DateTime.TryParseExact(strDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out d);
                if (!bOk)
                {
                    Logger.Info("ConvertYYYMMDD2Number", (string)(string.Format("[{0}] is not a valiad datetime with format yyyymmdd.", dateInYYYYMMDD)));
                    isOk = false;
                    return dateInYYYYMMDD;
                }
                int iFrom18991230 = (int)d.ToOADate();
                isOk = true;
                return iFrom18991230.ToString();
            }
            catch (Exception e)
            {
                Logger.Error("ConvertYYYMMDD2Number", (string)(strError = string.Format("Excetpion:[{0}]", e.Message, e)));
                return "";
            }

        }


        public void WaitForBreakPoint()
        {
            Logger.logBegin("WaitForBreakPoint");

            Thread thrdWait = new Thread(new ThreadStart(delegate ()
            {
                bool isInThread = false;
                while (isBreakpointNow)
                {
                    Logger.Info("WaitForBreak    Point", "begin sleep 5 seconds....");
                    isInThread = (!isInThread) ? false : true;
                    Thread.Sleep(5000);
                }
                if (isInThread)
                    Logger.Info("--waiting Thread--", "waiting Thread end...");
            }));
            //thrdWait.Start();

            Logger.Info("--waiting Thread--", "begin Join......");
            while (isBreakpointNow)
            {
                Thread.Sleep(1000);
            }
            //thrdWait.Join();
            Logger.Info("...wait.END...", "-----------------");
        }

        public bool isWaitfor()
        {
            return isBreakpointNow;
        }

        public bool IsJavaObjectType(string strObjType)
        {
            return TigerMarsUtil.RegularTest("Java", strObjType);
        }

        public bool RegularTest(string objParttern, string strData)
        {
            return TigerMarsUtil.RegularTest(objParttern, strData);
        }

        public string GetBinDirectory()
        {
#if _NO_C_DRIVER_WRITE
            string strFullPath = Environment.GetEnvironmentVariable("_MARS_SHELL_DIR_"); ;
            try
            {
                if (!Directory.Exists(Path.Combine(strFullPath, "MARS")))
                {
                    Directory.CreateDirectory(Path.Combine(strFullPath, "MARS"));
                }
                strFullPath = Path.Combine(strFullPath, "MARS");
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("Exception when create folders under Application Director:[{0}]\r\n{1}", strFullPath, e.Message));
                //return ;
                strFullPath = Assembly.GetExecutingAssembly().Location;
            }
            return strFullPath;
#else
            string strFullPath = Assembly.GetExecutingAssembly().Location;
            string strPrntDir = Directory.GetParent(Directory.GetParent(strFullPath).FullName).FullName;
            return strPrntDir;
#endif


        }

        public string CombinPath(string strTargetPath, bool isCreateWhenNotExits)
        {

            string strRoot = GetBinDirectory();
            string strTarget = Path.Combine(strRoot, strTargetPath);
            if (isCreateWhenNotExits)
            {
                if (!Directory.Exists(strTarget))
                    Directory.CreateDirectory(strTarget);
            }
            return strTarget;
        }

        public bool SendkeysDirectly(string strKeysToSend)
        {
            System.Windows.Forms.SendKeys.SendWait(strKeysToSend);
            return true;
        }

        private const string cnst_windpos_cmmd_center = "CENTER";
        private const string cnst_windpos_cmmd_bypos = "BYPOS:";
        public bool setWindowPosition(string strhwnd, string strCmmd, int iW, int iH, ref string strError, out int xLeft, out int yTop)
        {
            Logger.Info("setWindowPos", string.Format("hwnd [{0}] cmmd [{1}]", strhwnd, strCmmd));
            int hwnd;
            int.TryParse(strhwnd, out hwnd);
            if (string.IsNullOrEmpty(strCmmd))
                strCmmd = cnst_windpos_cmmd_center;

            if (strCmmd.ToUpper().StartsWith(cnst_windpos_cmmd_bypos))
            {
                Logger.Info("setWindowPos", "cnst_windpos_cmmd_bypos branch");
                string strPosInfo = strCmmd.Substring(strCmmd.Length);
                string[] arrStrPosInfo = strPosInfo.Split(':');
                if ((arrStrPosInfo == null) || (arrStrPosInfo.Length != 2))
                {
                    strError = "When command is ByPos, then another two integers are required with format num1:num2";
                    xLeft = -1;
                    yTop = -1;
                    return false;
                }
                int xLeftTmp, yTopTmp;
                if ((!(int.TryParse(arrStrPosInfo[0], out xLeftTmp))) || (!(int.TryParse(arrStrPosInfo[1], out yTopTmp))))
                {
                    strError = "When command is ByPos, then another two integers are required with format num1:num2";
                    xLeft = -1;
                    yTop = -1;
                    return false;
                }
                xLeft = xLeftTmp;
                yTop = yTopTmp;
                SetWindowPos(new IntPtr(hwnd), new IntPtr(0), xLeft, yTop, iW, iH, SWP_SHOWWINDOW);
                return true;
            }

            //Screen objTargetScreen = Screen.FromHandle((IntPtr)hwnd);
            //if (objTargetScreen == null)
            Screen objTargetScreen = Screen.PrimaryScreen;
            xLeft = (objTargetScreen.Bounds.Width - iW) / 2;
            yTop = (objTargetScreen.Bounds.Height - iH) / 2;
            IntPtr iRslt = SetWindowPos(new IntPtr(hwnd), new IntPtr(0), xLeft, yTop, iW, iH, SWP_SHOWWINDOW);
            Logger.Info("setWindowPosition", string.Format("hwnd:[{0}], Left, top:({1},{2}), result:{3}", hwnd, xLeft, yTop, iRslt));
            //bool isMoved = MoveWindow(new IntPtr(hwnd),())
            return true;
        }

        public bool CloseWindowByMessageAndHwnd(int hwnd, ref string strError)
        {
            Logger.Info("CloseWindowByMessageAndHwnd", string.Format("Handle:[{0}]", hwnd));

            try
            {
                //TigerMarsAPIs.SendMessage(new IntPtr(hwnd), TigerMarsAPIs.WM_CLOSE, 0, 0);
                TigerMarsAPIs.SendMessage(new IntPtr(hwnd), TigerMarsAPIs.WM_SYSCOMMAND, TigerMarsAPIs.SC_CLOSE, 0);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("CloseWindowByMessageAndHwnd", strError = string.Format("Exception:[{0}]\r\nTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        /// <summary>
        /// 通过消息实现对某个窗口的数据type
        /// </summary>
        /// <param name="hwnd">窗口的handle</param>
        /// <param name="strToSend">Data，需要输入到目标对象的</param>
        /// <param name="isRequireDel">是否需要先删除</param>
        /// <returns></returns>
        public static bool SendKeysByMessage(object ohwnd, object ostrToSend, object oisRequireDel)
        {
            try
            {

                int hwnd = int.Parse(ohwnd.ToString());
                string strToSend = ostrToSend.ToString();
                bool isRequireDel = bool.Parse(oisRequireDel.ToString());
                //KeysConverter k = new KeysConverter();
                //Keys ke;
                IntPtr pHwnd = new IntPtr(hwnd);
                //TigerMarsAPIs.SendMessage(pHwnd, (int)WMessages.WM_SETFOCUS, 0, 0);
                //if (isRequireDel)
                //{
                //    for(int i=0;i<30;i++)
                //    {
                //        sendKey(pHwnd, Keys.Delete, false);
                //    }
                //    for (int i = 0; i < 30; i++)
                //    {
                //        sendKey(pHwnd, Keys.Back, false);
                //    }
                //}

                //foreach (char c in strToSend)
                //{
                //    TigerMarsAPIs.PostMessage(pHwnd, (int)WMessages.WM_CHAR, (int)c, 0);
                //}
                //IntPtr ptxt = Marshal.StringToCoTaskMemAnsi(strToSend);

                TigerMarsAPIs.SendMessage(pHwnd, (int)WMessages.WM_SETTEXT, 0, new StringBuilder(strToSend));
                //Marshal.FreeCoTaskMem(ptxt);
                //sendKey(pHwnd, Keys.Tab, false);
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(string.Format("{0}\r\n{1}", e.Message, e.StackTrace));
                return false;
            }
        }
        public static void sendKey(IntPtr hwnd, Keys keyCode, bool extended)
        {
            uint scanCode = TigerMarsAPIs.MapVirtualKey((uint)keyCode, 0);
            uint lParam;

            //KEY DOWN
            lParam = (0x00000001 | (scanCode << 16));
            if (extended)
            {
                lParam |= 0x01000000;
            }
            TigerMarsAPIs.PostMessage(hwnd, (int)WMessages.WM_KEYDOWN, (int)keyCode, (int)lParam);

            //KEY UP
            lParam |= 0xC0000000;  // set previous key and transition states (bits 30 and 31)
            TigerMarsAPIs.PostMessage(hwnd, (int)WMessages.WM_KEYUP, (int)keyCode, (int)lParam);
        }


        public bool CloseWindowByPosition(string strCmd, int x, int y, int w, int h, ref string strError)
        {
            Logger.Info("CloseWindowByMessageAndHwnd", string.Format("cmd:[{0}] x:[{1}] y:[{2}], w:{3}]", strCmd, x, y, w));
            try
            {
                int iShiftX, iShiftY;
                if (!TigerMarsUtil.RegularTest("^byPos:", strCmd))
                {
                    Logger.Error("CloseWindowByPosition", strError = string.Format("None support format:[{0}], it should be start with byPos:x:y", strCmd));
                    return false;
                }
                string[] arrStr = strCmd.Split(':');
                if (arrStr.Length != 3)
                {
                    Logger.Error("CloseWindowByPosition", strError = string.Format("None support format:[{0}], Lenght desn't match. it should be start with byPos:x:y", strCmd));
                    return false;
                }
                if (int.TryParse(arrStr[1], out iShiftX) && int.TryParse(arrStr[2], out iShiftY))
                {
                    System.Windows.Forms.Cursor.Position = new Point(x + w + iShiftX, y + iShiftY);
                    TigerMarsAPIs.LeftMouseClick(x + w + iShiftX, y + iShiftY);
                    return true;
                }
                else
                {
                    Logger.Error("CloseWindowByPosition", strError = string.Format("None support format:[{0}], x/y should be number. it should be start with byPos:x:y", strCmd));
                    return false;
                }
            }
            catch (Exception e)
            {
                Logger.Error("CloseWindowByPosition", strError = string.Format("Exception:[{0}]\r\nTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }


        #region Selector
        private const string CNST_DEFAULT_KEYWORD = "Default";
        private const string CNST_KEYWORD_NAME_CHECKERROR = "CheckError";
        private const string CNST_SUB_KEYWORD_NAME_FILLEDIT_COMBO = "FillEdit_Combo";
        private const string CNST_NUMBERICEDITOR = "NUMBERIC_EDITOR";
        private const string CNST_SELECT_TAB = "SelectTab";
        private const string CNST_CLICK_BUTTON = "ClickButton";
        private const string CNST_SELECTLISTITEM = "SelectListItem";
        private const string CNST_SELECT_COMBOBOX = "SelectDropDown";
        private static Dictionary<string, List<KeywordsFunc>> gdicKeyword = new Dictionary<string, List<KeywordsFunc>>() {
            {"SelectListItem", new List<KeywordsFunc>(){
                                    new KeywordsFunc("^DevExpress.XtraTreeList.Tree.*", "SelectListItem_DevExpr"),
                                    new KeywordsFunc("^Sophis.Util.GUI.CustomTree.*", "SelectListItem_DevExpr"),
                                    new KeywordsFunc("^Summit.Framework.View.TreeCont.*", "SelectListItem_InfraExpr") ,
                                    new KeywordsFunc("^Infragistics.Win.UltraWinTree.UltraTree","SelectListItem_InfraExpr"),
                                    new KeywordsFunc("^Misys.OpicsPlus.Framework.PresentationLayer.Controls.InternalList.*", "SelectListItem_OpicsAddins"),

                                    new KeywordsFunc(CNST_DEFAULT_KEYWORD, "u_SelectBranch")
                                }
            },
            {"SelectMenuItem", new List<KeywordsFunc>(){
                                    new KeywordsFunc("^DevExpress.XtraBars.Controls.PopupMenu", "SelectMenuItem_DevExpr"),
                                    new KeywordsFunc(CNST_DEFAULT_KEYWORD, "u_clickMenuWithIdentifier")}
            },
            {"CaptureValue", new List<KeywordsFunc>(){
                                    new KeywordsFunc("^DevExpress.XtraTreeList.Tree.*","CaptureValue_DevExpr"),
                                    new KeywordsFunc("^Sophis.Util.GUI.CustomTree.*", "CaptureValue_DevExpr"),
                                    //new KeywordsFunc("^Misys.OpicsPlus.Framework.PresentationLayer.Controls.InternalList.*", "SelectListItem_OpicsAddins"),
                                    new KeywordsFunc(CNST_DEFAULT_KEYWORD, "_USINGDEFAULT_SCRIPT_")}
            },
            {
                CNST_SELECT_COMBOBOX,new List<KeywordsFunc>(){
                                    new KeywordsFunc("^Infragistics.Win.UltraWinGrid.UltraCombo", "SelectDropDown_Infra"),
                                    new KeywordsFunc(CNST_DEFAULT_KEYWORD, "_USINGDEFAULT_SCRIPT_")}
            },
            {CNST_KEYWORD_NAME_CHECKERROR, new List<KeywordsFunc>() {
                                    new KeywordsFunc("Opics.*", "CheckError_Opics"),
                                    new KeywordsFunc(CNST_DEFAULT_KEYWORD, "_USINGDEFAULT_SCRIPT_")
            }
            },
            {CNST_SUB_KEYWORD_NAME_FILLEDIT_COMBO, new List<KeywordsFunc>() {
                                    new KeywordsFunc("^Infragistics.Win.UltraWinGrid.UltraComb", "FillEditCombo"),
                                    new KeywordsFunc(CNST_DEFAULT_KEYWORD, "_USINGDEFAULT_SCRIPT_")
            }
            },
            {CNST_NUMBERICEDITOR, new List<KeywordsFunc>() {
                                    new KeywordsFunc("Infragistics.Win.UltraWinEditors.UltraNumeric.*", "FillEditNumericEditor"),
                                    new KeywordsFunc("TPG.Framework.Numeric.*", "FillEditNumericEditor"),
                                    new KeywordsFunc(CNST_DEFAULT_KEYWORD, "_USINGDEFAULT_SCRIPT_")
            }
            },
            {CNST_SELECT_TAB, new List<KeywordsFunc>() {
                                    new KeywordsFunc("Infragistics.Win.UltraWinToolbars.UltraToolbarsDockAr.*", "SelectTab_Ribbon_Infragistics"),
                                    new KeywordsFunc("Infragistics.Win.UltraWinTabbedMdi.MdiTabGroupControl.*", "SelectTab_OpicsPlus"),
                                    new KeywordsFunc("Infragistics.Win.UltraWinTabControl.UltraTabControl.*", "SelectTab_OpicsPlus"),
                                    new KeywordsFunc("MicWpfObject", "SelectTab_WPF"),
                                    new KeywordsFunc(CNST_DEFAULT_KEYWORD, "_USINGDEFAULT_SCRIPT_")
            }
            },
            {CNST_CLICK_BUTTON, new List<KeywordsFunc>() {
                                    new KeywordsFunc("Infragistics.Win.UltraWinToolbars.UltraToolbarsDockAr.*", "ClickButton_Ribbon_Infragistics"),
                                    new KeywordsFunc("Misys.OpicsPlus.Framework.PresentationLayer.Controls.Grids.BlotterGr.*", "ClickButton_Infra_Grid_Header"),
                                    new KeywordsFunc(CNST_DEFAULT_KEYWORD, "_USINGDEFAULT_SCRIPT_")
            }
            }

        };

        public void MaxWindow(int iHandle)
        {
            const string cnst_monitor_exename = "TestFrameMonitor";
            Logger.Info("MaxWindow", string.Format("begin,get hwnd:[{0}]", iHandle));
            Process[] arrp = Process.GetProcessesByName(cnst_monitor_exename);

            if (arrp == null || arrp.Length == 0)
            {
                Logger.Error("MaxWindow", "Can't finde TestFrameMonitor....");

            }
            RECT rect = new RECT();
            int iDefaultWidth = 348;
            if (!GetWindowRect(arrp[0].MainWindowHandle, ref rect))
            {
                rect.right = iDefaultWidth;
                try
                {
                    rect.bottom = Screen.FromHandle(arrp[0].MainWindowHandle).Bounds.Height;
                }
                catch (Exception e)
                {
                    Logger.Error("MaxWindow", string.Format("Exception:[{0}],default height will be used:900", e.Message), e);
                    rect.bottom = 900;
                }
            }
            Logger.Info("MaxWindow", string.Format("Rect:[{0}]", rect));

            Rectangle r = Screen.PrimaryScreen.Bounds;
            RECT rectNew = new RECT();
            rectNew.left = rect.right;
            rectNew.right = r.Right - rectNew.left;
            rectNew.top = rect.top;
            rectNew.bottom = rect.bottom;
            Logger.Info("MaxWindow", string.Format("new Rect:[{0}]", rectNew));
            IntPtr hwndNew = new IntPtr(iHandle);
            SetWindowPos(hwndNew, new IntPtr(0), rectNew.left, rectNew.top, rectNew.right, rectNew.bottom, SWP_SHOWWINDOW);
        }

        public string GetSelectorByName(object objTypeName, object objKeyWord)
        {
            Logger.logBegin("GetSelectorByName");
            string strTypeName = objTypeName == null ? null : objTypeName.ToString();
            string strKeyWord = objKeyWord == null ? null : objKeyWord.ToString();
            Logger.Info("GetSelectorByName", string.Format("Parameters : TypeName-[{0}], KeyWord-[{1}]", strTypeName, strKeyWord));
            if ((strTypeName == null) || (strKeyWord == null))
            {
                Logger.Error("GetSelectorByName", "TypeName or KeyWord is null, can't find regiested function.");
                return null;
            }
            foreach (string strKey in gdicKeyword.Keys)
            {
                Logger.Info("GetSelectorByName", string.Format("Key:[{0}],compare to :[{1}]", strKey, strKeyWord));
                if (TigerMarsUtil.RegularTest(strKey, strKeyWord))
                {
                    List<KeywordsFunc> lstKeyWordFunc = gdicKeyword[strKey];
                    Logger.Info("GetSelectorByName", string.Format("Key:[{0}] Passed,lstKeyWordFunc-Length:[{1}]", strKey, lstKeyWordFunc.Count));
                    string strDefaultFunc = null;
                    try
                    {
                        for (int i = 0; i < lstKeyWordFunc.Count; i++)
                        //foreach (KeywordsFunc objRegKeyFunc in gdicKeyword[strKey])
                        {
                            KeywordsFunc objRegKeyFunc = lstKeyWordFunc[i];
                            Logger.Info("GetSelectorByName", string.Format("Key:[{0}], TargetName:[{1}]", strKey, objRegKeyFunc.TargetTypeName));
                            if (objRegKeyFunc.TargetTypeName.CompareTo(CNST_DEFAULT_KEYWORD) == 0)
                            {
                                strDefaultFunc = objRegKeyFunc.FunctionName;
                            }
                            if (objRegKeyFunc.IsAppliedKeyWord(strTypeName))
                            {
                                Logger.Info("GetSelectorByName", string.Format("find regiested function [{0}] for keyword:[{1}]", objRegKeyFunc.FunctionName, strKeyWord));
                                return objRegKeyFunc.FunctionName;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error("ERROR", string.Format("can't find :[{0}] Exception:[{1}]", strTypeName, e.Message), e);
                    }
                    Logger.Info("ERROR", string.Format("can't find :[{0}] ", strTypeName));
                    /** return the default value to client  **/
                    if (strDefaultFunc != null) return strDefaultFunc;
                }
            }

            Logger.logEnd("GetSelectorByName");

            return "";
        }



        public bool getCheckErrorMethodBySelector(string strAppInfo, ref string strFuncName, ref string strError)
        {
            Logger.Info("getCheckErrorMethodByName", string.Format("strAppInfo :[{0}]", strAppInfo));
            string[] arrAppInfo = strAppInfo.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
            if (arrAppInfo.Length < 2)
            {
                Logger.Error("getCheckErrorMethodByName", strError = string.Format("RC format for CheckError keyword shoulde like :applicationShortName:Regular Express to Check.But current Data is :[{0}]", strAppInfo));
                return false;
            }
            strFuncName = GetSelectorByName(arrAppInfo[0], CNST_KEYWORD_NAME_CHECKERROR);
            if (string.IsNullOrEmpty(strFuncName))
            {
                Logger.Error("getCheckErrorMethodBySelector", strError = string.Format("Can't find Supported function for keyword:[{0}], for Application:[{1}]", CNST_KEYWORD_NAME_CHECKERROR, arrAppInfo[0]));
                return false;
            }
            return true;
        }
        #endregion //Selector

        public object ExecuteCommand(string strIdentifier, string strDataToRun, string strRC, string strDataRC, ref string strError)
        {
            return TigerMarsUtil.ExecuteCommand(strIdentifier, strDataToRun, strRC, strDataRC, ref strError);
        }

        /// <summary>
        /// 'keyword format
        ///   ' keyword : CopyExcelRangeToClipboard
        ///' Object  : null
        ///' RC      : null
        ///' Data    : [filePath];[RangeInfo]  RangeInfo sample: a2:b100
        /// </summary>
        /// <param name="strIdentifier"></param>
        /// <param name="strDataRC"></param>
        /// <param name="strRC"></param>
        /// <param name="strData"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        public bool CopyExcelRangeToClipboard(string strIdentifier, string strData, string strRC, string strDataRC, ref string strError)
        {
            Logger.Info("CopyExcelRangeToClipboard", string.Format("Indentifier:[{0}] Data:[{1}] RC:[{2}] DataRC:[{3}]", strIdentifier, strData, strRC, strDataRC));
#if ExcelSupport
            // check format of the data 
            string[] arrPthInfo = strData.Split(new string[] { "];" }, StringSplitOptions.RemoveEmptyEntries);
            if (arrPthInfo.Length != 2)
            {
                Logger.Error("CopyExcelRangeToClipboard", strError = string.Format("Data format should be :[filePath];[RangeInfo],but the data is :[{0}]", strData));
                return false;
            }
            string strPath = arrPthInfo[0];
            strPath = strPath.Replace("[", "");
            string strRange = arrPthInfo[1];
            strRange = strRange.Replace("]", "");
            strRange = strRange.Replace("[", "");
            return MarsExcel.CopyExcelRange2Clipboard(strPath, strRange, ref strError);
#else
            Logger.Error("CopyExcelRangeToClipboard", strError="This keyword is used only for Excel support.");
            return false ;
#endif
        }

        internal const string cnst_increase_last2 = "increase_last2";
        internal const string cnst_increase_last2_re = "increas_last2_regularExp";
        //isOk = gObjFrameWorkUtility.SearchAndReplaceGuiTxt(strTextSrc, RC, data, strNewTxt,strError)
        public object SearchAndReplaceGuiTxt(object objTextSrc, object objRC, object objDataToFind, ref object objNewTxt, ref object objError)
        {
            string strTextSrc, strRC, strDataToFind;
            Logger.Info("SearchAndReplaceGuiTxt", string.Format("Try to find:[{0}] from [{1}] and replace by [{2}]",
                strDataToFind = objDataToFind == null ? "" : objDataToFind.ToString(),
                strTextSrc = objTextSrc == null ? "" : objTextSrc.ToString(),
                strRC = objRC == null ? "" : objRC.ToString()));
            string[] arrRplCmd = strRC.Split(new string[] { ":" }, StringSplitOptions.None);
            if (arrRplCmd.Length <= 1)
            {
                ///normal mode
                /// 
                Logger.Info("SearchAndReplaceGuiTxt", "Normal Mode, just find and replace");
                string[] arrStrFindAndRplace = strTextSrc.Split(new string[] { ":" }, StringSplitOptions.None);
                if (arrStrFindAndRplace.Length != 2)
                {
                    objError = string.Format("Formatter is wrong.For normal mode replacing, Data field should be strSearch:strReplace, but the data is :[{0}]", strTextSrc);
                    return false;
                }
                objNewTxt = strTextSrc.Replace(arrStrFindAndRplace[0], arrStrFindAndRplace[1]);
                return true;
            }
            if (!TigerMarsUtil.RegularTest("^addins:", strRC))
            {
                Logger.Error("SearchAndReplaceGuiTxt", (string)(objError = string.Format("Non-supported format, only Addins:[string1];[string2] is supported. but the current data is :[{0}]", strRC)));
                return false;
            }
            strRC = strRC.Substring(strRC.ToUpper().IndexOf("ADDINS:") + "ADDINS:".Length);
            Logger.Info("SearchAndReplaceGuiTxt", string.Format("Find and replace Function is :[{0}]", strRC));
            string[] arrFuncAndPara = strRC.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            int iCreaseBy = 1;
            if (arrFuncAndPara.Length <= 1)
                iCreaseBy = 1;
            else
            {
                int.TryParse(arrFuncAndPara[1], out iCreaseBy);
            }
            string strError = "", strReslt = "";
            switch (arrFuncAndPara[0])
            {
                case cnst_increase_last2:
                    if (!AddinsFuncs4VBS.Increase_lastN(strDataToFind, 2, iCreaseBy, ref strReslt, ref strError))
                    {
                        objError = strError;
                        return false;
                    }
                    objNewTxt = strTextSrc.Replace(strDataToFind, strReslt);
                    return true;
                case cnst_increase_last2_re:
                    MatchCollection lstMtches = Regex.Matches(strTextSrc, strDataToFind);
                    if (lstMtches.Count <= 0)
                    {
                        Logger.Error("SearchAndReplaceGuiTxt", (string)(objError = string.Format("Can't find regular expression:[{0}]", strDataToFind)));
                        return false;
                    }
                    foreach (Match itm in lstMtches)
                    {
                        string strMatchedItm = itm.Value;
                        // increase last N characters
                        if (!AddinsFuncs4VBS.Increase_lastN(strMatchedItm, 2, iCreaseBy, ref strReslt, ref strError))
                        {
                            Logger.Error("SearchAndReplaceGuiTxt", string.Format("Can't increase:[{0}] ", strMatchedItm));
                            continue;
                        }
                        // replace strings
                        strTextSrc = strTextSrc.Replace(strMatchedItm, strReslt);
                    }
                    objNewTxt = strTextSrc;
                    return true;
                default:
                    objError = string.Format("No supported addins function:[{0}]", arrFuncAndPara[0]);
                    return false;

            }

        }
        internal const string cnst_captureCmd_extract = "exractBy:";

        /// <summary>
        /// VBS demo format:  CaptureValue(object, exractBy:abcd[dddd]{4}, global_var:dataa, "")
        /// </summary>
        /// <param name="objSrc"></param>
        /// <param name="objRC"></param>
        /// <param name="objError"></param>
        /// <param name="objResult"></param>
        /// <returns></returns>
        public bool ExtractDataForCaptureValue(object objSrc, object objRC, ref object objError, ref object objResult)
        {
            string strSrc = objSrc == null ? "" : objSrc.ToString(),
                strRC = objRC == null ? "" : objRC.ToString();
            if (!TigerMarsUtil.RegularTest("^" + cnst_captureCmd_extract, strRC))
            {
                Logger.Info("ExtractDataForCaptureValue", string.Format("No supported command format:[{0}], it should start with :[{1}]", strSrc.Substring(0, 20), cnst_captureCmd_extract));
                objResult = objSrc;
                return true;
            }
            strRC = strRC.Substring(cnst_captureCmd_extract.Length);
            MatchCollection lstMtches = Regex.Matches(strSrc, strRC);
            if (lstMtches.Count <= 0)
            {
                Logger.Error("ExtractDataForCaptureValue", (string)(objError = string.Format("No such Pattern is find:[{0}] from Source:[{1}]", strRC, strSrc)));
                return false;
            }
            objResult = lstMtches[0].Value;
            return true;
        }

        //changeWith:FEDtrade[0-9]{3} RC format
        internal const string cnst_ChangeWith = "changeWith:";
        internal const string cnst_directWrite = "DirectWrite";
        public bool FillEditBySearchAndUpdate(object objSrc, object objRC, ref object objError, ref object objTargetTx)
        {
            string strSrc = objSrc == null ? "" : objSrc.ToString(),
                strRC = objRC == null ? "" : objRC.ToString(),
                strDataToReplace = objTargetTx == null ? "" : objTargetTx.ToString();

            if (string.Compare(cnst_directWrite, strDataToReplace, true) == 0)
                return true;

            if (!TigerMarsUtil.RegularTest("^" + cnst_ChangeWith, strRC))
            {
                Logger.Error("FillEditBySearchAndUpdate", (string)(objError = string.Format("RC should start with:[{0}],but it is:[{1}]", cnst_ChangeWith, strRC)));
                return false;
            }
            strRC = strRC.Substring(cnst_ChangeWith.Length);
            MatchCollection lstMtches = Regex.Matches(strSrc, strRC);
            if (lstMtches.Count <= 0)
            {
                Logger.Error("FillEditBySearchAndUpdate", (string)(objError = string.Format("No such Pattern is find:[{0}] from Source:[{1}]", strRC, strSrc)));
                return false;
            }
            objTargetTx = strSrc.Replace(lstMtches[0].Value, strDataToReplace);
            return true;
        }

        public object CallAddinsForDataDeal_After(object objKeyword, object objPegwindowsName, object objName, object objRC, object objDataSource, ref object isOk, ref object objError)
        {
            string strKeyword = objKeyword == null ? "" : objKeyword.ToString(),
                strPegwind = objPegwindowsName == null ? "" : objPegwindowsName.ToString(),
                strObjName = objName == null ? "" : objName.ToString(),
                strRc = objRC == null ? "" : objRC.ToString(),
                strDataSource = objDataSource == null ? "" : objDataSource.ToString();
            Logger.Info("CallAddinsForDataDeal_After", string.Format("keyword:[{0}] peg:[{1}] obj:[{2}] RC:[{3}] dataSource:[{4}]", strKeyword, strPegwind, strObjName, strRc, strDataSource));
            if (!ClientAddinsMgr.checkObjectKeywordsSupported(strKeyword, strPegwind, strObjName))
            {
                Logger.Info("CallAddinsForDataDeal_After", String.Format("unsupported peg :[{0}] Obj:[{1}] strKeyword:[{2}]", strPegwind, strObjName, strKeyword));
                isOk = true;
                return strDataSource;
            }
            string strError = "";
            bool bIsOk = ClientAddinsMgr.InvokeDataDealAfter(strKeyword, strPegwind, strObjName, strRc, strDataSource, ref strDataSource, ref strError);
            isOk = bIsOk;
            objError = strError;
            return strDataSource;
        }

        public object ScrollMouse(object oNum, ref object strError)
        {
            Logger.logBegin("ScrollMouse");

            int iNum = -1;
            if (oNum != null)
            {
                if (!int.TryParse(oNum.ToString(), out iNum))
                {
                    Logger.Info("WARNNING ScrollMouse", string.Format("Ojbect is not a number:[{0}]", oNum.ToString()));
                    iNum = -1;
                }
            }
            else
            {
                iNum = 10000;
            }
            Logger.Info("ScrollMouse", string.Format("WHEEL COUNT:[{0}]", iNum));
            for (int i = 0; i < Math.Abs(iNum); i++)
            {
                MouseSimulator.ScrollMouseTo(iNum < 0 ? -1 : 1);
                Thread.Sleep(20);
            }
            return true;
        }

        public string DecodePwdForKeyword(string strEnCodedPwd, ref bool isOk, ref string strError)
        {
            Logger.logBegin("DecodePwdForKeyword");
            try
            {
                isOk = true;
                return MarsEncodePwd.DecodeString(strEnCodedPwd);
            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("DecodePwdForKeyword", strError = string.Format("Exception:[{0}]", e.Message), e);
                return strEnCodedPwd;
            }
            finally
            {
                Logger.logEnd("DecodePwdForKeyword");
            }
        }
    }

    class KeywordsFunc
    {
        public string TargetTypeName;
        public string FunctionName;

        public KeywordsFunc(string strKeywordName, string strFunc)
        {
            this.TargetTypeName = strKeywordName;
            FunctionName = strFunc;
        }

        public bool IsAppliedKeyWord(string strKeywordName)
        {
            return TigerMarsUtil.RegularTest(TargetTypeName, strKeywordName);
        }
    }
#if _TMP_DEBUGER_
    [Serializable]
    public class TestAssemblyclass
    {
        public TestAssemblyclass()
        {
            Console.WriteLine("Current Domain Name:{0}\r\nCurrent Thread Code:{1}", AppDomain.CurrentDomain.FriendlyName, Thread.CurrentThread.GetHashCode().ToString());
        }

        public void TestMethode()
        {
            Console.WriteLine("TestMethode is invoked");
        }
    }
#endif

    public class MouseSimulator
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MouseSimulator));
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public SendInputEventType type;
            public MouseKeybdhardwareInputUnion mkhi;
        }
        [StructLayout(LayoutKind.Explicit)]
        struct MouseKeybdhardwareInputUnion
        {
            [FieldOffset(0)]
            public MouseInputData mi;

            [FieldOffset(0)]
            public KEYBDINPUT ki;

            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        struct HARDWAREINPUT
        {
            public int uMsg;
            public short wParamL;
            public short wParamH;
        }
        struct MouseInputData
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public MouseEventFlags dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        [Flags]
        enum MouseEventFlags : uint
        {
            MOUSEEVENTF_MOVE = 0x0001,
            MOUSEEVENTF_LEFTDOWN = 0x0002,
            MOUSEEVENTF_LEFTUP = 0x0004,
            MOUSEEVENTF_RIGHTDOWN = 0x0008,
            MOUSEEVENTF_RIGHTUP = 0x0010,
            MOUSEEVENTF_MIDDLEDOWN = 0x0020,
            MOUSEEVENTF_MIDDLEUP = 0x0040,
            MOUSEEVENTF_XDOWN = 0x0080,
            MOUSEEVENTF_XUP = 0x0100,
            MOUSEEVENTF_WHEEL = 0x0800,
            MOUSEEVENTF_VIRTUALDESK = 0x4000,
            MOUSEEVENTF_ABSOLUTE = 0x8000
        }
        enum SendInputEventType : int
        {
            InputMouse = 0,
            InputKeyboard = 1,
            InputHardware = 2
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        public static void ClickLeftMouseButton()
        {
            INPUT mouseDownInput = new INPUT();
            mouseDownInput.type = SendInputEventType.InputMouse;
            mouseDownInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_LEFTDOWN;
            SendInput(1, ref mouseDownInput, Marshal.SizeOf(new INPUT()));

            INPUT mouseUpInput = new INPUT();
            mouseUpInput.type = SendInputEventType.InputMouse;
            mouseUpInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_LEFTUP;
            SendInput(1, ref mouseUpInput, Marshal.SizeOf(new INPUT()));
        }
        public static void ClickRightMouseButton()
        {
            INPUT mouseDownInput = new INPUT();
            mouseDownInput.type = SendInputEventType.InputMouse;
            mouseDownInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_RIGHTDOWN;
            SendInput(1, ref mouseDownInput, Marshal.SizeOf(new INPUT()));

            INPUT mouseUpInput = new INPUT();
            mouseUpInput.type = SendInputEventType.InputMouse;
            mouseUpInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_RIGHTUP;
            SendInput(1, ref mouseUpInput, Marshal.SizeOf(new INPUT()));
        }

        public static void ScrollMouseTo(int iNum = 10000)
        {
            Logger.logBegin("ScrollMouseTo");
            INPUT input = new INPUT();
            input.type = SendInputEventType.InputMouse;
            //input.mkhi.mi = new MouseInputData();
            input.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_WHEEL;
            input.mkhi.mi.dwExtraInfo = IntPtr.Zero;
            input.mkhi.mi.dx = 0;
            input.mkhi.mi.dy = 0;
            input.mkhi.mi.time = 0;
            input.mkhi.mi.mouseData = (uint)(iNum * 120);
            uint iError = SendInput(1, ref input, Marshal.SizeOf(typeof(INPUT)));
            Logger.Info("ScrollMouseTo", string.Format("size:[{0}],iError=[{1}]", Marshal.SizeOf(typeof(INPUT)), iError));

            //Thread.Sleep(1000);
            //mouse_event((int)MouseEventFlags.MOUSEEVENTF_WHEEL, 0, 0, 200, 0);
            //Logger.Info("ScrollMouseTo", "Mouse_event");
        }


    }

    internal sealed class AddinsFuncs4VBS
    {
        internal static bool Increase_lastN(string strSrc, int iLstChngNum, int iCreaseBy, ref string strResult, ref string strError)
        {
            if (string.IsNullOrEmpty(strSrc))
            {
                strError = "input string is null or empty!";
                return false;
            }
            if (strSrc.Length < iLstChngNum)
            {
                strError = string.Format("input string [{0}] is less than [{1}]", strSrc, iLstChngNum);
                return false;
            }
            string strLstN = strSrc.Substring(strSrc.Length - iLstChngNum);
            int iN = 0;
            if (!int.TryParse(strLstN, out iN))
            {
                strError = string.Format("Last [{0}] characters are not number,---[{1}]", iLstChngNum, strLstN);
                return false;
            }
            string strTmpNumber = (((int)Math.Pow(10, iLstChngNum)) + (iN + 1) + "").Substring(1);
            strResult = string.Format("{0}{1}", strSrc.Substring(0, strSrc.Length - iLstChngNum), strTmpNumber);

            return true;
        }

    }

}
