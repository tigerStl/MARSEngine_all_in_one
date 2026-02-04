using MARS.AI.NLP.Inter.restClient;
using MARS.AIL.NLP.Inter.AutoData;
using MARS.AIL.NLP.Inter.AutoSteps;
using MARS.AIL.NLP.Inter.restClient.communiteData;
using MARS.AIL.NLP.Inter.utilities.log;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARS.AIL.NLP.Inter.verb
{
    internal class FillActionForTestStep:VerbAction
    {
        private static NLog.Logger log = LogMgr.getLogByType(typeof(FillActionForTestStep));
        public FillActionForTestStep(string strVerb ) : base(strVerb)
        {

        }
        /// <summary>
        /// 改成应该产生filledit object, para, data 格式
        /// </summary>
        /// <param name="rslt"></param>
        /// <param name="strError"></param>
        /// <param name="isOk"></param>
        /// <returns></returns>
        protected override List<Nlp_TestSteps?> genSteps(AnalystASetence_Response rslt, ref string strError, ref bool isOk)
        {
            Nlp_TestSteps stp = new Nlp_TestSteps();
            stp.keyword = MARS_NLP_steps_Keywords.cnst_keyword_fillEdit;
            /// 这里存在两个问题
            /// 1，如何找到正确的对象
            /// 2，如何确定是非提供data的数据，比如 type USD on payside currency field, or type GPB on SWAP_PAY_CCY
            ///    此外，也可以用fill field payside currency with USD, fill USD to SWAP_PAY_CCY
            ///    
            /// 2024 6-4只处理简单模式
            /// 
            /// vo 模式，应该只有一个root
            /// 
            var rootToken = rslt.tokens.Where(p => MARSNLPConstant.cnst_sentence_dep_root.Equals(p.dep, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (rootToken == null ) {
                strError = "NOT A reconginizable sentence, please try again";
                log.Error($"genSteps\t|{strError}");
                isOk = false;
                return null;
            }
            List<Nlp_TestSteps?> stpLst = new List<Nlp_TestSteps?>();

            ///找到 direct object
            ///
            if (MARSNLPConstant.cnst_sentence_pattern_vopo.Equals(rslt.pattern, StringComparison.OrdinalIgnoreCase)
                ||(MARSNLPConstant.cnst_sentence_pattern_vo.Equals(rslt.pattern, StringComparison.OrdinalIgnoreCase)))
            {
                /// vopo
                /// 
                var objs = rslt.tokens.Where(p => MARSNLPConstant.cnst_sentence_dep_dobj.Equals(p.dep, StringComparison.OrdinalIgnoreCase) ||
                    MARSNLPConstant.cnst_sentence_dep_pobj.Equals(p.dep, StringComparison.OrdinalIgnoreCase)).ToList();
                if ((objs == null) || (objs.Count <= 0))
                {
                    strError = "No dobj or pobj exists from tokens";
                    log.Error($"genSteps\t|{strError}");
                    isOk = false; 
                    return null;
                }

                /// 从直接宾语或者间接宾语确定对象以及
                /// 
                RESTClientToNLPServer rstClnt = new RESTClientToNLPServer();                
                for ( var i = 0; i < objs.Count; i++ )
                {
                    var objInfo = rstClnt.lookupDictionary(objs[i].text.ToLower(), ref strError, ref isOk);
                    if (objInfo == null)
                    {
                        if (objs[i].dep.Equals(MARSNLPConstant.cnst_sentence_dep_pobj, StringComparison.OrdinalIgnoreCase)
                            || (objs[i].dep.Equals(MARSNLPConstant.cnst_sentence_dep_dobj, StringComparison.OrdinalIgnoreCase)))
                        {
                            stp.data = objs[i].text;
                            continue;
                        }
                        else
                        {
                            isOk = false;
                            strError = $"no dictionary is found for|{objs[i].text}";
                            log.Error($"genSteps\t|{strError}");
                            return null;
                        }
                    }
                    DictionaryData? cur_Data = null;
                    if (objInfo.Count > 0)
                    {
                        cur_Data = objInfo[0];
                    }
                    if (cur_Data == null)
                    {
                        strError = $"no dictionary is found for|{objs[i].text}, lookupDictionary returns 0 len array ";
                        strError = $"no dictionary is found for|{objs[i].text}";
                        log.Error($"genSteps\t|{strError}");
                        return null;
                    }
                    if (objs[i].dep.Equals(MARSNLPConstant.cnst_sentence_dep_pobj, StringComparison.OrdinalIgnoreCase))
                    {
                        ///介词宾语，对fill而言，这个应该是数据
                        ///
                        stp.data = objs[i].text;
                    }
                    else
                    {                        
                        stp.objectHappyName = objs[i].text;
                    }
                }                
            }
            return stpLst;
        }
    }
}
