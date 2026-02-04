using Mars.message.AutoTestingDriver.interProcess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace Mars.Inter.MQCenter.MSAASupport.MarsAccessibleKeywords
{
    class CaptureValueLabelOrStaticHelper : MarsUICapturevalueBase
    {
        internal static bool CaptureValueLabel(string keywordName, AutomationElement targetElement, string pegName, string objName,
            Dictionary<string, string> dictPegProperties,
            Dictionary<string, string> dictObjProperties,
            string strParaMeter, string strData,
            ref string strError, ref MARSDealResult dealResult)
        {
            return CaptureValue(keywordName, targetElement,
                pegName, objName, dictPegProperties, dictObjProperties,
                strParaMeter, strData, ref strError, ref dealResult);
        }
    }
}
