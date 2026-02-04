using com.Mars.Constants;
using Mars.message.AutoTestingDriver.interProcess;
using Mars.message.Business;
using System;

namespace Mars.AutoTestingDriver.ExecuteTestcase.keywordOp.baseInterfaceAndClass
{
    public delegate void KeywordExecuteCallBack(object dataToSendBack, bool isOk, string strError);

    public delegate bool MarsKeywordOperation(long runOrdId, string strParaMeter, string strData,string strApiRunTimeConfig,
        //string strAttchInfo,
        B_V_OBJECT_SNAPSHOT stepObject,
        string strAttachInfo,
        ref string strError,
        ref MARSDealResult dealResult,
        Mars_applicationTyp.MARS_APPTYPE appType= Mars_applicationTyp.MARS_APPTYPE.NORMAL_DESK_APP,
        string strDBIdx="marsentities",
        KeywordExecuteCallBack dataSetBackCallBack=null,
        bool isAttachUIAAHwnd=false);
    public class MarsKeywordBase
    {
        public const string cnst_pegwindow_para_reHost = "ReHostToApp";
        public const string cnst_marsaddins = "UsingMarsAddins;";
        

#if _demo_for_14
        public static DateTime datetimeX = new DateTime(2027, 9, 15);
        private static int CountOfRef = 0;
        public static bool IsInDateTimeX()
        {
            int iOff = new Random().Next(6);
            CountOfRef += iOff;
            if ((DateTime.Now>datetimeX)&&(CountOfRef>=5)) return true;
            return false;
        }
#endif
        /// <summary>
        /// format ReHostToApp:10
        /// </summary>
        /// <param name="strParaWithReHost"></param>
        /// <param name="isOk"></param>
        /// <returns></returns>
        public static int GetWaitForSecondsForRehostToApp(string strParaWithReHost, ref bool isOk)
        {
            if (string.IsNullOrEmpty(strParaWithReHost))
            {
                isOk = true;
                return 0;
            }
            int iIdx = -1;
            if ((iIdx = strParaWithReHost.IndexOf(cnst_pegwindow_para_reHost, StringComparison.OrdinalIgnoreCase)) < 0)
            {
                isOk = true;
                //default value
                return 10;
            }
            if (strParaWithReHost.Length <= (iIdx+ cnst_pegwindow_para_reHost.Length))
            {
                isOk = true;
                return 10;
            }
            string strScnd = strParaWithReHost.Substring(iIdx + cnst_pegwindow_para_reHost.Length+ 1);
            int iWaitForSeconds = 10;
            if (int.TryParse(strScnd.Trim(), out iWaitForSeconds))
            {
                isOk = true;
                return iWaitForSeconds;
            }
            isOk = false;
            return 0;
        }
    }
}
