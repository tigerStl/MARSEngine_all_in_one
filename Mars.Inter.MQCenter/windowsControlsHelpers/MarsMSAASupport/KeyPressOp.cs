using Mars.Inter.MQCenter.MSAASupport;
using Mars.message.AutoTestingDriver.interProcess;
using Mars.message.Inter.MQCenter.simpleLog;
using System;
using System.Collections.Generic;
using Accessibility;
using Mars.AutoTestingDriver.ExecuteTestcase.MarsMSAASupport;
using Mars.message.windowsWrapper.SystemUtil;
using Mars.Inter.MQCenter.MarsUtility;
namespace Mars.Inter.MQCenter.windowsControlsHelpers.MarsMSAASupport
{
    public class KeyPressOp
    {

        public static bool ParseAndExecuteActionFromHandle(int hwnd, string strObjMarsType, string strParaMeter, string strData, ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("ParseAndExecuteActionFromHandle", $"{iMark}|{strParaMeter}|{strData}|{strObjMarsType}");
            try
            {
                if (hwnd == 0)
                {
                    strError = "No validate handle for datagrid";
                    MarsLoggerSimple.Error("ParseAndExecuteActionFromHandle", $"{strError}|{Environment.StackTrace}");
                    dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError, AckTime = DateTime.Now };
                    return false;
                }
                var provider = new MarsAutoAccessibleSupportProvider();
                bool isOk = provider.CreateAccessibleObject(new IntPtr(hwnd), ref strError);
                if (!isOk)
                {
                    MarsLoggerSimple.Error("ParseAndExecuteActionFromHandle", $"{strError}|{Environment.StackTrace}");
                    dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError, AckTime = DateTime.Now };
                    return false;
                }
                return ParseAndExecuteAction(provider.AccessibleObject, strObjMarsType, strParaMeter, strData, ref strError, ref dealResult);
            }
            catch (Exception e)
            {
                strError = e.Message;
                MarsLoggerSimple.Error("ParseAndExecuteActionFromHandle", $"{iMark}|{strError}|{Environment.StackTrace}", e);
                dealResult = new MARSDealResult { ResultMessage = $"FAILED,{strError}", ErrorMessage = strError, AckTime = DateTime.Now };
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("ParseAndExecuteActionFromHandle", $"{iMark}|");
            }
        }

        public static bool ParseAndExecuteAction(dynamic targetObject, string strObjMarsType, string strParaMeter, string strData, ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = new Random().Next(10000);
            MarsLoggerSimple.logBegin("KeyPressOp.ParseAndExecuteAction", $"{iMark}|Parsing action: {strParaMeter}, ObjectType: {strObjMarsType}, Data: {strData}");
            try
            {
                if (targetObject == null)
                {
                    strError = "Target object is null.";
                    MarsLoggerSimple.Error("KeyPressOp.ParseAndExecuteAction", $"{iMark}|{strError}");
                    dealResult.ResultMessage = $"FAILED:{strError}";
                    dealResult.ActualInputData = strData;
                    dealResult.AckTime = DateTime.Now;
                    dealResult.Advice = "Please check the error message for details.";
                    dealResult.StackInfo = Environment.StackTrace;
                    return false;
                }   
                if (!(targetObject is IAccessible))
                {
                    strError = $"Target object is not Standard object Type.";
                    MarsLoggerSimple.Error("KeyPressOp.ParseAndExecuteAction", $"{iMark}|{strError}");
                    dealResult.ResultMessage = $"FAILED:{strError}";
                    dealResult.ActualInputData = strData;
                    dealResult.AckTime = DateTime.Now;
                    dealResult.Advice = "Please check the error message for details.";
                    dealResult.StackInfo = Environment.StackTrace;
                    return false;
                }
                /// 获得目标的Rectangle，然后点击，最后使用SendKeys发送按键
                /// 
                IAccessible accessible = (IAccessible)targetObject;
                bool isOk = false;
                System.Drawing.Rectangle rect = MARSMSAAHelper.GetRectangle(accessible, ref isOk, ref strError);
                if (!isOk)
                {
                    strError = $"Failed to get rectangle of target object. {strError}";
                    MarsLoggerSimple.Error("KeyPressOp.ParseAndExecuteAction", $"{iMark}|{strError}");
                    dealResult.ResultMessage = $"FAILED:{strError}";
                    dealResult.ActualInputData = strData;
                    dealResult.AckTime = DateTime.Now;
                    dealResult.Advice = "Please check the error message for details.";
                    dealResult.StackInfo = Environment.StackTrace;
                    return false;
                }
                if (isOk && (rect.Width <= 0 || rect.Height <= 0))
                {
                    strError = $"The rectangle of target object is invalid. {strError}";
                    MarsLoggerSimple.Error("KeyPressOp.ParseAndExecuteAction", $"{iMark}|{strError}");
                    dealResult.ResultMessage = $"FAILED:{strError}";
                    dealResult.ActualInputData = strData;
                    dealResult.AckTime = DateTime.Now;
                    dealResult.Advice = "Please check the error message for details.";
                    dealResult.StackInfo = Environment.StackTrace;
                    return false;
                }
                int x = rect.Left + rect.Width / 2;
                int y = rect.Top + rect.Height / 2;
                if ((string.IsNullOrEmpty(strParaMeter) || (strParaMeter.IndexOf("CURRENT_POS_NO_CLICK", StringComparison.OrdinalIgnoreCase) < 0)))
                {
                    /// 左键点击
                    /// 
                    MarsWindowsAPIsExtend.LeftMouseClick(x, y);
                }
                System.Threading.Thread.Sleep(200);

                // 发送按键 - 使用更可靠的SendInput方法替代SendKeys
                if (!KeyboardAgent.SendKeysWithSendInput(strData, ref strError))
                {
                    MarsLoggerSimple.Warning("KeyPressOp.ParseAndExecuteAction", $"{iMark}|SendInput failed, falling back to SendKeys: {strError}");
                    // 回退到SendKeys方法
                    try
                    {
                        System.Windows.Forms.SendKeys.SendWait(strData);
                    }
                    catch (Exception ex)
                    {
                        strError = $"Both SendInput and SendKeys failed: {ex.Message}";
                        MarsLoggerSimple.Error("KeyPressOp.ParseAndExecuteAction", $"{iMark}|{strError}");
                        dealResult.ResultMessage = $"FAILED:{strError}";
                        dealResult.ActualInputData = strData;
                        dealResult.AckTime = DateTime.Now;
                        dealResult.Advice = "Please check the error message for details.";
                        dealResult.StackInfo = Environment.StackTrace;
                        return false;
                    }
                }
                System.Threading.Thread.Sleep(200);
                dealResult.ResultMessage = "SUCCESS";
                dealResult.ActualInputData = strData;
                dealResult.AckTime = DateTime.Now;
                return true;

            }
            catch (Exception e)
            {
                strError = $"Exception in KeyPressOp.ParseAndExecuteAction: {e.Message}";
                MarsLoggerSimple.Error("KeyPressOp.ParseAndExecuteAction", $"{iMark}|{strError}");
                dealResult.ResultMessage = $"FAILED:{strError}";
                dealResult.ActualInputData = strData;
                dealResult.AckTime = DateTime.Now;
                dealResult.Advice = "Please check the error message for details.";
                dealResult.StackInfo = e.StackTrace;
                return false;
            }
            finally
            {
                MarsLoggerSimple.logEnd("KeyPressOp.ParseAndExecuteAction", $"{iMark}|Finished parsing action: {strParaMeter}, ObjectType: {strObjMarsType}, Data: {strData}");
            }
        }

        




        
    }
}
