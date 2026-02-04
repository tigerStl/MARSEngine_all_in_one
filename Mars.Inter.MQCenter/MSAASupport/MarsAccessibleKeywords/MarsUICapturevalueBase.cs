using Mars.message.AutoTestingDriver.interProcess;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.windowsWrapper.SystemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace Mars.Inter.MQCenter.MSAASupport.MarsAccessibleKeywords
{
    public class MarsUICapturevalueBase 
    {
        internal static bool CaptureValue(string keywordName, AutomationElement targetElement, string pegName, string objName,
            Dictionary<string, string> dictPegProperties,
            Dictionary<string, string> dictObjProperties,
            string strParaMeter, string strData,
            ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("CaptureValue", $"{iMark}|{keywordName ?? "CaptureValue"}|({pegName}.{objName},{strParaMeter}, {strData})|{MarsWindowsAPIsExtend.Dic2String(dictObjProperties)}");
            if (dealResult == null)
                dealResult = new MARSDealResult();
            try
            {
                if (targetElement == null)
                {
                    strError = "Target element is null";
                    MarsLoggerSimple.Error("CaptureValue", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                // 检查控件类型是否支持
                var controlType = targetElement.Current.ControlType;
                if (!IsSupportedControlType(controlType))
                {
                    strError = $"Unsupported control type: {controlType}. Supported types: Document, Edit, Text, ComboBox, CheckBox, Label, Button, ListItem, TreeItem, MenuItem, RadioButton, Slider, ProgressBar, TabItem, Window.";
                    MarsLoggerSimple.Error("CaptureValue", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }
                /// 在获取前，先Highlight
                /// 1,获得元素的边界框
                /// 2，调用HighlightWindow
                /// 
                // 使用封装的高亮方法
                //ShowElementHighlight(targetElement, iMark);

                // 根据控件类型获取内容
                string content = GetElementContentByType(targetElement, controlType, iMark);

                if (string.IsNullOrEmpty(content))
                {
                    strError = "No content found in the target element";
                    MarsLoggerSimple.Warning("CaptureValueEditor", $"{iMark}|{strError}");
                    dealResult.ErrorMessage = strError;
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    return false;
                }

                // 设置成功结果
                dealResult.ReturnedData = content;
                dealResult.ResultMessage = "SUCCESS";
                dealResult.ErrorMessage = "";
                dealResult.AckTime = DateTime.Now;

                MarsLoggerSimple.Info("CaptureValue", $"{iMark}|Successfully captured content: {content.Length} characters");
                return true;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                MarsLoggerSimple.Error("CaptureValue", $"{iMark}|{keywordName ?? "CaptureValue"}|({pegName}.{objName},{strParaMeter}, {strData})|{MarsWindowsAPIsExtend.Dic2String(dictObjProperties)}|{strError}");
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("CaptureValue", $"{iMark}|{keywordName ?? "CaptureValue"}|({pegName}.{objName},{strParaMeter}, {strData})|{MarsWindowsAPIsExtend.Dic2String(dictObjProperties)}|{strError}");
            }
        }

        /// <summary>
        /// 按优先级从元素中获取内容
        /// 优先级：TextPattern > ValuePattern > Name属性
        /// </summary>
        /// <param name="targetElement">目标元素</param>
        /// <param name="iMark">日志标记</param>
        /// <returns>获取到的内容</returns>
        private static string GetElementContent(AutomationElement targetElement, int iMark)
        {
            string content = string.Empty;

            // 优先级1：尝试 TextPattern（最全面，适用于文档和富文本）
            try
            {
                if (targetElement.TryGetCurrentPattern(TextPattern.Pattern, out object textPatternObj))
                {
                    var textPattern = (TextPattern)textPatternObj;
                    var documentRange = textPattern.DocumentRange;
                    content = documentRange.GetText(-1); // -1 表示获取所有文本

                    if (!string.IsNullOrEmpty(content))
                    {
                        MarsLoggerSimple.Info("GetElementContent", $"{iMark}|Successfully got content via TextPattern: {content.Length} characters");
                        return content;
                    }
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("GetElementContent", $"{iMark}|TextPattern failed: {ex.Message}");
            }

            // 优先级2：尝试 ValuePattern（适用于可编辑控件）
            try
            {
                if (targetElement.TryGetCurrentPattern(ValuePattern.Pattern, out object valuePatternObj))
                {
                    var valuePattern = (ValuePattern)valuePatternObj;
                    content = valuePattern.Current.Value;

                    if (!string.IsNullOrEmpty(content))
                    {
                        MarsLoggerSimple.Info("GetElementContent", $"{iMark}|Successfully got content via ValuePattern: {content.Length} characters");
                        return content;
                    }
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("GetElementContent", $"{iMark}|ValuePattern failed: {ex.Message}");
            }

            // 优先级3：使用 Name 属性（最后备选）
            try
            {
                content = targetElement.Current.Name ?? string.Empty;

                if (!string.IsNullOrEmpty(content))
                {
                    MarsLoggerSimple.Info("GetElementContent", $"{iMark}|Successfully got content via Name property: {content.Length} characters");
                    return content;
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("GetElementContent", $"{iMark}|Name property failed: {ex.Message}");
            }

            // 如果所有方法都失败，尝试获取子元素的文本内容
            try
            {
                var children = targetElement.FindAll(TreeScope.Children, System.Windows.Automation.Condition.TrueCondition);
                var childContents = new List<string>();

                foreach (AutomationElement child in children)
                {
                    string childContent = GetElementContent(child, iMark);
                    if (!string.IsNullOrEmpty(childContent))
                    {
                        childContents.Add(childContent);
                    }
                }

                if (childContents.Count > 0)
                {
                    content = string.Join(" ", childContents);
                    MarsLoggerSimple.Info("GetElementContent", $"{iMark}|Successfully got content from children: {content.Length} characters");
                    return content;
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("GetElementContent", $"{iMark}|Children content extraction failed: {ex.Message}");
            }

            MarsLoggerSimple.Warning("GetElementContent", $"{iMark}|All content extraction methods failed");
            return string.Empty;
        }

        /// <summary>
        /// 检查控件类型是否支持
        /// </summary>
        /// <param name="controlType">控件类型</param>
        /// <returns>是否支持</returns>
        private static bool IsSupportedControlType(ControlType controlType)
        {
            return controlType == ControlType.Document ||
                   controlType == ControlType.Edit ||
                   controlType == ControlType.Text ||
                   controlType == ControlType.ComboBox ||
                   controlType == ControlType.CheckBox ||
                   controlType == ControlType.Text ||
                   controlType == ControlType.Button ||
                   controlType == ControlType.ListItem ||
                   controlType == ControlType.TreeItem ||
                   controlType == ControlType.MenuItem ||
                   controlType == ControlType.RadioButton ||
                   controlType == ControlType.Slider ||
                   controlType == ControlType.ProgressBar ||
                   controlType == ControlType.TabItem ||
                   controlType == ControlType.Window;
        }

        /// <summary>
        /// 根据控件类型获取内容
        /// </summary>
        /// <param name="targetElement">目标元素</param>
        /// <param name="controlType">控件类型</param>
        /// <param name="iMark">日志标记</param>
        /// <returns>获取到的内容</returns>
        private static string GetElementContentByType(AutomationElement targetElement, ControlType controlType, int iMark)
        {
            MarsLoggerSimple.Info("GetElementContentByType", $"{iMark}|Getting content for control type: {controlType.ProgrammaticName}");

            if (controlType == ControlType.ComboBox)
            {
                return GetComboBoxContent(targetElement, iMark);
            }
            else if (controlType == ControlType.CheckBox)
            {
                return GetCheckBoxContent(targetElement, iMark);
            }
            else if (controlType == ControlType.RadioButton)
            {
                return GetRadioButtonContent(targetElement, iMark);
            }
            else if (controlType == ControlType.Slider)
            {
                return GetSliderContent(targetElement, iMark);
            }
            else if (controlType == ControlType.ProgressBar)
            {
                return GetProgressBarContent(targetElement, iMark);
            }
            else if (controlType == ControlType.Button)
            {
                return GetButtonContent(targetElement, iMark);
            }
            else if (controlType == ControlType.Text)
            {
                return GetTextContent(targetElement, iMark);
            }
            else if (controlType == ControlType.ListItem)
            {
                return GetItemContent(targetElement, iMark);
            }
            else if (controlType == ControlType.TreeItem)
            {
                return GetItemContent(targetElement, iMark);
            }
            else if (controlType == ControlType.MenuItem)
            {
                return GetItemContent(targetElement, iMark);
            }
            else if (controlType == ControlType.TabItem)
            {
                return GetTabItemContent(targetElement, iMark);
            }
            else if (controlType == ControlType.Window)
            {
                return GetWindowContent(targetElement, iMark);
            }
            else if (controlType == ControlType.Document || controlType == ControlType.Edit)
            {
                return GetElementContent(targetElement, iMark);
            }
            else
            {
                return GetElementContent(targetElement, iMark);
            }
        }

        /// <summary>
        /// 获取 ComboBox 内容
        /// </summary>
        private static string GetComboBoxContent(AutomationElement targetElement, int iMark)
        {
            try
            {
                // 尝试获取选中项的值
                if (targetElement.TryGetCurrentPattern(SelectionPattern.Pattern, out object selectionPatternObj))
                {
                    var selectionPattern = (SelectionPattern)selectionPatternObj;
                    var selectedItems = selectionPattern.Current.GetSelection();
                    if (selectedItems.Length > 0)
                    {
                        string selectedText = selectedItems[0].Current.Name ?? selectedItems[0].Current.AutomationId ?? "";
                        if (!string.IsNullOrEmpty(selectedText))
                        {
                            MarsLoggerSimple.Info("GetComboBoxContent", $"{iMark}|Got selected item: {selectedText}");
                            return selectedText;
                        }
                    }
                }

                // 尝试 ValuePattern
                if (targetElement.TryGetCurrentPattern(ValuePattern.Pattern, out object valuePatternObj))
                {
                    var valuePattern = (ValuePattern)valuePatternObj;
                    string value = valuePattern.Current.Value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        MarsLoggerSimple.Info("GetComboBoxContent", $"{iMark}|Got value: {value}");
                        return value;
                    }
                }

                // 使用 Name 属性
                string name = targetElement.Current.Name ?? "";
                if (!string.IsNullOrEmpty(name))
                {
                    MarsLoggerSimple.Info("GetComboBoxContent", $"{iMark}|Got name: {name}");
                    return name;
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("GetComboBoxContent", $"{iMark}|Error: {ex.Message}");
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取 CheckBox 内容
        /// </summary>
        private static string GetCheckBoxContent(AutomationElement targetElement, int iMark)
        {
            try
            {
                // 获取选中状态
                if (targetElement.TryGetCurrentPattern(TogglePattern.Pattern, out object togglePatternObj))
                {
                    var togglePattern = (TogglePattern)togglePatternObj;
                    var state = togglePattern.Current.ToggleState;
                    string stateText = state == ToggleState.On ? "Checked" : 
                                     state == ToggleState.Off ? "Unchecked" : "Indeterminate";
                    
                    MarsLoggerSimple.Info("GetCheckBoxContent", $"{iMark}|Got state: {stateText}");
                    return stateText;
                }

                // 使用 Name 属性
                string name = targetElement.Current.Name ?? "";
                if (!string.IsNullOrEmpty(name))
                {
                    MarsLoggerSimple.Info("GetCheckBoxContent", $"{iMark}|Got name: {name}");
                    return name;
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("GetCheckBoxContent", $"{iMark}|Error: {ex.Message}");
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取 RadioButton 内容
        /// </summary>
        private static string GetRadioButtonContent(AutomationElement targetElement, int iMark)
        {
            try
            {
                // 获取选中状态
                if (targetElement.TryGetCurrentPattern(SelectionItemPattern.Pattern, out object selectionItemPatternObj))
                {
                    var selectionItemPattern = (SelectionItemPattern)selectionItemPatternObj;
                    bool isSelected = selectionItemPattern.Current.IsSelected;
                    string stateText = isSelected ? "Selected" : "Not Selected";
                    
                    MarsLoggerSimple.Info("GetRadioButtonContent", $"{iMark}|Got state: {stateText}");
                    return stateText;
                }

                // 使用 Name 属性
                string name = targetElement.Current.Name ?? "";
                if (!string.IsNullOrEmpty(name))
                {
                    MarsLoggerSimple.Info("GetRadioButtonContent", $"{iMark}|Got name: {name}");
                    return name;
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("GetRadioButtonContent", $"{iMark}|Error: {ex.Message}");
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取 Slider 内容
        /// </summary>
        private static string GetSliderContent(AutomationElement targetElement, int iMark)
        {
            try
            {
                // 获取当前值
                if (targetElement.TryGetCurrentPattern(RangeValuePattern.Pattern, out object rangeValuePatternObj))
                {
                    var rangeValuePattern = (RangeValuePattern)rangeValuePatternObj;
                    double value = rangeValuePattern.Current.Value;
                    
                    MarsLoggerSimple.Info("GetSliderContent", $"{iMark}|Got value: {value}");
                    return value.ToString();
                }

                // 使用 Name 属性
                string name = targetElement.Current.Name ?? "";
                if (!string.IsNullOrEmpty(name))
                {
                    MarsLoggerSimple.Info("GetSliderContent", $"{iMark}|Got name: {name}");
                    return name;
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("GetSliderContent", $"{iMark}|Error: {ex.Message}");
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取 ProgressBar 内容
        /// </summary>
        private static string GetProgressBarContent(AutomationElement targetElement, int iMark)
        {
            try
            {
                // 获取当前值
                if (targetElement.TryGetCurrentPattern(RangeValuePattern.Pattern, out object rangeValuePatternObj))
                {
                    var rangeValuePattern = (RangeValuePattern)rangeValuePatternObj;
                    double value = rangeValuePattern.Current.Value;
                    double maxValue = rangeValuePattern.Current.Maximum;
                    double minValue = rangeValuePattern.Current.Minimum;
                    
                    string progressText = $"{value}% ({value}/{maxValue})";
                    MarsLoggerSimple.Info("GetProgressBarContent", $"{iMark}|Got progress: {progressText}");
                    return progressText;
                }

                // 使用 Name 属性
                string name = targetElement.Current.Name ?? "";
                if (!string.IsNullOrEmpty(name))
                {
                    MarsLoggerSimple.Info("GetProgressBarContent", $"{iMark}|Got name: {name}");
                    return name;
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("GetProgressBarContent", $"{iMark}|Error: {ex.Message}");
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取 Button 内容
        /// </summary>
        private static string GetButtonContent(AutomationElement targetElement, int iMark)
        {
            try
            {
                // 使用 Name 属性
                string name = targetElement.Current.Name ?? "";
                if (!string.IsNullOrEmpty(name))
                {
                    MarsLoggerSimple.Info("GetButtonContent", $"{iMark}|Got name: {name}");
                    return name;
                }

                // 尝试获取 Value
                if (targetElement.TryGetCurrentPattern(ValuePattern.Pattern, out object valuePatternObj))
                {
                    var valuePattern = (ValuePattern)valuePatternObj;
                    string value = valuePattern.Current.Value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        MarsLoggerSimple.Info("GetButtonContent", $"{iMark}|Got value: {value}");
                        return value;
                    }
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("GetButtonContent", $"{iMark}|Error: {ex.Message}");
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取 Text/Label 内容
        /// </summary>
        private static string GetTextContent(AutomationElement targetElement, int iMark)
        {
            try
            {
                // 使用 Name 属性
                string name = targetElement.Current.Name ?? "";
                if (!string.IsNullOrEmpty(name))
                {
                    MarsLoggerSimple.Info("GetTextContent", $"{iMark}|Got name: {name}");
                    return name;
                }

                // 尝试获取 Value
                if (targetElement.TryGetCurrentPattern(ValuePattern.Pattern, out object valuePatternObj))
                {
                    var valuePattern = (ValuePattern)valuePatternObj;
                    string value = valuePattern.Current.Value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        MarsLoggerSimple.Info("GetTextContent", $"{iMark}|Got value: {value}");
                        return value;
                    }
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("GetTextContent", $"{iMark}|Error: {ex.Message}");
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取 ListItem/TreeItem/MenuItem 内容
        /// </summary>
        private static string GetItemContent(AutomationElement targetElement, int iMark)
        {
            try
            {
                // 使用 Name 属性
                string name = targetElement.Current.Name ?? "";
                if (!string.IsNullOrEmpty(name))
                {
                    MarsLoggerSimple.Info("GetItemContent", $"{iMark}|Got name: {name}");
                    return name;
                }

                // 尝试获取 Value
                if (targetElement.TryGetCurrentPattern(ValuePattern.Pattern, out object valuePatternObj))
                {
                    var valuePattern = (ValuePattern)valuePatternObj;
                    string value = valuePattern.Current.Value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        MarsLoggerSimple.Info("GetItemContent", $"{iMark}|Got value: {value}");
                        return value;
                    }
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("GetItemContent", $"{iMark}|Error: {ex.Message}");
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取 TabItem 内容
        /// </summary>
        private static string GetTabItemContent(AutomationElement targetElement, int iMark)
        {
            try
            {
                // 使用 Name 属性
                string name = targetElement.Current.Name ?? "";
                if (!string.IsNullOrEmpty(name))
                {
                    MarsLoggerSimple.Info("GetTabItemContent", $"{iMark}|Got name: {name}");
                    return name;
                }

                // 尝试获取 Value
                if (targetElement.TryGetCurrentPattern(ValuePattern.Pattern, out object valuePatternObj))
                {
                    var valuePattern = (ValuePattern)valuePatternObj;
                    string value = valuePattern.Current.Value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        MarsLoggerSimple.Info("GetTabItemContent", $"{iMark}|Got value: {value}");
                        return value;
                    }
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("GetTabItemContent", $"{iMark}|Error: {ex.Message}");
            }

            return string.Empty;
        }

        /// <summary>
        /// 获取 Window 内容
        /// </summary>
        private static string GetWindowContent(AutomationElement targetElement, int iMark)
        {
            try
            {
                // 使用 Name 属性（窗口标题）
                string name = targetElement.Current.Name ?? "";
                if (!string.IsNullOrEmpty(name))
                {
                    MarsLoggerSimple.Info("GetWindowContent", $"{iMark}|Got title: {name}");
                    return name;
                }
            }
            catch (Exception ex)
            {
                MarsLoggerSimple.Warning("GetWindowContent", $"{iMark}|Error: {ex.Message}");
            }

            return string.Empty;
        }
    }
}
