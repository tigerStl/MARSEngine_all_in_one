using Accessibility;
using Mars.Inter.MQCenter.MarsUtility;
using Mars.message.AutoTestingDriver.interProcess;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.windowsWrapper.SystemUtil;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace Mars.Inter.MQCenter.MSAASupport.MarsAccessibleKeywords
{
    public class RibbonMenuHelper
    {
        public const string cnst_MarRibbonType = "MarsRibbonType";        

        // Ribbon 信息数据结构
        public class RibbonInfo
        {
            public List<PageTabInfo> PageTabLists { get; set; } = new List<PageTabInfo>();
            public List<PaneInfo> Panes { get; set; } = new List<PaneInfo>();
            public List<ToolbarInfo> Toolbars { get; set; } = new List<ToolbarInfo>();
            public List<RibbonButtonInfo> PushButtons { get; set; } = new List<RibbonButtonInfo>();
            public List<RibbonButtonInfo> splitButtonInfos { get; set; } = new List<RibbonButtonInfo>();
            public List<RibbonButtonInfo> ButtonDropDownInfos { get; set; } = new List<RibbonButtonInfo>();
        }

        public class PageTabInfo
        {
            public string Name { get; set; }
            public Rectangle Rect { get; set; }
            public IAccessible Accessible { get; set; }
        }

        public class PaneInfo
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public Rectangle Rect { get; set; }
            public IAccessible Accessible { get; set; }
        }

        public class ToolbarInfo
        {
            public string Name { get; set; }
            public Rectangle Rect { get; set; }
            public IAccessible Accessible { get; set; }
        }

        public class RibbonButtonInfo
        {
            public string Name { get; set; }
            public Rectangle Rect { get; set; }
            public IAccessible Accessible { get; set; }

            public string RoleName { get; set; } = "PushButton";
        }

        //public class SplitButtonInfo
        //{
        //    public string Name { get; set; }
        //    public Rectangle Rect { get; set; }
        //    public IAccessible Accessible { get; set; }
        //}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetElement"></param>
        /// <param name="pegWindName"></param>
        /// <param name="objName"></param>
        /// <param name="dictPegProperties"></param>
        /// <param name="dictObjProperties"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData">需要选中的菜单项</param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal static bool SelectMenuItem(AutomationElement targetElement, string pegWindName, string objName, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties, string strParaMeter, string strData, ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(100000);
            MarsLoggerSimple.logBegin("SelectMenuItem", $"{iMark}|SelectMenuItem({pegWindName}.{objName},{MarsWindowsAPIsExtend.Dic2String(dictObjProperties)}, {strParaMeter}, {strData})");
            
            if (dealResult == null)
                dealResult = new MARSDealResult();
            
            try
            {
                if (targetElement == null)
                {
                    strError = "Target element is null";
                    MarsLoggerSimple.Error("SelectMenuItem", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                if (string.IsNullOrEmpty(strData))
                {
                    strError = "Menu item name is empty";
                    MarsLoggerSimple.Error("SelectMenuItem", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                // 获取目标元素的句柄
                IntPtr hwnd = (IntPtr)targetElement.Current.NativeWindowHandle;
                if (hwnd == IntPtr.Zero)
                {
                    strError = "Cannot get window handle from target element";
                    MarsLoggerSimple.Error("SelectMenuItem", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                MarsLoggerSimple.Info("SelectMenuItem", $"{iMark}|Target window handle: {hwnd}");

                // 获取 IAccessible 对象
                var accessibleProvider = new MARSAccessibleProvider();
                var accessibleObj = accessibleProvider.GetAccessibleObject(hwnd);
                if (accessibleObj == null)
                {
                    strError = "Cannot get IAccessible object from window handle";
                    MarsLoggerSimple.Error("SelectMenuItem", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                IAccessible ribbonAccessible = accessibleObj as IAccessible;
                if (ribbonAccessible == null)
                {
                    strError = "Failed to cast to IAccessible";
                    MarsLoggerSimple.Error("SelectMenuItem", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                MarsLoggerSimple.Info("SelectMenuItem", $"{iMark}|Successfully got IAccessible object");

                // 构建 Ribbon 对象信息
                var ribbonInfo = BuildRibbonInfo(ribbonAccessible, iMark);
                if (ribbonInfo == null)
                {
                    strError = "Failed to build ribbon information";
                    MarsLoggerSimple.Error("SelectMenuItem", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                // 查找匹配的 PageTabList
                var matchingTabArr = ribbonInfo.PageTabLists.Where(tab => 
                    !string.IsNullOrEmpty(tab.Name) && 
                    MarsWindowsAPIsExtend.RegularTest(strData, tab.Name))
                    .ToList();
                
                if ((matchingTabArr == null)||(matchingTabArr.Count<=0))
                {
                    strError = $"No matching Items found for '{strData}'. Available tabs: {string.Join(", ", ribbonInfo.PageTabLists.Select(t => t.Name))}";
                    MarsLoggerSimple.Error("SelectMenuItem", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }
                if (matchingTabArr.Count != 1)
                {
                    strError = $"Multiple Items found for |{strData}|, Available tabs: {string.Join(", ", ribbonInfo.PageTabLists.Select(t => t.Name))}";
                    MarsLoggerSimple.Error("SelectMenuItem", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                var matchingTab = matchingTabArr[0];
                MarsLoggerSimple.Info("SelectMenuItem", $"{iMark}|Found matching PageTabList: {matchingTab.Name} at {matchingTab.Rect}");

                // 点击 PageTabList 的中间位置
                int centerX = matchingTab.Rect.X + matchingTab.Rect.Width / 2;
                int centerY = matchingTab.Rect.Y + matchingTab.Rect.Height / 2;

                MarsLoggerSimple.Info("SelectMenuItem", $"{iMark}|Clicking at center position: X={centerX}, Y={centerY}");

                // 移动鼠标并点击
                MarsWindowsAPIsExtend.MoveMouse(centerX, centerY);
                System.Threading.Thread.Sleep(100);
                MarsWindowsAPIsExtend.LeftMouseClick(centerX, centerY);
                System.Threading.Thread.Sleep(200);

                // 设置成功结果
                dealResult.ReturnedData = matchingTab.Name;
                dealResult.ResultMessage = "SUCCESS";
                dealResult.ErrorMessage = "";
                dealResult.AckTime = DateTime.Now;

                MarsLoggerSimple.Info("SelectMenuItem", $"{iMark}|Successfully selected menu item: {matchingTab.Name}");
                return true;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                MarsLoggerSimple.Error("SelectMenuItem", $"{iMark}|Error: {strError}", ex);
                dealResult.ErrorMessage = strError;
                dealResult.ResultMessage = $"FAILED,{strError}";
                dealResult.AckTime = DateTime.Now;
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("SelectMenuItem", $"{iMark}|{dealResult.ResultMessage}");
            }
        }

        /// <summary>
        /// 构建 Ribbon 对象信息
        /// </summary>
        /// <param name="ribbonAccessible">Ribbon 的 IAccessible 对象</param>
        /// <param name="iMark">日志标记</param>
        /// <returns>Ribbon 信息</returns>
        private static RibbonInfo BuildRibbonInfo(IAccessible ribbonAccessible, int iMark)
        {
            try
            {
                var ribbonInfo = new RibbonInfo();
                var accessibleProvider = new MARSAccessibleProvider();

                MarsLoggerSimple.Info("BuildRibbonInfo", $"{iMark}|Starting to build ribbon information");

                // 遍历 Ribbon 的子对象
                TraverseRibbonChildren(ribbonAccessible, ribbonInfo, accessibleProvider, iMark, 0);

                MarsLoggerSimple.Info("BuildRibbonInfo", $"{iMark}|Built ribbon info - PageTabLists: {ribbonInfo.PageTabLists.Count}, Panes: {ribbonInfo.Panes.Count}, Toolbars: {ribbonInfo.Toolbars.Count}, PushButtons: {ribbonInfo.PushButtons.Count}");

                return ribbonInfo;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("BuildRibbonInfo", $"{iMark}|Error building ribbon info: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 递归遍历 Ribbon 子对象
        /// </summary>
        private static void TraverseRibbonChildren(IAccessible parent, RibbonInfo ribbonInfo, MARSAccessibleProvider provider, int iMark, int depth)
        {
            try
            {
                int childCount = parent.accChildCount;
                if (childCount <= 0) return;

                object[] children = new object[childCount];
                int obtained = MARSAccessibleProvider.AccessibleChildren(parent, 0, childCount, children, out int nObtained);
                bool isSkip = false;
                for (int i = 0; i < nObtained; i++)
                {
                    if (children[i] is IAccessible childAcc)
                    {
                        ProcessRibbonChild(childAcc, ribbonInfo, provider, iMark, depth, ref isSkip);
                        if (isSkip)
                        {
                            isSkip = false;
                            continue;
                        }
                        // 递归遍历子对象
                        TraverseRibbonChildren(childAcc, ribbonInfo, provider, iMark, depth + 1);
                    }
                    else if (Marshal.IsComObject(children[i]))
                    {
                        IntPtr unk = Marshal.GetIUnknownForObject(children[i]);
                        try
                        {
                            var accChild = (IAccessible)Marshal.GetObjectForIUnknown(unk);
                            if (accChild != null)
                            {
                                ProcessRibbonChild(accChild, ribbonInfo, provider, iMark, depth, ref isSkip);
                                if (isSkip)
                                {
                                    isSkip = false;
                                    continue;
                                }
                                TraverseRibbonChildren(accChild, ribbonInfo, provider, iMark, depth + 1);
                            }
                        }
                        catch (Exception ex)
                        {
                            MarsLoggerSimple.Warning("TraverseRibbonChildren", $"{iMark}|Error processing COM object: {ex.Message}");
                        }
                        finally
                        {
                            Marshal.Release(unk);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("TraverseRibbonChildren", $"{iMark}|Error traversing children at depth {depth}: {ex.Message}");
            }
        }
        private static List<int> skipRoles = new List<int> {
            MARSAccessibleConstans.ROLE_SYSTEM_MENUBAR,
            MARSAccessibleConstans.ROLE_SYSTEM_TITLEBAR,
            MARSAccessibleConstans.ROLE_SYSTEM_BUTTONDROPDOWNGRID
        };
        /// <summary>
        /// 处理 Ribbon 子对象
        /// </summary>
        private static void ProcessRibbonChild(IAccessible child, RibbonInfo ribbonInfo,
            MARSAccessibleProvider provider, int iMark, int depth,
            ref bool isSkip)
        {
            try
            {
                int role = MARSAccessibleProvider.Get_Role(child);
                string roleName = MARSAccessibleProvider.GetRoleName(role);
                string name = GetAccessibleName(child);
                string value = GetAccessibleValue(child);
                Rectangle rect = GetAccessibleRect(child);
                MarsLoggerSimple.Info("ProcessRibbonChild", $"find|{roleName}|{name}|{value}");
                if (skipRoles.Contains(role)||(rect.Equals(Rectangle.Empty)))
                {
                    isSkip = true;
                    return;
                }

                string indent = new string(' ', depth * 2);
                MarsLoggerSimple.Info("ProcessRibbonChild", $"{iMark}|{indent}Processing: Role={roleName}({role}), Name='{name}', Value='{value}', Rect={rect}");

                // 根据角色类型分类
                switch (role)
                {
                    case MARSAccessibleConstans.ROLE_SYSTEM_PAGETABLIST:
                        // PageTabList 需要特殊处理，遍历其子项（通常是整数索引）
                        ProcessPageTabList(child, ribbonInfo, iMark, depth);
                        isSkip = true;/// 因为已经处理过了
                        break;

                    case MARSAccessibleConstans.ROLE_SYSTEM_PANE:
                        // 检查是否是 Lower Ribbon
                        if (!string.IsNullOrEmpty(name) && name.IndexOf("Lower Ribbon", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            ribbonInfo.Panes.Add(new PaneInfo
                            {
                                Name = name,
                                Value = value,
                                Rect = rect,
                                Accessible = child
                            });
                            MarsLoggerSimple.Info("ProcessRibbonChild", $"{iMark}|{indent}Added Lower Ribbon Pane: {name}");
                        }
                        break;

                    case MARSAccessibleConstans.ROLE_SYSTEM_TOOLBAR:
                        ribbonInfo.Toolbars.Add(new ToolbarInfo
                        {
                            Name = name,
                            Rect = rect,
                            Accessible = child
                        });
                        MarsLoggerSimple.Info("ProcessRibbonChild", $"{iMark}|{indent}Added Toolbar: {name}");
                        break;

                    case MARSAccessibleConstans.ROLE_SYSTEM_PUSHBUTTON:
                        ribbonInfo.PushButtons.Add(new RibbonButtonInfo
                        {
                            Name = name,
                            Rect = rect,
                            RoleName = "PushButton",
                            Accessible = child
                        });
                        MarsLoggerSimple.Info("ProcessRibbonChild", $"{iMark}|{indent}Added PushButton: {name}");
                        break;
                    case MARSAccessibleConstans.ROLE_SYSTEM_SPLITBUTTON:
                        ribbonInfo.splitButtonInfos.Add(new RibbonButtonInfo
                        {
                            Name = name,
                            Rect = rect,
                            RoleName= "SplitButton",
                            Accessible = child
                        });
                        MarsLoggerSimple.Info("ProcessRibbonChild", $"{iMark}|{indent}Added SplitButton: {name}");
                        break;
                    case MARSAccessibleConstans.ROLE_SYSTEM_BUTTONDROPDOWN:
                        ribbonInfo.ButtonDropDownInfos.Add(new RibbonButtonInfo
                        {
                            Name = name,
                            Rect = rect,
                            RoleName = "SplitButton",
                            Accessible = child
                        });
                        MarsLoggerSimple.Info("ProcessRibbonChild", $"{iMark}|{indent}Added SplitButton: {name}");
                        break;
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("ProcessRibbonChild", $"{iMark}|Error processing child: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理 PageTabList，遍历其子项（通常是整数索引）
        /// </summary>
        private static void ProcessPageTabList(IAccessible pageTabList, RibbonInfo ribbonInfo, int iMark, int depth)
        {
            try
            {
                int childCount = pageTabList.accChildCount;
                string indent = new string(' ', depth * 2);
                MarsLoggerSimple.Info("ProcessPageTabList", $"{iMark}|{indent}Processing PageTabList with {childCount} children");

                for (int idx = 1; idx <= childCount; idx++) // 索引从1开始
                {
                    try
                    {
                        // 通过索引获取名称
                        string tabName = GetAccessibleNameByIndex(pageTabList, idx);
                        
                        // 通过索引获取位置
                        Rectangle tabRect = GetAccessibleRectByIndex(pageTabList, idx);

                        if (!string.IsNullOrEmpty(tabName) && !tabRect.IsEmpty)
                        {
                            ribbonInfo.PageTabLists.Add(new PageTabInfo
                            {
                                Name = tabName,
                                Rect = tabRect,
                                Accessible = pageTabList
                            });
                            MarsLoggerSimple.Info("ProcessPageTabList", $"{iMark}|{indent}  Added PageTab[{idx}]: Name='{tabName}', Rect={tabRect}");
                        }
                        else
                        {
                            MarsLoggerSimple.Warning("ProcessPageTabList", $"{iMark}|{indent}  PageTab[{idx}] has empty name or rect: Name='{tabName}', Rect={tabRect}");
                        }
                    }
                    catch (Exception ex)
                    {
                        MarsLoggerSimple.Warning("ProcessPageTabList", $"{iMark}|{indent}  Error processing PageTab[{idx}]: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("ProcessPageTabList", $"{iMark}|Error processing PageTabList: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取 IAccessible 对象的名称
        /// </summary>
        private static string GetAccessibleName(IAccessible accessible)
        {
            try
            {
                return accessible.get_accName(0) ?? "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 获取 IAccessible 对象的值
        /// </summary>
        private static string GetAccessibleValue(IAccessible accessible)
        {
            try
            {
                return accessible.get_accValue(0) ?? "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 获取 IAccessible 对象的矩形区域
        /// </summary>
        private static Rectangle GetAccessibleRect(IAccessible accessible)
        {
            try
            {
                accessible.accLocation(out int left, out int top, out int width, out int height, 0);
                return new Rectangle(left, top, width, height);
            }
            catch
            {
                return Rectangle.Empty;
            }
        }

        /// <summary>
        /// 通过索引获取 IAccessible 对象的名称
        /// </summary>
        private static string GetAccessibleNameByIndex(IAccessible accessible, int index)
        {
            try
            {
                return accessible.get_accName(index) ?? "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 通过索引获取 IAccessible 对象的矩形区域
        /// </summary>
        private static Rectangle GetAccessibleRectByIndex(IAccessible accessible, int index)
        {
            try
            {
                accessible.accLocation(out int left, out int top, out int width, out int height, index);
                return new Rectangle(left, top, width, height);
            }
            catch
            {
                return Rectangle.Empty;
            }
        }

        internal static bool ClickMenuIcon(AutomationElement targetElement, string pegWindName, string objName, 
            Dictionary<string, string> dictPegProperties, 
            Dictionary<string, string> dictObjProperties, 
            string strParaMeter, string strData, ref string strError, 
            ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(100000);
            MarsLoggerSimple.logBegin("ClickMenuIcon", $"{iMark}|SelectMenuItem({pegWindName}.{objName},{MarsWindowsAPIsExtend.Dic2String(dictObjProperties)}, {strParaMeter}, {strData})");

            if (dealResult == null)
                dealResult = new MARSDealResult();

            try
            {
                MouseTrackRecorders.lastMousePoint = default(System.Windows.Point);  
                string[] arrButtons = strData.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                if ((arrButtons == null) || (arrButtons.Length<=0))
                {
                    strError = "Menu item names are empty";
                    MarsLoggerSimple.Error("ClickMenuIcon", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }
                string buttonCaptionToSearch = arrButtons[arrButtons.Length-1];

                if (targetElement == null)
                {
                    strError = "Target element is null";
                    MarsLoggerSimple.Error("ClickMenuIcon", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                if (string.IsNullOrEmpty(strData))
                {
                    strError = "Menu item name is empty";
                    MarsLoggerSimple.Error("ClickMenuIcon", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                // 获取目标元素的句柄
                IntPtr hwnd = (IntPtr)targetElement.Current.NativeWindowHandle;
                if (hwnd == IntPtr.Zero)
                {
                    strError = "Cannot get window handle from target element";
                    MarsLoggerSimple.Error("ClickMenuIcon", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                MarsLoggerSimple.Info("ClickMenuIcon", $"{iMark}|Target window handle: {hwnd}");

                // 获取 IAccessible 对象
                var accessibleProvider = new MARSAccessibleProvider();
                var accessibleObj = accessibleProvider.GetAccessibleObject(hwnd);
                if (accessibleObj == null)
                {
                    strError = "Cannot get IAccessible object from window handle";
                    MarsLoggerSimple.Error("ClickMenuIcon", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                IAccessible ribbonAccessible = accessibleObj as IAccessible;
                if (ribbonAccessible == null)
                {
                    strError = "Failed to cast to IAccessible";
                    MarsLoggerSimple.Error("ClickMenuIcon", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                MarsLoggerSimple.Info("ClickMenuIcon", $"{iMark}|Successfully got IAccessible object");

                // 构建 Ribbon 对象信息
                var ribbonInfo = BuildRibbonInfo(ribbonAccessible, iMark);
                if (ribbonInfo == null)
                {
                    strError = "Failed to build ribbon information";
                    MarsLoggerSimple.Error("ClickMenuIcon", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                bool isOk = DictionaryHelper.TryGetValueIgnoreCase(dictObjProperties, cnst_MarRibbonType, out string ribbonType);
                List<RibbonButtonInfo> matchingButtonInfo = null;
                /// 从最后一个button的caption开始查找
                if (ribbonType.Equals("PushButton", StringComparison.OrdinalIgnoreCase))
                {
                    matchingButtonInfo = ribbonInfo.PushButtons.Where(tab =>
                        !string.IsNullOrEmpty(tab.Name) &&
                        MarsWindowsAPIsExtend.RegularTest(buttonCaptionToSearch, tab.Name))
                        .ToList();
                }
                else if (ribbonType.Equals("ButtonDropDown", StringComparison.OrdinalIgnoreCase))
                {
                    matchingButtonInfo = ribbonInfo.ButtonDropDownInfos.Where(tab =>
                        !string.IsNullOrEmpty(tab.Name) &&
                        MarsWindowsAPIsExtend.RegularTest(buttonCaptionToSearch, tab.Name))
                        .ToList();
                }
                else if (ribbonType.Equals("SplitButton", StringComparison.OrdinalIgnoreCase))
                {
                    matchingButtonInfo = ribbonInfo.splitButtonInfos.Where(tab =>
                        !string.IsNullOrEmpty(tab.Name) &&
                        MarsWindowsAPIsExtend.RegularTest(buttonCaptionToSearch, tab.Name))
                        .ToList();
                }
                else
                {
                    matchingButtonInfo = ribbonInfo.splitButtonInfos.Where(tab =>
                        !string.IsNullOrEmpty(tab.Name) &&
                        MarsWindowsAPIsExtend.RegularTest(buttonCaptionToSearch, tab.Name))
                        .ToList();
                }
                if ((matchingButtonInfo == null) || (matchingButtonInfo.Count <= 0))
                {
                    strError = $"No matching Items found for '{strData}'. Available tabs: {string.Join(", ", ribbonInfo.PageTabLists.Select(t => t.Name))}";
                    MarsLoggerSimple.Error("ClickMenuIcon", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                string strGrpName = "";
                List<RibbonButtonInfo> matchingGrpButtonInfo = new List<RibbonButtonInfo>();
                if (arrButtons.Length == 2)
                {
                    foreach (var button in matchingButtonInfo)
                    {
                        if (button == null) continue;
                        if (button.Accessible == null) continue;
                        var prn = button.Accessible.accParent as IAccessible;
                        if (prn == null) continue;
                        strGrpName = prn.get_accName(0);
                        if (MarsWindowsAPIsExtend.RegularTest(arrButtons[0], strGrpName))
                        {
                            matchingGrpButtonInfo.Add(button);
                        }
                    }
                }
                else matchingGrpButtonInfo = matchingButtonInfo;

                if (matchingGrpButtonInfo.Count == 0)
                {
                    strError = $"No matching Group Items found for '{strData}' after filter by group caption. Available tabs: {string.Join(", ", ribbonInfo.PageTabLists.Select(t => t.Name))}";
                    MarsLoggerSimple.Error("ClickMenuIcon", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }
                var matchingTabArr = matchingGrpButtonInfo;
                
                if (matchingTabArr.Count != 1)
                {
                    strError = $"Multiple Items found for |{strData}|, Available tabs: {string.Join(", ", ribbonInfo.PageTabLists.Select(t => t.Name))}";
                    MarsLoggerSimple.Error("ClickMenuIcon", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                var matchingTab = matchingTabArr[0];
                MarsLoggerSimple.Info("ClickMenuIcon", $"{iMark}|Found matching PageTabList: {matchingTab.Name} at {matchingTab.Rect}");

                // 点击 PageTabList 的中间位置
                int centerX = matchingTab.Rect.X + matchingTab.Rect.Width / 2;
                int centerY = matchingTab.Rect.Y + matchingTab.Rect.Height / 2;

                MarsLoggerSimple.Info("ClickMenuIcon", $"{iMark}|Clicking at center position: X={centerX}, Y={centerY}");

                // 移动鼠标并点击
                MarsWindowsAPIsExtend.MoveMouse(centerX, centerY);
                System.Threading.Thread.Sleep(100);
                MarsWindowsAPIsExtend.LeftMouseClick(centerX, centerY);
                System.Threading.Thread.Sleep(200);
                MouseTrackRecorders.lastMousePoint = new System.Windows.Point(centerX, centerY);

                // 设置成功结果
                dealResult.ReturnedData = matchingTab.Name;
                dealResult.ResultMessage = "SUCCESS";
                dealResult.ErrorMessage = "";
                dealResult.AckTime = DateTime.Now;

                MarsLoggerSimple.Info("ClickMenuIcon", $"{iMark}|Successfully selected menu item: {matchingTab.Name}");
                return true;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                MarsLoggerSimple.Error("ClickMenuIcon", $"{iMark}|Error: {strError}", ex);
                dealResult.ErrorMessage = strError;
                dealResult.ResultMessage = $"FAILED,{strError}";
                dealResult.AckTime = DateTime.Now;
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("ClickMenuIcon", $"{iMark}|{dealResult.ResultMessage}");
            }
        }
    }
}
