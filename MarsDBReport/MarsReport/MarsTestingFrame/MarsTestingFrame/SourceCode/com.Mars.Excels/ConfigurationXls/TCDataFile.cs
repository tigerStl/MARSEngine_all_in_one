using com.Mars.Constants;
using MarsTestFrame.com.Mars.TestConfigObjects;
using MarsTestFrame.SourceCode.systemUtil;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace MarsTestFrame.SourceCode.com.Mars.Excels.ConfigurationXls
{
    public class TCDataFile : MarsExcelFileBase
    {
        protected const string CNST_DATA_SHEET_NAME = "Sheet1";

        private static MLogger Logger = MLogger.GetLogger(typeof(TCDataFile));

        private List<ConfigObjectBase> mlistTestData = new List<ConfigObjectBase>();

        protected override ERROR_CODE mAlystExcleFile()
        {
            Logger.logBegin("mAlystExcleFile");
            this.isContainHeader = false;
            ERROR_CODE error = this.GetDataTableFromExcelFile();
            if (error != ERROR_CODE._NO_ERROR)
            {
                Logger.Info("mAlystExcleFile", string.Format("GetDataTableFromExcelFile return errorcode[{0:X}]", ERROR_INFO.GET_ERROR_STR(error)));
                return error;
            }
            int iItm = this.CheckTableExists(CNST_DATA_SHEET_NAME);
            if (iItm < 0)
            {
                Logger.Info("mAlystExcleFile", string.Format("CheckTableExists return errorcode[{0:X}], \r\n\terror:[{1}]", ERROR_CODE._BATCH_ERROR_NO_EXECUTE_TABLE, ERROR_INFO.GET_ERROR_STR(ERROR_CODE._BATCH_ERROR_NO_EXECUTE_TABLE)));
                return ERROR_CODE._BATCH_ERROR_NO_EXECUTE_TABLE;
            }
            error = this.GetSpecialTableDataToList(this.mlstCurrentTables[iItm], mlistTestData);
            Logger.logEnd("mAlystExcleFile");
            return ERROR_CODE._NO_ERROR;
        }

        protected override ConfigObjectBase mLoadDataRow2ConfigObj(DataRow objOneRow)
        {
            Logger.logBegin("mLoadDataRow2ConfigObj");
            TestDataObject objTestData = new TestDataObject();
            objTestData.DataObjectName = objOneRow[0].ToString();
            if (string.IsNullOrEmpty(objTestData.DataObjectName))
            {
                return null;
            }
            for (int i = 1; i < objOneRow.Table.Columns.Count; i++)
            {
                objTestData.TestData.Add(objOneRow[i].ToString());
            }
            Logger.logEnd("mLoadDataRow2ConfigObj");
            return objTestData ;
        }
    }
}
