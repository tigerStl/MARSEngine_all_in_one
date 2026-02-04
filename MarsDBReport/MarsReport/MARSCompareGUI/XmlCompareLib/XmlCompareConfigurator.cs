using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XmlCompareLib
{
    public class XmlCompareConfigurator
    {
        public static void Configure()
        {
            XmlKeyFieldConfig.AddKeyList("TRADELIST", "TradeId");
            XmlKeyFieldConfig.AddKeyList("Assets", "PorS,dmAssetId");
            XmlKeyFieldConfig.AddKeyList("Events", "Date,ADate,Type,Ccy,BDate,CDate,EvStyle");
            XmlKeyFieldConfig.AddKeyList("AssignmentDetails", "dmOwnerTable,AssignVersion,AssignDate");
            XmlKeyFieldConfig.AddKeyList("CallableSchedule", "Notice1,Notice2");
            XmlKeyFieldConfig.AddKeyList("Formula", "Date,Formula");
            XmlKeyFieldConfig.AddKeyList("OpEvents", "Date,ADate,Type,Ccy,BDate,CDate,EvStyle");

        }

        public static void Configure(string configFileName)
        {
            XmlKeyFieldConfig.InitContainers();

            AppConfig.Change(configFileName);
            
            // Configure keys for entites
            string entities = System.Configuration.ConfigurationManager.AppSettings["entities"];
            List<string> entityList = entities.Split(',').ToList();
            foreach (string entity in entityList)
            {
                string fields = System.Configuration.ConfigurationManager.AppSettings[entity];
                XmlKeyFieldConfig.AddKeyList(entity, fields);
            }

            // Configure Ignore fields
            string ignoreFields = System.Configuration.ConfigurationManager.AppSettings["IgnoreFields"];
            XmlKeyFieldConfig.IgnoreList = ignoreFields.Split(',').ToList();

            // Configure mappings

            string mapFileName = System.Configuration.ConfigurationManager.AppSettings["MapFileName"];
            DataSet ds = ExcelWrapper.WorkbookToDataSet(mapFileName);
            //DataSet ds = ExcelWrapper.WorkbookToDataSet(@"C:\MDEV\xmlCompareTest\Config\mappings.xlsx");
            XmlKeyFieldConfig.AddMappings(ds);
        }

        public static void SetIgnoreState(bool ignoreFlag)
        {
            if (!ignoreFlag)
                XmlKeyFieldConfig.IgnoreList = new List<string>();
        }
    }
}
