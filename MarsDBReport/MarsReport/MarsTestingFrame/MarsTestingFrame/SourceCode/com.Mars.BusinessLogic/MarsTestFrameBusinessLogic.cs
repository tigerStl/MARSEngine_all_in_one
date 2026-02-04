extern alias clientWCF;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.Mars.Constants;
using MarsTestFrame.systemUtil;

using MarsTestFrame.SourceCode.com.Mars.Excels;
using com.Mars.TestFrame.Application;
using com.Mars.Config;
using clientWCF::Route2NSEx.src.Marquis.systemUtil;

namespace MarsTestFrame.SourceCode.com.Mars.BusinessLogic
{
    public class MarsTestFrameBusinessLogic
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsTestFrameBusinessLogic));
        public static IList<MarsKeyValues<string, string>> GetAvailableAppsByProjectIdAndStoryBoardID(long projectId, long storyboardId, ref ERROR_CODE eCde, ref List<MarsKeyValues<string, string>> lstUnavailableApps)
        {
            Logger.Info("GetAvailableAppsByProjectIdAndStoryBoardID",string.Format("ProjectId:[{0}] storyBoardId:[{1}]", projectId, storyboardId));
            List<MarsKeyValues<string, string>> lstResult = DashBoardFactory.RefreshProjectsWithApps(projectId, storyboardId);
            /// Get installed application from configuration file
            /// 
            if (lstResult.Count!=1)
            {
                Logger.Error("GetAvailableAppsByProjectIdAndStoryBoardID", string.Format("Can't get assigned right Projects, storyboard information. One result is required by [{0}] returns.", lstResult.Count));
                eCde = ERROR_CODE._STORYBOARD_ERROR_NOT_UNIQUE_STROYBOARDINFO_1;
                return null;
            }
            List<ConfigTestApplication> lstAvailableApps = TargetApplicationsManagement.CheckInstalledApps(lstResult[0].Children, ref lstUnavailableApps);
            List<MarsKeyValues<string, string>> lstAvailableAppsResult = new List<MarsKeyValues<string, string>>();
            foreach(ConfigTestApplication objApp in lstAvailableApps)
            {
                if (objApp == null) continue;
                foreach(MarsKeyValues<string,string> objAppKV in lstResult[0].Children)
                {
                    if (objAppKV == null) continue;
                    if (string.Compare(objApp.AppName, objAppKV.MValue,true)==0)
                    {
                        lstAvailableAppsResult.Add(objAppKV);
                    }
                }

            }
            Logger.Info("GetAvailableAppsByProjectIdAndStoryBoardID", string.Format("Total [{0}] Available applications found", lstAvailableAppsResult.Count));
            return lstAvailableAppsResult;
        }
    }
}
