using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Utility.clipboardManagement
{
    internal class ClipboardMgrForStoryboard
    {
        internal static string FormatStoryboardInfo(string[] arrItms)
        {
            if (arrItms == null) return "\r\n";            
            string strRslt = "";
            foreach(var itm in arrItms)
            {
                if (string.IsNullOrEmpty(itm))
                    strRslt = string.Format("{0}\t",strRslt);
                else
                {
                    ///判断是否存在回车键 以及"
                    /// 
                    string strNewItm = itm.Replace("\"", "\"\"");
                    if ((strNewItm.IndexOf("\r")>=0)||(strNewItm.IndexOf("\n")>=0))
                    {
                        strNewItm = string.Format("\"{0}\"", strNewItm);
                    }
                    strRslt = string.Format("{0}\t{1}",strRslt,strNewItm);
                }
            }
            strRslt += "\r\n";
            return strRslt;
        }
    }
}
