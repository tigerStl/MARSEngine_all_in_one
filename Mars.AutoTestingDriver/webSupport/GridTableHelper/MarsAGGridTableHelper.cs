//using Mars.message.windowsWrapper.SystemUtil;
using MarsEnginer.windowsWrapper.SystemUtil;
using OpenQA.Selenium;
using OpenQA.Selenium.Chromium;
using OpenQA.Selenium.Interactions;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Mars.AutoTestingDriver.webSupport.GridTableHelper
{
    public class MarsAGGridTableHelper
    {
        private static MLogger logger = MLogger.GetLogger(typeof(MarsAGGridTableHelper));
        private ChromiumDriver currentDriver = null;
        public MarsAGGridTableHelper(ChromiumDriver driver)
        {
            currentDriver = driver;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lstTargetObj"></param>
        /// <param name="strObjType">如果是column模式，CNST_WEB_OBJECT_TYPE_AGGRID_COL，否则CNST_WEB_OBJECT_TYPE_AGGRID_ROW</param>
        /// <param name="strParaMeter"></param>
        /// <param name="strData"></param>
        /// <param name="strError"></param>
        /// <param name="strAdv"></param>
        /// <param name="strStack"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        internal bool SearchAndClick(List<IWebElement> lstTargetObj, string strObjType, string strParaMeter, string strData,
            Dictionary<string, string> dictObjProperties,
            ref string strReturnedData, 
            ref string strError, ref string strAdv, ref string strStack)
        {
            logger.logBegin("SearchAndClick", $"object count|{lstTargetObj.Count}|strObjType|{strObjType}|para|{strParaMeter}|data|{strData}");
            bool isOk = false;
            try
            {
                
                var  kIndex = dictObjProperties.Keys.FirstOrDefault(p => p.Equals("index", StringComparison.OrdinalIgnoreCase));
                bool isIndexExist = kIndex != null;

                if (string.IsNullOrEmpty(strData))
                {
                    strAdv = "Please set right data to Data cell";
                    strError = $"Data is empty,{strAdv}";
                    isOk = false;
                    return false;
                }
                int idx = -1;
                string strIdx = "";
                if (!string.IsNullOrEmpty(kIndex))
                {
                    strIdx = dictObjProperties[kIndex];
                    if (!int.TryParse(strIdx, out idx))
                    {
                        strAdv = "please modify object definition that index is a number";
                        strError = $"index of object definition is |{strIdx}|, a int number is required.|{strAdv}";
                        logger.Error("SearchAndClick", strError);
                        return isOk = false;
                    }
                }
                string strTmpReturned = "";
                Actions actn = new Actions(this.currentDriver);
                //if (MARSWebDriver.CNST_WEB_OBJECT_TYPE_AGGRID_COL.Equals(strObjType, StringComparison.OrdinalIgnoreCase))
                {
                    int iXpath = -1;
                    if ((iXpath=strData.IndexOf("Xpath:",StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        string xPath = strData.Substring(iXpath+"Xpath:".Length);
                        /// 二次过滤模式
                        /// 获得//
                        var filteredObjs = lstTargetObj.SelectMany(p => p.FindElements(By.XPath(xPath)))
                                .ToList() ;
                        if ((filteredObjs != null) || (filteredObjs.Count<=0)) {
                            strAdv = "Please check the filter of test step settings.";
                            strError = $"No object is find after apply |{xPath}| from {lstTargetObj.Count} objects. {strAdv}";
                            isOk = false;
                            return false;
                        }
                        if (filteredObjs.Count == 1)
                        {
                            strReturnedData = filteredObjs[0].Text;
                            if (!filteredObjs[0].Displayed)
                            {
                                // 使用 JavaScript 将元素滚动到可视范围内
                                logger.Info("searchAndClick", "try to scroll object to view port");
                                IJavaScriptExecutor js = (IJavaScriptExecutor)currentDriver;
                                js.ExecuteScript("arguments[0].scrollIntoView({behavior: 'smooth', block: 'start'});", filteredObjs[0].Text);
                                System.Threading.Thread.Sleep(500);
                            }

                            actn.MoveToElement(filteredObjs[0])
                                .Click(filteredObjs[0])
                                .Perform();
                            return isOk = true;
                        } 
                        if ((filteredObjs.Count > 0) && (isIndexExist))
                        {
                            if (idx>= filteredObjs.Count)
                            {
                                strAdv = "Please decrease the index value of object definition.";
                                strError = $"object index is |{idx}| but only |{filteredObjs.Count}| after filtered by|{xPath}|. {strAdv}";
                                logger.Error("SearchAndClick", strError);
                                return isOk = false;
                            }
                            logger.Info("SearchAndClick", $"there are |{filteredObjs.Count} objects found");
                            var obj = filteredObjs[idx];
                            strReturnedData = obj.Text;
                            actn.Click(obj)
                                .Perform();
                            return isOk = true;
                        }
                        else
                        {
                            strAdv = "Please make sure only one target object exists in the screen";
                            strError = $"There are |{filteredObjs}| target objecst(s) after applied {xPath}.|{strAdv}";
                            logger.Error("SearchAndClick", strError);
                            return isOk = false;
                        }
                    }
                    else if (!string.IsNullOrEmpty(strData))
                    {
                        ///判断是否是文本对比模式
                        ///
                        for (int i= lstTargetObj.Count-1; i >= 0; i--)
                        {
                            var objTmp = lstTargetObj[i];
                            var t = objTmp.Text;
                            if ((!string.IsNullOrEmpty(t))&&((t.Equals(strData, StringComparison.OrdinalIgnoreCase)||(MarsWindowsAPIsExtend.RegularTest(strData, t)))))
                            {
                                continue;
                            }
                            lstTargetObj.RemoveAt(i);
                        }
                        if (lstTargetObj.Count == 1)
                        {
                            strReturnedData = lstTargetObj[0].Text;
                            actn.Click(lstTargetObj[0])
                                .Perform();
                            return isOk = true;
                        }
                        if ((lstTargetObj.Count > 0) && (isIndexExist))
                        {
                            if (idx >= lstTargetObj.Count)
                            {
                                strAdv = "Please decrease the index value of object definition.";
                                strError = $"object index is |{idx}| but only |{lstTargetObj.Count}| after filtered by|Text {strData}|. {strAdv}";
                                logger.Error("SearchAndClick", strError);
                                return isOk = false;
                            }
                            var obj = lstTargetObj[idx];
                            strReturnedData = obj.Text;
                            actn.Click(obj).Perform();
                            return isOk = true;
                        }
                        else
                        {
                            strAdv = "Please make sure only one target object exists in the screen";
                            strError = $"There are |{lstTargetObj}| target objecst(s) after applied text |{strData}|.|{strAdv}";
                            logger.Error("SearchAndClick", strError);
                            return isOk = false;
                        }
                    }
                    else
                    {
                        strAdv = $"Please modify object's definition";
                        strError = $"Only xpath and text are supported,but data cell is |{strData}. {strAdv}";
                        logger.Error("SearchAndClick", strError);
                        return isOk = false;
                    }
                }                
            }
            catch(Exception e)
            {
                strError = e.Message;
                strAdv = "Please check object type and data settings to filter";
                strStack = e.StackTrace;
                logger.Error("SearchAndClick", strError, e.Message);
                return isOk = false;
            }
            finally
            {
                logger.logEnd("SearchAndClick", $"returns|{isOk}");
            }
        }
    }
}
