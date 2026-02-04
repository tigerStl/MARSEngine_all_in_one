using OpenQA.Selenium;
using OpenQA.Selenium.Chromium;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.webSupport
{
    public class MarsButtonHelper
    {
        private static MLogger logger = MLogger.GetLogger(typeof(MarsButtonHelper));
        public const string cnst_IgnoreCondition = "IgnoreCondition";
        
        
        /// <summary>
        /// ignoreCondition should be always the last parameter
        /// </summary>
        /// <param name="strPara"></param>
        /// <returns></returns>
        public static bool IsCondintionClick(string strPara, ref string ignoreCondtion, ref string strError ,ref string strAdv, ref string strStack)
        {
            if (string.IsNullOrEmpty(strPara)) return false;
            int idx = -1;
            if ((idx =strPara.IndexOf(cnst_IgnoreCondition, StringComparison.OrdinalIgnoreCase))<0) return false;
            try
            {
                ignoreCondtion = strPara.Substring(idx + cnst_IgnoreCondition.Length + 1);
                return true;
            }
            catch (Exception e)
            {
                strStack = e.StackTrace;
                strAdv = "Please make sure the parameter matches the format.";
                strError = $"parameter for ignore conditon clicking should be like: IgnoreCondition:[some xpath to check]. |{strAdv}";
                logger.Error("IsCondintionClick", $"{e.Message}|\r\n{strError}", e);
                return false;
            }
        }

        internal static bool IsToIgnore(IWebElement targetElement, string strIgnoreConditon, ChromiumDriver chromDriv, ref string strError, ref string strAdv, ref string strStack)
        {
            logger.logBegin("IsToIgnore", $"if |{strIgnoreConditon}| matches then no click");
            try
            {
                bool hasClass = targetElement.FindElements(By.XPath($"./self::{strIgnoreConditon}")).Count > 0;
                if (hasClass) {
                    return true;
                }
                return false;
            }
            catch(Exception e)
            {
                logger.Error("IsToIgnore", strError = e.Message, strStack = e.StackTrace);
                return false;
            }

        }
    }
}
