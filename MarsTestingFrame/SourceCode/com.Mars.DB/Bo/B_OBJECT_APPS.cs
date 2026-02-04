using MarsTestFrame.SourceCode.com.Mars.DB.Dto;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace MarsTestFrame.SourceCode.com.Mars.DB.Bo
{
    public class B_OBJECT_APPS:V_OBJECT_APPS 
    {
        private static MLogger logger = MLogger.GetLogger(typeof(B_OBJECT_APPS));

        public const string CNST_APPLICATION_ID = "APPLICATION_ID";
        public const string CNST_OBJECT_ID = "OBJECT_ID";
        public const string CNST_COMMENT = "COMMENT";
        public const string CNST_ENUM_TYPE = "ENUM_TYPE";
        public const string CNST_OBJECT_HAPPY_NAME = "OBJECT_HAPPY_NAME";
        public const string CNST_OBJECT_TYPE = "OBJECT_TYPE";
        public const string CNST_QUICK_ACCESS = "QUICK_ACCESS";
        public const string CNST_TYPE_ID = "TYPE_ID";
        public const string CNST_APP_APPLICATION_ID = "APP_APPLICATION_ID";
        public const string CNST_APP_SHORT_NAME = "APP_SHORT_NAME";
        public const string CNST_APPLICATION_TYPE_ID = "APPLICATION_TYPE_ID";
        public const string CNST_COMMENT_APP = "COMMENT_APP";
        public const string CNST_EXTRAPOPUPMENU = "EXTRAPOPUPMENU";
        public const string CNST_EXTRAREQUIREMENT = "EXTRAREQUIREMENT";
        public const string CNST_PROCESS_IDENTIFIER = "PROCESS_IDENTIFIER";
        public const string CNST_STARTER_COMMAND = "STARTER_COMMAND";
        public const string CNST_STARTER_PATH = "STARTER_PATH";
        public const string CNST_VERSION = "VERSION";
        public const string CNST_TYPE_APPLICATION_TYPE_ID = "TYPE_APPLICATION_TYPE_ID";
        public const string CNST_TYPE_DESCRPTION = "TYPE_DESCRPTION";
        public const string CNST_TYP_TYPE_ID = "TYP_TYPE_ID";
        public const string CNST_TYPE_NAME = "TYPE_NAME";

        public List<V_OBJECT_APPS> GetObjectsFromDataSet(DataSet ds)
        {
            try
            {
                List<V_OBJECT_APPS> lstResult = new List<V_OBJECT_APPS>();
                foreach(DataRow objRow in ds.Tables[0].Rows)
                { 
                    V_OBJECT_APPS objResult = new V_OBJECT_APPS();
                    objResult.APPLICATION_ID = objRow[CNST_APPLICATION_ID] is DBNull?-1:(Int64)objRow[CNST_APPLICATION_ID];
                    objResult.APPLICATION_TYPE_ID = objRow[CNST_APPLICATION_TYPE_ID] is DBNull ? (short)-1 : (short)objRow[CNST_APPLICATION_TYPE_ID];
                    objResult.APP_APPLICATION_ID = objRow[CNST_APPLICATION_ID] is DBNull ? -1 : (Int64)objRow[CNST_APPLICATION_ID];
                    objResult.APP_SHORT_NAME = objRow[CNST_APP_SHORT_NAME] is DBNull? null : (String)objRow[CNST_APP_SHORT_NAME];
                    objResult.COMMENT = objRow[CNST_COMMENT] is DBNull ? null : (string)objRow[CNST_COMMENT];
                    objResult.COMMENT_APP = objRow[CNST_COMMENT_APP] is DBNull ? null : (string)objRow[CNST_COMMENT_APP];
                    objResult.ENUM_TYPE = objRow[CNST_ENUM_TYPE] is DBNull ? null : (string)objRow[CNST_ENUM_TYPE];
                    objResult.EXTRAPOPUPMENU = objRow[CNST_EXTRAPOPUPMENU] is DBNull ? null : (string)objRow[CNST_EXTRAPOPUPMENU];
                    objResult.EXTRAREQUIREMENT = objRow[CNST_EXTRAREQUIREMENT] is DBNull ? null : (string)objRow[CNST_EXTRAREQUIREMENT];
                    objResult.OBJECT_HAPPY_NAME = objRow[CNST_OBJECT_HAPPY_NAME] is DBNull ? null : (string)objRow[CNST_OBJECT_HAPPY_NAME];
                    objResult.OBJECT_ID = objRow[CNST_OBJECT_ID] is DBNull ? -1 : (Int64)objRow[CNST_OBJECT_ID];
                    objResult.OBJECT_TYPE = objRow[CNST_OBJECT_TYPE] is DBNull ? null : (string)objRow[CNST_OBJECT_TYPE];
                    objResult.PROCESS_IDENTIFIER = objRow[CNST_PROCESS_IDENTIFIER] is DBNull ? null : (string)objRow[CNST_PROCESS_IDENTIFIER];
                    objResult.QUICK_ACCESS = objRow[CNST_QUICK_ACCESS] is DBNull ? null : (string)objRow[CNST_QUICK_ACCESS];
                    objResult.STARTER_COMMAND = objRow[CNST_STARTER_COMMAND] is DBNull ? null : (string)objRow[CNST_STARTER_COMMAND];
                    objResult.STARTER_PATH = objRow[CNST_STARTER_PATH] is DBNull ? null : (string)objRow[CNST_STARTER_PATH];
                    objResult.TYPE_ID = objRow[CNST_TYPE_ID] is DBNull ? -1 : (Int64)objRow[CNST_TYPE_ID];
                    objResult.VERSION = objRow[CNST_VERSION] is DBNull ? null : (string)objRow[CNST_VERSION];
                    objResult.TYPE_APPLICATION_TYPE_ID = objRow[CNST_TYPE_APPLICATION_TYPE_ID] is DBNull ? (short)-1 : (short)objRow[CNST_TYPE_APPLICATION_TYPE_ID];
                    objResult.TYPE_DESCRPTION = objRow[CNST_TYPE_DESCRPTION] is DBNull ? null : (string)objRow[CNST_TYPE_DESCRPTION];
                    objResult.TYP_TYPE_ID = objRow[CNST_TYP_TYPE_ID] is DBNull ? -1 : (Int64)objRow[CNST_TYP_TYPE_ID];        
                    objResult.TYPE_NAME = objRow[CNST_TYPE_NAME] is DBNull ? null : (string)objRow[CNST_TYPE_NAME];
                    lstResult.Add(objResult);
                }
                
                logger.Info("GetOneFromDataSet", string.Format("Total {0} objects returns", lstResult.Count));
                return lstResult;
            }
            catch (Exception e)
            {
                logger.Error("GetOneFromDataSet",string.Format("Exception:[{0}] when Get Per row from View V_OBJECT_APPS",e.Message),e );
                return null;           
            }
        }
    }
}
