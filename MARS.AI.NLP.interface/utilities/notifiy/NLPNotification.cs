using MARS.AIL.NLP.Inter.AutoSteps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARS.AIL.NLP.Inter.utilities.notifiy
{
    public class NLP_TextToAnalyst
    {
        public string query_id { get; set; } /// set by outside application, for caller to locate its thread
        public string currentText { get; set; }
        public override string ToString()
        {
            return currentText;
        }
    }


    /// <summary>
    /// 作为客户端回调的结构。
    /// </summary>
    public class NLP_TextAnalystStatus: NLP_TextToAnalyst
    {
        public bool isLastNotification { get; set; }
        public List<Nlp_TestSteps> generatedStepsList { get; set; } 
        public bool isWithError { get; set; } = false; /// default value is false, until there is error when deal with the 
    }
    public delegate void NLP_AnalystTextCallback(NLP_TextAnalystStatus message);
}
