using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MARS.TEMP
{
    class ResultDataRow
    {
        Dictionary<string, ResultCell> rowData = new Dictionary<string, ResultCell>();
        public ErrorDescriptor errDescr;
        public List<string> columnNames;

        internal void PopulateData(System.Xml.XmlAttributeCollection attributes, List<string> colNames, string suffix)
        {
            foreach (string colName in colNames)
            {
                //Console.WriteLine("Processing attr:" + colName);

                // Should use fieldMapper here
                string dataItem = "";
                if (attributes[colName] != null)
                    dataItem = attributes[colName].InnerText;
                else
                    Console.WriteLine("Attr NOT FOUND: " + colName);

                ResultCell cell;
                if (dataItem != null)
                {
                    cell = new ResultCell(dataItem);
                }
                else
                {
                    cell = new ResultCell("");
                    errDescr = new ErrorDescriptor(ErrorDescriptor.ErrorType.COL_NOT_FOUND);
                    cell.SetErrorDescriptor(errDescr);
                }

                rowData[colName + "_" + suffix] = cell;
            }
        }

        internal void setError(string attr, string suffix, ErrorDescriptor errDesc)
        {
            ResultCell cell = null;
            try 
            {
                cell = rowData[attr + "_" + suffix];
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            
            if (cell != null)
                cell.SetErrorDescriptor(errDesc);
        }

        internal string GetData(string header)
        {
            if (rowData.ContainsKey(header))
            {
                ResultCell cell = rowData[header];
                if (cell != null)
                    return cell.GetData();
                else
                    return "";
            }
            else
                return "";
        }

        internal ErrorDescriptor GetErrorDescr(string header)
        {
            if (rowData.ContainsKey(header))
            {
                ResultCell cell = rowData[header];
                if (cell != null)
                    return cell.GetErrorDescr();
                else
                    return null;
            }
            else
                return null;

        }
    }
}
