using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.testType
{
    internal class TestTypeParameter_StepsMode
    {
        private const string cnst_stpMode_cmd = "command";
        private const string cnst_stpMode_fromClip = "-FromClipboard";
        public static bool ParseStepsModePara(string[] arrParas)
        {
            //MARSENGINE/Mars.AutoTestingDriver.application?userName=tiger&command=-FromClipboard&storyBoadName=temp&storyBoardId=-1&app=213&guid=a1b06b48-041f-442c-94eb-d7480c57a647&currentDB=GEN_MARS_10	
            if (arrParas == null) return false;
            var x = arrParas.Select((itm, index) => new { itm = itm, index = index });
            var cmd = x.FirstOrDefault(p => p.itm.Equals(cnst_stpMode_cmd, StringComparison.OrdinalIgnoreCase));
            if (cmd == null) return false;
            var cmdData = x.FirstOrDefault( p=>p.index == (cmd.index + 1));
            if (cmdData == null) return false;
            if (!cmdData.Equals(cnst_stpMode_fromClip, StringComparison.OrdinalIgnoreCase)))return false;
            /// 初始化数据
            /// 
        }
    }
}
