using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Mars.message.AutoTestingDriver.interProcess;
//using Route2NSEx.src.Marquis.systemUtil;
using Mars.message.windowsWrapper.SystemUtil;
using Mars.AutoTestingDriver.ExecuteTestcase.MarsProcess;
using Mars.Inter.MQCenter.MSAASupport;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.Inter.MQCenter.windowsControlsHelpers.MarsMSAASupport;
using System.Drawing;
using Accessibility;

namespace Mars.AutoTestingDriver.ExecuteTestcase.MarsMSAASupport
{
    /// <summary>
    /// Helper class for supporting MSAA (Microsoft Active Accessibility) methods
    /// </summary>
    public class MARSMSAAHelper
    {
        //private static MLogger MarsLoggerSimple = MLogger.GetLogger(typeof(MARSMSAAHelper));

        /// <summary>
        /// Determines if the object should use MSAA based on its properties
        /// </summary>
        /// <param name="dictPegProperties">Dictionary containing peg window properties</param>
        /// <param name="dictObjProperties">Dictionary containing object properties</param>
        /// <param name="strError">Reference to error message string</param>
        /// <returns>True if the object should use MSAA, false otherwise</returns>
        public static bool IsUsingMSAA(Dictionary<string, string> dictPegProperties, 
            Dictionary<string, string> dictObjProperties, ref string strError)
        {
            MarsLoggerSimple.logBegin("IsUsingMSAA", "Checking if object should use MSAA");
            
            try
            {
                if (dictPegProperties == null && dictObjProperties == null)
                {
                    strError = "Both peg properties and object properties are null";
                    MarsLoggerSimple.Error("IsUsingMSAA", strError);
                    return false;
                }

                // Check for MSAA-related properties in object properties
                if (dictObjProperties != null)
                {
                    // Check for common MSAA properties
                    var msaaKeys = new[] { "attachText","winClass", "controlId", "winTable", "mfcTable", "StandardTable", "AccessibleName", "AccessibleRole", "AccessibleValue" };
                    
                    foreach (var key in msaaKeys)
                    {
                        var foundKey = dictObjProperties.Keys.FirstOrDefault(k => 
                            string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
                        if (foundKey != null)
                        {
                            MarsLoggerSimple.Info("IsUsingMSAA", $"Found MSAA property: {foundKey}");
                            return true;
                        }
                    }

                    // Check for MSAA-related object types
                    var msaaTypes = new[] { "Table", "List", "Tree", "Tab", "Menu", "ToolBar", "StatusBar", "ProgressBar" };
                    if (dictObjProperties.ContainsKey("type"))
                    {
                        string objectType = dictObjProperties["type"];
                        if (msaaTypes.Any(type => string.Equals(type, objectType, StringComparison.OrdinalIgnoreCase)))
                        {
                            MarsLoggerSimple.Info("IsUsingMSAA", $"Found MSAA object type: {objectType}");
                            return true;
                        }
                    }
                }

                // Check for MSAA-related properties in peg properties
                if (dictPegProperties != null)
                {
                    var msaaKeys = new[] { "control_Id", "AccessibleName", "AccessibleRole" };
                    
                    foreach (var key in msaaKeys)
                    {
                        var foundKey = dictPegProperties.Keys.FirstOrDefault(k => 
                            string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
                        if (foundKey != null)
                        {
                            MarsLoggerSimple.Info("IsUsingMSAA", $"Found MSAA property in peg: {foundKey}");
                            return true;
                        }
                    }
                }

                MarsLoggerSimple.Info("IsUsingMSAA", "No MSAA properties found, object will not use MSAA");
                return false;
            }
            catch (Exception ex)
            {
                strError = $"Exception occurred while checking MSAA properties: {ex.Message}";
                MarsLoggerSimple.Error("IsUsingMSAA", strError);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("IsUsingMSAA");
            }
        }

        /// <summary>
        /// Searches for an object using MSAA and performs a click action
        /// 参数模式如下：
        /// 
        /// </summary>
        /// <param name="keywordName">Keyword name</param>
        /// <param name="stepId">Step ID</param>
        /// <param name="dictPegProperties">Peg window properties</param>
        /// <param name="dictObjProperties">Object properties</param>
        /// <param name="strParaMeter">1，标准模式：
        ///         @"MarsAddins;\S.*;Action:(NO_ACTION|LEFT_CLICK|LEFT_DBL_CLICK|RIGHT_CLICK|SCROLLLEFT_DBL_CLICK|L_CLICKROWHEADER(:\S.*){0,}|L_CLICKAT:\S.*)"
        ///         2, 多column searchAndClick:
        ///         @"MultipleSearch;\[\S+\];Action:(NO_ACTION|LEFT_CLICK|LEFT_DBL_CLICK|RIGHT_CLICK|SCROLL|L_CLICKROWHEADER(:\S.*){0,}|L_CLICKAT:\S.*)";
        ///         3，多column，+index（当有多个满足条件的时候，依据index进行选择）
        ///   在ClickAt，使用相对坐标。如何为负数，如-5,-6，表示从右下角开始算起        
        /// </param>
        /// <param name="strData">Data parameter</param>
        /// <param name="typeName">Object type name</param>
        /// <param name="strAttachInfo">Attachment info</param>
        /// <param name="v2">Parameter 2</param>
        /// <param name="v3">Parameter 3</param>
        /// <param name="strError">Error message reference</param>
        /// <param name="dealResult">Deal result reference</param>
        /// <returns>True if successful, false otherwise</returns>
        public static bool MARSStandard_SearchAndClick(string keywordName, long stepId, 
            Dictionary<string, string> dictPegProperties, 
            Dictionary<string, string> dictObjProperties, string strParaMeter, 
            string strData, string typeName, string strAttachInfo, 
            string pegWindName, string objName, ref string strError, ref MARSDealResult dealResult)
        {
            MarsLoggerSimple.logBegin("MARSStandard_SearchAndClick", $"keywordName: {keywordName}, stepId: {stepId}|{pegWindName}.{objName}");

            try
            {
                // Step 1: Find the parent window based on peg properties
                IntPtr parentHwnd = IntPtr.Zero;
                if (!FindParentWindow(dictPegProperties, ref parentHwnd, ref strError))
                {
                    MarsLoggerSimple.Error("MARSStandard_SearchAndClick", $"{pegWindName}.{objName}|Failed to find parent window: {strError}");
                    return false;
                }

                MarsLoggerSimple.Info("MARSStandard_SearchAndClick", $"Found parent window: {parentHwnd}");

                // Step 2: Create MSAA interface for parent window
                using (var provider = new MarsAutoAccessibleSupportProvider())
                {
                    if (!provider.CreateAccessibleObject(parentHwnd, ref strError))
                    {
                        MarsLoggerSimple.Error("MARSStandard_SearchAndClick", $"{pegWindName}.{objName}|Failed to create interface: {strError}");
                        return false;
                    }

                    MarsLoggerSimple.Info("MARSStandard_SearchAndClick", "Successfully created MSAA interface for parent window");

                    // Step 3: Find target child object using MSAA
                    dynamic targetObject = null;
                    if (!provider.FindChildObject(dictObjProperties, parentHwnd, ref targetObject, ref strError))
                    {
                        MarsLoggerSimple.Error("MARSStandard_SearchAndClick", $"{pegWindName}.{objName}|Failed to find target child object: {strError}");
                        return false;
                    }

                    MarsLoggerSimple.Info("MARSStandard_SearchAndClick", "Successfully found target child object");

                    // Step 4: Parse action parameter and perform the action
                    if (!SearchAndClickOp.ParseAndExecuteAction(targetObject, typeName, strParaMeter, strData, ref strError, ref dealResult))
                    {
                        MarsLoggerSimple.Error("MARSStandard_SearchAndClick", $"{pegWindName}.{objName}|Failed to execute action: {strError}");
                        return false;
                    }
                }

                MarsLoggerSimple.Info("MARSStandard_SearchAndClick", "Successfully executed MSAA action");
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Exception occurred in MARSStandard_SearchAndClick: {ex.Message}";
                MarsLoggerSimple.Error("MARSStandard_SearchAndClick", strError);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("MARSStandard_SearchAndClick");
            }
        }

        public static Rectangle GetRectangle(IAccessible accObj, ref bool isOk, ref string strError)
        {
            Rectangle rect = new Rectangle(0, 0, 0, 0);
            try
            {
                if (accObj == null)
                {
                    strError = "Accessible object is null.";
                    MarsLoggerSimple.Error("GetRectangle", strError);
                    isOk = false;
                    return rect;
                }
                int left = 0, top = 0, width = 0, height = 0;
                accObj.accLocation(out left, out top, out width, out height, 0);
                rect = new Rectangle(left, top, width, height);
                isOk = true;
                return rect;
            }
            catch (Exception e)
            {
                strError = $"Exception in GetRectangle: {e.Message}";
                MarsLoggerSimple.Error("GetRectangle", strError);
                isOk = false;
                return rect;
            }

        }

        /// <summary>
        /// Finds the parent window based on peg properties
        /// </summary>
        /// <param name="dictPegProperties">Peg properties containing text/title and controlId</param>
        /// <param name="parentHwnd">Reference to parent window handle</param>
        /// <param name="strError">Error message reference</param>
        /// <returns>True if parent window found, false otherwise</returns>
        private static bool FindParentWindow(Dictionary<string, string> dictPegProperties, ref IntPtr parentHwnd, ref string strError)
        {
            MarsLoggerSimple.logBegin("FindParentWindow", "Searching for parent window");

            try
            {
                if (dictPegProperties == null || dictPegProperties.Count == 0)
                {
                    strError = "Peg properties are null or empty";
                    return false;
                }

                // Get text/title pattern from peg properties
                string textPattern = null;
                var textKey = dictPegProperties.Keys.FirstOrDefault(k => 
                    string.Equals(k, "text", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(k, "title", StringComparison.OrdinalIgnoreCase));
                
                if (textKey != null)
                {
                    textPattern = dictPegProperties[textKey];
                }

                // Get control ID from peg properties
                string controlIdStr = null;
                var controlIdKey = dictPegProperties.Keys.FirstOrDefault(k => 
                    string.Equals(k, "controlid", StringComparison.OrdinalIgnoreCase));
                
                if (controlIdKey != null)
                {
                    controlIdStr = dictPegProperties[controlIdKey];
                }

                // Get winClass from peg properties
                string winClassPattern = null;
                var winClassKey = dictPegProperties.Keys.FirstOrDefault(k => 
                    string.Equals(k, "winclass", StringComparison.OrdinalIgnoreCase));
                
                if (winClassKey != null)
                {
                    winClassPattern = dictPegProperties[winClassKey];
                }

                // Get nativeClass from peg properties (same handling as winClass)
                string nativeClassPattern = null;
                var nativeClassKey = dictPegProperties.Keys.FirstOrDefault(k => 
                    string.Equals(k, "nativeclass", StringComparison.OrdinalIgnoreCase));
                
                if (nativeClassKey != null)
                {
                    nativeClassPattern = dictPegProperties[nativeClassKey];
                }

                // Get index if specified
                string indexStr = null;
                var indexKey = dictPegProperties.Keys.FirstOrDefault(k => 
                    string.Equals(k, "index", StringComparison.OrdinalIgnoreCase));
                
                if (indexKey != null)
                {
                    indexStr = dictPegProperties[indexKey];
                }

                // Check if we should only search top-level windows
                bool isTopWindowOnly = false; // Default to false (search all windows including child windows)
                var isChildWindowKey = dictPegProperties.Keys.FirstOrDefault(k => 
                    string.Equals(k, "is Child Window", StringComparison.OrdinalIgnoreCase));
                
                if (isChildWindowKey != null)
                {
                    bool.TryParse(dictPegProperties[isChildWindowKey], out bool isChildWindow);
                    isTopWindowOnly = !isChildWindow; // If is Child Window is false, then only search top-level windows
                }
                // If isChildWindowKey is null (property doesn't exist), keep isTopWindowOnly = false to search all windows

                if (string.IsNullOrEmpty(textPattern) && string.IsNullOrEmpty(controlIdStr) && 
                    string.IsNullOrEmpty(winClassPattern) && string.IsNullOrEmpty(nativeClassPattern))
                {
                    strError = "No matching criteria found in peg properties (text/title, controlId, winClass, or nativeClass)";
                    return false;
                }

                List<IntPtr> matchingWindows = new List<IntPtr>();
                int targetProcessId = MARSTestProcess.CurrentTestProcessId;
                
                // First, find all top-level windows belonging to the target process
                List<IntPtr> processTopLevelWindows = new List<IntPtr>();
                
                // Define the function to collect top-level windows from target process
                MarsWindowsAPIs.EnumWindowsProc collectTopLevelWindows = (hWnd, lParam) =>
                {
                    return CollectTopLevelProcessWindows(hWnd, targetProcessId, processTopLevelWindows);
                };

                // Collect all top-level windows from target process
                MarsLoggerSimple.Info("FindParentWindow", $"Collecting top-level windows from process {targetProcessId}");
                MarsWindowsAPIs.EnumWindows(collectTopLevelWindows, IntPtr.Zero);
                
                MarsLoggerSimple.Info("FindParentWindow", $"Found {processTopLevelWindows.Count} top-level windows from process {targetProcessId}");

                // Now collect all windows (top-level + child windows) based on isTopWindowOnly setting
                List<IntPtr> allProcessWindows = new List<IntPtr>();
                
                foreach (IntPtr topLevelHwnd in processTopLevelWindows)
                {
                    // Always add the top-level window
                    allProcessWindows.Add(topLevelHwnd);
                    
                    // If not top-level only, also collect child windows
                    if (!isTopWindowOnly)
                    {
                        MarsLoggerSimple.Info("FindParentWindow", $"Collecting child windows from top-level window: {topLevelHwnd}");
                        MarsWindowsAPIs.EnumChildWindows(topLevelHwnd, (childHwnd, lParam) =>
                        {
                            // Verify child window belongs to target process (should always be true, but double-check)
                            MarsWindowsAPIs.GetWindowThreadProcessId(childHwnd, out int childProcessId);
                            if (childProcessId == targetProcessId)
                            {
                                allProcessWindows.Add(childHwnd);
                                MarsLoggerSimple.Info("FindParentWindow", $"Collected child window: {childHwnd}");
                            }
                            return true; // Continue enumeration
                        }, IntPtr.Zero);
                    }
                }

                // Now filter the collected windows based on criteria
                MarsLoggerSimple.Info("FindParentWindow", $"Collected {allProcessWindows.Count} windows from process {targetProcessId}");
                
                foreach (IntPtr hWnd in allProcessWindows)
                {
                    CheckWindowMatch(hWnd, targetProcessId, textPattern, controlIdStr, winClassPattern, 
                        nativeClassPattern, matchingWindows);
                }

                if (matchingWindows.Count == 0)
                {
                    strError = $"No matching windows found in process {targetProcessId}";
                    return false;
                }

                if (matchingWindows.Count > 1)
                {
                    if (string.IsNullOrEmpty(indexStr))
                    {
                        strError = $"Multiple matching windows found ({matchingWindows.Count}) but no index specified";
                        return false;
                    }

                    if (!int.TryParse(indexStr, out int index) || index < 0 || index >= matchingWindows.Count)
                    {
                        strError = $"Invalid index {indexStr}. Valid range: 0-{matchingWindows.Count - 1}";
                        return false;
                    }

                    parentHwnd = matchingWindows[index];
                }
                else
                {
                    parentHwnd = matchingWindows[0];
                }

                MarsLoggerSimple.Info("FindParentWindow", $"Found parent window: {parentHwnd}");
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Exception in FindParentWindow: {ex.Message}";
                MarsLoggerSimple.Error("FindParentWindow", strError);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("FindParentWindow");
            }
        }

        /// <summary>
        /// Collects top-level windows belonging to the target process
        /// </summary>
        /// <param name="hWnd">Window handle to check</param>
        /// <param name="targetProcessId">Target process ID</param>
        /// <param name="topLevelWindows">List to add top-level windows to</param>
        /// <returns>True to continue enumeration, false to stop</returns>
        private static bool CollectTopLevelProcessWindows(IntPtr hWnd, int targetProcessId, List<IntPtr> topLevelWindows)
        {
            try
            {
                // Check if the window belongs to the target process
                MarsWindowsAPIs.GetWindowThreadProcessId(hWnd, out int windowProcessId);
                if (windowProcessId == targetProcessId)
                {
                    topLevelWindows.Add(hWnd);
                    MarsLoggerSimple.Info("CollectTopLevelProcessWindows", $"Collected top-level window: {hWnd} (ProcessId: {windowProcessId})");
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("CollectTopLevelProcessWindows", $"Exception while collecting top-level window {hWnd}: {ex.Message}");
            }
            
            return true; // Continue enumeration
        }

        /// <summary>
        /// Checks if a window matches the specified criteria
        /// </summary>
        /// <param name="hWnd">Window handle to check</param>
        /// <param name="targetProcessId">Target process ID</param>
        /// <param name="textPattern">Text pattern to match</param>
        /// <param name="controlIdStr">Control ID string to match</param>
        /// <param name="winClassPattern">Window class pattern to match</param>
        /// <param name="nativeClassPattern">Native class pattern to match</param>
        /// <param name="matchingWindows">List to add matching windows to</param>
        /// <returns>True to continue enumeration, false to stop</returns>
        private static bool CheckWindowMatch(IntPtr hWnd, int targetProcessId, 
            string textPattern, string controlIdStr, 
            string winClassPattern, string nativeClassPattern, 
            List<IntPtr> matchingWindows)
        {
            try
            {
                // First, check if the window belongs to the target process
                // 这里的处理没有必要，因为所有的窗口已经是通过processid过来的
                int windowProcessId = targetProcessId;
                //MarsWindowsAPIs.GetWindowThreadProcessId(hWnd, out int windowProcessId);
                //if (windowProcessId != targetProcessId)
                //{
                //    return true; // Continue enumeration, skip this window
                //}

                bool matches = true;

                // Check text/title pattern if specified
                if (!string.IsNullOrEmpty(textPattern))
                {
                    int length = MarsWindowsAPIs.GetWindowTextLength(hWnd);
                    if (length > 0)
                    {
                        var sb = new System.Text.StringBuilder(length + 1);
                        MarsWindowsAPIs.GetWindowText(hWnd, sb, sb.Capacity);
                        string windowText = sb.ToString();
                        matches = MarsWindowsAPIsExtend.RegularTest(textPattern, windowText);
                        //if (!Regex.IsMatch(windowText, textPattern, RegexOptions.IgnoreCase))
                        //{
                        //    matches = false;
                        //}
                    }
                    else
                    {
                        matches = false;
                    }
                }

                // Check control ID if specified
                if (matches && !string.IsNullOrEmpty(controlIdStr))
                {
                    int windowControlId = MarsWindowsAPIs.GetDlgCtrlID(hWnd);
                    if (windowControlId != 0)
                    {
                        int expectedControlId = 0;
                        bool parseSuccess = false;
                        
                        // Support 0x prefix for hexadecimal integers
                        if (controlIdStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        {
                            parseSuccess = int.TryParse(controlIdStr.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out expectedControlId);
                        }
                        else
                        {
                            parseSuccess = int.TryParse(controlIdStr, out expectedControlId);
                        }
                        
                        if (parseSuccess && windowControlId != expectedControlId)
                        {
                            matches = false;
                        }
                        else if (!parseSuccess)
                        {
                            // If parsing fails, fall back to string comparison
                            if (windowControlId.ToString() != controlIdStr)
                            {
                                matches = false;
                            }
                        }
                    }
                    else
                    {
                        matches = false;
                    }
                }

                // Check winClass if specified
                if (matches && !string.IsNullOrEmpty(winClassPattern))
                {
                    var sb = new System.Text.StringBuilder(256);
                    int length = MarsWindowsAPIs.GetClassName(hWnd, sb, sb.Capacity);
                    if (length > 0)
                    {
                        string windowClass = sb.ToString();
                        if (!MarsWindowsAPIsExtend.RegularTest(winClassPattern, windowClass))
                        {
                            matches = false;
                        }
                    }
                    else
                    {
                        matches = false;
                    }
                }

                // Check nativeClass if specified (same handling as winClass)
                if (matches && !string.IsNullOrEmpty(nativeClassPattern))
                {
                    var sb = new System.Text.StringBuilder(256);
                    int length = MarsWindowsAPIs.GetClassName(hWnd, sb, sb.Capacity);
                    if (length > 0)
                    {
                        string windowClass = sb.ToString();
                        if (!MarsWindowsAPIsExtend.RegularTest(nativeClassPattern, windowClass))
                        {
                            matches = false;
                        }
                    }
                    else
                    {
                        matches = false;
                    }
                }

                if (matches)
                {
                    matchingWindows.Add(hWnd);
                    MarsLoggerSimple.Info("CheckWindowMatch", $"Found matching window: {hWnd} (ProcessId: {windowProcessId})");
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("CheckWindowMatch", $"Exception while checking window {hWnd}: {ex.Message}");
            }
            
            return true; // Continue enumeration
        }



        /// <summary>
        /// MSAA Pegwindow method - finds parent window for current test process
        /// </summary>
        /// <param name="stepId">Step ID</param>
        /// <param name="dictPegProperties">Peg window properties</param>
        /// <param name="dictObjProperties">Object properties</param>
        /// <param name="strParaMeter">Parameter string</param>
        /// <param name="strData">Data parameter</param>
        /// <param name="typeName">Object type name</param>
        /// <param name="strAttachInfo">Attachment info</param>
        /// <param name="pegWindName">Peg window name</param>
        /// <param name="objName">Object name</param>
        /// <param name="strError">Error message reference</param>
        /// <param name="dealResult">Deal result reference</param>
        /// <returns>True if parent window found, false otherwise</returns>
        public static bool MARSStandard_Pegwindow(long stepId, Dictionary<string, string> dictPegProperties, 
            Dictionary<string, string> dictObjProperties, string strParaMeter, string strData, string typeName, string strAttachInfo, 
            string pegWindName, string objName, ref string strError, ref MARSDealResult dealResult)
        {
            MarsLoggerSimple.logBegin("MARSStandard_Pegwindow", $"stepId: {stepId}|{pegWindName}.{objName}");

            try
            {
                // 1. 获得当前测试的MARSTestProcess.CurrentTestProcessId
                int currentProcessId = MARSTestProcess.CurrentTestProcessId;
                MarsLoggerSimple.Info("MARSStandard_Pegwindow", $"Current test process ID: {currentProcessId}");

                // 2. 调用FindParentWindow
                IntPtr parentHwnd = IntPtr.Zero;
                if (!FindParentWindow(dictPegProperties, ref parentHwnd, ref strError))
                {
                    MarsLoggerSimple.Error("MARSStandard_Pegwindow", $"{pegWindName}.{objName}|Failed to find parent window: {strError}");
                    return false;
                }

                // 3. 如果有当前的window，返回true，否则为false同时返回错误信息
                if (parentHwnd != IntPtr.Zero)
                {
                    MarsLoggerSimple.Info("MARSStandard_Pegwindow", $"Successfully found parent window: {parentHwnd}");
                    
                    // Set result
                    dealResult = new MARSDealResult
                    {
                        AckTime = DateTime.Now,
                        ResultMessage = "OK",
                        ActualInputData = strData
                    };
                    
                    return true;
                }
                else
                {
                    strError = "Parent window handle is invalid (IntPtr.Zero)";
                    MarsLoggerSimple.Error("MARSStandard_Pegwindow", $"{pegWindName}.{objName}|{strError}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                strError = $"Exception occurred in MARSStandard_Pegwindow: {ex.Message}";
                MarsLoggerSimple.Error("MARSStandard_Pegwindow", strError);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("MARSStandard_Pegwindow");
            }
        }

        public static bool MARSStandard_PressKey(long stepId, Dictionary<string, string> dictPegProperties,
            Dictionary<string, string> dictObjProperties, string strParaMeter, string strData, string typeName, string strAttachInfo,
            string pegWindName, string objName, ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("MARSStandard_PressKey", $"stepId: {stepId}|{pegWindName}.{objName}");
            try
            {
                // 1. 获得当前测试的MARSTestProcess.CurrentTestProcessId
                int currentProcessId = MARSTestProcess.CurrentTestProcessId;
                MarsLoggerSimple.Info("MARSStandard_PressKey", $"Current test process ID: {currentProcessId}");
                // 2. 调用FindParentWindow
                IntPtr parentHwnd = IntPtr.Zero;
                if (!FindParentWindow(dictPegProperties, ref parentHwnd, ref strError))
                {
                    MarsLoggerSimple.Error("MARSStandard_PressKey", $"{pegWindName}.{objName}|Failed to find parent window: {strError}");
                    return false;
                }

                // Step 2: Create MSAA interface for parent window
                using (var provider = new MarsAutoAccessibleSupportProvider())
                {
                    if (!provider.CreateAccessibleObject(parentHwnd, ref strError))
                    {
                        MarsLoggerSimple.Error("MARSStandard_PressKey", $"{pegWindName}.{objName}|Failed to create interface: {strError}");
                        return false;
                    }

                    MarsLoggerSimple.Info("MARSStandard_PressKey", "Successfully created MSAA interface for parent window");

                    // Step 3: Find target child object using MSAA
                    dynamic targetObject = null;
                    if (!provider.FindChildObject(dictObjProperties, parentHwnd, ref targetObject, ref strError))
                    {
                        MarsLoggerSimple.Error("MARSStandard_PressKey", $"{pegWindName}.{objName}|Failed to find target child object: {strError}");
                        return false;
                    }

                    MarsLoggerSimple.Info("MARSStandard_PressKey", "Successfully found target child object");

                    // Step 4: Parse action parameter and perform the action
                    if (!KeyPressOp.ParseAndExecuteAction(targetObject, typeName, strParaMeter, strData, ref strError, ref dealResult))
                    {
                        MarsLoggerSimple.Error("MARSStandard_PressKey", $"{pegWindName}.{objName}|Failed to execute action: {strError}");
                        return false;
                    }
                }

                MarsLoggerSimple.Info("MARSStandard_SearchAndClick", "Successfully executed MSAA action");
                return true;
            }
            catch (Exception e)
            {
                strError = e.Message;
                MarsLoggerSimple.Error("MARSStandard_PressKey", $"{iMark}|{strError}");
                dealResult.ErrorMessage = strError;
                dealResult.ResultMessage = $"FAILED:{strError}";
                dealResult.AckTime = DateTime.Now;
                dealResult.ActualInputData = strData;
                dealResult.StackInfo = Environment.StackTrace;
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("MARSStandard_PressKey", $"{iMark}");
            }
        }
    }
}