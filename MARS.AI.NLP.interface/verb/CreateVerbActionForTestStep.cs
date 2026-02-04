using MARS.AI.NLP.Inter.restClient;
using MARS.AIL.NLP.Inter.AutoData;
using MARS.AIL.NLP.Inter.AutoSteps;
using MARS.AIL.NLP.Inter.lang;
using MARS.AIL.NLP.Inter.restClient.communiteData;
using MARS.AIL.NLP.Inter.utilities.log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARS.AIL.NLP.Inter.verb
{
    internal class CreateVerbActionForTestStep : VerbAction
    {
        private static NLog.Logger log = LogMgr.getLogByType(typeof(CreateVerbActionForTestStep));
        public CreateVerbActionForTestStep(string strVerb) : base(strVerb)
        {
        }
        /// <summary>
        /// 对Create而言，属于常用词，因此有两种模式，一种是词组模式，一种是词模式。词组通常是动宾，比如create
        /// 什么交易。所以，需要查询
        /// </summary>
        /// <param name="rslt"></param>
        /// <param name="strError"></param>
        /// <param name="isOk"></param>
        /// <returns></returns>
        protected override List<Nlp_TestSteps?>? genSteps(AnalystASetence_Response sourceTokens, ref string strError, ref bool isOk)
        {
            log.Info("genSteps\tbegin");
            try
            {
                
                var v = sourceTokens.tokens.FirstOrDefault(p => MARSNLP_Verb_dictionary_AUTOMATION.cnst_verb_create.Equals(p.lemma, StringComparison.OrdinalIgnoreCase) 
                        && MARSNLPConstant.cnst_sentence_pos_verb.Equals(p.pos, StringComparison.OrdinalIgnoreCase));
                /// 寻找宾语。可能宾语是组合词
                /// 对于常用动词，如create, start, 建议使用动宾词组作为辞典
                string strDirectObject = "";
                var direct_compound_object = sourceTokens.tokens
                    .FirstOrDefault(p => (p.idx > v.idx)
                                && (p.head.Equals(v.text)));


                if (direct_compound_object == null)
                {
                    /// 这里应该直接宾语，like vo
                    /// -----require more work
                    strError = Resource.NLP_NO_OBJECT;
                    isOk = false;
                    log.Error($"buildVOStarts\t{strError}");
                    return new List<Nlp_TestSteps?>();
                }
                /// create a new word
                /// 
                string combined_word_vo = ((direct_compound_object.compound_words==null)||(string.IsNullOrEmpty(direct_compound_object.compound_words.compound_word))) ? 
                    $"{v.lemma} {direct_compound_object.text}":
                    $"{v.lemma} {direct_compound_object.compound_words.compound_word}";
                combined_word_vo = combined_word_vo.Replace("  "," ").ToLower();
                string direct_object = ((direct_compound_object.compound_words == null) || (string.IsNullOrEmpty(direct_compound_object.compound_words.compound_word))) ?
                    direct_compound_object.text.Replace("  ", " ").ToLower() :
                    direct_compound_object.compound_words.compound_word.Replace("  ", " ").ToLower();
                /// 查字典
                /// 因爲是动宾结构，所以需要查询create等词后面的宾语
                /// 

                RESTClientToNLPServer rstClnt = new RESTClientToNLPServer();
                var objInfo = rstClnt.lookupDictionaries(new string[] { (combined_word_vo).ToLower(), direct_object },
                    ref strError, ref isOk);
                if ((!isOk) || (objInfo == null))
                {
                    log.Error($"genSteps\tError after lookupDictionaries|{strError}");
                    return new List<Nlp_TestSteps?>();
                }
                List<Nlp_TestSteps?> rsltStp = new List<Nlp_TestSteps?>();
                Nlp_TestSteps stp = new Nlp_TestSteps();
                var subobj_FromDictionary = objInfo.Where(p => p.k.Equals(combined_word_vo, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();
                if ((subobj_FromDictionary == null)||(!subobj_FromDictionary.IsMarsObjExists()) )
                {
                    strError = $"{Resource.NLP_NO_WORD_DESCRIPTION}|{combined_word_vo}";
                    log.Error($"genSteps\tError|{strError}");
                    isOk = false;
                    return new List<Nlp_TestSteps?>();
                }
                var obj_o_FromDictionary = objInfo.Where(p => p.k.Equals(direct_object,StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();
                string[] data = new string[] { direct_object };
                if (obj_o_FromDictionary != null)
                {
                    /// 判断是不是mars data
                    /// 
                    var marsDataList = obj_o_FromDictionary.getMarsDataSettings();
                    if ((marsDataList!= null ) &&(marsDataList.Count>0))
                    { 
                        data = marsDataList.Where(p=>!string.IsNullOrEmpty(p.alias))
                            .Select(p=>p.alias??"").ToArray();
                    }
                }
                
                foreach (var itmFromDictionary in subobj_FromDictionary.obj)
                {
                    if (itmFromDictionary == null)
                    {
                        log.Warn($"genSteps\tWARNING|{subobj_FromDictionary.k}|has NULL marsobj setting");
                        continue;
                    }
                    foreach (var marsObj in itmFromDictionary._MARS_OBJ)
                    {
                        Nlp_TestSteps nlp_TestSteps = new Nlp_TestSteps();
                        if (string.IsNullOrEmpty(marsObj.ref_keyword))
                        {
                            nlp_TestSteps.keyword = getKeywordBasedOnType(marsObj.swftype??"");
                        }
                        else
                        {
                            nlp_TestSteps.keyword=marsObj.ref_keyword;
                        }
                        nlp_TestSteps.objectHappyName = itmFromDictionary.alias;
                        foreach (var d in data)
                        {
                            Nlp_TestSteps tmpStp = new Nlp_TestSteps()
                            {
                                keyword = nlp_TestSteps.keyword,
                                objectHappyName = nlp_TestSteps.objectHappyName
                            };
                            tmpStp.data = d;
                            rsltStp.Add(tmpStp);

                        }
                    }
                    
                }
                isOk = true;
                return rsltStp;
            }
            catch(Exception ex)
            {
                strError = Resource.ERROR_NLP_EXCEPTION_WHEN_GEN_STEP;
                log.Error($"genSteps\t|{ex.Message}|\r\n|{ex.StackTrace}");
                isOk = false;
                return new List<Nlp_TestSteps?>();
            }
            finally
            {
                log.Info($"genSteps\tend");
            }
        }
    }
}
