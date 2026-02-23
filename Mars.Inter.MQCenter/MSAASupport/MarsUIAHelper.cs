using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using Condition = System.Windows.Automation.Condition;
using Mars.message.windowsWrapper.SystemUtil;
using Accessibility;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.AutoTestingDriver.interProcess;
using Mars.AutoTestingDriver.ExecuteTestcase.MarsProcess;
using Mars.message.Inter.MQCenter.interProcess;
using Mars.AutoTestingDriver.MarsUISupport;
using Mars.Inter.MQCenter.MarsUtility;
using Mars.Inter.MQCenter.MSAASupport.MarsAccessibleKeywords;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;

using Mars.AutoTestingDriver.ExecuteTestcase.MarsMSAASupport;
using Mars.Inter.MQCenter.windowsControlsHelpers.MarsMSAASupport;
using System.Runtime.CompilerServices;
using System.Net.NetworkInformation;


namespace Mars.Inter.MQCenter.MSAASupport
{

    public class MarsMixedMARSUIAndIAccessibleChildren
    {
        public AutomationElement? ParentElement { get; set; }
        public List<(AutomationElement, IAccessible)> AccessibleChildren { get; set; } = new List<(AutomationElement, IAccessible)>();
    }


    public static class MarsMARSUIHelper
    {
        public const string CNST_POPUP_FROM_CUR_POS = "POPUP_FROM_CUR_POS";
        public const string CNST_POPUP_FROM_CURRENT_POSITION = "POPUP_FROM_CURRENT_POSITION";
        public const string CNST_POPUP_FROM_CUR_POSITION = "POPUP_FROM_CUR_POSITION";
        public const string CNST_POPUP_FROM_CURSOR_POS = "POPUP_FROM_CURSOR_POS";
        public const string CNST_POPUP_OFFSET_CURRENT_POS = "OffsetCurrentPos";

        private static readonly Regex PopupOffsetCurrentPosRegex =
            new Regex(@"OffsetCurrentPos\s*:\s*(?<x>-?\d+)\s*(?:,\s*(?<y>-?\d+))?",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);


        public static bool IsUIAAPopupMenuParameter(string strPara, ref int offsetX, ref int offsetY)
        {
            if (string.IsNullOrEmpty(strPara)) return false;
            if (strPara.IndexOf(CNST_POPUP_FROM_CURSOR_POS, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (strPara.IndexOf(CNST_POPUP_FROM_CUR_POS, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (strPara.IndexOf(CNST_POPUP_FROM_CURRENT_POSITION, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (strPara.IndexOf(CNST_POPUP_FROM_CUR_POSITION, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            var match = PopupOffsetCurrentPosRegex.Match(strPara);
            if (match.Success)
            {
                if (int.TryParse(match.Groups["x"].Value, out var x))
                {
                    offsetX = x;
                    if (match.Groups["y"].Success && !string.IsNullOrWhiteSpace(match.Groups["y"].Value))
                    {
                        if (int.TryParse(match.Groups["y"].Value, out var y))
                        {
                            offsetY = y;
                        }
                        else
                        {
                            MarsLoggerSimple.Warning(nameof(IsUIAAPopupMenuParameter),
                                $"OffsetCurrentPos parse failed for y, raw: {strPara}");
                        }
                    }
                    MarsLoggerSimple.Info(nameof(IsUIAAPopupMenuParameter),
                        $"OffsetCurrentPos parsed: x={offsetX}, y={offsetY}");
                }
                else
                {
                    MarsLoggerSimple.Warning(nameof(IsUIAAPopupMenuParameter),
                        $"OffsetCurrentPos parse failed, raw: {strPara}");
                }
                return true;
            }
            return false;

        }


        public static string GetControlIdFromAutomationUI(AutomationElement ae, bool isCached = false)
        {
            if (ae == null) return null;
            try
            {
                int hwnd = 0;
                if (!isCached)
                    hwnd = ae.Current.NativeWindowHandle;
                else hwnd = ae.Cached.NativeWindowHandle;
                var controlid = MarsWindowsAPIs.GetDlgCtrlID(new IntPtr(hwnd));
                return controlid + "";
            }
            catch (Exception e)
            {
                MarsLoggerSimple.Error("GetControlIdFromAutomationUI", $"Error getting ControlId: {e.Message}", e);
                return null;
            }
        }

        /// <summary>
        /// 从屏幕坐标 (x,y) 获取该点处 MARSUI 元素，并以其顶层 Window 作为根，遍历其 ControlView 树
        /// </summary>
        /// <param name="x">屏幕 X</param>
        /// <param name="y">屏幕 Y</param>
        /// <param name="writer">输出（默认 Console.Out）</param>
        /// <param name="includeOffscreen">是否包含屏幕外元素</param>
        /// <param name="maxDepth">最大深度（根=0；-1 为不限）</param>
        public static void DumpTreeFromPoint(int x, int y, TextWriter? writer = null, bool includeOffscreen = true, int maxDepth = -1)
        {
            EnsureStaThread();

            writer ??= Console.Out;

            AutomationElement elemAtPoint;
            try
            {
                elemAtPoint = AutomationElement.FromPoint(new Point(x, y));
            }
            catch (Exception ex)
            {
                writer.WriteLine($"[ERR] ElementFromPoint failed: {ex.Message}");
                return;
            }

            // 回溯到顶层 Window（或直到无父）
            var root = GetTopWindowFor(elemAtPoint);
            if (root == null)
            {
                writer.WriteLine("[WARN] Top window not found from point; fallback to element itself.");
                root = elemAtPoint;
            }

            // 复用现有从句柄遍历的实现（若能取到 hwnd），否则直接从元素遍历
            int hwnd = 0;
            try { hwnd = root.Current.NativeWindowHandle; } catch { hwnd = 0; }
            if (hwnd != 0)
            {
                DumpTreeFromHwnd((IntPtr)hwnd, writer, includeOffscreen, maxDepth);
                return;
            }

            DumpTreeFromElement(root, writer, includeOffscreen, maxDepth);
        }

        public static string GetUIAObjectWindowClass(AutomationElement element, ref bool isOk, ref string strError)
        {
            if (element == null)
            {
                isOk = false;
                strError = "Source Object is null";
                return null;
            }

            int hwnd = element.Current.NativeWindowHandle;
            try
            {
                StringBuilder sb = new StringBuilder(256);
                int iLen = MarsWindowsAPIs.GetClassName(new IntPtr(hwnd), sb, 255);
                isOk = true;
                return sb.ToString();
            } catch (Exception e)
            {
                isOk = false;
                strError = e.Message;
                return null;
            }
        }

        public static bool IsDesktopElement(AutomationElement element)
        {
            if (element == null) return false;

            // 安全获取元素属性
            bool isPane = false;
            bool isClassDesktop = false;
            bool isAutoIdEmpty = true;
            bool isNameDesktop = false;

            try
            {
                // 1. ControlType == Pane
                isPane = element.Current.ControlType == ControlType.Pane;

                // 2. ClassName == "Desktop"（有些环境可能为空）
                string className = element.Current.ClassName ?? "";
                isClassDesktop = string.Equals(className, "Desktop", StringComparison.OrdinalIgnoreCase);
                isClassDesktop = isClassDesktop || className.StartsWith("Desktop", StringComparison.OrdinalIgnoreCase);

                // 3. AutomationId 为空
                isAutoIdEmpty = string.IsNullOrEmpty(element.Current.AutomationId);

                // 4. Name == "Desktop"
                isNameDesktop = string.Equals(element.Current.Name, "Desktop", StringComparison.OrdinalIgnoreCase);
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // COM 错误时使用默认值
                return false;
            }
            catch (Exception)
            {
                // 其他错误时使用默认值
                return false;
            }

            // 5. Parent == null（Desktop是MARSUI树的根节点）
            var walker = TreeWalker.ControlViewWalker;
            bool isRoot = walker.GetParent(element) == null;

            // 综合判断（最常见的Desktop满足这些条件）
            return isPane && isRoot && (isClassDesktop || isNameDesktop);
        }

        /// <summary>
        /// 判断指定属性字典是否使用MARSUI（Microsoft Active Accessibility）
        /// </summary>
        /// <param name="dictPegProperties">包含元素属性的字典</param>
        /// <param name="strError">错误信息引用</param>
        /// <param name="language">语言代码，默认为"en"（英语）</param>
        /// <returns>如果dictPegProperties中存在key为"Catalog"且value为"MARSUI"则返回true，否则返回false</returns>
        public static bool ISUsingMARSUI(Dictionary<string, string> dictPegProperties, ref string strError, string language = "en")
        {
            try
            {
                // 检查输入参数
                if (dictPegProperties == null)
                {
                    strError = GetErrorMessage("null_parameter", language, "dictPegProperties");
                    return false;
                }

                // 检查是否存在Catalog键
                if (dictPegProperties.ContainsKey("Catalog"))
                {
                    string catalogValue = dictPegProperties["Catalog"];

                    // 检查Catalog的值是否为MARSUI
                    if (string.Equals(catalogValue, "MARSUI", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    else
                    {
                        strError = GetErrorMessage("invalid_catalog_value", language, catalogValue);
                        return false;
                    }
                }
                else
                {
                    strError = GetErrorMessage("missing_catalog_key", language);
                    return false;
                }
            }
            catch (Exception ex)
            {
                strError = GetErrorMessage("exception_occurred", language, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 获取多语言错误信息
        /// </summary>
        /// <param name="errorKey">错误键</param>
        /// <param name="language">语言代码</param>
        /// <param name="parameters">错误信息参数</param>
        /// <returns>本地化的错误信息</returns>
        private static string GetErrorMessage(string errorKey, string language, params string[] parameters)
        {
            // 默认语言为英语
            if (string.IsNullOrEmpty(language))
                language = "en";

            // 错误信息字典
            var errorMessages = new Dictionary<string, Dictionary<string, string>>
            {
                ["null_parameter"] = new Dictionary<string, string>
                {
                    ["en"] = "Parameter cannot be null: {0}",
                    ["zh"] = "参数不能为null: {0}",
                    ["zh-CN"] = "参数不能为null: {0}",
                    ["ja"] = "パラメータがnullです: {0}",
                    ["ko"] = "매개변수가 null입니다: {0}",
                    ["fr"] = "Le paramètre ne peut pas être null: {0}",
                    ["de"] = "Parameter darf nicht null sein: {0}",
                    ["es"] = "El parámetro no puede ser null: {0}",
                    ["ru"] = "Параметр не может быть null: {0}"
                },
                ["invalid_catalog_value"] = new Dictionary<string, string>
                {
                    ["en"] = "Catalog value is not MARSUI, current value: {0}",
                    ["zh"] = "Catalog的值不是MARSUI，当前值为: {0}",
                    ["zh-CN"] = "Catalog的值不是MARSUI，当前值为: {0}",
                    ["ja"] = "Catalogの値がMARSUIではありません。現在の値: {0}",
                    ["ko"] = "Catalog 값이 MARSUI가 아닙니다. 현재 값: {0}",
                    ["fr"] = "La valeur du catalogue n'est pas MARSUI, valeur actuelle: {0}",
                    ["de"] = "Katalogwert ist nicht MARSUI, aktueller Wert: {0}",
                    ["es"] = "El valor del catálogo no es MARSUI, valor actual: {0}",
                    ["ru"] = "Значение каталога не MARSUI, текущее значение: {0}"
                },
                ["missing_catalog_key"] = new Dictionary<string, string>
                {
                    ["en"] = "Catalog key does not exist in dictPegProperties",
                    ["zh"] = "dictPegProperties中不存在Catalog键",
                    ["zh-CN"] = "dictPegProperties中不存在Catalog键",
                    ["ja"] = "dictPegPropertiesにCatalogキーが存在しません",
                    ["ko"] = "dictPegProperties에 Catalog 키가 존재하지 않습니다",
                    ["fr"] = "La clé Catalog n'existe pas dans dictPegProperties",
                    ["de"] = "Catalog-Schlüssel existiert nicht in dictPegProperties",
                    ["es"] = "La clave Catalog no existe en dictPegProperties",
                    ["ru"] = "Ключ Catalog не существует в dictPegProperties"
                },
                ["exception_occurred"] = new Dictionary<string, string>
                {
                    ["en"] = "Exception occurred while checking MARSUI: {0}",
                    ["zh"] = "检查MARSUI时发生异常: {0}",
                    ["zh-CN"] = "检查MARSUI时发生异常: {0}",
                    ["ja"] = "MARSUIのチェック中に例外が発生しました: {0}",
                    ["ko"] = "MARSUI 확인 중 예외가 발생했습니다: {0}",
                    ["fr"] = "Exception survenue lors de la vérification de MARSUI: {0}",
                    ["de"] = "Ausnahme beim Überprüfen von MARSUI aufgetreten: {0}",
                    ["es"] = "Excepción ocurrida al verificar MARSUI: {0}",
                    ["ru"] = "Исключение при проверке MARSUI: {0}"
                }
            };

            // 获取指定语言的错误信息，如果不存在则使用英语
            if (!errorMessages.ContainsKey(errorKey))
                return $"Unknown error key: {errorKey}";

            var languageMessages = errorMessages[errorKey];
            if (!languageMessages.ContainsKey(language))
                language = "en"; // 回退到英语

            string messageTemplate = languageMessages[language];

            // 格式化消息
            if (parameters != null && parameters.Length > 0)
            {
                try
                {
                    return string.Format(messageTemplate, parameters);
                }
                catch
                {
                    return messageTemplate; // 如果格式化失败，返回原始模板
                }
            }

            return messageTemplate;
        }

        //public static bool IsHwndStandardOrAFX(int hwnd)
        //{
        //    if (hwnd == 0) return false;
        //    string className = "";
        //    try
        //    {
        //        var sb = new StringBuilder(256);
        //        int ret = MarsWindowsAPIs.GetClassName((IntPtr)hwnd, sb, sb.Capacity);
        //        if (ret > 0) className = sb.ToString();
        //    }
        //    catch { return false; }
        //    if (string.IsNullOrEmpty(className)) return false;
        //    // 常见的标准类名
        //    var standardClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        //    {
        //        "Button", "Edit", "ComboBox", "ListBox", "SysListView32", "SysTreeView32",
        //        "DirectUIHWND", "Internet Explorer_Server", "Shell_TrayWnd", "WorkerW",
        //        "MsgrIMEWindowClass", "Afx:400000:0", "Afx:400000:8", "Afx:400000:9",
        //        "AfxWndW", "AfxFrameOrView42u", "AfxControlBar42u"
        //    };
        //    if (standardClasses.Contains(className)) return true;
        //    // AFX 开头的类名
        //    if (className.StartsWith("Afx", StringComparison.OrdinalIgnoreCase)) return true;
        //    return false;
        //}

        /// <summary>
        /// 返回从点命中的元素向上的 MARSUI 祖先链（从顶层 Window 开始到命中元素），便于构建 MARSUI 路径
        /// accessbileLst 为需要用IAccessible接口处理的元素列表，列表中AutomationElement对象是parent
        /// <paramref name="targetUIObject"/>返回命中的元素</param> 
        /// </summary>
        public static List<AutomationElement> GetElementChainFromPoint(int x, int y, List<AutomationElement> siblings,
            List<(AutomationElement, IAccessible)> accessbileLst,
            ref AutomationElement targetUIObject)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("GetElementChainFromPoint", $"{iMark}|{x}-{y}");

            EnsureStaThread();
            AutomationElement elem = SafeGetElementFromPoint(x, y, iMark, "GetElementChainFromPoint");

            if (elem == null)
            {
                MarsLoggerSimple.Warning("GetElementChainFromPoint", $"{iMark}|Could not get element from point ({x},{y}), returning empty list");
                return new List<AutomationElement>();
            }

            var walker = TreeWalker.RawViewWalker;
            var chain = new List<AutomationElement>();

            // 回溯到顶层（不断取 parent，直到 null），最后再反转，确保 parent 在上、child 在下
            AutomationElement cur = elem;
            targetUIObject = elem;

            // 安全获取元素名称，避免 COM 互操作错误
            string elementName = "Unknown";
            try
            {
                elementName = elem.Current.Name ?? "Unnamed";
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                MarsLoggerSimple.Warning("GetElementChainFromPoint", $"{iMark}|COM error getting element name: {comEx.Message}");
                elementName = "COM_Error";
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("GetElementChainFromPoint", $"{iMark}|Error getting element name: {ex.Message}");
                elementName = "Error";
            }

            MarsLoggerSimple.Info("GetElementChainFromPoint", $"{iMark}|Starting traversal from element: {elementName}");

            bool isDrectParent = true;
            MARSAccessibleProvider accessibleProvider = new MARSAccessibleProvider();
            int level = 0;
            while (cur != null)
            {
                chain.Insert(0, cur);

                // 安全获取当前元素信息
                string currentName = "Unknown";
                string controlType = "Unknown";
                try
                {
                    currentName = cur.Current.Name ?? "Unnamed";
                    controlType = cur.Current.ControlType?.LocalizedControlType ?? "Unknown";
                }
                catch (System.Runtime.InteropServices.COMException comEx)
                {
                    MarsLoggerSimple.Warning("GetElementChainFromPoint", $"{iMark}|COM error getting element info at level {level}: {comEx.Message}");
                    currentName = "COM_Error";
                    controlType = "COM_Error";
                }
                catch (Exception ex)
                {
                    MarsLoggerSimple.Warning("GetElementChainFromPoint", $"{iMark}|Error getting element info at level {level}: {ex.Message}");
                    currentName = "Error";
                    controlType = "Error";
                }

                MarsLoggerSimple.Info("GetElementChainFromPoint", $"{iMark}|Level {level}: {currentName} ({controlType})");

                AutomationElement parent = null!;
                try
                {
                    parent = walker.GetParent(cur);
                    if (parent != null)
                    {
                        // 安全获取父元素信息
                        string parentName = "Unknown";
                        string parentControlType = "Unknown";
                        try
                        {
                            parentName = parent.Current.Name ?? "Unnamed";
                            parentControlType = parent.Current.ControlType?.LocalizedControlType ?? "Unknown";
                        }
                        catch (System.Runtime.InteropServices.COMException comEx)
                        {
                            MarsLoggerSimple.Warning("GetElementChainFromPoint", $"{iMark}|COM error getting parent info: {comEx.Message}");
                            parentName = "COM_Error";
                            parentControlType = "COM_Error";
                        }
                        catch (Exception ex)
                        {
                            MarsLoggerSimple.Warning("GetElementChainFromPoint", $"{iMark}|Error getting parent info: {ex.Message}");
                            parentName = "Error";
                            parentControlType = "Error";
                        }

                        MarsLoggerSimple.Info("GetElementChainFromPoint", $"{iMark}|Got parent: {parentName} ({parentControlType})");
                    }
                }
                catch (Exception ex)
                {
                    MarsLoggerSimple.Error("GetElementChainFromPoint", $"{iMark}|Error getting parent at level {level}: {ex.Message}", ex);
                    parent = null!;
                }

                if (parent == null)
                {
                    MarsLoggerSimple.Info("GetElementChainFromPoint", $"{iMark}|Parent is null, stopping traversal at level {level}");
                    break;
                }

                if (IsDesktopElement(parent))
                {
                    MarsLoggerSimple.Info("GetElementChainFromPoint", $"{iMark}|Reached desktop element, stopping traversal at level {level}");
                    break;
                }

                if (Equals(parent, AutomationElement.RootElement))
                {
                    MarsLoggerSimple.Info("GetElementChainFromPoint", $"{iMark}|Reached root element, stopping traversal at level {level}");
                    break;
                }

                cur = parent;
                level++;
                if (isDrectParent)
                {
                    isDrectParent = false;
                    MarsLoggerSimple.Info("GetElementChainFromPoint", $"{iMark}|Processing direct parent at level {level}");

                    var kids = parent.FindAll(TreeScope.Children, Condition.TrueCondition);
                    MarsLoggerSimple.Info("GetElementChainFromPoint", $"{iMark}|Found {kids?.Count ?? 0} children for direct parent");

                    if ((kids == null) || (kids.Count == 0))
                    {
                        /// 判断hwndclass是否Standard的，包括AFX开头的类
                        /// 
                        int curHwnd = parent.Current.NativeWindowHandle;
                        MarsLoggerSimple.Info("GetElementChainFromPoint", $"{iMark}|No MARSUI children, checking IAccessible for HWND: 0x{curHwnd:X}");

                        /// 是否有IAccessible对象
                        /// 
                        try
                        {
                            var acc = accessibleProvider.GetAccessibleObject((IntPtr)curHwnd);
                            if ((acc != null) && (acc is IAccessible accOfMARSUI))
                            {
                                accessbileLst.Add((parent, accOfMARSUI));

                                // 安全获取父元素名称
                                string parentName = "Unknown";
                                try
                                {
                                    parentName = parent.Current.Name ?? "Unnamed";
                                }
                                catch (System.Runtime.InteropServices.COMException)
                                {
                                    parentName = "COM_Error";
                                }
                                catch (Exception)
                                {
                                    parentName = "Error";
                                }

                                MarsLoggerSimple.Info("GetElementChainFromPoint", $"{iMark}|Successfully added IAccessible object for parent: {parentName}");
                            }
                            else
                            {
                                /// 记录发生了什么
                                /// 

                                // 安全获取父元素详细信息
                                string parentName = "Unknown";
                                string parentId = "Unknown";
                                string parentClassName = "Unknown";
                                string parentControlType = "Unknown";

                                try
                                {
                                    parentName = parent.Current.Name ?? "Unnamed";
                                    parentId = parent.Current.AutomationId ?? "Unnamed";
                                    parentClassName = parent.Current.ClassName ?? "Unnamed";
                                    parentControlType = parent.Current.ControlType?.LocalizedControlType ?? "Unknown";
                                }
                                catch (System.Runtime.InteropServices.COMException)
                                {
                                    parentName = "COM_Error";
                                    parentId = "COM_Error";
                                    parentClassName = "COM_Error";
                                    parentControlType = "COM_Error";
                                }
                                catch (Exception)
                                {
                                    parentName = "Error";
                                    parentId = "Error";
                                    parentClassName = "Error";
                                    parentControlType = "Error";
                                }

                                MarsLoggerSimple.Info("GetElementChainFromPoint", $"{iMark}|window is not UIElement and not IAccessible|parent is|{parentName}|id|{parentId}|{parentClassName}|controlType|{parentControlType}|object|{acc}");
                            }
                        }
                        catch (Exception ex)
                        {
                            MarsLoggerSimple.Error("GetElementChainFromPoint", $"{iMark}|Error getting IAccessible object for HWND 0x{curHwnd:X}: {ex.Message}", ex);
                        }
                    }

                    try
                    {
                        for (int i = 0; i < kids.Count; i++)
                        {
                            var child = kids[i];
                            if (!Automation.Compare(child, elem))
                            {
                                siblings.Add(child);

                                // 安全获取子元素信息
                                string childName = "Unknown";
                                string childControlType = "Unknown";
                                try
                                {
                                    childName = child.Current.Name ?? "Unnamed";
                                    childControlType = child.Current.ControlType?.LocalizedControlType ?? "Unknown";
                                }
                                catch (System.Runtime.InteropServices.COMException comEx)
                                {
                                    MarsLoggerSimple.Warning("GetElementChainFromPoint", $"{iMark}|COM error getting child info: {comEx.Message}");
                                    childName = "COM_Error";
                                    childControlType = "COM_Error";
                                }
                                catch (Exception ex)
                                {
                                    MarsLoggerSimple.Warning("GetElementChainFromPoint", $"{iMark}|Error getting child info: {ex.Message}");
                                    childName = "Error";
                                    childControlType = "Error";
                                }

                                MarsLoggerSimple.Info("GetElementChainFromPoint", $"{iMark}|Added sibling: {childName} ({childControlType})");
                            }
                        }
                        MarsLoggerSimple.Info("GetElementChainFromPoint", $"{iMark}|Added {siblings.Count} siblings total");
                    }
                    catch (Exception ex)
                    {
                        MarsLoggerSimple.Error("GetElementChainFromPoint", $"{iMark}|Error processing siblings: {ex.Message}", ex);
                    }
                }
            }
            //chain.Reverse();
            MarsLoggerSimple.Info("GetElementChainFromPoint", $"{iMark}|Successfully constructed chain with {chain.Count} elements");
            MarsLoggerSimple.logEnd("GetElementChainFromPoint", $"{iMark}|chainCount|{chain.Count}|siblingsCount|{siblings.Count}|accessibleCount|{accessbileLst.Count}");
            return chain;
        }

        /// <summary>
        /// 从屏幕点 (x,y) 命中元素的“直接父亲”处，获取其所有同层级的兄弟元素及其递归子元素（形成父->siblings 的子树）
        /// 返回父元素，以及展平的子树列表（父节点的所有直接子元素及其后代）。
        /// </summary>
        public static (AutomationElement? parent, List<AutomationElement> subtree) GetParentSiblingsTreeFromPoint(
            int x, int y, bool includeOffscreen = true, int maxDepth = -1)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("GetParentSiblingsTreeFromPoint", $"{iMark}|{x}-{y}");

            EnsureStaThread();
            var result = new List<AutomationElement>();
            AutomationElement elem = SafeGetElementFromPoint(x, y, iMark, "GetParentSiblingsTreeFromPoint");

            if (elem == null)
            {
                MarsLoggerSimple.Warning("GetParentSiblingsTreeFromPoint", $"{iMark}|Could not get element from point ({x},{y}), returning null parent and empty list");
                return (null, result);
            }

            var walker = TreeWalker.ControlViewWalker;
            AutomationElement parent = null!;
            try { parent = walker.GetParent(elem); } catch { parent = null!; }
            if (parent == null) return (null, result);

            Condition cond = includeOffscreen
                ? Condition.TrueCondition
                : new PropertyCondition(AutomationElement.IsOffscreenProperty, false);

            AutomationElementCollection siblings = null!;
            try { siblings = parent.FindAll(TreeScope.Children, cond); } catch { }
            if (siblings == null || siblings.Count == 0) return (parent, result);

            for (int i = 0; i < siblings.Count; i++)
            {
                var sib = siblings[i];
                if (sib == null) continue;
                CollectSubtree(sib, 0, maxDepth, includeOffscreen, result);
            }

            return (parent, result);
        }

        private static void CollectSubtree(AutomationElement node, int depth, int maxDepth, bool includeOffscreen, List<AutomationElement> acc)
        {
            if (node == null) return;
            acc.Add(node);
            if (maxDepth >= 0 && depth >= maxDepth) return;

            Condition cond = includeOffscreen
                ? Condition.TrueCondition
                : new PropertyCondition(AutomationElement.IsOffscreenProperty, false);

            AutomationElementCollection kids = null!;
            try { kids = node.FindAll(TreeScope.Children, cond); } catch { }
            if (kids == null || kids.Count == 0) return;

            for (int i = 0; i < kids.Count; i++)
            {
                var child = kids[i];
                if (child == null) continue;
                CollectSubtree(child, depth + 1, maxDepth, includeOffscreen, acc);
            }
        }

        private static AutomationElement? GetTopWindowFor(AutomationElement elem)
        {
            var walker = TreeWalker.ControlViewWalker;
            AutomationElement cur = elem;
            AutomationElement top = elem;
            while (cur != null)
            {
                top = cur;
                AutomationElement parent = null!;
                try { parent = walker.GetParent(cur); } catch { parent = null!; }
                if (parent == null) break;
                bool isWindow = false;
                try
                {
                    isWindow = (parent.Current.ControlType != null && parent.Current.ControlType.Id == 50032 /*Window*/);
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    // COM 错误时默认为 false
                    isWindow = false;
                }
                catch (Exception)
                {
                    // 其他错误时默认为 false
                    isWindow = false;
                }
                cur = parent;
                if (isWindow) { top = parent; break; }
            }
            return top;
        }

        /// <summary>
        /// 从任意 MARSUI 元素为根遍历 ControlView 树
        /// </summary>
        private static void DumpTreeFromElement(AutomationElement root, TextWriter writer, bool includeOffscreen, int maxDepth)
        {
            Condition cond = includeOffscreen
                ? Condition.TrueCondition
                : new PropertyCondition(AutomationElement.IsOffscreenProperty, false);

            var q = new Queue<(AutomationElement elem, int depth, string parentPath)>();
            q.Enqueue((root, 0, ""));

            while (q.Count > 0)
            {
                var (elem, depth, parentPath) = q.Dequeue();
                if (maxDepth >= 0 && depth > maxDepth) continue;

                var info = ReadCoreInfo(elem);
                string segment = BuildSegment(elem, info);
                string fullPath = string.IsNullOrEmpty(parentPath) ? segment : parentPath + "/" + segment;
                PrintOne(writer, depth, info, fullPath);

                AutomationElementCollection kids = null!;
                try { kids = elem.FindAll(TreeScope.Children, cond); } catch { }
                if (kids == null || kids.Count == 0) continue;

                var bucketIndex = new Dictionary<string, int>(StringComparer.Ordinal);
                int len = kids.Count;
                for (int i = 0; i < len; i++)
                {
                    AutomationElement child = null!;
                    try { child = kids[i]; } catch { }
                    if (child == null) continue;

                    var childInfo = ReadCoreInfo(child);
                    string bucketKey = $"{childInfo.ControlType}:{childInfo.ClassName}:{childInfo.AutomationId}";
                    if (!bucketIndex.TryGetValue(bucketKey, out int n)) n = 0;
                    bucketIndex[bucketKey] = n + 1;
                    childInfo.RuntimeTagSiblingIndex = n;
                    CacheInfo(child, childInfo);

                    q.Enqueue((child, depth + 1, fullPath));
                }
            }
        }
        /// <summary>
        /// 从指定 hwnd 作为根，BFS 遍历 MARSUI 树（ControlView），
        /// 打印每个元素的唯一性/识别性强的字段与路径。
        /// </summary>
        /// <param name="hwnd">窗口句柄（根）</param>
        /// <param name="writer">输出（默认 Console.Out）</param>
        /// <param name="includeOffscreen">是否包含屏幕外元素</param>
        /// <param name="maxDepth">最大深度（根=0；-1 为不限）</param>
        public static void DumpTreeFromHwnd(IntPtr hwnd, TextWriter? writer = null, bool includeOffscreen = true, int maxDepth = -1)
        {
            EnsureStaThread();

            writer ??= Console.Out;

            AutomationElement root;
            try
            {
                root = AutomationElement.FromHandle(hwnd);
            }
            catch (Exception ex)
            {
                writer.WriteLine($"[ERR] ElementFromHandle failed: {ex.Message}");
                return;
            }

            Condition cond = includeOffscreen
                ? Condition.TrueCondition
                : new PropertyCondition(AutomationElement.IsOffscreenProperty, false);

            var q = new Queue<(AutomationElement elem, int depth, string parentPath)>();
            q.Enqueue((root, 0, ""));

            while (q.Count > 0)
            {
                var (elem, depth, parentPath) = q.Dequeue();
                if (maxDepth >= 0 && depth > maxDepth) continue;

                var info = ReadCoreInfo(elem);
                string segment = BuildSegment(elem, info);
                string fullPath = string.IsNullOrEmpty(parentPath) ? segment : parentPath + "/" + segment;
                PrintOne(writer, depth, info, fullPath);

                AutomationElementCollection kids = null!;
                try { kids = elem.FindAll(TreeScope.Children, cond); } catch { }
                if (kids == null || kids.Count == 0) continue;

                var bucketIndex = new Dictionary<string, int>(StringComparer.Ordinal);
                int len = kids.Count;
                for (int i = 0; i < len; i++)
                {
                    AutomationElement child = null!;
                    try { child = kids[i]; } catch { }
                    if (child == null) continue;

                    var childInfo = ReadCoreInfo(child);
                    string bucketKey = $"{childInfo.ControlType}:{childInfo.ClassName}:{childInfo.AutomationId}";
                    if (!bucketIndex.TryGetValue(bucketKey, out int n)) n = 0;
                    bucketIndex[bucketKey] = n + 1;
                    childInfo.RuntimeTagSiblingIndex = n; // 0-based
                    CacheInfo(child, childInfo);

                    q.Enqueue((child, depth + 1, fullPath));
                }
            }
        }

        // ----------------- 信息模型与读取 -----------------

        private class CoreInfo
        {
            public string Name = "";
            public string AutomationId = "";
            public string ClassName = "";
            public string FrameworkId = "";
            public string ControlTypeText = "";
            public int ControlType = 0;
            public int ProcessId = 0;
            public int NativeWindowHandle = 0;
            public string RuntimeId = "";
            public MARSUIBounds Bounds;
            public int RuntimeTagSiblingIndex = -1; // 为 sibling 编号临时使用
        }

        private struct MARSUIBounds
        {
            public double Left, Top, Right, Bottom;
            public override string ToString()
                => $"[{Left:0},{Top:0},{Right:0},{Bottom:0}]";
        }

        private static CoreInfo ReadCoreInfo(AutomationElement e)
        {
            var ci = new CoreInfo();
            try { ci.Name = e.Current.Name ?? ""; } catch (System.Runtime.InteropServices.COMException) { ci.Name = "COM_Error"; } catch { ci.Name = "Error"; }
            try { ci.AutomationId = e.Current.AutomationId ?? ""; } catch (System.Runtime.InteropServices.COMException) { ci.AutomationId = "COM_Error"; } catch { ci.AutomationId = "Error"; }
            try { ci.ClassName = e.Current.ClassName ?? ""; } catch (System.Runtime.InteropServices.COMException) { ci.ClassName = "COM_Error"; } catch { ci.ClassName = "Error"; }
            try { ci.FrameworkId = e.Current.FrameworkId ?? ""; } catch (System.Runtime.InteropServices.COMException) { ci.FrameworkId = "COM_Error"; } catch { ci.FrameworkId = "Error"; }
            try { ci.ControlType = e.Current.ControlType != null ? e.Current.ControlType.Id : 0; } catch (System.Runtime.InteropServices.COMException) { ci.ControlType = 0; } catch { ci.ControlType = 0; }
            ci.ControlTypeText = ControlTypeToString(ci.ControlType);

            try { ci.ProcessId = e.Current.ProcessId; } catch { }
            try { ci.NativeWindowHandle = e.Current.NativeWindowHandle; } catch { }

            try
            {
                var arr = e.GetRuntimeId();
                if (arr != null && arr.Length > 0)
                    ci.RuntimeId = string.Join("-", arr);
            }
            catch { }

            try
            {
                var r = e.Current.BoundingRectangle;
                ci.Bounds = new MARSUIBounds { Left = r.Left, Top = r.Top, Right = r.Right, Bottom = r.Bottom };
            }
            catch { }

            return ci;
        }

        // ----------------- 路径片段构建 -----------------

        // 以 RuntimeId 字符串为缓存键
        private static readonly Dictionary<string, CoreInfo> _cache = new(StringComparer.Ordinal);
        private static void CacheInfo(AutomationElement e, CoreInfo info)
        {
            if (!string.IsNullOrEmpty(info.RuntimeId))
                _cache[info.RuntimeId] = info;
        }

        private static bool TryGetCached(AutomationElement e, out CoreInfo info)
        {
            info = default!;
            try
            {
                var arr = e.GetRuntimeId();
                if (arr != null && arr.Length > 0)
                {
                    var key = string.Join("-", arr);
                    return _cache.TryGetValue(key, out info);
                }
            }
            catch { }
            return false;
        }

        private static string BuildSegment(AutomationElement e, CoreInfo fallback)
        {
            // 优先使用缓存（含 sibling index）
            if (!TryGetCached(e, out var ci)) ci = fallback;

            // 1) AutomationId 最优（若存在）
            if (!string.IsNullOrEmpty(ci.AutomationId))
                return $"{ci.ControlTypeText}[@AutomationId='{Escape(ci.AutomationId)}']";

            // 2) 其次：ClassName + ControlType + 同级序号（0-based -> 1-based）
            int index = ci.RuntimeTagSiblingIndex >= 0 ? ci.RuntimeTagSiblingIndex + 1 : 1;
            string cls = string.IsNullOrEmpty(ci.ClassName) ? "?" : ci.ClassName;
            return $"{ci.ControlTypeText}[Class='{Escape(cls)}'][{index}]";
        }

        // ----------------- 打印 -----------------

        private static void PrintOne(TextWriter w, int depth, CoreInfo c, string path)
        {
            string indent = new string(' ', depth * 2);
            w.WriteLine($"{indent}- {c.ControlTypeText}  Path: {path}");
            w.WriteLine($"{indent}  Name: '{c.Name}'  AutoId: '{c.AutomationId}'  Class: '{c.ClassName}'  Framework: '{c.FrameworkId}'");
            w.WriteLine($"{indent}  Hwnd: 0x{c.NativeWindowHandle:X}  PID: {c.ProcessId}  Bounds: {c.Bounds}");
            w.WriteLine($"{indent}  RuntimeId: {c.RuntimeId}");
        }

        // ----------------- 工具 -----------------

        private static string ControlTypeToString(int id)
        {
            // 取自 MARSUI_ControlTypeIds，覆盖常见类型
            return id switch
            {
                50000 => "Button",
                50001 => "Calendar",
                50002 => "CheckBox",
                50003 => "ComboBox",
                50004 => "Edit",
                50005 => "Hyperlink",
                50006 => "Image",
                50007 => "ListItem",
                50008 => "List",
                50009 => "Menu",
                50010 => "MenuBar",
                50011 => "MenuItem",
                50012 => "ProgressBar",
                50013 => "RadioButton",
                50014 => "ScrollBar",
                50015 => "Slider",
                50016 => "Spinner",
                50017 => "StatusBar",
                50018 => "Tab",
                50019 => "TabItem",
                50020 => "Text",
                50021 => "ToolBar",
                50022 => "ToolTip",
                50023 => "Tree",
                50024 => "TreeItem",
                50025 => "Custom",
                50026 => "Group",
                50027 => "Thumb",
                50028 => "DataGrid",
                50029 => "DataItem",
                50030 => "Document",
                50031 => "SplitButton",
                50032 => "Window",
                50033 => "Pane",
                50034 => "Header",
                50035 => "HeaderItem",
                50036 => "Table",
                50037 => "TitleBar",
                50038 => "Separator",
                50039 => "SemanticZoom",
                50040 => "AppBar",
                _ => $"ControlType({id})"
            };
        }

        private static string Escape(string s) => s.Replace("'", "\\'");

        private static void EnsureStaThread()
        {
            // MARSUI/COM 建议在 STA；若非 STA 给出提示（仍然允许继续）。
            if (System.Threading.Thread.CurrentThread.GetApartmentState() != System.Threading.ApartmentState.STA)
            {
                Console.Error.WriteLine("[WARN] Current thread is not STA. Consider running on an STA thread to avoid COM/MARSUI issues.");
            }
        }

        /// <summary>
        /// 安全地获取 AutomationElement，处理 RPC_E_CANTCALLOUT_ININPUTSYNCCALL 错误
        /// </summary>
        private static AutomationElement SafeGetElementFromPoint(int x, int y, int iMark, string methodName)
        {
            try
            {
                // 首先尝试直接调用
                var elem = AutomationElement.FromPoint(new Point(x, y));
                if (elem != null)
                {
                    MarsLoggerSimple.Info(methodName, $"{iMark}|Successfully got element from point ({x},{y})");
                    return elem;
                }
                else
                {
                    MarsLoggerSimple.Warning(methodName, $"{iMark}|Got null element from point ({x},{y})");
                    return null;
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error(methodName, $"{iMark}|Failed to get element from point ({x},{y}): {ex.Message}", ex);

                // 如果是 RPC_E_CANTCALLOUT_ININPUTSYNCCALL 错误，尝试多种解决方案
                if (ex.Message.Contains("0x8001010D") || ex.Message.Contains("RPC_E_CANTCALLOUT_ININPUTSYNCCALL"))
                {
                    MarsLoggerSimple.Info(methodName, $"{iMark}|Detected RPC_E_CANTCALLOUT_ININPUTSYNCCALL, attempting solutions");

                    // 方案1: 延迟重试
                    try
                    {
                        System.Threading.Thread.Sleep(50);
                        var elem1 = AutomationElement.FromPoint(new Point(x, y));
                        if (elem1 != null)
                        {
                            MarsLoggerSimple.Info(methodName, $"{iMark}|Delayed retry successful");
                            return elem1;
                        }
                    }
                    catch (Exception ex1)
                    {
                        MarsLoggerSimple.Error(methodName, $"{iMark}|Delayed retry failed: {ex1.Message}", ex1);
                    }

                    // 方案2: 使用 Application.DoEvents() 处理消息队列
                    try
                    {
                        System.Windows.Forms.Application.DoEvents();
                        var elem2 = AutomationElement.FromPoint(new Point(x, y));
                        if (elem2 != null)
                        {
                            MarsLoggerSimple.Info(methodName, $"{iMark}|DoEvents retry successful");
                            return elem2;
                        }
                    }
                    catch (Exception ex2)
                    {
                        MarsLoggerSimple.Warning(methodName, $"{iMark}|DoEvents retry failed: {ex2.Message}");
                    }

                    // 方案3: 使用 Task.Run 在后台线程执行
                    try
                    {
                        var task = Task.Run(() => AutomationElement.FromPoint(new Point(x, y)));
                        task.Wait(1000); // 等待最多1秒
                        if (task.IsCompleted && task.Result != null)
                        {
                            MarsLoggerSimple.Info(methodName, $"{iMark}|Task.Run retry successful");
                            return task.Result;
                        }
                    }
                    catch (Exception ex3)
                    {
                        MarsLoggerSimple.Warning(methodName, $"{iMark}|Task.Run retry failed: {ex3.Message}");
                    }

                    MarsLoggerSimple.Error(methodName, $"{iMark}|All retry attempts failed for RPC_E_CANTCALLOUT_ININPUTSYNCCALL");
                }

                return null;
            }
        }

        #region code from chatgpt
        public static void DumpAfxTree(IntPtr hwndAfx, string outFile)
        {
            EnsureSta();

            var root = AutomationElement.FromHandle(hwndAfx);
            using var w = new StreamWriter(outFile, false, System.Text.Encoding.UTF8);

            w.WriteLine($"# MARSUI Dump for Afx HWND=0x{hwndAfx.ToInt64():X}  {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            w.WriteLine();

            // 先试 ControlView
            var controlChildren = GetChildren(root, view: View.Control);
            if (controlChildren.Count == 0)
            {
                w.WriteLine("// ControlView 下没有子节点，切换 RawView …");
            }

            // 遍历（先 ControlView；若某节点在 ControlView 无子，则对该节点用 RawView 兜底）
            var q = new Queue<(AutomationElement elem, int depth)>();
            q.Enqueue((root, 0));

            // Cache 常用属性：减少跨进程调用成本
            var cache = new CacheRequest();
            {
                // 重要：你要在哪些层级上一次性缓存属性
                cache.TreeScope = TreeScope.Element | TreeScope.Children;

                cache.Add(AutomationElement.NameProperty);
                cache.Add(AutomationElement.AutomationIdProperty);
                cache.Add(AutomationElement.ClassNameProperty);
                cache.Add(AutomationElement.ControlTypeProperty);
                cache.Add(AutomationElement.FrameworkIdProperty);
                cache.Add(AutomationElement.NativeWindowHandleProperty);
                cache.Add(AutomationElement.IsOffscreenProperty);
                cache.Add(AutomationElement.IsControlElementProperty);
                cache.Push();

                while (q.Count > 0)
                {
                    var (e, depth) = q.Dequeue();

                    // 打印当前节点
                    PrintOne(w, e, depth);

                    // 先按 ControlView 拿子节点
                    var kids = GetChildren(e, View.Control);
                    // ControlView 没子节点时，用 RawView 再试一次（有些 MFC 自绘控件只在 RawView 暴露结构）
                    if (kids.Count == 0)
                        kids = GetChildren(e, View.Raw);

                    foreach (var child in kids)
                        q.Enqueue((child, depth + 1));
                }
            }
        }

        // ---- 内部：获取某个元素的“直接子节点”（按视图选择） ----
        private enum View { Control, Raw }

        private static List<AutomationElement> GetChildren(AutomationElement parent, View view)
        {
            var list = new List<AutomationElement>();
            TreeWalker walker = (view == View.Control) ? TreeWalker.ControlViewWalker : TreeWalker.RawViewWalker;

            var child = walker.GetFirstChild(parent);
            while (child != null)
            {
                list.Add(child);
                child = walker.GetNextSibling(child);
            }
            return list;
        }
        static readonly AutomationPattern LegacyPattern = AutomationPattern.LookupById(10018);
        // Legacy 属性（按 ID 查）
        private static readonly AutomationProperty Legacy_NameProperty = AutomationProperty.LookupById(30092);
        private static readonly AutomationProperty Legacy_ValueProperty = AutomationProperty.LookupById(30093);
        private static readonly AutomationProperty Legacy_DescriptionProperty = AutomationProperty.LookupById(30094);
        private static readonly AutomationProperty Legacy_RoleProperty = AutomationProperty.LookupById(30095);
        private static readonly AutomationProperty Legacy_StateProperty = AutomationProperty.LookupById(30096);
        private static readonly AutomationProperty Legacy_HelpProperty = AutomationProperty.LookupById(30097);
        private static readonly AutomationProperty Legacy_KeyboardShortcutProp = AutomationProperty.LookupById(30098);
        private static readonly AutomationProperty Legacy_SelectionProperty = AutomationProperty.LookupById(30099);
        private static readonly AutomationProperty Legacy_DefaultActionProperty = AutomationProperty.LookupById(30100);
        private static readonly AutomationProperty Legacy_ChildIdProperty = AutomationProperty.LookupById(30091);
        // ---- 内部：打印一个元素的关键信息 ----
        private static void PrintOne(StreamWriter w, AutomationElement e, int depth)
        {
            string ind = new string(' ', depth * 2);

            // 是否支持 Legacy：直接尝试取模式
            object patObj;
            bool hasLegacy = e.TryGetCurrentPattern(LegacyPattern, out patObj);

            var ctObj = GetCachedOrCurrent<ControlType>(e, AutomationElement.ControlTypeProperty);
            string ct = ctObj != null ? ctObj.ProgrammaticName : "ControlType.?";
            string name = GetCachedOrCurrent<string>(e, AutomationElement.NameProperty) ?? "";
            string aid = GetCachedOrCurrent<string>(e, AutomationElement.AutomationIdProperty) ?? "";
            string cls = GetCachedOrCurrent<string>(e, AutomationElement.ClassNameProperty) ?? "";
            string fwk = GetCachedOrCurrent<string>(e, AutomationElement.FrameworkIdProperty) ?? "";
            int hwnd = GetCachedOrCurrent<int>(e, AutomationElement.NativeWindowHandleProperty);
            bool off = GetCachedOrCurrent<bool>(e, AutomationElement.IsOffscreenProperty);

            w.WriteLine($"{ind}- {ct}  Name='{Trim1(name)}'  AutoId='{aid}'  Class='{cls}'  Fwk='{fwk}'  Hwnd=0x{hwnd:X}");

            if (hasLegacy)
            {
                // 通过元素读取 Legacy 属性（不依赖 legacy.Current）
                string lName = GetCachedOrCurrent<string>(e, Legacy_NameProperty) ?? "";
                string lVal = GetCachedOrCurrent<string>(e, Legacy_ValueProperty) ?? "";
                string lAct = GetCachedOrCurrent<string>(e, Legacy_DefaultActionProperty) ?? "";
                int role = GetCachedOrCurrent<int>(e, Legacy_RoleProperty);
                string roleText = MARSAccessibleProvider.GetRoleName(role);

                w.WriteLine($"{ind}  Offscreen={off}  LegacyIAccessible=Yes  Role={role}({roleText})  LName='{Trim1(lName)}'  LValue='{Trim1(lVal)}'  LDefAct='{Trim1(lAct)}'");
            }
            else
            {
                w.WriteLine($"{ind}  Offscreen={off}  LegacyIAccessible=No");
            }
        }

        static string Trim1(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\r", " ").Replace("\n", " ");
            return s.Length > 120 ? s.Substring(0, 120) + "…" : s;
        }

        // 读 Cached->Current，避免 NotSupported
        static T GetCachedOrCurrent<T>(AutomationElement e, AutomationProperty p)
        {
            try
            {
                object v = e.GetCachedPropertyValue(p, true);
                if (v != AutomationElement.NotSupported) return (T)v;
            }
            catch { }
            try
            {
                object v = e.GetCurrentPropertyValue(p, true);
                if (v != AutomationElement.NotSupported) return (T)v;
            }
            catch { }
            return default(T);
        }


        private static T Safe<T>(Func<T> f)
        {
            try { return f(); } catch { return default!; }
        }

        private static string Trim(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\r", " ").Replace("\n", " ");
            return s.Length > 120 ? s.Substring(0, 120) + "…" : s;
        }

        private static void EnsureSta()
        {
            var ap = System.Threading.Thread.CurrentThread.GetApartmentState();
            if (ap != System.Threading.ApartmentState.STA)
                Console.Error.WriteLine("[WARN] MARSUI 建议在 STA 线程调用。");
        }
        #endregion

        /// <summary>
        /// MARSUI Pegwindow方法 - 查找MARSUI对象
        /// </summary>
        /// <param name="stepId">步骤ID</param>
        /// <param name="dictPegProperties">Peg窗口属性</param>
        /// <param name="dictObjProperties">对象属性</param>
        /// <param name="strParaMeter">参数字符串</param>
        /// <param name="strData">数据参数</param>
        /// <param name="typeName">对象类型名称</param>
        /// <param name="strAttachInfo">附加信息</param>
        /// <param name="pegWindName">Peg窗口名称</param>
        /// <param name="objName">对象名称</param>
        /// <param name="strError">错误信息引用</param>
        /// <param name="dealResult">处理结果引用</param>
        /// <returns>如果找到MARSUI对象则返回true，否则返回false</returns>
        public static bool MARSUI_Pegwindow(long stepId, Dictionary<string, string> dictPegProperties,
            Dictionary<string, string> dictObjProperties, string strParaMeter, string strData, string typeName, string strAttachInfo,
            string pegWindName, string objName, ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("MARSUI_Pegwindow", $"{iMark}|stepId: {stepId}|{pegWindName}.{objName}");

            try
            {
                // 1. 提供log
                MarsLoggerSimple.Info("MARSUI_Pegwindow", $"{iMark}|Starting MARSUI Pegwindow search for {pegWindName}.{objName}");

                bool isOk = true;
                /// 增加对现有peg对象的缓存
                /// 
                if (PegWindowUtilites.isCacheAsEnabled(strParaMeter))
                {
                    MarsSpiedObjectInfo pegToCache = MARSUIAppSideVariables.GetInstance() == null ? null : MARSUIAppSideVariables.GetInstance().currentPegwindow as MarsSpiedObjectInfo;
                    if (pegToCache == null)
                    {
                        strError = "No current PegWindow object to cache";
                        MarsLoggerSimple.Error("MARSUI_Pegwindow", $"{iMark}|{strError}|{Environment.StackTrace}");
                        dealResult = new MARSDealResult
                        {
                            ResultMessage = $"FAILED,{strError}",
                            ErrorMessage = strError
                        };
                        return false;
                    }
                    isOk = PegWindowUtilites.addToCache(strData, pegToCache, ref strError);
                    if (!isOk)
                    {
                        MarsLoggerSimple.Error("MARSUI_Pegwindow", $"{iMark}|Failed to cache PegWindow: {strError}|{Environment.StackTrace}");
                        dealResult = new MARSDealResult
                        {
                            ResultMessage = $"FAILED,{strError}",
                            ErrorMessage = strError
                        };
                        return false;
                    }
                    else
                    {
                        MarsLoggerSimple.Info("MARSUI_Pegwindow", $"{iMark}|Successfully cached PegWindow");
                        dealResult = new MARSDealResult
                        {
                            ResultMessage = "SUCCESS",
                            ErrorMessage = "",
                            ReturnedData = $"Cached|{strData}",
                            ActualInputData = strData
                        };
                        return true;
                    }
                }

                if (PegWindowUtilites.isToRestore(strParaMeter))
                {
                    MarsSpiedObjectInfo restoredPeg = PegWindowUtilites.restoreFromCache(strData, ref strError);
                    if (restoredPeg == null)
                    {
                        MarsLoggerSimple.Error("MARSUI_Pegwindow", $"{iMark}|Failed to restore PegWindow from cache: {strError}|{Environment.StackTrace}");
                        dealResult = new MARSDealResult
                        {
                            ResultMessage = $"FAILED,{strError}",
                            ErrorMessage = strError
                        };
                        return false;
                    }
                    else
                    {
                        var v = MARSUIAppSideVariables.GetInstance();
                        v.currentPegwindow = restoredPeg;
                        MarsLoggerSimple.Info("MARSUI_Pegwindow", $"{iMark}|Successfully restored PegWindow from cache");
                        dealResult = new MARSDealResult
                        {
                            ResultMessage = "SUCCESS",
                            ErrorMessage = "",
                            ReturnedData = restoredPeg.ToString(),
                            ActualInputData = strData
                        };
                        return true;
                    }
                }
                // 2. 通过FindPegWindow方法实现主要逻辑
                MarsSpiedObjectInfo foundObject = null;
                if (!FindPegWindow(dictPegProperties, ref foundObject, ref strError))
                {
                    MarsLoggerSimple.Error("MARSUI_Pegwindow", $"{iMark}|Failed to find PegWindow: {strError}");
                    dealResult = new MARSDealResult
                    {
                        ResultMessage = $"FAILED,{strError}",
                        ErrorMessage = strError
                    };
                    return false;
                }

                // 3. 将找到的对象存储到MARSUIKeywordsVariables
                if (foundObject != null)
                {
                    // 这里需要根据实际的MARSUIKeywordsVariables实现来存储对象
                    var v = MARSUIAppSideVariables.GetInstance();
                    v.currentPegwindow = foundObject;


                    // 暂时使用dealResult来返回找到的对象信息
                    dealResult = new MARSDealResult
                    {
                        ResultMessage = "SUCCESS",
                        ErrorMessage = "",
                        ReturnedData = foundObject.ToString()
                    };

                    MarsLoggerSimple.Info("MARSUI_Pegwindow", $"{iMark}|Successfully found MARSUI object: {foundObject.objectName}");
                    MarsLoggerSimple.logEnd("MARSUI_Pegwindow", $"{iMark}|Success");
                    return true;
                }
                else
                {
                    strError = "No MARSUI object found matching the criteria";
                    MarsLoggerSimple.Error("MARSUI_Pegwindow", $"{iMark}|{strError}");
                    dealResult = new MARSDealResult
                    {
                        ResultMessage = "FAILED",
                        ErrorMessage = strError
                    };
                    return false;
                }
            }
            catch (Exception ex)
            {
                strError = $"Exception in MARSUI_Pegwindow: {ex.Message}";
                MarsLoggerSimple.Error("MARSUI_Pegwindow", $"{iMark}|{strError}", ex);
                dealResult = new MARSDealResult
                {
                    ResultMessage = "FAILED",
                    ErrorMessage = strError
                };
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elem"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        private static bool MatchingCondition(AutomationElement elem, string key, string value, ref bool isOk, ref string strError)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("MatchingCondition", $"{iMark}|{key}-{value}");
            try
            {
                switch (key.ToLower())
                {
                    case "attachtext":
                    case "attatch text":
                    case "text":
                    case "name":
                        var tmpT = elem.Current.Name ?? "";
                        MarsLoggerSimple.Info($"\t", $"{iMark}|try to compare|{tmpT}--{value}");
                        return MarsWindowsAPIsExtend.RegularTest(value, tmpT);
                    //return string.Equals(elem.Current.Name ?? "", value, StringComparison.OrdinalIgnoreCase);
                    case "automationid":
                    case "automation id":
                        var tmpId = elem.Current.AutomationId ?? "";
                        MarsLoggerSimple.Info($"\t", $"{iMark}|try to compare|{tmpId}--{value}");
                        return MarsWindowsAPIsExtend.RegularTest(value, tmpId);
                    //return string.Equals(elem.Current.AutomationId ?? "", value, StringComparison.OrdinalIgnoreCase);
                    case "classname":
                    case "class name":
                        var tmpCls = elem.Current.ClassName ?? "";
                        MarsLoggerSimple.Info("\t", $"{iMark}|try to compare|{tmpCls}--{value}");
                        return MarsWindowsAPIsExtend.RegularTest(value, tmpCls);
                    case "controltype":
                    case "control type":
                        var ct = MapControlTypFromMarsIDS(value, ref isOk);
                        if (ct == null)
                        {
                            strError = $"Unknown ControlType specified: {value}";
                            isOk = false;
                            return false;
                        }
                        return elem.Current.ControlType != null && elem.Current.ControlType.Id == ct.Id;
                    case "frameworkid":
                    case "framework id":
                        var tmpFrm = elem.Current.FrameworkId ?? "";
                        MarsLoggerSimple.Info("\t", $"{iMark}|try to compare|{tmpFrm}--{value}");
                        return MarsWindowsAPIsExtend.RegularTest(value, tmpFrm);
                    case "winclass":
                        var curClass = elem.Current.ClassName;
                        int hwnd = elem.Current.NativeWindowHandle;
                        StringBuilder sb = new StringBuilder(256);
                        MarsWindowsAPIs.GetClassName(new IntPtr(hwnd), sb, 256);
                        MarsLoggerSimple.Info("\t", $"{iMark}|try to compare|{sb.ToString()}--{value}");
                        return MarsWindowsAPIsExtend.RegularTest(value, sb.ToString());
                    default:
                        strError = $"Unsupported property key: {key}";
                        MarsLoggerSimple.Info("\t", $"{iMark}|{strError}");
                        isOk = false;
                        return false;
                }
            }
            catch (Exception ex)
            {
                strError = $"Exception in MatchingCondition for key '{key}': {ex.Message}";
                MarsLoggerSimple.Error("MatchingCondition", $"{iMark}|{strError}", ex);
                isOk = false;
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("MatchingCondition", $"{iMark}");
            }
        }

        /// <summary>
        /// 查找PegWindow的主要实现方法
        /// </summary>
        /// <param name="dictPegProperties">Peg窗口属性字典</param>
        /// <param name="foundObject">找到的对象引用</param>
        /// <param name="strError">错误信息引用</param>
        /// <returns>如果成功找到对象则返回true，否则返回false</returns>
        private static bool FindPegWindow(Dictionary<string, string> dictPegProperties,
            ref MarsSpiedObjectInfo foundObject, ref string strError)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("FindPegWindow", $"{iMark}|{MarsWindowsAPIsExtend.Dic2String(dictPegProperties)}");

            try
            {
                bool isOk = true;
                // 3.1 首先判断dictPegProperties中是否有catalog key，以及值是否是MARSUI
                if (!ISUsingMARSUI(dictPegProperties, ref strError))
                {
                    MarsLoggerSimple.Warning("FindPegWindow", $"{iMark}|Not using MARSUI: {strError}");
                    return false;
                }

                // 3.2 判断是否有MarsNamePath的key，或者Mars Name Path(大小写不敏感），如果不存在，就默认只有一层
                string marsNamePath = "";
                if (dictPegProperties.ContainsKey("MarsNamePath"))
                {
                    marsNamePath = dictPegProperties["MarsNamePath"];
                }
                else if (dictPegProperties.ContainsKey("Mars Name Path"))
                {
                    marsNamePath = dictPegProperties["Mars Name Path"];
                }
                else
                {
                    // 默认只有一层，使用Text属性
                    marsNamePath = dictPegProperties.ContainsKey("Text") ? dictPegProperties["Text"] : "";
                }

                MarsLoggerSimple.Info("FindPegWindow", $"{iMark}|MarsNamePath: {marsNamePath}");

                // 3.3 将MarsNamePath按照";"的分割，判断有几层
                string[] pathLayers = string.IsNullOrEmpty(marsNamePath) ? new string[0] : marsNamePath.Split(';');
                int layerCount = pathLayers.Length;

                //if (layerCount == 0)
                //{
                //    strError = "No MarsNamePath specified";
                //    MarsLoggerSimple.Error("FindPegWindow", $"{iMark}|{strError}");
                //    return false;
                //}

                MarsLoggerSimple.Info("FindPegWindow", $"{iMark}|Found {layerCount} layers in MarsNamePath");

                // 3.4 循环获得层数，及每层的内容。用UIA技术寻找MARSTestProcess.CurrentTestProcessId这个process下的当前层的UIA对象
                List<MarsSpiedObjectInfo> currentLayerObjects = new List<MarsSpiedObjectInfo>();
                List<MarsSpiedObjectInfo> matchingObjects = new List<MarsSpiedObjectInfo>();

                // 获取当前测试进程ID
                int currentProcessId = MARSTestProcess.CurrentTestProcessId;
                MarsLoggerSimple.Info("FindPegWindow", $"{iMark}|Current test process ID: {currentProcessId}");

                // 使用GetProcessTopLevelUIElement快速获取顶级窗口
                if (!GetProcessTopLevelUIElement(currentProcessId, out currentLayerObjects))
                {
                    strError = $"No top-level windows found for process {currentProcessId}";
                    MarsLoggerSimple.Error("FindPegWindow", $"{iMark}|{strError}");
                    return false;
                }
                #region 从type过滤
                if (DictionaryHelper.TryGetValueIgnoreCase(dictPegProperties, "Object Type", out string pegTypeValue)
                    || DictionaryHelper.TryGetValueIgnoreCase(dictPegProperties, "ObjectType", out pegTypeValue))
                {
                    /// 说明用户指定了window的type，应该从顶级窗口中过滤
                    /// 
                    var ct = MapControlTypFromMarsIDS(pegTypeValue, ref isOk);
                    if (ct == null) {
                        strError = $"Unknown Object Type specified: {pegTypeValue}";
                        MarsLoggerSimple.Error("FindPegWindow", $"{iMark}|{strError}");
                        return false;
                    }
                    List<AutomationElement> matchedUIElement = new List<AutomationElement>();
                    foreach (var prnt in currentLayerObjects)
                    {
                        if (prnt == null) continue;
                        var prntUI = prnt.referenceToObj as AutomationElement;
                        if (prntUI == null) continue;
                        var lstObj = FilterObjectsFromParent(prntUI, ct, ref isOk, ref strError);
                        /// 添加根对象
                        /// 
                        matchedUIElement.Add(prntUI);
                        if ((!isOk) || (lstObj == null))
                        {
                            MarsLoggerSimple.Error("FindPegWindow", $"{iMark}|{strError}");
                            continue;
                        }
                        matchedUIElement.AddRange(lstObj);
                    }
                    if (matchedUIElement.Count == 0)
                    {
                        strError = $"No top-level windows found for process {currentProcessId} with Object Type {pegTypeValue}";
                        MarsLoggerSimple.Error("FindPegWindow", $"{iMark}|{strError}");
                        return false;
                    }
                    /// 判断其他属性，通常需要text
                    /// 
                    int iKeyCount = 0;
                    List<string> keys = new List<string>();
                    bool isIndxExist = false;
                    int iIdx = -1;
                    List<AutomationElement> finalMatchedUIElement = new List<AutomationElement>(matchedUIElement);

                    foreach (var kvp in dictPegProperties.Keys)
                    {
                        if (kvp.Equals("Object Type", StringComparison.OrdinalIgnoreCase)
                            || kvp.Equals("ObjectType", StringComparison.OrdinalIgnoreCase)
                            || kvp.Equals("MarsNamePath", StringComparison.OrdinalIgnoreCase)
                            || kvp.Equals("Mars Name Path", StringComparison.OrdinalIgnoreCase)
                            || kvp.Equals("Catalog", StringComparison.OrdinalIgnoreCase)
                            || kvp.Equals("MaxDepth", StringComparison.OrdinalIgnoreCase)
                            || kvp.Equals("SearchDepth", StringComparison.OrdinalIgnoreCase)
                            || kvp.Equals("Depth", StringComparison.OrdinalIgnoreCase)
                            )
                        {
                            iKeyCount++;
                            keys.Add(kvp);
                            continue;
                        }
                        if (DictionaryHelper.TryGetValueIgnoreCase(dictPegProperties, "index", out string strIdx))
                        {
                            isIndxExist = true;
                            if (!int.TryParse(strIdx, out iIdx))
                            {
                                strError = $"Invalid index value: {strIdx}";
                                MarsLoggerSimple.Error("FindPegWindow", $"{iMark}|{strError}");
                                return false;
                            }
                            iKeyCount++;

                            keys.Add(kvp);
                            continue;
                        }
                        List<AutomationElement> tmpFound = new List<AutomationElement>();
                        foreach (var uiElement in finalMatchedUIElement)
                        {
                            if (!MatchingCondition(uiElement, kvp, dictPegProperties[kvp], ref isOk, ref strError))
                            {
                                MarsLoggerSimple.Error("FindPegWindow", $"{iMark}|{strError}");
                                // 不匹配，从finalMatchedUIElement中移除
                                //finalMatchedUIElement.Remove(uiElement);
                                continue;
                            }
                            tmpFound.Add(uiElement);
                        }
                        if (tmpFound.Count == 0)
                        {
                            strError = $"No top-level windows found for process {currentProcessId} with Object Type {pegTypeValue} and property {kvp}={dictPegProperties[kvp]}";
                            MarsLoggerSimple.Error("FindPegWindow", $"{iMark}|{strError}");
                            return false;
                        }
                        finalMatchedUIElement = new List<AutomationElement>(tmpFound);
                    }
                    /// 还要判断最终匹配的数量，是否有些没有匹配到--缺
                    /// 
                    AutomationElement targetUIObj = null;
                    if (finalMatchedUIElement.Count > 1)
                    {
                        if ((iIdx < 0) || (iIdx >= finalMatchedUIElement.Count))
                        {
                            strError = $"Multiple top-level windows found for process {currentProcessId} with Object Type {pegTypeValue} and other properties, but index is not specified or out of range.";
                            MarsLoggerSimple.Error("FindPegWindow", $"{iMark}|{strError}");
                            return false;
                        }
                        // 选择指定index的对象
                        targetUIObj = finalMatchedUIElement[iIdx];
                        if (targetUIObj == null)
                        {
                            strError = $"Selected UI element at index {iIdx} is null.";
                            MarsLoggerSimple.Error("FindPegWindow", $"{iMark}|{strError}");
                            return false;
                        }
                    }
                    if (finalMatchedUIElement.Count < 0)
                    {
                        strError = $"No target object found for process {currentProcessId} with Object Type {pegTypeValue} and other properties.";
                        MarsLoggerSimple.Error("FindPegWindow", $"{iMark}|{strError}");
                        return false;
                    }
                    foundObject = GetMarsSpiedObjectInfo(finalMatchedUIElement[0]);
                    isOk = true;
                    return true;
                    //var spiedObj = ConvertAutomationElementToMarsSpiedObjectInfo(selectedUI);
                    //    if (spiedObj != null)
                    //    {
                    //        foundObject = spiedObj;
                    //        MarsLoggerSimple.Info("FindPegWindow", $"{iMark}|Selected object at index {iIdx}: {foundObject.objectName}");
                    //        return true;
                    //    }
                    //    else
                    //    {
                    //        strError = $"Failed to convert selected UI element to MarsSpiedObjectInfo.";
                    //        MarsLoggerSimple.Error("FindPegWindow", $"{iMark}|{strError}");
                    //        return false;
                    //    }


                }
                #endregion

                MarsLoggerSimple.Info("FindPegWindow", $"{iMark}|Found {currentLayerObjects.Count} top-level windows for process {currentProcessId}");

                // 逐层匹配
                for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
                {
                    string currentLayerPath = pathLayers[layerIndex].Trim();
                    MarsLoggerSimple.Info("FindPegWindow", $"{iMark}|Processing layer {layerIndex + 1}: {currentLayerPath}");

                    // 解析当前层的技术方法和名称
                    string techType = "UIA"; // 默认第一层使用UIA
                    string layerName = currentLayerPath;

                    if (currentLayerPath.Contains(":"))
                    {
                        string[] parts = currentLayerPath.Split(':');
                        if (parts.Length >= 2)
                        {
                            techType = parts[0].Trim().ToUpper();
                            layerName = parts[1].Trim();
                        }
                    }

                    MarsLoggerSimple.Info("FindPegWindow", $"{iMark}|Layer {layerIndex + 1} - TechType: {techType}, Name: {layerName}");

                    matchingObjects.Clear();

                    // 在当前层的对象中查找匹配的对象
                    foreach (var obj in currentLayerObjects)
                    {

                        if (IsObjectMatchingLayer(obj, layerName, dictPegProperties))
                        {
                            matchingObjects.Add(obj);
                            MarsLoggerSimple.Info("FindPegWindow", $"{iMark}|Found matching object: {obj.objectName}");
                        }
                    }

                    // 检查是否找到匹配的对象
                    if (matchingObjects.Count == 0)
                    {
                        strError = $"No objects found matching layer {layerIndex + 1}: {currentLayerPath}";
                        MarsLoggerSimple.Error("FindPegWindow", $"{iMark}|{strError}");
                        return false;
                    }

                    MarsLoggerSimple.Info("FindPegWindow", $"{iMark}|Found {matchingObjects.Count} matching objects for layer {layerIndex + 1}");

                    // 如果不是最后一层，准备下一层的搜索
                    if (layerIndex < layerCount - 1)
                    {
                        // 解析下一层的技术方法
                        string nextLayerPath = pathLayers[layerIndex + 1].Trim();
                        string nextTechType = "UIA"; // 默认使用UIA

                        if (nextLayerPath.Contains(":"))
                        {
                            string[] nextParts = nextLayerPath.Split(':');
                            if (nextParts.Length >= 2)
                            {
                                nextTechType = nextParts[0].Trim().ToUpper();
                            }
                        }

                        MarsLoggerSimple.Info("FindPegWindow", $"{iMark}|Next layer will use technology: {nextTechType}");

                        currentLayerObjects.Clear();

                        foreach (var matchingObj in matchingObjects)
                        {
                            List<MarsSpiedObjectInfo> childObjects = new List<MarsSpiedObjectInfo>();

                            if (nextTechType == "UIA")
                            {
                                // 使用UIA技术获取子对象
                                childObjects = GetChildUIAElements(matchingObj);
                                MarsLoggerSimple.Info("FindPegWindow", $"{iMark}|Got {childObjects.Count} UIA child objects from {matchingObj.objectName}");
                            }
                            else if (nextTechType == "STANDARD" || nextTechType == "IACC")
                            {
                                // 使用IAccessible技术获取子对象
                                childObjects = GetChildIAccessibleElements(matchingObj);
                                MarsLoggerSimple.Info("FindPegWindow", $"{iMark}|Got {childObjects.Count} IAccessible child objects from {matchingObj.objectName}");
                            }
                            else
                            {
                                MarsLoggerSimple.Warning("FindPegWindow", $"{iMark}|Unknown technology type: {nextTechType}, defaulting to UIA");
                                childObjects = GetChildUIAElements(matchingObj);
                            }

                            currentLayerObjects.AddRange(childObjects);
                        }

                        MarsLoggerSimple.Info("FindPegWindow", $"{iMark}|Total child objects for next layer: {currentLayerObjects.Count}");
                    }
                }

                // 3.5 层数循环结束后，判断是否有多个？如果是多个满足，判断dictPegProperties是否包含index
                if (matchingObjects.Count > 1)
                {
                    MarsLoggerSimple.Info("FindPegWindow", $"{iMark}|Found {matchingObjects.Count} matching objects, checking for index");

                    if (dictPegProperties.ContainsKey("index"))
                    {
                        if (int.TryParse(dictPegProperties["index"], out int indexValue))
                        {
                            if (indexValue >= 0 && indexValue < matchingObjects.Count)
                            {
                                foundObject = matchingObjects[indexValue];
                                MarsLoggerSimple.Info("FindPegWindow", $"{iMark}|Selected object at index {indexValue}: {foundObject.objectName}");
                            }
                            else
                            {
                                strError = $"Index {indexValue} is out of range. Available objects: {matchingObjects.Count}";
                                MarsLoggerSimple.Error("FindPegWindow", $"{iMark}|{strError}");
                                return false;
                            }
                        }
                        else
                        {
                            strError = "Invalid index value in dictPegProperties";
                            MarsLoggerSimple.Error("FindPegWindow", $"{iMark}|{strError}");
                            return false;
                        }
                    }
                    else
                    {
                        strError = $"Multiple objects found ({matchingObjects.Count}) but no index specified";
                        MarsLoggerSimple.Error("FindPegWindow", $"{iMark}|{strError}");
                        return false;
                    }
                }
                else
                {
                    foundObject = matchingObjects[0];
                    MarsLoggerSimple.Info("FindPegWindow", $"{iMark}|Selected single matching object: {foundObject.objectName}");
                }

                MarsLoggerSimple.logEnd("FindPegWindow", $"{iMark}|Success");
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Exception in FindPegWindow: {ex.Message}";
                MarsLoggerSimple.Error("FindPegWindow", $"{iMark}|{strError}", ex);
                return false;
            }
        }

        /// <summary>
        /// 获取指定进程中的所有UIA元素
        /// </summary>
        /// <param name="rootElement">根元素</param>
        /// <param name="processId">进程ID</param>
        /// <returns>UIA元素列表</returns>
        private static List<MarsSpiedObjectInfo> GetAllUIAElementsInProcess(AutomationElement rootElement, int processId)
        {
            var elements = new List<MarsSpiedObjectInfo>();

            try
            {
                var walker = TreeWalker.ControlViewWalker;
                CollectElementsInProcess(rootElement, processId, elements, walker);
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetAllUIAElementsInProcess", $"Exception: {ex.Message}", ex);
            }

            return elements;
        }

        /// <summary>
        /// 递归收集指定进程中的UIA元素
        /// </summary>
        private static void CollectElementsInProcess(AutomationElement element, int processId, List<MarsSpiedObjectInfo> elements, TreeWalker walker)
        {
            try
            {
                // 检查当前元素是否属于目标进程
                if (element.Current.ProcessId == processId)
                {
                    var objInfo = GetMarsSpiedObjectInfo(element);
                    if (objInfo != null)
                    {
                        elements.Add(objInfo);
                    }
                }

                // 递归处理子元素
                var child = walker.GetFirstChild(element);
                while (child != null)
                {
                    CollectElementsInProcess(child, processId, elements, walker);
                    child = walker.GetNextSibling(child);
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("CollectElementsInProcess", $"Exception processing element: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取UIA元素的子元素
        /// </summary>
        private static List<MarsSpiedObjectInfo> GetChildUIAElements(MarsSpiedObjectInfo parentObj)
        {
            var childElements = new List<MarsSpiedObjectInfo>();

            try
            {
                // 这里根据 parentObj.referenceToObj（应为 UIA AutomationElement）来获取直接子元素
                if (parentObj == null)
                {
                    MarsLoggerSimple.Warning("GetChildUIAElements", "parentObj is null");
                    return childElements;
                }

                AutomationElement parentEl = null;
                if (parentObj.referenceToObj is AutomationElement ae)
                {
                    parentEl = ae;
                }
                else if (parentObj.hwnd != 0)
                {
                    // 回退方案：通过 hwnd 获取 AutomationElement
                    try { parentEl = AutomationElement.FromHandle(new IntPtr(parentObj.hwnd)); } catch { parentEl = null; }
                }

                if (parentEl == null)
                {
                    MarsLoggerSimple.Warning("GetChildUIAElements", $"No AutomationElement found for object: {parentObj.objectName}");
                    return childElements;
                }

                MarsLoggerSimple.Info("GetChildUIAElements", $"Getting UIA children for object: {parentObj.objectName}");

                // 使用 ControlView 遍历直接子节点
                var walker = TreeWalker.ControlViewWalker;
                var child = walker.GetFirstChild(parentEl);
                while (child != null)
                {
                    try
                    {
                        var info = GetMarsSpiedObjectInfo(child);
                        if (info != null)
                        {
                            // 同步句柄
                            if (info.hwnd == 0)
                            {
                                try { info.hwnd = child.Current.NativeWindowHandle; } catch { }
                            }
                            childElements.Add(info);
                        }
                    }
                    catch (Exception exChild)
                    {
                        MarsLoggerSimple.Warning("GetChildUIAElements", $"Convert child failed: {exChild.Message}");
                    }

                    child = walker.GetNextSibling(child);
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetChildUIAElements", $"Exception: {ex.Message}", ex);
            }

            return childElements;
        }

        /// <summary>
        /// 获取IAccessible元素的子元素
        /// </summary>
        private static List<MarsSpiedObjectInfo> GetChildIAccessibleElements(MarsSpiedObjectInfo parentObj)
        {
            var childElements = new List<MarsSpiedObjectInfo>();

            try
            {
                MarsLoggerSimple.Info("GetChildIAccessibleElements", $"Getting IAccessible children for object: {parentObj.objectName} (HWND: 0x{parentObj.hwnd:X})");

                if (parentObj.hwnd == 0)
                {
                    MarsLoggerSimple.Warning("GetChildIAccessibleElements", $"Parent object {parentObj.objectName} has no HWND, cannot get IAccessible children");
                    return childElements;
                }

                // 使用MARSAccessibleProvider获取IAccessible对象
                var accessibleProvider = new MARSAccessibleProvider();
                var accessible = accessibleProvider.GetAccessibleObject((IntPtr)parentObj.hwnd);

                if (accessible != null && accessible is IAccessible iAccessible)
                {
                    try
                    {
                        // 获取子对象数量
                        int childCount = iAccessible.accChildCount;
                        MarsLoggerSimple.Info("GetChildIAccessibleElements", $"Found {childCount} IAccessible children");

                        for (int i = 1; i <= childCount; i++) // IAccessible子对象索引从1开始
                        {
                            try
                            {
                                // 获取子对象信息
                                object childName = iAccessible.get_accName(i);
                                object childRole = iAccessible.get_accRole(i);
                                object childState = iAccessible.get_accState(i);

                                // 创建MarsSpiedObjectInfo对象
                                var childObj = new MarsSpiedObjectInfo
                                {
                                    objectName = childName?.ToString() ?? "",
                                    objectType = GetRoleName((int)childRole),
                                    Text = childName?.ToString() ?? "",
                                    controlClassTypeFromAPI = MarsSpiedObjectBasicInfo.cnst_control_source_type_msaa,
                                    obj_uuid = Guid.NewGuid().ToString(),
                                    hwnd = parentObj.hwnd, // 子对象通常共享父对象的HWND
                                    isVisible = true,
                                    isEnabled = true,
                                    controlMarsType = GetRoleName((int)childRole)
                                };

                                childElements.Add(childObj);
                                MarsLoggerSimple.Info("GetChildIAccessibleElements", $"Added child: {childObj.objectName} ({childObj.objectType})");
                            }
                            catch (Exception childEx)
                            {
                                MarsLoggerSimple.Warning("GetChildIAccessibleElements", $"Exception getting child {i}: {childEx.Message}");
                            }
                        }
                    }
                    catch (Exception accEx)
                    {
                        MarsLoggerSimple.Warning("GetChildIAccessibleElements", $"Exception accessing IAccessible children: {accEx.Message}");
                    }
                }
                else
                {
                    MarsLoggerSimple.Warning("GetChildIAccessibleElements", $"Could not get IAccessible object for HWND 0x{parentObj.hwnd:X}");
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetChildIAccessibleElements", $"Exception: {ex.Message}", ex);
            }

            return childElements;
        }

        /// <summary>
        /// 获取IAccessible角色名称
        /// </summary>
        private static string GetRoleName(int role)
        {
            // 这里可以根据需要实现角色ID到名称的映射
            // 暂时返回角色ID的字符串表示
            return $"Role_{role}";
        }

        /// <summary>
        /// 获取指定进程的顶级UI元素
        /// </summary>
        /// <param name="processId">目标进程ID</param>
        /// <param name="topLevelElements">输出参数，包含找到的顶级UI元素列表</param>
        /// <returns>如果成功找到顶级元素则返回true，否则返回false</returns>
        /// <remarks>
        /// 此方法专门用于获取指定进程的顶级窗口和对话框等顶级UI元素，
        /// 这些元素通常没有父窗口或者父窗口是桌面
        /// </remarks>
        public static bool GetProcessTopLevelUIElement(int processId, out List<MarsSpiedObjectInfo> topLevelElements)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("GetProcessTopLevelUIElement", $"{iMark}|processId: {processId}");

            topLevelElements = new List<MarsSpiedObjectInfo>();

            try
            {
                // 获取桌面根元素
                AutomationElement rootElement = AutomationElement.RootElement;
                MarsLoggerSimple.Info("GetProcessTopLevelUIElement", $"{iMark}|Starting search for top-level elements in process {processId}");

                // 使用TreeWalker遍历所有顶级窗口
                var walker = TreeWalker.ControlViewWalker;
                var currentElement = walker.GetFirstChild(rootElement);

                while (currentElement != null)
                {
                    try
                    {
                        // 检查当前元素是否属于目标进程
                        if (currentElement.Current.ProcessId == processId)
                        {
                            // 检查是否为顶级窗口（Window类型）
                            if (currentElement.Current.ControlType == ControlType.Window)
                            {
                                var objInfo = GetMarsSpiedObjectInfo(currentElement);
                                if (objInfo != null)
                                {
                                    topLevelElements.Add(objInfo);
                                    MarsLoggerSimple.Info("GetProcessTopLevelUIElement",
                                        $"{iMark}|Found top-level window: {objInfo.objectName} (HWND: 0x{objInfo.hwnd:X})");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MarsLoggerSimple.Warning("GetProcessTopLevelUIElement",
                            $"{iMark}|Exception processing element: {ex.Message}");
                    }

                    // 移动到下一个兄弟元素
                    currentElement = walker.GetNextSibling(currentElement);
                }

                MarsLoggerSimple.Info("GetProcessTopLevelUIElement",
                    $"{iMark}|Found {topLevelElements.Count} top-level elements for process {processId}");

                MarsLoggerSimple.logEnd("GetProcessTopLevelUIElement", $"{iMark}|Success");
                return topLevelElements.Count > 0;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetProcessTopLevelUIElement",
                    $"{iMark}|Exception: {ex.Message}", ex);
                MarsLoggerSimple.logEnd("GetProcessTopLevelUIElement", $"{iMark}|Failed");
                return false;
            }
        }

        /// <summary>
        /// 获取MarsSpiedObjectInfo的静态方法 - 将UIA AutomationElement转换为MarsSpiedObjectInfo
        /// </summary>
        /// <param name="element">UIA AutomationElement对象</param>
        /// <returns>MarsSpiedObjectInfo对象，包含完整的对象信息，如果转换失败则返回null</returns>
        /// <remarks>
        /// 此方法提供统一的对象信息获取接口，确保所有MarsSpiedObjectInfo对象都包含一致的核心信息：
        /// - 基础属性：objectName, objectType, objectNamePath (使用objectName堆叠), objectTypePath, Text
        /// - 位置信息：objectRect (x, y, width, height)
        /// - 标识信息：obj_uuid, hwnd
        /// - 状态信息：isVisible, isEnabled, controlMarsType
        /// - 框架信息：controlClassTypeFromAPI (设置为UIA)
        /// 
        /// 注意：objectNamePath使用objectName而不是AutomationId，因为AutomationId可能会变化
        /// </remarks>
        public static MarsSpiedObjectInfo GetMarsSpiedObjectInfo(AutomationElement element)
        {
            if (element == null)
            {
                MarsLoggerSimple.Warning("GetMarsSpiedObjectInfo", "Input AutomationElement is null");
                return null;
            }

            try
            {
                return ConvertToMarsSpiedObjectInfo(element);
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GetMarsSpiedObjectInfo", $"Failed to convert AutomationElement to MarsSpiedObjectInfo: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 将AutomationElement转换为MarsSpiedObjectInfo的内部实现方法
        /// </summary>
        /// <param name="element">UIA AutomationElement对象</param>
        /// <returns>MarsSpiedObjectInfo对象</returns>
        private static MarsSpiedObjectInfo ConvertToMarsSpiedObjectInfo(AutomationElement element)
        {
            try
            {
                // 创建MarsSpiedObjectInfo对象
                var objInfo = new MarsSpiedObjectInfo();

                // 1. 设置基础属性
                objInfo.objectName = element.Current.Name ?? "";
                objInfo.objectType = element.Current.ControlType?.ProgrammaticName ?? "";
                objInfo.objectNamePath = element.Current.Name ?? ""; // 使用objectName而不是AutomationId
                objInfo.objectTypePath = element.Current.ClassName ?? "";
                objInfo.Text = element.Current.Name ?? "";

                // 2. 设置位置和大小信息
                var bounds = element.Current.BoundingRectangle;
                objInfo.objectRect = new System.Drawing.Rectangle(
                    (int)bounds.Left, (int)bounds.Top,
                    (int)bounds.Width, (int)bounds.Height);

                // 3. 设置核心标识信息
                objInfo.obj_uuid = Guid.NewGuid().ToString();
                objInfo.hwnd = element.Current.NativeWindowHandle;

                // 4. 设置状态信息
                objInfo.isVisible = !element.Current.IsOffscreen;
                objInfo.isEnabled = element.Current.IsEnabled;
                objInfo.controlMarsType = element.Current.ControlType?.ProgrammaticName ?? "";

                // 5. 设置框架信息
                objInfo.controlClassTypeFromAPI = MarsSpiedObjectBasicInfo.cnst_control_source_type_uia;

                objInfo.referenceToObj = element;
                // 6. 记录转换成功日志
                MarsLoggerSimple.Info("ConvertToMarsSpiedObjectInfo",
                    $"Successfully converted UIA element: {objInfo.objectName} ({objInfo.objectType})");

                return objInfo;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("ConvertToMarsSpiedObjectInfo",
                    $"Exception converting element: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 判断对象是否匹配指定层
        /// </summary>
        private static bool IsObjectMatchingLayer(MarsSpiedObjectInfo obj, string layerPath, Dictionary<string, string> dictPegProperties)
        {
            try
            {
                dictPegProperties = dictPegProperties ?? new Dictionary<string, string>();
                // 检查Text属性是否匹配
                if (DictionaryHelper.TryGetValueIgnoreCase(dictPegProperties, "Text", out var expectedText) && !string.IsNullOrEmpty(expectedText))
                {
                    if (!MarsWindowsAPIsExtend.RegularTest(expectedText, layerPath))
                    {
                        return false;
                    }
                }

                // 检查对象类型是否匹配
                if ((DictionaryHelper.TryGetValueIgnoreCase(dictPegProperties, "Object Type", out var expectedType)
                    || DictionaryHelper.TryGetValueIgnoreCase(dictPegProperties, "ControlType", out expectedType)
                    || DictionaryHelper.TryGetValueIgnoreCase(dictPegProperties, "ObjectType", out expectedType))
                    && !string.IsNullOrEmpty(expectedType))
                {
                    if (!string.Equals(obj.objectType, expectedType, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }


                // 检查类名是否匹配
                if (DictionaryHelper.TryGetValueIgnoreCase(dictPegProperties, "ClassName", out var expectedClassName) && !string.IsNullOrEmpty(expectedClassName))
                {
                    if (!string.Equals(obj.objectTypePath, expectedClassName, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                // 检查AutomationId是否匹配（注意：objectNamePath现在使用objectName而不是AutomationId）
                if (DictionaryHelper.TryGetValueIgnoreCase(dictPegProperties, "AutomationId", out var expectedAutomationId) && !string.IsNullOrEmpty(expectedAutomationId))
                {
                    // 如果dictPegProperties中指定了AutomationId，需要从UIA元素中直接获取进行比较
                    // 这里可以根据需要实现更复杂的匹配逻辑
                    MarsLoggerSimple.Warning("IsObjectMatchingLayer", "AutomationId matching not fully implemented - objectNamePath now uses objectName");
                }

                // 如果指定了layerPath，检查是否匹配
                if (!string.IsNullOrEmpty(layerPath))
                {
                    // 这里可以根据需要实现更复杂的路径匹配逻辑
                    if (!MarsWindowsAPIsExtend.RegularTest(layerPath, obj.Text))
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("IsObjectMatchingLayer", $"Exception checking object match: {ex.Message}");
                return false;
            }
        }


        private static ControlType MapControlTypFromMarsIDS(string strType, ref bool isOk)
        {
            if (string.IsNullOrEmpty(strType))
            {
                isOk = false;
                return null;
            }

            isOk = true;
            switch (strType.Trim().ToLowerInvariant())
            {
                case "controltype.tab":
                case "control.tab":
                case "tab":
                    return ControlType.Tab;
                case "controltype.tabitem":
                case "control.tabitem":
                case "tabitem":
                    return ControlType.TabItem;
                case "controltype.button":
                case "control.button":
                case "button":
                    return ControlType.Button;
                case "controltype.edit":
                case "control.edit":
                case "edit":
                    return ControlType.Edit;
                case "controltype.window":
                case "control.window":
                case "window":
                    return ControlType.Window;
                case "controltype.menu":
                case "control.menu":
                case "menu":
                    return ControlType.Menu;
                case "controltype.menuitem":
                case "control.menuitem":
                case "menuitem":
                    return ControlType.MenuItem;
                case "controltype.list":
                case "control.list":
                case "list":
                    return ControlType.List;
                case "controltype.listitem":
                case "control.listitem":
                case "listitem":
                    return ControlType.ListItem;
                case "controltype.combobox":
                case "control.combobox":
                case "combobox":
                    return ControlType.ComboBox;
                case "controltype.checkbox":
                case "control.checkbox":
                case "checkbox":
                    return ControlType.CheckBox;
                case "controltype.radiobutton":
                case "control.radiobutton":
                case "radiobutton":
                    return ControlType.RadioButton;
                case "controltype.tree":
                case "control.tree":
                case "tree":
                    return ControlType.Tree;
                case "controltype.treeitem":
                case "control.treeitem":
                case "treeitem":
                    return ControlType.TreeItem;
                case "controltype.pane":
                case "control.pane":
                case "pane":
                    return ControlType.Pane;
                case "controltype.text":
                case "control.text":
                case "text":
                    return ControlType.Text;
                case "controltype.group":
                case "control.group":
                case "group":
                    return ControlType.Group;
                case "controltype.custom":
                case "control.custom":
                case "custom":
                    return ControlType.Custom;
                case "controltype.document":
                case "control.document":
                case "document":
                    return ControlType.Document;
                // 可根据需要继续扩展其它类型
                default:
                    isOk = false;
                    return null;
            }
        }

        /// <summary>
        /// 判断当前层是否匹配 
        /// </summary>
        /// <param name="tech"></param>
        /// <param name="layerName"></param>
        /// <param name="uiChildObject"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        private static bool IsCurrentLayerMatching(string tech, string layerName,
            MarsSpiedObjectInfo uiChildObject, ref string strError, ref bool hasError)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("IsCurrentLayerMatching", $"{iMark}|tech: {tech}, layerValue: {layerName}");
            hasError = false;
            if ((tech.Equals("UIA", StringComparison.OrdinalIgnoreCase)) || (tech.Equals("MARSUIA", StringComparison.OrdinalIgnoreCase)))
            {
                /// 通过UIA技术获得层的名称
                /// 
                var uiaE = uiChildObject.referenceToObj as AutomationElement;
                if (uiaE == null)
                {
                    strError = "Parent object is not a MARSUI type";
                    MarsLoggerSimple.Error("IsCurrentLayerMatching", $"{iMark}|{strError}|for layer {tech}|{layerName}");
                    hasError = true;
                    return false;
                }
                string uiaECurrentName = string.IsNullOrEmpty(uiaE.Current.Name) ? "Unknow" : uiaE.Current.Name;
                return MarsWindowsAPIsExtend.RegularTest(layerName, uiaECurrentName);
            }
            else if ((tech.Equals("IAcc", StringComparison.OrdinalIgnoreCase)) || (tech.Equals("MARSACC", StringComparison.OrdinalIgnoreCase)))
            {
                /// 处理IAccessible对象
                /// 
                var iacc = uiChildObject.referenceToObj as IAccessible;
                if (iacc == null) return false;
                string accName = iacc.get_accName(0);
                accName = string.IsNullOrEmpty(accName) ? "Unknow" : accName;
                return (MarsWindowsAPIsExtend.RegularTest(layerName, accName));
            } else
            {
                return HandleUnknownTechnologyType(tech, layerName, iMark, ref strError, ref hasError);
            }
        }

        /// <summary>
        /// 处理未知技术类型的错误情况
        /// </summary>
        /// <param name="tech">技术类型</param>
        /// <param name="layerName">层名称</param>
        /// <param name="iMark">标记</param>
        /// <param name="strError">错误信息</param>
        /// <param name="hasError">是否有错误</param>
        /// <returns>总是返回false</returns>
        private static bool HandleUnknownTechnologyType(string tech, string layerName, int iMark, ref string strError, ref bool hasError)
        {
            strError = $"Unknown MARS technology type: {tech}";
            MarsLoggerSimple.Error("IsCurrentLayerMatching", $"{iMark}|{strError}|for layer {tech}|{layerName}");
            hasError = true;
            return false;
        }

        /// <summary>
        /// 从parent中筛选指定ControlType的对象
        /// </summary>
        /// <param name="prnt"></param>
        /// <param name="ct"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        private static List<AutomationElement> FilterObjectsFromParent(AutomationElement prnt, ControlType ct,
            ref bool isOk, ref string strError)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("FilterObjectsFromParent", $"{iMark}|Parent HWND: 0x{prnt.Current.NativeWindowHandle:X}, ControlType: {ct?.ProgrammaticName}");
            try
            {
                var tabCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ct);

                var lstTmp = FindElementsByControlTypeWithDepthLimit(prnt, tabCondition, 4, ref strError);
                MarsLoggerSimple.Info("FilterObjectsFromParent", $"{iMark}|Found {lstTmp.Count} elements of type {ct?.ProgrammaticName}");

                //var lstTmp = prnt.FindAll(TreeScope.Descendants, tabCondition);
                if (lstTmp.Count <= 0)
                {
                    strError = $"Can't find such type from pegwindow's Descendants|{ct}";
                    //dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("FilterObjectsFromParent", $"{iMark}|{strError}");
                    isOk = false;
                    return null;
                }
                AutomationElement[] arrElements = new AutomationElement[lstTmp.Count];
                lstTmp.CopyTo(arrElements, 0);
                isOk = true;
                return arrElements.ToList();
            }
            finally
            {
                MarsLoggerSimple.logEnd("FilterObjectsFromParent", $"{iMark}|Completed");
            }
        }

        /// <summary>
        /// 选中tab页 - 未实现
        /// </summary>
        /// <param name="stepId"></param>
        /// <param name="dictPegProperties"></param>
        /// <param name="dictObjProperties"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData">tab页header的内容</param>
        /// <param name="typeName"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="pegWindName"></param>
        /// <param name="objName"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static bool MARSUI_SelectTab(long stepId, Dictionary<string, string> dictPegProperties,
            Dictionary<string, string> dictObjProperties, string strParaMeter, string strData, string typeName, string strAttachInfo,
            string pegWindName, string objName, ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("MARSUI_SelectTab", $"{iMark}|stepId:{stepId}|{pegWindName}.{objName}");
            try
            {
                bool hasError = false, isOk = true;

                var vars = MARSUIAppSideVariables.GetInstance();
                if (vars == null || vars.currentPegwindow == null)
                {
                    strError = "Please run Pegwindow first";
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_SelectTab", $"{iMark}|{strError}");
                    return false;
                }

                var peg = vars.currentPegwindow as MarsSpiedObjectInfo;
                if (string.IsNullOrEmpty(peg.objectNamePath))
                {
                    peg.objectNamePath = CreatePegRuntimePath(peg);
                    MarsLoggerSimple.Info("MARSUI_SelectTab", $"{iMark}|CreatePegRuntimePath|{peg.objectNamePath}");
                }

                string namePathType = "relative";
                if (dictObjProperties != null && dictObjProperties.ContainsKey("MarsNamePathType"))
                    namePathType = (dictObjProperties["MarsNamePathType"] ?? "relative").Trim().ToLower();

                string marsNamePath = "";
                if (dictObjProperties != null && dictObjProperties.ContainsKey("MarsNamePath"))
                    marsNamePath = dictObjProperties["MarsNamePath"] ?? "";



                // 初始候选集合
                List<MarsSpiedObjectInfo> currentLayerObjects = new List<MarsSpiedObjectInfo>();
                if (string.Equals(namePathType, "abs", StringComparison.OrdinalIgnoreCase))
                {
                    int currentPid = MARSTestProcess.CurrentTestProcessId;
                    /// 如果是绝对路径，则从进程顶层开始，这里有错误
					if (!GetProcessTopLevelUIElement(currentPid, out currentLayerObjects))
                    {
                        strError = $"No top-level windows found for process {currentPid}";
                        dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                        MarsLoggerSimple.Error("MARSUI_SelectTab", $"{iMark}|{strError}");
                        return false;
                    }
                }
                else
                {
                    // relative，从pegwindow开始
                    currentLayerObjects.Add(peg);
                }

                // 如果没有提供路径，则取pegwindow的直接子对象，直接从对象类别走
                //if (string.IsNullOrWhiteSpace(marsNamePath))
                //{
                //	var children = new List<MarsSpiedObjectInfo>();
                //	foreach (var parent in currentLayerObjects)
                //	{
                //		children.AddRange(GetChildUIAElements(parent));
                //		children.AddRange(GetChildIAccessibleElements(parent));
                //	}
                //	currentLayerObjects = children;
                //}
                //else
                //{
                // 分层匹配，支持 UIA:Name / Standard:Name / IAcc:Name
                var pegAutoUI = peg.referenceToObj as AutomationElement;
                if (pegAutoUI == null) {
                    strError = "No MARSUI page is set, please recheck Object settings";
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_SelectTab", strError);
                    return false;
                }
                string objTyp = "";
                List<AutomationElement> foundData = new();
                if (DictionaryHelper.TryGetValueIgnoreCase(dictObjProperties, "Object type", out objTyp) || DictionaryHelper.TryGetValueIgnoreCase(dictObjProperties, "objecttype", out objTyp))
                {
                    // 构造条件：ControlType 为 Tab                        
                    var typ2Search = MapControlTypFromMarsIDS(objTyp, ref isOk);
                    if (typ2Search != ControlType.TabItem)
                    {
                        /// 目前只支持TabItem类型
                        /// 
                        strError = $"MARSUI_SelectTab only support Object type TabItem, but it is |{objTyp}";
                        dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                        MarsLoggerSimple.Error("MARSUI_SelectTab", $"{iMark}|{strError}");
                        return false;
                    }

                    var lstObjects = FilterObjectsFromParent(pegAutoUI, typ2Search, ref isOk, ref strError);
                    if (!isOk)
                    {
                        strError = $"FilterObjectsFromParent failed|{strError}";
                        dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                        MarsLoggerSimple.Error("MARSUI_SelectTab", $"{iMark}|{strError}");
                        return false;
                    }
                    foundData.AddRange(lstObjects);
                }
                /// 过滤visible
                /// 对比data
                /// 
                if (string.IsNullOrEmpty(strData))
                {
                    strError = "Please provide tab header text in Data field";
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_SelectTab", $"{iMark}|{strError}");
                    return false;
                }
                List<AutomationElement> matchedData = new();
                foreach (var fe in foundData)
                {
                    try
                    {
                        if (fe.Current.IsOffscreen) continue;
                        string name = fe.Current.Name ?? "";
                        if (MarsWindowsAPIsExtend.RegularTest(strData, name))
                            matchedData.Add(fe);
                    }
                    catch (Exception ex)
                    {
                        MarsLoggerSimple.Warning("MARSUI_SelectTab", $"Exception processing found element: {ex.Message}");
                    }
                }
                DictionaryHelper.TryGetValueIgnoreCase(dictObjProperties, "index", out var strIdx);
                if (!int.TryParse(strIdx, out int indx))
                {
                    indx = -1;
                }
                if ((matchedData.Count > 1) && (indx < 0))
                {
                    strError = $"There are multiple tabs exists |{matchedData.Count}|Please provide index in Object properties to pick one tab";
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_SelectTab", $"{iMark}|{strError}");
                    return false;
                }
                if (matchedData.Count <= 0)
                {
                    strError = $"Can't find tab matched the header text|{strData}";
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_SelectTab", $"{iMark}|{strError}");
                    return false;
                }
                if (indx >= matchedData.Count)
                {
                    strError = $"The index provided is out of range|{indx}|max:{matchedData.Count - 1}";
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_SelectTab", $"{iMark}|{strError}");
                    return false;
                }
                AutomationElement picked = indx >= 0 ? matchedData[indx] : matchedData[0];
                /// 获得tabitem的rect，然后用鼠标点击
                /// 
                var rect = picked.Current.BoundingRectangle;
                int x = (int)((rect.Left + rect.Right) / 2);
                int y = (int)((rect.Top + rect.Bottom) / 2);
                MarsWindowsAPIs.SetCursorPos(x, y);

                System.Threading.Thread.Sleep(100);
                MarsWindowsAPIsExtend.LeftMouseClick(x, y);
                dealResult = new MARSDealResult {
                    ResultMessage = "SUCCESS",
                    ErrorMessage = "",
                    ReturnedData = picked.Current.Name,
                    ActualInputData = strData,
                    AckTime = DateTime.Now
                };
                MarsLoggerSimple.logEnd("MARSUI_SelectTab", $"{iMark}|Success|picked:{picked.Current.Name}");
                //}

                //// 末层后进一步基于其他属性过滤（Object type / Text 等）
                //var finalCandidates = new List<MarsSpiedObjectInfo>();
                //foreach (var c in currentLayerObjects)
                //{
                //	if (IsObjectMatchingLayer(c, strData /*tab header*/, dictObjProperties ?? new Dictionary<string, string>()))
                //		finalCandidates.Add(c);
                //}

                //if (finalCandidates.Count == 0)
                //{
                //	strError = "No tab matched the specified criteria";
                //	dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                //	MarsLoggerSimple.Error("MARSUI_SelectTab", $"{iMark}|{strError}");
                //	return false;
                //}

                //// 处理 index
                //int pickIndex = 0;
                //string idxKey = "index";
                //if ((dictObjProperties != null) && dictObjProperties.ContainsKey(idxKey) && int.TryParse(dictObjProperties[idxKey], out var idx) && idx >= 0 && idx < finalCandidates.Count)
                //	pickIndex = idx;

                //var target = finalCandidates[pickIndex];
                // 此处可根据UIA/IAccessible执行真正的Select操作；当前先返回成功结果

                dealResult = new MARSDealResult { ResultMessage = "SUCCESS", ErrorMessage = "", ReturnedData = picked.Current.Name, ActualInputData = strData };
                MarsLoggerSimple.logEnd("MARSUI_SelectTab", $"{iMark}|Success|picked:{picked.Current.Name}");
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Exception in MARSUI_SelectTab: {ex.Message}";
                dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                MarsLoggerSimple.Error("MARSUI_SelectTab", $"{iMark}|{strError}", ex);
                return false;
            }
        }

        private static int[] GetPositionIndex(string positionInfo, ref bool isOk, ref string strError)
        {
            if (string.IsNullOrEmpty(positionInfo))
            {
                strError = "Two number should be set for PositionIndex property";
                isOk = false;
                return null;
            }
            var arr = positionInfo.Split(new string[] { ";", ",", ":" }, StringSplitOptions.RemoveEmptyEntries);
            if ((arr == null) || (arr.Length != 2))
            {
                strError = "property PositionIndex doesn't set right. two numbers, should be splited by ;,:, is not found";
                isOk = false;
                return null;
            }
            try
            {
                int[] result = new int[2];
                if ((int.TryParse(arr[0], out result[0])) && (int.TryParse(arr[1], out result[1])))
                {
                    isOk = true;
                    return result;
                }
                strError = "two number should be set";
                isOk = false;
                return null;
            } catch (Exception e)
            {
                strError = $"two numbers should be set but there are |{positionInfo}";
                isOk = false;
                return null;
            }
        }

        private static List<AutomationElement> FilterObjectsByProperties(Dictionary<string, string> dicObjProperteis,
            List<AutomationElement> sourceControlLis, ref bool isOk, ref string strError)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("FilterObjectsByProperties", $"{iMark}|Start filtering {sourceControlLis.Count} elements");
            var filteredList = new List<AutomationElement>();
            bool matches = true;
            bool isIndexRequred = false, isIndexInited = false;
            int idx = -1;
            bool isPositionIndexRequired = false;
            int[] currentPositionInfo = new int[2];
            try
            {
                foreach (var element in sourceControlLis)
                {
                    matches = true;
                    isPositionIndexRequired = false;
                    try
                    {
                        //if (element.Current.IsOffscreen) continue;
                        foreach (var kvp in dicObjProperteis)
                        {
                            if (kvp.Key.Equals("control type", StringComparison.OrdinalIgnoreCase) ||
                                kvp.Key.Equals("controlType", StringComparison.OrdinalIgnoreCase) ||
                                kvp.Key.Equals("objectType", StringComparison.OrdinalIgnoreCase) ||
                                kvp.Key.Equals("object Type", StringComparison.OrdinalIgnoreCase) ||
                                kvp.Key.Equals("catalog", StringComparison.OrdinalIgnoreCase) ||
                                kvp.Key.Equals("TypePath", StringComparison.OrdinalIgnoreCase) ||
                                kvp.Key.Equals("MaxDepth", StringComparison.OrdinalIgnoreCase) ||
                                kvp.Key.Equals("Depth", StringComparison.OrdinalIgnoreCase)
                                )
                                continue;
                            if (kvp.Key.Equals("index", StringComparison.OrdinalIgnoreCase))
                            {
                                isIndexRequred = true;
                                if (!int.TryParse(kvp.Value, out idx))
                                {
                                    strError = $"index value is not valid integer|{kvp.Value}";
                                    MarsLoggerSimple.Error("FilterObjectsByProperties", $"{iMark}|{strError}");
                                    matches = false;
                                    break;
                                }
                                continue;
                            }

                            if (kvp.Key.Equals("MarsRibbonType", StringComparison.OrdinalIgnoreCase))
                            {
                                //此处为子对象处理的部分，因此，先忽略
                                continue;
                            }

                            string propertyValue = kvp.Value;
                            string elementValue = "";
                            if (kvp.Key.Equals("Text", StringComparison.OrdinalIgnoreCase)
                                || kvp.Key.Equals("ObjectName", StringComparison.OrdinalIgnoreCase)
                                || kvp.Key.Equals("attachText", StringComparison.OrdinalIgnoreCase))
                            {
                                elementValue = element.Current.Name ?? "";
                                //elementValue = element.Cached.Name ?? ""; // 使用缓存值提高性能
                            }
                            else if (kvp.Key.Equals("Value", StringComparison.OrdinalIgnoreCase))
                            {
                                try
                                {
                                    elementValue = element.GetCurrentPropertyValue(AutomationElement.NameProperty)?.ToString() ?? "";
                                }
                                catch
                                {
                                    elementValue = "";
                                }
                            } else if (kvp.Key.Equals("winclass", StringComparison.OrdinalIgnoreCase))
                            {
                                string className = element.Current.ClassName;
                                //string className = GetUIAObjectWindowClass(element, ref isOk, ref strError);
                                if (!MarsWindowsAPIsExtend.RegularTest(propertyValue, className))
                                {
                                    strError = $"window class doesn't match|{propertyValue}|{className}";
                                    MarsLoggerSimple.Error("FilterObjectsByProperties", $"{iMark}|{strError}");
                                    matches = false;
                                    break;
                                }
                                continue;
                            } else if (kvp.Key.Equals("MarsNamePath", StringComparison.OrdinalIgnoreCase))
                            {
                                /// 暂时对path不做处理
                                continue;
                            } else if (kvp.Key.Equals("PositionIndex", StringComparison.OrdinalIgnoreCase))
                            {
                                /// 暂时对位置不做处理
                                /// 
                                isPositionIndexRequired = true;
                                currentPositionInfo = GetPositionIndex(kvp.Value, ref isOk, ref strError);
                                if (!isOk)
                                {
                                    currentPositionInfo = null;
                                    matches = false;
                                    MarsLoggerSimple.Error("FilterObjectsByProperties", $"{iMark}|PositionIndex check|{strError}");
                                    break;
                                }
                                continue;
                            } else if ((kvp.Key.Equals("Control ID", StringComparison.OrdinalIgnoreCase))
                                || (kvp.Key.Equals("ControlID", StringComparison.OrdinalIgnoreCase)))
                            {
                                elementValue = GetControlIdFromAutomationUI(element, false);
                                if (!elementValue.Equals(kvp.Value))
                                {
                                    strError = $"Control ID doesn't match|{kvp.Value}|{elementValue}";
                                    MarsLoggerSimple.Error("FilterObjectsByProperties", $"{iMark}|{strError}");
                                    matches = false;
                                    break;
                                }
                            }
                            else
                            {
                                strError = $"unsupported property|{kvp.Key}|please change properties";
                                MarsLoggerSimple.Error("FilterObjectsByProperties", $"{iMark}|{strError}");
                                matches = false;
                                break;
                            }
                            if (!MarsWindowsAPIsExtend.RegularTest(propertyValue, elementValue))
                            {
                                matches = false;
                                break;
                            }
                            else
                                matches = true;
                        }
                        if (matches)
                        {
                            filteredList.Add(element);
                        }
                    }
                    catch (Exception ex)
                    {
                        MarsLoggerSimple.Warning("FilterObjectsByProperties", $"Exception processing element: {ex.Message}");
                        matches = false;
                        break;
                    }
                }

                //if (isPositionIndexRequired)
                //{
                //    if (filteredList.Count > 1)
                //    {
                //        /// 将filteredList按位置进行排序，方到一个二维数组中，用行列索引。
                //    }
                //}

                if ((filteredList.Count > 1) && (!isIndexRequred))
                {
                    strError = $"There are multiple elements exists |{filteredList.Count}|Please provide index in Object properties to pick one element";
                    MarsLoggerSimple.Error("FilterObjectsByProperties", $"{iMark}|{strError}");
#if DEBUG
                    int iFlashCount = 0;
                    string tmpStrError = "";
                    foreach (var fe in filteredList)
                    {
                        MarsLoggerSimple.Info("FilterObjectsByProperties", $"{iMark}|Element Name: {fe.Current.Name}, HWND: 0x{fe.Current.NativeWindowHandle:X}");
                        if (iFlashCount < 3) {
                            MarsWindowsAPIs.RECT r = new MarsWindowsAPIs.RECT();
                            r.Left = (int)fe.Current.BoundingRectangle.Left;
                            r.Top = (int)fe.Current.BoundingRectangle.Top;
                            r.Right = (int)fe.Current.BoundingRectangle.Right;
                            r.Bottom = (int)fe.Current.BoundingRectangle.Bottom;
                            XorDrawing.DrawXorRectangleOnDeskTop(r, ref tmpStrError);
                            iFlashCount++;
                        }
                    }
#endif
                    isOk = false;
                    return null;
                }
                if ((filteredList.Count <= 0) || (idx >= filteredList.Count))
                {
                    strError = $"Can't find element matched the specified properties";
                    MarsLoggerSimple.Error("FilterObjectsByProperties", $"{iMark}|{strError}");
                    isOk = false;
                    return null;
                }
                if ((isIndexRequred) & (idx < 0))
                {
                    strError = "index value is not right";
                    MarsLoggerSimple.Error("FilterObjectsByProperties", $"{iMark}|{strError}");
                    isOk = false;
                    return null;
                }
                if (isIndexRequred)
                {
                    var targetUI = filteredList[idx];
                    isOk = true;
                    return new List<AutomationElement> { targetUI };
                }
                isOk = true;
                //filteredList.Clear();
                //filteredList.Add(targetUI);
                if (filteredList.Count != 1)
                {
                    strError = $"No element matched the specified properties |{filteredList.Count} |or multiple UI objects were found, please make sure only one UI should be located";
                    isOk = false;
                    MarsLoggerSimple.Error("FilterObjectsByProperties", $"{iMark}|{strError}");
                    return null;
                }
                isOk = true;
                MarsLoggerSimple.logEnd("FilterObjectsByProperties", $"{iMark}|Filtered down to {filteredList.Count}");
                return new List<AutomationElement> { filteredList[0] };
            }
            catch (Exception ex)
            {
                strError = $"Exception in FilterObjectsByProperties: {ex.Message}";
                isOk = false;
                MarsLoggerSimple.Error("FilterObjectsByProperties", $"{iMark}|{strError}", ex);
                return null;
            }
            finally
            {
                MarsLoggerSimple.logEnd("FilterObjectsByProperties", $"{iMark}|Completed");
            }
        }

        /// <summary>
        /// 填充编辑框 
        /// </summary>
        /// <param name="stepId"></param>
        /// <param name="dictPegProperties"></param>
        /// <param name="dictObjProperties"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="typeName"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="pegWindName"></param>
        /// <param name="objName"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <returns></returns>
        public static bool MARSUI_FillEdit(long stepId, Dictionary<string, string> dictPegProperties,
            Dictionary<string, string> dictObjProperties, string strParaMeter, string strData, string typeName, string strAttachInfo,
            string pegWindName, string objName, ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("MARSUI_FillEdit", $"{iMark}|stepId:{stepId}|{pegWindName}.{objName}|{MarsWindowsAPIsExtend.Dic2String(dictObjProperties)}");
            try
            {
                // 统一查找指定ControlType的元素
                if (!TryFindElementsByControlType(dictObjProperties, out var foundElements, ref strError))
                {
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_FillEdit", $"{iMark}|{strError}");
                    return false;
                }

                // 5. 从objProperties的key中，除去control type外还有那些？如果有Text/ObjectName，从UIA对象中获得Name，value，text的值，并且用RegularTest判断是否符合，如果符合，放到待选列表中
                var candidates = new List<AutomationElement>();
                bool isOk = false;
                candidates = FilterObjectsByProperties(dictObjProperties, foundElements, ref isOk, ref strError);
                #region old code, to be removed
                //foreach (AutomationElement element in foundElements)
                //{
                //    try
                //    {
                //        if (element.Current.IsOffscreen) continue;

                //        bool matches = true;
                //        foreach (var kvp in dictObjProperties)
                //        {
                //            if (kvp.Key.Equals("control type", StringComparison.OrdinalIgnoreCase) ||
                //                kvp.Key.Equals("controlType", StringComparison.OrdinalIgnoreCase) ||
                //                kvp.Key.Equals("objectType", StringComparison.OrdinalIgnoreCase) ||
                //                kvp.Key.Equals("object Type", StringComparison.OrdinalIgnoreCase) ||
                //                kvp.Key.Equals("catalog", StringComparison.OrdinalIgnoreCase) ||
                //                kvp.Key.Equals("index", StringComparison.OrdinalIgnoreCase))
                //                continue;

                //            string propertyValue = kvp.Value;
                //            string elementValue = "";

                //            if (kvp.Key.Equals("Text", StringComparison.OrdinalIgnoreCase) 
                //                || kvp.Key.Equals("ObjectName", StringComparison.OrdinalIgnoreCase)
                //                || kvp.Key.Equals("attachText", StringComparison.OrdinalIgnoreCase))
                //            {
                //                elementValue = element.Current.Name ?? "";
                //            }
                //            else if (kvp.Key.Equals("Value", StringComparison.OrdinalIgnoreCase))
                //            {
                //                try
                //                {
                //                    elementValue = element.GetCurrentPropertyValue(AutomationElement.NameProperty)?.ToString() ?? "";
                //                }
                //                catch
                //                {
                //                    elementValue = "";
                //                }
                //            } else
                //            {
                //                strError = $"unsupported property|{kvp.Key}|please change properties";
                //                MarsLoggerSimple.Error("MARSUI_FillEdit", $"{iMark}|{strError}");
                //                matches = false;
                //                break;
                //            }

                //            if (!MarsWindowsAPIsExtend.RegularTest(propertyValue, elementValue))
                //            {
                //                matches = false;
                //                break;
                //            }
                //            else matches = true;
                //        }

                //        if (matches)
                //        {
                //            candidates.Add(element);
                //        }
                //    }
                //    catch (Exception ex)
                //    {
                //        MarsLoggerSimple.Warning("MARSUI_FillEdit", $"Exception processing element: {ex.Message}");
                //    }
                //}
                #endregion
                // 6. 如果没有候选对象，报错，有多个，判断是否有index？否则报错
                if ((candidates.Count == 0) || (!isOk))
                {
                    strError = $"No matching elements found based on the specified criteria|{strError}";
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_FillEdit", $"{iMark}|{strError}");
                    return false;
                }
                #region old code, to be removed
                //if (candidates.Count > 1)
                //{
                //    if (!dictObjProperties.ContainsKey("index"))
                //    {
                //        strError = $"Multiple elements found ({candidates.Count}). Please provide 'index' in Object properties to select one";
                //        dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                //        MarsLoggerSimple.Error("MARSUI_FillEdit", $"{iMark}|{strError}");
                //        return false;
                //    }
                //}

                //// 选择目标元素
                //int targetIndex = 0;
                //if (dictObjProperties.ContainsKey("index"))
                //{
                //    if (!int.TryParse(dictObjProperties["index"], out targetIndex) || targetIndex < 0 || targetIndex >= candidates.Count)
                //    {
                //        strError = $"Invalid index: {dictObjProperties["index"]}. Must be between 0 and {candidates.Count - 1}";
                //        dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                //        MarsLoggerSimple.Error("MARSUI_FillEdit", $"{iMark}|{strError}");
                //        return false;
                //    }
                //}

                //var targetElement = candidates[targetIndex];
                #endregion
                var targetElement = candidates[0];

                // 7. 判断进程是否处于等待接收输入情况，最长等待时间180秒
                if (!WaitForProcessReady(180))
                {
                    strError = "Process is not ready to receive input within 180 seconds";
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_FillEdit", $"{iMark}|{strError}");
                    return false;
                }

                // 8. 使用mouse event先在rectangle的中点点击，然后用keyboard event发送endkey和Shift home key，最后再发送del key
                var rect = targetElement.Current.BoundingRectangle;
                int x = (int)((rect.Left + rect.Right) / 2);
                int y = (int)((rect.Top + rect.Bottom) / 2);

                // 点击中点
                MarsWindowsAPIs.SetCursorPos(x, y);
                System.Threading.Thread.Sleep(50);
                MarsWindowsAPIsExtend.LeftMouseClick(x, y);
                System.Threading.Thread.Sleep(100);

                // 发送End键
                string keyError = "";
                SendKeysToInput.Send("{END}+{HOME}{DEL 50}", 15);
                System.Threading.Thread.Sleep(100);
                SendKeysToInput.Send(strData, 15);

                System.Threading.Thread.Sleep(100);
                dealResult = new MARSDealResult
                {
                    ResultMessage = "SUCCESS",
                    ErrorMessage = "",
                    ReturnedData = targetElement.Current.Name,
                    ActualInputData = strData,
                    AckTime = DateTime.Now
                };
                MarsLoggerSimple.logEnd("MARSUI_FillEdit", $"{iMark}|Success|target:{targetElement.Current.Name}");
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Exception in MARSUI_FillEdit: {ex.Message}";
                dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                MarsLoggerSimple.Error("MARSUI_FillEdit", $"{iMark}|{strError}", ex);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("MARSUI_FillEdit", $"{iMark}|Completed");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stepId"></param>
        /// <param name="dictPegProperties"></param>
        /// <param name="dictObjProperties"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="typeName"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="pegWindName"></param>
        /// <param name="objName"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <returns></returns>
        public static bool MARSUI_ClickButton(long stepId, Dictionary<string, string> dictPegProperties,
            Dictionary<string, string> dictObjProperties, string strParaMeter, string strData, string typeName, string strAttachInfo,
            string pegWindName, string objName, ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("MARSUI_ClickButton", $"{iMark}|stepId:{stepId}|{pegWindName}.{objName}|{MarsWindowsAPIsExtend.Dic2String(dictObjProperties)}");
            try
            {
                // 统一查找指定ControlType的元素
                if (!TryFindElementsByControlType(dictObjProperties, out var foundElements, ref strError))
                {
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_ClickButton", $"{iMark}|{strError}");
                    return false;
                }

                // 5. 从objProperties的key中，除去control type外还有那些？如果有Text/ObjectName，从UIA对象中获得Name，value，text的值，并且用RegularTest判断是否符合，如果符合，放到待选列表中
                var candidates = new List<AutomationElement>();
                bool isOk = false;
                candidates = FilterObjectsByProperties(dictObjProperties, foundElements, ref isOk, ref strError);

                // 6. 如果没有候选对象，报错，有多个，判断是否有index？否则报错
                if ((candidates.Count == 0) || (!isOk))
                {
                    strError = $"No matching elements found based on the specified criteria|{strError}";
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_ClickButton", $"{iMark}|{strError}");
                    return false;
                }

                var targetElement = candidates[0];

                // 7. 判断进程是否处于等待接收输入情况，最长等待时间180秒
                if (!WaitForProcessReady(180))
                {
                    strError = "Process is not ready to receive input within 180 seconds";
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_ClickButton", $"{iMark}|{strError}");
                    return false;
                }

                // 8. 使用mouse event先在rectangle的中点点击
                var rect = targetElement.Current.BoundingRectangle;
                int x = (int)((rect.Left + rect.Right) / 2);
                int y = (int)((rect.Top + rect.Bottom) / 2);

                var textFromButton = targetElement.Current.Name;

                bool isDoubleClick = false;
                if ((!string.IsNullOrEmpty(strParaMeter)) && (strParaMeter.IndexOf("LEFT_DBL_CLICK", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    isDoubleClick = true;
                }
                // 点击中点
                MarsWindowsAPIs.SetCursorPos(x, y);
                System.Threading.Thread.Sleep(100);
                MarsWindowsAPIsExtend.LeftMouseClick(x, y);
                if (isDoubleClick)
                {
                    System.Threading.Thread.Sleep(100);
                    MarsWindowsAPIsExtend.LeftMouseClick(x, y);
                }
                System.Threading.Thread.Sleep(200);
                dealResult = new MARSDealResult
                {
                    ResultMessage = "SUCCESS",
                    ErrorMessage = "",
                    ReturnedData = textFromButton, //targetElement.Current.Name, 不能够使用这里，因为对于dialog而言，对象已经消亡了
                    ActualInputData = strData,
                    AckTime = DateTime.Now
                };
                MarsLoggerSimple.logEnd("MARSUI_ClickButton", $"{iMark}|Success|target:{textFromButton}");
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Exception in MARSUI_ClickButton: {ex.Message}";
                dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                MarsLoggerSimple.Error("MARSUI_ClickButton", $"{iMark}|{strError}", ex);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("MARSUI_ClickButton", $"{iMark}|Completed");
            }
        }

        /// <summary>
        /// 等待进程准备接收输入
        /// </summary>
        /// <param name="maxWaitSeconds">最大等待时间（秒）</param>
        /// <returns>是否准备就绪</returns>
        private static bool WaitForProcessReady(int maxWaitSeconds)
        {
            try
            {
                var startTime = DateTime.Now;
                while ((DateTime.Now - startTime).TotalMilliseconds < maxWaitSeconds)
                {
                    // 这里可以添加具体的进程状态检查逻辑
                    // 目前简化处理，等待一小段时间后返回true
                    System.Threading.Thread.Sleep(100);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 处理sendandwaits格式的字符串，包括{^--control}
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <returns>处理后的字符串</returns>
        private static string ProcessSendAndWaitsFormat(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // 处理{^--control}格式的特殊字符
            // 这里可以根据实际需求扩展更多的特殊字符处理
            string result = input;

            // 处理{^--control}格式
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\{\^--control\}", "\u0017"); // Control character

            // 可以添加更多特殊字符的处理
            // 例如：{^--enter} -> Enter键
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\{\^--enter\}", "\r");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\{\^--tab\}", "\t");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\{\^--space\}", " ");

            return result;
        }

        private static string CreatePegRuntimePath(MarsSpiedObjectInfo peg)
        {
            try
            {
                if (peg == null) return "";
                string name = string.IsNullOrEmpty(peg.objectName) ? (peg.Text ?? "Unknown") : peg.objectName;
                // 简化版本：顶层一般为 UIA
                return $"UIA:{name}";
            }
            catch (Exception)
            {
                return "";
            }
        }

        private static string GetPathComponent(Object info, ref bool isOk, ref string strError)
        {
            try
            {
                if (info == null) return "";
                string objectName = "";
                // Determine the technology type
                string techType = "";
                if (info is AutomationElement uiae)
                {
                    techType = "UIA";
                    objectName = uiae.Current.Name;
                }
                else if (info is Accessibility.IAccessible iacc)
                {
                    techType = "IAcc";
                    objectName = iacc.get_accName(0);
                }
                else
                {
                    techType = "Unknown";
                    objectName = "Unknown";
                }
                isOk = true;
                return $"{techType}:{objectName}";
            }
            catch (Exception ex)
            {
                isOk = false;
                MarsLoggerSimple.Error("GetPathComponent", strError = $"Error getting path component: {ex.Message}", ex);
                return "Error";
            }
        }

        public static List<string> GenerateMarsTypePath(MarsSpiedObjectInfo sourceObjectInfo, ref bool isOk, ref string strError)
        {
            List<string> typPath = new List<string>();
            try
            {
                var currentNode = sourceObjectInfo.referenceToObj;
                TreeWalker walker = TreeWalker.RawViewWalker;
                // Traverse up to the root node
                while (currentNode != null && currentNode is AutomationElement info)
                {
                    if (info.Current.ProcessId != MARSTestProcess.CurrentTestProcessId)
                        break;
                    string component = info.Current.ControlType.ProgrammaticName;
                    if (!isOk)
                    {
                        MarsLoggerSimple.Error("GenerateMarsNamePath", $"Failed to get path component: {strError}|{Environment.StackTrace}");
                        return null;
                    }
                    if (!string.IsNullOrEmpty(component))
                    {
                        typPath.Insert(0, component); // Insert at beginning to maintain root-to-target order
                    }
                    currentNode = walker.GetParent(info);
                }
                return typPath;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GenerateMarsNamePath", strError = $"Error generating path: {ex.Message}", ex);
                isOk = false;
                return null;
            }

        }

        public static List<string> GenerateMarsNamePath(MarsSpiedObjectInfo sourceObjectInfo, ref bool isOk, ref string strError)
        {
            try
            {
                var pathComponents = new List<string>();
                var currentNode = sourceObjectInfo.referenceToObj;
                TreeWalker walker = TreeWalker.RawViewWalker;

                // Traverse up to the root node
                while (currentNode != null && currentNode is AutomationElement info)
                {
                    if (info.Current.ProcessId != MARSTestProcess.CurrentTestProcessId)
                        break;
                    string component = GetPathComponent(info, ref isOk, ref strError);
                    if (!isOk)
                    {
                        MarsLoggerSimple.Error("GenerateMarsNamePath", $"Failed to get path component: {strError}|{Environment.StackTrace}");
                        return null;
                    }
                    if (!string.IsNullOrEmpty(component))
                    {
                        pathComponents.Insert(0, component); // Insert at beginning to maintain root-to-target order
                    }
                    currentNode = walker.GetParent(info);
                }
                return pathComponents;
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("GenerateMarsNamePath", strError = $"Error generating path: {ex.Message}", ex);
                isOk = false;
                return null;
            }

        }

        /// <summary>
        /// 根据 dictObjProperties 中的 control type/object type 查找元素列表
        /// </summary>
        private static bool TryFindElementsByControlType(Dictionary<string, string> dictObjProperties,
            out List<AutomationElement> foundElements, ref string strError)
        {
            foundElements = null;
            var vars = MARSUIAppSideVariables.GetInstance();
            if (vars == null || vars.currentPegwindow == null)
            {
                strError = "Please run Pegwindow first";
                return false;
            }
            if (dictObjProperties == null)
            {
                strError = "Object properties is null; must contain control type/object type";
                return false;
            }
            string controlTypeStr = "";
            if (!DictionaryHelper.TryGetValueIgnoreCase(dictObjProperties, "control type", out controlTypeStr) &&
                !DictionaryHelper.TryGetValueIgnoreCase(dictObjProperties, "controlType", out controlTypeStr) &&
                !DictionaryHelper.TryGetValueIgnoreCase(dictObjProperties, "object type", out controlTypeStr) &&
                !DictionaryHelper.TryGetValueIgnoreCase(dictObjProperties, "objectType", out controlTypeStr))
            {
                strError = "Object properties must contain control type/object type";
                return false;
            }

            bool isOk = true;
#if DEBUG
            /// 临时调试 ，打印所有的对象的属性
            /// 

#endif
            var mappedControlType = MapControlTypFromMarsIDS(controlTypeStr, ref isOk);
            if (!isOk || mappedControlType == null)
            {
                strError = $"Invalid control type: {controlTypeStr}";
                return false;
            }

            //var cacheRequest = new CacheRequest();
            //cacheRequest.Add(AutomationElement.NameProperty);
            //cacheRequest.Add(AutomationElement.ControlTypeProperty);
            //cacheRequest.TreeScope = TreeScope.Descendants;

            var peg = vars.currentPegwindow as MarsSpiedObjectInfo;
            var pegAutoUI = peg?.referenceToObj as AutomationElement;
            if (pegAutoUI == null)
            {
                strError = "No MARSUI page is set, please recheck Object settings";
                return false;
            }
#if DEBUG
            MarsLoggerSimple.Info("TryFindElementsByControlType", $"Searching for ControlType: {controlTypeStr} under Pegwindow: {pegAutoUI.Current.Name}");
#endif
            PropertyCondition controlTypeCondition = null;
            /// 为提高性能，使用path方式定位根对象集合，算法如下：
            /// 1，判断是否有TypePath和MarsNamePath属性，如果有，继续
            /// 2，获得当前pegwindow的AutomationElement，以及name path和control type，对比层次。如果Pegwindow有2层，
            /// 就将typepath和namepath的前面两层去掉，如果没有了，就是从该对象开始找，过滤control type
            ///
            /// 

            //List<AutomationElement> results;
            ////using (cacheRequest.Activate())
            //{
            //controlTypeCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, mappedControlType);

            // 4. 在最终元素中查找所有符合条件的子对象，要注意，层次的设定
            controlTypeCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, mappedControlType);

            // 检查是否指定了搜索深度限制
            int searchDepth = 8; // 默认无限深度
            if (DictionaryHelper.TryGetValueIgnoreCase(dictObjProperties, "SearchDepth", out string depthStr) ||
                DictionaryHelper.TryGetValueIgnoreCase(dictObjProperties, "Depth", out depthStr) ||
                DictionaryHelper.TryGetValueIgnoreCase(dictObjProperties, "MaxDepth", out depthStr))
            {
                if (int.TryParse(depthStr, out int parsedDepth))
                {
                    searchDepth = parsedDepth;
                    MarsLoggerSimple.Info("TryFindElementsByControlType", $"Using search depth limit: {searchDepth}");
                }
            }

            // 使用新方法查找元素，支持深度限制
            foundElements = FindElementsByControlTypeWithDepthLimit(pegAutoUI, controlTypeCondition, searchDepth, ref strError);

            if (foundElements == null || foundElements.Count == 0)
            {
                strError = $"No elements found with control type: {controlTypeStr} in filtered path (depth: {searchDepth})";
                MarsLoggerSimple.Error("TryFindElementsByControlType", strError);
                return false;
            }

            //    var cachedResults = pegAutoUI.FindAll(TreeScope.Descendants, controlTypeCondition);
            //    results = cachedResults.Cast<AutomationElement>().ToList();
            //}

            //var results = pegAutoUI.FindAll(TreeScope.Descendants, controlTypeCondition);
            //if (results == null || results.Count == 0)
            //{
            //    strError = $"No elements found with control type: {controlTypeStr}";
            //    return false;
            //}
            //foundElements = new List<AutomationElement> ( results.Cast<AutomationElement>() );
#if DEBUG
            /// 调试模式，打印找到的对象信息，包括ControlType，Name，AutomationId等
            /// 
            foreach (var itm in foundElements)
            {
                try
                {
                    var ae = itm as AutomationElement;
                    if (ae != null)
                    {
                        string info = $"|ControlType: {ae.Current.ControlType.ProgrammaticName}, Name: {ae.Current.Name}, AutomationId: {ae.Current.AutomationId}";
                        MarsLoggerSimple.Info("TryFindElementsByControlType", info);
                    }
                }
                catch { }

            }
#endif
            return true;
        }

        /// <summary>
        /// 在指定的根元素下查找特定ControlType的元素，支持限定搜索深度
        /// </summary>
        /// <param name="rootElement">根元素，从此元素开始搜索</param>
        /// <param name="controlTypeCondition">要查找的ControlType条件</param>
        /// <param name="maxDepth">最大搜索深度，0表示只搜索直接子元素，-1表示无限深度(Descendants)</param>
        /// <param name="strError">错误信息</param>
        /// <returns>找到的元素列表</returns>
        public static List<AutomationElement> FindElementsByControlTypeWithDepthLimit(
            AutomationElement rootElement,
            PropertyCondition controlTypeCondition,
            int maxDepth,
            ref string strError)
        {
            var results = new List<AutomationElement>();

            try
            {
                if (rootElement == null)
                {
                    strError = "Root element is null";
                    return results;
                }

                // 如果maxDepth为-1，使用Descendants搜索所有后代
                if (maxDepth == -1)
                {
                    var foundElements = rootElement.FindAll(TreeScope.Descendants, controlTypeCondition);
                    if (foundElements != null && foundElements.Count > 0)
                    {
                        foreach (AutomationElement element in foundElements)
                        {
                            results.Add(element);
                        }
                    }
                    return results;
                }

                // 使用深度限制的递归搜索
                FindElementsByControlTypeRecursive(rootElement, controlTypeCondition, maxDepth, 0, results, ref strError);

                return results;
            }
            catch (Exception ex)
            {
                strError = $"Error in FindElementsByControlTypeWithDepthLimit: {ex.Message}";
                MarsLoggerSimple.Error("FindElementsByControlTypeWithDepthLimit", strError, ex);
                return results;
            }
        }

        /// <summary>
        /// 递归查找指定深度内的特定ControlType元素
        /// </summary>
        /// <param name="currentElement">当前元素</param>
        /// <param name="controlTypeCondition">要查找的ControlType条件</param>
        /// <param name="maxDepth">最大搜索深度</param>
        /// <param name="currentDepth">当前深度</param>
        /// <param name="results">结果列表</param>
        /// <param name="strError">错误信息</param>
        private static void FindElementsByControlTypeRecursive(
            AutomationElement currentElement,
            PropertyCondition controlTypeCondition,
            int maxDepth,
            int currentDepth,
            List<AutomationElement> results,
            ref string strError)
        {
            try
            {
                // 如果已达到最大深度，停止递归
                if (currentDepth > maxDepth)
                {
                    return;
                }

                // 获取当前元素的直接子元素
                var children = currentElement.FindAll(TreeScope.Children, Condition.TrueCondition);
                if (children == null || children.Count == 0)
                {
                    return;
                }

                foreach (AutomationElement child in children)
                {
                    try
                    {
                        // 检查当前子元素是否符合条件
                        // controlTypeCondition.Value 是 ControlType 对象，需要比较 Id
                        int expectedControlType = (int)controlTypeCondition.Value;
                        MarsLoggerSimple.Info("\t", $"depth:{currentDepth}|{child.Current.NativeWindowHandle:X}|{child.Current.Name}|{child.Current.ClassName}|{child.Current.ControlType.ProgrammaticName}|{child.Current.ItemType}|{child.Current.BoundingRectangle}|");

                        if (child.Current.ControlType != null &&
                            //expectedControlType != null &&
                            child.Current.ControlType.Id == expectedControlType)
                        {
                            results.Add(child);
                        }

                        // 如果还没达到最大深度，继续递归搜索
                        if (currentDepth < maxDepth)
                        {
                            FindElementsByControlTypeRecursive(child, controlTypeCondition, maxDepth, currentDepth + 1, results, ref strError);
                        }
                    }
                    catch (Exception ex)
                    {
                        MarsLoggerSimple.Error("FindElementsByControlTypeRecursive", $"Error processing child at depth {currentDepth}: {ex.Message}");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Error("FindElementsByControlTypeRecursive", $"Error at depth {currentDepth}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 使用UIA查找下拉列表(ControlType.List)并选择匹配文本
        /// </summary>
        public static bool MARSUI_SelectDropdown(long stepId, Dictionary<string, string> dictPegProperties,
            Dictionary<string, string> dictObjProperties, string strParaMeter, string strData, string typeName, string strAttachInfo,
            string pegWindName, string objName, ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("MARSUI_SelectDropdown", $"{iMark}|stepId:{stepId}|{pegWindName}.{objName}|data:{strData}|{MarsWindowsAPIsExtend.Dic2String(dictObjProperties)}");
            bool isOk = false;
            try
            {
                // 强制使用 List 类型
                //dictObjProperties = dictObjProperties ?? new Dictionary<string, string>();
                //dictObjProperties["control type"] = "list";

                if (!TryFindElementsByControlType(dictObjProperties, out var foundLists, ref strError))
                {
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_SelectDropdown", $"{iMark}|{strError}");
                    return false;
                }

                var visibleLists = FilterObjectsByProperties(dictObjProperties, foundLists, ref isOk, ref strError);

                // 过滤可见的 List
                //var visibleLists = new List<AutomationElement>();
                //foreach (AutomationElement lst in foundLists)
                //{
                //    try { if (!lst.Current.IsOffscreen) visibleLists.Add(lst); } catch { }
                //}
                //if (visibleLists.Count == 0)
                //{
                //    strError = "No visible List control found";
                //    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                //    MarsLoggerSimple.Error("MARSUI_SelectDropdown", $"{iMark}|{strError}");
                //    return false;
                //}

                // 默认取第一个可见 List（可扩展支持 index）
                var list = visibleLists[0];

                // 1. 模拟用户点击下拉按钮打开列表
                MarsLoggerSimple.Info("MARSUI_SelectDropdown", $"{iMark}|Attempting to click dropdown button");

                // 先查找下拉按钮（通常是 Button 类型的子控件）
                var buttonCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);
                var buttons = list.FindAll(TreeScope.Descendants, buttonCondition);

                if (buttons != null && buttons.Count > 0)
                {
                    // 找到按钮，点击它
                    var btn = buttons[0];
                    var btnRect = btn.Current.BoundingRectangle;
                    int btnX = (int)((btnRect.Left + btnRect.Right) / 2);
                    int btnY = (int)((btnRect.Top + btnRect.Bottom) / 2);
                    MarsLoggerSimple.Info("MARSUI_SelectDropdown", $"{iMark}|Clicking dropdown button at ({btnX},{btnY})");
                    MarsWindowsAPIs.SetCursorPos(btnX, btnY);
                    System.Threading.Thread.Sleep(200);
                    MarsWindowsAPIsExtend.LeftMouseClick(btnX, btnY);
                }
                else
                {
                    // 没有找到按钮，点击列表控件中心
                    var listRect = list.Current.BoundingRectangle;
                    int listX = (int)((listRect.Left + listRect.Right) / 2);
                    int listY = (int)((listRect.Top + listRect.Bottom) / 2);
                    MarsLoggerSimple.Info("MARSUI_SelectDropdown", $"{iMark}|No button found, clicking list center at ({listX},{listY})");
                    MarsWindowsAPIs.SetCursorPos(listX, listY);
                    System.Threading.Thread.Sleep(200);
                    MarsWindowsAPIsExtend.LeftMouseClick(listX, listY);
                }

                // 2. 使用线程和最大等待循环等待弹出菜单显示
                MarsLoggerSimple.Info("MARSUI_SelectDropdown", $"{iMark}|Starting popup wait thread with 10s timeout");

                bool popupFound = false;
                var waitEvent = new System.Threading.ManualResetEvent(false);
                var startTime = DateTime.Now;
                const int maxWaitMs = 10000; // 10秒最大等待时间
                const int checkIntervalMs = 100; // 每100ms检查一次

                // 创建STA线程来持续检查Popup窗口（UI操作需要在STA模式下进行）
                var waitThread = new System.Threading.Thread(() =>
                {
                    try
                    {
                        while (!popupFound && (DateTime.Now - startTime).TotalMilliseconds < maxWaitMs)
                        {
                            // 检查是否有Popup窗口出现
                            //var popupCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem);
                            //var popups = list.FindAll(TreeScope.Descendants, popupCondition);

                            //if (popups != null && popups.Count > 0)
                            //{
                            //    MarsLoggerSimple.Info("MARSUI_SelectDropdown", $"{iMark}|Popup found after {(DateTime.Now - startTime).TotalMilliseconds:F0}ms");
                            //    popupFound = true;
                            //    waitEvent.Set();
                            //    break;
                            //}

                            // 也检查是否有ListItem出现（某些情况下Popup可能不直接可见）
                            var itemCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem);
                            var items = list.FindAll(TreeScope.Descendants, itemCondition);

                            if (items != null && items.Count > 0)
                            {
                                MarsLoggerSimple.Info("MARSUI_SelectDropdown", $"{iMark}|ListItems found after {(DateTime.Now - startTime).TotalMilliseconds:F0}ms");
                                popupFound = true;
                                waitEvent.Set();
                                break;
                            }

                            System.Threading.Thread.Sleep(checkIntervalMs);
                        }

                        if (!popupFound)
                        {
                            MarsLoggerSimple.Warning("MARSUI_SelectDropdown", $"{iMark}|Popup wait timeout after {maxWaitMs}ms");
                            waitEvent.Set();
                        }
                    }
                    catch (Exception ex)
                    {
                        MarsLoggerSimple.Error("MARSUI_SelectDropdown", $"{iMark}|Exception in popup wait thread: {ex.Message}");
                        waitEvent.Set();
                    }
                })
                {
                    IsBackground = false, // 使用前台线程确保能完成等待
                    Name = "MARSUI_PopupWaitThread"
                };

                // 设置线程为STA模式，用于UI操作
                waitThread.SetApartmentState(System.Threading.ApartmentState.STA);

                waitThread.Start();

                // 等待线程完成或超时
                if (!waitEvent.WaitOne(maxWaitMs))
                {
                    MarsLoggerSimple.Warning("MARSUI_SelectDropdown", $"{iMark}|Wait event timeout after {maxWaitMs}ms");
                }

                // 确保线程结束
                if (!waitThread.Join(1000))
                {
                    MarsLoggerSimple.Warning("MARSUI_SelectDropdown", $"{iMark}|Wait thread did not finish in time");
                }
                System.Threading.Thread.Sleep(500);
                // 3. 重新查找 ListItem（可能在弹出的 Popup 中）
                var itemCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem);
                var items = list.FindAll(TreeScope.Descendants, itemCondition);
                if (items == null || items.Count == 0)
                {
                    strError = "No items found in the List after opening dropdown";
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_SelectDropdown", $"{iMark}|{strError}");
                    return false;
                }

                // 4. Debug模式：打印所有子对象的基本信息
                //#if DEBUG
                MarsLoggerSimple.Info("MARSUI_SelectDropdown", $"{iMark}|Found {items.Count} items, listing all:");
                for (int i = 0; i < items.Count; i++)
                {
                    try
                    {
                        var it = items[i];
                        string itemName = it.Current.Name ?? "(no name)";
                        string itemType = it.Current.ControlType?.ProgrammaticName ?? "(no type)";
                        var itemRect = it.Current.BoundingRectangle;
                        bool isOffscreen = it.Current.IsOffscreen;
                        MarsLoggerSimple.Info("MARSUI_SelectDropdown",
                            $"{iMark}|  [{i}] Name='{itemName}', Type={itemType}, Rect=({itemRect.Left},{itemRect.Top},{itemRect.Right},{itemRect.Bottom}), IsOffscreen={isOffscreen}");
                    }
                    catch (Exception ex)
                    {
                        MarsLoggerSimple.Warning("MARSUI_SelectDropdown", $"{iMark}|  [{i}] Failed to get item info: {ex.Message}");
                    }
                }
                //#endif

                // 5. 查找匹配的项
                AutomationElement picked = null;
                List<AutomationElement> listitemMatched = new();
                string strAllItemNames = "";
                string allSelectedText = "";
                for (int i = 0; i < items.Count; i++)
                {
                    var it = items[i];
                    try
                    {
                        string name = it.Current.Name ?? "";
                        strAllItemNames = $"{strAllItemNames};{name}";
                        if (MarsWindowsAPIsExtend.RegularTest(strData ?? "", name))
                        {
                            listitemMatched.Add(it);
                            picked = it;
                            MarsLoggerSimple.Info("MARSUI_SelectDropdown", $"{iMark}|Matched item at index {i}: '{name}'");
                            //break;
                        }
                    }
                    catch { }
                }

                if (listitemMatched.Count > 1)
                {

                    foreach (var itm in listitemMatched) {
                        if (itm == null)
                            continue;
                        allSelectedText = $"{allSelectedText};{itm.Current.Name ?? ""}";
                    }
                    strError = $"find multiple items|{allSelectedText}|try to find|{strData}|->|{allSelectedText}|\r\nPlease make sure only one item can be selected";
                    MarsLoggerSimple.Error("MARSUI_SelectDropdown", strError, Environment.StackTrace);
                    isOk = false;
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    return false;
                }
                if (listitemMatched.Count <= 0)
                {
                    strError = $"Can't find |{strData}| from list|{strAllItemNames}. Please make sure the item exists";
                    MarsLoggerSimple.Error("MARSUI_SelectDropdown", strError, Environment.StackTrace);
                    isOk = false;
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    return false;
                }
                picked = listitemMatched[0];

                // 6. 如果目标不在屏幕内，尝试滚动
                if (picked.Current.IsOffscreen)
                {
                    MarsLoggerSimple.Info("MARSUI_SelectDropdown", $"{iMark}|Target item is offscreen, attempting to scroll into view");
                    try
                    {
                        object scrollObj;
                        if (picked.TryGetCurrentPattern(ScrollItemPattern.Pattern, out scrollObj) && scrollObj is ScrollItemPattern scrollItem)
                        {
                            scrollItem.ScrollIntoView();
                            System.Threading.Thread.Sleep(300);
                            MarsLoggerSimple.Info("MARSUI_SelectDropdown", $"{iMark}|Scrolled item into view using ScrollItemPattern");
                        }
                        else
                        {
                            // 尝试在父容器上滚动
                            var parent = TreeWalker.ControlViewWalker.GetParent(picked);
                            if (parent != null && parent.TryGetCurrentPattern(ScrollPattern.Pattern, out scrollObj) && scrollObj is ScrollPattern scrollPattern)
                            {
                                // 简单向下滚动
                                if (scrollPattern.Current.VerticallyScrollable)
                                {
                                    scrollPattern.ScrollVertical(ScrollAmount.SmallIncrement);
                                    System.Threading.Thread.Sleep(300);
                                    MarsLoggerSimple.Info("MARSUI_SelectDropdown", $"{iMark}|Scrolled parent container");
                                }
                            }
                        }
                    }
                    catch (Exception exScroll)
                    {
                        MarsLoggerSimple.Warning("MARSUI_SelectDropdown", $"{iMark}|Failed to scroll: {exScroll.Message}");
                    }
                }

                // 7. 点击目标项
                try
                {
                    var rect = picked.Current.BoundingRectangle;
                    int x = (int)((rect.Left + rect.Right) / 2);
                    int y = (int)((rect.Top + rect.Bottom) / 2);
                    MarsLoggerSimple.Info("MARSUI_SelectDropdown", $"{iMark}|Clicking target item at ({x},{y})");
                    MarsWindowsAPIs.SetCursorPos(x, y);
                    System.Threading.Thread.Sleep(100);
                    MarsWindowsAPIsExtend.LeftMouseClick(x, y);
                }
                catch (Exception exClick)
                {
                    strError = $"Failed to click list item: {exClick.Message}";
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_SelectDropdown", $"{iMark}|{strError}");
                    return false;
                }

                dealResult = new MARSDealResult
                {
                    ResultMessage = "SUCCESS",
                    ErrorMessage = "",
                    ReturnedData = picked.Current.Name,
                    ActualInputData = strData,
                    AckTime = DateTime.Now
                };
                MarsLoggerSimple.logEnd("MARSUI_SelectDropdown", $"{iMark}|Success|picked:{picked.Current.Name}");
                return true;
            }
            catch (Exception ex)
            {
                strError = $"Exception in MARSUI_SelectDropdown: {ex.Message}";
                dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                MarsLoggerSimple.Error("MARSUI_SelectDropdown", $"{iMark}|{strError}", ex);
                return false;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stepId"></param>
        /// <param name="dictPegProperties"></param>
        /// <param name="dictObjProperties"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="typeName"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="pegWindName"></param>
        /// <param name="objName"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static bool MARSUI_CaptureValue(long stepId, Dictionary<string, string> dictPegProperties,
            Dictionary<string, string> dictObjProperties, string strParaMeter, string strData, string typeName, string strAttachInfo,
            string pegWindName, string objName, ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("MARSUI_CaptureValue", $"{iMark}|stepId:{stepId}|{pegWindName}.{objName}|{strParaMeter}|data:{strData}");
            try
            {
                if (string.IsNullOrEmpty(typeName))
                {
                    strError = "typeName is null or empty";
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_CaptureValue", $"{iMark}|{strError}");
                    return false;
                }
                /// 判断是什么类型的对象需要捕获
                /// 
                // 统一查找指定ControlType的元素
                if (!TryFindElementsByControlType(dictObjProperties, out var foundElements, ref strError))
                {
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_CaptureValue", $"{iMark}|{strError}");
                    return false;
                }

                // 5. 从objProperties的key中，除去control type外还有那些？如果有Text/ObjectName，从UIA对象中获得Name，value，text的值，并且用RegularTest判断是否符合，如果符合，放到待选列表中
                var candidates = new List<AutomationElement>();
                bool isOk = false;
                candidates = FilterObjectsByProperties(dictObjProperties, foundElements, ref isOk, ref strError);
                // 6. 如果没有候选对象，报错，有多个，判断是否有index？否则报错
                if ((candidates.Count == 0) || (!isOk))
                {
                    strError = $"No matching elements found based on the specified criteria|{strError}";
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_CaptureValue", $"{iMark}|{strError}");
                    return false;
                }

                var targetElement = candidates[0];

                FlashControlHelper.FlashControlByXORDrawing(targetElement);
                /// 已经获得目标对象，然后依据不同的类型进行捕获
                /// 
                switch (typeName.ToLower())
                {
                    case "wintable":
                    case "swftable":
                        /// 因为是wintable，需要使用IAccessible接口
                        /// 
                        return CaptureValueTableHelper.CaptureValueTable("CaptureValue", targetElement, pegWindName, objName,
                            dictPegProperties, dictObjProperties, strParaMeter, strData, ref strError, ref dealResult);
                    case "winedit":
                    case "swfedit":
                        return CaptureValueEditHelper.CaptureValueEditor("CaptureValue", targetElement, pegWindName, objName,
                            dictPegProperties, dictObjProperties, strParaMeter, strData, ref strError, ref dealResult);
                    case "wincombobox":
                    case "swfcombobox":
                        return CaptureValueComboboxHelper.CaptureValueCombobox("CaptureValue", targetElement, pegWindName, objName,
                            dictPegProperties, dictObjProperties, strParaMeter, strData, ref strError, ref dealResult);
                    case "swflabel":
                    case "winstatic":
                    case "wintext":
                        return CaptureValueLabelOrStaticHelper.CaptureValueLabel("CaptureValue", targetElement, pegWindName, objName,
                            dictPegProperties, dictObjProperties, strParaMeter, strData, ref strError, ref dealResult);
                    default:
                        strError = $"Unsupported typeName for CaptureValue|{typeName}|currently only wintable/swftable are supported";
                        dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError };
                        return false;

                }
            }
            catch (Exception e)
            {
                strError = $"Exception in MARSUI_CaptureValue: {e.Message}";
                dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError };
                MarsLoggerSimple.Error("MARSUI_CaptureValue", $"{iMark}|{strError}", e);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("MARSUI_CaptureValue", $"{iMark}|Completed");
            }

        }

        public static bool MARSUI_Snapshot(long stepId, Dictionary<string, string> dictPegProperties,
            Dictionary<string, string> dictObjProperties, string strParaMeter, string strData, string typeName, string strAttachInfo,
            string pegWindName, string objName, ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("MARSUI_Snapshot", $"{iMark}|stepId:{stepId}|{pegWindName}.{objName}|{strParaMeter}|data:{strData}");
            try
            {
                if (string.IsNullOrEmpty(typeName))
                {
                    strError = "typeName is null or empty";
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_Snapshot", $"{iMark}|{strError}|{Environment.StackTrace}");
                    return false;
                }
                string tmpTypeName = string.IsNullOrEmpty(typeName) ? "" : typeName.ToLower();
                bool isPegwindow = Mars.message.Inter.MQCenter.objectTypeMapping.MarsObjectKeyword.cnst_pegwindow.Equals(tmpTypeName, StringComparison.OrdinalIgnoreCase);
                AutomationElement targetElement = null;
                if (isPegwindow)
                {
                    /// 如果是pegwindow，直接获取当前pegwindow的AutomationElement对象
                    dictObjProperties = dictPegProperties;
                }
                /// 如果要截图的是pegwindow本身，不需要再去找对象，以为为pegwindow已经保留，且text之类会变化，导致无法找到对象
                /// 如果是pegwindow，直接获取当前pegwindow的AutomationElement对象 
                if (isPegwindow
                    //&& ("no_reget".Equals(strParaMeter, StringComparison.OrdinalIgnoreCase))
                   )
                {
                    var vars = MARSUIAppSideVariables.GetInstance();
                    if (vars == null || vars.currentPegwindow == null)
                    {
                        strError = "Please run Pegwindow first";
                        MarsLoggerSimple.Error("MARSUI_Snapshot", $"{iMark}|{strError}|{Environment.StackTrace}");
                        dealResult = new MARSDealResult { ResultMessage = $"FAILED, {strError}", ErrorMessage = strError };
                        return false;
                    }
                    var curPeg = vars.currentPegwindow as MarsSpiedObjectInfo;
                    if (curPeg == null)
                    {
                        strError = "Only MARSUI object issupport";
                        MarsLoggerSimple.Error("MARSUI_Snapshot", $"{iMark}|{strError}|{Environment.StackTrace}");
                        dealResult = new MARSDealResult { ResultMessage = $"FAILED, {strError}", ErrorMessage = strError };
                        return false;
                    }
                    var curPegUIA = curPeg.referenceToObj as AutomationElement;
                    if (curPegUIA == null)
                    {
                        strError = "(Reference object)Only MARSUI object issupport";
                        MarsLoggerSimple.Error("MARSUI_Snapshot", $"{iMark}|{strError}|{Environment.StackTrace}");
                        dealResult = new MARSDealResult { ResultMessage = $"FAILED, {strError}", ErrorMessage = strError };
                        return false;
                    }
                    targetElement = curPegUIA;
                }
                else
                {   
                    /// 判断是什么类型的对象需要捕获
                    /// 
                    // 统一查找指定ControlType的元素
                    if (!TryFindElementsByControlType(dictObjProperties ?? dictPegProperties, out var foundElements, ref strError))
                    {
                        dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                        MarsLoggerSimple.Error("MARSUI_Snapshot", $"{iMark}|{strError}|{Environment.StackTrace}");
                        return false;
                    }

                    // 5. 从objProperties的key中，除去control type外还有那些？如果有Text/ObjectName，从UIA对象中获得Name，value，text的值，并且用RegularTest判断是否符合，如果符合，放到待选列表中
                    var candidates = new List<AutomationElement>();
                    bool isOk = false;                    
                    candidates = FilterObjectsByProperties(dictObjProperties, foundElements, ref isOk, ref strError);
                    
                    // 6. 如果没有候选对象，报错，有多个，判断是否有index？否则报错
                    if ((candidates.Count == 0) || (!isOk))
                    {
                        strError = $"No matching elements found based on the specified criteria|{strError}";
                        dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                        MarsLoggerSimple.Error("MARSUI_Snapshot", $"{iMark}|{strError}|{Environment.StackTrace}");
                        return false;
                    }

                    targetElement = candidates[0];
                }
                /// 已经获得目标对象，然后依据不同的类型进行捕获
                /// 
                return SnapshotHelper.SnapshotMARSUIObj(targetElement, pegWindName, objName,
                            dictPegProperties, dictObjProperties, strParaMeter, strData, ref strError, ref dealResult);

            }
            catch (Exception e)
            {
                strError = $"Exception in MARSUI_CaptureValue: {e.Message}";
                dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                MarsLoggerSimple.Error("MARSUI_Snapshot", $"{iMark}|{strError}", e);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("MARSUI_Snapshot", $"{iMark}|Completed");
            }

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stepId"></param>
        /// <param name="dictPegProperties">可能为空</param>
        /// <param name="dictObjProperties">可能为空。如果peg和obj都是空，那么parameter必须是Default_MARSUI_Popup_menu</param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="typeName"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="pegWindName"></param>
        /// <param name="objName"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static bool MARSUI_SelectMenuItem(long stepId, Dictionary<string, string> dictPegProperties,
            Dictionary<string, string> dictObjProperties, string strParaMeter, string strData, string typeName, string strAttachInfo,
            string pegWindName, string objName, ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("MARSUI_SelectMenuItem", $"{iMark}|stepId:{stepId}|{pegWindName}.{objName}|{strParaMeter}|data:{strData}");
            try
            {
                /// 判断是否是全局的弹出菜单
                /// 
                if (!string.IsNullOrEmpty(strParaMeter) && (PopupMenuHelper.IsPopupMenuRequired(strParaMeter)))
                {
                    return PopupMenuHelper.SelectMenuItemFromGlobalPopupMenu(pegWindName, objName,
                            dictPegProperties, dictObjProperties, strParaMeter, strData,
                            ref strError, ref dealResult);
                }

                if (string.IsNullOrEmpty(typeName))
                {
                    strError = "typeName is null or empty";
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_SelectMenuItem", $"{iMark}|{strError}");
                    return false;
                }


                /// 判断是什么类型的对象需要捕获
                /// 
                // 统一查找指定ControlType的元素
                if (!TryFindElementsByControlType(dictObjProperties, out var foundElements, ref strError))
                {
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_SelectMenuItem", $"{iMark}|{strError}");
                    return false;
                }

                // 5. 从objProperties的key中，除去control type外还有那些？如果有Text/ObjectName，从UIA对象中获得Name，value，text的值，并且用RegularTest判断是否符合，如果符合，放到待选列表中
                var candidates = new List<AutomationElement>();
                bool isOk = false;
                candidates = FilterObjectsByProperties(dictObjProperties, foundElements, ref isOk, ref strError);
                // 6. 如果没有候选对象，报错，有多个，判断是否有index？否则报错
                if ((candidates.Count == 0) || (!isOk))
                {
                    strError = $"No matching elements found based on the specified criteria|{strError}";
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_SelectMenuItem", $"{iMark}|{strError}");
                    return false;
                }

                var targetElement = candidates[0];
                /// 已经获得目标对象，然后依据不同的类型进行捕获
                /// 
                return RibbonMenuHelper.SelectMenuItem(targetElement, pegWindName, objName,
                            dictPegProperties, dictObjProperties, strParaMeter, strData,
                            ref strError, ref dealResult);

            }
            catch (Exception e)
            {
                strError = $"Exception in MARSUI_CaptureValue: {e.Message}";
                dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                MarsLoggerSimple.Error("MARSUI_SelectMenuItem", $"{iMark}|{strError}", e);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("MARSUI_SelectMenuItem", $"{iMark}|Completed");
            }
        }

        public static bool MARSUI_ClickMenuIcon(long stepId, Dictionary<string, string> dictPegProperties,
            Dictionary<string, string> dictObjProperties, string strParaMeter, string strData, string typeName, string strAttachInfo,
            string pegWindName, string objName, ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("MARSUI_ClickMenuIcon", $"{iMark}|stepId:{stepId}|{pegWindName}.{objName}|{strParaMeter}|data:{strData}");
            try
            {
                if (string.IsNullOrEmpty(typeName))
                {
                    strError = "typeName is null or empty";
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_ClickMenuIcon", $"{iMark}|{strError}");
                    return false;
                }
                /// 判断是什么类型的对象需要捕获
                /// 
                // 统一查找指定ControlType的元素
                if (!TryFindElementsByControlType(dictObjProperties, out var foundElements, ref strError))
                {
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_ClickMenuIcon", $"{iMark}|{strError}");
                    return false;
                }

                // 5. 从objProperties的key中，除去control type外还有那些？如果有Text/ObjectName，从UIA对象中获得Name，value，text的值，并且用RegularTest判断是否符合，如果符合，放到待选列表中
                var candidates = new List<AutomationElement>();
                bool isOk = false;
                candidates = FilterObjectsByProperties(dictObjProperties, foundElements, ref isOk, ref strError);
                // 6. 如果没有候选对象，报错，有多个，判断是否有index？否则报错
                if ((candidates.Count == 0) || (!isOk))
                {
                    strError = $"No matching elements found based on the specified criteria|{strError}";
                    dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_ClickMenuIcon", $"{iMark}|{strError}");
                    return false;
                }

                var targetElement = candidates[0];
                MouseTrackRecorders.lastMouseInRectange = targetElement.Current.BoundingRectangle;
                /// 已经获得目标对象，然后依据不同的类型进行捕获
                /// 
                return RibbonMenuHelper.ClickMenuIcon(targetElement, pegWindName, objName,
                            dictPegProperties, dictObjProperties, strParaMeter, strData,
                            ref strError, ref dealResult);

            }
            catch (Exception e)
            {
                strError = $"Exception in MARSUI_CaptureValue: {e.Message}";
                dealResult = new MARSDealResult { ResultMessage = "FAILED", ErrorMessage = strError };
                MarsLoggerSimple.Error("MARSUI_ClickMenuIcon", $"{iMark}|{strError}", e);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("MARSUI_ClickMenuIcon", $"{iMark}|Completed");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stepId"></param>
        /// <param name="objPegProperties"></param>
        /// <param name="dicObjProperties"></param>
        /// <param name="strPara"></param>
        /// <param name="strData"></param>
        /// <param name="typeName"></param>
        /// <param name="attachText"></param>
        /// <param name="pegName"></param>
        /// <param name="objName"></param>
        /// <param name="strError"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static bool MARSUI_SearchAndClick(int stepId, Dictionary<string, string> objPegProperties,
            Dictionary<string, string> dicObjProperties,
            string strPara, string strData, string typeName, string attachText,
            string pegWindName, string objName, ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("MARSUI_SearchAndClick", $"{iMark}|stepId:{stepId}|{pegWindName}.{objName}|{strPara}|data:{strData}");
            try
            {
                if (string.IsNullOrEmpty(typeName))
                {
                    strError = "typeName is null or empty";
                    dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_SearchAndClick", $"{iMark}|{strError}|{Environment.StackTrace}");
                    return false;
                }
                /// 判断是什么类型的对象需要捕获
                /// 
                // 统一查找指定ControlType的元素
                if (!TryFindElementsByControlType(dicObjProperties, out var foundElements, ref strError))
                {
                    dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_SearchAndClick", $"{iMark}|{strError}|{Environment.StackTrace}");
                    return false;
                }

                // 5. 从objProperties的key中，除去control type外还有那些？如果有Text/ObjectName，从UIA对象中获得Name，value，text的值，并且用RegularTest判断是否符合，如果符合，放到待选列表中
                var candidates = new List<AutomationElement>();
                bool isOk = false;
                candidates = FilterObjectsByProperties(dicObjProperties, foundElements, ref isOk, ref strError);
                // 6. 如果没有候选对象，报错，有多个，判断是否有index？否则报错
                if ((candidates.Count == 0) || (!isOk))
                {
                    strError = $"No matching elements found based on the specified criteria|{strError}";
                    dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_SearchAndClick", $"{iMark}|{strError}|{Environment.StackTrace}");
                    return false;
                }

                var targetElement = candidates[0];

                // 7. 判断进程是否处于等待接收输入情况，最长等待时间180秒
                if (!WaitForProcessReady(1800))
                {
                    strError = "Process is not ready to receive input within 180 seconds";
                    dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_SearchAndClick", $"{iMark}|{strError}|{Environment.StackTrace}");
                    return false;
                }
                // Test code to get children
                //var walker = TreeWalker.RawViewWalker;
                //var child = walker.GetFirstChild(targetElement);
                //List<AutomationElement> lstChild = new List<AutomationElement>();
                //while (child != null)
                //{
                //    // 处理 child
                //    lstChild.Add(child);
                //    child = walker.GetNextSibling(child);
                //}

                /// 8 判断是否需要使用standard的技术， IAccessible
                /// 
                if (!DictionaryHelper.TryGetValueIgnoreCase(dicObjProperties, "winClass", out string gridClass))
                {
                    var allChildren = targetElement.FindAll(TreeScope.Descendants, Condition.TrueCondition);
                    MarsLoggerSimple.Info("MARSUI_SearchAndClick", $"{allChildren.Count}");
                    if (allChildren.Count <= 0)
                    {
                        strError = $"can't find rows from grid";
                        MarsLoggerSimple.Error("MARSUI_SearchAndClick", $"{iMark}|{strError}|{Environment.StackTrace}");
                        dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError };
                        return false;
                    }
                    else
                    {
                        strError = $"not supported for the grid";
                        MarsLoggerSimple.Error("MARSUI_SearchAndClick", $"{strError}|{Environment.StackTrace}");
                        dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError };
                        return false;
                    }
                }
                /// 创建IAccessible对象
                /// 
                int hwnd = targetElement.Current.NativeWindowHandle;
                if (hwnd == 0)
                {
                    strError = "No validate handle for the grid";
                    MarsLoggerSimple.Error("MARSUI_SearchAndClick", $"{strError}|{Environment.StackTrace}");
                    dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError, AckTime = DateTime.Now };
                    return false;
                }
                return SearchAndClickOp.ParseAndExecuteActionFromHandle(hwnd, typeName, strPara, strData, ref strError, ref dealResult);
            }
            catch (Exception e)
            {
                strError = e.Message;
                dealResult = new MARSDealResult {
                    ResultMessage = $"FAILED,{strError}", ErrorMessage = strError,
                    AckTime = DateTime.Now
                };
                MarsLoggerSimple.Error("MARSUI_SearchAndClick", $"{iMark}|{strError}", e);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("MARSUI_SearchAndClick", $"{iMark}|Completed|{strError}|");
            }
        }

        /// <summary>
        /// 主窗口或者其他经常会在操作后，改变text，title等。现在的模式是保留了原有的pegwindow，
        /// 因此，对于pegwind的某些操作不希望再次获得pegwindow， 所以增加了"NO_REGET"的参数
        /// </summary>
        /// <param name="stepId"></param>
        /// <param name="objPegProperties"></param>
        /// <param name="dicObjProperties"></param>
        /// <param name="strPara"></param>
        /// <param name="strData"></param>
        /// <param name="typeName"></param>
        /// <param name="attachText"></param>
        /// <param name="pegWindName"></param>
        /// <param name="objName"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <returns></returns>
        public static bool MARSUI_PressKeys(long stepId, Dictionary<string, string> objPegProperties,
            Dictionary<string, string> dicObjProperties,
            string strPara, string strData, string typeName, string attachText,
            string pegWindName, string objName, ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("MARSUI_PressKeys", $"{iMark}|stepId:{stepId}|{typeName}|{pegWindName}.{objName}|{strPara}|data:{strData}");
            try
            {

                if ((!string.IsNullOrEmpty(strPara)) && ("CURRENT_POS".Equals(strPara, StringComparison.OrdinalIgnoreCase)))
                {
                    MarsLoggerSimple.Info("MARSUI_PressKeys", "no object required mode, just press keys");
                    WaitForProcessReady(180);
                    if (!KeyboardAgent.SendKeysWithSendInput(strData, ref strError))
                    {
                        dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError };
                        MarsLoggerSimple.Error("MARSUI_PressKeys", $"{iMark}|{strError}|{Environment.StackTrace}");
                        return false;
                    }
                    System.Threading.Thread.Sleep(200);
                    dealResult.ResultMessage = "SUCCESS";
                    dealResult.ActualInputData = strData;
                    dealResult.AckTime = DateTime.Now;
                    return true;
                }

                AutomationElement targetElement = null;
                if (string.IsNullOrEmpty(typeName))
                {
                    strError = "typeName is null or empty";
                    dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_PressKeys", $"{iMark}|{strError}|{Environment.StackTrace}");
                    return false;
                }
                bool isToFindObject = true;
                if (!string.IsNullOrEmpty(strPara))
                {
                    if ((!string.IsNullOrEmpty(typeName)) && (typeName.Equals("pegwindow", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (strPara.Equals("NO_REGET", StringComparison.OrdinalIgnoreCase))
                        {
                            // 不需要重新获取pegwindow
                            var vars = MARSUIAppSideVariables.GetInstance();
                            if (vars == null || vars.currentPegwindow == null)
                            {
                                strError = "Please run Pegwindow first";
                                MarsLoggerSimple.DEBUG("MARSUI_PressKeys", $"{iMark}|{strError}|{Environment.StackTrace}");
                                dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError };
                                return false;
                            }
                            /// 将window，设置focus
                            /// 
                            var curPeg = vars.currentPegwindow as MarsSpiedObjectInfo;
                            if (curPeg == null) {
                                strError = "Only MARSUI object is supported.";
                                MarsLoggerSimple.DEBUG("MARSUI_PressKeys", $"{iMark}|{strError}|{Environment.StackTrace}");
                                dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError };
                                return false;
                            }
                            var curPegUI = curPeg.referenceToObj as AutomationElement;
                            if (curPegUI == null)
                            {
                                strError = "(Reference object) Only MARSUI object is supported.";
                                MarsLoggerSimple.DEBUG("MARSUI_PressKeys", $"{iMark}|{strError}|{Environment.StackTrace}");
                                dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError };
                                return false;
                            }
                            try
                            {
                                curPegUI.SetFocus();
                            } catch (Exception e)
                            {

                            }
                            isToFindObject = false;
                            System.Threading.Thread.Sleep(20);
                            targetElement = curPegUI;
                        }
                        else
                        {
                            strError = $"Unsupported parameter for pegwindow|{strPara}|currently only NO_REGET is supported";
                            dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError };
                            MarsLoggerSimple.Error("MARSUI_PressKeys", $"{iMark}|{strError}|{Environment.StackTrace}");
                            return false;
                        }
                    }
                    //else
                    //{
                    //    strError = $"Unsupported parameter for type|{typeName}|currently only pegwindow is supported";
                    //    dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError };
                    //    MarsLoggerSimple.Error("MARSUI_PressKeys", $"{iMark}|{strError}|{Environment.StackTrace}");
                    //    return false;
                    //}
                }
                if (isToFindObject)
                {
                    /// 判断是什么类型的对象需要捕获
                    /// 
                    // 统一查找指定ControlType的元素
                    if (!TryFindElementsByControlType(dicObjProperties, out var foundElements, ref strError))
                    {
                        dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError };
                        MarsLoggerSimple.Error("MARSUI_PressKeys", $"{iMark}|{strError}|{Environment.StackTrace}");
                        return false;
                    }

                    // 5. 从objProperties的key中，除去control type外还有那些？如果有Text/ObjectName，从UIA对象中获得Name，value，text的值，并且用RegularTest判断是否符合，如果符合，放到待选列表中
                    var candidates = new List<AutomationElement>();
                    bool isOk = false;
                    candidates = FilterObjectsByProperties(dicObjProperties, foundElements, ref isOk, ref strError);
                    // 6. 如果没有候选对象，报错，有多个，判断是否有index？否则报错
                    if ((candidates.Count == 0) || (!isOk))
                    {
                        strError = $"No matching elements found based on the specified criteria|{strError}";
                        dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError };
                        MarsLoggerSimple.Error("MARSUI_PressKeys", $"{iMark}|{strError}|{Environment.StackTrace}");
                        return false;
                    }

                    targetElement = candidates[0];
                }

                // 7. 判断进程是否处于等待接收输入情况，最长等待时间180秒
                if (!WaitForProcessReady(180))
                {
                    strError = "Process is not ready to receive input within 180 seconds";
                    dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError };
                    MarsLoggerSimple.Error("MARSUI_PressKeys", $"{iMark}|{strError}|{Environment.StackTrace}");
                    return false;
                }
                if (isToFindObject)
                {
                    // Test code to get children
                    //var walker = TreeWalker.RawViewWalker;
                    //var child = walker.GetFirstChild(targetElement);
                    //List<AutomationElement> lstChild = new List<AutomationElement>();
                    //while (child != null)
                    //{
                    //    // 处理 child
                    //    lstChild.Add(child);
                    //    child = walker.GetNextSibling(child);
                    //}

                    /// 8 判断是否需要使用standard的技术， IAccessible
                    /// 
                    if (!DictionaryHelper.TryGetValueIgnoreCase(dicObjProperties, "winClass", out string gridClass))
                    {
                        var allChildren = targetElement.FindAll(TreeScope.Descendants, Condition.TrueCondition);
                        MarsLoggerSimple.Info("MARSUI_PressKeys", $"{allChildren.Count}");
                        if (allChildren.Count <= 0)
                        {
                            strError = $"can't find rows from grid";
                            MarsLoggerSimple.Error("MARSUI_PressKeys", $"{iMark}|{strError}|{Environment.StackTrace}");
                            dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError };
                            return false;
                        }
                        else
                        {
                            strError = $"not supported for the grid";
                            MarsLoggerSimple.Error("MARSUI_PressKeys", $"{strError}|{Environment.StackTrace}");
                            dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError };
                            return false;
                        }
                    }
                }
                /// 创建IAccessible对象
                /// 
                int hwnd = targetElement.Current.NativeWindowHandle;
                if (hwnd == 0)
                {
                    strError = "No validate handle for the grid";
                    MarsLoggerSimple.Error("MARSUI_PressKeys", $"{strError}|{Environment.StackTrace}");
                    dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError, AckTime = DateTime.Now };
                    return false;
                }
                return KeyPressOp.ParseAndExecuteActionFromHandle(hwnd, typeName, strPara, strData, ref strError, ref dealResult);
            }
            catch (Exception e)
            {
                strError = e.Message;
                dealResult = new MARSDealResult
                {
                    ResultMessage = $"FAILED,{strError}",
                    ErrorMessage = strError,
                    AckTime = DateTime.Now
                };
                MarsLoggerSimple.Error("MARSUI_PressKeys", $"{iMark}|{strError}", e);
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("MARSUI_PressKey", $"{iMark}|Completed|{strError}|");
            }
        }


        public static bool ScanMenuAroundCursor(ref AutomationElement targetMenu, ref string strError, ref string strAdv, ref string strStatck, int offsetX = 10, int offsetY = 10)
        {
            MarsLoggerSimple.logBegin("ScanMenuAroundCursor",$"offsetX|{offsetX}|offsetY|{offsetY}");
            // 获取当前鼠标位置
            Mars.message.windowsWrapper.SystemUtil.POINT pt = default;
            if (!MarsWindowsAPIs.GetCursorPos(ref pt))
            {
                strError = "can't get Cursor positon";
                strAdv = "please ensure that monitor is connected and Mouse is enabled";
                strStatck = Environment.StackTrace;
                return false;
            }

            // 八个方向偏移
            var directions = new[]
            {
                new { dx = offsetX, dy = offsetY, name = "BottomRight" },
                new { dx = 0, dy = -offsetY, name = "up" },
                new { dx = 0, dy = offsetY, name = "down" },
                new { dx = -offsetX, dy = 0, name = "left" },
                new { dx = offsetX, dy = 0, name = "right" },
                new { dx = -offsetX, dy = -offsetY, name = "TopLeft" },
                new { dx = offsetX, dy = -offsetY, name = "TopRight" },
                new { dx = -offsetX, dy = offsetY, name = "BottomLeft" }
            };

            foreach (var dir in directions)
            {
                int x = pt.X + dir.dx;
                int y = pt.Y + dir.dy;
                IntPtr hwnd = MarsWindowsAPIs.WindowFromPoint(new System.Drawing.Point(x, y));
                if (hwnd == IntPtr.Zero)
                {
                    strError = $"Direction[{dir.name}],no hwnd from point";
                    MarsLoggerSimple.Info("ScanMenuAroundCursor", strError);
                    continue;
                }

                AutomationElement element = AutomationElement.FromHandle(hwnd);
                if (element == null)
                {
                    MarsLoggerSimple.Info("ScanMenuAroundCursor", $"direction[{dir.name}]:no AutomationElement object from hanlde");
                    continue;
                }

                if (element.Current.ControlType == ControlType.Menu)
                {
                    MarsLoggerSimple.Info("ScanMenuAroundCursor", $"Direction[{dir.name}]:find popup menu: {element.Current.Name}|{element.Current.ControlType.ProgrammaticName}");
                    targetMenu = element;
                    return true;
                }
                else
                {
                    MarsLoggerSimple.Info("ScanMenuAroundCursor", $"direction[{dir.name}]:not PopupMenu, is {element.Current.ControlType.ProgrammaticName}");
                }
            }
            strError = "can't find popup menu around cursor";
            strAdv = "Please ensure that popup menu is popped";
            strStatck = Environment.StackTrace;
            MarsLoggerSimple.Error("ScanMenuAroundCursor", $"{strError}\r\n{strAdv}\r\n{strStatck}");
            return false;
        }


        public static bool Performance_SelectMenuItemPopup(string strData, int offsetX, int offsetY, ref string strReturnedData, ref string strError, ref string strAdv, ref string strStack)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("Performance_SelectMenuItemPopup", $"{strData}|offsetX:{offsetX},offsetY:{offsetY}");
            // 获取当前鼠标位置
            Mars.message.windowsWrapper.SystemUtil.POINT pt = default;
            if (!MarsWindowsAPIs.GetCursorPos(ref pt))
            {
                strError = "Can't get Cursor postion";
                strAdv = "Please ensure that a monitor is connected and Mouse is enabled";
                strStack = Environment.StackTrace;
                MarsLoggerSimple.Error("Performance_SelectMenuItemPopup", $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                return false;
            }
            AutomationElement element = default;
            bool isOk = ScanMenuAroundCursor(ref element, ref strError, ref strAdv, ref strStack, offsetX, offsetY);
            if (!isOk)
            {
                MarsLoggerSimple.Error("Performance_SelectMenuItemPopup", $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                return false;
            }

            MarsLoggerSimple.Info("Performance_SelectMenuItemPopup", $"find menu|{element.Current.Name}|{element.Current.ControlType.ProgrammaticName}");

            var menuNames = (strData ?? string.Empty)
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrEmpty(item))
                .ToList();
            if (menuNames.Count == 0)
            {
                strError = "Menu data is empty";
                strAdv = "Please provide at least one menu item";
                strStack = Environment.StackTrace;
                MarsLoggerSimple.Error("Performance_SelectMenuItemPopup", $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                return false;
            }

            AutomationElement currentMenu = element;
            List<string> selectedMenuNames = new List<string>();
            const int submenuWaitTimeoutMilliseconds = 2000;
            const int submenuWaitIntervalMilliseconds = 200;

            for (int level = 0; level < menuNames.Count; level++)
            {
                string targetMenuItem = menuNames[level];
                MarsLoggerSimple.Info("Performance_SelectMenuItemPopup", $"level {level}|target menu: {targetMenuItem}");

                AutomationElementCollection menuItems = currentMenu.FindAll(TreeScope.Descendants, Condition.TrueCondition);
                List<AutomationElement> matchedItems = new List<AutomationElement>();
                string strAllMenuitems = "";
                foreach (AutomationElement menuItem in menuItems)
                {
                    MarsLoggerSimple.Info("Performance_SelectMenuItemPopup",
                        $"level {level}|current item: {menuItem.Current.Name}|{menuItem.Current.ItemType}|{menuItem.Current.ControlType.ProgrammaticName}|{menuItem.Current.Name}|{menuItem.Current.ClassName}");
                    strAllMenuitems = $"{strAllMenuitems};{menuItem.Current.Name}";
                    if (MarsWindowsAPIsExtend.RegularTest(targetMenuItem, menuItem.Current.Name))
                    {
                        matchedItems.Add(menuItem);
                    }
                }

                if (matchedItems.Count != 1)
                {
                    strError = $"find {matchedItems.Count} Menuitems: {targetMenuItem}|from |{strAllMenuitems}|";
                    strAdv = "Plese change data settings and ensure only one item could be selected";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error("Performance_SelectMenuItemPopup", $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }

                var matchedItem = matchedItems[0];
                var rect = matchedItem.Current.BoundingRectangle;
                if ((rect.Width == 0) || (rect.Height == 0))
                {
                    strError = "Menuitem has no valid bounding rectangle";
                    strAdv = "Please ensure that the menu item is visible";
                    strStack = Environment.StackTrace;
                    MarsLoggerSimple.Error("Performance_SelectMenuItemPopup", $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                    return false;
                }

                selectedMenuNames.Add(matchedItem.Current.Name);
                int clickX = (int)(rect.Left + rect.Width / 2);
                int clickY = (int)(rect.Top + rect.Height / 2);
                MarsLoggerSimple.Info("Performance_SelectMenuItemPopup", $"level {level}|click position: {clickX},{clickY}");
                MarsWindowsAPIsExtend.LeftMouseClick(clickX, clickY);
                System.Threading.Thread.Sleep(200);

                if (level < menuNames.Count - 1)
                {
                    bool hasChildMenuItems = false;
                    DateTime waitStart = DateTime.Now;

                    while ((DateTime.Now - waitStart).TotalMilliseconds <= submenuWaitTimeoutMilliseconds)
                    {
                        try
                        {
                            AutomationElementCollection childMenuItems = matchedItem.FindAll(TreeScope.Descendants, Condition.TrueCondition);
                            /// 其实只要判断是否存在子对象即可
                            if ((childMenuItems != null) && (childMenuItems.Count > 0))
                            {
                                foreach (AutomationElement child in childMenuItems)
                                {
                                    if (child == null)
                                    {
                                        continue;
                                    }
                                    if (child.Equals(matchedItem))
                                    {
                                        continue;
                                    }
                                    hasChildMenuItems = true;
                                    break;
                                }
                            }
                        }
                        catch (ElementNotAvailableException)
                        {
                            strError = $"Menu item is no longer available: {matchedItem.Current.Name}";
                            strAdv = "Please ensure the menu remains open while selecting sub menu items";
                            strStack = Environment.StackTrace;
                            MarsLoggerSimple.Error("Performance_SelectMenuItemPopup", $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                            return false;
                        }

                        if (hasChildMenuItems)
                        {
                            MarsLoggerSimple.Info("Performance_SelectMenuItemPopup",
                                $"level {level}|submenu detected under menu item {matchedItem.Current.Name}");
                            break;
                        }
                        System.Threading.Thread.Sleep(submenuWaitIntervalMilliseconds);
                    }

                    if (!hasChildMenuItems)
                    {
                        strError = $"can't find submenu under menu item: {matchedItem.Current.Name}";
                        strAdv = "Please ensure the submenu is available and visible";
                        strStack = Environment.StackTrace;
                        MarsLoggerSimple.Error("Performance_SelectMenuItemPopup", $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                        return false;
                    }

                    currentMenu = matchedItem;
                }
            }

            strReturnedData = string.Join(";", selectedMenuNames);
            if (!WaitIfMenuProcessBusy(element, ref strError, ref strAdv, ref strStack))
            {
                MarsLoggerSimple.Error("Performance_SelectMenuItemPopup", $"{iMark}|{strError}\r\n{strAdv}\r\n{strStack}");
                return false;
            }
            return true;

        }

        private static bool WaitIfMenuProcessBusy(AutomationElement menuElement, ref string strError, ref string strAdv, ref string strStack)
        {
            if (menuElement == null) return true;
            int processId = 0;
            try { processId = menuElement.Current.ProcessId; } catch { }
            if (processId <= 0) return true;

            Process process = null;
            try { process = Process.GetProcessById(processId); } catch { return true; }

            if (process == null || process.HasExited) return true;

            try { process.Refresh(); } catch { }
            if (process.Responding) return true;

            const int maxWaitMilliseconds = 10 * 60 * 1000;
            DateTime start = DateTime.UtcNow;
            BusyWaitForm waitForm = null;
            var readyEvent = new ManualResetEventSlim(false);

            var uiThread = new Thread(() =>
            {
                try
                {
                    waitForm = new BusyWaitForm();
                    waitForm.UpdateText(BuildBusyWaitText(DateTime.UtcNow - start));
                    waitForm.PositionToBottomRightOfCursorScreen();
                    readyEvent.Set();
                    System.Windows.Forms.Application.Run(waitForm);
                }
                catch
                {
                    readyEvent.Set();
                }
            })
            {
                IsBackground = true
            };
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();
            readyEvent.Wait();

            MarsLoggerSimple.Warning("Performance_SelectMenuItemPopup",
                $"Menu process is busy. Waiting up to 10 minutes. PID={processId}");

            bool timedOut = false;
            while (true)
            {
                TimeSpan elapsed = DateTime.UtcNow - start;
                if (elapsed.TotalMilliseconds >= maxWaitMilliseconds)
                {
                    timedOut = true;
                    break;
                }

                try
                {
                    process.Refresh();
                    if (process.HasExited || process.Responding) break;
                }
                catch
                {
                    break;
                }

                UpdateBusyWaitForm(waitForm, elapsed);
                Thread.Sleep(1000);
            }

            CloseBusyWaitForm(waitForm);

            if (timedOut)
            {
                strError = $"Menu process still busy after 10 minutes. PID={processId}";
                strAdv = "Please check target application status or retry later";
                strStack = Environment.StackTrace;
                MarsLoggerSimple.Warning("Performance_SelectMenuItemPopup", strError);
                return false;
            }
            return true;
        }

        private static void UpdateBusyWaitForm(BusyWaitForm waitForm, TimeSpan elapsed)
        {
            if (waitForm == null) return;
            waitForm.UpdateText(BuildBusyWaitText(elapsed));
        }

        private static void CloseBusyWaitForm(BusyWaitForm waitForm)
        {
            if (waitForm == null) return;
            try
            {
                if (waitForm.IsDisposed) return;
                if (waitForm.InvokeRequired)
                {
                    waitForm.BeginInvoke(new Action(() => waitForm.Close()));
                }
                else
                {
                    waitForm.Close();
                }
            }
            catch
            {
                // ignore shutdown errors
            }
        }

        private static string BuildBusyWaitText(TimeSpan elapsed)
        {
            string elapsedText = elapsed.ToString(@"mm\:ss");
            return $"target application Is busy, waiting....({elapsedText})/10min";
        }

        private sealed class BusyWaitForm : System.Windows.Forms.Form
        {
            private readonly System.Windows.Forms.Label _label;

            public BusyWaitForm()
            {
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                StartPosition = System.Windows.Forms.FormStartPosition.Manual;
                ShowInTaskbar = false;
                TopMost = true;
                BackColor = System.Drawing.Color.LightYellow;
                _label = new System.Windows.Forms.Label
                {
                    AutoSize = true,
                    Padding = new System.Windows.Forms.Padding(8),
                    Text = "target application Is busy, waiting....(00:00)/10min"
                };
                Controls.Add(_label);
            }

            protected override bool ShowWithoutActivation => true;

            protected override System.Windows.Forms.CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                    return cp;
                }
            }

            public void UpdateText(string text)
            {
                if (IsDisposed) return;
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => _label.Text = text));
                }
                else
                {
                    _label.Text = text;
                }
            }

            public void PositionToBottomRightOfCursorScreen()
            {
                var screen = System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position);
                var workArea = screen.WorkingArea;
                int x = Math.Max(workArea.Left, workArea.Right - Width - 10);
                int y = Math.Max(workArea.Top, workArea.Bottom - Height - 10);
                Location = new System.Drawing.Point(x, y);
            }
        }
    }

}
