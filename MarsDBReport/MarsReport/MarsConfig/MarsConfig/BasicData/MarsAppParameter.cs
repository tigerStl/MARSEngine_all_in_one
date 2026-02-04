using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mars.BasicData
{
    public class MarsAppParameter
    {
        public const string cnst_para_mode          = "-MODE";
        public const string cnst_mode_FromWebComp   = "FromWebComp";
        public const string cnst_para_compareId     = "-CompareId";
        public const string cnst_para_db            = "-DB";//=-Mode FromWebComp -CompareId "xxxxx" -DB GEN_MARS_5
        public const string cnst_para_mars_home     = "-MARSHOME";
        public const string cnst_para_out_dir       = "-OutPutDir";
        public const string cnst_para_uui           = "-uuid";

        public const string cnst_callback_baseUri_P = "CompareCallBackBaseUri";

        public string appMode { get; set; }

        public string appDb { get; set; }

        public string appCompareId {
            get;
            set;
        }

        public string uuid
        {
            get;
            set; 
        }

        public string appHomeDirectory { get; set; }

        public string outputDir { get; set; }

        public static MarsAppParameter AnalystCommandLines(string[] strArgs, ref bool isOk, ref string strError)
        {
            var idxMode = Array.FindIndex(strArgs, s => s.Equals(cnst_para_mode, StringComparison.OrdinalIgnoreCase));
            /// not the new mode
            if (idxMode == -1)
            {
                isOk = true;
                return null;
            }

            string strMode = idxMode>=(strArgs.Length-1)?null:strArgs[idxMode];
            if (strMode == null)
            {
                isOk =false;
                strError = "wrong parameter format for mode command";
                return null;
            }
            MarsAppParameter rslt = new MarsAppParameter();
            if (!rslt.analystWebComparePara(strArgs, ref strError))
            {
                isOk = false;
                strError = "wrong parameters format for mode web integrate";
                return null;
            }
            isOk = true;
            return rslt;
        }

        private bool analystWebComparePara(string[] strArgs, ref string strError)
        {
            
            bool isOk = true;
            string tmpPara = getQueryParaAndValue(strArgs, cnst_para_compareId, ref isOk, ref strError);
            if (!isOk)
                return false;
            this.appCompareId = tmpPara;

            tmpPara = getQueryParaAndValue(strArgs, cnst_para_mode, ref isOk, ref strError);
            if (!isOk)
                return false;
            this.appMode = tmpPara;

            tmpPara = getQueryParaAndValue(strArgs, cnst_para_db, ref isOk, ref strError);
            if (!isOk)
                return false;
            this.appDb = tmpPara;

            tmpPara = getQueryParaAndValue(strArgs, cnst_para_mars_home, ref isOk, ref strError);
            if (!isOk)
            {
                isOk = true; // 
                tmpPara = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            }
            this.appHomeDirectory = tmpPara;

            tmpPara = getQueryParaAndValue(strArgs, cnst_para_uui, ref isOk, ref strError);
            if (!isOk)
                return false;
            this.uuid = tmpPara;            

            tmpPara = getQueryParaAndValue(strArgs, cnst_para_out_dir, ref isOk, ref strError);
            this.outputDir = tmpPara;
            setDefaultOutPutDir(); // now, use default output direct \reportCompareOutPut\
            
            return true;
        }

        private bool setDefaultOutPutDir()
        {

            string f = Assembly.GetExecutingAssembly().Location;
            f = System.IO.Path.GetDirectoryName(f);
            f = System.IO.Path.Combine(f, "..\\reportCompareOutPut");
            if (!System.IO.Directory.Exists(f))
            {
                System.IO.Directory.CreateDirectory(f);
            }

            this.outputDir = f;
            
            return true;            
        }

        private string getQueryParaAndValue(string[] args, string strParaIdx,  ref bool isOk, ref string strError)
        {
            var idxDbId = Array.FindIndex(args, s => s.Equals(strParaIdx, StringComparison.OrdinalIgnoreCase));
            if (idxDbId == -1)
            {
                strError = $"wrong parameter format for mode command, can't find |{strParaIdx}|";
                isOk = false;
                return null;
            }
            if (!((idxDbId >= 0) && (idxDbId < (args.Length - 1))))
            {
                strError = $"wrong parameter format for mode command, can't find value for |{strParaIdx}|";
                isOk = false;
            }
            isOk = true;
            return args[idxDbId + 1];
        }
    }
}
