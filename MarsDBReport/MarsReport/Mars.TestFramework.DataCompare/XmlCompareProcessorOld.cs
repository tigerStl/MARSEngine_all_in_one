using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Data;
using Route2NSEx.src.Marquis.systemUtil;

namespace Mars.TestFramework.DataCompare
{
    class XmlCompareProcessorOld
    {
        private static MLogger Logger = MLogger.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        DataTable Header = new DataTable();
        DataTable difftablevalues = new DataTable();
        List<string> KF = new List<string>(); //KeyFields
        List<string> KFVal = new List<string>(); //KeyFieldValues
        List<string> CF = new List<string>(); //CompareFields
        List<string> Source1CFVal = new List<string>(); //CompareFieldValuesS1
        List<string> Source2CFVal = new List<string>(); //CompareFieldValuesS2
        XmlDocument xmlfile1;
        XmlDocument xmlfile2;
        //private string xmlFileName1;
        //private string xmlFileName2;
        private XmlCompareConfig xcc;
        XmlDocument document1;
        XmlDocument document2;
        XmlNamespaceManager xmlnsManager1;
        XmlNamespaceManager xmlnsManager2;
        XmlCompareResult result = new XmlCompareResult();
        bool AllOptChecked = false;

        public XmlCompareProcessorOld(XmlDocument xmlfile1, XmlDocument xmlfile2, XmlCompareConfig xcc)
        {
            // TODO: Complete member initialization
            this.xmlfile1 = xmlfile1;
            this.xmlfile2 = xmlfile2;
            this.xcc = xcc;
        }

        /*public XmlCompareProcessor(string xmlFileName1, string xmlFileName2, XmlCompareConfig xcc)
        {
            // TODO: Complete member initialization
            this.xmlFileName1 = xmlFileName1;
            this.xmlFileName2 = xmlFileName2;
            this.xcc = xcc;
        }*/

        public XmlCompareResult ProcessCompare()
        {
            document1 = xmlfile1;
            document2 = xmlfile2;
            /*if (xcc.InputDataType != null && xcc.InputDataType.Equals("CSV"))
            {
                document1 = CsvUtil.CsvToDom(xmlFileName1);
                document2 = CsvUtil.CsvToDom(xmlFileName2);
            }
            else
            {
                document1 = new XmlDocument();
                document1.Load(xmlFileName1);
                document2 = new XmlDocument();
                document2.Load(xmlFileName2);
                
            }*/
            xmlnsManager1 = new XmlNamespaceManager(document1.NameTable);

            xmlnsManager1.AddNamespace("s", "uuid:BDC6E3F0-6DA3-11d1-A2A3-00AA00C14882");
            xmlnsManager1.AddNamespace("dt", "uuid:C2F41010-65B3-11d1-A29F-00AA00C14882");
            xmlnsManager1.AddNamespace("rs", "urn:schemas-microsoft-com:rowset");
            xmlnsManager1.AddNamespace("z", "#RowsetSchema");

            xmlnsManager2 = new XmlNamespaceManager(document1.NameTable);
            xmlnsManager2.AddNamespace("s", "uuid:BDC6E3F0-6DA3-11d1-A2A3-00AA00C14882");
            xmlnsManager2.AddNamespace("dt", "uuid:C2F41010-65B3-11d1-A29F-00AA00C14882");
            xmlnsManager2.AddNamespace("rs", "urn:schemas-microsoft-com:rowset");
            xmlnsManager2.AddNamespace("z", "#RowsetSchema");


            result = CompareDocuments();
            return result;
        }

        private XmlCompareResult CompareDocuments()
        {
            Logger.Info("CompareDocuments", "Begin");

            result.InitHeaders(xcc.ShowFields);
            
            //New code for diff - Defining the datatable and forming a header
            //DataTable Header = new DataTable();
            Header = DiffReport.DefineTable(xcc.KeyFields);

            // look for diff left to right
            foreach (XmlNode targetNode in document1.SelectNodes("//" + xcc.BlockTag, xmlnsManager1))
            {
                XmlNode n = targetNode.SelectSingleNode("./" + xcc.ElementTag, xmlnsManager1);
                string allAttrs = AllAttrString(n);
                Console.WriteLine("***");
                Console.WriteLine(allAttrs);
                Console.WriteLine("***");

                if (xcc.devMode == true)
                    return null;

                foreach (XmlNode rowNode1 in targetNode.SelectNodes("./" + xcc.ElementTag, xmlnsManager1))
                {
                    // modify settings if AllOpt flag is true -- do it just the first time
                    if (!AllOptChecked && xcc.AllOption)
                    {
                        ModifyCompareSettings(rowNode1);
                        result.InitHeaders(xcc.ShowFields);
                        AllOptChecked = true;
                    }


                    MapAttrNames(rowNode1);
                    ResultDataRow resultRow = result.CreateRow();
                    resultRow.PopulateData(rowNode1.Attributes, xcc.ShowFields, "1");

                    XmlNode rowNode2 = GetCorrespondingNode(rowNode1);
                    if (rowNode2 == null)
                    {
                        Console.WriteLine("Node not found");
                        resultRow.errDescr = new ErrorDescriptor(ErrorDescriptor.ErrorType.ROW_NOT_FOUND);
                    }
                    else
                    {
                        resultRow.PopulateData(rowNode2.Attributes, xcc.ShowFields, "2");
                        CompareAllAttributes(rowNode1, rowNode2, resultRow);
                    }
                    //log.Info("Processing node for " + id + "=" + targetNode.SelectSingleNode(id).InnerText);

                }
                Logger.Info("CompareDocuments", "End");
            }

            // look for diff right to left
            foreach (XmlNode targetNode in document2.SelectNodes("//" + xcc.BlockTag, xmlnsManager2))
            {
                foreach (XmlNode rowNode2 in targetNode.SelectNodes("./" + xcc.ElementTag, xmlnsManager2))
                {
                    //Console.WriteLine(rowNode2.OuterXml);


                    XmlNode rowNode1 = GetCorrespondingNodeRev(rowNode2);
                    if (rowNode1 == null)
                    {
                        ResultDataRow resultRow = result.CreateRow();
                        resultRow.PopulateData(rowNode2.Attributes, xcc.ShowFields, "2");
                        Console.WriteLine("GetCorrespondingNodeRev: Node not found");
                        resultRow.errDescr = new ErrorDescriptor(ErrorDescriptor.ErrorType.ROW_NOT_FOUND);
                    }
                }
            }
            return result;
        }


        private string AllAttrString(XmlNode rowNode1)
        {
            string all = "";

            foreach (XmlAttribute attr in rowNode1.Attributes)
            {
                all += attr.Name + ",";
            }

            return all;
        }

        private void ModifyCompareSettings(XmlNode rowNode1)
        {
            List<string> fields = new List<string>();

            foreach (XmlAttribute attr in rowNode1.Attributes)
            {
                if (xcc.ExcludeFields.Contains(attr.Name) == false)
                    fields.Add(xcc.fieldNameMapper.GetMappedValue(attr.Name));
                else
                    Console.WriteLine(attr.Name + "is excluded");
            }

            xcc.KeyFields = fields;
            xcc.CompareFields = fields;
            xcc.ShowFields = fields;

            string showTmp = "";
            foreach (string s in xcc.ShowFields)
                showTmp += s + ", ";
            Console.WriteLine(showTmp);

        }

        private void MapAttrNames(XmlNode rowNode1)
        {

            //foreach (XmlAttribute attr in rowNode1.Attributes)
            for (int i = rowNode1.Attributes.Count - 1; i > 0; i--)
            {
                XmlAttribute attr = rowNode1.Attributes[i];
                if (xcc.fieldNameMapper.map.ContainsKey(attr.Name))
                {
                    string newName = xcc.fieldNameMapper.map[attr.Name];
                    string value = attr.InnerText;
                    rowNode1.Attributes.Remove(attr);
                    XmlElement el = (XmlElement)rowNode1;
                    el.SetAttribute(newName, value);
                }
            }

        }

        private string GetAttrValue(XmlNode rowNode, string attr)
        {
            string value = "";

            if (rowNode != null && rowNode.Attributes[attr] != null)
                value = rowNode.Attributes[attr].InnerText;

            return value;
        }

        private void CompareAllAttributes(XmlNode rowNode1, XmlNode rowNode2, ResultDataRow resultRow)
        {
            
            foreach (string attr in xcc.CompareFields)
            {
                //if (attr.Equals("Funding"))
                //    Console.WriteLine("Funding");

                string attrValue1 = GetAttrValue(rowNode1, attr);
                string attrValue2 = GetAttrValue(rowNode2, attr);

                // Compare values taking into consideration value adjustments
                if (CompareSingleAttribute(attr + "_1", attr + "_2", attrValue1, attrValue2) == false)
                {
                    ErrorDescriptor errDesc = new ErrorDescriptor(ErrorDescriptor.ErrorType.COL_NOT_MATCHING);
                    resultRow.setError(attr, "1", errDesc);
                    resultRow.setError(attr, "2", errDesc);

                    resultRow.errDescr = errDesc;

                    Console.WriteLine("Warning: Attributes are not equal for attr  " + attr);
                    Console.WriteLine("attrValue1 = " + attrValue1);
                    Console.WriteLine("attrValue2 = " + attrValue2);

                    //Code for the diff/summary page
                    
                    //Getting key field values
                    List<string> KFValues = new List<string>();
                    foreach (string item in xcc.KeyFields)
                    {
                        KFValues.Add(GetAttrValue(rowNode1, item));
                    }

                    //Populating the datatable
                    DataTable Diff = new DataTable();
                    Diff = DiffReport.PopulateDTRow(Header, KFValues, attr, attrValue1, attrValue2);

                    //Setting values for other processes
                    difftablevalues = Diff;
                    KF = xcc.KeyFields;
                    KFVal = KFValues;
                    CF = xcc.CompareFields;

                    List<string> CFV1 = new List<string>(); 
                    foreach (string item in xcc.CompareFields)
                    {
                        CFV1.Add(GetAttrValue(rowNode1, item));
                    }

                    List<string> CFV2 = new List<string>();
                    foreach (string item in xcc.CompareFields)
                    {
                        CFV2.Add(GetAttrValue(rowNode2, item));
                    }

                    Source1CFVal = CFV1;
                    Source2CFVal = CFV2;

                }
            }
        }

        public DataTable GetDiffTable()
        {
            return difftablevalues;
        }

        public List<string> GetKeyFields()
        {
            return KF;
        }

        public List<string> GetKeyFieldValues()
        {
            return KFVal;
        }

        public List<string> GetCompareFields()
        {
            return CF;
        }

        public List<string> GetS1CompareFieldValues()
        {
            return Source1CFVal;
        }

        public List<string> GetS2CompareFieldValues()
        {
            return Source2CFVal;
        }

        private bool CompareSingleAttribute(string attrName1, string attrName2, string attrValue1, string attrValue2)
        {
            bool isEqual = false;
            string adjAction1 = xcc.adjustDataMap.GetMappedValue(attrName1);
            string adjAction2 = xcc.adjustDataMap.GetMappedValue(attrName2);

            if (adjAction1.Length == 0 && adjAction2.Length == 0) // no adjutments
                isEqual = attrValue1.Equals(attrValue2);

            else if (adjAction1.Equals("IGNORE") || adjAction2.Equals("IGNORE"))
                isEqual = true;

            else if (adjAction1.Equals("MULT100"))
                isEqual = CompareConvertedFloatValues(attrValue1, attrValue2, 100, 1);
            else if (adjAction2.Equals("MULT100"))
                isEqual = CompareConvertedFloatValues(attrValue1, attrValue2, 100, 2);

            return isEqual;
        }

        private bool CompareConvertedFloatValues(string attrValue1, string attrValue2, int multiplier, int nameNumber)
        {
            bool isEqual = false;
            double val1, val2;

            if (nameNumber == 1)
            {
                val1 = ConvertValue(attrValue1, multiplier);
                val2 = ConvertValue(attrValue2, 1);
            }

            else
            {
                val1 = ConvertValue(attrValue1, 1);
                val2 = ConvertValue(attrValue2, multiplier);
            }



            if (val1 == val2)
                isEqual = true;
            else
            {
                val1 = SetSigFigs(val1, 6);
                val2 = SetSigFigs(val2, 6);
            }

            if (val1 == val2)
                isEqual = true;
            else
                isEqual = false;

            return isEqual;
        }

        public static double SetSigFigs(double d, int digits)
        {
            double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);

            return scale * Math.Round(d / scale, digits);
        }

        private float ConvertValue(string valueStr, int mult)
        {
            return float.Parse(valueStr) * mult;
        }


        private XmlNode GetCorrespondingNode(XmlNode rowNode1)
        {
            XmlNode rowNode2 = null;

            XmlNode tmpNode = document2.SelectSingleNode("//" + xcc.BlockTag, xmlnsManager1);
            string matchingStr = BuildMatchString(rowNode1);
            //Console.WriteLine("matchingStr: ", matchingStr);
            rowNode2 = tmpNode.SelectSingleNode(matchingStr, xmlnsManager2);

            return rowNode2;
        }

        private XmlNode GetCorrespondingNodeRev(XmlNode rowNode1)
        {
            XmlNode rowNode2 = null;

            XmlNode tmpNode = document1.SelectSingleNode("//" + xcc.BlockTag, xmlnsManager1);
            string matchingStr = BuildMatchString(rowNode1);
            rowNode2 = tmpNode.SelectSingleNode(matchingStr, xmlnsManager1);

            return rowNode2;
        }

        private string BuildMatchString(XmlNode rowNode1)
        {
            // ex: field[@Username='{0}' and @UserPassword='{1}']

            string matchStr = xcc.ElementTag + "[";
            foreach (string attrName in xcc.KeyFields)
            {
                XmlAttribute attr = rowNode1.Attributes[attrName];
                string data = " ";
                if (attr != null)
                    data = attr.InnerText;
                matchStr = matchStr + "@" + attrName + "=\"" + data + "\" and ";
            }
            int index = matchStr.LastIndexOf("and");
            matchStr = matchStr.Remove(index);
            matchStr = matchStr + "]";
            //Console.WriteLine(matchStr);
            return matchStr;
        }
    }
}
