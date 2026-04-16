
using log4net.Repository.Hierarchy;
using Mars.AutoTestingDriver.webSupport;
//using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.WebHelpers
{
    public enum MARSStep_WebConnectionMode
    {
        _noSet = 0x00, 
        _bySelenium, 
        _byMarsHostAgent
    }

    public class MarsSeleniumWarp
    {
        public int remote_debugger_port { get; set; }

        private static MarsSeleniumWarp _instance = null;
        public static MarsSeleniumWarp currentInstance => _instance;
        public static MarsSeleniumWarp GetInstance(string strMode)
        {
            if (string.IsNullOrEmpty(strMode)) return _instance = null;
            if (strMode.StartsWith(MARSKeywordWebHelpers.cnst_mode_bySelenium))
            {
                int iPort = -1;
                if (MARSKeywordWebHelpers.cnst_mode_bySelenium.Length >= strMode.Length)
                {
                    //iPort = 9222;
                    _instance = MARSKeywordWebHelpers.currentMarsSeleniumWarp;// new() { remote_debugger_port = iPort };
                    return _instance;
                }
                else
                {
                    var tmpPort = strMode.Substring(MARSKeywordWebHelpers.cnst_mode_bySelenium.Length + 1);

                    if (int.TryParse(tmpPort, out iPort))
                    {
                        _instance = new() { remote_debugger_port = iPort };
                        return _instance;
                    }
                    else
                    {
                        return _instance = null;
                    }
                }
            }
            return null;
        }
    }

    /// <summary>
    /// 当使用混合模式时候，需要判断当前使用selenium还是marsWebHost
    /// </summary>
    public class MARSKeywordWebHelpers
    {
        public const string cnst_mode_bySelenium = "bySelenium";
        public const string cnst_mode_byMARSAgent = "byMARSAgent";

        public static MARSStep_WebConnectionMode currentWebStepMode { get; set; }
        public static MarsSeleniumWarp currentMarsSeleniumWarp { get; set; } = new () { remote_debugger_port = 9222 };

        public static void SetCurrentWebStepMode(string modeInfofromStep)
        {
            currentWebStepMode = MARSStep_WebConnectionMode._noSet;
            if (string.IsNullOrEmpty(modeInfofromStep))
            {
                return;
            }
            var inst = MarsSeleniumWarp.GetInstance(modeInfofromStep);
            if (inst == null)
            {
                //判断是否是byMarsAgent
                if (cnst_mode_byMARSAgent.Equals(modeInfofromStep??"", StringComparison.OrdinalIgnoreCase))
                {
                    currentWebStepMode = MARSStep_WebConnectionMode._byMarsHostAgent;
                    return;
                }
            }
            else
            {
                currentWebStepMode = MARSStep_WebConnectionMode._bySelenium;
                currentMarsSeleniumWarp = inst;
            }
        }
        /// <summary>
        /// 使用webdriver，链接到webview或者browser， 在dot net中，通常使用chrome
        /// </summary>
        /// <param name="dictPegProperties"></param>
        /// <param name="dictObjProperties"></param>
        /// <param name="currentWebStepMode"></param>
        /// <param name="remote_debugger_port"></param>
        /// <param name="strError"></param>
        /// <param name="strAdv"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal static bool ConnectToWebView(Dictionary<string, string> dictPegProperties, 
            Dictionary<string, string> dictObjProperties, MARSStep_WebConnectionMode currentWebStepMode, int remote_debugger_port,
            ref string strError, ref string strAdv, string strBaseURL = "localhost")
        {
            
            if (currentWebStepMode != MARSStep_WebConnectionMode._bySelenium)
            {
                ///
                strAdv = $"Please use {MARSKeywordWebHelpers.cnst_mode_bySelenium} instead of others.";
                strError = $"Only bySelenium is supported on this MARS Engine version.|{strAdv}";

                return false;
            }
            return MARSWebDriver.AttachToWebViewAndCreateDriverChrome(remote_debugger_port, dictObjProperties, ref strError, strBaseURL);
            
        }
    }
}
