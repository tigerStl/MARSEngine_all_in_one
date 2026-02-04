using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace MarsTestFrame.systemUtil
{
    public class TigerMarsUtil
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TigerMarsUtil));
        public static string GetPathWithoutFileName(string strFileWithPath)
        {
            Logger.logBegin("GetPathWithoutFileName");
            try
            {
                if (strFileWithPath == null) return null;

                int iLastPos = strFileWithPath.LastIndexOf("\\");
                if (iLastPos == -1)
                {
                    return null;
                }

                return strFileWithPath.Substring(0, iLastPos);

            }
            finally
            {
                Logger.logEnd("GetPathWithoutFileName");

            }
        }

        public static string GetParameter(string strParaName, string strValue)
        {
            return string.Format(" ,[{0}={1}] ", strParaName, strValue);
        }

        public static string GetParameter(string[] arrParaName, string[] strValues)
        {
            string strFormat = "";
            int iMaxLen = arrParaName == null ? -1 : arrParaName.Length;
            iMaxLen = Math.Max(iMaxLen, strValues == null ? -1 : strValues.Length);
            for (int i = 0; i < iMaxLen; i++)
            {
                strFormat = string.Format("{0},[{1}={2}]", strFormat, arrParaName[i], strValues[i]);
            }
            return strFormat;
        }

        public static bool RegularTest(string strPartern, string strValue)
        {
            if (strValue == null) return false;
            RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace;
            return Regex.IsMatch(strValue, strPartern, options);
        }

        #region system keyword

        #region system constant
        internal const string CNST_NOWAIT = "nowait";
        internal const string CNST_WAIT = "wait";
        #endregion



        /// <summary>
        /// Called by QTP script.
        /// The format should be ExecuteCommand [null] Wait|NoWait "c:\temp\summitFT.cmd" parameter1 parameter2 ......
        /// </summary>
        /// <param name="strIdentifier"></param>
        /// <param name="strDataToRun"></param>
        /// <param name="strRC"></param>
        /// <param name="strDataRC"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        /// 

        public static bool ExecuteCommand(string strIdentifier, string strDataToRun, string strRC, string strDataRC, ref string strError)
        {

            Logger.Info("ExecuteCommand", string.Format("Identifier:[{0}], DataToRun:[{1}] RC:[{2}] DataRC:[{3}]", strIdentifier, strDataToRun, strRC, strDataRC));

            try
            {
                string strCmdWithPath, strParameters;
                #region get command path and command
                string strTmpCmd = strDataToRun.Trim();
                int iPos = strTmpCmd.IndexOf("\"", 1);
                if (iPos <= 0)
                {
                    Logger.Error("ExecuteCommand", strError = string.Format("Command format is not right, it should start with \""));
                    return false;
                }
                strParameters = strTmpCmd.Substring(iPos + 1);
                strCmdWithPath = strTmpCmd.Substring(0, iPos);
                strCmdWithPath = strCmdWithPath.Replace("\"", "");


                if ((!strCmdWithPath.ToLower().StartsWith("http")) && (!File.Exists(strCmdWithPath)))
                {
                    Logger.Error("ExecuteCommand", strError = string.Format("No such file exists:[{0}]", strCmdWithPath));
                    return false;
                }
                #endregion

                ProcessStartInfo objPInfo = new ProcessStartInfo();
                objPInfo.FileName = strCmdWithPath;
                objPInfo.Arguments = strParameters;
                Process p = Process.Start(objPInfo);


                if (string.Compare(CNST_NOWAIT, strRC, true) == 0)
                {
                    p.WaitForExit(2000);
                    return true;
                }
                p.WaitForInputIdle();
                p.WaitForExit();

                return true;
            }
            catch (Exception e)
            {
                Logger.Error("ExecuteCommand", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
        }
        #endregion 
    }
}
