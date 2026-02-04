using Mars.message.AutoTestingDriver.interProcess;
using MarsEnginer.windowsWrapper.SystemUtil;

//using Mars.message.windowsWrapper.SystemUtil;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.ExecuteTestcase.keywordOp
{
    public class InputHelper
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(InputHelper));
        public const string cnstClickAtOffset_fromCurpos = "offset_fromCurPos";
        /// <summary>
        /// 算法：从当前位置偏移点击
        /// 
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="stepId"></param>
        /// <param name="dictPegProperties"></param>
        /// <param name="dictObjProperties"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="tYPE_NAME"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <returns></returns>
        internal static bool DoClickAtOffset_fromCurpos(string keyWordsName, long stepId, 
            Dictionary<string, string> dictPegProperties, 
            Dictionary<string, string> dictObjProperties, string strParaMeter, string strData, string tYPE_NAME, 
            string strAttachInfo, string pegName, string objName, 
            ref string strError, ref MARSDealResult dealResult)
        {
            Logger.logBegin("DoClickAtOffset_fromCurpos", $"{keyWordsName}|{stepId}|{strData}|{strParaMeter}|{tYPE_NAME}|pegname:{pegName}|objName:{objName}|");
            /// 检查参数是否是cnstClickAtOffset_fromCurpos
            /// 
            try
            {
                if (dealResult == null)
                {
                    dealResult = new MARSDealResult();
                }
                if (!cnstClickAtOffset_fromCurpos.Equals(strParaMeter ?? "", StringComparison.OrdinalIgnoreCase))
                {
                    strError = $"parameter is wrong, expected is：{cnstClickAtOffset_fromCurpos}，but it is：{strParaMeter}";
                    Logger.Error("keyWordsName", strError);
                    dealResult.ActualInputData = $"{strParaMeter}:{strData}";
                    dealResult.ReturnedData = $"FAILED,{strError}";
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.ErrorMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    Logger.Error("DoClickAtOffset_fromCurpos", $"{strError}", Environment.StackTrace);
                    return false;
                }

                /// 解析数据，格式：X偏移量,Y偏移量   
                /// 
                if (string.IsNullOrEmpty(strData))
                {
                    strError = $"Data should not empty, should be x:y";
                    Logger.Error("keyWordsName", strError);
                    dealResult.ActualInputData = $"{strParaMeter}:{strData}";
                    dealResult.ReturnedData = $"FAILED,{strError}";
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.ErrorMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    Logger.Error("DoClickAtOffset_fromCurpos", $"{strError}", Environment.StackTrace);
                    return false;
                }
                string[] arrOffset = strData.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (arrOffset.Length != 2)
                {
                    strError = $"Data format is wrong, expected is x:y, but it is {strData}";
                    Logger.Error("keyWordsName", strError);
                    dealResult.ActualInputData = $"{strParaMeter}:{strData}";
                    dealResult.ReturnedData = $"FAILED,{strError}";
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.ErrorMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    Logger.Error("DoClickAtOffset_fromCurpos", $"{strError}", Environment.StackTrace);
                    return false;
                }
                if (!int.TryParse(arrOffset[0], out int xOffset))
                {
                    strError = $"X offset is not valid integer, it is {arrOffset[0]}";
                    Logger.Error("keyWordsName", strError);
                    dealResult.ActualInputData = $"{strParaMeter}:{strData}";
                    dealResult.ReturnedData = $"FAILED,{strError}";
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.ErrorMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    Logger.Error("DoClickAtOffset_fromCurpos", $"{strError}", Environment.StackTrace);
                    return false;
                }
                if (!int.TryParse(arrOffset[1], out int yOffset))
                {
                    strError = $"Y offset is not valid integer, it is {arrOffset[1]}";
                    Logger.Error("keyWordsName", strError);
                    dealResult.ActualInputData = $"{strParaMeter}:{strData}";
                    dealResult.ReturnedData = $"FAILED,{strError}";
                    dealResult.ResultMessage = $"FAILED,{strError}";
                    dealResult.ErrorMessage = $"FAILED,{strError}";
                    dealResult.AckTime = DateTime.Now;
                    Logger.Error("DoClickAtOffset_fromCurpos", $"{strError}", Environment.StackTrace);
                    return false;
                }
                /// 计算目标位置
                /// 
                System.Drawing.Point curPos = System.Windows.Forms.Cursor.Position;
                int targetX = curPos.X + xOffset;
                int targetY = curPos.Y + yOffset;
                /// 执行点击
                /// 
                Logger.Info("DoClickAtOffset_fromCurpos", $"Click at offset from current position, current position is {curPos.X},{curPos.Y}, offset is {xOffset},{yOffset}, target position is {targetX},{targetY}");
                System.Threading.Thread.Sleep(100);
                MarsWindowsAPIsExtend.LeftMouseClick(targetX, targetY);
                Logger.Info("DoClickAtOffset_fromCurpos", $"Clicked");
                dealResult.ResultMessage = MARSDealResult.CNST_SUCCESS;
                dealResult.AckTime = DateTime.Now;
                dealResult.ReturnedData = MARSDealResult.CNST_SUCCESS;
                dealResult.ActualInputData = $"{strParaMeter}:{strData}";
                return true;
            }
            finally
            {
                Logger.logEnd("DoClickAtOffset_fromCurpos");
            }
        }
    }
}
