using MARS.AI.NLP.Inter.restClient;
using MARS.AIL.NLP.Inter.AutoData;
using MARS.AIL.NLP.Inter.AutoSteps;
using MARS.AIL.NLP.Inter.restClient.communiteData;
using MARS.AIL.NLP.Inter.utilities.log;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARS.AIL.NLP.Inter.verb
{
    internal class ISAreVerbActionForTestStep: VerbAction
    {
        private static NLog.Logger log = LogMgr.getLogByType(typeof(ISAreVerbActionForTestStep));

        public ISAreVerbActionForTestStep(string strVerb) : base(strVerb)
        {
        }
        /// <summary>
        /// 对于be动词而言，存在比较复杂的模式, 比如：Counterparty is XXXXX, 需要对Counterparty和xxxx同时查指定行业和应用的字典， 
        /// 以确定各自可以对应什么对象的内容和名称，
        /// 当前没有对应用，行业进行处理，默认是Summit，因此，作为POC，直接调用相关的lookup
        /// </summary>
        /// <param name="rslt"></param>
        /// <param name="strError"></param>
        /// <param name="isOk"></param>
        /// <returns></returns>
        protected override List<Nlp_TestSteps?> genSteps(AnalystASetence_Response rslt, ref string strError, ref bool isOk)
        {
            log.Info($"genSteps\tBegin");
            try
            {
                /// 类别应该是SBO，需要找到S和O，然后查字典
                /// 获得S 
                ///
                var subject = rslt.tokens.FirstOrDefault(p => MARSNLPConstant.cnst_sentence_dep_nsubj.Equals(p.dep, StringComparison.OrdinalIgnoreCase));
                if (subject == null)
                {
                    strError = Resource.ERROR_NLP_NO_SUBJECT_FOR_IS_ARE;
                    isOk = false;
                    log.Error($"genSteps\tError:|{strError}|\r\n{rslt.text}");
                    return new List<Nlp_TestSteps?>();
                }
                /// 获得O
                /// 
                var obj = rslt.tokens.FirstOrDefault(p => MARSNLPConstant.cnst_sentence_dep_attr.Equals(p.dep));
                if (obj == null)
                {
                    strError = Resource.ERROR_NLP_NO_OBJ_FOR_IS_ARE;
                    isOk = false;
                    log.Error($"genSteps\tError:|{strError}|\r\n{rslt.text}");
                    return new List<Nlp_TestSteps?>();
                }
                /// lookupDictionarys
                /// 
                RESTClientToNLPServer rstClnt = new RESTClientToNLPServer();
                var objInfo = rstClnt.lookupDictionaries(new string[] { (subject.text??"").ToLower(), (obj.text??"").ToLower() },
                    ref strError, ref isOk);
                /// 查询字典后，如果是sbo模式，将返回多个数据，其中一个数据应该对象
                /// 
                if ((!isOk)||(objInfo==null))
                {
                    log.Error($"genSteps\tError after lookupDictionaries|{strError}");
                    return new List<Nlp_TestSteps?>();
                }
                List<Nlp_TestSteps?> rsltStp = new List<Nlp_TestSteps?>();
                Nlp_TestSteps stp = new Nlp_TestSteps();
                /// 这里需要调整，因为是一个step里面的多个词的字典意思
                /// 算法：
                /// 1，判断是否有MARSobject 对象，
                /// 2，如果没有，说明用户输入的信息缺乏对象对应的字典，直接创建一个error
                /// 3, 如果有，决策keyword
                /// 1，判断是否有MARSobject 对象,作为主语
                /// 
                var subobj_FromDictionary = objInfo.Where(p=>p.k.Equals(subject.text,StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if ((subobj_FromDictionary == null) || (subobj_FromDictionary.Count <= 0))
                {
                    strError = $"{Resource.NLP_NO_WORD_DESCRIPTION}|{subject.text}";
                    log.Error($"genSteps\tError|{strError}");
                    isOk = false;
                    return new List<Nlp_TestSteps?>();
                }
                var obj_FromDictionary = objInfo.Where(p=>p.k.Equals(obj.text, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                /// 宾语为空，无所谓
                /// 主语处理
                foreach (var itm in subobj_FromDictionary)
                {
                    if (itm == null) continue;
                    if ((itm.obj == null)||(itm.obj.Count<=0))
                    {
                        log.Warn($"{itm.k}|obj is NULL");
                        continue;
                    }
                    foreach (var itm_obj in itm.obj)
                    {
                        if (itm_obj == null) continue;
                        if ((itm_obj._MARS_OBJ==null)||(itm_obj._MARS_OBJ.Count<=0))
                            continue;
                        string keyword = "";
                        foreach(var itm_obj_mars in itm_obj._MARS_OBJ)
                        {
                            if (itm_obj_mars == null) continue;

                            Nlp_TestSteps itm_stp = new Nlp_TestSteps();
                            keyword = getDefaultKeywordBasedOnType((itm_obj_mars.swftype ?? "").ToLower());
                            
                            itm_stp.keyword = keyword;
                            itm_stp.objectHappyName = itm_obj.alias;
                            if ((obj_FromDictionary == null) || (obj_FromDictionary.Count <= 0)) {
                                itm_stp.data = MARS_NLP_steps_Keywords.cnst_data_not_set;
                                rsltStp.Add(itm_stp);
                            }
                            else
                            {
                                foreach(var sub_from_dictionary_itm in obj_FromDictionary)
                                {
                                    if (sub_from_dictionary_itm == null) continue;
                                    Nlp_TestSteps tmpStp = new Nlp_TestSteps();
                                    tmpStp.keyword = keyword;
                                    tmpStp.objectHappyName = itm_obj.alias;
                                    tmpStp.data = obj.text;
                                    rsltStp.Add(tmpStp);
                                }
                            }
                        }
                    }
                }

                isOk = true;
                return rsltStp;
            }
            catch (Exception e)
            {
                strError = $"MARS-AI can't understrand |{rslt.text}| so far, please try to train it";
                log.Error($"genSteps\tException|{strError}\r\n{e.Message}\r\n{e.StackTrace}");
                isOk = false;
                return null;
            }
            finally
            {
                log.Info($"genSteps\tEnd|isOks|{isOk}|{strError}");
            }
        }
    }
}
