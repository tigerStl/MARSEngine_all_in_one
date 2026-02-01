using Mars.Utility;
using MarsTestFrame.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.Delegate
{
    #region Ribbon_Test
    public enum E_DISPLAY_TESTRIBBON
    {
        E_HIDDEN_ALL = 0x01,
        E_ENABLE_STORYBOARD_TEST=0x02,
        E_ENABLE_TESTCASE_TEST=0x04
    }
    public enum E_ERROR_CODE_TEST_FRAMEWORK
    {
        E_ERROR_NO_ERROR=0x00,
        E_ERROR_NO_APPLICATIONS=0x300,
        E_ERROR_NOT_ENABLED_APPLICATION_SELECTLIST=0X301,
        E_ERROR_NO_APPLICATION_IS_SELECTED=0x302,
        E_ERROR_WRONG_SELECTED_ITEM_TYPE_PARA_2 = 0x303,
    }
    public delegate void OnMainRibbon_ProjectTestChange(HashSet<E_DISPLAY_TESTRIBBON> displayId);
    public delegate void OnRibbonTestApplicationsReady(MarsKeyValues<int,string>[] arrListApplicationsInfo);
    public delegate int OnRequireRibbonCurrentTestApplication(ref string errorMessage,ref E_ERROR_CODE_TEST_FRAMEWORK errorCode);

    public delegate void OnStoryBoardDetailIdChangeEvent(long? storyboardDetailId);
    public delegate void OnRefreshRequired(string strPropertyName=null);
    
    #endregion //Ribbon_Test
    public class DelegateForMainWindows
    {

    }
}
