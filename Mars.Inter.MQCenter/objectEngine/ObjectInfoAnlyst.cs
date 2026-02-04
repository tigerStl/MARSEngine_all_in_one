using Mars.message.Inter.MQCenter.simpleLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Inter.MQCenter.objectEngine
{
    class ObjectInfoAnlyst
    {
        
        internal static Dictionary<string, string> AlystObjectPropertiesFromQtp(string strQuickAccess, ref bool isOk)
        {
            if (string.IsNullOrEmpty(strQuickAccess))
            {
                isOk = false;
                return null;
            }
            string[] arrProperties = strQuickAccess.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, string> dictResult = new Dictionary<string, string>();
            foreach (var itm in arrProperties)
            {
                int iPos = itm.IndexOf(":=");
                if (iPos == -1)
                {
                    isOk = false;
                    return null;
                }
                string strProperty = itm.Substring(0, iPos);
                string strValue = itm.Substring(iPos + ":=".Length);
                dictResult.Add(strProperty, strValue.Trim());
            }
            isOk = true;
            return dictResult;
        }

        internal static bool AlystObjectQuickAccessToPegAndObj(string strPegSource, string strObjSource, ref Dictionary<string, string> PegIdentifier, ref Dictionary<string, string> ObjIdentifier, ref string strError)
        {
            bool isOk = false;
            MarsLoggerSimple.Info("AlystObjectQuickAccessToPegAndObj", string.Format("Peg:[{0}] Obj:[{1}]", strPegSource, strObjSource));

            PegIdentifier = AlystObjectPropertiesFromQtp(strPegSource, ref isOk);
            if (!isOk)
            {
                strError = string.Format("Pegwindows Quick_access format is wrong [{0}]", strPegSource);
                return false;
            }
            ObjIdentifier = null;
            if (string.Compare(strPegSource, strObjSource, true) != 0)
            {
                ObjIdentifier = AlystObjectPropertiesFromQtp(strObjSource, ref isOk);
                if (!isOk)
                {
                    strError = string.Format("Object Quick_access format is wrong [{0}]", strObjSource);
                    return false;
                }
            }

            return true;
        }
    }
}
