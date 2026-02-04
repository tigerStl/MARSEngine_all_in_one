using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTableCompare
{
    public class ToleranceUtil
    {
        private static MLogger logger = MLogger.GetLogger(typeof(ToleranceUtil));
        private static string V2_MARKER = "==";
        public static string DEFAULT_TOLERANCE_TYPE = "A";
        public static string DEFAULT_TOLERANCE_VALUE = "0.0000000001";

        public static Dictionary<string, ToleranceConfig> GenerateTMap(string compareFields, ref bool isOk, ref string error)
        {
            Dictionary<string, ToleranceConfig> dict = new Dictionary<string, ToleranceConfig>();
            if (compareFields.StartsWith(V2_MARKER) == false)
                compareFields = ConverToV2(compareFields);

            string initialValues = compareFields.TrimStart('=').TrimStart('=');

            string[] rows = initialValues.Split(';');
            if (rows == null || rows.Length == 0)
            {
                isOk = false;
                error = $"compare fields format is wrong|{compareFields}";
                logger.Error("GenerateTMap", error);
                return null;
            }
            foreach (string row in rows)
            {
                if (!string.IsNullOrWhiteSpace(row))
                {
                string[] values = row.Split('|');

                string fieldName = values[0];
                string type = values[2];
                string value = values[3];
                
                if (type == "")
                {
                    type = DEFAULT_TOLERANCE_TYPE;
                    value = DEFAULT_TOLERANCE_VALUE;
                }
                
                ToleranceConfig tc = new ToleranceConfig();
                tc.FieldName = fieldName;
                tc.CompareType = type;
                tc.ToleranceValue = 0;

                double v;
                if (double.TryParse(value, out v))
                {
                    tc.ToleranceValue = v;
                }

                    try
                    {
                    dict.Add(fieldName, tc);
                }
                catch (Exception ex)
                {
                        logger.Error("GenerateTMap", ex.Message, ex);
                    Console.Write(ex.ToString());
                }
            }
            }
            return dict;
        }

        private static string ConverToV2(string compareFields)
        {
            string retStr = "";
            string[] strings = compareFields.Split(',');
            foreach(string str in strings)
            {
                retStr += str.Trim() + "|1||;";
            }

            retStr = retStr.TrimEnd(';');
            return retStr;
        }

        public static string ExtractFields(string compareFields)
        {
            string returnString = "";
            string workString = "";
            if (!string.IsNullOrWhiteSpace(compareFields) && compareFields.StartsWith(V2_MARKER))
            {
                int index = compareFields.IndexOf(V2_MARKER);
                workString = (index < 0)
                    ? compareFields
                    : compareFields.Remove(index, V2_MARKER.Length);
                string[] rows = workString.Split(';');


                foreach (string row in rows)
                {
                    string[] values = row.Split('|');

                    string fieldName = values[0];

                    returnString += fieldName + ", ";
                }
                returnString = returnString.TrimEnd(' ').TrimEnd(',');
            }
            else
                returnString = compareFields;
            return returnString;
        }

    }
}
