using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarsExcelDataProvider
{
    public class ExcelDataHandler
    {
        private string path;
        DataSet ExcelDS;

        public ExcelDataHandler(string path, out string reason)
        {
            reason = "";
            // TODO: Complete member initialization
            this.path = path;
            try
            {
                ExcelDS = ExcelUtilities.ImportExcelToDS(path);
            }
            catch (Exception ex)
            {
                reason = ex.Message;
            }
        }

        public bool SaveAndClose(out string reason)
        {
            reason = "";
            bool success = true;
            try
            {
                ExcelUtilities.ExportDataSetToExcel(ExcelDS, path, true);
            }

            catch (Exception e)
            {
                reason = e.Message;
                success = false;
            }

            return success;
        }

        public bool SetValue(string sheetName, string index, string value, int colNum, out string reason )
        {
            reason = "";
            bool success = true;
            try
            {

                DataTable dt = ExcelDS.Tables[sheetName];
                DataRow[] rows = dt.Select("ObjectName = '" + index + "'");
                DataRow row = rows[0];

                int idx = dt.Rows.IndexOf(row);

                dt.Rows[idx][colNum + 1] = value;

                if (colNum == 1)
                {
                    string firstValue = dt.Rows[idx][colNum].ToString();

                    if (value.Equals(firstValue))
                    {
                        dt.Rows[idx][colNum + 2] = "TRUE";
                    }
                    else
                    {
                        dt.Rows[idx][colNum + 2] = "FALSE";
                    }
                }
            }
            catch (Exception e)
            {
                reason = e.Message;
                success = false;
            }

            return success;
        }

        public bool GetValue(string sheetName, string index, int colNum, out string value, out string reason)
        {
            reason = "";
            value = "";
            bool success = true;
            try
            {
                DataTable dt = ExcelDS.Tables[sheetName];
                DataRow[] rows = dt.Select("ObjectName = '" + index + "'");
                DataRow row = rows[0];

                int idx = dt.Rows.IndexOf(row);

                value = dt.Rows[idx][colNum + 1].ToString();
            }

            catch (Exception e)
            {
                reason = e.Message;
                success = false;
            }

            return success;
        }

        public bool CreateSheet(string p, out string reason)
        {
            reason = "";
            bool success = true;
            try
            {
                DataTable dt = new DataTable();
                dt.TableName = p;
                ExcelDS.Tables.Add(dt);
            }
            catch (Exception e)
            {
                reason = e.Message;
                success = false;
            }

            return success;
        }
    }
}
