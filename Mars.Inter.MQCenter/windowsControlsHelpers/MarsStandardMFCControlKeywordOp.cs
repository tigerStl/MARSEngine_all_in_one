using Mars.Inter.MQCenter.DataLayer.network.ErrorCheckData;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.windowsWrapper.SystemUtil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Mars.Inter.MQCenter.windowsControlsHelpers.ProcessAllControlsEnumerator;

namespace Mars.Inter.MQCenter.windowsControlsHelpers
{
    public class MarsStandardMFCControlKeywordOp
    {
        private static WindowInfo? CurrrentPegwindow = null;

        /// <summary>
        /// 查找唯一控件，返回唯一WindowInfo或null
        /// </summary>
        private static WindowInfo? FindUniqueControl(IntPtr parentHwnd, Dictionary<string, string> objProperties, ref bool isOk, ref string strError, ref string strStack, ref string strAdv)
        {
            isOk = false;
            var childHwnds = MarsWindowsAPIsExtend.GetChildWindows(parentHwnd);
            List<WindowInfo> allControls = new List<WindowInfo>();
            foreach (var childHwnd in childHwnds)
            {
                var childInfoOpt = ProcessAllControlsEnumerator.GetWindowInfoBatch(childHwnd);
                if (childInfoOpt != null)
                    allControls.Add(childInfoOpt.Value);
            }

            IEnumerable<WindowInfo> filtered = allControls;
            if (objProperties != null)
            {
                foreach (var key in objProperties.Keys)
                {
                    var value = objProperties[key];
                    if (key.Equals("Text", StringComparison.OrdinalIgnoreCase))
                    {
                        filtered = filtered.Where(w => MarsWindowsAPIsExtend.RegularTest(value, w.WindowText));
                    }
                    else if (key.Equals("ControlID", StringComparison.OrdinalIgnoreCase))
                    {
                        if (long.TryParse(value, out long controlId))
                            filtered = filtered.Where(w => w.controID == controlId);
                        else
                            filtered = filtered.Where(w => value.Equals(w.controID.ToString()));
                    }
                    else if (key.Equals("ClassName", StringComparison.OrdinalIgnoreCase))
                    {
                        filtered = filtered.Where(w => MarsWindowsAPIsExtend.RegularTest(value, w.ClassName));
                    }
                    // 可扩展其他属性
                }
            }

            var resultList = filtered.ToList();
            var keyIdx = objProperties?.Keys.FirstOrDefault(p => p.Equals("Index", StringComparison.OrdinalIgnoreCase));
            if (resultList.Count > 1)
            {
                if (!string.IsNullOrEmpty(keyIdx))
                {
                    if (!int.TryParse(objProperties[keyIdx], out int idx))
                    {
                        strError = "Index setting is not a number";
                        strStack = Environment.StackTrace;
                        strAdv = "Please check value of Index, and ensure it is a number";
                        return null;
                    }
                    if (idx < 0 || idx >= resultList.Count)
                    {
                        strError = $"Index {idx} out of range, only {resultList.Count} controls matched";
                        strStack = Environment.StackTrace;
                        strAdv = "Please check Index value, ensure it is within the range of matched controls";
                        return null;
                    }
                    resultList = new List<WindowInfo> { resultList[idx] };
                }
                else
                {
                    strError = "Multiple controls matched, but no Index specified";
                    strStack = Environment.StackTrace;
                    strAdv = "Please specify Index or add more filter conditions";
                    return null;
                }
            }
            if (resultList.Count == 0)
            {
                strError = "No control matched";
                strStack = Environment.StackTrace;
                strAdv = "Please check filter conditions";
                return null;
            }
            isOk = true;
            return resultList[0];
        }

        internal static bool Pegwindow(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName, string strObjName,
            Dictionary<string, string> objProperties,
            Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError, ref string strDataReturn, ref string strStack,
            ref string strAdv, ref string strSnapshotForShouldBeFile, bool isInnerCall,
            int waitingTime)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("MarsStandardMFCControlKeywordOp.Pegwindow", $"{iMark}|Pegwindow({strPegName}.{strObjName})|attach-{strAttachInfo}|peg-{MarsWindowsAPIsExtend.Dic2String(objPegProperties)}|obj-{MarsWindowsAPIsExtend.Dic2String(objProperties)}");

            // 1. 枚举所有窗口和子窗口
            int processId = Process.GetCurrentProcess().Id;
            var topWindows = MarsWindowsAPIsExtend.GetWindows(processId);
            List<WindowInfo> allWindows = new List<WindowInfo>();

            foreach (var hwnd in topWindows)
            {
                var winInfoOpt = ProcessAllControlsEnumerator.GetWindowInfoBatch(hwnd);
                if (winInfoOpt != null)
                    allWindows.Add(winInfoOpt.Value);

                // 枚举子窗口
                var childHwnds = MarsWindowsAPIsExtend.GetChildWindows(hwnd);
                foreach (var childHwnd in childHwnds)
                {
                    var childInfoOpt = ProcessAllControlsEnumerator.GetWindowInfoBatch(childHwnd);
                    if (childInfoOpt != null)
                        allWindows.Add(childInfoOpt.Value);
                }
            }

            // 2. 依据objPegProperties过滤
            IEnumerable<WindowInfo> filtered = allWindows;
            if (objPegProperties != null)
            {
                var keyTxt = objPegProperties.Keys.FirstOrDefault(p => p.Equals("Text", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(keyTxt))
                {
                    string textPattern = objPegProperties[keyTxt];
                    filtered = filtered.Where(w => MarsWindowsAPIsExtend.RegularTest(textPattern, w.WindowText));
                }
                var ctrlId = objPegProperties.Keys.FirstOrDefault(p => p.Equals("ControlID", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(ctrlId))
                {
                    if (long.TryParse(objPegProperties[ctrlId], out long controlId))
                    {
                        filtered = filtered.Where(w => w.controID == controlId);
                    }
                    else
                    {
                        filtered = filtered.Where(w => objPegProperties[ctrlId].Equals(w.controID.ToString()));
                    }
                }
                // 可扩展其他key
                var classNameKey = objPegProperties.Keys.FirstOrDefault(p => p.Equals("ClassName", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(classNameKey))
                {
                    string classPattern = objPegProperties[classNameKey];
                    filtered = filtered.Where(w => MarsWindowsAPIsExtend.RegularTest(classPattern, w.ClassName));
                }
            }

            var resultList = filtered.ToList();

            if (resultList.Count == 0)
            {
                strError = "Pegwindow: No window matched";
                strStack = Environment.StackTrace;
                strAdv = "Please check Text or ControlID";
                MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.Pegwindow", $"{iMark}|{strError}|{strStack}");
                return false;
            }

            var keyIdx = objPegProperties.Keys.FirstOrDefault(p => p.Equals("Index", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(keyIdx))
            {
                if (!int.TryParse(objPegProperties[keyIdx], out int idx))
                {
                    strError = "Pegwindow: Index setting is not a number";
                    strStack = Environment.StackTrace;
                    strAdv = "Please check value of Index, and ensure it is a nubmer";
                    MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.Pegwindow", $"{iMark}|{strError}|{strStack}");
                    return false;
                }
                if ((idx >= resultList.Count) && (idx >= 0))
                {
                    strError = $"Pegwindow: Index {idx} out of range, only {resultList.Count} windows matched";
                    strStack = Environment.StackTrace;
                    strAdv = "Please check Index value, ensure it is within the range of matched windows";
                    MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.Pegwindow", $"{iMark}|{strError}|{strStack}");
                    return false;
                }

                resultList = new List<WindowInfo> { resultList[idx] };
            }
            if (resultList.Count > 1)
            {
                strError = "there is more than windows, please ensure settings is right";
                strStack = "Pegwindow: Multiple windows matched";
                strAdv = "Please ensure only one windows is matched";
                MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.Pegwindow", $"{iMark}|{strError}|{strStack}");
                return false;
            }

            // 唯一窗口
            CurrrentPegwindow = resultList[0];
            MarsLoggerSimple.logEnd("MarsStandardMFCControlKeywordOp.Pegwindow", $"{iMark}|OK");
            return true;
        }

        internal static bool FillEdit(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName, string strObjName,
            Dictionary<string, string> objProperties,
            Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError, ref string strDataReturn, ref string strStack,
            ref string strAdv, ref string strSnapshotForShouldBeFile, bool isInnerCall,
            int waitingTime)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("MarsStandardMFCControlKeywordOp.FillEdit", $"{iMark}|FillEdit({strPegName}.{strObjName}, {strParaMeter},{strData})|attach-{strAttachInfo}|peg-{MarsWindowsAPIsExtend.Dic2String(objPegProperties)}|obj-{MarsWindowsAPIsExtend.Dic2String(objProperties)}");
            if (CurrrentPegwindow == null)
            {
                strError = "No current pegwindow is set";
                strStack = Environment.StackTrace;
                strAdv = "please execute Pegwindow first";
                MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.FillEdit", $"{iMark}|{strError}");
                return false;
            }

            bool isSkip_notExist = string.Compare("SKIP_NOTEXIST", strParaMeter, true) == 0;
            bool isOk = false;

            var targetInfo = FindUniqueControl((IntPtr)CurrrentPegwindow.Value.hwnd, objProperties, ref isOk, ref strError, ref strStack, ref strAdv);
            if (!isOk || targetInfo == null)
            {
                MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.FillEdit", $"{iMark}|{strError}");
                if (isSkip_notExist)
                {
                    MarsLoggerSimple.Warnning("MarsStandardMFCControlKeywordOp.ClickButton", $"{iMark}|SKIP_NOTEXIST|ignore error");
                    return true;
                }
                return false;
            }

            IntPtr editHwnd = (IntPtr)targetInfo.Value.hwnd;

            bool noClean = !string.IsNullOrEmpty(strParaMeter) && strParaMeter.IndexOf("no_clean", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!noClean)
            {
                int centerX = targetInfo.Value.Rect.Left + (targetInfo.Value.Rect.Right - targetInfo.Value.Rect.Left) / 2;
                int centerY = targetInfo.Value.Rect.Top + (targetInfo.Value.Rect.Bottom - targetInfo.Value.Rect.Top) / 2;
                MarsWindowsAPIsExtend.LeftMouseClick(centerX, centerY);

                MarsWindowsAPIsExtend.RepeatSendVK(editHwnd, (int)VirtualKeyStates.VK_BACK);
                MarsWindowsAPIsExtend.RepeatSendVK(editHwnd, (int)VirtualKeyStates.VK_DELETE);
            }
            System.Threading.Thread.Sleep(100);
            MarsWindowsAPIsExtend.SimulateInputString(strData);

            strDataReturn = "填充成功";
            MarsLoggerSimple.logEnd("MarsStandardMFCControlKeywordOp.FillEdit", $"{iMark}|OK");
            return true;
        }

        internal static bool ClickButton(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName, string strObjName,
            Dictionary<string, string> objProperties,
            Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError, ref string strDataReturn, ref string strStack,
            ref string strAdv, ref string strSnapshotForShouldBeFile, bool isInnerCall,
            int waitingTime)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("MarsStandardMFCControlKeywordOp.ClickButton", $"{iMark}|ClickButton({strPegName}.{strObjName})|attach-{strAttachInfo}|peg-{MarsWindowsAPIsExtend.Dic2String(objPegProperties)}|obj-{MarsWindowsAPIsExtend.Dic2String(objProperties)}");
            if (CurrrentPegwindow == null)
            {
                strError = "No current pegwindow is set";
                strStack = Environment.StackTrace;
                strAdv = "please execute Pegwindow first";
                MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.ClickButton", $"{iMark}|{strError}");
                return false;
            }

            bool isSkip_notExist = string.Compare("SKIP_NOTEXIST", strParaMeter, true) == 0;
            bool isOk = false;

            var targetInfo = FindUniqueControl((IntPtr)CurrrentPegwindow.Value.hwnd, objProperties, ref isOk, ref strError, ref strStack, ref strAdv);
            if (!isOk || targetInfo == null)
            {
                MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.ClickButton", $"{iMark}|{strError}");
                if (isSkip_notExist) {
                    MarsLoggerSimple.Warnning("MarsStandardMFCControlKeywordOp.ClickButton", $"{iMark}|SKIP_NOTEXIST|ignore error" );
                    return true;
                }
                return false;
            }

            IntPtr buttonHwnd = (IntPtr)targetInfo.Value.hwnd;
            int centerX = targetInfo.Value.Rect.Left + (targetInfo.Value.Rect.Right - targetInfo.Value.Rect.Left) / 2;
            int centerY = targetInfo.Value.Rect.Top + (targetInfo.Value.Rect.Bottom - targetInfo.Value.Rect.Top) / 2;
            MarsWindowsAPIsExtend.LeftMouseClick(centerX, centerY);

            strDataReturn = "点击成功";
            MarsLoggerSimple.logEnd("MarsStandardMFCControlKeywordOp.ClickButton", $"{iMark}|OK");
            return true;
        }

        internal static bool SelectTab(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName, string strObjName,
            Dictionary<string, string> objProperties,
            Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError, ref string strDataReturn, ref string strStack,
            ref string strAdv, ref string strSnapshotForShouldBeFile, bool isInnerCall,
            int waitingTime)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("MarsStandardMFCControlKeywordOp.SelectTab", $"{iMark}|SelectTab({strPegName}.{strObjName})|attach-{strAttachInfo}|peg-{MarsWindowsAPIsExtend.Dic2String(objPegProperties)}|obj-{MarsWindowsAPIsExtend.Dic2String(objProperties)}");
            if (CurrrentPegwindow == null)
            {
                strError = "No current pegwindow is set";
                strStack = Environment.StackTrace;
                strAdv = "please execute Pegwindow first";
                MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.SelectTab", $"{iMark}|{strError}");
                return false;
            }

            bool isSkip_notExist = string.Compare("SKIP_NOTEXIST", strParaMeter, true) == 0;
            bool isOk = false;

            var targetInfo = FindUniqueControl((IntPtr)CurrrentPegwindow.Value.hwnd, objProperties, ref isOk, ref strError, ref strStack, ref strAdv);
            if (!isOk || targetInfo == null)
            {
                MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.SelectTab", $"{iMark}|{strError}");
                if (isSkip_notExist)
                {
                    MarsLoggerSimple.Warnning("MarsStandardMFCControlKeywordOp.SelectTab", $"{iMark}|SKIP_NOTEXIST|ignore error");
                    return true;
                }
                return false;
            }

            IntPtr tabHwnd = (IntPtr)targetInfo.Value.hwnd;

            // 1. 获取所有tab header caption
            const int TCM_GETITEMCOUNT = 0x1304;
            IntPtr tmp = new IntPtr();
            int tabCount = MarsWindowsAPIs.SendMessage(tabHwnd, TCM_GETITEMCOUNT, 0, ref tmp).ToInt32();
            List<string> tabHeaders = new List<string>();
            for (int i = 0; i < tabCount; i++)
            {
                string itemText = MFCAndStandardTabControlHelper.GetTabItemText(tabHwnd, i);
                tabHeaders.Add(itemText ?? "");
            }

            // 2. 用regularTest定位目标tabindex
            List<int> matchedIndexes = new List<int>();
            for (int i = 0; i < tabHeaders.Count; i++)
            {
                if (MarsWindowsAPIsExtend.RegularTest(strData, tabHeaders[i]))
                    matchedIndexes.Add(i);
            }

            int targetTabIndex = -1;
            if (matchedIndexes.Count == 0)
            {
                strError = $"SelectTab: No tab matched with caption '{strData}'";
                strStack = Environment.StackTrace;
                strAdv = "Please check tab caption or regular expression";
                MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.SelectTab", $"{iMark}|{strError}");
                return false;
            }
            else if (matchedIndexes.Count > 1)
            {
                // 如果有Index参数则用Index，否则报错
                if (objProperties != null && objProperties.TryGetValue("Index", out string idxStr) && int.TryParse(idxStr, out int idx) && idx >= 0 && idx < matchedIndexes.Count)
                {
                    targetTabIndex = matchedIndexes[idx];
                }
                else
                {
                    strError = $"SelectTab: Multiple tabs matched, but no Index specified. Matched indexes: {string.Join(",", matchedIndexes)}";
                    strStack = Environment.StackTrace;
                    strAdv = "Please specify Index or use more specific regular expression";
                    MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.SelectTab", $"{iMark}|{strError}");
                    return false;
                }
            }
            else
            {
                targetTabIndex = matchedIndexes[0];
            }

            // 3. 获取tabheader位置，移动鼠标并点击
            var tabRect = MFCAndStandardTabControlHelper.GetTabItemRect(tabHwnd, targetTabIndex);
            int centerX = tabRect.Left + (tabRect.Right - tabRect.Left) / 2;
            int centerY = tabRect.Top + (tabRect.Bottom - tabRect.Top) / 2;
            MarsWindowsAPIsExtend.LeftMouseClick(centerX, centerY);

            // 等待进程进入空闲状态
            try
            {
                Process currentProcess = Process.GetCurrentProcess();
                currentProcess.WaitForInputIdle(60000); // 最多等待10秒
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.SelectTab", $"{iMark}|WaitForInputIdle failed|{ex.Message}");
            }

            // 4. 检查是否完成切换
            const int TCM_GETCURSEL = 0x130B;
            int curSel = MarsWindowsAPIs.SendMessage(tabHwnd, TCM_GETCURSEL,0, IntPtr.Zero).ToInt32();
            if (curSel == targetTabIndex)
            {
                strDataReturn = $"Tab has been switched to Index={targetTabIndex}, Caption={tabHeaders[targetTabIndex]}";
                MarsLoggerSimple.logEnd("MarsStandardMFCControlKeywordOp.SelectTab", $"{iMark}|OK");
                return true;
            }
            else
            {
                strError = $"SelectTab: Tab switched failed，current tab Index={curSel}, target Index={targetTabIndex}";
                strStack = Environment.StackTrace;
                strAdv = "Please monitor Tab actions";
                MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.SelectTab", $"{iMark}|{strError}");
                return false;
            }
        }

        internal static bool SelectDropDown(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName, string strObjName,
            Dictionary<string, string> objProperties,
            Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError, ref string strDataReturn, ref string strStack,
            ref string strAdv, ref string strSnapshotForShouldBeFile, bool isInnerCall,
            int waitingTime)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("MarsStandardMFCControlKeywordOp.SelectDropDown", $"{iMark}|SelectDropDown({strPegName}.{strObjName})|attach-{strAttachInfo}|peg-{MarsWindowsAPIsExtend.Dic2String(objPegProperties)}|obj-{MarsWindowsAPIsExtend.Dic2String(objProperties)}");
            if (CurrrentPegwindow == null)
            {
                strError = "No current pegwindow is set";
                strStack = Environment.StackTrace;
                strAdv = "please execute Pegwindow first";
                MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.SelectDropDown", $"{iMark}|{strError}");
                return false;
            }

            bool isSkip_notExist = string.Compare("SKIP_NOTEXIST", strParaMeter, true) == 0;
            bool isOk = false;

            var targetInfo = FindUniqueControl((IntPtr)CurrrentPegwindow.Value.hwnd, objProperties, ref isOk, ref strError, ref strStack, ref strAdv);
            if (!isOk || targetInfo == null)
            {
                MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.SelectDropDown", $"{iMark}|{strError}");
                if (isSkip_notExist)
                {
                    MarsLoggerSimple.Warnning("MarsStandardMFCControlKeywordOp.SelectDropDown", $"{iMark}|SKIP_NOTEXIST|ignore error");
                    return true;
                }
                return false;
            }

            IntPtr comboHwnd = (IntPtr)targetInfo.Value.hwnd;

            // 1. 点击下拉框展开
            var comboRect = targetInfo.Value.Rect;
            int centerX = comboRect.Left + (comboRect.Right - comboRect.Left) / 2;
            int centerY = comboRect.Top + (comboRect.Bottom - comboRect.Top) / 2;
            MarsWindowsAPIsExtend.LeftMouseClick(centerX, centerY);
            System.Threading.Thread.Sleep(200);

            // 2. 获取所有选项文本
            const int CB_GETCOUNT = 0x0146;
            int itemCount = MarsWindowsAPIs.SendMessage(comboHwnd, CB_GETCOUNT, 0, IntPtr.Zero).ToInt32();
            List<string> itemTexts = new List<string>();
            for (int i = 0; i < itemCount; i++)
            {
                string itemText = MFCAndStandardComboboxHelper.GetComboBoxItemText(comboHwnd, i);
                itemTexts.Add(itemText ?? "");
            }

            // 3. 用regularTest定位目标index
            List<int> matchedIndexes = new List<int>();
            for (int i = 0; i < itemTexts.Count; i++)
            {
                if (MarsWindowsAPIsExtend.RegularTest(strData, itemTexts[i]))
                    matchedIndexes.Add(i);
            }

            int targetIndex = -1;
            if (matchedIndexes.Count == 0)
            {
                strError = $"SelectDropDown: No item matched with text '{strData}'";
                strStack = Environment.StackTrace;
                strAdv = "Please check dropdown item text or regular expression";
                MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.SelectDropDown", $"{iMark}|{strError}");
                return false;
            }
            else if (matchedIndexes.Count > 1)
            {
                if (objProperties != null && objProperties.TryGetValue("Index", out string idxStr) && int.TryParse(idxStr, out int idx) && idx >= 0 && idx < matchedIndexes.Count)
                {
                    targetIndex = matchedIndexes[idx];
                }
                else
                {
                    strError = $"SelectDropDown: Multiple items matched, but no Index specified. Matched indexes: {string.Join(",", matchedIndexes)}";
                    strStack = Environment.StackTrace;
                    strAdv = "Please specify Index or use more specific regular expression";
                    MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.SelectDropDown", $"{iMark}|{strError}");
                    return false;
                }
            }
            else
            {
                targetIndex = matchedIndexes[0];
            }

            // 4. 获取目标item位置，点击选中
            var itemRect = MFCAndStandardComboboxHelper.GetComboBoxItemRect(comboHwnd, targetIndex);
            int itemCenterX = itemRect.Left + (itemRect.Right - itemRect.Left) / 2;
            int itemCenterY = itemRect.Top + (itemRect.Bottom - itemRect.Top) / 2;
            MarsWindowsAPIsExtend.LeftMouseClick(itemCenterX, itemCenterY);

            // 等待进程空闲
            try
            {
                Process currentProcess = Process.GetCurrentProcess();
                currentProcess.WaitForInputIdle(60000);
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.SelectDropDown", $"{iMark}|WaitForInputIdle failed|{ex.Message}");
            }

            // 5. 检查是否选中
            const int CB_GETCURSEL = 0x0147;
            int curSel = MarsWindowsAPIs.SendMessage(comboHwnd, CB_GETCURSEL, 0, IntPtr.Zero).ToInt32();
            if (curSel == targetIndex)
            {
                strDataReturn = $"combobox successed, Index={targetIndex}, Text={itemTexts[targetIndex]}";
                MarsLoggerSimple.logEnd("MarsStandardMFCControlKeywordOp.SelectDropDown", $"{iMark}|OK");
                return true;
            }
            else
            {
                strError = $"SelectDropDown: failed，currrent Index={curSel},but target Index={targetIndex}";
                strStack = Environment.StackTrace;
                strAdv = "please monitor ComboBox's action ";
                MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.SelectDropDown", $"{iMark}|{strError}");
                return false;
            }
        }

        internal static bool SnapShot(string strParaMeter, string strData, string strobjType,
            string strAttachInfo, string strPegName, string strObjName,
            Dictionary<string, string> objProperties,
            Dictionary<string, string> objPegProperties,
            MarsErrorCheckData errorCheckObj,
            ref string strError, ref string strDataReturn, ref string strStack,
            ref string strAdv, ref string strSnapshotForShouldBeFile, bool isInnerCall,
            int waitingTime)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("MarsStandardMFCControlKeywordOp.SnapShot", $"{iMark}|SnapShot({strPegName}.{strObjName})|attach-{strAttachInfo}|peg-{MarsWindowsAPIsExtend.Dic2String(objPegProperties)}|obj-{MarsWindowsAPIsExtend.Dic2String(objProperties)}");
            if (CurrrentPegwindow == null)
            {
                strError = "No current pegwindow is set";
                strStack = Environment.StackTrace;
                strAdv = "please execute Pegwindow first";
                MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.SnapShot", $"{iMark}|{strError}");
                return false;
            }

            bool isOk = false;
            var targetInfo = FindUniqueControl((IntPtr)CurrrentPegwindow.Value.hwnd, objProperties, ref isOk, ref strError, ref strStack, ref strAdv);
            if (!isOk || targetInfo == null)
            {
                MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.SnapShot", $"{iMark}|{strError}");
                return false;
            }

            IntPtr hwnd = (IntPtr)targetInfo.Value.hwnd;
            var rect = targetInfo.Value.Rect;

            // 生成文件名
            string fileName = $"{strPegName}_{strObjName}_{DateTime.Now:yyyyMMdd_HHmmssfff}.png";
            // 获取当前程序集目录
            string assemblyDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            // 拼接 /captureImages/ 子目录
            string captureDir = System.IO.Path.Combine(assemblyDir, "captureImages");
            // 若目录不存在则创建
            if (!System.IO.Directory.Exists(captureDir))
                System.IO.Directory.CreateDirectory(captureDir);
            // 生成完整文件路径
            string filePath = System.IO.Path.Combine(captureDir, fileName);

            // 截图并保存
            bool captureOk = CaptureWindowToFile(hwnd, rect, filePath, ref strError);
            if (!captureOk)
            {
                strStack = Environment.StackTrace;
                strAdv = "Please check window handle and rect";
                MarsLoggerSimple.Error("MarsStandardMFCControlKeywordOp.SnapShot", $"{iMark}|{strError}");
                return false;
            }

            strDataReturn = strSnapshotForShouldBeFile = filePath;
            //strDataReturn = $"Snapshot saved: {filePath}";
            MarsLoggerSimple.logEnd("MarsStandardMFCControlKeywordOp.SnapShot", $"{iMark}|OK|{filePath}");
            return true;
        }

        private static bool CaptureWindowToFile(IntPtr hwnd, MarsWindowsAPIs.RECT rect, string filePath, ref string strError)
        {
            try
            {
                using (var bmp = new System.Drawing.Bitmap(rect.Right - rect.Left, rect.Bottom - rect.Top))
                {
                    using (var g = System.Drawing.Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                    }
                    bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                }
                return true;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                return false;
            }
        }

    }
}
