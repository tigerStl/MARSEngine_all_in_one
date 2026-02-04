using Mars.message.AutoTestingDriver.interProcess;
using Mars.message.Inter.MQCenter.keywordOperation;
using Mars.message.Inter.MQCenter.simpleLog;
using Mars.message.Utility.visualObjects.objectSpyer;
using Mars.message.windowsWrapper.SystemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;

namespace Mars.Inter.MQCenter.MSAASupport.MarsAccessibleKeywords
{
    public class CaptureValueEditHelper : MarsUICapturevalueBase
    {
        internal static bool CaptureValueEditor(string keywordName, AutomationElement targetElement,
            string pegName, string objName,
            Dictionary<string, string> dictPegProperties,
            Dictionary<string, string> dictObjProperties,
            string strParaMeter, string strData, ref string strError, ref MARSDealResult dealResult)
        {
            return CaptureValue(keywordName, targetElement,
                pegName, objName, dictPegProperties, dictObjProperties,
                strParaMeter, strData, ref strError, ref dealResult);
            
        }

    }
}
