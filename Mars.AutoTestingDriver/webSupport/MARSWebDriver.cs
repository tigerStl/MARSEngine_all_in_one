#if !_mars_msg_center
extern alias clientWCF;

using clientWCF::Route2NSEx.src.Marquis.systemUtil;
using Mars.AutoTestingDriver.ErrorMessage;

//using Mars.AutoTestingDriver.interProcess;
using MarsEnginer.windowsWrapper.SystemUtil;

using Mars.AutoTestingDriver.ExecuteStoryboard;
using Mars.message.AutoTestingDriver.interProcess;

using Mars.message.Business;
using Mars.AutoTestingDriver.ExecuteTestcase.keywordOp.baseInterfaceAndClass;
using OpenQA.Selenium.Support.Extensions;
#else
///_mars_msg_center ÊÇ×¨ÃÅÓÃÓÚ.net coreµÄMARSCoreMessageCenterµÄÃüÃû¿Õ¼ä
///·ÇÆäËû³ÌÐò²»¿ÉÓÃ
using Route2NSEx.src.Marquis.systemUtil;
using Mars.message.windowsWrapper.SystemUtil;
using MARSCoreMessageCenter.basicData;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Windows.Forms;
using OpenQA.Selenium;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Edge;
using System.Text.RegularExpressions;
using Mars.AutoTestingDriver.WebHelpers;
using Mars.webSupport;
using Keys = OpenQA.Selenium.Keys;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Chromium;

using System.ServiceModel;

using System.Collections.ObjectModel;
using System.Xml.Linq;
using Mars.Inter.MQCenter.objectEngine;

namespace Mars.AutoTestingDriver.webSupport
{

    public static class MarsTestXpathDialog
    {
        public const string cnst_test_dialog = "TEST_XPAPATH";
        public static string CheckingTestDialog(string strParaMeter, ref bool isTestDialog)
        {
            strParaMeter = strParaMeter ?? "";
            isTestDialog = false;
            int iH = strParaMeter.IndexOf(cnst_test_dialog);
            if (iH >= 0)
            {
                isTestDialog = true;
                strParaMeter = strParaMeter.Replace(cnst_test_dialog, "");
                if (strParaMeter.StartsWith(";"))
                {
                    strParaMeter = strParaMeter.Substring(1);
                }
            }

            return strParaMeter;
        }

        public static void ShowTestDialog() { 
            throw new NotImplementedException();
        }
    }

    public class MarsSeleniumWindowsInfo
    {
        public string windowsHandle { get; set; }
        public string url { get; set; }
        public string title { get; set; }
        public override string ToString()
        {
            return $"{windowsHandle}---{url}";
        }
    }


    public static class MarsWEBHighlighter
    {
        public const string cnst_highlight_object = "HIGHLIGHT";
        public const string cnst_test_xpath_dialog = "XPATH_TEST_DIALOG";

        public static void HighlightElement(IWebDriver driver, IWebElement element, string color = "red", int thickness = 3)
        {
            var jsExecutor = (IJavaScriptExecutor)driver;
            jsExecutor.ExecuteScript($"arguments[0].style.border='{thickness}px solid {color}'", element);
        }

        public static void RemoveHighlight(IWebDriver driver, IWebElement element)
        {
            var jsExecutor = (IJavaScriptExecutor)driver;
            jsExecutor.ExecuteScript("arguments[0].style.border=''", element);
        }

        public static void HighlightElement3Times(IWebDriver driver, IWebElement element, string color="red", int thickness = 3)
        {
            // ´´½¨ IJavaScriptExecutor ÊµÀý
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

            // ¶¨Òå JavaScript ´úÂë
            string script = @"
                var element = arguments[0];                
                var originalBorder = element.style.border;
                var count = 0;
                function Marsflash() {
                    if (count < 6) { // ÉÁË¸ 3 ´Î£¨Ã¿´ÎÉÁË¸°üÀ¨ºìÉ«ºÍ»Ö¸´£©
                        if (element.style.border === '2px solid red') {
                            element.style.border = originalBorder; // »Ö¸´Ô­Ê¼ÑùÊ½
                        } else {
                            element.style.border = '2px solid red'; // ÉèÖÃÎªºìÉ«±ß¿ò
                        }
                        count++;
                        setTimeout(Marsflash, 200); // Ã¿ 200 ºÁÃëÉÁË¸Ò»´Î
                    } else {
                        element.style.border = originalBorder; // ×îÖÕ»Ö¸´Ô­Ê¼ÑùÊ½
                    }
                }

                Marsflash(); // ¿ªÊ¼ÉÁË¸
            ";

            // Ö´ÐÐ JavaScript ´úÂë
            js.ExecuteScript(script, element);

            // µÈ´ýÉÁË¸Íê³É
            Thread.Sleep(2000);
        }

        public static string CheckHightLightSettings(string strPara, ref bool isOk)
        {
            strPara = strPara ?? "";
            isOk = false;
            int iH = strPara.IndexOf(cnst_highlight_object);
            if (iH >= 0)
            {
                isOk = true;
                strPara = strPara.Replace(cnst_highlight_object, "");
                if (strPara.StartsWith(";"))
                {
                    strPara =strPara.Substring(1);
                }
            }
            
            return strPara;
        }


    }

    class EdgeExtension
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Publisher { get; set; }
        public string Enabled { get; set; }

        public EdgeExtension(string extensionPath)
        {
            // Get the extension name.
            string extensionName = Path.GetFileNameWithoutExtension(extensionPath);
            Name = extensionName;

            // Get the extension version.
            string extensionVersion = FileVersionInfo.GetVersionInfo(extensionPath).FileVersion;
            Version = extensionVersion;

            // Get the extension description.            
            string extensionDescription = File.ReadAllText(Path.Combine(extensionPath, ".json"));
            dynamic extensionJson = JsonConvert.DeserializeObject(extensionDescription);
            string description = extensionJson.GetMember("description");

            Description = extensionDescription;

            // Get the extension publisher.
            string extensionPublisher = //File.ReadAllText(extensionPath + ".json")["manifest"]["version"]["developer"]["name"];
                extensionJson.GetMember("manifest")
                    .GetMember("version")
                    .GetMember("developer")
                    .GetMember("name");
            Publisher = extensionPublisher;

            // Get the extension enabled status.
            bool extensionEnabled = File.Exists(extensionPath + ".enable");
            Enabled = extensionEnabled ? "true" : "false";
        }

        //public static List<EdgeExtension> GetAllExtension()
    }


    public class MarsWebEdgeExtension
    {
        private const string cnst_extension_prefix = "EXTENSION";

        public string[] extensions { get; set; }

        public static MarsWebEdgeExtension isParaRequireExtension(string strPara, ref bool isOk)
        {
            isOk = false;
            if (string.IsNullOrEmpty(strPara)) return null;
            //Regex rx = new Regex($"{cnst_extension_prefix}:(\\S+\\s+\\S+;)+");
            Regex rx = new Regex($"{cnst_extension_prefix}:.*;");
            MatchCollection m = rx.Matches(strPara);
            isOk = m.Count > 0;
            if (!isOk) return null;
            string[] exts = new string[m.Count];
            for (int i= 0 ;i< m.Count;i++)
            {
                if (i == 0)
                {
                    exts[i] = m[i].Value.Substring(cnst_extension_prefix.Length + 1);
                    // remove the last ';'
                    exts[i]= exts[i].Remove(exts[i].Length - 1);     
                }
            }
            MarsWebEdgeExtension rslt = new MarsWebEdgeExtension();
            rslt.extensions = exts;
            isOk = true;
            return rslt;

        }       

    }


    public class MarsThreadForDialogClose
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsThreadForDialogClose));

        private static Thread _thread;
        private const string cnst_Partern = "MARS_ThreadSend:";
        private static string marsThreadToPress = "";

        public static bool IsThreadToCloseDialog(string strPara)
        {
            marsThreadToPress = null; 
            if (string.IsNullOrEmpty(strPara)) return false;
            System.Text.RegularExpressions.Regex rg = new System.Text.RegularExpressions.Regex(cnst_Partern);
            
            System.Text.RegularExpressions.Match m = rg.Match(strPara);
            if (!m.Success) return false;
            
            int idx = strPara.IndexOf(cnst_Partern);
            if (idx < 0) return false;

            string strSub = strPara.Substring(idx+cnst_Partern.Length);
            idx = strSub.IndexOf(";");
            
            if (idx >= 0)
            {
                strSub = strSub.Substring(0, idx);
            }
            /// strSub ÊÇÐèÒªthread °´µÄkeyÐòÁÐ
            /// 
            marsThreadToPress = strSub;
            return true;
        }

        private static void PressKeys()
        {
            Logger.logBegin("PressKeys", "Thread begins");
            try
            {
                if (string.IsNullOrEmpty(marsThreadToPress)) return;

                /// »ñµÃtopµÄwindow
                /// 
                //Thread.Sleep(10000);
                MarsWindowsAPIsExtend.WaitForCurrentProcessResponse(5);
                IntPtr topWndHndl = Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.GetTopWindow(IntPtr.Zero);
                Logger.Info("PressKeys", $"{topWndHndl}");
#if _demo_for_14
                if (MarsKeywordBase.IsInDateTimeX())
                {
                    topWndHndl = IntPtr.Zero;
                }
#endif
                Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIs.SetFocus(topWndHndl);                
                System.Windows.Forms.SendKeys.SendWait(marsThreadToPress);
            }
            finally
            {
                Logger.logEnd("PressKeys");
            }
        }

        public static bool StartThread()
        {
            _thread = new Thread(new ThreadStart(PressKeys));
            _thread.Start();
            return true;
        }
    }

    public sealed class MARSWebDriver
    {
        public const string CNST_WEB_IE = "MS_IE";
        public const string CNST_WEB_EDGE = "MS_EDGE";
        public const string CNST_WEB_CHROME = "MS_CHROME";

        public const string cnst_clickAt_para_pos_center = "Center";

        private static string homeWindowsHandle = null;

        class RootHostOffset
        {
            internal int miSwitchedLevel;

        }       

        private static string FindChromeDriver(ref bool isOk, ref string strError,string strVersion = null)
        {
            var pth = typeof(MARSWebDriver).Assembly.Location;
            pth = System.IO.Path.GetDirectoryName(pth);
            pth = System.IO.Path.Combine(pth, $"WebDriver\\chrome{strVersion}\\");
            var chromeDriverName = System.IO.Path.Combine(pth, "chromedriver.exe");
            if (!System.IO.File.Exists(chromeDriverName))
            {
                strError = $"no such file exists|{chromeDriverName}";
                isOk = false;
                return null;
            }
            isOk = true;
            return pth;
        }

        //public static string GetElementXPath(IWebElement element)
        //{
        //    var jsExecutor = (IJavaScriptExecutor)_driver;
        //    return (string)jsExecutor.ExecuteScript(
        //        "function getElementXPath(element) {" +
        //        "if (element.id !== '') {" +
        //        "return 'id(\"' + element.id + '\")';" +
        //        "}" +
        //        "if (element === document.body) {" +
        //        "return element.tagName.toLowerCase();" +
        //        "}" +
        //        "var ix = 0;" +
        //        "var siblings = element.parentNode.childNodes;" +
        //        "for (var i = 0; i < siblings.length; i++) {" +
        //        "var sibling = siblings[i];" +
        //        "if (sibling === element) {" +
        //        "return getElementXPath(element.parentNode) + '/' + element.tagName.toLowerCase() + '[' + (ix + 1) + ']';" +
        //        "}" +
        //        "if (sibling.nodeType === 1 && sibling.tagName === element.tagName) {" +
        //        "ix++;" +
        //        "}" +
        //        "}" +
        //        "}" +
        //        "return getElementXPath(arguments[0]);", element);
        //}
        private static bool killChromeDrivers()
        {
            var arrp = Process.GetProcessesByName("chromedriver");
            logger.Info("killChromeDrivers", $"find drivers in memory|{arrp?.Length}|");
            for (int i = 0; i < arrp.Length; i++)
            {
                try
                {
                    if (!arrp[i].HasExited)
                        arrp[i].Kill();
                }
                catch (Exception e)
                {
                }
            }
            return true;
        }

        private static bool WaitChromeDriverConnected(int maxWaitTimeInSeconds=120)
        {   
            const int checkIntervalInMilliseconds = 500; // ¼ì²é¼ä¸ôÊ±¼ä
            int elapsedTime = 0;

            logger.logBegin("WaitChromeDriverConnected", "Waiting for ChromeDriver to connect...");

            try
            {
                while (elapsedTime < maxWaitTimeInSeconds * 1000)
                {
                    try
                    {
                        // ¼ì²é ChromeDriver ÊÇ·ñÒÑÁ¬½Ó
                        if (g_WebDriver != null && g_WebDriver.marsChromeDriver != null)
                        {
                            var sessionId = g_WebDriver.marsChromeDriver.SessionId;
                            if (sessionId != null)
                            {
                                logger.Info("WaitChromeDriverConnected", $"ChromeDriver connected with SessionId: {sessionId}");
                                return true; // Á¬½Ó³É¹¦£¬ÍË³ö·½·¨
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // ²¶»ñÒì³£µ«²»ÖÕÖ¹Ñ­»·£¬¼ÌÐøµÈ´ý
                        logger.Warnning("WaitChromeDriverConnected", $"Connection check failed: {ex.Message}");
                    }

                    // µÈ´ýÒ»¶ÎÊ±¼äºóÔÙ´Î¼ì²é
                    Thread.Sleep(checkIntervalInMilliseconds);
                    elapsedTime += checkIntervalInMilliseconds;
                }

                // ³¬Ê±´¦Àí
                logger.Error("WaitChromeDriverConnected", "Timeout: ChromeDriver did not connect within 120 seconds.");
                return false ;
            }
            finally
            {
                logger.logEnd("WaitChromeDriverConnected", "Finished waiting for ChromeDriver connection.");
            }
        }


        public static bool AttachToWebViewAndCreateDriverChrome(int remoteDebuggerPort, Dictionary<string, string> dictObjProperties, ref string strError, 
            string strBaseUrl="localhost")
        {
            logger.logBegin("AttachToWebViewAndCreateDriverChrome", $"{strBaseUrl}:{remoteDebuggerPort}");
            try
            {
                bool isOk = false;
                string strChromeDriver = FindChromeDriver(ref isOk ,ref strError);
                if (!isOk)
                {
                    return false;
                }
                
                // Ê×ÏÈ£¬kill chromedriver.exe
                killChromeDrivers();
                // 
                var options = new ChromeOptions();
                //var service = ChromeDriverService.CreateDefaultService(strChromeDriver);
                //service.Start();
                
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");                
                options.DebuggerAddress = $"{strBaseUrl}:{remoteDebuggerPort}";
                //chromDriver = new ChromeDriver(options);
                g_WebDriver = new MARSWebDriver(webId: MARSWebDriver.CNST_WEB_CHROME, isWebView:true);
                int iTimeOut = 120;
                logger.Info("AttachToWebViewAndCreateDriverChrome", $"set chromedriver's directory|{strChromeDriver}");
                g_WebDriver.marsChromeDriver = new ChromeDriver(strChromeDriver, options, TimeSpan.FromSeconds(iTimeOut));
                //g_WebDriver.marsChromeDriver = new ChromeDriver(service, options, TimeSpan.FromSeconds(120));
                logger.Info("AttachToWebViewAndCreateDriverChrome", "after call new ChromeDriver");
                var isConnected = WaitChromeDriverConnected(iTimeOut);
                if (!isConnected)
                {
                    strError = $"ChromeDriver did not connect within {iTimeOut} seconds.";
                    logger.Error("AttachToWebViewAndCreateDriverChrome", strError);
                    //return false;
                }
                //Thread.Sleep(1000);
                if (dictObjProperties != null)
                {
                    if (dictObjProperties.Keys.Contains(CNST_WebURL))
                    {
                        string strTargetMainWindowUrl = dictObjProperties[CNST_WebURL];
                        if (string.IsNullOrEmpty(strTargetMainWindowUrl))
                        {
                            logger.Error("AttachToWebViewAndCreateDriverChrome", strError =$"{CNST_WebURL}| is empty, please set if more than one windows open." );
                            return false;
                        }
                        string allUrls = "";
                        int idx = 1;
                        foreach (var hdl in g_WebDriver.marsChromeDriver.WindowHandles){
                            if (hdl ==null ) continue;
                            var drv = g_WebDriver.marsChromeDriver.SwitchTo().Window(hdl);
                            string tmpDrvUrl = drv.Url;
                            allUrls = $"{idx}--{tmpDrvUrl}\r\n";
                            if (string.IsNullOrEmpty(tmpDrvUrl)) continue;
                            if ((tmpDrvUrl.Equals(strTargetMainWindowUrl, StringComparison.OrdinalIgnoreCase))||(MarsWindowsAPIsExtend.RegularTest(strTargetMainWindowUrl, tmpDrvUrl)))
                            {
                                g_WebDriver.marsChromeDriverCurrentWindowHandle = hdl;
                                return true;
                            }
                        }
                        logger.Error("AttachToWebViewAndCreateDriverChrome", strError = $"can't find window with URL identifier infor|{strTargetMainWindowUrl}. Please ensure the URL settings. \r\n\tall current URLs are:\r\n\t{allUrls}");
                        return false;
                    }
                }
                return true;
            }catch(Exception e)
            {
                strError = e.Message;
                logger.Error("AttachToWebViewAndCreateDriverChrome", strError, e);
                if (g_WebDriver != null)
                {
                    try
                    {
                        g_WebDriver.marsChromeDriver.Close();
                    }
                    catch { }
                    
                }
                g_WebDriver = null;
                return false;
            }
            finally
            {
                logger.logEnd("AttachToWebViewAndCreateDriverChrome");
            }
        }

        internal static bool ConnectToWebByURL(string url, ref string strError)
        {
            logger.logBegin("AttachToWebViewAndCreateDriverChrome", url);
            try
            {
                bool isOk = false;
                string strChromeDriver = FindChromeDriver(ref isOk, ref strError);
                if (!isOk)
                {
                    return false;
                }
                var options = new ChromeOptions();

                options.DebuggerAddress = url;
                //chromDriver = new ChromeDriver(options);
                g_WebDriver = new MARSWebDriver(webId: MARSWebDriver.CNST_WEB_CHROME, isWebView: true);
                logger.Info("AttachToWebViewAndCreateDriverChrome", $"set chromedriver's directory|{strChromeDriver}");
                g_WebDriver.marsChromeDriver = new ChromeDriver(strChromeDriver, options);
                Thread.Sleep(1000);                
                return true;
            }
            catch (Exception e)
            {
                strError = e.Message;
                logger.Error("AttachToWebViewAndCreateDriverChrome", strError, e);
                if (g_WebDriver != null)
                {
                    try
                    {
                        g_WebDriver.marsChromeDriver.Close();
                    }
                    catch { }

                }
                g_WebDriver = null;
                return false;
            }
            finally
            {
                logger.logEnd("AttachToWebViewAndCreateDriverChrome");
            }
        }

        public static List<MarsSeleniumWindowsInfo> GetWindowsInfo()
        {
            if ((g_WebDriver == null)||(g_WebDriver.marsChromeDriver==null))
            {
                MessageBox.Show("Not Init Selenium Engine.");
                return null;
            }
            List<MarsSeleniumWindowsInfo> lstRslt = new List<MarsSeleniumWindowsInfo>();
            foreach (var hdl in g_WebDriver.marsChromeDriver.WindowHandles)
            {
                if (hdl == null) continue;
                MarsSeleniumWindowsInfo wnd = new MarsSeleniumWindowsInfo();                
                var drv = g_WebDriver.marsChromeDriver.SwitchTo().Window(hdl);
                wnd.url = drv.Url;
                wnd.title = drv.Title;
                wnd.windowsHandle = hdl;
                lstRslt.Add(wnd);
            }
            return lstRslt;
        }

        internal static string getWindowHTML(string windowsHandle, string url)
        {
            if ((g_WebDriver == null) || (g_WebDriver.marsChromeDriver == null))
            {
                MessageBox.Show("Not Init Selenium Engine.");
                return null;
            }
            var drv = g_WebDriver.marsChromeDriver.SwitchTo().Window(windowsHandle);
            return drv.PageSource;
        }

        internal static string GetEdgeDriverPath()
        {
            string pth = Assembly.GetExecutingAssembly().Location;
            string pthDirs = System.IO.Path.GetDirectoryName(pth);

            return System.IO.Path.Combine(pthDirs, @"..\webDrivers\edgedriver");
        }

        internal enum mars_web_obj_type
        {
            mars_ie = 0x0,
            mars_chrome,
            mars_firefox
        }

        private const string CNST_SUB_STR = "SUB_STR";
        private const string CNST_LAST_COLON = "LAST_COLON";

        internal class MarsWebMemoryObject
        {
            public const string CNST_PARA_TOMARSPARENTOBJ = "ToMarsParentObj";
            public const string CNST_PARA_FROMPARENTOBJ = "FromParentObj";

            internal string memoryObjectName;
            internal IWebElement targetObject;
            internal mars_web_obj_type memoryObjType;
            internal int sourceStatus; //0-save to, 1-get from
            internal static Dictionary<string, MarsWebMemoryObject> currentMemoryVariables = new Dictionary<string, MarsWebMemoryObject>();
            internal static List<MarsWebMemoryObject> PhraseParameters(string strPara)
            {
                if (string.IsNullOrEmpty(strPara)) return null;
                string[] arrParas = strPara.Split(';');
                List<MarsWebMemoryObject> lstRslt = new List<MarsWebMemoryObject>();

                foreach (var itm in arrParas)
                {
                    if (string.IsNullOrEmpty(itm)) continue;
                    string[] arrTmp = itm.Split(':');
                    if (arrTmp.Length != 2) continue;
                    if (string.Compare(arrTmp[0], CNST_PARA_TOMARSPARENTOBJ, true) == 0)
                    {
                        lstRslt.Add(new MarsWebMemoryObject()
                        {
                            memoryObjectName = arrTmp[1],
                            targetObject = null,
                            memoryObjType = mars_web_obj_type.mars_ie, //default
                            sourceStatus = 0
                        }
                        );
                        continue;
                    } else if (string.Compare(arrTmp[0], CNST_PARA_FROMPARENTOBJ, true) == 0)
                    {
                        lstRslt.Add(new MarsWebMemoryObject()
                        {
                            memoryObjectName = arrTmp[1],
                            targetObject = null,
                            memoryObjType = mars_web_obj_type.mars_ie, //default
                            sourceStatus = 1
                        }
                        );
                        continue;
                    }
                }
                return lstRslt;
            }

            internal static MarsWebMemoryObject GetFromObj(List<MarsWebMemoryObject> lstSrc)
            {
                if (lstSrc == null) return null;
                foreach (var itm in lstSrc)
                {
                    if (itm == null) continue;
                    if (itm.sourceStatus == 1) return itm;
                }
                return null;
            }


            internal static MarsWebMemoryObject GetToObj(List<MarsWebMemoryObject> lstSrc)
            {
                if (lstSrc == null) return null;
                foreach (var itm in lstSrc)
                {
                    if (itm == null) continue;
                    if (itm.sourceStatus == 0) return itm;
                }
                return null;
            }

            internal static IWebElement GetFromObjVariable(string memoryObjectName)
            {
                if (string.IsNullOrEmpty(memoryObjectName)) return null;
                if (currentMemoryVariables == null)
                {
                    logger.Error("GetFromObjVariable", "table is null");
                    return null;
                }
                if (!currentMemoryVariables.ContainsKey(memoryObjectName)) return null;
                if (currentMemoryVariables[memoryObjectName] == null) return null;
                if (currentMemoryVariables[memoryObjectName].targetObject == null) return null;
                return currentMemoryVariables[memoryObjectName].targetObject;
            }

            internal static void StoreObject2Memory(string momeryObjName, IWebElement source)
            {
                if (currentMemoryVariables.ContainsKey(momeryObjName))
                {
                    currentMemoryVariables[momeryObjName].targetObject = source;
                }
                else
                {
                    currentMemoryVariables.Add(momeryObjName, new MarsWebMemoryObject()
                    {
                        memoryObjectName = momeryObjName,
                        targetObject = source
                    });
                }
            }
        }

        private static MLogger logger = MLogger.GetLogger(typeof(MARSWebDriver));

        private const string CNST_TYP_TITLE = "webTitle";
        private const string CNST_WEBID = "webId";
        private const string CNST_WEBNAME = "webName";
        private const string CNST_WEBCLASS = "webClass";
        private const string CNST_XPATH = "webXPath";
        private const string CNST_VALUE = "WebValue";
        private const string CNST_TAG = "webTag";
        private const string CNST_INNERHTML = "webInnerHTML";
        private const string CNST_FRAME = "webFrame";
        private const string CNST_FRAME_BYNAME = "webFrameName";
        private const string CNST_CSSSELECTOR = "webCSSSelector";
        private const string CNST_WEBREPLACETEXT = "webReplaceText";// ¸ñÊ½£¬ÆäÖÐÓÃdataÌæ»»::?:: Ê¾ÀýÈçÏÂ //td[text()='::?::'])[1]
        private const string CNST_RESERVE_FRAME_ROOT = "MARS_ROOT";//ÓÃÀ´switch to root
        private const string CNST_WebURL = "WebUrl";
        public  const string cnst_NoClickIfNotFind = "IgnoreIfNotFind";

        public const string CNST_RESERVE_FLASH_TABLE = "flashTable";
        public const string CNST_RESERVE_ALL_TABLE = "ALLROWS";

        public const string CNST_PEG_SWITCH_TO_BROWSER = "SwitchToBrowser";
        public const string CNST_PEG_SWITCH_TO_HOMEBROWSER = "SwitchToHomeBrowser";

        public const string CNST_WEB_OBJECT_TYPE_AGGRID_COL = "WebAGGridColumn";
        public const string CNST_WEB_OBJECT_TYPE_AGGRID_ROW = "WebAGGridRow";

        

        public static List<string> WebObject_types_index = new() {
            CNST_TYP_TITLE,CNST_WEBID,CNST_WEBNAME,CNST_WEBCLASS,CNST_XPATH,CNST_VALUE,CNST_TAG,CNST_INNERHTML,CNST_FRAME,CNST_FRAME_BYNAME,CNST_CSSSELECTOR,CNST_WEBREPLACETEXT
        };

        private static MARSWebDriver g_WebDriver;        
        private IWebElement currentPeg;
        private IWebElement currentObj = null;
        public static MARSWebDriver GetInstance(string strPara=null)
        {
            if (g_WebDriver == null)
            {
                g_WebDriver = new MARSWebDriver(strPara);
            }
            return g_WebDriver;
        }

        //private InternetExplorerDriver marsWebDriver = null;

#if _EnableChrome
        private ChromiumDriver marsChromeDriver = null;
        private ChromiumDriver marsWebDriver = null;
        private string marsChromeDriverCurrentWindowHandle = null;
#else
        //private EdgeDriver marsWebDriver = null;
        private ChromiumDriver marsWebDriver    = null;
        private ChromiumDriver marsChromeDriver = null;
        private string marsChromeDriverCurrentWindowHandle = null;
#endif

        private MARSWebDriver(string strPara=null, string webId = CNST_WEB_EDGE, bool isWebView=false)
        {
            InitIEDriver(strPara, webId, isWebView);
        }

        private void killChromes()
        {
            var arrp = Process.GetProcessesByName("chrome");
            for (int i = 0; i < arrp.Length; i++)
            {
                try
                {
                    if (!arrp[i].HasExited)
                    arrp[i].Kill();
                }catch(Exception e)
                {

                }
            }
        }
        static string GetChromeUserDataFolderPath()
        {
            // Replace with the correct path based on the user's OS
            // Example for Windows:
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "User Data");
        }

        private void InitIEDriver(string strPara, string webId = CNST_WEB_EDGE, bool isWebView = false)
        {
            logger.logBegin("InitIEDriver", $"{strPara}|{webId}|{isWebView}");
            //½â¾öÊäÈëºÜÂýµÄÎÊÌâ
            if (!isWebView)
            {
                InternetExplorerOptions ieOptn = new InternetExplorerOptions();
                ieOptn.RequireWindowFocus = true;
                ieOptn.BrowserAttachTimeout = TimeSpan.FromSeconds(120);
            }
            MarsWebEdgeExtension webExtn = null;
            bool isOk = true;
            switch (webId)
            {                
                case CNST_WEB_CHROME:
                    ///ÐèÒªÔÚÆäËûµØ·½³õÊ¼»¯chromedriver
                    ///
                    if (!isWebView)
                    {
                        logger.Info("InitIEDriver", $"try to start services");
                        var service = ChromeDriverService.CreateDefaultService();
                        service.Start();

                        var option = new ChromeOptions();
                        option.AddArgument("--no-sandbox");
                        option.AddArgument("--disable-dev-shm-usage");

                        this.marsChromeDriver = new ChromeDriver(service, option, TimeSpan.FromSeconds(180));
                    }
                    return;
                case CNST_WEB_EDGE:
                case CNST_WEB_IE:
                    //marsWebDriver = new InternetExplorerDriver(ieOptn);
                    EdgeOptions edgeoptn = new EdgeOptions();
                    EdgeOptions options = new EdgeOptions();

#if _EnableChrome
                    ChromeOptions chromeOptions = new ChromeOptions();
                    if (!string.IsNullOrEmpty(strPara))
                    {
                        webExtn = MarsWebEdgeExtension.isParaRequireExtension(strPara, ref isOk);

                    }
#endif
                    if (webExtn != null) {

                        // add extension
                        // test
                        //string strBinanceW = @"C:\Users\gengf\AppData\Local\Microsoft\Edge\User Data\Default\Extensions\fhbohimaelbohpjbbldcngcnapndodjp.crx";
                        //options.AddExtensions(webExtn.extensions);
                        //options.AddExtensions(strBinanceW);
#if _EnableChrome
                        //if (string.Compare(CNST_WEB_IE, webId, true) == 0)
                        //{
                            killChromes();
                        //}

                        //chromeOptions.AddExcludedArgument("disable-default-apps");

                        //chromeOptions.AddArguments("--enable-extensions");
                        //chromeOptions.AddArguments($"--app-id=fhbohimaelbohpjbbldcngcnapndodjp");
                        chromeOptions.AddExcludedArguments("--disable-background-networking",
                            "--disable-backgrounding-occluded-windows",
                            "--disable-client-side-phishing-detection",
                            "disable-default-apps",
                            "--disable-hang-monitor",
                            "--disable-popup-blocking",
                            "--disable-prompt-on-repost",
                            "--disable-sync",
                            //"--enable-blink-features=ShadowDOMV0",
                            "--enable-logging",
                            "--log-level=0",
                            "--no-first-run",
                            "--no-service-autorun",
                            "--use-mock-keychain",
                            "--allow-pre-commit-input");
                        string userDir = GetChromeUserDataFolderPath();
                        chromeOptions.AddArguments($"user-data-dir={userDir}");
                        //chromeOptions.AddEncodedExtension
                        //chromeOptions.AddArgument("");
#endif
                        //options.AddArgument($"--app-id=fhbohimaelbohpjbbldcngcnapndodjp");
                    }
                    
                    //options.PageLoadStrategy = (PageLoadStrategy)EdgePageLoadStrategy.Eager;
                    System.Environment.SetEnvironmentVariable("webdriver.edge.driver", GetEdgeDriverPath());
                    //new DriverManager().SetUpDriver(new EdgeConfig());

#if _EnableChrome
                   
                    logger.Info("\t", ChromeDriver.LaunchAppCommand);
                    ChromeDriverService svc = ChromeDriverService.CreateDefaultService();
                    //this.marsChromeDriver = new ChromeDriver(svc,chromeOptions);
                    this.marsChromeDriver = new ChromeDriver(chromeOptions);
                    logger.Info("\t", $"{this.marsChromeDriver.WindowHandles.Count}");
                    homeWindowsHandle = this.marsChromeDriver.CurrentWindowHandle;
#else
                     marsWebDriver = new EdgeDriver(options);
#endif
                    return;
                default:
                    //marsWebDriver = new OpenQA.Selenium.Chrome.ChromeDriver();
                    
#if _EnableChrome
                    this.marsChromeDriver = new ChromeDriver();
                    
#else
                    marsWebDriver = new EdgeDriver();
#endif
                    return;
            }


        }

        public bool StartWebApplication(string strURL, string strPara, ref string strError, ref string strAdv, ref string strStack)
        {
            try
            {
                logger.logBegin("StartWebApplication", $"para:{strPara}, {strURL}");
                
#if _EnableChrome
                this.marsChromeDriver.Navigate().GoToUrl(strURL);
                logger.Info("StartWebApplication", $"get page source:\r\n{marsChromeDriver.PageSource}");
#else
                marsWebDriver.Navigate().GoToUrl(strURL);
                logger.Info("StartWebApplication", $"get page source:\r\n{marsWebDriver.PageSource}");
#endif

                return true;
            }
            catch (Exception e)
            {
                logger.Error("StartWebApplication", strError = e.Message, e);
                strAdv = "Contact Marquis";
                strStack = e.StackTrace;
                if (string.Compare("IGNORERROR", strPara ?? "", true) == 0)
                {
                    logger.Info("\t", "IGNORERROR detected, return true");
                    return true;
                }
                return false;
            }
            finally
            {
                logger.logEnd("StartWebApplication");
            }

        }



        internal bool WebPressKey(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties,
            string strParaMeter, string strData, string strObjType, string strAttachInfo, string pegName, string objName, ref string strError, 
            ref MARSDealResult dealResult, bool isMergeMode = false)
        {
            logger.logBegin("WebPressKey", string.Format("{0}, stepId:{1} (Peg:{2}, obj:{3}, para:{4}, data:{5}), type {6}, attach:[{7}] pegName:[{8}], objName:[{9}]",
                strKeyword, stepId,
                dictPegProperties == null ? "" : string.Join(";", dictPegProperties.Keys),
                dictObjProperties == null ? "" : string.Join(";", dictObjProperties.Keys),
                strParaMeter, strData, strObjType, strAttachInfo, pegName, objName));

            ChromiumDriver chromDriv = GetChromiumDriver();
            if (((!isMergeMode) && string.IsNullOrEmpty(chromDriv.Url)))
            {
                strError = "Not navigate a link yet.";
                return false;
            }

            bool isOk = false;
            if (dealResult == null)
                dealResult = new MARSDealResult();
            dealResult.ActualInputData = strData;
            dealResult.ReturnedData = strAttachInfo;
            bool isSwitchTo = false;
            string strRestoreWindow = "";

            try
            {
                if (string.Compare("DIRECTLY_INPUT", strParaMeter, true) == 0)
                {
                    System.Windows.Forms.SendKeys.SendWait(strData??"");
                    dealResult.ActualInputData = strData;
                    dealResult.AckTime = DateTime.Now;

                    dealResult.ResultMessage = "OK";
                    return true;
                }
                

                List<IWebElement> lstTargetElement = FindObject(dictPegProperties, dictObjProperties, strAttachInfo, strParaMeter, strData, strObjType, 
                    ref isOk, ref strError,
                    ref isSwitchTo, ref strRestoreWindow
                    );

                if ((!isOk) || (lstTargetElement == null) || (lstTargetElement.Count == 0))
                {
                    logger.Error("\t", strError = "no data is set");
                    dealResult.ResultMessage = "FAILED";
                    dealResult.AckTime = DateTime.Now;
                    dealResult.ErrorMessage = strError;
#if !_mars_msg_center
                    dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
#endif
                    dealResult.Advice = "Contact Marsquis";
                    dealResult.ReturnedData = strError;

                    return false;
                }
                var targetElement = lstTargetElement[0];

                Actions act = new Actions(chromDriv);                
                //targetElement.Click();
                act.MoveToElement(targetElement)
                    .Click()
                    .Perform();

                //if (string.IsNullOrEmpty(strData))
                //{
                //    logger.Error("\t", strError = "no data is set");
                //    dealResult.ResultMessage = "FAILED";
                //    dealResult.AckTime = DateTime.Now;
                //    dealResult.ErrorMessage = strError;
                //    dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
                //    dealResult.Advice = "Contact Marsquis";
                //    dealResult.ReturnedData = strError;
                //    return false;
                //}
                if (string.IsNullOrEmpty(strData))
                {
                    dealResult.ActualInputData = "";
                    dealResult.AckTime = DateTime.Now;

                    dealResult.ResultMessage = "OK";
                    return true;
                }

                Thread.Sleep(50);
                switch (strData.ToLower())
                {
                    case "key.home":
                        targetElement.SendKeys(Keys.Home);
                        break;
                    case "key.end":
                        targetElement.SendKeys(Keys.End);
                        break;
                    case "key.pagedown":
                    case "{PGDN}":
                        //targetElement.SendKeys(Keys.PageDown);
                        act.KeyDown(targetElement, Keys.PageDown)
                            .KeyUp(targetElement, Keys.PageDown)
                            .Perform();
                        break;
                    case "key.pageup":
                        targetElement.SendKeys(Keys.PageUp);
                        break;
                    case "{enter}":
                    case "key.enter":
                        act.KeyDown(targetElement, Keys.Enter)
                            .KeyUp(targetElement, Keys.Enter)
                            .Perform();
                        break;
                    case "{tab}":
                    case "key.tab":
                        targetElement.SendKeys(Keys.Tab);
                        break;
                    case "{down}":
                        targetElement.SendKeys(Keys.Down);
                        break;
                    case "{esc}":
                    case "key.esc":
                        act.KeyDown(targetElement, Keys.Escape)
                            .KeyUp(targetElement, Keys.Escape)
                            .Perform();
                        break;
                    case "{up}":
                        targetElement.SendKeys(Keys.Up);
                        break;
                    default:
                        targetElement.SendKeys(strData);
                        break;
                }

                dealResult.ActualInputData = strData;
                dealResult.AckTime = DateTime.Now;

                dealResult.ResultMessage = "OK";
                return true;
            }
            catch (Exception e)
            {
                strError = e.Message;
                dealResult.ResultMessage = "FAILED";
                dealResult.AckTime = DateTime.Now;
                dealResult.ErrorMessage = e.Message;
                dealResult.StackInfo = e.StackTrace;
                dealResult.Advice = "Contact Marsquis";
                dealResult.ReturnedData = e.Message;
                logger.Error("WebPressKey", e.Message, e);
                return false;
            }
            finally
            {
                if (isSwitchTo)
                {
                    try
                    {
#if _EnableChrome
                        this.marsChromeDriver.SwitchTo().Window(strRestoreWindow);
#else
                        this.marsWebDriver.SwitchTo().Window(strRestoreWindow);
#endif
                    }catch(Exception ee)
                    {
                        logger.Error("\t", ee.Message, ee);
                    }
                }

                logger.logEnd("WebPressKey");
            }
        }

        internal bool WebMaximizeWindow(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties,
            string strParaMeter, string strData, string strObjType, string strAttachInfo, string pegName, string objName, ref string strError, 
            ref MARSDealResult dealResult, bool isMergeMode = false)
        {
            logger.logBegin("WebMaximizeWindow", string.Format("{0}, stepId:{1} (Peg:{2}, obj:{3}, para:{4}, data:{5}), type {6}, attach:[{7}] pegName:[{8}], objName:[{9}]",
                strKeyword, stepId,
                dictPegProperties == null ? "" : string.Join(";", dictPegProperties.Keys),
                dictObjProperties == null ? "" : string.Join(";", dictObjProperties.Keys),
                strParaMeter, strData, strObjType, strAttachInfo, pegName, objName));
#if _EnableChrome
            if (this.marsChromeDriver == null || string.IsNullOrEmpty(marsChromeDriver.Url))
#else
            if (this.marsWebDriver == null || string.IsNullOrEmpty(marsWebDriver.Url))
#endif
            {
                strError = "Not navigate a link yet.";
                return false;
            }

            bool isOk = false;
            if (dealResult == null)
                dealResult = new MARSDealResult();
            dealResult.ActualInputData = strData;
            dealResult.ReturnedData = strAttachInfo;
            try
            {
#if _EnableChrome
                this.marsChromeDriver.Manage().Window.Maximize();
#else
                this.marsWebDriver.Manage().Window.Maximize();
#endif

                Thread.Sleep(50);
                dealResult.ActualInputData = strData;
                dealResult.AckTime = DateTime.Now;

                dealResult.ResultMessage = "OK";
                return true;
            }
            catch (Exception e)
            {
                strError = e.Message;
                dealResult.ResultMessage = "FAILED";
                dealResult.AckTime = DateTime.Now;
                dealResult.ErrorMessage = e.Message;
                dealResult.StackInfo = e.StackTrace;
                dealResult.Advice = "Contact Marsquis";
                dealResult.ReturnedData = e.Message;
                logger.Error("WebMaximizeWindow", e.Message, e);
                return false;
            }
            finally
            {
                logger.logEnd("WebMaximizeWindow");
            }
        }

        internal bool WebSetBox(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties,
            string strParaMeter, string strData, string strObjType, string strAttachInfo, string pegName, string objName, ref string strError, 
            ref MARSDealResult dealResult,
            bool isMergeMode = false)
        {
            logger.logBegin("WebSetBox", string.Format("{0}, stepId:{1} (Peg:{2}, obj:{3}, para:{4}, data:{5}), type {6}, attach:[{7}] pegName:[{8}], objName:[{9}]",
                strKeyword, stepId,
                dictPegProperties == null ? "" : string.Join(";", dictPegProperties.Keys),
                dictObjProperties == null ? "" : string.Join(";", dictObjProperties.Keys),
                strParaMeter, strData, strObjType, strAttachInfo, pegName, objName));

            ChromiumDriver chromDriv = GetChromiumDriver();
            if (((!isMergeMode) && string.IsNullOrEmpty(chromDriv.Url)))
            {
                strError = "Not navigate a link yet.";
                return false;
            }
            bool isOk = false;
            if (dealResult == null)
                dealResult = new MARSDealResult();
            dealResult.ActualInputData = strData;
            dealResult.ReturnedData = strAttachInfo;

            bool isSwitchTo = false;
            string strRestoreWindow = "";
            try
            {
                List<IWebElement> lstTargetElement = FindObject(dictPegProperties, dictObjProperties, strAttachInfo, strParaMeter, strData, strObjType,
                    ref isOk, ref strError,
                    ref isSwitchTo, ref strRestoreWindow);
                if ((!isOk) || (lstTargetElement == null) || (lstTargetElement.Count <= 0))
                {
                    return false;
                }

                var targetElement = lstTargetElement[0];
                bool dataIsSetCheck = false;
                strData = strData ?? "";
                if (!string.IsNullOrEmpty(strData))
                {
                    if ((string.Compare("on", strData, true) == 0)||(string.Compare("yes", strData, true)==0))
                        dataIsSetCheck = true;
                    else if ((string.Compare("off", strData, true) == 0)||(string.Compare("no", strData, true)==0))
                        dataIsSetCheck = false;
                    else if (bool.TryParse(strData,out dataIsSetCheck))
                    {
                        logger.Warnning("WebSetBox", strError = "the data should be yes|on|true for checked, no|off|false for non-checked");
                        return false;
                    }
                }
                logger.Info("wetSetBox", $"data|{strData}|convertTo|{dataIsSetCheck}|object.Selected|{targetElement.Selected}|location|{targetElement.Location}|{targetElement.Size}");
                if (targetElement.Selected!=dataIsSetCheck)
                {
                    Actions actn = new Actions(chromDriv);
                    actn.MoveToElement(targetElement)
                        .Click()
                        .Perform();
                    //targetElement.Click();
                }                

                Thread.Sleep(50);
                dealResult.ActualInputData = strData;
                dealResult.AckTime = DateTime.Now;

                dealResult.ResultMessage = "OK";
                return true;
            }
            catch (Exception e)
            {
                strError = e.Message;
                dealResult.ResultMessage = "FAILED";
                dealResult.AckTime = DateTime.Now;
                dealResult.ErrorMessage = e.Message;
                dealResult.StackInfo = e.StackTrace;
                dealResult.Advice = "Contact Marsquis";
                dealResult.ReturnedData = e.Message;
                logger.Error("WebSelectTab", e.Message, e);
                return false;
            }
            finally
            {
                if (isSwitchTo)
                {
                    try
                    {
#if _EnableChrome
                        this.marsChromeDriver.SwitchTo().Window(strRestoreWindow);
#else
                        this.marsWebDriver.SwitchTo().Window(strRestoreWindow);
#endif
                    }
                    catch (Exception ee)
                    {
                        logger.Error("\t", ee.Message, ee);
                    }
                }
                logger.logEnd("WebSelectTab");
            }
        }


        internal bool WebSelectTab(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties,
            string strParaMeter, string strData, string strObjType, string strAttachInfo, string pegName, string objName, ref string strError, 
            ref MARSDealResult dealResult, bool isMergeMode = false)
        {
            logger.logBegin("WebSelectTab", string.Format("{0}, stepId:{1} (Peg:{2}, obj:{3}, para:{4}, data:{5}), type {6}, attach:[{7}] pegName:[{8}], objName:[{9}]",
                strKeyword, stepId,
                dictPegProperties == null ? "" : string.Join(";", dictPegProperties.Keys),
                dictObjProperties == null ? "" : string.Join(";", dictObjProperties.Keys),
                strParaMeter, strData, strObjType, strAttachInfo, pegName, objName));

            ChromiumDriver chromDriv = GetChromiumDriver();
            if (((!isMergeMode) && string.IsNullOrEmpty(chromDriv.Url)))
            {
                strError = "Not navigate a link yet.";
                return false;
            }
            bool isOk = false;
            if (dealResult == null)
                dealResult = new MARSDealResult();
            dealResult.ActualInputData = strData;
            dealResult.ReturnedData = strAttachInfo;

            bool isSwitchTo = false;
            string strRestoreWindow = "";
            try
            {
                List<IWebElement> lstTargetElement = FindObject(dictPegProperties, dictObjProperties, strAttachInfo, strParaMeter, strData, strObjType,
                    ref isOk, ref strError,
                    ref isSwitchTo, ref strRestoreWindow);
                if ((!isOk) || (lstTargetElement == null) || (lstTargetElement.Count <= 0))
                {
                    return false;
                }

                var targetElement = lstTargetElement[0];
                targetElement.Click();

                Thread.Sleep(50);
                dealResult.ActualInputData = strData;
                dealResult.AckTime = DateTime.Now;

                dealResult.ResultMessage = "OK";
                return true;
            } catch (Exception e)
            {
                strError = e.Message;
                dealResult.ResultMessage = "FAILED";
                dealResult.AckTime = DateTime.Now;
                dealResult.ErrorMessage = e.Message;
                dealResult.StackInfo = e.StackTrace;
                dealResult.Advice = "Contact Marsquis";
                dealResult.ReturnedData = e.Message;
                logger.Error("WebSelectTab", e.Message, e);
                return false;
            }
            finally
            {
                if (isSwitchTo)
                {
                    try
                    {
#if _EnableChrome
                        this.marsChromeDriver.SwitchTo().Window(strRestoreWindow);
#else
                        this.marsWebDriver.SwitchTo().Window(strRestoreWindow);
#endif
                    }
                    catch (Exception ee)
                    {
                        logger.Error("\t", ee.Message, ee);
                    }
                }
                logger.logEnd("WebSelectTab");
            }
        }

        internal void HighLightObject(IWebElement[]? elements, string webXPath, string webName, ref bool isOk, ref string strError)
        {
            try
            {
                IWebElement[] filteredElements = null;
                ChromiumDriver chromDriv = GetChromiumDriver();
                if ((elements != null) || (elements.Length > 0))
                {
                    filteredElements = elements
                        .SelectMany(parent => parent.FindElements(By.XPath(webXPath)))
                        .ToArray();
                }
                else
                {
                    filteredElements = chromDriv.FindElements(By.XPath(webXPath)).ToArray();
                }
                /// 
                if ((filteredElements == null) || (filteredElements.Length <= 0))
                {
                    isOk = false;
                    logger.Error("HighLightObject", $"cant find the target object|{webXPath}");
                    return;
                }
                if ((!string.IsNullOrEmpty(webName)) && (filteredElements.Length > 1))
                {
                    filteredElements = filteredElements.Where(p => p.GetAttribute("name").Equals(webName, StringComparison.OrdinalIgnoreCase)
                        || p.GetAttribute("id").Equals(webName, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    if ((filteredElements == null) || (filteredElements.Length <= 0))
                    {
                        isOk = false;
                        logger.Error("HighLightObject", $"cant find the target object after filted by id/name|{webXPath}|{webName}");
                        return;
                    }
                }

                foreach (IWebElement element in filteredElements)
                {
                    if (element == null) continue;
                    HightlightObject(element, 3, 1000);
                }
                isOk = true;
            }catch(Exception e)
            {
                logger.Error("HighLightObject", strError = e.Message, e);
                isOk = false;
            }
        }

        internal void HighLightObjectByCss(IWebElement element, int iHighLightCount=3, int iWaitTime = 1000)
        {
            // ¶¨Òå CSS ¶¯»­
            string css = @"@keyframes blink {
            0% { border: 3px solid red; }
            50% { border: 3px solid transparent; }
            100% { border: 3px solid red; }
        }
        .blink-animation {
            animation: blink 1s "+ iHighLightCount +"\r\n}";       
            ChromiumDriver chromDriv = GetChromiumDriver();
            var js = chromDriv as IJavaScriptExecutor;
            // ×¢Èë CSS
            js.ExecuteScript($"var style = document.createElement('style'); style.innerHTML = `{css}`; document.head.appendChild(style);");

            // Ìí¼Ó¶¯»­Àà
            js.ExecuteScript("arguments[0].classList.add('blink-animation')", element);

            // 3 ÃëºóÒÆ³ý¶¯»­Àà£¨¶¯»­³ÖÐø 3 ´Î£©
            Thread.Sleep(3000);
            js.ExecuteScript("arguments[0].classList.remove('blink-animation')", element);
        }


        internal object HightlightObject(IWebElement targetElement, int iHightlightCount=3, int iWaitTime=1000)
        {
            if (targetElement == null) return null;
            object srcBorder = null;

            // 3. ±£´æÔ­Ê¼ÑùÊ½£¨±ÜÃâ¸²¸Ç£©
            ChromiumDriver chromDriv = GetChromiumDriver();
            IJavaScriptExecutor js = (IJavaScriptExecutor)chromDriv;
            string strOldBorder = (string)js.ExecuteScript("return arguments[0].style.border", targetElement);

            //for (int i = 0; i < iHightlightCount; i++)
            {
                //string strOldBorder = "return arguments[0].style.border ;";
#if _EnableChrome
                srcBorder = this.marsChromeDriver.ExecuteScript(strOldBorder, new object[] { targetElement });
                string strtmpScript = "arguments[0].style.border = '1px solid red' ;";
                this.marsChromeDriver.ExecuteScript(strtmpScript, new object[] { targetElement });
#else           
                //srcBorder = this.marsWebDriver.ExecuteScript(strOldBorder, new object[] { targetElement });
                string strtmpScript = "arguments[0].style.border = '1px solid red' ;";
                //this.marsWebDriver.ExecuteScript(strtmpScript, new object[] { targetElement });
                chromDriv.ExecuteScript(strtmpScript, new object[] { targetElement });
                Thread.Sleep(iWaitTime*iHightlightCount);
#endif
                //js.ExecuteScript("arguments[0].style.border = ''", targetElement);

            }

            //// 5. »Ö¸´Ô­Ê¼ÑùÊ½
            //if (!string.IsNullOrEmpty(strOldBorder))
            //{
            //    js.ExecuteScript($"arguments[0].style.border = '{strOldBorder}'", targetElement);
            //}
            //else
            //{
            //    // Èç¹ûÔ­±¾Ã»ÓÐ style.border£¬Ö±½ÓÇå³ý
            //    js.ExecuteScript("arguments[0].style.border = ''", targetElement);
            //}

            return srcBorder;
        }

        private void RestoreObjectBorder(IWebElement targetElement, object srcBorder)
        {
            string strRestore = "arguments[0].style.border = '" + (srcBorder == null ? "" : srcBorder.ToString()) + "';";
#if _EnableChrome
            this.marsChromeDriver.ExecuteScript(strRestore, new object[] { targetElement });
#else
            this.marsWebDriver.ExecuteScript(strRestore, new object[] { targetElement });
#endif
        }


        private bool WebInnerCapture(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties,
            string strParaMeter, string strData, string strObjType, string strAttachInfo, string pegName, string objName, ref string strError,
            ref MARSDealResult dealResult, bool isMergeMode)
        {
            logger.logBegin("WebInnerCapture", string.Format("{0}, stepId:{1} (Peg:{2}, obj:{3}, para:{4}, data:{5}), type {6}, attach:[{7}] pegName:[{8}], objName:[{9}]",
                strKeyword, stepId,
                dictPegProperties == null ? "" : string.Join(";", dictPegProperties.Keys),
                dictObjProperties == null ? "" : string.Join(";", dictObjProperties.Keys),
                strParaMeter, strData, strObjType, strAttachInfo, pegName, objName));

            ChromiumDriver chromDriv = GetChromiumDriver();
            if (((!isMergeMode) && string.IsNullOrEmpty(chromDriv.Url)))
            {
                strError = "Not navigate a link yet.";
                return false;
            }
            bool isOk = false;
            if (dealResult == null)
                dealResult = new MARSDealResult();
            dealResult.ActualInputData = strData;
            dealResult.ReturnedData = strAttachInfo;
            bool isSwitchTo = false;
                string strRestoreWindow = "";

            bool isHighLight = false;
            strParaMeter = MarsWEBHighlighter.CheckHightLightSettings(strParaMeter, ref isHighLight);
#if !_mars_msg_center
            List<Mars.AutoTestingDriver.SystemUtil.MarsParametersOp> lstParaOp = Mars.AutoTestingDriver.SystemUtil.MarsParametersOp.GetOpType(strParaMeter, ref strError);
#endif
            try
            {
                if (!((string.Compare(strKeyword, "CaptureValue", true) == 0) || (string.Compare(strKeyword, "CaptureAndCompare", true) == 0)))
                {
                    dealResult.ResultMessage = "FAILED";
                    dealResult.AckTime = DateTime.Now;
                    dealResult.ErrorMessage = strError = "only captureValue and CaputureAndCompare can be supported for web capture";
#if !_mars_msg_center
                    dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
#endif
                    dealResult.Advice = "Contact Marsquis";
                    dealResult.ReturnedData = strAttachInfo;
                    return false;
                }
                IWebElement targetElement = null;
                List<IWebElement> lstTargetElement = FindObject(dictPegProperties, dictObjProperties, strAttachInfo, strParaMeter, strData, strObjType, 
                    ref isOk, ref strError,
                    ref isSwitchTo, ref strRestoreWindow);
                if ((!isOk) || (lstTargetElement == null) || (lstTargetElement.Count <= 0))
                {
                    dealResult.ResultMessage = "FAILED";
                    dealResult.AckTime = DateTime.Now;
                    dealResult.ErrorMessage = strError;
#if !_mars_msg_center
                    dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
#endif
                    dealResult.Advice = "Contact Marsquis";
                    dealResult.ReturnedData = strAttachInfo;
                    return false;
                }
                targetElement = lstTargetElement[0];

                Actions act = new Actions(chromDriv);
                act.MoveToElement(targetElement)
                    .Perform();

                switch (strObjType.ToUpper())
                {
                    case "SWFTABLE":
                        //}
                        //if (string.Compare("swfTable", strObjType, true) == 0)
                        {
                            string[] arrParas = strParaMeter == null ? null : strParaMeter.Split(';');
                            int iColCoumn;
                            if (!int.TryParse(arrParas[1], out iColCoumn))
                            {
                                logger.Error("WebCaptureAndCompare", strError = $"Only column index number is supported, but it is [{strParaMeter}] ");
                                dealResult.ResultMessage = "FAILED";
                                dealResult.AckTime = DateTime.Now;
                                dealResult.ErrorMessage = strError;
#if !_mars_msg_center
                                dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
#endif
                                dealResult.Advice = "Contact Marsquis";
                                dealResult.ReturnedData = strAttachInfo;
                                return false;
                            }

                            var lstOfTr = targetElement.FindElements(By.CssSelector($"tr > td:nth-child({iColCoumn})")).ToList();
                            if (lstOfTr == null)
                            {
                                logger.Error("WebCaptureAndCompare", strError = $"can't find such column data [{strParaMeter}]");
                                dealResult.ResultMessage = "FAILED";
                                dealResult.AckTime = DateTime.Now;
                                dealResult.ErrorMessage = strError;
#if !_mars_msg_center
                                dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
#endif
                                dealResult.Advice = "Contact Marsquis";
                                dealResult.ReturnedData = strAttachInfo;
                                return false;
                            }

                            //string strOldBorder = "return arguments[0].style.border ;";
                            //object srcBorder = this.marsWebDriver.ExecuteScript(strOldBorder, new object[] { targetElement});
                            //string strtmpScript = "arguments[0].style.border = '1px solid red' ;";
                            //this.marsWebDriver.ExecuteScript(strtmpScript, new object[] { targetElement });
                            object srcBorder = this.HightlightObject(targetElement);

                            List<string> lstRslt = new List<string>();
                            foreach (var itm in lstOfTr)
                            {
                                if (itm == null) continue;
                                if (itm.Size.Height < 5) continue; //¹ýÂË¸ñÊ½
                                string strItmTxt = itm.Text;
#if !_mars_msg_center
                                if (lstParaOp != null)
                                {
                                    if (!string.IsNullOrEmpty(strItmTxt))
                                        foreach (var x in lstParaOp)
                                        {
                                            strItmTxt = x.dealWithData(strItmTxt);
                                        }
                                }
#endif
                                lstRslt.Add(strItmTxt);
                            }
                            System.Threading.Thread.Sleep(200);

                            this.RestoreObjectBorder(targetElement, srcBorder);
                            //for (int i = 0; i < 2; i++)
                            //{
                            //    this.HightlightObject(targetElement);
                            //    System.Threading.Thread.Sleep(200);
                            //    this.RestoreObjectBorder(targetElement, srcBorder);
                            //}

                            dealResult.ActualInputData = strData;
                            dealResult.AckTime = DateTime.Now;
                            dealResult.ReturnedData = string.Join("\r\n", lstRslt);
                            dealResult.ResultMessage = "OK";
                            //string strColumn = targetElement.FindElement(By.CssSelector("tr:first-child td"))
                            return true;
                        }
                        break;
                    case "SWFEDIT":
                    case "SWFLABEL":

                        //if ((string.Compare("swfEdit", strObjType, true) == 0)
                        //|| (string.Compare("swfLabel", strObjType, true) == 0))
                        {
                            object srcObjectBoarder = this.HightlightObject(targetElement);

                            dealResult.ReturnedData = targetElement.GetAttribute("value");
                            if (dealResult.ReturnedData == null)
                            {
                                dealResult.ReturnedData = targetElement.Text;
                            }
#if !_mars_msg_center
                            if (lstParaOp != null)
                            {
                                if (!string.IsNullOrEmpty(dealResult.ReturnedData))
                                    foreach (var x in lstParaOp)
                                    {
                                        dealResult.ReturnedData = x.dealWithData(dealResult.ReturnedData);
                                    }
                            }
#endif
                            dealResult.ActualInputData = strData;
                            dealResult.AckTime = DateTime.Now;

                            dealResult.ResultMessage = "SUCCESS";
                            this.RestoreObjectBorder(targetElement, srcObjectBoarder);
                            //Thread.Sleep(500);
                            //for (int i = 0; i < 2; i++)
                            //{
                            //    srcObjectBoarder = this.HightlightObject(targetElement);
                            //    Thread.Sleep(500);
                            //    this.RestoreObjectBorder(targetElement, srcObjectBoarder);
                            //}
                            logger.Info("WebCaptureAndCompare", $"captured data is [{dealResult.ReturnedData}]");
                            return true;
                        }
                        break;
                    case "SWFRADIOBUTTON":
                //if ((string.Compare("swfRadioButton", strObjType, true) == 0))
                    {
                            bool isChecked = false;
                            string strTmpRadioAll = "", txt = "";

                            foreach (var itm in lstTargetElement)
                            {
                                if (itm == null)
                                {
                                    continue;
                                }
                                var srcObjStyle = this.HightlightObject(itm);
                                var tmpLabel = itm.FindElement(By.XPath("following-sibling::*"));
                                if (tmpLabel == null)
                                {
                                    strTmpRadioAll += (";");
                                    Thread.Sleep(200);
                                    this.RestoreObjectBorder(itm, srcObjStyle);
                                    continue;
                                }
                                Thread.Sleep(200);
                                txt = tmpLabel.Text;
                                strTmpRadioAll += (";" + txt);
                                this.RestoreObjectBorder(itm, srcObjStyle);

                                string strChecked = itm.GetAttribute("CHECKED");
                                if ((string.Compare("checked", strChecked, true) == 0)
                                    || (string.Compare("true", strChecked, true) == 0))
                                {
                                    isChecked = true;
                                    dealResult.ReturnedData = txt;
                                    break;
                                }
                            }
                            if (!isChecked)
                            {
                                dealResult.ReturnedData = strTmpRadioAll;
                            }
#if !_mars_msg_center
                            if (lstParaOp != null)
                            {
                                if (!string.IsNullOrEmpty(dealResult.ReturnedData))
                                    foreach (var x in lstParaOp)
                                    {
                                        dealResult.ReturnedData = x.dealWithData(dealResult.ReturnedData);
                                    }
                            }
#endif
                            dealResult.ResultMessage = "SUCCESS";
                            dealResult.AckTime = DateTime.Now;
                            return true;
                        }
                        break;
                    default:
                        {
                            dealResult.ResultMessage = "FAILED";
                            dealResult.AckTime = DateTime.Now;
                            dealResult.ErrorMessage = strError = $"unsupported object type:[{strObjType}]";
#if !_mars_msg_center
                            dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
#endif
                            dealResult.Advice = "Contact Marsquis";
                            dealResult.ReturnedData = strAttachInfo;

                            return false;
                        }
                }
            }
            catch (Exception e)
            {
                strError = e.Message;
                dealResult.ResultMessage = "FAILED";
                dealResult.AckTime = DateTime.Now;
                dealResult.ErrorMessage = e.Message;
                dealResult.StackInfo = e.StackTrace;
                dealResult.Advice = "Contact Marsquis";
                dealResult.ReturnedData = e.Message;
                logger.Error("WebCaptureAndCompare", e.Message, e);
                return false;
            }
            finally
            {
                if (isSwitchTo)
                {
                    try
                    {
#if _EnableChrome
                        this.marsChromeDriver.SwitchTo().Window(strRestoreWindow);
#else
                        this.marsWebDriver.SwitchTo().Window(strRestoreWindow);
#endif
                    }
                    catch (Exception ee)
                    {
                        logger.Error("\t", ee.Message, ee);
                    }
                }
                logger.logEnd("WebCaptureAndCompare");
            }
        }
        internal bool WebCaptureAndCompare(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties,
            string strParaMeter, string strData, string strObjType, string strAttachInfo, string pegName, string objName, ref string strError, 
            ref MARSDealResult dealResult, bool isMergeMode = false)
        {

            return WebInnerCapture(strKeyword, stepId, dictPegProperties, dictObjProperties, strParaMeter, strData, strObjType, strAttachInfo, pegName,
                objName, ref strError, ref dealResult, isMergeMode);
        }


        internal bool WebCaptureValue(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties,
            string strParaMeter, string strData, string strObjType, string strAttachInfo, string pegName, string objName, ref string strError, 
            ref MARSDealResult dealResult, bool isMergeMode = false)
        {
            return WebInnerCapture(strKeyword, stepId, dictPegProperties, dictObjProperties, strParaMeter, strData, strObjType, strAttachInfo, pegName,
                objName, ref strError, ref dealResult, isMergeMode);
        }

        
        internal bool WebScrollWindow(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties,
            string strParaMeter, string strData, string strObjType, string strAttachInfo, string pegName, string objName, ref string strError, 
            ref MARSDealResult dealResult, bool isMergeMode = false)
        {
            logger.logBegin("WebScrollWindow", string.Format("{0}, stepId:{1} (Peg:{2}, obj:{3}, para:{4}, data:{5}), type {6}, attach:[{7}] pegName:[{8}], objName:[{9}]",
                strKeyword, stepId,
                dictPegProperties == null ? "" : string.Join(";", dictPegProperties.Keys),
                dictObjProperties == null ? "" : string.Join(";", dictObjProperties.Keys),
                strParaMeter, strData, strObjType, strAttachInfo, pegName, objName));

            ChromiumDriver chromDriv = GetChromiumDriver();
            if (((!isMergeMode) && string.IsNullOrEmpty(chromDriv.Url)))
            {
                strError = "Not navigate a link yet.";
                return false;
            }
            bool isOk = false;
            if (dealResult == null)
                dealResult = new MARSDealResult();
            dealResult.ActualInputData = strData;
            dealResult.ReturnedData = strAttachInfo;

            bool isSwitchTo = false;
            string strRestoreWindow = "";
            try
            {
                List<IWebElement> lstTargetElement = FindObject(dictPegProperties, dictObjProperties, strAttachInfo, strParaMeter, strData, strObjType, 
                    ref isOk, ref strError,
                    ref isSwitchTo, ref strRestoreWindow);
                if ((!isOk) || (lstTargetElement == null) || (lstTargetElement.Count <= 0))
                {
                    return false;
                }
                var targetElement = lstTargetElement[0];

                //targetElement.Click();
                //var pt = targetElement.Location;

#if _EnableChrome
                this.marsChromeDriver.ExecuteScript("arguments[0].scrollIntoView(true);", targetElement);
#else
                this.marsWebDriver.ExecuteScript("arguments[0].scrollIntoView(true);", targetElement);
#endif

                dealResult.ActualInputData = strData;
                dealResult.AckTime = DateTime.Now;

                dealResult.ResultMessage = "OK";
                return true;
            }
            catch (Exception e)
            {
                strError = e.Message;
                dealResult.ResultMessage = "FAILED";
                dealResult.AckTime = DateTime.Now;
                dealResult.ErrorMessage = e.Message;
                dealResult.StackInfo = e.StackTrace;
                dealResult.Advice = "Contact Marsquis";
                dealResult.ReturnedData = e.Message;
                logger.Error("WebScrollWindow", e.Message, e);
                return false;
            }
            finally
            {
                logger.logEnd("WebScrollWindow");
            }
        }
        /// <summary>
        /// popup a dialog first, then wait for inputing of the code, then fill edit, parameter is wait seconds, default wait seconds is 120 s
        /// </summary>
        /// <param name="strKeyword"></param>
        /// <param name="stepId"></param>
        /// <param name="dictPegProperties"></param>
        /// <param name="dictObjProperties"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="strObjType"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="pegName"></param>
        /// <param name="objName"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <returns></returns>
        internal bool WebSelectListItem(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties,
            string strParaMeter, string strData, string strObjType, string strAttachInfo, string pegName, string objName, ref string strError,
            ref MARSDealResult dealResult, bool isMergeMode = false)
        {
            logger.logBegin("WebSelectListItem", string.Format("{0}, stepId:{1} (Peg:{2}, obj:{3}, para:{4}, data:{5}), type {6}, attach:[{7}] pegName:[{8}], objName:[{9}]",
                strKeyword, stepId,
                dictPegProperties == null ? "" : string.Join(";", dictPegProperties.Keys),
                dictObjProperties == null ? "" : string.Join(";", dictObjProperties.Keys),
                strParaMeter, strData, strObjType, strAttachInfo, pegName, objName));

            ChromiumDriver chromDriv = GetChromiumDriver();
            if (((!isMergeMode) && string.IsNullOrEmpty(chromDriv.Url)))
            {
                strError = "Not navigate a link yet.";
                return false;
            }
            bool isOk = false;
            if (dealResult == null)
                dealResult = new MARSDealResult();
            dealResult.ActualInputData = strData;
            dealResult.ReturnedData = strAttachInfo;

            bool isSwitchTo = false;
            string strRestoreWindow = "";
            try
            {
                List<IWebElement> lstTargetElement = FindObject(dictPegProperties, dictObjProperties, strAttachInfo, strParaMeter, strData, strObjType, 
                    ref isOk, ref strError,
                    ref isSwitchTo, ref strRestoreWindow);
                if ((!isOk) || (lstTargetElement == null) || (lstTargetElement.Count <= 0))
                {
                    return false;
                }
                var targetElement = lstTargetElement[0];
                string strReadOnly = targetElement.GetAttribute("readonly");
                
                //targetElement.Click();
                var pt = targetElement.Location;
                bool isReadOnly = !string.IsNullOrEmpty(strReadOnly);
                logger.Info("\t....", $"readonly:{isReadOnly}");

                logger.Info("\t", $"pt xy - [{pt}]");
                //var left = targetElement.GetAttribute("Left");
                //Mars.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(pt.X + 5, pt.Y + 5);
                Thread.Sleep(50);
                if (!isReadOnly)
                {
                    var lstOfLi = targetElement.FindElements(By.XPath("./li"));
                    string strTmpTxt = "";
                    foreach (var itmLi  in lstOfLi)
                    {
                        Console.WriteLine(strTmpTxt = itmLi.Text);
                        if (string.Compare(strData, strTmpTxt, true) == 0)
                        {
                            Actions acttmp = new Actions(chromDriv); 
                            acttmp.MoveToElement(itmLi)
                                .Click()
                                .Perform();
                            dealResult.AckTime = DateTime.Now;

                            dealResult.ResultMessage = "OK";
                            return true;
                        }
                    }
                    logger.Error("\t", strError = $"can't find sub li with text|{strData}|");
                    return false;
                    
                    //targetElement.Clear();
                    //targetElement.SendKeys(Keys.Home);
                    //Thread.Sleep(50);
                    //if ((targetElement.Enabled) && (!string.IsNullOrEmpty(targetElement.Text)))
                    //{
                    //Actions act = new Actions(marsWebDriver);
                    //act.MoveToElement(targetItem)
                    //    .Perform();
                    //act.Click(targetItem)                        
                    //    .Perform();

                    
                    //dealResult.ActualInputData = strData;
                }
                else
                {
                    dealResult.ActualInputData = strData;
                    try
                    {
                        Actions act = new Actions(chromDriv);
                        act.MoveToElement(targetElement)
                            .Perform();
                        act.Click(targetElement)
                            .SendKeys(strData) // should be arrow and wenter
                            .Perform();
                    }
                    catch (Exception e)
                    {
                        logger.Error("\t", e.Message, e);
                    }
                }

                dealResult.AckTime = DateTime.Now;

                dealResult.ResultMessage = "OK";
                return true;
            }
            catch (Exception e)
            {
                strError = e.Message;
                dealResult.ResultMessage = "FAILED";
                dealResult.AckTime = DateTime.Now;
                dealResult.ErrorMessage = e.Message;
                dealResult.StackInfo = e.StackTrace;
                dealResult.Advice = "Contact Marsquis";
                dealResult.ReturnedData = e.Message;
                logger.Error("WebSelectListItem", e.Message, e);
                return false;
            }
            finally
            {
                if (isSwitchTo)
                {
                    try
                    {
#if _EnableChrome
                        this.marsChromeDriver.SwitchTo().Window(strRestoreWindow);
#else
                        this.marsWebDriver.SwitchTo().Window(strRestoreWindow);
#endif
                    }
                    catch (Exception ee)
                    {
                        logger.Error("\t", ee.Message, ee);
                    }
                }
                logger.logEnd("WebSelectListItem");
            }
        }
#if !_mars_msg_center
        internal bool WebWaitMFACode(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties,
            string strParaMeter, string strData, string strObjType, string strAttachInfo, string pegName, string objName, ref string strError, 
            ref MARSDealResult dealResult, bool isMergeMode = false)
        {
            logger.logBegin("WebWaitMFACode", string.Format("{0}, stepId:{1} (Peg:{2}, obj:{3}, para:{4}, data:{5}), type {6}, attach:[{7}] pegName:[{8}], objName:[{9}]",
                strKeyword, stepId,
                dictPegProperties == null ? "" : string.Join(";", dictPegProperties.Keys),
                dictObjProperties == null ? "" : string.Join(";", dictObjProperties.Keys),
                strParaMeter, strData, strObjType, strAttachInfo, pegName, objName));
            const string cnst_readOlny = "check:readonly";

            ChromiumDriver chromDriv = GetChromiumDriver();
            if (((!isMergeMode) && string.IsNullOrEmpty(chromDriv.Url)))
            {
                strError = "Not navigate a link yet.";
                return false;
            }

            bool isOk = false;
            if (dealResult == null)
                dealResult = new MARSDealResult();
            dealResult.ActualInputData = strData;
            dealResult.ReturnedData = strAttachInfo;
            bool isSwitchTo = false;
            string strRestoreWindow = "";
            try
            {
                int iWaitSecondes = 120;
                if (!string.IsNullOrEmpty(strParaMeter))
                {
                    if (!int.TryParse(strParaMeter.Trim(), out iWaitSecondes))
                    {
                        iWaitSecondes = 120;
                    }
                }
                MarsMFACodeInputForm frm = new MarsMFACodeInputForm();
                frm.setTimer(iWaitSecondes);
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    strData = frm.currrentMFACode;
                }
                frm = null;

                List<IWebElement> lstTargetElement = FindObject(dictPegProperties, dictObjProperties, strAttachInfo, strParaMeter, strData, strObjType,
                    ref isOk, ref strError,
                    ref isSwitchTo, ref strRestoreWindow);
                if ((!isOk) || (lstTargetElement == null) || (lstTargetElement.Count <= 0))
                {
                    if (string.IsNullOrEmpty(strError))
                    {
                        strError = "no Error returns, but no object is found.";
                    }
                    logger.Error("WebWaitMFACode", strError);
                    return false;
                }
                var targetElement = lstTargetElement[0];
                string strReadOnly = targetElement.GetAttribute("readonly");
                targetElement.Click();
                var pt = targetElement.Location;
                bool isReadOnly = !string.IsNullOrEmpty(strReadOnly);
               
                //var left = targetElement.GetAttribute("Left");
                //Mars.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(pt.X + 5, pt.Y + 5);
                Thread.Sleep(50);
                if (!isReadOnly)
                {

                    //targetElement.Clear();
                    //targetElement.SendKeys(Keys.Home);
                    //Thread.Sleep(50);
                    //if ((targetElement.Enabled) && (!string.IsNullOrEmpty(targetElement.Text)))
                    //{
                    Actions act = new Actions(chromDriv);
                    act.MoveToElement(targetElement)
                        .Perform();
                    act.Click(targetElement)
                        .KeyDown(Keys.Shift)
                        .SendKeys(Keys.End)
                        .SendKeys(Keys.Home)
                        .KeyUp(Keys.Shift)
                        .SendKeys(Keys.Delete)
                        .Perform();
                    //}
                    System.Threading.Thread.Sleep(50);
                    act.SendKeys(strData)
                            .Perform();
                    dealResult.ActualInputData = strData;
                }
                else
                {
                    dealResult.ActualInputData = strData;
                    try
                    {

                        Actions act = new Actions(chromDriv);
                        act.MoveToElement(targetElement)
                            .Perform();
                        //act.Click(targetElement)
                        //    .SendKeys(strData) // should be arrow and wenter
                        //    .Perform();
                    }
                    catch (Exception e)
                    {
                        logger.Error("\t", e.Message, e);
                    }
                }

                dealResult.AckTime = DateTime.Now;

                dealResult.ResultMessage = "OK";
                return true;
            }
            catch (Exception e)
            {
                strError = e.Message;
                dealResult.ResultMessage = "FAILED";
                dealResult.AckTime = DateTime.Now;
                dealResult.ErrorMessage = e.Message;
                dealResult.StackInfo = e.StackTrace;
                dealResult.Advice = "Contact Marsquis";
                dealResult.ReturnedData = e.Message;
                logger.Error("WebWaitMFACode", e.Message, e);
                return false;
            }
            finally
            {
                logger.logEnd("WebWaitMFACode");
            }
        }
        
#endif
        internal bool WebFillEdit(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties,
            string strParaMeter, string strData, string strObjType, string strAttachInfo, string pegName, string objName,
            ref string strError, ref MARSDealResult dealResult, bool isMergeMode=false)
        {
            logger.logBegin("WebFillEdit", string.Format("{0}, stepId:{1} (Peg:{2}, obj:{3}, para:{4}, data:{5}), type {6}, attach:[{7}] pegName:[{8}], objName:[{9}]",
                strKeyword, stepId,
                dictPegProperties == null ? "" : string.Join(";", dictPegProperties.Keys),
                dictObjProperties == null ? "" : string.Join(";", dictObjProperties.Keys),
                strParaMeter, strData, strObjType, strAttachInfo, pegName, objName));
            const string cnst_readOlny = "check:readonly";

            ChromiumDriver chromDriv = GetChromiumDriver();
            if (((!isMergeMode) && string.IsNullOrEmpty(chromDriv.Url)))
            {
                strError = "Not navigate a link yet.";
                return false;
            }
            
            bool isOk = false;
            if (dealResult == null)
                dealResult = new MARSDealResult();
            dealResult.ActualInputData = strData;
            dealResult.ReturnedData = strAttachInfo;
            bool isSwitchTo = false;
            string strRestoreWindow = "";
            try
            {
                List<IWebElement> lstTargetElement = FindObject(dictPegProperties, dictObjProperties, strAttachInfo, strParaMeter, strData, strObjType,
                    ref isOk, ref strError,
                    ref isSwitchTo, ref strRestoreWindow);
                if ((!isOk) || (lstTargetElement == null) || (lstTargetElement.Count <= 0))
                {
                    if (string.IsNullOrEmpty(strError))
                    {
                        strError = "no Error returns, but no object is found.";
                    }
                    logger.Error("WebFillEdit", strError);
                    return false;
                }
                var targetElement = lstTargetElement[0];
                string strReadOnly = targetElement.GetAttribute("readonly");
                targetElement.Click();
                var pt = targetElement.Location;
                bool isReadOnly = !string.IsNullOrEmpty(strReadOnly);
                logger.Info("\t....", $"readonly:{isReadOnly}");
                if ((!string.IsNullOrEmpty(strParaMeter)))
                {
                    Regex rg = new Regex(cnst_readOlny);
                    if (rg.IsMatch(strParaMeter))
                    {

                        
                    }
                }

                logger.Info("\t", $"pt xy - [{pt}]");
                //var left = targetElement.GetAttribute("Left");
                //Mars.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.LeftMouseClick(pt.X + 5, pt.Y + 5);
                Thread.Sleep(50);
                if (!isReadOnly)
                {

                    targetElement.Clear();
                    targetElement.SendKeys(Keys.Home);
                    Thread.Sleep(50);
                    //ChromiumDriver chromDriv = GetChromiumDriver();
                    Actions act = new Actions(chromDriv);
                    act.MoveToElement(targetElement)
                        .Perform();
                    act.Click(targetElement)
                        .KeyDown(Keys.Shift)
                        .SendKeys(Keys.End)
                        .SendKeys(Keys.Home)
                        .KeyUp(Keys.Shift)
                        .SendKeys(Keys.Delete)
                        .Perform();

                    //}
                    System.Threading.Thread.Sleep(50);
                    int iSubstrIdx = -1;
                    if ((!string.IsNullOrEmpty(strParaMeter)) && ((iSubstrIdx = strParaMeter.ToUpper().IndexOf(CNST_SUB_STR)) >= 0))
                    {
                        string strSubPara = strParaMeter.Substring(iSubstrIdx + CNST_SUB_STR.Length + 1);
                        int istart = 0, iend = strData.Length - 1;
                        if ((!(string.IsNullOrEmpty(strSubPara))) && (string.Compare(CNST_LAST_COLON, strSubPara, true) == 0))
                        {
                            int iColonPos = strData.IndexOf(':');
                            if (iColonPos < 0)
                            {
                                strError = $"no ':' in data [{strData}]";
                                return false;
                            }
                            strData = strData.Substring(iColonPos + 1);
                        }
                        else
                        {
                            string[] arrData = strData.Split();
                            if (!int.TryParse(arrData[0], out istart))
                            {
                                istart = 0;
                            }

                            if (!((arrData.Length >= 2) && (int.TryParse(arrData[1], out iend))))
                            {
                                iend = strData.Length;
                            }
                            strData = strData.Substring(istart, iend - istart);
                        }
                    }
                    if ((!string.IsNullOrEmpty(strData)) && (strData.IndexOf("{", StringComparison.InvariantCultureIgnoreCase) >= 0))
                    {
                        System.Windows.Forms.SendKeys.SendWait(strData);
                    }
                    else if ((!string.IsNullOrEmpty(strData)) && (string.Compare("{PGDN}", strData, true) == 0))
                    {
                        act.SendKeys(Keys.PageDown)
                            .Perform();

                        //targetElement.SendKeys(strData);
                    }
                    else
                        act.SendKeys(strData)
                            .Perform();
                    dealResult.ActualInputData = strData;
                }
                else
                {
                    dealResult.ActualInputData = strData;
                    try
                    {
                        Actions act = new Actions(chromDriv);
                        act.MoveToElement(targetElement)
                            .Perform();
                    }
                    catch(Exception e)
                    {
                        logger.Error("\t", e.Message, e);
                    }
                }

                dealResult.AckTime = DateTime.Now;

                dealResult.ResultMessage = "OK";
                return true;
            }
            catch (Exception e)
            {
                strError = e.Message;
                dealResult.ResultMessage = "FAILED";
                dealResult.AckTime = DateTime.Now;
                dealResult.ErrorMessage = e.Message;
                dealResult.StackInfo = e.StackTrace;
                dealResult.Advice = "Contact Marsquis";
                dealResult.ReturnedData = e.Message;
                logger.Error("WebFillEdit", e.Message, e);
                return false;
            }
            finally
            {
                logger.logEnd("WebFillEdit");
            }
        }

        private void FilterObjectsByAttr(List<IWebElement> objsSrc, string strAttrName, string strAttrValue, ref bool isOk, ref string strError)
        {
            logger.logBegin("FilterObjectsByAttr",
                string.Format("filter by [{0}] from objList count :[{1}]",
                strAttrName, objsSrc == null ? 0 : objsSrc.Count));
            if (objsSrc == null) return;
            if (string.IsNullOrEmpty(strAttrName)) return;
            try
            {
                int idx = 0;
                while (idx < objsSrc.Count)
                {
                    var o = objsSrc[idx];
                    if (o == null)
                    {
                        idx++;
                        continue;
                    }
                    var strAttrVFromObj = o.GetAttribute(strAttrName);
                    logger.Info("\t", $"try to regular test:[{strAttrValue}] - [{strAttrVFromObj}]");
                    if (!MarsWindowsAPIsExtend.RegularTest(strAttrValue, strAttrVFromObj))
                    {
                        objsSrc.RemoveAt(idx);
                    }
                    else
                        idx++;
                }
                isOk = true;
            } catch (Exception e) {
                logger.Error("FilterObjectsByAttr", strError = e.Message, e);
                isOk = false;
            }
            finally
            {
                logger.logEnd("FilterObjectsByAttr");
            }
        }

        public ChromiumDriver GetChromiumDriver()
        {
            logger.logBegin("GetChromiumDriver", string.Format("currentWebStepMode|{0}|", MARSKeywordWebHelpers.currentWebStepMode));
            return MARSKeywordWebHelpers.currentWebStepMode == MARSStep_WebConnectionMode._bySelenium ?
                this.marsChromeDriver :
                this.marsWebDriver;
        }

        private void SetBackToEdgeOrChromeDriver(ChromiumDriver chrmdrvr)
        {
            if (MARSKeywordWebHelpers.currentWebStepMode == MARSStep_WebConnectionMode._bySelenium)
            {
                this.marsChromeDriver = chrmdrvr;
            }
            else
            {
                this.marsWebDriver = chrmdrvr;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dictPegProperties"></param>
        /// <param name="dictObjProperties"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="strObjType"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <param name="isSwtchUsed"></param>
        /// <param name="restoreToWindow"></param>
        /// <param name="memoryVarList"></param>
        /// <param name="isAllowMuliptleObj">ÓÐÐ©¶ÔÏóÔËÐÐ·µ»Ø¶à¸ö¶ÔÏóÈ»ºó½øÐÐ¶þ´Î¹ýÂË</param>
        /// <returns></returns>
        internal List<IWebElement> FindObject(Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties,
            string strAttachInfo, 
            string strParaMeter, string strData, string strObjType,
            ref bool isOk, ref string strError,ref bool isSwtchUsed, ref string restoreToWindow,
            List<MarsWebMemoryObject> memoryVarList = null,
            bool isAllowMuliptleObj = false)
        {
            int iMark = new Random().Next();
            logger.logBegin("FindObject", $"strData|{strData}");
            isSwtchUsed = false;

            ChromiumDriver chromDriv = GetChromiumDriver();
            logger.Info("FindObject", $"current title|{this.marsChromeDriver.Title}|");
            ITargetLocator swtchTo = chromDriv.SwitchTo();            
            restoreToWindow = chromDriv.CurrentWindowHandle;

            MarsWebMemoryObject toObj = null, fromObj = null;            
            IWebDriver subFrameOrWindowDriver = null;
            try
            {
                if (memoryVarList != null)
                {
                    fromObj = MarsWebMemoryObject.GetFromObj(memoryVarList);
                    toObj = MarsWebMemoryObject.GetToObj(memoryVarList);
                }

                //get pegwindow
                //currentPeg = GetCurrentPeg(dictPegProperties, ref isOk, ref strError);
                //if ((!isOk) || (currentPeg == null))
                //{
                //    isOk = false;
                //    strError = " no Peg find";
                //    return null;
                //}
                isOk = true;
                List<IWebElement> objs = null;
                IWebElement currentTmpObj = null;
                int idx = -1;
                string strKeys = string.Join(";", dictObjProperties.Keys);
                List<IWebElement> lstTmpObject = new List<IWebElement>();
                logger.Info("\t", $"strKeys:|{strKeys}|");
                int iSwithTime = 0; // for multiple iframe levles
                foreach (var k in dictObjProperties.Keys)
                {
                    
                    if (string.IsNullOrEmpty(k)) continue;
                    string strObjLocateV = dictObjProperties[k];
                    if (string.IsNullOrEmpty(strObjLocateV)) continue;

                    logger.Info("\t", $"key|{k}|");
                    if (string.Compare(k, CNST_FRAME, true) == 0)
                    {
                            
                        if (iSwithTime == 0)
                        {
#if _EnableChrome
                            this.marsChromeDriver = (ChromeDriver)this.marsChromeDriver.SwitchTo().DefaultContent();
#else

                            //this.marsWebDriver = (EdgeDriver)this.marsWebDriver.SwitchTo().DefaultContent();
                            chromDriv = (ChromiumDriver)chromDriv.SwitchTo().DefaultContent();
                            SetBackToEdgeOrChromeDriver(chromDriv);
#endif
                        }
                        iSwithTime++;
                        //ÐèÒªÊ¹ÓÃswitch£¬Ä¿Ç°Ö»Ö§³ÖiframeºÍid

                        //var targetFrame = this.marsWebDriver.FindElements(By.Id(strObjLocateV)).FirstOrDefault();
                        var targetFrame = chromDriv.FindElements(By.Id(strObjLocateV)).FirstOrDefault();                        
                        if (targetFrame == null)
                        {
                            strError = $"can't find frame or window by id: [{strObjLocateV}]";
                            logger.Error("\t", strError);
                            break;
                        }
                        logger.Info("\t", "switch to ....");
                        subFrameOrWindowDriver = swtchTo.Frame(targetFrame);
                        isSwtchUsed = true;

                        continue;
                    }
                    if (string.Compare(k, CNST_FRAME_BYNAME, true) == 0)
                    {
                        //ÐèÒªÊ¹ÓÃswitch£¬Ä¿Ç°Ö»Ö§³ÖiframeºÍid
                        if (iSwithTime == 0)
                        {
                            //this.marsWebDriver = (EdgeDriver)this.marsWebDriver.SwitchTo().DefaultContent();
                            chromDriv = (ChromiumDriver)chromDriv.SwitchTo().DefaultContent();
                            SetBackToEdgeOrChromeDriver(chromDriv);
                        }
                        iSwithTime++;
                        //var targetFrame = this.marsWebDriver.FindElements(By.Name(strObjLocateV)).FirstOrDefault();
                        var targetFrame = chromDriv.FindElements(By.Name(strObjLocateV)).FirstOrDefault();

                        if (targetFrame == null)
                        {
                            strError = $"can't find frame or window by name: [{strObjLocateV}]";
                            logger.Error("\t", strError);
                            break;
                        }
                        logger.Info("\t", "switch to ....");
                        
                        subFrameOrWindowDriver = swtchTo.Frame(targetFrame);
                        isSwtchUsed = true;

                        continue;
                    }

                    if (string.Compare(k, CNST_CSSSELECTOR, true) == 0)
                    {

                        IWebElement parentObjFromMemory = null;
                        if (fromObj != null)
                        {
                            parentObjFromMemory = MarsWebMemoryObject.GetFromObjVariable(fromObj.memoryObjectName);
                            if (parentObjFromMemory == null)
                            {
                                logger.Error("\t", strError = $"no object [{fromObj.memoryObjectName}] stored in variable table");
                                return null;
                            }
                            var prnt = parentObjFromMemory.FindElement(By.XPath(".."));
                            if (prnt == null)
                            {
                                logger.Error("\t", strError = "can't find parent by Xpath '..'");
                                return null;
                            }

                            lstTmpObject = prnt.FindElements(By.CssSelector(strObjLocateV))
                                .ToList();
                        }
                        else
                        {
                            lstTmpObject = chromDriv.FindElements(By.CssSelector(strObjLocateV))
                                .ToList();
                        }
                        if (objs == null)
                        {
                            objs = lstTmpObject;
                        }
                        else
                        {
                            objs = objs.Intersect(lstTmpObject).ToList();
                        }

                        logger.Info("\t", string.Format("after selector CSS:[{0}], objs is/are [{1}]", strObjLocateV,
                            objs == null ? -1 : objs.Count));
                        continue;
                    }

                    if (string.Compare(k, CNST_WEBID, true) == 0)
                    {
                        try
                        {
                            logger.Info("\t", $"checking {k} begin...");
                            if (objs == null)
                            {
                                var objstmp = chromDriv.FindElements(By.Id(strObjLocateV));
                                objs = objstmp.ToList();
                            }
                            else
                            {
                                FilterObjectsByAttr(objs, "id", strObjLocateV, ref isOk, ref strError);
                                if (!isOk)
                                    return null;
                            }

                            if ((objs == null) || (objs.Count == 0))
                            {
                                strError = $"Unable to locate the object by ID [{strObjLocateV}]";
                                logger.Error("\t", strError);
                                isOk = false;
                                return null;
                            }
                            if (objs.Count > 1)
                            {
                                strError = $"There are [{objs.Count}] objects by ID [{strObjLocateV}]";
                                logger.Error("\t", strError);
                                isOk = false;
                                return null;
                            }
                            currentTmpObj = objs[0];

                            logger.Info("\t", $"checking {k} end with {isOk}...");
                            continue;
                        }
                        finally
                        {
                            logger.Info("\t", $"checking {k} end with {isOk}...");
                        }

                    }
                    else if ((string.Compare(k, CNST_WEBNAME, true) == 0)||(string.Compare(k,"SwfName",true)==0))
                    {
                        try
                        {
                            if (objs == null)
                            {

                                var objstmp = chromDriv.FindElements(By.Name(strObjLocateV));
                                objs = objstmp.ToList();
                            }
                            else
                            {
                                FilterObjectsByAttr(objs, "name", strObjLocateV, ref isOk, ref strError);
                                if (!isOk)
                                    return null;
                            }
                            if ((objs == null) || (objs.Count == 0))
                            {
                                strError = $"Unable to locate the object by Name [{strObjLocateV}]";
                                logger.Error("\t", strError);
                                isOk = false;
                                return null;
                            }
                        }
                        finally
                        {
                            logger.Info("\t", $"checking {k} end with {isOk}...");
                        }
                    }
                    else if (string.Compare(k, "index", true) == 0)
                    {
                        if (!int.TryParse(strObjLocateV, out idx))
                        {
                            isOk = false;
                            strError = $"index number is not a number-[{strObjLocateV}]";
                            logger.Error("\t", strError);
                            break;
                        }
                    }
                    else if (string.Compare(k, CNST_WEBCLASS, true) == 0)
                    {// class Ó¦¸Ã²»ÊÇµÚÒ»¸ö¹ýÂËµÄÌõ¼þ 
                        try
                        {
                            logger.Info("\t", $"checking {k} Begin...");
                            var tmpLst = chromDriv.FindElements(By.CssSelector($".{strObjLocateV}")).ToList();
                            objs = objs.Intersect(tmpLst).ToList();

                            if ((objs == null) || (objs.Count == 0))
                            {
                                strError = $"Unable to locate the object by class [{strObjLocateV}]";
                                logger.Error("\t", strError);
                                isOk = false;
                                return null;
                            }
                        }
                        finally
                        {
                            logger.Info("\t", $"checking {k} end with {isOk}...");
                        }
                    }
                    else if (string.Compare(k, CNST_XPATH, true) == 0)
                    {
                        try
                        {
                            logger.Info("\t", $"checking {k} Begin|"+(objs==null?-1: objs.Count)+"|");
                            
                            List<IWebElement> lstTmp = GetObjectsByXPath(strObjLocateV, ref strError, ref isOk);
                            if ((!isOk) || (lstTmp == null) || (lstTmp.Count <= 0))
                            {
                                strError = $"Unable to locate the object by XPath [{strObjLocateV}]";
                                logger.Error("\t", strError);
                                isOk = false;
                                return null;
                            }
                            if (objs == null)
                            {
                                objs = lstTmp;
                            }
                            else
                            {

                                //filter 
                                objs = objs.Intersect(lstTmp)
                                .ToList();
                            }
                            if ((objs == null) || (objs.Count <= 0))
                            {
                                strError = $"Unable to locate the object by XPath [{strObjLocateV}]";
                                logger.Error("\t", strError);
                                isOk = false;
                                return null;
                            }
                        }
                        finally
                        {
                            logger.Info("\t", $"checking {k} end with {isOk}...");
                        }
                    }
                    else if (string.Compare(k, CNST_VALUE, true) == 0)
                    {
                        try
                        {
                            logger.Info("\t", $"checking {k} Begin...");
                            //this one should not be the first item of identifers
                            if (objs == null)
                            {
                                strError = "webValue should not be the first item for identifying an object.";
                                logger.Error("\t", strError);
                                isOk = false;
                                return null;
                            }
                            int tmpidx = 0;

                            while (tmpidx < objs.Count)
                            {
                                var itm = objs[tmpidx];
                                if (itm == null)
                                {
                                    objs.RemoveAt(tmpidx);
                                    continue;
                                }
                                if (!MarsWindowsAPIsExtend.RegularTest(strObjLocateV, itm.Text))
                                {
                                    objs.RemoveAt(tmpidx);
                                    continue;
                                }
                                tmpidx++;
                            }
                        }
                        finally
                        {
                            logger.Info("\t", $"checking {k} end with {isOk}...");
                        }
                    }
                    else if (string.Compare(k, CNST_TAG, true) == 0)
                    {
                        try
                        {
                            logger.Info("\t", $"checking {k} Begin...");
                            if (objs == null)
                            {
                                var objstmp = chromDriv.FindElements(By.TagName(strObjLocateV));
                                objs = objstmp.ToList();
                            }
                            else
                            {
                                List<IWebElement> lstTmp = chromDriv.FindElements(By.TagName(strObjLocateV)).ToList();
                                objs = objs.Intersect(lstTmp).ToList();
                            }
                            if ((objs == null) || (objs.Count <= 0))
                            {
                                strError = $"Unable to locate the object by Tag [{strObjLocateV}]";
                                logger.Error("\t", strError);
                                isOk = false;
                                return null;
                            }
                        }
                        finally
                        {
                            logger.Info("\t", $"checking {k} end with {isOk}...");
                        }
                    }
                    else if (string.Compare(k, CNST_INNERHTML, true) == 0)
                    {
                        try
                        {
                            logger.Info("\t", $"checking {k} Begin...");
                            if (objs == null)
                            {
                                strError = "innerHTML should not be the first item of object's identifier.";
                                logger.Error("\t", strError);
                                isOk = false;
                                return null;
                            }
                            idx = 0;
                            FilterObjectsByAttr(objs, "innerHTML", strObjLocateV, ref isOk, ref strError);
                        }
                        finally
                        {
                            logger.Info("\t", $"checking {k} end with {isOk}...");
                        }
                    }
                    else
                    {
                        strError = $"unsupported object's feature :[{k}]";
                        logger.Error("\t", strError);
                        isOk = false;
                        return null;
                    }

                    if ((objs == null) || (objs.Count == 0)) break;
                }
                if ((objs == null) || (objs.Count <= 0))
                {
                    strError = $"Unable to locate the object by identifiers :{strKeys}";
                    logger.Error("\t", strError);
                    isOk = false;
                    return null;
                }
                if (idx >= 0)
                {
                    if (idx >= objs.Count)
                    {
                        strError = $"Index [{idx}] is out of selected objects [{objs.Count}].";
                        logger.Error("\t", strError);
                        isOk = false;
                        return null;
                    }
                    isOk = true;
                    return new List<IWebElement>() { objs[idx] };
                }

                if (String.Compare("swfRadioButton", strObjType, true) == 0)
                {
                    //¶ÔÓÚradiobutton, ÐèÒªÅÐ¶ÏÊÇ·ñÊÇradio
                    List<IWebElement> lstRadios = chromDriv.FindElements(By.CssSelector("input[type=\"radio\"]")).ToList();

                    objs = objs.Intersect(lstRadios).ToList();
                    logger.Info("FindObject", string.Format("Radio button, returns :[{0}]", objs == null ? 0 : objs.Count));
                    return objs;
                }

                if (objs.Count != 1)
                {
                    if (isAllowMuliptleObj) {
                        isOk = true;
                        return objs;
                    }
                    strError = $"There are more than one objects. [{objs.Count}]";
                    for (int i = 0; i < objs.Count; i++)
                    {
                        logger.Info("GetObjectsByXPath", $"{objs[i].Text}|{objs[i].Location}|{objs[i].Size}");
                    }
                    logger.Error("\t", strError);
                    isOk = false;
                    return null;
                }
                if (toObj != null)
                {
                    //put the only object to the memory table
                    MarsWebMemoryObject.StoreObject2Memory(toObj.memoryObjectName, objs[0]);
                }
                return new List<IWebElement>() { objs[0] };
            }
            finally
            {
                //if (isSwtchUsed)
                //{
                //    for (int i = 0; i < iSwtchCount; i++)
                //    {
                //        this.marsWebDriver.SwitchTo().ParentFrame();
                //    }
                //}

            }
        }
        /// <summary>
        /// ¶ÔÓÚwebµÄgrid¶øÑÔ£¬²»Í¬µÄ¿Ø¼þ²úÉúµÄ½á¹¹²»Ò»Ñù£¬¶ÔÓÚAG Grid£¬½«Ìá¹©Á½ÖÖÀàÐÍWebAGGridColumn£¬WebAGGridRow
        /// SearchAndClick, object(column), ColMode:Header;Left_click:-5:5£¬ EQA,¾ÍÊÇ´ÓobjectÖÐ¹ýÂËtextÎªEQAµÄÖµ¡£
        /// SearchAndClick, object(row), dynamicrow;Left_click, XPath://div[3] ,¾ÍÊÇ´ÓÄ³ÐÐÖÐ,Ñ¡Ôñµã»÷Ä³¸ö¶ÔÏó£¬¶ÔÏóµÄ¶¨Î»°´ÕÕxpathµÄÄÚÈÝÈ·¶¨
        /// </summary>
        /// <param name="strKeyword"></param>
        /// <param name="stepId"></param>
        /// <param name="dictPegProperties"></param>
        /// <param name="dictObjProperties"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="strObjType"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="pegName"></param>
        /// <param name="objName"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <param name="isMergeMode"></param>
        /// <returns></returns>
        internal bool WebSearchAndClick(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, 
            Dictionary<string, string> dictObjProperties,
            string strParaMeter, string strData,
            string strObjType,
            string strAttachInfo,
            string pegName,
            string objName,
            ref string strError,
            ref MARSDealResult dealResult, bool isMergeMode = false)
        {
            logger.logBegin("SearchAndClick", string.Format("{0}, stepId:{1} (Peg:{2}, obj:{3}, para:{4}, data:{5}), type {6}, attach:[{7}] pegName:[{8}], objName:[{9}]",
                strKeyword, stepId,
                dictPegProperties == null ? "" : string.Join(";", dictPegProperties.Keys),
                dictObjProperties == null ? "" : string.Join(";", dictObjProperties.Keys),
                strParaMeter, strData, strObjType, strAttachInfo, pegName, objName));
            bool isOk = false;
            if (dealResult == null)
                dealResult = new MARSDealResult();
            dealResult.ActualInputData = strData;
            dealResult.ReturnedData = strAttachInfo;

            bool isHighLight = false;
            strParaMeter = MarsWEBHighlighter.CheckHightLightSettings(strParaMeter, ref isHighLight);
            bool isTestDialog = false;
            strParaMeter = MarsTestXpathDialog.CheckingTestDialog(strParaMeter, ref isTestDialog);

            ChromiumDriver chromDriv = GetChromiumDriver();
            bool isSwitchTo = false;
            string strRestoreWindow = "";
            try
            {
                var lstTargetObj = FindObject(dictPegProperties, dictObjProperties, strAttachInfo, strParaMeter, strData, strObjType, ref isOk,
                    ref strError,
                    ref isSwitchTo, ref strRestoreWindow, null, true);
                if ((!isOk) || (lstTargetObj == null) || (lstTargetObj.Count <= 0))
                {
                    logger.Error("WebSearchAndClick", strError);
                    return false;
                }

                if (isHighLight)
                {
                    int iObjCount = 0;
                    foreach (var obj in lstTargetObj)
                    {
                        MarsWEBHighlighter.HighlightElement3Times(chromDriv, obj);
                        iObjCount++;
                        if (iObjCount >= 5)
                            break;
                    }
                }

                if ((CNST_WEB_OBJECT_TYPE_AGGRID_COL.Equals(strObjType,StringComparison.OrdinalIgnoreCase))
                    ||(CNST_WEB_OBJECT_TYPE_AGGRID_ROW.Equals(strObjType, StringComparison.OrdinalIgnoreCase)))
                {
                    /// ag grid Êý¾Ý
                    /// 
                    string strAdv = "", strStack = "", strReturned= "";
                    isOk = (new Mars.AutoTestingDriver.webSupport.GridTableHelper.MarsAGGridTableHelper(chromDriv).SearchAndClick(lstTargetObj, 
                        strObjType,strParaMeter,strData, dictObjProperties, 
                        ref strReturned,
                        ref strError, ref strAdv, ref strStack));
                    dealResult.AckTime = DateTime.Now;
                    if (!isOk)
                    {
                        dealResult.ErrorMessage = strError;
                        dealResult.Advice = strAdv;
                        dealResult.ResultMessage = "FAILED";
                        
                        logger.Error("WebSearchAndClick", $"{strError}|{strAdv}");
                        return false;
                    }
                    dealResult.ResultMessage = "SUCCESS";
                    dealResult.ReturnedData = strReturned;
                    return true;
                }

                var targetObj = lstTargetObj[0];
                if (string.Compare(CNST_RESERVE_FLASH_TABLE, strObjType, true) == 0)
                {
                    return WebSearchAndClick_FlashTable(targetObj, strParaMeter, strData, ref strError, ref dealResult);
                }
                return false;
            }
            finally
            {
                if (isSwitchTo)
                {
                    try
                    {
                        //this.marsWebDriver.SwitchTo().Window(strRestoreWindow);
                        chromDriv.SwitchTo().Window(strRestoreWindow);
                    }
                    catch (Exception ee)
                    {
                        logger.Error("\t", ee.Message, ee);
                    }
                }
                logger.logEnd("WebSelectMenuItem");
            }
        }

        private bool WebSearchAndClick_FlashTable(IWebElement targetObj, string strParaMeter, string strData, ref string strError, ref MARSDealResult dealResult)
        {
            logger.logBegin("WebSearchAndClick_FlashTable", $"para:{strParaMeter } data:[{strData}]");

            if (dealResult == null)
                dealResult = new MARSDealResult();
            try
            {
                if (string.IsNullOrEmpty(strData))
                {
                    logger.Error("\t", strError = "no data is set");
                    dealResult.ResultMessage = "FAILED";
                    dealResult.AckTime = DateTime.Now;
                    dealResult.ErrorMessage = strError;
#if !_mars_msg_center
                    dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
#endif
                    dealResult.Advice = "Contact Marsquis";
                    dealResult.ReturnedData = strError;
                    return false;
                }
                string objId = targetObj.GetAttribute("id");

                string strScript = "var obj = document.getElementById('" + objId + "') ; " +
                    @"  var iRows = obj.qaFlexTableGetNbRows();
                        var foDealId = -1 ;
                        for (var i=0 ;i<iRows ;i++){
                            foDealId = obj.qaFlexTableGetCellValueByColumnName(i, 'foDealId') ;
                            if (foDealId==" + strData + @" ){
                                obj.qaFlexTableSelectRow(i) ;
                                return i ;
                            }
                        }
                        return -1";

                if (!string.IsNullOrEmpty(objId))
                {
                    //this.marsWebDriver.ExecuteScript(@"alert('nb rows ' + qaFlexTableGetNbRows('*GFtable***0***TableSample***table'))");
#if _EnableChrome
                    var tmpValueFromScript = this.marsChromeDriver.ExecuteScript(strScript);
#else
                    var tmpValueFromScript = this.marsWebDriver.ExecuteScript(strScript);
#endif
                    int iSelectedRow = -1;
                    if (!int.TryParse(tmpValueFromScript == null ? "-1" : tmpValueFromScript.ToString(), out iSelectedRow))
                    {
                        strError = $"can't find trade Id [{strData}]";
                        dealResult.ResultMessage = "FAILED";
                        dealResult.AckTime = DateTime.Now;
                        dealResult.ErrorMessage = strError;
#if !_mars_msg_center
                        dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
#endif
                        dealResult.Advice = "Contact Marsquis";
                        dealResult.ReturnedData = strError;
                        return false;
                    }
                    logger.Info("\t", string.Format("data returns:[{0}]", objId == null ? "" : objId.ToString()));
                    dealResult.ResultMessage = "OK";
                    dealResult.ActualInputData = strData;
                    dealResult.AckTime = DateTime.Now;
                    return true;
                }
                else
                {
                    logger.Error("\t", strError = "Flash object should assign an id");
                    dealResult.ResultMessage = "FAILED";
                    dealResult.AckTime = DateTime.Now;
                    dealResult.ErrorMessage = strError;
#if !_mars_msg_center
                    dealResult.StackInfo = MarsErrorStacks.StackTraceDump();
#endif
                    dealResult.Advice = "Contact Marsquis";
                    dealResult.ReturnedData = strError;
                    return false;
                }
                return false;
            }
            catch (Exception e)
            {
                logger.Error("WebSearchAndClick_FlashTable", strError = e.Message, e);
                dealResult.ResultMessage = "FAILED";
                dealResult.AckTime = DateTime.Now;
                dealResult.ErrorMessage = strError = e.Message;
                dealResult.StackInfo = e.StackTrace;
                dealResult.Advice = "Contact Marsquis";
                dealResult.ReturnedData = e.Message;
                return false;
            } finally
            {
                logger.logEnd("WebSearchAndClick_FlashTable");
            }
        }

        private List<IWebElement> GetObjectsByXPath(string strXPath, ref string strError, ref bool isOk)
        {
            logger.logBegin("GetObjectsByXPath", strXPath);
            try
            {
                ChromiumDriver chromDriv = GetChromiumDriver();
                if (chromDriv == null)
                {
                    isOk = false;
                    strError = "no available target link is opened.";
                    return null;
                }
                List<IWebElement> lstTarget = chromDriv.FindElements(By.XPath(strXPath)).ToList();
                /// filter size==0
                /// 
                int iCnt = lstTarget==null?-1:lstTarget.Count;
                logger.Info("GetObjectByXpath", $"get object|{iCnt}");
                for (int i = iCnt - 1; i >= 0; i--)
                {
                    if (lstTarget[i] == null) lstTarget.RemoveAt(i);
                    if (lstTarget[i].Size.IsEmpty) {
                        logger.Debug("GetObjectByXPath", "remove as empty size");
                        lstTarget.RemoveAt(i);
                    }
                }
                //string strSrc = chromDriv.PageSource;
                //logger.Debug("GetObjectByXpath", $"url|{chromDriv.Url}\r\n|sourcePage|{strSrc}");
                //var hdls = chromDriv.WindowHandles;
                //logger.Debug("GetObjectByXpath", $"handles|{hdls.Count}|title|{chromDriv.Title}");
                //foreach(var handle in hdls){
                //    chromDriv.SwitchTo().Window(handle);
                //    // »ñÈ¡µ±Ç°Ò³ÃæµÄ URL ºÍÄÚÈÝ
                //    var currentUrl = chromDriv.Url;
                //    var pageSource = chromDriv.PageSource;

                //    // ´òÓ¡Ò³ÃæÐÅÏ¢
                //    logger.Debug("GetObjectByXpath", $"Ò³Ãæ URL: {currentUrl}");
                //    logger.Debug("GetObjectByXpath", $"Ò³ÃæÄÚÈÝ: {pageSource}");
                //    logger.Debug("GetObjectByXpath", "------");
                //}
                //var drv = chromDriv.SwitchTo().DefaultContent();
                //var xx = drv.FindElements(By.XPath(strXPath)).ToList();
                //logger.Info("GetObjectByXpath", $"xx count|{xx.Count}");
                //var zz = drv.FindElements(By.XPath("//div[contains(@class, 'h-tabs-tab') and contains(@class, 'h-tabs-tab-no-animation')]")).ToList();
                //logger.Info("GetObjectByXpath", $"zz count|{zz.Count}");

                isOk = true;
                
                return lstTarget;
            }
            catch (Exception e)
            {
                logger.Error("GetObjectsByXPath", strError = e.Message, e);
                isOk = true;
                return null;
            }
            finally
            {
                logger.logEnd("GetObjectsByXPath");
            }
        }

        internal bool WebSelectMenuItem(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties,
            string strParaMeter, string strData, string strObjType, string strAttachInfo,
            string pegName, string objName, ref string strError, ref MARSDealResult dealResult,
            bool isMergeMode = false)
        {
            logger.logBegin("WebSelectMenuItem", string.Format("{0}, stepId:{1} (Peg:{2}, obj:{3}, para:{4}, data:{5}), type {6}, attach:[{7}] pegName:[{8}], objName:[{9}]",
                strKeyword, stepId,
                dictPegProperties == null ? "" : string.Join(";", dictPegProperties.Keys),
                dictObjProperties == null ? "" : string.Join(";", dictObjProperties.Keys),
                strParaMeter, strData, strObjType, strAttachInfo, pegName, objName));
            ChromiumDriver chromDriv = GetChromiumDriver();
            if (((!isMergeMode) && string.IsNullOrEmpty(chromDriv.Url)))
            {
                strError = "Not navigate a link yet. clickMenuItem should run after a pegwindow and startapplication.";
                logger.Error("WebSelectMenuItem", strError);
                return false;
            }
            bool isOk = false;
            if (dealResult == null)
                dealResult = new MARSDealResult();
            dealResult.ActualInputData = strData;
            dealResult.ReturnedData = strAttachInfo;
            bool isSwitchTo = false;
            string strRestoreWindow = "";
            try
            {
                List<MarsWebMemoryObject> memoryVarList = null;
                if (!string.IsNullOrEmpty(strParaMeter))
                {
                    memoryVarList = MarsWebMemoryObject.PhraseParameters(strParaMeter);
                }

                List<IWebElement> lstTargetElement = FindObject(dictPegProperties, dictObjProperties, strAttachInfo, strParaMeter, strData, strObjType, 
                    ref isOk, ref strError,
                    ref isSwitchTo, ref strRestoreWindow, memoryVarList);

                if ((!isOk) || (lstTargetElement == null) || (lstTargetElement.Count <= 0))
                {
                    logger.Error("WebSelectMenuItem", $"Unable to locate the object with Error:[{strError}]");
                    return false;
                }
                var targetElement = lstTargetElement[0];
                logger.Info("WebSelectMenuItem", $"location:{targetElement.Location.X}-{targetElement.Location.Y}, type:{targetElement.GetType()}");

                targetElement.Click();
                Thread.Sleep(50);
                System.Threading.Thread.Sleep(50);
                dealResult.ActualInputData = strData;
                dealResult.AckTime = DateTime.Now;

                dealResult.ResultMessage = "OK";
                return true;
            }
            catch (Exception e)
            {
                strError = e.Message;
                dealResult.ResultMessage = "FAILED";
                dealResult.AckTime = DateTime.Now;
                dealResult.ErrorMessage = strError = e.Message;
                dealResult.StackInfo = e.StackTrace;
                dealResult.Advice = "Contact Marsquis";
                dealResult.ReturnedData = e.Message;
                logger.Error("WebSelectMenuItem", e.Message, e);
                return false;
            }
            finally
            {
                if (isSwitchTo)
                {
                    try
                    {
                        //this.marsWebDriver.SwitchTo().Window(strRestoreWindow);
                        chromDriv.SwitchTo().Window(strRestoreWindow);
                    }
                    catch (Exception ee)
                    {
                        logger.Error("\t", ee.Message, ee);
                    }
                }
                logger.logEnd("WebSelectMenuItem");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dictPegProperties"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        /// 
        private static IWebElement GetCurrentPegViaDriver(IWebDriver curDriver, Dictionary<string, string> dictPegProperties, ref bool isOk, ref string strError)
        {
            logger.logBegin("GetCurrentPegViaDriver");
            //isOk = SwitchToTopDriver(ref strError);
            //IWebDriver subFrameDriver = null;
            IWebElement targetBody = null;
            try
            {
                if (dictPegProperties == null)
                {
                    strError = "No peg window info is set";
                    isOk = false;
                    return null;
                }
                string strValueFromDB = "";
                foreach (var k in dictPegProperties.Keys)
                {
                    isOk = false;
                    if (string.IsNullOrEmpty(k)) continue;

                    strValueFromDB = dictPegProperties[k]; // this.marsWebDriver.Title;
                    if (string.IsNullOrEmpty(strValueFromDB))
                    {
                        logger.Error("GetCurrentPegViaDriver", strError = $"{k} value of pegwindow is empty, please modify object identifier to locate Peg");
                        isOk = false;
                        return null;
                    }
                    if (string.Compare(k, CNST_TYP_TITLE, true) == 0)
                    {
                        if (string.IsNullOrEmpty(strValueFromDB))
                        {
                            strError = "Only title of page is supported to find Pegwindow";
                            isOk = false;
                            return null;
                        }
                        if (!MarsWindowsAPIsExtend.RegularTest(strValueFromDB, curDriver.Title))
                        {
                            strError = $"title of page is not match:[{curDriver.Title}] vs [{strValueFromDB}]";
                            isOk = false;
                            return null;
                        }
                    }else if (CNST_WebURL.Equals(k, StringComparison.OrdinalIgnoreCase))
                    {
                        /// »ñµÃµ±Ç°µÄdriverµÄurl£¬ÅÐ¶ÏurlÖÐÊÇ·ñ´æÔÚÖ¸¶¨µÄ×Ö·û´®
                        /// 
                        string webUrl = curDriver.Url;
                        if (string.IsNullOrEmpty(webUrl))
                        {
                            logger.Error("GetCurrentPegViaDriver", strError = $"No URL is set for the current page, please try it later, or ensure the right app is running.");
                            isOk = false;
                            return null;
                        }
                        if ((webUrl.IndexOf(strValueFromDB,StringComparison.OrdinalIgnoreCase)>=0)||(MarsWindowsAPIsExtend.RegularTest(strValueFromDB, webUrl)))
                        {
                            isOk = true;                            
                        }
                    }
                    else
                    {
                        logger.Error("GetCurrentPegViaDriver", strError = $"unsupported|{k}| and value|{strValueFromDB}. To find Web pegwindow, |{CNST_TYP_TITLE}| and |{CNST_WebURL}| are supported for now");
                        isOk = false;
                        return null;
                    }
                }

                logger.Info("\t", "before get body");
                targetBody = curDriver.FindElements(By.TagName("body"))
                    .FirstOrDefault();
                if (targetBody == null)
                {
                    strError = "Can't find body from current page";
                    logger.Error("GetCurrentPegViaDriver", strError);
                    isOk = false;
                    return null;
                }

                isOk = true;
                return targetBody;
            }
            finally
            {
                logger.logEnd("GetCurrentPegViaDriver", string.Format("find peg:{0} with Error:[{1}]",
                    targetBody == null ? "NULL" : targetBody.ToString(),
                    strError));
            }
        }

        private IWebElement GetCurrentPeg(Dictionary<string, string> dictPegProperties, ref bool isOk, ref string strError)
        {
            logger.logBegin("GetCurrentPeg");
            isOk = SwitchToTopDriver(ref strError);
            ChromiumDriver chromDriv = GetChromiumDriver();
            //IWebDriver subFrameDriver = null;
            try
            {
                if (dictPegProperties == null)
                {
                    strError = "No peg window info is set";
                    isOk = false;
                    return null;
                }
                string strValueFromDB = "";
                foreach (var k in dictPegProperties.Keys)
                {
                    if (string.IsNullOrEmpty(k)) continue;

                    strValueFromDB = dictPegProperties[k]; // this.marsWebDriver.Title;
                    if (string.Compare(k, CNST_TYP_TITLE, true) == 0)
                    {
                        if (string.IsNullOrEmpty(strValueFromDB))
                        {
                            strError = "Only title of page is supported to find Pegwindow";
                            isOk = false;
                            return null;
                        }
                        if (!MarsWindowsAPIsExtend.RegularTest(strValueFromDB, chromDriv.Title))
                        {
                            strError = $"title of page is not match:[{chromDriv.Title}] vs [{strValueFromDB}]";
                            isOk = false;
                            return null;
                        }
                    }

                    if (string.Compare(k, CNST_FRAME, true) == 0)
                    {
                        if (string.IsNullOrEmpty(strValueFromDB))
                        {
                            //find the top level host frame or iframe
                            isOk = SwitchToTopDriver(ref strError);
                            if (!isOk)
                            {
                                return null;
                            }
                            continue;
                        }
                        isOk = SwitchToTargetFrame(strValueFromDB, ref strError);
                        if (!isOk) return null;
                    }
                    if (string.Compare(k, CNST_FRAME_BYNAME, true) == 0)
                    {
                        if (string.IsNullOrEmpty(strValueFromDB))
                        {
                            //find the top level host frame or iframe
                            isOk = SwitchToTopDriver(ref strError);
                            if (!isOk)
                            {
                                return null;
                            }
                            continue;
                        }
                        isOk = SwitchToTargetFrame_Name(strValueFromDB, ref strError);
                        if (!isOk) return null;
                    }
                    
                }

                logger.Info("\t", "before get body");
                var body = chromDriv.FindElements(By.TagName("body"))
                    .FirstOrDefault();
                if (body == null)
                {
                    strError = "Can't find body from current page";
                    isOk = false;
                    return null;
                }

                this.currentPeg = body;
                isOk = true;
                return this.currentPeg;
            }
            finally {
                logger.logEnd("GetCurrentPeg", string.Format("find peg:{0} with Error:[{1}]",
                    this.currentPeg == null ? "NULL" : this.currentPeg.ToString(),
                    strError));
            }
        }

        private bool SwitchToTargetFrame(string strFrameIds, ref string strError)
        {
            logger.logBegin("SwitchToTargetFrame", $"try to switch to frames:[{strFrameIds}]");
            try
            {
                ChromiumDriver chromDriv = GetChromiumDriver();
                if (string.IsNullOrEmpty(strFrameIds))
                {
                    return SwitchToTopDriver(ref strError);
                }
                string[] arrFrames = strFrameIds == null ? null : strFrameIds.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                if (arrFrames == null)
                {
                    return SwitchToTopDriver(ref strError);
                }
                foreach (var f in arrFrames)
                {
                    if (string.IsNullOrEmpty(f)) continue;
                    var currentFrame = chromDriv.FindElements(By.Id(f)).FirstOrDefault();

                    if (currentFrame == null)
                    {
                        logger.Error("SwitchToTargetFrame", strError = $"no such [{f}] iframe is web, from [{strFrameIds}]");
                        return true;
                    }
                    chromDriv.SwitchTo().Frame(currentFrame);
                }
                return true;
            }
            catch (Exception e)
            {
                logger.Error("SwitchToTargetFrame", strError = e.Message, e);
                return false;
            }
            finally
            {
                logger.logEnd("SwitchToTargetFrame");
            }
        }

        private bool SwitchToTargetFrame_Name(string strFrameIds, ref string strError)
        {
            logger.logBegin("SwitchToTargetFrame", $"try to switch to frames:[{strFrameIds}]");
            ChromiumDriver chromDriv = GetChromiumDriver();
            try
            {
                if (string.IsNullOrEmpty(strFrameIds))
                {
                    return SwitchToTopDriver(ref strError);
                }
                string[] arrFrames = strFrameIds == null ? null : strFrameIds.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                if (arrFrames == null)
                {
                    return SwitchToTopDriver(ref strError);
                }
                foreach (var f in arrFrames)
                {
                    if (string.IsNullOrEmpty(f)) continue;
                    var currentFrame = chromDriv.FindElements(By.Name(f)).FirstOrDefault();
                    if (currentFrame == null)
                    {
                        logger.Error("SwitchToTargetFrame", strError = $"no such [{f}] iframe is web, from [{strFrameIds}]");
                        return true;
                    }
                    chromDriv = (ChromiumDriver)chromDriv.SwitchTo().Frame(currentFrame);
                    this.SetBackToEdgeOrChromeDriver(chromDriv);
                }
                return true;
            }
            catch (Exception e)
            {
                logger.Error("SwitchToTargetFrame", strError = e.Message, e);
                return false;
            }
            finally
            {
                logger.logEnd("SwitchToTargetFrame");
            }
        }

        private static bool SwitchToTopViaDriver(ref IWebDriver curDriver, ref string strError)
        {
            try
            {
                //try to switch to top level

#if _EnableChrome
                if (curDriver == null)
                {
                    strError = "Driver is NUll";
                    logger.Error("SwitchToTopDriver", strError);
                    return false;
                }
                curDriver = (ChromeDriver)curDriver.SwitchTo().DefaultContent();
#else
                if (curDriver == null)
                {
                    strError = "Driver is NUll";
                    logger.Error("SwitchToTopDriver", strError);
                    return false;
                }
                curDriver = (EdgeDriver)curDriver.SwitchTo().DefaultContent();
                //if (this.marsWebDriver == null)
                //{
                //    strError = "Driver is NUll";
                //    logger.Error("SwitchToTopDriver", strError);
                //    return false;
                //}
                //this.marsWebDriver = (EdgeDriver)this.marsWebDriver.SwitchTo().DefaultContent();
#endif
                return true;
            }
            catch (Exception e)
            {
                logger.Error("SwitchToTopViaDriver", strError = e.Message, e);
                return false;
            }
        }

        private bool SwitchToTopDriver(ref string strError)
        {
            try
            {
                //try to switch to top level
                ChromiumDriver chromDriv = GetChromiumDriver();
                if (chromDriv == null)
                {
                    strError = "Driver is NUll";
                    logger.Error("SwitchToTopDriver", strError);
                    return false;
                }
                //this.marsWebDriver = (EdgeDriver)this.marsWebDriver.SwitchTo().DefaultContent();
                chromDriv = (ChromeDriver)chromDriv.SwitchTo().DefaultContent();
                this.SetBackToEdgeOrChromeDriver(chromDriv);

                return true;
            }
            catch (Exception e)
            {
                logger.Error("SwitchToTopDriver", strError = e.Message, e);
                return false;
            }

        }

        internal bool WebSelectDropDown(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties, string strParaMeter,
            string strData, string strObjType, string strAttachInfo,
            string pegName, string objName, ref string strError, ref MARSDealResult dealResult, 
            bool isMergeMode = false)
        {
            logger.logBegin("WebSelectDropDown", string.Format("{0}, stepId:{1} (Peg:{2}, obj:{3}, para:{4}, data:{5}), type {6}, attach:[{7}] pegName:[{8}], objName:[{9}]",
                strKeyword, stepId,
                dictPegProperties == null ? "" : string.Join(";", dictPegProperties.Keys),
                dictObjProperties == null ? "" : string.Join(";", dictObjProperties.Keys),
                strParaMeter, strData, strObjType, strAttachInfo, pegName, objName));

            ChromiumDriver chromDriv = GetChromiumDriver();
            if (((!isMergeMode) && string.IsNullOrEmpty(chromDriv.Url)))
            {
                strError = "Not navigate a link yet. clickbutton should run after a pegwindow and startapplication.";
                logger.Error("\t", strError);
                return false;
            }
            bool isOk = false;
            if (dealResult == null)
                dealResult = new MARSDealResult();
            dealResult.ActualInputData = strData;
            dealResult.ReturnedData = strAttachInfo;
            bool isSwitchTo = false;
            string strRestoreWindow = "";
            try
            {
                var lstTargetElement = FindObject(dictPegProperties, dictObjProperties, strAttachInfo, strParaMeter, strData, strObjType,
                    ref isOk, ref strError,
                    ref isSwitchTo, ref strRestoreWindow);
                if ((!isOk) || (lstTargetElement == null) || (lstTargetElement.Count <= 0))
                {
                    logger.Error("\t", $"FindObject return false, with Error:[{strError}]");
                    return false;
                }
                var targetElement = lstTargetElement[0];

                logger.Info("WebSelectDropDown", $"{targetElement.TagName}|location:{targetElement.Location.X}-{targetElement.Location.Y}-{(targetElement.Size)}, type:{targetElement.GetType()}");
                //Actions actn = new Actions(this.marsWebDriver);
                //actn.Click()
                //    .Perform();
                /*if (!string.IsNullOrEmpty(strData))
                {
                    object c = this.marsWebDriver.ExecuteScript(strData);
                    if (c != null)
                    {
                        logger.Info("\t", $"Executed script and returns [{c.ToString()}]");
                    }
                }
                else
                    targetElement.Click();
                */
                
                OpenQA.Selenium.Support.UI.SelectElement slct = 
                    new OpenQA.Selenium.Support.UI.SelectElement(targetElement);
                if (slct == null)
                {

                }
                String allTxt = "";
                foreach (var itm in slct.Options)
                {
                    if (itm == null) continue;
                    allTxt = $"{itm.Text};{allTxt}";
                    if (string.IsNullOrEmpty(itm.Text)) continue ;
                    if (Mars.message.windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest(strData, itm.Text))
                    {
                        Actions act = new Actions(chromDriv); 
                        act.MoveToElement(targetElement)
                            .Perform();
                        slct.SelectByText(itm.Text);
                        act.SendKeys("{enter}")
                            .Perform();
                        dealResult.ActualInputData = strData;
                        dealResult.AckTime = DateTime.Now;

                        dealResult.ResultMessage = "OK";
                        Thread.Sleep(50);
                        System.Threading.Thread.Sleep(50);
                        return true;
                    }
                    //if ( itm.Text)
                }
                
                //Select selectBox = new Select(web.findElement(By.id(elementId)));
                /*
                strParaMeter = strParaMeter ?? "";
                if ((string.Compare("double", strParaMeter, true) == 0)
                    || (string.Compare("activeAndClick", strParaMeter, true) == 0))
                {
                    System.Threading.Thread.Sleep(20);
                    if (targetElement.Displayed)
                        targetElement.Click();
                }
                */
                Thread.Sleep(50);
                System.Threading.Thread.Sleep(50);
                dealResult.ActualInputData = $"Can't find [{strData}] from [{allTxt}]";
                dealResult.AckTime = DateTime.Now;

                dealResult.ResultMessage = "FAILED";
                return true;
            }
            catch (Exception e)
            {
                strError = e.Message;
                dealResult.ResultMessage = "FAILED";
                dealResult.AckTime = DateTime.Now;
                dealResult.ErrorMessage = e.Message;
                dealResult.StackInfo = e.StackTrace;
                dealResult.Advice = "Contact Marsquis";
                dealResult.ReturnedData = e.Message;
                logger.Error("WebSelectDropDown", e.Message, e);
                return false;
            }
            finally
            {
                if (isSwitchTo)
                {
                    try
                    {
#if _EnableChrome
                        this.marsChromeDriver.SwitchTo().Window(strRestoreWindow);
#else
                        this.marsWebDriver.SwitchTo().Window(strRestoreWindow);
#endif
                    }
                    catch (Exception ee)
                    {
                        logger.Error("\t", ee.Message, ee);
                    }
                }
                logger.logEnd("WebSelectDropDown");
            }
        }

        internal bool WebPegWindow(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties,
            string strParaMeter, string strData, string strObjType, 
            string strAttachInfo, string pegName, string objName, 
            ref string strError, ref MARSDealResult dealResult, bool isMergeMode = false)
        {
            logger.logBegin("WebPegWindow", string.Format("{0}, stepId:{1} (Peg:{2}, obj:{3}, para:{4}, data:{5}), type {6}, attach:[{7}] pegName:[{8}], objName:[{9}]",
                strKeyword, stepId,
                dictPegProperties == null ? "" : string.Join(";", dictPegProperties.Keys),
                dictObjProperties == null ? "" : string.Join(";", dictObjProperties.Keys),
                strParaMeter, strData, strObjType, strAttachInfo, pegName, objName));

            if (dealResult == null)
                dealResult = new MARSDealResult();
            dealResult.ActualInputData = strData;
            dealResult.ReturnedData = strAttachInfo;

            if (string.IsNullOrEmpty(homeWindowsHandle))
            {
                ChromiumDriver chromDriv = GetChromiumDriver();
                if (((!isMergeMode) && string.IsNullOrEmpty(chromDriv.Url)))
                {
                    strError = "Not navigate a link yet.";
                    return false;
                }
            }
            bool isOk = false;
            Func<IWebDriver, bool> morethanOneWindows = (d) => {
                return d.WindowHandles.Count > 1;
            };

            if (string.Compare(CNST_PEG_SWITCH_TO_BROWSER, strParaMeter, true) == 0)
            {

#if _EnableChrome
                //WebDriverWait wait = new WebDriverWait(this.marsChromeDriver, TimeSpan.FromSeconds(10));
                //wait.Until<bool>(morethanOneWindows);
                var allWindows = this.marsChromeDriver.WindowHandles;
                logger.Info("WebPegWindow", $"windows|{allWindows.Count}|");
                for (int i = 0; i < allWindows.Count; i++)
                {
                    try
                    {                        
                        //if (allWindows[i] != homeWindowsHandle)
                        if (allWindows[i] != this.marsChromeDriver.CurrentWindowHandle)
                        {                       
                            var tmpDriver = this.marsChromeDriver.SwitchTo().Window(allWindows[i]);
                            if (tmpDriver == null) continue;
                            /// ÅÐ¶ÏÊÇ·ñÊÇÐèÒªµÄÒ³Ãæ
                            var slctBody = GetCurrentPegViaDriver(tmpDriver, dictPegProperties, ref isOk, ref strError);
                            if (isOk)
                            {
                                this.currentPeg = slctBody;
                                this.marsChromeDriver = (ChromeDriver)tmpDriver;
                                break;
                            }                        
                        }
                    }
                    catch (Exception e)
                    { //
                        logger.Error("WebPegWindow--Ignore this", e.Message, e);
                    }
                }
                if (!isOk)
                {
                    dealResult.ResultMessage = "FAILED";
                    dealResult.Advice = "Please make sure the target Browser has the right Title, and it is loaded by Test Engine.";
                    // no such new browser exists
                    logger.Error("WebPegWindow", strError = $"can't find new browser for |{pegName}|\r\n{dealResult.Advice}");
                    dealResult.ErrorMessage = strError;                    
                    dealResult.ReturnedData = strError;
                }
                else
                {
                    dealResult.ResultMessage = "OK";
                }
                dealResult.ActualInputData = strData;
                dealResult.AckTime = DateTime.Now;
                
                return isOk;
#endif
            }
            else if (string.Compare(CNST_PEG_SWITCH_TO_HOMEBROWSER, strParaMeter, true) == 0)
            {
#if _EnableChrome
                var allWindows = this.marsChromeDriver.WindowHandles;
                if (allWindows.Count == 1)
                {
                    homeWindowsHandle = allWindows[0];
                    /// do nothing
                }   //else
                this.marsChromeDriver.SwitchTo().Window(homeWindowsHandle);
                dealResult.ActualInputData = strData;
                dealResult.AckTime = DateTime.Now;
                dealResult.ResultMessage = "OK";
                return isOk = true;
#endif
            }

            
            try
            {
                this.GetCurrentPeg(dictPegProperties, ref isOk, ref strError);

                dealResult.ActualInputData = strData;
                dealResult.AckTime = DateTime.Now;

                dealResult.ResultMessage = "OK";
                if (string.Compare("ShowObjects", strParaMeter??"", true) == 0)
                {
                    //this.marsWebDriver.ele
                }
                return isOk;
            }
            catch (Exception e)
            {
                dealResult.ResultMessage = "FAILED";
                dealResult.AckTime = DateTime.Now;
                dealResult.ErrorMessage = e.Message;
                dealResult.StackInfo = e.StackTrace;
                dealResult.Advice = "Contact Marsquis";
                dealResult.ReturnedData = e.Message;
                logger.Error("WebPegWindow", strError = e.Message, e);
                return false;
            }
            finally
            {
                logger.logEnd("WebPegWindow");
            }
        }


        internal bool WebMoveMouseOver(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties, string strParaMeter,
            string strData, string strObjType, string strAttachInfo,
            string pegName, string objName, ref string strError, ref MARSDealResult dealResult, bool isMergeMode = false)
        {
            logger.logBegin("WebMoveMouseOver", string.Format("{0}, stepId:{1} (Peg:{2}, obj:{3}, para:{4}, data:{5}), type {6}, attach:[{7}] pegName:[{8}], objName:[{9}]",
                strKeyword, stepId,
                dictPegProperties == null ? "" : string.Join(";", dictPegProperties.Keys),
                dictObjProperties == null ? "" : string.Join(";", dictObjProperties.Keys),
                strParaMeter, strData, strObjType, strAttachInfo, pegName, objName));

            ChromiumDriver chromDriv = GetChromiumDriver();
            if (((!isMergeMode) && string.IsNullOrEmpty(chromDriv.Url)))
            {
                strError = "Not navigate a link yet. MARSKEYWORD_MoveMouseOver should run after a pegwindow and startapplication.";
                return false;
            }
            bool isOk = false;
            if (dealResult == null)
                dealResult = new MARSDealResult();
            dealResult.ActualInputData = strData;
            dealResult.ReturnedData = strAttachInfo;
            bool isSwitchTo = false;
            string strRestoreWindow = "";
            try
            {
                var lstTargetElement = FindObject(dictPegProperties, dictObjProperties, strAttachInfo, strParaMeter, strData, strObjType,
                    ref isOk, ref strError,
                    ref isSwitchTo, ref strRestoreWindow);
                if ((!isOk) || (lstTargetElement == null) || (lstTargetElement.Count <= 0))
                {
                    logger.Error("\t", $"FindObject return false, with Error:[{strError}]");
                    return false;
                }
                var targetElement = lstTargetElement[0];
                logger.Info("WebMoveMouseOver", $"location:{targetElement.Location.X}-{targetElement.Location.Y}, type:{targetElement.GetType()}");

                if (!string.IsNullOrEmpty(strParaMeter))
                {
                    if (MarsThreadForDialogClose.IsThreadToCloseDialog(strParaMeter))
                    {
                        MarsThreadForDialogClose.StartThread();
                        logger.Info("WebMoveMouseOver", "after thread created....");
                    }
                }
                Actions actn = new Actions(chromDriv);
                if (!string.IsNullOrEmpty(strData))
                {

                    object c = chromDriv.ExecuteScript(strData);
                    if (c != null)
                    {
                        logger.Info("\t", $"Executed script and returns [{c.ToString()}]");
                    }
                }
                else
                {
                    actn.MoveToElement(targetElement)
                        .Perform();
                    //actn.Click()
                    //    .Perform();

                    //targetElement.Click();
                }
                
                Thread.Sleep(50);
                System.Threading.Thread.Sleep(50);
                dealResult.ActualInputData = strData;
                dealResult.AckTime = DateTime.Now;

                dealResult.ResultMessage = "OK";
                return true;
            }
            catch (Exception e)
            {
                strError = e.Message;
                dealResult.ResultMessage = "FAILED";
                dealResult.AckTime = DateTime.Now;
                dealResult.ErrorMessage = e.Message;
                dealResult.StackInfo = e.StackTrace;
                dealResult.Advice = "Contact Marsquis";
                dealResult.ReturnedData = e.Message;
                logger.Error("WebMoveMouseOver", e.Message, e);
                return false;
            }
            finally
            {
                if (isSwitchTo)
                {
                    try
                    {
                        chromDriv.SwitchTo().Window(strRestoreWindow);
                    }
                    catch (Exception ee)
                    {
                        logger.Error("\t", ee.Message, ee);
                    }
                }
                logger.logEnd("WebMoveMouseOver");
            }
        }
        private string GetElementIdFromWebElement(IWebElement element)
        {
            string? strId = "";
            if (element == null) return strId;
            strId = element.GetType().GetProperty("Id", BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance)?.GetValue(element)?.ToString();
            return strId==null?string.Empty:strId.ToString();
        }

        internal const string cnst_js_file_getlementbyxpath = "GetElementXPath.js";
        internal List<Mars.webSupport.MARSWebElementXpathInfo>? GetAllTextComboboxXpath(ref bool isOk, ref string strError,
            ref List<MarsWebAGGridColumns> lstAllColumns,
            string[] objectFilters=null)
        {
            logger.logBegin("GetAllTextComboboxXpath");

            string strJsGetXPath = typeof(MARSWebDriver).Assembly.Location;
            strJsGetXPath = System.IO.Path.Combine( System.IO.Path.GetDirectoryName(strJsGetXPath)!, "scripts", cnst_js_file_getlementbyxpath);
            if (!System.IO.File.Exists(strJsGetXPath))
            {
                strError = $"Can't find js file [{cnst_js_file_getlementbyxpath}]";
                logger.Error("GetAllTextComboboxXpath", strError);
                isOk = false;
                return null;
            }
            // ¼ÓÔØ JavaScript ÎÄ¼þ
            var _script = File.ReadAllText(strJsGetXPath);
            ChromiumDriver chromDrivOrg = GetChromiumDriver();
            if (chromDrivOrg == null)
            {
                strError = "Driver is NUll";
                logger.Error("GetAllTextComboboxXpath", strError);
                isOk = false;
                return null;
            }
            List<Mars.webSupport.MARSWebElementXpathInfo>? rslt = new List<MARSWebElementXpathInfo>();

            /// ÏÈ¼ÓÔØ»ùÓÚwebµÄÅäÖÃ
            /// 
            var cfgFile = ObjectEngineConfigFileManagement.GetEngineObject();

            /// ±éÀúÃ¿¸ö´°¿Ú£¬»ñµÃÄÚÈÝ
            /// 
            try
            {
                foreach (var hdl in chromDrivOrg.WindowHandles)
                {
                    var chromDriv = marsChromeDriver.SwitchTo().Window(hdl);
                    logger.Info("GetAllTextComboboxXpath", $"Switch to window|{chromDriv.Url}|");

                    /// ÏÈ»ñµÃcfgFileÖÐ¹ØÓÚwebµÄprescanµÄÄÚÈÝ
                    /// 
                    List<MarsWebAGGridColumns> lstOfColumns = DealWithPreScan(cfgFile, chromDriv, ref isOk, ref strError);
                    if (isOk)
                    {
                        lstAllColumns.AddRange(lstOfColumns);
                    }

                    var inputElements = chromDriv.FindElements(By.XPath("//input"));
                    logger.Info("FindElements(By.XPath(\"//input\")", $"inputElements|{inputElements.ToList().Count}");
                    var buttonElements = chromDriv.FindElements(By.XPath("//button"));
                    var textareaElements = chromDriv.FindElements(By.XPath("//textarea"));
                    bool _saveClass = true;
                    object xpath = "";
                    var jsExecutor = (IJavaScriptExecutor)chromDriv;
                    string strElementType = "";
                    foreach (var element in inputElements)
                    {
                        strElementType = element.GetAttribute("type");

                        //if (t == "text")
                        {
                            xpath = jsExecutor.ExecuteScript(_script + $" return getElementXPath(arguments[0], {_saveClass.ToString().ToLower()});", element);
                            if (xpath != null)
                            {
                                logger.Info("GetAllTextComboboxXpath", $"xpath:{xpath}");

                                rslt.Add(new MARSWebElementXpathInfo()
                                {
                                    webElement = element,
                                    elementId = GetElementIdFromWebElement(element),
                                    marsObjectType = strElementType,
                                    webXpath = xpath.ToString(),
                                    webTag = element.TagName,
                                    webName = element.GetDomProperty("name"),
                                    isDisplayed = element.Displayed,
                                    webId = element.GetDomProperty("id"),
                                    webClassInfo = element.GetDomProperty("class"),
                                });
                            }
                        }

                        logger.Info("GetAllTextComboboxXpath", $"element is input|{strElementType}|{xpath?.ToString()}");
                    }
                    foreach (var element in buttonElements)
                    {
                        strElementType = element.GetAttribute("type");
                        xpath = jsExecutor.ExecuteScript(_script + $" return getElementXPath(arguments[0], {_saveClass.ToString().ToLower()});", element);
                        if (xpath != null)
                        {
                            logger.Info("GetAllTextComboboxXpath", $"xpath:{xpath}|");
                            rslt.Add(new MARSWebElementXpathInfo()
                            {
                                webElement = element,
                                elementId = GetElementIdFromWebElement(element),
                                marsObjectType = strElementType,
                                webXpath = xpath.ToString(),
                                webTag = element.TagName,
                                webName = element.GetDomProperty("name"),
                                isDisplayed = element.Displayed,
                                webId = element.GetDomProperty("id"),
                                webClassInfo = element.GetDomProperty("class"),
                            });
                        }
                    }
                    foreach (var element in textareaElements)
                    {
                        strElementType = element.GetAttribute("type");
                        xpath = jsExecutor.ExecuteScript(_script + $" return getElementXPath(arguments[0], {_saveClass.ToString().ToLower()});", element);
                        if (xpath != null)
                        {
                            logger.Info("GetAllTextComboboxXpath", $"xpath:{xpath}|");
                            rslt.Add(new MARSWebElementXpathInfo()
                            {
                                webElement = element,
                                elementId = GetElementIdFromWebElement(element),
                                marsObjectType = strElementType,
                                webXpath = xpath.ToString(),
                                webTag = element.TagName,
                                webName = element.GetDomProperty("name"),
                                isDisplayed = element.Displayed,
                                webId = element.GetDomProperty("id"),
                                webClassInfo = element.GetDomProperty("class"),
                            });
                        }
                    }
                }
                string strObj = System.Text.Json.JsonSerializer.Serialize(rslt);
                logger.Info("GetAllTextComboboxXpath", $"GetAllTextComboboxXpath|\r\n\t{strObj}");
                return rslt;
            }
            catch (Exception e)
            {
                strError = e.Message;
                logger.Error("GetAllTextComboboxXpath", e.Message, e);
                isOk = false;
                return null;
            }
            finally
            {
                logger.logEnd("GetAllTextComboboxXpath");
            }
       
        }

        private List<MarsWebAGGridColumns> DealWithPreScan(ObjectEngineConfigFile cfgFile, IWebDriver chromDriv, ref bool isOk, ref string strError)
        {
            logger.logBegin("DealWithPreScan", $"cfgFile:{cfgFile.ToString()}");
            if (cfgFile.marsTypeMapping_web_core == null)
            {
                strError = "No web core type mapping is set";
                logger.Error("DealWithPreScan", strError);
                isOk = false;
                return null;
            }
            if ((cfgFile.marsTypeMapping_web_core.PreviewScanType ==null)||
                (cfgFile.marsTypeMapping_web_core.PreviewScanType.Count <= 0))
            {
                strError = "No web core type mapping is set";
                logger.Error("DealWithPreScan", strError);
                isOk = false;
                return null;
            }
            /// ´ÓTypesAndKeywordsÖÐÕÒµ½PreviewScanTypeÖÐµÄËùÓÐ½Úµã             
            var lstOfTypeAndKeywords = cfgFile.marsTypeMapping_web_core.TypesAndKeywords.Where(p =>
                cfgFile.marsTypeMapping_web_core.PreviewScanType.Contains(p.marsType.ToString(), StringComparer.OrdinalIgnoreCase))
                .ToList();
            if ((lstOfTypeAndKeywords == null)||
                (lstOfTypeAndKeywords.Count <= 0))
            {
                strError = "No web core type mapping is set";
                logger.Error("DealWithPreScan", strError);
                isOk = false;
                return null;
            }
            List<MarsWebAGGridColumns> lstOfColumns = new List<MarsWebAGGridColumns>();
            List<string> lstOfElementId = new List<string>();
            foreach (var itm in lstOfTypeAndKeywords)
            {
                if (itm == null) continue;
                if ((itm.webXPaths == null) || (itm.webXPaths.Count <= 0)) continue;
                foreach (var xpath in itm.webXPaths)
                {
                    if (string.IsNullOrEmpty(xpath)) continue;
                    var elements = chromDriv.FindElements(By.XPath(xpath));
                    if ((elements == null) || (elements.Count <= 0)) continue;
                    int idx = 0;
                    MarsWebAGGridColumns col = new MarsWebAGGridColumns();
                    col.marsType = itm.marsType;
                    col.marsXPath = xpath;
                    col.marsHeaderProperty = itm.webColumnHeaderTextProperty;
                    col.marsColumnNames = new List<string>();
                    col.marsKeyword = itm.defaultKeywords.FirstOrDefault();
                    
                    foreach (var element in elements)
                    {
                        idx++;
                        if (element == null) continue;
                        /// ¹ýÂËÖØ¸´µÄ¶ÔÏó
                        string elementId = GetElementIdFromWebElement(element);
                        if (lstOfElementId.Contains(elementId)) continue;
                        lstOfElementId.Add(elementId);
                        col.marsTag = element.TagName;
                        /// find text from elemenet by itm.webColumnHeaderTextProperty
                        /// 
                        string text = element.GetAttribute(itm.webColumnHeaderTextProperty);
                        string textX = element.Text;
                        if ((string.IsNullOrEmpty(textX))&&(string.IsNullOrEmpty(text)))
                        {
                            // Ã»ÓÐheader£¬ignore
                            logger.Info("DealWithPreScan", $"no text header for |{idx}|{xpath}|<{element.TagName}>");
                            continue;
                        }
                        col.marsColumnNames.Add(string.IsNullOrEmpty(textX) ? text : textX);
                    }
                    lstOfColumns.Add(col);
                }
            }
            isOk = true;
            logger.Info("DealWithPreScan", $"total find|{lstOfColumns.Count}");
            return lstOfColumns;
            /// 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strKeyword"></param>
        /// <param name="stepId"></param>
        /// <param name="dictPegProperties"></param>
        /// <param name="dictObjProperties"></param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="strObjType"></param>
        /// <param name="strAttachInfo"></param>
        /// <param name="pegName"></param>
        /// <param name="objName"></param>
        /// <param name="strError"></param>
        /// <param name="dealResult"></param>
        /// <param name="isMergeMode"></param>
        /// <returns></returns>
        internal bool WebClickAt(string strKeyword, long stepId, 
            Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties, 
            string strParaMeter,
            string strData, string strObjType, string strAttachInfo,
            string pegName, string objName, ref string strError, ref MARSDealResult dealResult,
            bool isMergeMode = false
            )
        {
            logger.logBegin("WebClickAt", string.Format("{0}, stepId:{1} (Peg:{2}, obj:{3}, para:{4}, data:{5}), type {6}, attach:[{7}] pegName:[{8}], objName:[{9}]",
                strKeyword, stepId,
                dictPegProperties == null ? "" : string.Join(";", dictPegProperties.Keys),
                dictObjProperties == null ? "" : string.Join(";", dictObjProperties.Keys),
                strParaMeter, strData, strObjType, strAttachInfo, pegName, objName));

            ChromiumDriver chromDriv = GetChromiumDriver();
            if (((!isMergeMode) && string.IsNullOrEmpty(chromDriv.Url)))

            {
                strError = "Not navigate a link yet. clickbutton should run after a pegwindow and startapplication.";
                return false;
            }
            bool isHighLight = false;
            strParaMeter = MarsWEBHighlighter.CheckHightLightSettings(strParaMeter, ref isHighLight);

            bool isOk = false;
            if (dealResult == null)
                dealResult = new MARSDealResult();
            dealResult.ActualInputData = strData;
            dealResult.ReturnedData = strAttachInfo;
            bool isSwitchTo = false;
            string strRestoreWindow = "";
            try
            {
                var lstTargetElement = FindObject(dictPegProperties, dictObjProperties, strAttachInfo, strParaMeter, strData, strObjType,
                    ref isOk, ref strError,
                    ref isSwitchTo, ref strRestoreWindow);
                if ((!isOk) || (lstTargetElement == null) || (lstTargetElement.Count <= 0))
                {
                    logger.Error("\t", $"FindObject return false, with Error:[{strError}]");
                    return false;
                }
                var targetElement = lstTargetElement[0];
                logger.Info("WebClickAt", $"location:{targetElement.Location.X}-{targetElement.Location.Y}, type:{targetElement.GetType()}");

                if (!string.IsNullOrEmpty(strParaMeter))
                {
                    if (MarsThreadForDialogClose.IsThreadToCloseDialog(strParaMeter))
                    {
                        MarsThreadForDialogClose.StartThread();
                        logger.Info("WebClickAt", "after thread created....");
                    }
                }                

                Actions actn = new Actions(chromDriv);

                int offX = 0, offy = 0;
                if (!string.IsNullOrEmpty(strData))
                {

                    if (string.Compare(cnst_clickAt_para_pos_center, strData, true) == 0)
                    {
                        //µã»÷ÖÐ¼ä
                        offX = targetElement.Size.Width / 2;
                        offy = targetElement.Size.Height / 2;
                    }
                    else
                    {
                        string[] arrPos = strData.Split(new char[] { ',', ':' });
                        if (arrPos.Length != 2)
                        {
                            dealResult.Advice = $"please change data format";
                            strError = $"Position should be x,y or x:y format.|{dealResult.Advice}";
                            dealResult.ErrorMessage = strError;
                            dealResult.ResultMessage = "FAILED";
                            dealResult.ReturnedData = strError;
                            dealResult.AckTime = DateTime.Now;
                            logger.Error("WebClickAt", dealResult.ErrorMessage);
                            return false;
                        }
                        if (!(int.TryParse(arrPos[0].Trim(), out offX) && int.TryParse(arrPos[1].Trim(), out offy)))
                        {
                            dealResult.Advice = $"please change data format";
                            strError = $"Position should be x,y or x:y format (x, y should be Integers).|{dealResult.Advice}";
                            dealResult.ErrorMessage = strError;
                            dealResult.ResultMessage = "FAILED";
                            dealResult.ReturnedData = strError;
                            dealResult.AckTime = DateTime.Now;
                            logger.Error("WebClickAt", dealResult.ErrorMessage);
                            return false;
                        }
                        
                    }
                    if (isHighLight)
                        MarsWEBHighlighter.HighlightElement(chromDriv, targetElement);
                    Thread.Sleep(50);
                    //actn.MoveToElement(targetElement, offX, offy)
                    //        .Perform();
                    /// ´¦Àíleft click£¬double clclik
                    /// 
                    int iClickTime = 1, iLeftClickIs0=0 ;
                    if (!string.IsNullOrEmpty(strParaMeter))
                    {
                        if ("LEFT_DBL_CLICK".Equals(strParaMeter, StringComparison.OrdinalIgnoreCase)
                            ||"LEFT_DOUBLE_CLICK".Equals(strParaMeter, StringComparison.OrdinalIgnoreCase)) {
                            iClickTime = 2;
                        }
                        else if ("RIGHT_CLICK".Equals(strParaMeter, StringComparison.OrdinalIgnoreCase))
                        {
                            iLeftClickIs0 = 1;
                        }
                    }
                    if (iLeftClickIs0 == 0)
                    {
                        if (iClickTime == 1)
                        {
                            if ((offX == 0) && (offy == 0)){
                                actn.MoveToElement(targetElement)
                                    .Click()
                                    .Perform();
                            }else
                                actn.MoveToElement(targetElement, offX, offy)
                                    .Click()
                                    .Perform();
                        }
                        else
                        {
                            if ((offX == 0) && (offy == 0))
                                actn.MoveToElement(targetElement)
                                        .DoubleClick()
                                        .Perform();
                            else {
                                 actn.MoveToElement(targetElement, offX, offy)
                                        .DoubleClick()
                                        .Perform();
                                    }
                        }
                    }
                    else
                    {
                        if ((offX == 0) && (offy == 0))
                            actn.MoveToElement(targetElement)
                                    .ContextClick()
                                    .Perform();
                        else
                                    actn.MoveToElement(targetElement, offX, offy)
                                    .ContextClick()
                                    .Perform();
                    }
                }
                else
                {
                    actn.MoveToElement(targetElement)
                        .Click()
                        .Perform();                    
                }
                System.Threading.Thread.Sleep(50);
                dealResult.ActualInputData = strData;
                dealResult.AckTime = DateTime.Now;
                dealResult.ResultMessage = "OK";
                return true;
            }
            catch (Exception e)
            {
                strError = e.Message;
                dealResult.ResultMessage = "FAILED";
                dealResult.AckTime = DateTime.Now;
                dealResult.ErrorMessage = e.Message;
                dealResult.StackInfo = e.StackTrace;
                dealResult.Advice = "Contact Marsquis";
                dealResult.ReturnedData = e.Message;
                logger.Error("WebClickAt", e.Message, e);
                return false;
            }
            finally
            {
                if (isSwitchTo)
                {
                    try
                    {
                        chromDriv.SwitchTo().Window(strRestoreWindow);
                    }
                    catch (Exception ee)
                    {
                        logger.Error("\t", ee.Message, ee);
                    }
                }
                logger.logEnd("WebClickAt");
            }
        }

        internal bool WebClickButton(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties, string strParaMeter,
            string strData, string strObjType, string strAttachInfo,
            string pegName, string objName, ref string strError, ref MARSDealResult dealResult, 
            bool isMergeMode = false
            )
        {
            logger.logBegin("WebClickButton", string.Format("{0}, stepId:{1} (Peg:{2}, obj:{3}, para:{4}, data:{5}), type {6}, attach:[{7}] pegName:[{8}], objName:[{9}]",
                strKeyword, stepId,
                dictPegProperties == null ? "" : string.Join(";", dictPegProperties.Keys),
                dictObjProperties == null ? "" : string.Join(";", dictObjProperties.Keys),
                strParaMeter, strData, strObjType, strAttachInfo, pegName, objName));

            ChromiumDriver chromDriv = GetChromiumDriver();
            if (((!isMergeMode) && string.IsNullOrEmpty(chromDriv.Url)))
            {
                strError = "Not navigate a link yet. clickbutton should run after a pegwindow and startapplication.";
                return false;
            }
            bool isOk = false;
            if (dealResult == null)
                dealResult = new MARSDealResult();
            dealResult.ActualInputData = strData;
            dealResult.ReturnedData = strAttachInfo;
            bool isSwitchTo = false;
            string strRestoreWindow = "";
            try
            {
                var lstTargetElement = FindObject(dictPegProperties, dictObjProperties, strAttachInfo, strParaMeter, strData, strObjType,
                    ref isOk, ref strError,
                    ref isSwitchTo, ref strRestoreWindow);
                if ((!isOk) || (lstTargetElement == null)||(lstTargetElement.Count<=0))
                {
                    if (string.IsNullOrEmpty(strParaMeter) &&(strParaMeter.IndexOf(cnst_NoClickIfNotFind, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        logger.Warnning("WebClickButton", $"no click mode, please ensure the logic is right yourself!!!!!");
                        return true;
                    }
                    logger.Error("\t", $"FindObject return false, with Error:[{strError}]");
                    return false;
                }
                var targetElement = lstTargetElement[0];
                logger.Info("WebClickButton", $"location:{targetElement.Location.X}-{targetElement.Location.Y}, type:{targetElement.GetType()}");
                string strIgnoreConditon = "";
                string strAdv = "", strStack = "";
                if (!string.IsNullOrEmpty(strParaMeter))
                {
                    if (MarsThreadForDialogClose.IsThreadToCloseDialog(strParaMeter))
                    {
                        MarsThreadForDialogClose.StartThread();
                        logger.Info("WebClickButton", "after thread created....");
                    }else if (MarsButtonHelper.IsCondintionClick(strParaMeter, ref strIgnoreConditon, ref strError, ref strAdv, ref strStack))
                    {
                        if (MarsButtonHelper.IsToIgnore(targetElement, strIgnoreConditon, chromDriv, ref strError, ref strAdv, ref strStack)){
                            return true;
                        }
                    }
                }

                Actions actn = new Actions(chromDriv);
                if (!string.IsNullOrEmpty(strData))
                {
                    object c = chromDriv.ExecuteScript(strData);
                    if (c != null)
                    {
                        logger.Info("\t", $"Executed script and returns [{c.ToString()}]");
                    }
                }
                else
                {
                    actn.MoveToElement(targetElement)
                        .Perform();
                    actn.Click()
                        .Perform();
                    
                    //targetElement.Click();
                }
                strParaMeter = strParaMeter ?? "";
                if ((string.Compare("double", strParaMeter, true) == 0)
                    ||(string.Compare("activeAndClick", strParaMeter,true)==0))
                {
                    System.Threading.Thread.Sleep(20);
                    if (targetElement.Displayed)
                        targetElement.Click();
                }
                Thread.Sleep(50);                
                System.Threading.Thread.Sleep(50);
                dealResult.ActualInputData = strData;
                dealResult.AckTime = DateTime.Now;

                dealResult.ResultMessage = "OK";
                return true;
            }
            catch (Exception e)
            {
                strError = e.Message;
                dealResult.ResultMessage = "FAILED";
                dealResult.AckTime = DateTime.Now;
                dealResult.ErrorMessage = e.Message;
                dealResult.StackInfo = e.StackTrace;
                dealResult.Advice = "Contact Marsquis";
                dealResult.ReturnedData = e.Message;
                logger.Error("WebClickButton", e.Message, e);
                return false;
            }
            finally
            {
                if (isSwitchTo)
                {
                    try
                    {
                        chromDriv.SwitchTo().Window(strRestoreWindow);
                    }
                    catch (Exception ee)
                    {
                        logger.Error("\t", ee.Message, ee);
                    }
                }
                logger.logEnd("WebClickButton");
            }
        }

        private string GetTmpSnapshotFileName(string attach="")
        {
            string strPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(MARSWebDriver).Assembly.Location), "..\\snapshot\\");
            if (!System.IO.Directory.Exists(strPath))
            {
                System.IO.Directory.CreateDirectory(strPath);
            }
            string d = DateTime.Now.ToString("yyyyMMdd HHmmss fff");
            d = string.Format("MarsSnapShot{0}{1}.jpg", d,attach);
            return strPath = System.IO.Path.Combine(strPath, d);
        }
        //
        internal bool WebSwitchToNewBrowser(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties, string strParaMeter,
            string strData, string strObjType, string strAttachInfo,
            string pegName, string objName, ref string strError, ref MARSDealResult dealResult, bool isMergeMode = false)
        {
            logger.logBegin("WebSwitchToNewBrowser", string.Format("{0}, stepId:{1} (Peg:{2}, obj:{3}, para:{4}, data:{5}), type {6}, attach:[{7}] pegName:[{8}], objName:[{9}]",
                strKeyword, stepId,
                dictPegProperties == null ? "" : string.Join(";", dictPegProperties.Keys),
                dictObjProperties == null ? "" : string.Join(";", dictObjProperties.Keys),
                strParaMeter, strData, strObjType, strAttachInfo, pegName, objName));
            return false;
        }
        internal bool WebSnapShot(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties, string strParaMeter,
            string strData, string strObjType, string strAttachInfo,
            string pegName, string objName, ref string strError, ref MARSDealResult dealResult, bool isMergeMode = false)
        {
            logger.logBegin("WebSnapShot", string.Format("{0}, stepId:{1} (Peg:{2}, obj:{3}, para:{4}, data:{5}), type {6}, attach:[{7}] pegName:[{8}], objName:[{9}]",
                strKeyword, stepId,
                dictPegProperties == null ? "" : string.Join(";", dictPegProperties.Keys),
                dictObjProperties == null ? "" : string.Join(";", dictObjProperties.Keys),
                strParaMeter, strData, strObjType, strAttachInfo, pegName, objName));

            ChromiumDriver chromDriv = GetChromiumDriver();
            if (((!isMergeMode) && string.IsNullOrEmpty(chromDriv.Url)))
            {
                strError = "Not navigate a link yet. clickbutton should run after a pegwindow and startapplication.";
                return false;
            }
            bool isOk = false;
            if (dealResult == null)
                dealResult = new MARSDealResult();
            dealResult.ActualInputData = strData;
            dealResult.ReturnedData = strAttachInfo;
            bool isSwitchTo = false;
            string strRestoreWindow = "";
            try
            {
                if (!string.IsNullOrEmpty(strParaMeter))
                {
                    if (strParaMeter.IndexOf("AllPage", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        logger.Info("WebSnapShot", "all page mode");
                        var wholeScreenShot = chromDriv.GetScreenshot();
                        string strAllFileName = GetTmpSnapshotFileName("_full"), strAllFileName_1 = strAllFileName;
                        wholeScreenShot.SaveAsFile(strAllFileName_1);
                        dealResult.ActualInputData = strData;
                        dealResult.AckTime = DateTime.Now;
                        dealResult.ReturnedData = strAllFileName_1;
                        dealResult.snapshotFilePath = strAllFileName_1;
                        dealResult.ResultMessage = "OK";
                        return true;
                    }
                    else
                    {
                        logger.Info("WebSnapShot", $"Not all page mode|{strParaMeter}|");
                    }
                }
                else
                {
                    logger.Info("WebSnapShot", $"No parameter mode|{strParaMeter}|");
                }

                    var lstTargetElement = FindObject(dictPegProperties, dictObjProperties, strAttachInfo, strParaMeter, strData, strObjType,
                        ref isOk, ref strError,
                        ref isSwitchTo, ref strRestoreWindow);
                if ((!isOk) || (lstTargetElement == null) || (lstTargetElement.Count <= 0))
                {
                    logger.Error("\t", $"FindObject return false, with Error:[{strError}]");
                    return false;
                }                

                var targetElement = lstTargetElement[0];
                logger.Info("WebSnapShot", $"location:{targetElement.Location.X}-{targetElement.Location.Y}, type:{targetElement.GetType()}");
                HightlightObject(targetElement, 3);

                Point point = targetElement.Location;
                point.X -= 3;
                point.Y -= 3;
                int width = targetElement.Size.Width + 6, height = targetElement.Size.Height + 6;

                var screenshot = chromDriv.GetScreenshot();
                string strTmpFileName = GetTmpSnapshotFileName("_full"), strTmpFileName_1= strTmpFileName;
                screenshot.SaveAsFile(strTmpFileName_1);
                System.Drawing.Image image = System.Drawing.Image.FromFile(strTmpFileName_1);
                Bitmap destinationImage = new Bitmap(image.Width+6, image.Height+6);
                Graphics g = Graphics.FromImage(destinationImage);
                g.DrawImage(image,new System.Drawing.Rectangle(0,0, width,height),
                    new System.Drawing.Rectangle(point.X, point.Y,width,height),
                    GraphicsUnit.Pixel);
                strTmpFileName = GetTmpSnapshotFileName();
                destinationImage.Save(strTmpFileName);

                Thread.Sleep(50);
                System.Threading.Thread.Sleep(50);
                dealResult.ActualInputData = strData;
                dealResult.AckTime = DateTime.Now;
                dealResult.ReturnedData = strTmpFileName;
                dealResult.snapshotFilePath = strTmpFileName;
                dealResult.ResultMessage = "OK";
                return true;
            }
            catch (Exception e)
            {
                strError = e.Message;
                dealResult.ResultMessage = "FAILED";
                dealResult.AckTime = DateTime.Now;
                dealResult.ErrorMessage = e.Message;
                dealResult.StackInfo = e.StackTrace;
                dealResult.Advice = "Contact Marsquis";
                dealResult.ReturnedData = e.Message;
                logger.Error("WebSnapShot", e.Message, e);
                return false;
            }
            finally
            {
                if (isSwitchTo)
                {
                    try
                    {
                        chromDriv.SwitchTo().Window(strRestoreWindow);
                    }
                    catch (Exception ee)
                    {
                        logger.Error("\t", ee.Message, ee);
                    }
                }
                logger.logEnd("WebSnapShot");
            }
        }


        internal bool WebCloseWindow(string strKeyword, long stepId, Dictionary<string, string> dictPegProperties, Dictionary<string, string> dictObjProperties, string strParaMeter,
            string strData, string strObjType, string strAttachInfo,
            string pegName, string objName, ref string strError, ref MARSDealResult dealResult,bool isMergeMode = false)
        {
            logger.logBegin("WebCloseWindow", string.Format("{0}, stepId:{1} (Peg:{2}, obj:{3}, para:{4}, data:{5}), type {6}, attach:[{7}] pegName:[{8}], objName:[{9}]",
                strKeyword, stepId,
                dictPegProperties == null ? "" : string.Join(";", dictPegProperties.Keys),
                dictObjProperties == null ? "" : string.Join(";", dictObjProperties.Keys),
                strParaMeter, strData, strObjType, strAttachInfo, pegName, objName));

            ChromiumDriver chromDriv = GetChromiumDriver();
            if (((!isMergeMode) && string.IsNullOrEmpty(chromDriv.Url)))
            {
                strError = "Not navigate a link yet. clickbutton should run after a pegwindow and startapplication.";
                return false;
            }
            if (dealResult == null)
                dealResult = new MARSDealResult();
            dealResult.ActualInputData = strData;
            dealResult.ReturnedData = strAttachInfo;

            try
            {
                int iCntBefore = this.marsChromeDriver.WindowHandles.Count;
                logger.Info("WebCloseWindow", $"to close window|{this.marsChromeDriver.CurrentWindowHandle}|");
                chromDriv.Close();
                Thread.Sleep(100);
                int iCntAfter = this.marsChromeDriver.WindowHandles.Count;

                logger.Info("WebCloseWindow", $"windows/tab count before|{iCntBefore}|After|{iCntAfter}");
                dealResult.ActualInputData = strData;
                dealResult.AckTime = DateTime.Now;
                dealResult.ReturnedData = "OK";
                dealResult.ResultMessage = "OK";
                return true;
            }
            catch (Exception e)
            {
                strError = e.Message;
                dealResult.ResultMessage = "FAILED";
                dealResult.AckTime = DateTime.Now;
                dealResult.ErrorMessage = e.Message;
                dealResult.StackInfo = e.StackTrace;
                dealResult.Advice = "Contact Marsquis";
                dealResult.ReturnedData = e.Message;
                logger.Error("WebSnapShot", e.Message, e);
                return false;
            }
            finally
            {
                logger.logEnd("WebSnapShot");
            }
        }

        internal ReadOnlyCollection<IWebElement> GetObjectsByXpath(string xpth)
        {
            ChromiumDriver chromDriv = GetChromiumDriver();
            var lst = chromDriv.FindElements(By.XPath(xpth));
            return lst;
        }

        
    }
}
