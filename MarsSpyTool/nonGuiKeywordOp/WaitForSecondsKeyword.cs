using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarsSpyTool.nonGuiKeywordOp
{
    internal class WaitForSecondsKeyword
    {
        /// <summary>
        /// waitforseconds的操作
        /// </summary>
        /// <param name="strPara"></param>
        /// <param name="strWaitForSecondsCount"></param>
        /// <param name="strError"></param>
        public bool doKeyword(string strPara, string strWaitForSecondsCount, ref string strError) {
            try
            {
                int iSeconds = 10;
                if (!int.TryParse(strWaitForSecondsCount, out iSeconds))
                {
                    iSeconds = 10;
                }
                Thread.Sleep(iSeconds * 1000);
                return true;
            }
            catch (Exception ex) {
                strError = ex.Message;
                return false;
            }
        }
    }
}
