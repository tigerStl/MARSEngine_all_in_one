using MARS.AIL.NLP.Inter.AutoData;
using MARS.AIL.NLP.Inter.restClient.communiteData;
using MARS.AIL.NLP.Inter.utilities.log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARS.AIL.NLP.Inter.utilities.data_helper
{
    internal class NLPTokenHelper
    {
        private static NLog.Logger log = LogMgr.getLogByType(typeof(NLPTokenHelper));
        /// <summary>
        /// 获得最后一个宾语，或者最后一个名词，通常用在后面是从句，需要找到从句的主语作为备用
        /// morpheme 词素
        /// </summary>
        /// <param name="morphemeStack"></param>
        /// <param name="isOk"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        internal AnalystASetence_ResponseToken? GetLastOFromStack(Stack<AnalystASetence_ResponseToken> morphemeStack, ref bool isOk, ref string strError)
        {
            log.Info($"GetLastOFromStack\tbegin");
            try
            {

                if (morphemeStack == null)
                {
                    isOk = false;
                    strError = Resource.SYS_PARA_MORPHEMEM_IS_NULL;
                    log.Error($"GetLastOFromStack\t{strError}");
                    return null;
                }
                // 有可能是主语从句
                var lastObject = morphemeStack.LastOrDefault(p => p.pos.Equals(MARSNLPConstant.cnst_sentence_pos_noun, StringComparison.OrdinalIgnoreCase));
                isOk = true;
                return lastObject;
            }
            catch (Exception e )
            {
                isOk = false;
                strError = string.Format(Resource.SYS_EXCEPTION_FROM_GETLASTOFROMSTACK, e.Message);
                log.Error($"GetLastOFromStack\tException|{strError}|{e.Message}");
                return null;
            }
            finally
            {
                log.Info($"GetLastOFromStack\tEnd");
            }

        }
    }
}
