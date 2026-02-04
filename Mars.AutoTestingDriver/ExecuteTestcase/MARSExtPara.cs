
#if _EngineDriver
using MarsEnginer.windowsWrapper.SystemUtil;
#else
using Mars.message.windowsWrapper.SystemUtil;
//using Mars.windowsWrapper.SystemUtil;

#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.ExecuteTestcase
{
    public enum MARSParaPrefixType
    {
        _noPrefix =0x0, 
        _withPrefix_nagetiveTest 
    }

    /// <summary>
    /// 用来处理test step的参数的前后对比，以及处理类型
    /// </summary>
    public class MARSExtPara
    {
        internal const string cnst_paraFix_nagetive = "^MARS_NAGETIVE:";

        public string sourcePara { get; set; }
        public string paraAfterExtract { get; set; }
        public MARSParaPrefixType paraPrefixType { get; set; } = MARSParaPrefixType._noPrefix;

        public static MARSExtPara checkParaType(string strPara)
        {
            if (string.IsNullOrEmpty(strPara))
            {
                return new MARSExtPara()
                {
                    sourcePara = strPara,
                    paraAfterExtract = strPara,
                    paraPrefixType = MARSParaPrefixType._noPrefix
                };
            }
            MARSExtPara rsltObj = new MARSExtPara();
            rsltObj.sourcePara = strPara;
            if (MarsWindowsAPIsExtend.RegularTest(cnst_paraFix_nagetive, strPara)){
                
                rsltObj.paraAfterExtract = strPara.Substring(cnst_paraFix_nagetive.Length-1);
                rsltObj.paraPrefixType = MARSParaPrefixType._withPrefix_nagetiveTest;
            }
            else
            {
                rsltObj.paraPrefixType = MARSParaPrefixType._noPrefix;
                rsltObj.paraAfterExtract = strPara;
            }
            return rsltObj;
        }
    }
}
