using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mars.AutoTestingDriver.ExecuteTestcase.keywordOp.para
{
    internal class ParaCheckMainWait
    {
        private const string cnst_rg = "CheckMain:\\d+(;){0,1}";
        private ParaCheckMainWait() { }
        public int waitTime = 2;

        public static ParaCheckMainWait IsParaForCheckWait(string strPara)
        {
            
            Regex rg = new Regex(cnst_rg);
            try
            {
                var m= rg.Matches(strPara);
                if (m.Count <= 0) return null;
                var v = m[0].Value;
                if (string.IsNullOrEmpty(v)) return null;   
                int iPos = v.LastIndexOf(":");
                string t = v.Substring(iPos + 1);
                if (v[v.Length - 1].Equals(";"))
                {
                    t=t.Remove(t.Length - 1);
                }
                ParaCheckMainWait rslt = new ParaCheckMainWait();
                rslt.waitTime = int.Parse(t);
                return rslt;
            }
            catch(Exception e)
            {
                return null;
            }
        }
    }
}
