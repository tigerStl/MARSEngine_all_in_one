using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.TestFramework.DataCompare
{
    public class XmlKeyFieldConfig
    {
        static Dictionary<string, List<String>> nodeIdKeyDict;
        static List<String> ignoreList;
        static Dictionary<string, Dictionary<string, string>> mappingsDict;
        
        public static void InitContainers()
        {
            nodeIdKeyDict = new Dictionary<string, List<String>>();
            ignoreList = new List<string>();
            mappingsDict = new Dictionary<string,Dictionary<string,string>>();
        }

        public static string GetMappedValue(string fieldName, string oldValue)
        {
            string newValue = oldValue;
            Dictionary<string, string> fieldDict;
            if (mappingsDict.TryGetValue(fieldName, out fieldDict))
            {
                if (fieldDict.TryGetValue(oldValue, out newValue))
                {
                    Console.WriteLine("Mapping found");
                }
                
            }

            return newValue;
        }

        public static List<String> IgnoreList
        {
            get { return XmlKeyFieldConfig.ignoreList; }
            set { XmlKeyFieldConfig.ignoreList = value; }
        }

        public static void AddMappings(DataSet ds)
        {
            foreach (DataTable fieldTable in ds.Tables)
            {
                Dictionary<string, string> fieldDict = new Dictionary<string, string>();
                mappingsDict.Add(fieldTable.TableName, fieldDict);

                foreach (DataRow row in fieldTable.Rows)
                {
                    string str0 = row[0].ToString();
                    string str1 = row[1].ToString();
                    fieldDict.Add(str0, str1);
                    fieldDict.Add(str1, str0);
                }
            }
        }

       
        public static void AddKeyList(string parentName, string keyString)
        {
            List<string> keyList = keyString.Split(',').ToList();
            nodeIdKeyDict.Add(parentName, keyList);
        }

        public static List<string> GetKeyList(string parentName)
        {
            List<string> list = null;

            if (nodeIdKeyDict.TryGetValue(parentName, out list) == false)
            {
                Console.WriteLine("Warning: Entity >" + parentName + "< is NOT configured");
                Console.WriteLine(Environment.StackTrace);
                throw new XmlFileCompareException();
            }
            return list;
            //return nodeIdKeyDict[parentName];
        }
    }
}
