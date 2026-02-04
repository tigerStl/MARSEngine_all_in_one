using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARSWebDriver
{
    public class MarsWebObjectIdentification
    {
        public string QUICK_ACCESS;
        public string PEG_QUICK_ACCESS;
    }

    

    public delegate bool MarsWebKeywordOperation(long lStepId, string strParaMeter, string strData,
        //string strAttchInfo,
        MarsWebObjectIdentification stepObject,
        ref string strError,
        ref MARSDealResult dealResult);
    public class MarsWebDriverEntry
    {
        public Dictionary<string, MarsWebKeywordOperation> MARS_WEB_KEYWORD_OPS=new Dictionary<string, MarsWebKeywordOperation>()
        {
            { "CLICKBUTTON"              , MARSKEYWORD_ClickButton},
        }
        static void StartWebAutomation()
        {
            using (IWebDriver driver = new FirefoxDriver())
            {
                driver.Navigate()
            }
        }

        private static bool MARSKEYWORD_ClickButton(long lStepId, string strParaMeter, string strData,
        //string strAttchInfo,
        MarsWebObjectIdentification stepObject,
        ref string strError,
        ref MARSDealResult dealResult){
            return false;
        }
    }
}
