using Mars.message.Inter.MQCenter.interProcess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Inter.MQCenter.MSAASupport.MarsAccessibleKeywords
{
    public static class PegWindowUtilites
    {
        public const string CNST_PARA_CACHE_AS = "CACHE_AS";
        public const string CNST_PARA_RESTORE_FROM= "RESTORE_FROM_CACHE";

        public static bool isCacheAsEnabled(string strPara)
        {
            if (string.IsNullOrEmpty(strPara)) return false;
            if (CNST_PARA_CACHE_AS.Equals(strPara.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }
        private static Dictionary<string, MarsSpiedObjectInfo> CachedPeginwinfoInfo = new Dictionary<string, MarsSpiedObjectInfo>();
        internal static bool addToCache(string strIdx, MarsSpiedObjectInfo pegwindowInfo, ref string strError)
        {
            if (string.IsNullOrEmpty(strIdx))
            {
                strError = "Index for cache is null or empty.";
                return false;   
            }
            if (CachedPeginwinfoInfo.ContainsKey(strIdx)) {
                CachedPeginwinfoInfo[strIdx] = pegwindowInfo;
                return true;
            }
            CachedPeginwinfoInfo.Add(strIdx, pegwindowInfo);
            return true;
        }

        internal static bool isToRestore(string strParaMeter)
        {
            /// 从缓存中恢复
            /// 
            if (string.IsNullOrEmpty(strParaMeter)) return false;
            if (CNST_PARA_RESTORE_FROM.Equals(strParaMeter.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        internal static MarsSpiedObjectInfo restoreFromCache(string strData, ref string strError)
        {
            /// 从缓存中恢复指定的pegwindow信息
            /// 
            if (string.IsNullOrEmpty(strData))
            {
                strError = "Index for restore is null or empty.";
                return null;
            }
            if (CachedPeginwinfoInfo.ContainsKey(strData))
            {
                return CachedPeginwinfoInfo[strData];
            }
            strError = $"No cached pegwindow info found for index:{strData}";
            return null;
        }
    }
}
