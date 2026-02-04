using Mars.AutoTestingDriver.ExecuteTestcase.MarsProcess;
using Mars.Inter.MQCenter.MarsUtility;
using Mars.message.AutoTestingDriver.interProcess;
using Mars.message.Inter.MQCenter.interProcess;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.windowsWrapper.SystemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using Accessibility;

namespace Mars.Inter.MQCenter.MSAASupport.MarsAccessibleKeywords
{
    public class PopupMenuHelper
    {
        public static string cnst_defaultPopupMenuItem = "Default_MARSUI_Popup_menu";
        public static bool IsPopupMenuRequired(string strPara)
        {
            return strPara.IndexOf(cnst_defaultPopupMenuItem,StringComparison.OrdinalIgnoreCase)>=0;
        }

        internal static bool SelectMenuItemFromGlobalPopupMenu(string pegWindName, string objName,
            Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties, 
            string strParaMeter, string strData, 
            ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(100000);
            MarsLoggerSimple.logBegin("SelectMenuItemFromGlobalPopupMenu",$"{iMark}|{strParaMeter}|");
            //var cond = new AndCondition(
            //    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Menu),
            //    new PropertyCondition(AutomationElement.IsOffscreenProperty, false)
            //);
            var cond = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Menu);
            List<MarsSpiedObjectInfo> currentLayerObjects = new List<MarsSpiedObjectInfo>();

            if ((!MarsMARSUIHelper.GetProcessTopLevelUIElement(MARSTestProcess.CurrentTestProcessId, out currentLayerObjects))
                ||(currentLayerObjects==null)
                ||(currentLayerObjects.Count<=0))
            {
                strError = $"No top-level windows found for process {MARSTestProcess.CurrentTestProcessId}";
                MarsLoggerSimple.Error("SelectMenuItemFromGlobalPopupMenu", $"{iMark}|{strError}");
                //strError = "Cannot find popup menu.";                
                dealResult.ErrorMessage = $"FAILED,{strError}";
                dealResult.ReturnedData = "FAILD";
                dealResult.ResultMessage = $"FAILED,{strError}";
                return false;
            }
            var processRoot = currentLayerObjects[0].referenceToObj as AutomationElement;
            //var menus = AutomationElement.RootElement.FindAll(TreeScope.Subtree, cond);
            var menus = MarsMARSUIHelper.FindElementsByControlTypeWithDepthLimit(processRoot, cond, 5, ref strError);

            MarsLoggerSimple.Info("SelectMenuItemFromGlobalPopupMenu", $"find menu:{menus.Count}");
            AutomationElement best = null;

            for (int i = 0; i < menus.Count; i++)
            {
                var m = menus[i];
                if (m == null) continue;
                MarsLoggerSimple.Info("SelectMenuItemFromGlobalPopupMenu", $"{iMark}|Menu {i}|{menus.Count}: Name='{Safe(() => m.Current.Name)}', ProcessId={Safe(() => m.Current.ProcessId)}, Rect={Safe(() => m.Current.BoundingRectangle)}");

                if (Safe(() => m.Current.ProcessId) != MARSTestProcess.CurrentTestProcessId) continue; // 仅同进程，降低误判
                var r = Safe(() => m.Current.BoundingRectangle);
                if (r == Rect.Empty) continue;

                // 与按钮附近相交/靠近就认为是它的 popup
                if (IntersectsOrNear(MouseTrackRecorders.lastMouseInRectange, r))
                {
                    best = m; 
                    break;
                }
            }
            if (best == null)
            {
                strError = "Cannot find popup menu.";
                MarsLoggerSimple.Error("SelectMenuItemFromGlobalPopupMenu", $"{iMark}|{strError}.|");
                dealResult.ErrorMessage = $"FAILED,{strError}";
                dealResult.ReturnedData = "FAILD";
                dealResult.ResultMessage = $"FAILED,{strError}";
                return false;
            }

            // 找菜单项
            string strMenuName = "";
            var mi = FindMenuItem(best, strData,ref strMenuName);
            if (mi == null)
            {
                strError = $"Cannot find menu item '{strData}'";
                MarsLoggerSimple.Error("SelectMenuItemFromGlobalPopupMenu", $"{iMark}|{strError}.|");
                dealResult.ErrorMessage = $"FAILED,{strError}";
                dealResult.ReturnedData = "FAILD";
                dealResult.ResultMessage = $"FAILED,{strError}";
                return false;
            }
            // 点击
            if (!ClickElement(mi))
            {
                strError = $"Cannot click menu item '{strData}'";
                MarsLoggerSimple.Error("SelectMenuItemFromGlobalPopupMenu", $"{iMark}|{strError}.|");
                dealResult.ErrorMessage = $"FAILED,{strError}";
                dealResult.ReturnedData = "FAILD";
                dealResult.ResultMessage = $"FAILED,{strError}";
                return false;
            }
            dealResult.ErrorMessage = "OK";
            dealResult.ReturnedData = "SUCCESS";
            dealResult.ResultMessage = "SUCCESS";
            dealResult.ActualInputData = strMenuName;
            dealResult.AckTime = DateTime.Now;
            return true;
        }


        static T Safe<T>(Func<T> f)
        {
            try { return f(); } catch { return default(T); }
        }

        static bool IntersectsOrNear(Rect a, Rect b)
        {
            if (a == Rect.Empty || b == Rect.Empty) return false;
            // 扩一点容差
            var ex = Inflate(a, 20);
            return ex.IntersectsWith(b);
        }

        static Rect Inflate(Rect r, double d)
        {
            if (r == Rect.Empty) return r;
            return new Rect(r.Left - d, r.Top - d, r.Width + 2 * d, r.Height + 2 * d);
        }

        // ---------- 辅助：在菜单里找菜单项 ----------
        static AutomationElement FindMenuItem(AutomationElement menu, string namePart,ref string strMenuItemCaption)
        {
            MarsLoggerSimple.logBegin("FindMenuItem", $"{namePart}|{strMenuItemCaption}");
            string strAllItems = "";
            
            // 尝试将menu的handle转换为IAccessible对象并打印其子对象
            try
            {
                PrintMenuAsIAccessible(menu);
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("FindMenuItem", $"Error printing menu as IAccessible: {ex.Message}");
            }
            
            // 先 Children 精确/部分匹配
            var walker = TreeWalker.ControlViewWalker;
            try
            {
                for (var c = walker.GetFirstChild(menu); c != null; c = walker.GetNextSibling(c))
                {
                    MarsLoggerSimple.Info("FindMenuItem", $"{c.Current.Name}|{c.Current.ControlType}|");
                    FlashControlHelper.FlashControlByXORDrawing(c);
                    
                    // 检查是否是ToolBar对象
                    var currentControlType = Safe(() => c.Current.ControlType);
                    if (currentControlType == ControlType.ToolBar)
                    {
                        MarsLoggerSimple.Info("FindMenuItem", $"Found ToolBar: {Safe(() => c.Current.Name)}");
                        PrintToolBarChildren(c);
                        continue;
                    }
                    
                    if (currentControlType != ControlType.MenuItem)
                    {               
                        continue;
                    }
                    var nm = Safe(() => c.Current.Name) ?? "";
                    strAllItems += nm + ";";
                    if (MarsWindowsAPIsExtend.RegularTest(namePart, nm))
                    {
                        strMenuItemCaption = nm;
                        //if (nm.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0)
                        return c;
                    }
                }

                var cntrls = menu.FindAll(TreeScope.Descendants, System.Windows.Automation.Condition.TrueCondition);
                for(int i = 0; i < cntrls.Count; i++)
                {
                    MarsLoggerSimple.Info("FindMenuItem", $"itms:|{cntrls[i].Current.Name}|{cntrls[i].Current.ControlType.ProgrammaticName}");
                }
                return null;
            }
            finally
            {
                MarsLoggerSimple.Info("FindMenuItem", $"Menu items: {strAllItems}");
            }            
        }

        /// <summary>
        /// 打印ToolBar的所有子项及其详细信息
        /// </summary>
        /// <param name="toolBar">ToolBar对象</param>
        private static void PrintToolBarChildren(AutomationElement toolBar)
        {
            try
            {
                if (toolBar == null)
                {
                    MarsLoggerSimple.Error("PrintToolBarChildren", "ToolBar is null");
                    return;
                }

                MarsLoggerSimple.Info("PrintToolBarChildren", "=== ToolBar Children Information ===");
                MarsLoggerSimple.Info("PrintToolBarChildren", $"ToolBar Name: {Safe(() => toolBar.Current.Name)}");
                MarsLoggerSimple.Info("PrintToolBarChildren", $"ToolBar AutomationId: {Safe(() => toolBar.Current.AutomationId)}");
                MarsLoggerSimple.Info("PrintToolBarChildren", $"ToolBar ClassName: {Safe(() => toolBar.Current.ClassName)}");

                // 获取所有子元素
                var children = toolBar.FindAll(TreeScope.Children, System.Windows.Automation.Condition.TrueCondition);
                if (children == null || children.Count == 0)
                {
                    MarsLoggerSimple.Info("PrintToolBarChildren", "No children found in ToolBar");
                    return;
                }

                MarsLoggerSimple.Info("PrintToolBarChildren", $"Total children count: {children.Count}");
                MarsLoggerSimple.Info("PrintToolBarChildren", "---");

                int index = 0;
                foreach (AutomationElement child in children)
                {
                    try
                    {
                        string name = Safe(() => child.Current.Name) ?? "(No Name)";
                        string automationId = Safe(() => child.Current.AutomationId) ?? "(No AutomationId)";
                        string className = Safe(() => child.Current.ClassName) ?? "(No ClassName)";
                        string controlType = Safe(() => child.Current.ControlType?.ProgrammaticName) ?? "(No ControlType)";
                        bool isEnabled = Safe(() => child.Current.IsEnabled);
                        bool isOffscreen = Safe(() => child.Current.IsOffscreen);
                        var boundingRect = Safe(() => child.Current.BoundingRectangle);
                        string helpText = Safe(() => child.Current.HelpText) ?? "";
                        string itemType = Safe(() => child.Current.ItemType) ?? "";
                        
                        MarsLoggerSimple.Info("PrintToolBarChildren", $"[{index}] Name: {name}");
                        MarsLoggerSimple.Info("PrintToolBarChildren", $"    ControlType: {controlType}");
                        MarsLoggerSimple.Info("PrintToolBarChildren", $"    AutomationId: {automationId}");
                        MarsLoggerSimple.Info("PrintToolBarChildren", $"    ClassName: {className}");
                        MarsLoggerSimple.Info("PrintToolBarChildren", $"    IsEnabled: {isEnabled}");
                        MarsLoggerSimple.Info("PrintToolBarChildren", $"    IsOffscreen: {isOffscreen}");
                        MarsLoggerSimple.Info("PrintToolBarChildren", $"    BoundingRect: {boundingRect}");
                        
                        if (!string.IsNullOrEmpty(helpText))
                        {
                            MarsLoggerSimple.Info("PrintToolBarChildren", $"    HelpText: {helpText}");
                        }
                        
                        if (!string.IsNullOrEmpty(itemType))
                        {
                            MarsLoggerSimple.Info("PrintToolBarChildren", $"    ItemType: {itemType}");
                        }

                        // 尝试获取Value Pattern
                        if (child.TryGetCurrentPattern(ValuePattern.Pattern, out object valuePatternObj))
                        {
                            var valuePattern = valuePatternObj as ValuePattern;
                            if (valuePattern != null)
                            {
                                string value = Safe(() => valuePattern.Current.Value) ?? "";
                                if (!string.IsNullOrEmpty(value))
                                {
                                    MarsLoggerSimple.Info("PrintToolBarChildren", $"    Value: {value}");
                                }
                            }
                        }

                        // 尝试获取Toggle Pattern
                        if (child.TryGetCurrentPattern(TogglePattern.Pattern, out object togglePatternObj))
                        {
                            var togglePattern = togglePatternObj as TogglePattern;
                            if (togglePattern != null)
                            {
                                var toggleState = Safe(() => togglePattern.Current.ToggleState);
                                MarsLoggerSimple.Info("PrintToolBarChildren", $"    ToggleState: {toggleState}");
                            }
                        }

                        MarsLoggerSimple.Info("PrintToolBarChildren", "---");
                        index++;
                    }
                    catch (Exception ex)
                    {
                        MarsLoggerSimple.Error("PrintToolBarChildren", $"Error processing child {index}: {ex.Message}");
                    }
                }

                MarsLoggerSimple.Info("PrintToolBarChildren", "=== End of ToolBar Children ===");
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("PrintToolBarChildren", $"Error printing toolbar children: {ex.Message}", ex);
            }
        }

        // ---------- 工具：在某元素上点击 ----------
        static bool ClickElement(AutomationElement e)
        {
            // ClickablePoint（更可靠，不用换算坐标系）
            try
            {
                if (e.TryGetClickablePoint(out System.Windows.Point pt))
                {
                    //MarsWindowsAPIsExtend.SetCursorPos((int)pt.X, (int)pt.Y);
                    MarsWindowsAPIsExtend.LeftMouseClick((int)pt.X, (int)pt.Y);
                    //MarsWindowsAPIsExtend.mouse_event(MarsWindowsAPIsExtend.MOUSEEVENTF_LEFTDOWN, (int)pt.X, (int)pt.Y, 0, 0);
                    return true;
                }
                else
                {
                    var rect = e.Current.BoundingRectangle;
                    if (rect == Rect.Empty) return false;
                    double cx = (rect.Left + rect.Right) / 2.0;
                    double cy = (rect.Top + rect.Bottom) / 2.0;
                    MarsWindowsAPIsExtend.LeftMouseClick((int)cx, (int)cy);
                    return true;
                }
                
            }
            catch (Exception xe)
            {
                return false;
            }

        }

        /// <summary>
        /// 将AutomationElement转换为IAccessible对象并打印其所有子对象
        /// </summary>
        /// <param name="menu">菜单AutomationElement</param>
        private static void PrintMenuAsIAccessible(AutomationElement menu)
        {
            try
            {
                if (menu == null)
                {
                    MarsLoggerSimple.Error("PrintMenuAsIAccessible", "Menu AutomationElement is null");
                    return;
                }

                // 获取窗口句柄
                int hwnd = Safe(() => menu.Current.NativeWindowHandle);
                if (hwnd == 0)
                {
                    MarsLoggerSimple.Error("PrintMenuAsIAccessible", "Unable to get window handle from menu");
                    return;
                }

                MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"=== Menu IAccessible Information ===");
                MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"Window Handle: 0x{hwnd:X8}");

                // 使用MfcAccessibleHelper获取IAccessible对象
                var iacc = MfcAccessibleHelper.GetIAccessibleFromAfxWindow(new IntPtr(hwnd));
                if (iacc == null)
                {
                    MarsLoggerSimple.Error("PrintMenuAsIAccessible", "Unable to get IAccessible from window handle");
                    return;
                }

                // 打印IAccessible根对象信息
                PrintIAccessibleInfo(iacc, 0, 0);

                // 获取并打印所有子对象
                var children = MfcAccessibleHelper.GetDirectChildren(iacc);
                int childIndex = 0;
                foreach (var child in children)
                {
                    childIndex++;
                    if (child is IAccessible childAcc)
                    {
                        MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"--- Child {childIndex} (IAccessible) ---");
                        PrintIAccessibleInfo(childAcc, 0, childIndex);
                        
                        // 递归打印子对象的子对象
                        PrintIAccessibleChildren(childAcc, 1, childIndex);
                    }
                    else if (child is int childId)
                    {
                        MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"--- Child {childIndex} (ChildID: {childId}) ---");
                        PrintIAccessibleInfoByChildId(iacc, childId, childIndex);
                    }
                }

                MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"=== End of Menu IAccessible Information ===");
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("PrintMenuAsIAccessible", $"Error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 递归打印IAccessible的子对象
        /// </summary>
        private static void PrintIAccessibleChildren(IAccessible acc, int level, int parentIndex)
        {
            try
            {
                var children = MfcAccessibleHelper.GetDirectChildren(acc);
                int childIndex = 0;
                foreach (var child in children)
                {
                    childIndex++;
                    string indent = new string(' ', level * 2);
                    
                    if (child is IAccessible childAcc)
                    {
                        MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"{indent}--- Level {level} Child {childIndex} (IAccessible) ---");
                        PrintIAccessibleInfo(childAcc, 0, childIndex);
                        
                        // 递归（限制深度以防止无限递归）
                        if (level < 5)
                        {
                            PrintIAccessibleChildren(childAcc, level + 1, childIndex);
                        }
                    }
                    else if (child is int childId)
                    {
                        MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"{indent}--- Level {level} Child {childIndex} (ChildID: {childId}) ---");
                        PrintIAccessibleInfoByChildId(acc, childId, childIndex);
                    }
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("PrintMenuAsIAccessible", $"Error in PrintIAccessibleChildren: {ex.Message}");
            }
        }

        /// <summary>
        /// 打印IAccessible对象的详细信息
        /// </summary>
        private static void PrintIAccessibleInfo(IAccessible acc, int childId, int index)
        {
            try
            {
                string name = Safe(() => acc.get_accName(childId)) ?? "(No Name)";
                string value = Safe(() => acc.get_accValue(childId)) ?? "(No Value)";
                string description = Safe(() => acc.get_accDescription(childId)) ?? "(No Description)";
                object roleObj = Safe(() => acc.get_accRole(childId));
                string role = Safe(() => GetRoleName(roleObj));
                string state = Safe(() => GetStateName(acc.get_accState(childId)));
                string defaultAction = Safe(() => acc.get_accDefaultAction(childId)) ?? "(No DefaultAction)";
                string keyboardShortcut = Safe(() => acc.get_accKeyboardShortcut(childId)) ?? "(No Shortcut)";
                string help = Safe(() => acc.get_accHelp(childId)) ?? "(No Help)";
                MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"    children|{acc.accChildCount}");
                MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"    [{index}] Name: {name}");
                MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"         Role: {role}");
                MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"         State: {state}");
                
                // 检测是否是MENUBAR角色
                if (roleObj is int roleInt && roleInt == 0x0B) // 0x0B = MENUBAR
                {
                    MarsLoggerSimple.Info("PrintMenuAsIAccessible", "         *** MENUBAR detected - Getting all children ***");
                    var menuBarChildren = GetMenuBarChildren(acc);
                    PrintMenuBarChildrenInfo(menuBarChildren);
                }
                
                if (!string.IsNullOrEmpty(value) && value != "(No Value)")
                {
                    MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"         Value: {value}");
                }
                
                if (!string.IsNullOrEmpty(description) && description != "(No Description)")
                {
                    MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"         Description: {description}");
                }
                
                if (!string.IsNullOrEmpty(defaultAction) && defaultAction != "(No DefaultAction)")
                {
                    MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"         DefaultAction: {defaultAction}");
                }
                
                if (!string.IsNullOrEmpty(keyboardShortcut) && keyboardShortcut != "(No Shortcut)")
                {
                    MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"         KeyboardShortcut: {keyboardShortcut}");
                }
                
                if (!string.IsNullOrEmpty(help) && help != "(No Help)")
                {
                    MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"         Help: {help}");
                }

                // 尝试获取位置信息
                try
                {
                    acc.accLocation(out int left, out int top, out int width, out int height, childId);
                    MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"         Location: ({left}, {top}), Size: {width}x{height}");
                }
                catch { }

                // 获取子对象数量
                int childCount = Safe(() => acc.accChildCount);
                if (childCount > 0)
                {
                    MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"         ChildCount: {childCount}");
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("PrintMenuAsIAccessible", $"Error printing IAccessible info: {ex.Message}");
            }
        }

        /// <summary>
        /// 通过ChildID打印IAccessible对象信息
        /// </summary>
        private static void PrintIAccessibleInfoByChildId(IAccessible parent, int childId, int index)
        {
            try
            {
                string name = Safe(() => parent.get_accName(childId)) ?? "(No Name)";
                string value = Safe(() => parent.get_accValue(childId)) ?? "(No Value)";
                string role = Safe(() => GetRoleName(parent.get_accRole(childId)));
                string state = Safe(() => GetStateName(parent.get_accState(childId)));

                MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"    [{index}] Name: {name}");
                MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"         Role: {role}");
                MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"         State: {state}");
                
                if (!string.IsNullOrEmpty(value) && value != "(No Value)")
                {
                    MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"         Value: {value}");
                }

                // 尝试获取位置信息
                try
                {
                    parent.accLocation(out int left, out int top, out int width, out int height, childId);
                    MarsLoggerSimple.Info("PrintMenuAsIAccessible", $"         Location: ({left}, {top}), Size: {width}x{height}");
                }
                catch { }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("PrintMenuAsIAccessible", $"Error printing IAccessible info by ChildID: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取角色名称
        /// </summary>
        private static string GetRoleName(object role)
        {
            if (role == null) return "Unknown";
            
            if (role is int roleInt)
            {
                // MSAA角色常量
                switch (roleInt)
                {
                    case 0x09: return "WINDOW";
                    case 0x0A: return "CLIENT";
                    case 0x0B: return "MENUBAR";
                    case 0x0C: return "MENUITEM";
                    case 0x0D: return "MENUPOPUP";
                    case 0x2B: return "PUSHBUTTON";
                    case 0x2C: return "CHECKBUTTON";
                    case 0x2D: return "RADIOBUTTON";
                    case 0x2E: return "COMBOBOX";
                    case 0x2F: return "EDIT";
                    case 0x21: return "LIST";
                    case 0x22: return "LISTITEM";
                    case 0x3C: return "PANE";
                    case 0x0E: return "TOOLTIP";
                    default: return $"Role_{roleInt}";
                }
            }
            
            return role.ToString();
        }

        /// <summary>
        /// 获取状态名称
        /// </summary>
        private static string GetStateName(object state)
        {
            if (state == null) return "Unknown";
            
            if (state is int stateInt)
            {
                var states = new List<string>();
                
                if ((stateInt & 0x00000001) != 0) states.Add("UNAVAILABLE");
                if ((stateInt & 0x00000002) != 0) states.Add("SELECTED");
                if ((stateInt & 0x00000004) != 0) states.Add("FOCUSED");
                if ((stateInt & 0x00000008) != 0) states.Add("PRESSED");
                if ((stateInt & 0x00000010) != 0) states.Add("CHECKED");
                if ((stateInt & 0x00000020) != 0) states.Add("MIXED");
                if ((stateInt & 0x00000100) != 0) states.Add("READONLY");
                if ((stateInt & 0x00008000) != 0) states.Add("INVISIBLE");
                if ((stateInt & 0x00010000) != 0) states.Add("OFFSCREEN");
                if ((stateInt & 0x00020000) != 0) states.Add("SIZEABLE");
                if ((stateInt & 0x00040000) != 0) states.Add("MOVEABLE");
                if ((stateInt & 0x00100000) != 0) states.Add("FOCUSABLE");
                
                return states.Count > 0 ? string.Join("|", states) : $"State_{stateInt}";
            }
            
            return state.ToString();
        }

        /// <summary>
        /// 获取MENUBAR的所有子对象到List中
        /// </summary>
        /// <param name="menuBar">MENUBAR的IAccessible对象</param>
        /// <returns>子对象列表</returns>
        private static List<IAccessible> GetMenuBarChildren(IAccessible menuBar)
        {
            var childrenList = new List<IAccessible>();
            
            try
            {
                if (menuBar == null)
                {
                    MarsLoggerSimple.Error("GetMenuBarChildren", "MenuBar IAccessible is null");
                    return childrenList;
                }

                // 获取子对象数量
                int childCount = Safe(() => menuBar.accChildCount);
                MarsLoggerSimple.Info("GetMenuBarChildren", $"MenuBar has {childCount} children");

                if (childCount <= 0)
                {
                    return childrenList;
                }

                // 使用MfcAccessibleHelper获取所有直接子对象
                var children = MfcAccessibleHelper.GetDirectChildren(menuBar);
                
                foreach (var child in children)
                {
                    if (child is IAccessible childAcc)
                    {
                        childrenList.Add(childAcc);
                        
                        // 获取子对象的基本信息用于日志
                        string childName = Safe(() => childAcc.get_accName(0)) ?? "Unknown";
                        string childRole = Safe(() => GetRoleName(childAcc.get_accRole(0)));
                        MarsLoggerSimple.Info("GetMenuBarChildren", $"  Added child: {childName} (Role: {childRole})");
                    }
                    else if (child is int childId)
                    {
                        // 对于简单子元素（通过childId访问），尝试获取其IAccessible接口
                        try
                        {
                            object childObj = menuBar.get_accChild(childId);
                            if (childObj is IAccessible childAccFromId)
                            {
                                childrenList.Add(childAccFromId);
                                string childName = Safe(() => childAccFromId.get_accName(0)) ?? "Unknown";
                                string childRole = Safe(() => GetRoleName(childAccFromId.get_accRole(0)));
                                MarsLoggerSimple.Info("GetMenuBarChildren", $"  Added child (from ID {childId}): {childName} (Role: {childRole})");
                            }
                        }
                        catch (Exception ex)
                        {
                            MarsLoggerSimple.Error("GetMenuBarChildren", $"Cannot get IAccessible for childId {childId}: {ex.Message}");
                        }
                    }
                }

                MarsLoggerSimple.Info("GetMenuBarChildren", $"Total IAccessible children collected: {childrenList.Count}");
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetMenuBarChildren", $"Error getting MenuBar children: {ex.Message}", ex);
            }

            return childrenList;
        }

        /// <summary>
        /// 打印MENUBAR子对象列表的详细信息
        /// </summary>
        /// <param name="children">子对象列表</param>
        private static void PrintMenuBarChildrenInfo(List<IAccessible> children)
        {
            try
            {
                if (children == null || children.Count == 0)
                {
                    MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", "No children to print");
                    return;
                }

                MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", "========================================");
                MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", $"=== MENUBAR Children Details ({children.Count} items) ===");
                MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", "========================================");

                for (int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    
                    try
                    {
                        MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", $"");
                        MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", $"--- MENUBAR Child #{i + 1} ---");
                        
                        // 基本属性
                        string name = Safe(() => child.get_accName(0)) ?? "(No Name)";
                        string value = Safe(() => child.get_accValue(0)) ?? "(No Value)";
                        string description = Safe(() => child.get_accDescription(0)) ?? "(No Description)";
                        string role = Safe(() => GetRoleName(child.get_accRole(0)));
                        string state = Safe(() => GetStateName(child.get_accState(0)));
                        string defaultAction = Safe(() => child.get_accDefaultAction(0)) ?? "(No DefaultAction)";
                        string keyboardShortcut = Safe(() => child.get_accKeyboardShortcut(0)) ?? "(No Shortcut)";
                        string help = Safe(() => child.get_accHelp(0)) ?? "(No Help)";
                        
                        MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", $"  Name: {name}");
                        MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", $"  Role: {role}");
                        MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", $"  State: {state}");
                        
                        if (!string.IsNullOrEmpty(value) && value != "(No Value)")
                        {
                            MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", $"  Value: {value}");
                        }
                        
                        if (!string.IsNullOrEmpty(description) && description != "(No Description)")
                        {
                            MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", $"  Description: {description}");
                        }
                        
                        if (!string.IsNullOrEmpty(defaultAction) && defaultAction != "(No DefaultAction)")
                        {
                            MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", $"  DefaultAction: {defaultAction}");
                        }
                        
                        if (!string.IsNullOrEmpty(keyboardShortcut) && keyboardShortcut != "(No Shortcut)")
                        {
                            MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", $"  KeyboardShortcut: {keyboardShortcut}");
                        }
                        
                        if (!string.IsNullOrEmpty(help) && help != "(No Help)")
                        {
                            MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", $"  Help: {help}");
                        }

                        // 位置信息
                        try
                        {
                            child.accLocation(out int left, out int top, out int width, out int height, 0);
                            MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", $"  Location: ({left}, {top}), Size: {width}x{height}");
                        }
                        catch { }

                        // 子对象信息
                        int childCount = Safe(() => child.accChildCount);
                        if (childCount > 0)
                        {
                            MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", $"  ChildCount: {childCount}");
                            
                            // 列出子菜单项
                            MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", $"  Sub-items:");
                            var subChildren = MfcAccessibleHelper.GetDirectChildren(child);
                            int subIndex = 0;
                            foreach (var subChild in subChildren)
                            {
                                subIndex++;
                                if (subChild is IAccessible subAcc)
                                {
                                    string subName = Safe(() => subAcc.get_accName(0)) ?? "Unknown";
                                    string subRole = Safe(() => GetRoleName(subAcc.get_accRole(0)));
                                    MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", $"    [{subIndex}] {subName} ({subRole})");
                                }
                                else if (subChild is int subChildId)
                                {
                                    string subName = Safe(() => child.get_accName(subChildId)) ?? "Unknown";
                                    string subRole = Safe(() => GetRoleName(child.get_accRole(subChildId)));
                                    MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", $"    [{subIndex}] {subName} ({subRole}) [ChildID: {subChildId}]");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MarsLoggerSimple.Error("PrintMenuBarChildrenInfo", $"Error printing child #{i + 1}: {ex.Message}");
                    }
                }

                MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", "");
                MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", "========================================");
                MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", "=== End of MENUBAR Children Details ===");
                MarsLoggerSimple.Info("PrintMenuBarChildrenInfo", "========================================");
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("PrintMenuBarChildrenInfo", $"Error printing MenuBar children info: {ex.Message}", ex);
            }
        }
    }
}
