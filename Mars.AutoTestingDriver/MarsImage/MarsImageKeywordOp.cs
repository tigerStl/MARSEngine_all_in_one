using log4net.Repository.Hierarchy;
using Mars.message.AutoTestingDriver.interProcess;
using MarsEnginer.windowsWrapper.SystemUtil;

//using Mars.message.windowsWrapper.SystemUtil;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.MarsImage
{
    public class MarsImageKeywordOp
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsImageKeywordOp));

        private bool PreProcessingImageKeyword(Dictionary<string, string> dictObjProperties,
            MARSDealResult dealResult,
            ref double dPrecision,
            ref string strImageFile,
            ref string strError)
        {
            int iMark = new Random().Next(100000);
            Logger.logBegin("PreProcessingImageKeyword", $"{iMark}|");
            string idx = "";
            idx = dictObjProperties.Keys.FirstOrDefault(k => k.Equals("swfImageFile", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(idx))
            {
                strError = "no sfwImageFile exists in object location properties";
                dealResult.AckTime = DateTime.Now;
                dealResult.ResultMessage = $"FAILED, {strError}";
                return false;
            }
            // 2，swfiamgefile是一个文件名称，判断是否存在，否则返回错误。
            strImageFile = dictObjProperties[idx];
            if (!System.IO.File.Exists(strImageFile))
            {
                strError = $"swfImageFile {strImageFile} does not exist";
                dealResult.AckTime = DateTime.Now;
                dealResult.ResultMessage = $"FAILED, {strError}";
                return false;
            }
            // 3，判断dictobjProperties中是否包含precision,默认为.95 获得全屏图像，加载swfimagefile图像，在全屏图像中查找目标图像，返回位置。
            //double dPrecision = 0.95;
            idx = dictObjProperties.Keys.FirstOrDefault(k => k.Equals("precision", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(idx))
            {
                if (!double.TryParse(dictObjProperties[idx], out dPrecision))
                {
                    dPrecision = 0.95;
                }
            }
            return true;
        }

        private (double x, double y) AnalyzeOffset(Dictionary<string, string> dictObjProperties,
            ref string strError, ref bool isOk)
        {
            double xOffsetRatio = double.NaN, yOffsetRatio = double.NaN;
            string keyOffX = dictObjProperties.Keys.FirstOrDefault(k => k.Equals("offsetX", StringComparison.OrdinalIgnoreCase));
            string keyOffY = dictObjProperties.Keys.FirstOrDefault(k => k.Equals("offsetY", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(keyOffX))
            {
                /// offsetX 是一个偏移的小数。比如0.5表示向右偏移50%的宽度。因为为唯一定位一个图形区域，需要保留前后左右的空间，以定位
                /// 
                if (!double.TryParse(dictObjProperties[keyOffX], out xOffsetRatio))
                {
                    strError = $"offsetX {dictObjProperties[keyOffX]} is not a valid number";
                    Logger.Error("FillEdit", strError);
                    isOk = false;
                    return (double.NaN, double.NaN);
                }
            }
            if (!string.IsNullOrEmpty(keyOffY))
            {
                if (!double.TryParse(dictObjProperties[keyOffY], out yOffsetRatio))
                {
                    strError = $"offsetY {dictObjProperties[keyOffY]} is not a valid number";
                    Logger.Error("FillEdit", strError);
                    isOk = false;
                    return (double.NaN, double.NaN);
                }
            }
            isOk = true;
            return (xOffsetRatio, yOffsetRatio);
        }

        /// <summary>
        /// 使用opensvm进行图像识别。首先获得全屏的图像，然后在全屏图像中查找目标图像，算法如下：
        /// 1，判断dictObjProperties中是否包含“swfImageFile”属性(大小写ignore)，如果没有，返回错误。
        /// 2，swfiamgefile是一个文件名称，判断是否存在，否则返回错误。
        /// 3，判断dictobjProperties中是否包含precision,默认为.95 获得全屏图像，加载swfimagefile图像，在全屏图像中查找目标图像，返回位置。
        /// 4，判断dictobjProperties中是否包含“offsetX”和“offsetY”属性，如果有，进行位置偏移。
        /// 5，使用鼠标事件，先移动到位置，然后进行单击
        /// 6，参数中是否包括strParameter, no_clear，如果有，并且值为true,则不清除屏幕，否则清除屏幕（发送{del 20}{backspace 20}）
        /// 7，发送strData
        /// </summary>
        /// <param name="dictPegProperties"></param>
        /// <param name="dictObjProperties"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal bool FillEdit(Dictionary<string, string> dictPegProperties,
            Dictionary<string, string> dictObjProperties, string strParaMeter, string strData,
            string strComment,
            ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = (new Random()).Next(10000);
            Logger.logBegin("FillEdit", $"{iMark}|{strParaMeter}|{strData}");
            try
            {
                /// 1，判断dictObjProperties中是否包含“swfImageFile”属性(大小写ignore)，如果没有，返回错误。
                /// 
                if (dealResult == null)
                {
                    dealResult = new MARSDealResult();
                    dealResult.MessageType = MARSMessageType.e_Run_TestStep_Result;
                }
                string idx = "";
                idx = dictObjProperties.Keys.FirstOrDefault(k => k.Equals("swfImageFile", StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrEmpty(idx))
                {
                    strError = "no sfwImageFile exists in object location properties";
                    dealResult.AckTime = DateTime.Now;
                    dealResult.ResultMessage = $"FAILED, {strError}";
                    return false;
                }
                // 2，swfiamgefile是一个文件名称，判断是否存在，否则返回错误。
                string strImageFile = dictObjProperties[idx];
                if (!System.IO.File.Exists(strImageFile))
                {
                    strError = $"swfImageFile {strImageFile} does not exist";
                    dealResult.AckTime = DateTime.Now;
                    dealResult.ResultMessage = $"FAILED, {strError}";
                    return false;
                }
                // 3，判断dictobjProperties中是否包含precision,默认为.95 获得全屏图像，加载swfimagefile图像，在全屏图像中查找目标图像，返回位置。
                double dPrecision = 0.95;
                idx = dictObjProperties.Keys.FirstOrDefault(k => k.Equals("precision", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(idx))
                {
                    if (!double.TryParse(dictObjProperties[idx], out dPrecision))
                    {
                        dPrecision = 0.95;
                    }
                }
                // 获得全屏图像
                System.Drawing.Bitmap bmpScreen = MARSImageObjectHelper.CaptureScreen();
                System.Drawing.Rectangle? rect = MARSImageObjectHelper.FindImage(bmpScreen, strImageFile, dPrecision, ref strError);
                if (rect == null)
                {
                    strError = $"can't find image {strImageFile} on screen";
                    dealResult.AckTime = DateTime.Now;
                    dealResult.ResultMessage = $"FAILED, {strError}";
                    return false;
                }
                /// 4，判断dictobjProperties中是否包含“offsetX”和“offsetY”属性，如果有，进行位置偏移。
                /// 
                int clickX = rect.Value.X;
                int clickY = rect.Value.Y;
                double xOffsetRatio = 0, yOffsetRatio = 0;

                string keyOffX = dictObjProperties.Keys.FirstOrDefault(k => k.Equals("offsetX", StringComparison.OrdinalIgnoreCase));
                string keyOffY = dictObjProperties.Keys.FirstOrDefault(k => k.Equals("offsetY", StringComparison.OrdinalIgnoreCase));
                int offsetX = 0;
                if (!string.IsNullOrEmpty(keyOffX))
                {
                    /// offsetX 是一个偏移的小数。比如0.5表示向右偏移50%的宽度。因为为唯一定位一个图形区域，需要保留前后左右的空间，以定位
                    /// 
                    if (double.TryParse(dictObjProperties[keyOffX], out xOffsetRatio))
                    {
                        clickX = rect.Value.X + (int)(rect.Value.Width * xOffsetRatio);
                    }
                    else
                    {
                        strError = $"offsetX {dictObjProperties[keyOffX]} is not a valid number";
                        dealResult.AckTime = DateTime.Now;
                        dealResult.ResultMessage = $"FAILED, {strError}";
                        Logger.Error("FillEdit", strError);
                        return false;
                    }
                }
                else
                {
                    clickX = rect.Value.X + (int)(rect.Value.Width * 0.5);
                }
                if (!string.IsNullOrEmpty(keyOffY))
                {
                    if (double.TryParse(dictObjProperties[keyOffY], out yOffsetRatio))
                    {
                        clickY = rect.Value.Y + (int)(rect.Value.Height * yOffsetRatio);
                    }
                    else
                    {
                        strError = $"offsetY {dictObjProperties[keyOffY]} is not a valid number";
                        dealResult.AckTime = DateTime.Now;
                        dealResult.ResultMessage = $"FAILED, {strError}";
                        Logger.Error("FillEdit", strError);
                        return false;
                    }
                }
                else
                {
                    clickY = rect.Value.Y + (int)(rect.Value.Height * 0.5);
                }

                // 5，使用鼠标事件，先移动到位置，然后进行单击
                MarsWindowsAPIs.SetCursorPos(clickX, clickY);
                System.Threading.Thread.Sleep(500);
                MarsWindowsAPIsExtend.LeftMouseClick(clickX, clickY);
                /// 6，参数中是否包括strParameter, no_clear，如果有，并且值为true,则不清除屏幕，否则清除屏幕（发送{del 20}{backspace 20}）
                /// 
                if ((!string.IsNullOrEmpty(strParaMeter)) && (strParaMeter.IndexOf("no_clear", StringComparison.OrdinalIgnoreCase) < 0))
                {                    
                    System.Windows.Forms.SendKeys.SendWait("{DEL 20}{BACKSPACE 20}");
                    System.Threading.Thread.Sleep(500);
                }
                /// 7，发送strData
                /// 
                System.Windows.Forms.SendKeys.SendWait(strData);
                dealResult.ResultMessage = "SUCCESS";
                dealResult.ActualInputData = strData;
                dealResult.AckTime = DateTime.Now;
                Logger.Info("FillEdit", "DONE");
                return true;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                dealResult.AckTime = DateTime.Now;
                dealResult.ResultMessage = $"FAILED, {strError}";
                Logger.Error("FillEdit", strError, ex);
                return false;
            }
            finally
            {
                Logger.logEnd("FillEdit", $"{iMark}");
            }
        }
        /// <summary>
        /// clickbutton模式。和FillEdit不同的是，这里不需要先click，要先定位
        /// </summary>
        /// <param name="dictPegProperties"></param>
        /// <param name="dictObjProperties"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="strComment"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <returns></returns>
        internal bool ClickButton(Dictionary<string, string> dictPegProperties,
            Dictionary<string, string> dictObjProperties, string strParaMeter, string strData,
            string strComment,
            ref string strError, ref MARSDealResult dealResult)
        {
            int iMark = (new Random()).Next(10000);
            Logger.logBegin("ClickButton", $"{iMark}|{strParaMeter}|{strData}");
            try
            {
                /// 1，判断dictObjProperties中是否包含“swfImageFile”属性(大小写ignore)，如果没有，返回错误。
                /// 
                if (dealResult == null)
                {
                    dealResult = new MARSDealResult();
                    dealResult.MessageType = MARSMessageType.e_Run_TestStep_Result;
                }
                double dPrecision = 0.92;
                string strImageFile = "";
                bool isOk = PreProcessingImageKeyword(dictObjProperties, dealResult, ref dPrecision, ref strImageFile, ref strError);
                if (!isOk)
                {
                    Logger.Error("ClickButton", $"{iMark}|{strError}");
                    dealResult.AckTime = DateTime.Now;
                    dealResult.ResultMessage = $"FAILED, {strError}";
                    Logger.Error("ClickButton", $"{iMark}|PreProcessingImageKeyword|{strError}");
                    return false;
                }

                // 获得全屏图像
                System.Drawing.Bitmap bmpScreen = MARSImageObjectHelper.CaptureScreen();
                System.Drawing.Rectangle? rect = MARSImageObjectHelper.FindImage(bmpScreen, strImageFile, dPrecision, ref strError);
                if (rect == null)
                {
                    strError = $"can't find image {strImageFile} on screen";
                    dealResult.AckTime = DateTime.Now;
                    dealResult.ResultMessage = $"FAILED, {strError}";
                    Logger.Error("ClickButton", $"{iMark}|FindImage|{strError}");
                    return false;
                }
                /// 4，判断dictobjProperties中是否包含“offsetX”和“offsetY”属性，如果有，进行位置偏移。
                /// 
                var rslt = AnalyzeOffset(dictObjProperties, ref strError, ref isOk);
                if (!isOk)
                {
                    dealResult.AckTime = DateTime.Now;
                    dealResult.ResultMessage = $"FAILED, {strError}";
                    Logger.Error("ClickButton", $"{iMark}|AnalyzeOffset|{strError}");
                    return false;
                }
                int clickX = rect.Value.X;
                int clickY = rect.Value.Y;
                double xOffsetRatio = rslt.x, yOffsetRatio = rslt.y;
                string keyOffX = dictObjProperties.Keys.FirstOrDefault(k => k.Equals("offsetX", StringComparison.OrdinalIgnoreCase));
                string keyOffY = dictObjProperties.Keys.FirstOrDefault(k => k.Equals("offsetY", StringComparison.OrdinalIgnoreCase));
                int offsetX = 0;
                if (!string.IsNullOrEmpty(keyOffX))
                {
                    /// offsetX 是一个偏移的小数。比如0.5表示向右偏移50%的宽度。因为为唯一定位一个图形区域，需要保留前后左右的空间，以定位
                    /// 
                    if (double.TryParse(dictObjProperties[keyOffX], out xOffsetRatio))
                    {
                        clickX = rect.Value.X + (int)(rect.Value.Width * xOffsetRatio);
                    }
                    else
                    {
                        strError = $"offsetX {dictObjProperties[keyOffX]} is not a valid number";
                        dealResult.AckTime = DateTime.Now;
                        dealResult.ResultMessage = $"FAILED, {strError}";
                        Logger.Error("FillEdit", strError);
                        return false;
                    }
                }
                else
                {
                    clickX = rect.Value.X + (int)(rect.Value.Width * 0.5);
                }
                if (!string.IsNullOrEmpty(keyOffY))
                {
                    if (double.TryParse(dictObjProperties[keyOffY], out yOffsetRatio))
                    {
                        clickY = rect.Value.Y + (int)(rect.Value.Height * yOffsetRatio);
                    }
                    else
                    {
                        strError = $"offsetY {dictObjProperties[keyOffY]} is not a valid number";
                        dealResult.AckTime = DateTime.Now;
                        dealResult.ResultMessage = $"FAILED, {strError}";
                        Logger.Error("FillEdit", strError);
                        return false;
                    }
                }
                else
                {
                    clickY = rect.Value.Y + (int)(rect.Value.Height * 0.5);
                }

                // 5，使用鼠标事件，先移动到位置，然后进行单击
                MarsWindowsAPIs.SetCursorPos(clickX, clickY);
                System.Threading.Thread.Sleep(500);
                /// 6，参数中是否包括strParameter, no_clear，如果有，并且值为true,则不清除屏幕，否则清除屏幕（发送{del 20}{backspace 20}）
                /// 
                if ((!string.IsNullOrEmpty(strParaMeter)) && (strParaMeter.IndexOf("no_clear", StringComparison.OrdinalIgnoreCase) < 0))
                {
                    System.Windows.Forms.SendKeys.SendWait("{DEL 20}{BACKSPACE 20}");
                    System.Threading.Thread.Sleep(500);
                }
                /// 7，click
                /// 
                MarsWindowsAPIsExtend.LeftMouseClick(clickX, clickY);
                dealResult.ResultMessage = "SUCCESS";
                dealResult.ActualInputData = strData;
                dealResult.AckTime = DateTime.Now;
                Logger.Info("FillEdit", "DONE");
                return true;
            }
            catch (Exception ex)
            {
                strError = ex.Message;
                dealResult.AckTime = DateTime.Now;
                dealResult.ResultMessage = $"FAILED, {strError}";
                Logger.Error("FillEdit", strError, ex);
                return false;
            }
            finally
            {
                Logger.logEnd("FillEdit", $"{iMark}");
            }
        }
        
    }
}
