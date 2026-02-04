using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.TestFramework.DataCompare.DataCompareBatch
{
    public class DataCompareBatch
    {
        private string batchConfigFileName;
        bool runAll;
        DataCompareBatchConfig config;
        public DataCompareBatch(string batchConfigFileName, bool runAll)
        {
            this.batchConfigFileName = batchConfigFileName;
            this.runAll = runAll;
            config = new DataCompareBatchConfig(batchConfigFileName);
        }

        public void Run()
        {
            foreach (DataCompareBatchConfigItem item in config.itemList)
            {
                item.Comment = "Fake run";
            }

            config.UpdateConfigFile();
        }

        public void Run(MarsCompare mc)
        {
            foreach (DataCompareBatchConfigItem item in config.itemList)
            {
                if (item.isEmpty)
                    continue;

                bool file1IsRequired = true;
                bool file2IsRequired = true;

                bool compareConfigurationFound = mc.GetFileRequirements(item.CompareConfigID, out file1IsRequired, out file2IsRequired);

                if (runAll == true || 
                    item.Action.ToLower().Equals("run") ||
                    item.Action.ToLower().Equals("execute"))
                {
                    DataCompareError error = new DataCompareError();

                    if (file1IsRequired && File.Exists(item.File1) == false)
                    {
                        error.Message = "File1 not found.  "; 
                    }

                    if (file2IsRequired && File.Exists(item.File2) == false)
                    {
                        error.Message += "File2 not found.  ";
                    }

                    if (error.Message.Contains("not found"))
                    {
                        error.Status = false;
                        item.Status = "" + false;
                        item.Comment = error.Message;
                        continue;
                    }

                    mc.RunCompare(item.CompareConfigID,
                                item.File1,
                                item.File2,
                                item.OutputFile,
                                ref error);

                    item.Status = "" + error.Status;
                    item.Comment = error.Message;

                    if (error.Status == true &&
                        item.Action.ToLower().Equals("run"))
                        item.Action = "DONE";

                }
            }
            config.UpdateConfigFile();
        }
    }
}
