#if ExcelSupport

using Mars.message.Utility;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.IO;
using Excel = Microsoft.Office.Interop.Excel;

namespace TestFlowClient.Mars.Office.Support
{
    public class MarsExcel
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsExcel));
        /// <summary>
        /// 'keyword format
        ///   ' keyword : CopyExcelRangeToClipboard
        ///' Object  : null
        ///' RC      : null
        ///' Data    : [filePath];[RangeInfo]  RangeInfo sample: sheet1:a2:b100
        /// </summary>
        /// <param name="strIdentifier"></param>
        /// <param name="strDataRC"></param>
        /// <param name="strRC"></param>
        /// <param name="strData"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        internal static bool CopyExcelRange2Clipboard(string strPath, string strRanges, ref string strError)
        {
            Logger.Info("CopyExcelRange2Clipboard", string.Format("strPath:[{0}] strRanges:[{1}] ]", strPath, strRanges));
            Excel.Application objExcelApp = null;
            try
            {
                objExcelApp = ExcelUtil.GetExcelApplication();
                objExcelApp.Visible = false;
                if (!File.Exists(strPath))
                {
                    Logger.Error("CopyExcelRange2Clipboard", strError = string.Format("No such File exists:[{0}]", strPath));
                    return false;
                }
                //internal static bool CopyRangeValueToClipBoard(string strFilePath,string strRanges, ref string strError, Microsoft.Office.Interop.Excel.Application objExcelApp=null)
                bool isRight = ExcelUtil.CopyRangeValueToClipBoard(strPath, strRanges, ref strError, objExcelApp);
                if (!isRight)
                {
                    Logger.Error("CopyExcelRange2Clipboard", strError = string.Format("Error returns from ExcelUtil.CopyRangeValueToClipBoard：[{0}]"));
                    return false;
                }
                Logger.Info("CopyExcelRange2ClipBoard", string.Format("Copied to Clipboard! [{0}]", strRanges));
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("CopyExcelRange2Clipboard", strError = string.Format("Exceptions:[{0}]", e.Message), e);
                return false;
            }
            finally
            {
                if (objExcelApp != null)
                {
                    ExcelUtil.CloseExcelApp(objExcelApp);
                }
            }


        }
    }
}

#endif
