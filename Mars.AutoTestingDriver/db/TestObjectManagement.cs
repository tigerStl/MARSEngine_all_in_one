extern alias clientWCF;

using clientWCF::Route2NSEx.src.Marquis.systemUtil;
using Mars.AutoTestingDriver.ErrorMessage;
using Mars.message.Business;
using Mars.message.DataLayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Mars.AutoTestingDriver.db
{
    internal class TestObjectManagement
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestObjectManagement));

        private static Dictionary<long, List<B_V_OBJECT_SNAPSHOT>> CachedObjectData = new Dictionary<long, List<B_V_OBJECT_SNAPSHOT>>();
        public static List<B_V_OBJECT_SNAPSHOT> GetObjectInfoByAppId(long lAppId, ref bool isOk, ref string strError, ref string strAdv, ref string strStack,
            bool isShowDialog ,string strDBIdx = MarsEntitiesExtends.cnst_default_dbName)
        {
            Logger.logBegin("GetObjectInfoByAppId", string.Format("App Id:[{0}]", lAppId));
            try
            {
                List<B_V_OBJECT_SNAPSHOT> objB = null;
                if (!CachedObjectData.ContainsKey(lAppId))
                {
                    ///get object information
                    /// 
#if !_forWebClient
                    objB = B_V_OBJECT_SNAPSHOT.GetObjectInfoByAppId(lAppId, ref isOk, ref strError, strDBIdx);
#else
                    objB = (new MarsRESTfulApiClient(strDBIdx)).GetObjectInfoByAppId(lAppId, ref isOk, ref strError);
#endif
                    if (!isOk)
                    {
                        strAdv = "";
                        strStack = MarsErrorStacks.StackTraceDump();
                        MarsErrorMessageBox.MarsErrorMessageBox.ShowMarsError(strError, "N/A", "N/A", "N/A", 
                            strAdv, "N/A", 
                            strStack, 
                            isShowDialog);
                        return null;
                    }
                    CachedObjectData.Add(lAppId, objB);
                }
                isOk = true;
                return objB = CachedObjectData[lAppId];

            }
            catch (Exception e)
            {
                isOk = false;
                Logger.Error("GetObjectInfoByAppId", strError = string.Format("Exception:[{0}]", e.Message));
                strAdv = "";
                StackFrame stck = new StackFrame();
                strStack = e.StackTrace;
                return null;
            }
            finally
            {
                Logger.logEnd("GetObjectInfoByAppId");
            }
        }
    }

}
