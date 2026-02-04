using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;
using MessagingStandards.Entities.SWIFT;
using MessagingStandards.SWIFT;

namespace Mars.TestFramework.DataCompare
{
    class SwiftFileToDt
    {
        DataTable dt = new DataTable();
        string InputFileName;
        List<string> HeaderList = new List<string>();

        string headers = "Seq, Row, DealNo, MsgType, Msg";

        public DataTable ConvertToDT(string inputFileName)
        {

            InputFileName = inputFileName;

            string strAll;

            string value = "";

            int seq = 0;
            int row = 0;
            string dealNo = "";
            string msgType = "";
            string msg = "";

            HeaderList = new List<string>(headers.Split(','));
            try
            {
                InitDtColumns();
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            using (StreamReader sr = File.OpenText(inputFileName))
            {
                strAll = sr.ReadToEnd();
            }

            string[] swiftMessages = strAll.Split('$');

            foreach (string str in swiftMessages)
            {
                SwiftMessage message = new SwiftMessage();
                message.ParseSwiftMessage(str.Trim());

                Console.WriteLine("MsgType:" + message.Block2.MessageType);

                msgType = message.Block2.MessageType;
                switch (msgType)
                {
                    case "362":
                        foreach (var block in message.Block4)
                        {
                            if (block.TagName == "20")
                            {
                                value = block.Value;
                                //dealNo = value.Substring(0, 9);
                                dealNo = value;
                                Console.WriteLine("DealNumber:" + dealNo);
                                break;
                            }
                        }
                        break;

                    case "515":
                    case "540":
                    case "541":
                        foreach (var block in message.Block4)
                        {
                            if (block.TagName == "20C")
                            {
                                value = block.Value;
                                dealNo = value.Substring(0, 9);
                                Console.WriteLine("DealNumber:" + dealNo);
                                break;
                            }
                        }
                        break;
                }
                msg = str;
                AddDataToTable(seq, row, dealNo, msgType, msg);
                seq++;
                row += CountLines(str); ;
            }
            return dt;
        }

        private int CountLines(string str)
        {
            return str.Split('\n').Count();
        }

        private void AddDataToTable(int seq, int row, string dealNo,  string msgType, string msg)
        {
            DataRow workRow = dt.NewRow();
            
            workRow["Seq"] = seq;
            workRow["Row"] = row;
            workRow["DealNo"] = dealNo;
            workRow["MsgType"] = msgType;
            workRow["Msg"] = msg;
        
            dt.Rows.Add(workRow);
        }

        private void InitDtColumns()
        {
            foreach (string header in HeaderList)
            {
                dt.Columns.Add(header.Trim(), typeof(String));
            }
        }

    }
}
