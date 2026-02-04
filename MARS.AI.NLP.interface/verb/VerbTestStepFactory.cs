using MARS.AIL.NLP.Inter.AutoData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MARS.AIL.NLP.Inter.verb
{
    internal class VerbTestStepFactory
    {
        public static VerbAction? GetVerbActionByVerb(string strVerb, ref bool isOk, ref string strError, MARSNLP_Industry industry= MARSNLP_Industry._Automation)
        {
            if (industry!= MARSNLP_Industry._Automation)
            {
                isOk = false;
                strError = "Currently, only automation industry is supported";
                return null;
            }
            if (string.IsNullOrEmpty(strVerb))
            {
                isOk = false;
                strError = "no verb is provided";
                return null;
            }
            switch (strVerb.ToLower())
            {
                case MARSNLP_Verb_dictionary_AUTOMATION.cnst_verb_fill:
                    isOk = true;
                    return new FillActionForTestStep(strVerb);
                case MARSNLP_Verb_dictionary_AUTOMATION.cnst_verb_select:
                    isOk = true;
                    return new SelectActionForTestStep(strVerb);
                case MARSNLP_Verb_dictionary_AUTOMATION.cnst_verb_lemme_be:
                    isOk = true;
                    return new ISAreVerbActionForTestStep(strVerb);
                case MARSNLP_Verb_dictionary_AUTOMATION.cnst_verb_create:
                    isOk = true;
                    return new CreateVerbActionForTestStep(strVerb);
                default:
                    isOk = false;
                    strError = $"currently, we can't understand {strVerb}, we will work more hard to understand it";
                    return null;
            }

        }

        
    }
}
