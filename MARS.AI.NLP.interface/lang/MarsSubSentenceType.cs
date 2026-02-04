using MARS.AIL.NLP.Inter.restClient.communiteData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARS.AIL.NLP.Inter.lang
{
    /// <summary>
    /// 该类记录子句的模式和核心的基本信息，包括主谓宾
    /// </summary>
    internal class MarsSubSentenceType
    {
        public string subSentencePatternInShort { get; set; }
        public AnalystASetence_ResponseToken? predictToken { get; set; }
        public AnalystASetence_ResponseToken? subjectToken { get; set; }
        public AnalystASetence_ResponseToken? objectToken {  get; set; }         
    }
}
