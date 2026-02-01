using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Utility
{


    class SystemCommonUtil
    {

        internal static string GetCurrentPathDir()
        {
            DirectoryInfo dir= Directory.GetParent(typeof(SystemCommonUtil).Assembly.Location);
            if (dir == null) return null;
            return dir.ToString();
        }
        
        internal static string CombinePath(string dir1, string dir2)
        {
            return Path.Combine(dir1, dir2);
        }

        internal static string ExtractFieldNameFromParameterForKeyword(string strSrc,ref bool isOk,ref string strError)
        {
            if (strSrc==null)
            {
                isOk = false;
                strError = "String desn't match [null] the format. it should like 'dynamicrows;fieldName;....'";
                return null;
            }
            string[] arrStr = strSrc.Split(';');
            if (arrStr.Length<2)
            {
                isOk = false;
                strError = "String desn't match the format. it should like 'dynamicrows;fieldName;....'";
                return null;
            }
            return arrStr[1];
        }

        internal static string ExtractFieldNameFromParameterGroupModeForKeyword(string strSrc, ref bool isOk, ref string strError)
        {
            strError = @"string dosn't match the format, it should be \S+;\S+:\S+.*-\S+.*";
            if (string.IsNullOrEmpty(strSrc))
            {
                isOk = false;                
                return null;
            }
            string[] arrStr = strSrc.Split(';');
            if (arrStr.Length!=2)
            {
                isOk = false;
                return null;
            }
            string strGroup0 = arrStr[0];
            arrStr = arrStr[1].Split(':');
            if (arrStr.Length!=3)
            {
                isOk = false;
                return null;
            }
            arrStr = arrStr[1].Split('-');
            if (arrStr.Length!=2)
            {
                isOk = false;
                return null;
            }
            isOk = true;
            return string.Format("{0}.{1}", strGroup0,arrStr[0]);
        }
    }
}
