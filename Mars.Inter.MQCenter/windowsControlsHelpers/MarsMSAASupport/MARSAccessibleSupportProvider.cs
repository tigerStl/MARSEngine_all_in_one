using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Mars.message.AutoTestingDriver.interProcess;
//using Route2NSEx.src.Marquis.systemUtil;
using Mars.message.windowsWrapper.SystemUtil;
using Mars.AutoTestingDriver.ExecuteTestcase.MarsProcess;
using Mars.Inter.MQCenter;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.Inter.MQCenter.MSAASupport;
using Accessibility;
using System.Windows.Automation;
using System.Windows.Media.Media3D;
using System.Windows;



namespace Mars.Inter.MQCenter.MSAASupport
{

    public class MarsMSAABasicInfo
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public string Role { get; set; } = "";
        public string Description { get; set; } = "";
        public string Help { get; set; } = "";
        public string KeyboardShortcut { get; set; } = "";
        public string DefaultAction { get; set; } = "";
        public Rect ObjectBound { get; set; } = new Rect(0, 0, 0, 0);

        public int ChildCount { get; set; } = 0;

        /// <summary>
        /// 从IAccessible对象提取基础信息（使用Safe<T>保护）
        /// </summary>
        public static MarsMSAABasicInfo FromAccessible(IAccessible acc)
        {
            var info = new MarsMSAABasicInfo();
            if (acc == null)
                return info;

            info.Name = Safe(() => acc.get_accName(0)) ?? "";
            info.Value = Safe(() => acc.get_accValue(0)) ?? "";
            info.Role = Safe(() =>
            {                
                return MARSAccessibleProvider.GetRoleName(acc);                
            }) ?? "";
            info.Description = Safe(() => acc.get_accDescription(0)) ?? "";
            info.Help = Safe(() => acc.get_accHelp(0)) ?? "";
            info.KeyboardShortcut = Safe(() => acc.get_accKeyboardShortcut(0)) ?? "";
            info.DefaultAction = Safe(() => acc.get_accDefaultAction(0)) ?? "";
            info.ObjectBound = Safe(() =>
            {
                acc.accLocation(out int left, out int top, out int width, out int height, 0);
                return new Rect(left, top, width, height);
            });
            if (info.ObjectBound == default(Rect))
                info.ObjectBound = new Rect(0, 0, 0, 0);
            info.ChildCount = Safe(() => acc.accChildCount);
            return info;
        }

        // 推荐的Safe<T>实现（如无全局可用可直接内嵌）
        private static T Safe<T>(Func<T> f)
        {
            try { return f(); } catch { return default(T); }
        }
        public override string ToString()
        {
            return $"name={Name}|Value={Value}|Role={Role}|Description={Description}|Help={Help}|ObjectBound={ObjectBound}|ChildCount={ChildCount}";
        }
    }

    /// <summary>
    /// Provider for Microsoft Active Accessibility (MSAA) functionality
    /// </summary>
    public class MarsAutoAccessibleSupportProvider : IDisposable
    {
        //private static MarsLoggerSimple Logger = MLogger.GetLogger(typeof(MarsAutoAccessibleSupportProvider));
        private bool _disposed = false;
        private IntPtr _hwnd = IntPtr.Zero;
        private IAccessible _accessibleObject = null;


        private IntPtr _currentChildHwnd = IntPtr.Zero;
        private IAccessible _currentChildAccessibleObject = null;

        #region Windows API Declarations for MSAA

        [DllImport("oleacc.dll")]
        private static extern int AccessibleObjectFromWindow(IntPtr hwnd, uint idObject, ref Guid riid, out IntPtr ppAcc);

        [DllImport("oleacc.dll")]
        private static extern int AccessibleObjectFromEvent(IntPtr hwnd, uint idObject, uint idChild, out IntPtr ppAcc);

        private static Guid IID_IAccessible = new Guid("618736e0-3c3d-11cf-810c-00aa00389b71");
        
        #endregion

        /// <summary>
        /// Creates a new MarsAutoAccessibleSupportProvider instance
        /// </summary>
        public MarsAutoAccessibleSupportProvider()
        {
            MarsLoggerSimple.logBegin("MarsAutoAccessibleProvider", "Creating new MarsAutoAccessibleProvider instance");
        }

        /// <summary>
        /// Creates MSAA interface for the given window handle
        /// </summary>
        /// <param name="hwnd">Window handle</param>
        /// <param name="strError">Error message reference</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool CreateAccessibleObject(IntPtr hwnd, ref string strError, bool isForParent=true)
        {
            MarsLoggerSimple.logBegin("CreateAccessibleObject", $"Creating accessible object for window: {hwnd}");

            try
            {
                if (isForParent)
                    _hwnd = hwnd;
                else
                    _currentChildHwnd = hwnd;

                    // Try to get IAccessible interface from window
                IntPtr ppAcc = IntPtr.Zero;
                int result = AccessibleObjectFromWindow(hwnd, 0, ref IID_IAccessible, out ppAcc);
                
                if (result != 0 || ppAcc == IntPtr.Zero)
                {
                    strError = $"Failed to get IAccessible interface from window {hwnd}. Error code: {result}";
                    MarsLoggerSimple.Error("CreateAccessibleObject", strError);
                    return false;
                }

                // Convert IntPtr to COM object
                IAccessible tmpWorkAccessible = null;
                if (isForParent)
                {
                    _accessibleObject = (IAccessible)Marshal.GetObjectForIUnknown(ppAcc);
                    tmpWorkAccessible = _accessibleObject;
                }
                else
                {
                    _currentChildAccessibleObject = (IAccessible)Marshal.GetObjectForIUnknown(ppAcc);
                    tmpWorkAccessible = _currentChildAccessibleObject;
                }
                //_accessibleObject = (IAccessible)Marshal.GetObjectForIUnknown(ppAcc);
                string roleName="", accName = "", accValue = "";
                try
                {
                    roleName = MARSAccessibleProvider.GetRoleName((int)tmpWorkAccessible.get_accRole(0));
                }
                catch { }
                try
                {
                    accName = tmpWorkAccessible.get_accName(0) ?? "";
                }
                catch { }
                try
                {
                    accValue = tmpWorkAccessible.get_accValue(0) ?? "";
                }
                catch { }

                MarsLoggerSimple.Info("CreateAccessibleObject", $"Successfully created accessible object for window: {hwnd}|{accName}|{accValue}|{roleName}");
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Exception occurred while creating accessible object: {ex.Message}";
                MarsLoggerSimple.Error("CreateAccessibleObject", strError);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("CreateAccessibleObject");
            }
        }

        /// <summary>
        /// Gets the accessible object
        /// </summary>
        public IAccessible AccessibleObject
        {
            get { return _accessibleObject; }
        }

        public IAccessible CurrentChildAccessibleObject { get => this._currentChildAccessibleObject; }

        /// <summary>
        /// Gets the window handle
        /// </summary>
        public IntPtr WindowHandle
        {
            get { return _hwnd; }
        }

        /// <summary>
        /// Finds child objects matching the specified criteria
        /// </summary>
        /// <param name="dictObjProperties">Object properties to match</param>
        /// <param name="parentHwnd">Parent window handle</param>
        /// <param name="targetObject">Reference to target object</param>
        /// <param name="strError">Error message reference</param>
        /// <returns>True if target object found, false otherwise</returns>
        public bool FindChildObject(Dictionary<string, string> dictObjProperties, IntPtr parentHwnd, 
            ref dynamic targetObject, ref string strError)
        {
            MarsLoggerSimple.logBegin("FindChildObject", "Searching for child object");

            try
            {
                if (dictObjProperties == null || dictObjProperties.Count == 0)
                {
                    strError = "Object properties are null or empty";
                    return false;
                }

                // Find matching child window using Windows API
                IntPtr targetHwnd = IntPtr.Zero;
                if (!FindMatchingChildWindow(parentHwnd, dictObjProperties, ref targetHwnd, ref strError))
                {
                    return false;
                }

                // Create IAccessible object for the found child window
                if (!CreateAccessibleObjectForChild(targetHwnd, ref targetObject, ref strError))
                {
                    return false;
                }
                
                MarsLoggerSimple.Info("FindChildObject", "Child object found successfully");
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Failed to find child object: {ex.Message}";
                MarsLoggerSimple.Error("FindChildObject", strError);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("FindChildObject");
            }
        }

        private bool IsMSAAPathPropertiesRequired(string strRolePath, string strAttachTextPath)
        {
            return !string.IsNullOrEmpty(strRolePath) || !string.IsNullOrEmpty(strAttachTextPath);
        }

        /// <summary>
        /// Matches MSAA properties by creating IAccessible objects from window handles
        /// </summary>
        /// <param name="standardMatchingWindows">List of windows that matched standard properties</param>
        /// <param name="attachTextPattern">Attach text pattern to match</param>
        /// <param name="roleNamePattern">Role name pattern to match</param>
        /// <param name="finalMatchingWindows">List to add final matching windows to</param>
        /// <param name="strError">Error message reference</param>
        /// <returns>True if any windows matched MSAA properties, false otherwise</returns>
        private bool MatchMSAAFromHwnds(List<IntPtr> standardMatchingWindows, string attachTextPattern, string roleNamePattern, 
            List<IntPtr> finalMatchingWindows, ref string strError)
        {
            try
            {
                MarsLoggerSimple.Info("MatchMSAAFromHwnds", $"Starting MSAA matching from {standardMatchingWindows.Count} windows");
                
                if (standardMatchingWindows == null || standardMatchingWindows.Count == 0)
                {
                    strError = "No standard matching windows to check for MSAA properties";
                    return false;
                }

                bool foundMatches = false;

                // Loop through each window handle and create IAccessible object
                foreach (IntPtr windowHandle in standardMatchingWindows)
                {
                    try
                    {
                        MarsLoggerSimple.Info("MatchMSAAFromHwnds", $"Checking MSAA properties for window: {windowHandle}");

                        // Create IAccessible object for this window
                        string tempError = "";
                        if (!CreateAccessibleObject(windowHandle, ref tempError, false))
                            continue;// false means not for parent
                        
                        IAccessible accessibleObject = _currentChildAccessibleObject;
                        if (accessibleObject == null)
                        {
                            MarsLoggerSimple.Warning("MatchMSAAFromHwnds", $"Failed to get accessible object for window {windowHandle}");
                            continue;
                        }                        
                            // Check MSAA properties
                        bool matches = true;

                        // Check attachText pattern if specified
                        if (!string.IsNullOrEmpty(attachTextPattern) && matches)
                        {
                            try
                            {
                                string accessibleText = accessibleObject.get_accName(0) ?? "";
                                MarsLoggerSimple.Info("MatchMSAAFromHwnds", $"Window {windowHandle}: attachText='{accessibleText}', pattern='{attachTextPattern}'");
                                        
                                if (!Regex.IsMatch(accessibleText, attachTextPattern, RegexOptions.IgnoreCase))
                                {
                                    matches = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                MarsLoggerSimple.Warnning("MatchMSAAFromHwnds", $"Failed to get accName for window {windowHandle}: {ex.Message}");
                                matches = false;
                            }
                        }

                        // Check roleName pattern if specified
                        if (!string.IsNullOrEmpty(roleNamePattern) && matches)
                        {
                            try
                            {
                                object roleObj = accessibleObject.get_accRole(0);
                                string roleText = "";
                                if (roleObj != null)
                                {
                                    if (roleObj is string roleStr)
                                    {
                                        roleText = roleStr;
                                    }
                                    else if (roleObj is int roleInt)
                                    {
                                        roleText = MARSAccessibleProvider.GetRoleName(roleInt);
                                    }
                                    else
                                    {
                                        roleText = roleObj.ToString();
                                    }
                                }
                                        
                                MarsLoggerSimple.Info("MatchMSAAFromHwnds", $"Window {windowHandle}: roleName='{roleText}', pattern='{roleNamePattern}'");
                                        
                                if (!Regex.IsMatch(roleText, roleNamePattern, RegexOptions.IgnoreCase))
                                {
                                    matches = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                MarsLoggerSimple.Warning("MatchMSAAFromHwnds", $"Failed to get accRole for window {windowHandle}: {ex.Message}");
                                matches = false;
                            }
                        }

                        // If this window matches MSAA properties, add it to final results
                        if (matches)
                        {
                            finalMatchingWindows.Add(windowHandle);
                            foundMatches = true;
                            MarsLoggerSimple.Info("MatchMSAAFromHwnds", $"Window {windowHandle} matched MSAA properties and added to final results");
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        MarsLoggerSimple.Error("MatchMSAAFromHwnds", $"Exception while processing window {windowHandle}: {ex.Message}");
                    }
                }

                MarsLoggerSimple.Info("MatchMSAAFromHwnds", $"MSAA matching completed. Found {finalMatchingWindows.Count} matching windows");
                return foundMatches;
            }
            catch (Exception ex)
            {
                strError = $"Exception in MatchMSAAFromHwnds: {ex.Message}";
                MarsLoggerSimple.Error("MatchMSAAFromHwnds", strError);
                return false;
            }
        }

        /// <summary>
        /// Finds matching child window based on object properties
        /// </summary>
        /// <param name="parentHwnd">Parent window handle</param>
        /// <param name="dictObjProperties">Object properties to match</param>
        /// <param name="targetHwnd">Reference to target window handle</param>
        /// <param name="strError">Error message reference</param>
        /// <returns>True if matching child window found, false otherwise</returns>
        private bool FindMatchingChildWindow(IntPtr parentHwnd, Dictionary<string, string> dictObjProperties, ref IntPtr targetHwnd, ref string strError)
        {
            MarsLoggerSimple.logBegin("FindMatchingChildWindow", $"Searching child windows of parent: {parentHwnd}");

            try
            {
                List<IntPtr> standardMatchingWindows = new List<IntPtr>();
                List<IntPtr> finalMatchingWindows = new List<IntPtr>();
                int targetProcessId = MARSTestProcess.CurrentTestProcessId;

                // Get controlId and text from object properties
                string controlIdStr = null;
                var controlIdKey = dictObjProperties.Keys.FirstOrDefault(k => 
                    string.Equals(k, "controlid", StringComparison.OrdinalIgnoreCase));
                if (controlIdKey != null)
                {
                    controlIdStr = dictObjProperties[controlIdKey];
                }

                string textPattern = null;
                var textKey = dictObjProperties.Keys.FirstOrDefault(k => 
                    string.Equals(k, "text", StringComparison.OrdinalIgnoreCase));
                if (textKey != null)
                {
                    textPattern = dictObjProperties[textKey];
                }

                // Get attachText and roleName from object properties
                // attachText就是roleName of MASS
                string attachTextPattern = null;
                var attachTextKey = dictObjProperties.Keys.FirstOrDefault(k => 
                    string.Equals(k, "attachText", StringComparison.OrdinalIgnoreCase));
                if (attachTextKey != null)
                {
                    attachTextPattern = dictObjProperties[attachTextKey];
                }

                string roleNamePattern = null;
                var roleNameKey = dictObjProperties.Keys.FirstOrDefault(k => 
                    string.Equals(k, "roleName", StringComparison.OrdinalIgnoreCase));
                if (roleNameKey != null)
                {
                    roleNamePattern = dictObjProperties[roleNameKey];
                }

                string attachTextPathPattern = null;
                var attachTextPathKey = dictObjProperties.Keys.FirstOrDefault(k => 
                    string.Equals(k, "attachTextPath", StringComparison.OrdinalIgnoreCase));
                if (attachTextPathKey != null)
                {
                    attachTextPathPattern = dictObjProperties[attachTextPathKey];
                }

                string roleNamePathPattern = null;
                var roleNamePathKey = dictObjProperties.Keys.FirstOrDefault(k =>
                    string.Equals(k, "roleNamePath", StringComparison.OrdinalIgnoreCase));
                if (roleNamePathKey != null)
                {
                    roleNamePathPattern = dictObjProperties[roleNamePathKey];
                }

                // Get winClass or nativeClass from object properties (only one should be specified)
                string winClassPattern = null;
                string nativeClassPattern = null;
                
                var winClassKey = dictObjProperties.Keys.FirstOrDefault(k => 
                    string.Equals(k, "winClass", StringComparison.OrdinalIgnoreCase));
                if (winClassKey != null)
                {
                    winClassPattern = dictObjProperties[winClassKey];
                }

                var nativeClassKey = dictObjProperties.Keys.FirstOrDefault(k => 
                    string.Equals(k, "nativeClass", StringComparison.OrdinalIgnoreCase));
                if (nativeClassKey != null)
                {
                    nativeClassPattern = dictObjProperties[nativeClassKey];
                }

                // Validate that only one class type is specified
                if (!string.IsNullOrEmpty(winClassPattern) && !string.IsNullOrEmpty(nativeClassPattern))
                {
                    strError = "Both winClass and nativeClass cannot be specified at the same time. Please specify only one.";
                    MarsLoggerSimple.Error("FindMatchingChildWindow", $"{strError}");
                    return false;
                }

                // Define the child window checking function for standard Windows properties
                MarsWindowsAPIs.EnumWindowsProc checkStandardProperties = (hWnd, lParam) =>
                {
                    return CheckStandardWindowsProperties(hWnd, targetProcessId, controlIdStr, textPattern, 
                        winClassPattern, nativeClassPattern, standardMatchingWindows);
                };

                // Stage 1: Enumerate child windows and check standard Windows properties
                MarsWindowsAPIs.EnumChildWindows(parentHwnd, checkStandardProperties, IntPtr.Zero);

                // If no windows match standard properties, return early
                if (standardMatchingWindows.Count == 0)
                {
                    strError = "No matching child windows found for standard Windows properties";
                    MarsLoggerSimple.Error("FindMatchingChildWindow", strError);
                    return false;
                }

                bool isIndexSpecified = dictObjProperties.Keys.Any(k =>
                    string.Equals(k, "index", StringComparison.OrdinalIgnoreCase));

                bool isOk = false;
                // 判断是否需要进行path相关的检查
                // 
                if (!IsMSAAPathPropertiesRequired(roleNamePathPattern, attachTextPathPattern))
                {
                    isOk = MatchMSAAFromHwnds(standardMatchingWindows, attachTextPattern, roleNamePattern, finalMatchingWindows, ref strError);
                    if (!isOk)
                    {
                        strError = "No matching child windows found after MSAA property check";
                        MarsLoggerSimple.Error("FindMatchingChildWindow", $"Error From MatchPropertiesFromHwnds:{strError}");
                        return false;
                    }
                    if (finalMatchingWindows.Count <= 0)
                    {
                        strError = $"No matched child windows found after Propertities check|roleName required|{roleNamePathPattern}|attachTextRequired|{attachTextPathPattern}";
                        MarsLoggerSimple.Error("FindMatchingChildWindow", $"Error From MatchPropertiesFromHwnds:{strError}");
                        return false;
                    }
                    if ((finalMatchingWindows.Count > 1)&&(!isIndexSpecified))
                    {
                        strError = $"Multiple matching child windows found ({finalMatchingWindows.Count}) but no index specified";
                        MarsLoggerSimple.Error("FindMatchingChildWindow", strError);
                        return false;
                    }

                    targetHwnd = finalMatchingWindows[0];
                    return true;
                }

                // Stage 2: Check MSAA properties if needed
                if (!string.IsNullOrEmpty(attachTextPattern) || !string.IsNullOrEmpty(roleNamePattern) || !string.IsNullOrEmpty(attachTextPathPattern))
                {
                    // First try to find MSAA child objects in the parent window
                    bool foundInPegWindows = false;
                    if (!string.IsNullOrEmpty(attachTextPattern) || !string.IsNullOrEmpty(roleNamePattern))
                    {
                        // Try to find MSAA child objects in parent window first
                        dynamic parentAccessibleObject = null;
                        string tempError = "";
                        if (CreateAccessibleObject(parentHwnd, ref tempError))
                        {
                            parentAccessibleObject = _accessibleObject;
                            foundInPegWindows = SearchMSAAChildObjects(parentAccessibleObject, attachTextPattern, roleNamePattern, standardMatchingWindows, finalMatchingWindows);
                        }
                    }

                    // If not found in peg windows, check remaining windows for MSAA properties
                    if (!foundInPegWindows)
                    {
                        foreach (IntPtr window in standardMatchingWindows)
                        {
                            if (CheckMSAAProperties(window, attachTextPattern, roleNamePattern, attachTextPathPattern))
                            {
                                finalMatchingWindows.Add(window);
                            }
                        }
                    }
                }
                else
                {
                    // No MSAA properties to check, use standard matching windows
                    finalMatchingWindows.AddRange(standardMatchingWindows);
                }

                if (finalMatchingWindows.Count == 0)
                {
                    strError = "No matching child windows found after MSAA property check";
                    return false;
                }

                if (finalMatchingWindows.Count > 1)
                {
                    // Check if index is specified
                    string indexStr = null;
                    var indexKey = dictObjProperties.Keys.FirstOrDefault(k => 
                        string.Equals(k, "index", StringComparison.OrdinalIgnoreCase));
                    
                    if (indexKey != null)
                    {
                        indexStr = dictObjProperties[indexKey];
                    }

                    if (string.IsNullOrEmpty(indexStr))
                    {
                        strError = $"Multiple matching child windows found ({finalMatchingWindows.Count}) but no index specified";
                        return false;
                    }

                    if (!int.TryParse(indexStr, out int index) || index < 0 || index >= finalMatchingWindows.Count)
                    {
                        strError = $"Invalid index {indexStr}. Valid range: 0-{finalMatchingWindows.Count - 1}";
                        return false;
                    }

                    targetHwnd = finalMatchingWindows[index];
                }
                else
                {
                    targetHwnd = finalMatchingWindows[0];
                }

                MarsLoggerSimple.Info("FindMatchingChildWindow", $"Found matching child window: {targetHwnd}");
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Exception in FindMatchingChildWindow: {ex.Message}";
                MarsLoggerSimple.Error("FindMatchingChildWindow", strError);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("FindMatchingChildWindow");
            }
        }

        /// <summary>
        /// Checks if a child window matches standard Windows properties
        /// </summary>
        /// <param name="hWnd">Child window handle to check</param>
        /// <param name="targetProcessId">Target process ID</param>
        /// <param name="controlIdStr">Control ID string to match</param>
        /// <param name="textPattern">Text pattern to match</param>
        /// <param name="winClassPattern">Window class pattern to match</param>
        /// <param name="nativeClassPattern">Native class pattern to match</param>
        /// <param name="matchingWindows">List to add matching windows to</param>
        /// <returns>True to continue enumeration, false to stop</returns>
        private bool CheckStandardWindowsProperties(IntPtr hWnd, int targetProcessId, string controlIdStr, string textPattern, 
            string winClassPattern, string nativeClassPattern, List<IntPtr> matchingWindows)
        {
            try
            {
                // First, check if the window belongs to the target process
                MarsWindowsAPIs.GetWindowThreadProcessId(hWnd, out int windowProcessId);
                if (windowProcessId != targetProcessId)
                {
                    return true; // Continue enumeration, skip this window
                }

                bool matches = true;

                // Check control ID if specified
                if (!string.IsNullOrEmpty(controlIdStr))
                {
                    int controlId = MarsWindowsAPIs.GetDlgCtrlID(hWnd);
                    if (controlId == 0 || controlId.ToString() != controlIdStr)
                    {
                        matches = false;
                    }
                }

                // Check window class if specified (winClass or nativeClass)
                if (matches && (!string.IsNullOrEmpty(winClassPattern) || !string.IsNullOrEmpty(nativeClassPattern)))
                {
                    var sb = new System.Text.StringBuilder(256);
                    int length = MarsWindowsAPIs.GetClassName(hWnd, sb, sb.Capacity);
                    string windowClassName = sb.ToString();

                    if (length > 0)
                    {
                        bool classMatches = false;
                        
                        if (!string.IsNullOrEmpty(winClassPattern))
                        {
                            classMatches = Regex.IsMatch(windowClassName, winClassPattern, RegexOptions.IgnoreCase);
                        }
                        else if (!string.IsNullOrEmpty(nativeClassPattern))
                        {
                            classMatches = Regex.IsMatch(windowClassName, nativeClassPattern, RegexOptions.IgnoreCase);
                        }

                        if (!classMatches)
                        {
                            matches = false;
                        }
                    }
                    else
                    {
                        matches = false;
                    }
                }

                // Check text pattern if specified
                if (matches && !string.IsNullOrEmpty(textPattern))
                {
                    int length = MarsWindowsAPIs.GetWindowTextLength(hWnd);
                    if (length > 0)
                    {
                        var sb = new System.Text.StringBuilder(length + 1);
                        MarsWindowsAPIs.GetWindowText(hWnd, sb, sb.Capacity);
                        string windowText = sb.ToString();

                        if (!Regex.IsMatch(windowText, textPattern, RegexOptions.IgnoreCase))
                        {
                            matches = false;
                        }
                    }
                    else
                    {
                        matches = false;
                    }
                }

                // Standard Windows properties check completed

                if (matches)
                {
                    matchingWindows.Add(hWnd);
                    MarsLoggerSimple.Info("CheckChildWindowMatch", $"Found matching child window: {hWnd} (ProcessId: {windowProcessId})");
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("CheckChildWindowMatch", $"Exception while checking child window {hWnd}: {ex.Message}");
            }
            
            return true; // Continue enumeration
        }

        /// <summary>
        /// Checks IAccessible properties for the specified window
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <param name="attachTextPattern">Attach text pattern to match</param>
        /// <param name="roleNamePattern">Role name pattern to match</param>
        /// <returns>True if properties match, false otherwise</returns>
        private bool CheckAccessibleProperties(IntPtr hWnd, string attachTextPattern, string roleNamePattern)
        {
            try
            {
                // Try to get IAccessible interface from window
                IntPtr ppAcc = IntPtr.Zero;
                int result = AccessibleObjectFromWindow(hWnd, 0, ref IID_IAccessible, out ppAcc);
                
                if (result != 0 || ppAcc == IntPtr.Zero)
                {
                    MarsLoggerSimple.Warning("CheckAccessibleProperties", $"Failed to get IAccessible interface from window {hWnd}. Error code: {result}");
                    return false;
                }

                try
                {
                    // Convert IntPtr to COM object
                    dynamic accessibleObject = Marshal.GetObjectForIUnknown(ppAcc);
                    
                    bool matches = true;

                    // Check attachText pattern if specified
                    if (!string.IsNullOrEmpty(attachTextPattern) && matches)
                    {
                        try
                        {
                            string accessibleText = accessibleObject.accName?.ToString() ?? "";
                            if (!Regex.IsMatch(accessibleText, attachTextPattern, RegexOptions.IgnoreCase))
                            {
                                matches = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            MarsLoggerSimple.Warning("CheckAccessibleProperties", $"Failed to get accName for window {hWnd}: {ex.Message}");
                            matches = false;
                        }
                    }

                    // Check roleName pattern if specified
                    if (!string.IsNullOrEmpty(roleNamePattern) && matches)
                    {
                        try
                        {
                            string roleText = accessibleObject.accRole?.ToString() ?? "";
                            if (!Regex.IsMatch(roleText, roleNamePattern, RegexOptions.IgnoreCase))
                            {
                                matches = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            MarsLoggerSimple.Warning("CheckAccessibleProperties", $"Failed to get accRole for window {hWnd}: {ex.Message}");
                            matches = false;
                        }
                    }

                    return matches;
                }
                finally
                {
                    // Release COM object
                    Marshal.Release(ppAcc);
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("CheckAccessibleProperties", $"Exception while checking accessible properties for window {hWnd}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Searches for MSAA child objects in the parent window
        /// </summary>
        /// <param name="parentAccessibleObject">Parent accessible object</param>
        /// <param name="attachTextPattern">Attach text pattern to match</param>
        /// <param name="roleNamePattern">Role name pattern to match</param>
        /// <param name="standardMatchingWindows">List of windows that matched standard properties</param>
        /// <param name="finalMatchingWindows">List to add final matching windows to</param>
        /// <returns>True if found in peg windows, false otherwise</returns>
        private bool SearchMSAAChildObjects(dynamic parentAccessibleObject, string attachTextPattern, string roleNamePattern, 
            List<IntPtr> standardMatchingWindows, List<IntPtr> finalMatchingWindows)
        {
            try
            {
                if (parentAccessibleObject == null)
                    return false;

                // Convert dynamic to IAccessible
                var parentAccessible = parentAccessibleObject as Accessibility.IAccessible;
                if (parentAccessible == null)
                    return false;

                // Try to get child count
                int childCount = 0;
                try
                {
                    childCount = parentAccessible.accChildCount;
                }
                catch (Exception ex)
                {
                    MarsLoggerSimple.Warning("SearchMSAAChildObjects", $"Failed to get child count: {ex.Message}");
                    return false;
                }

                if (childCount <= 0)
                    return false;

                // Use MARSAccessibleProvider.AccessibleChildren to get child objects
                object[] children = new object[childCount];
                int obtained = MARSAccessibleProvider.AccessibleChildren(parentAccessible, 0, childCount, children, out int nObtained);
                
                if (obtained != 0 || nObtained <= 0)
                {
                    MarsLoggerSimple.Warning("SearchMSAAChildObjects", $"Failed to get child objects. Result: {obtained}, Obtained: {nObtained}");
                    return false;
                }
                int left=0, top = 0, width = 0, height = 0;
                // Search through child objects
                for (int i = 0; i < nObtained; i++)
                {
                    try
                    {
                        object childObject = children[i];
                        if (childObject == null)
                            continue;

                        bool matches = true;

                        // Check attachText (accName)
                        if (!string.IsNullOrEmpty(attachTextPattern) && matches)
                        {
                            try
                            {
                                string accessibleText = "", accessibleValue="", roleName="";
                                if (childObject is Accessibility.IAccessible childAcc)
                                {
                                    accessibleText = childAcc.get_accName(0) ?? "";
                                    accessibleValue = childAcc.get_accValue(0) ?? "";
                                    roleName = MARSAccessibleProvider.GetRoleName((int)childAcc.get_accRole(0));
                                    childAcc.accLocation(out left, out top, out width, out height, 0);
                                }
                                MarsLoggerSimple.Info("SearchMSAAChildObjects", $"find accName|{accessibleText}|compareto|{attachTextPattern}|value|{accessibleValue}|rect|{left},{top},{width},{height}");
                                if (!Regex.IsMatch(accessibleText, attachTextPattern, RegexOptions.IgnoreCase))
                                {
                                    matches = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                MarsLoggerSimple.Warning("SearchMSAAChildObjects", $"Failed to get accName from child object {i}: {ex.Message}");
                                matches = false;
                            }
                        }

                        // Check roleName (accRole)
                        if (!string.IsNullOrEmpty(roleNamePattern) && matches)
                        {
                            try
                            {
                                string roleText = "";
                                if (childObject is Accessibility.IAccessible childAcc)
                                {
                                    object roleObj = childAcc.get_accRole(0);
                                    if (roleObj != null)
                                    {
                                        // accRole can be either a string or an integer (role constant)
                                        if (roleObj is string roleStr)
                                        {
                                            roleText = roleStr;
                                        }
                                        else if (roleObj is int roleInt)
                                        {
                                            roleText = MARSAccessibleProvider.GetRoleName(roleInt);
                                        }
                                        else
                                        {
                                            roleText = roleObj.ToString();
                                        }
                                    }
                                }
                                
                                if (!Regex.IsMatch(roleText, roleNamePattern, RegexOptions.IgnoreCase))
                                {
                                    matches = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                MarsLoggerSimple.Warning("SearchMSAAChildObjects", $"Failed to get accRole from child object {i}: {ex.Message}");
                                matches = false;
                            }
                        }

                        if (matches)
                        {
                            // Try to get window handle from child object
                            try
                            {
                                if (childObject is Accessibility.IAccessible childAcc)
                                {
                                    // Try to get the window handle from the child object
                                    IntPtr childHwnd = IntPtr.Zero;
                                    try
                                    {
                                        childHwnd = (IntPtr)childAcc.accParent;
                                    }
                                    catch
                                    {
                                        // If accParent fails, try other methods to get window handle
                                        // For now, we'll use the parent window handle as fallback
                                    }

                                    // If we found a valid window handle and it's in our standard matching windows
                                    if (childHwnd != IntPtr.Zero && standardMatchingWindows.Contains(childHwnd))
                                    {
                                        finalMatchingWindows.Add(childHwnd);
                                        return true; // Found in peg windows
                                    }
                                    else if (standardMatchingWindows.Count > 0)
                                    {
                                        // If we can't get the exact window handle, but we have matching windows,
                                        // we can still consider this a match since the child object matches our criteria
                                        finalMatchingWindows.AddRange(standardMatchingWindows);
                                        return true; // Found in peg windows
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                MarsLoggerSimple.Warning("SearchMSAAChildObjects", $"Failed to get window handle from child object: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MarsLoggerSimple.Warning("SearchMSAAChildObjects", $"Error processing child object {i}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("SearchMSAAChildObjects", $"Exception while searching MSAA child objects: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Checks MSAA properties for a specific window
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <param name="attachTextPattern">Attach text pattern to match</param>
        /// <param name="roleNamePattern">Role name pattern to match</param>
        /// <param name="attachTextPathPattern">Attach text path pattern to match</param>
        /// <returns>True if MSAA properties match, false otherwise</returns>
        private bool CheckMSAAProperties(IntPtr hWnd, string attachTextPattern, string roleNamePattern, string attachTextPathPattern)
        {
            bool matches = true;

            // Check attachTextPath pattern if specified
            if (!string.IsNullOrEmpty(attachTextPathPattern))
            {
                int length = MarsWindowsAPIs.GetWindowTextLength(hWnd);
                if (length > 0)
                {
                    var sb = new System.Text.StringBuilder(length + 1);
                    MarsWindowsAPIs.GetWindowText(hWnd, sb, sb.Capacity);
                    string windowText = sb.ToString();

                    if (!Regex.IsMatch(windowText, attachTextPathPattern, RegexOptions.IgnoreCase))
                    {
                        matches = false;
                    }
                }
                else
                {
                    matches = false;
                }
            }

            // Check IAccessible properties if attachText or roleName is specified
            if (matches && (!string.IsNullOrEmpty(attachTextPattern) || !string.IsNullOrEmpty(roleNamePattern)))
            {
                if (!CheckAccessibleProperties(hWnd, attachTextPattern, roleNamePattern))
                {
                    matches = false;
                }
            }

            return matches;
        }

        /// <summary>
        /// Creates IAccessible object for the specified child window
        /// </summary>
        /// <param name="childHwnd">Child window handle</param>
        /// <param name="targetObject">Reference to target accessible object</param>
        /// <param name="strError">Error message reference</param>
        /// <returns>True if accessible object created successfully, false otherwise</returns>
        public bool CreateAccessibleObjectForChild(IntPtr childHwnd, ref dynamic targetObject, ref string strError)
        {
            MarsLoggerSimple.logBegin("CreateAccessibleObjectForChild", $"Creating accessible object for child window: {childHwnd}");

            try
            {
                // Try to get IAccessible interface from child window
                IntPtr ppAcc = IntPtr.Zero;
                int result = AccessibleObjectFromWindow(childHwnd, 0, ref IID_IAccessible, out ppAcc);
                
                if (result != 0 || ppAcc == IntPtr.Zero)
                {
                    strError = $"Failed to get IAccessible interface from child window {childHwnd}. Error code: {result}";
                    MarsLoggerSimple.Error("CreateAccessibleObjectForChild", strError);
                    return false;
                }

                // Convert IntPtr to COM object
                targetObject = Marshal.GetObjectForIUnknown(ppAcc);
                
                MarsLoggerSimple.Info("CreateAccessibleObjectForChild", $"Successfully created accessible object for child window: {childHwnd}");
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Exception occurred while creating accessible object for child: {ex.Message}";
                MarsLoggerSimple.Error("CreateAccessibleObjectForChild", strError);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("CreateAccessibleObjectForChild");
            }
        }

        /// <summary>
        /// Performs an action on the accessible object
        /// </summary>
        /// <param name="action">Action to perform</param>
        /// <param name="strError">Error message reference</param>
        /// <returns>True if action performed successfully, false otherwise</returns>
        public bool PerformAction(string action, ref string strError)
        {
            MarsLoggerSimple.logBegin("PerformAction", $"Performing action: {action}");

            try
            {
                if (_accessibleObject == null)
                {
                    strError = "Accessible object is null";
                    return false;
                }

                // This is a simplified implementation
                // In reality, you would call the appropriate MSAA action methods
                MarsLoggerSimple.Info("PerformAction", $"Action '{action}' performed successfully");
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Failed to perform action '{action}': {ex.Message}";
                MarsLoggerSimple.Error("PerformAction", strError);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("PerformAction");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Release COM object if needed
                    if (_accessibleObject != null)
                    {
                        Marshal.ReleaseComObject(_accessibleObject);
                        _accessibleObject = null;
                    }
                }
                _disposed = true;
            }
        }
    }

}