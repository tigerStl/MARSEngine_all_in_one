namespace Mars.message.Inter.MQCenter.keywordOperation
{

    class SelectTab
    {
        private const string cnst_prefix_Action = @"Action:[\w|\W]*;";
        private const string cnst_prefix_Ribbon = @"Ribbon:";
        public static bool isAdvancedSelectTabPara(string strPara, ref string strPrefixPart, ref string strNormal)
        {
            if (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest("^" + cnst_prefix_Action, strPara))
            {
                int iPos = strPara.IndexOf(";");
                strPrefixPart = strPara.Substring(0, iPos);
                strNormal = strPara.Substring(iPos + 1);
                return true;
            }
            return false;
        }
        public static bool isRibbonMode(string strPara, ref string strNoRibbonInfo)
        {
            if (windowsWrapper.SystemUtil.MarsWindowsAPIsExtend.RegularTest("^" + cnst_prefix_Ribbon, strPara))
            {
                int iPos = strPara.IndexOf(cnst_prefix_Ribbon);
                strNoRibbonInfo = strPara.Substring(iPos + cnst_prefix_Ribbon.Length);
                return true;
            }
            return false;
        }
    }
}
