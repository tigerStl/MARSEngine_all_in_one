using System;
using System.Collections.Generic;

namespace TestFlowClient.ClientAddins.AddinsMgr
{
    internal sealed class ClientAddinsMgr
    {
        static Dictionary<string, List<AddCheckClass>> supportedAddinsInfo = new Dictionary<string, List<AddCheckClass>>
        {
            {"CaptureAndCompare", new List<AddCheckClass> {
                new AddCheckClass {
                    Keyword = "CaptureAndCompare",
                    SupportedPegAndItsChildren = new Dictionary<string, List<AddinsDataDealMgrClassAgent>> {
                        { "OPICS_ICAP",new List<AddinsDataDealMgrClassAgent>
                            {
                                new AddinsDataDealMgrClassAgent() { ObjectName="ICAP_INTERFACE_TABLE", DealClassFullName = "TestFlowClient.ClientAddins.ApplicationAddins.OpicsAddins.OpicsDataAddins_ListViewForICAP"},
                                new AddinsDataDealMgrClassAgent() { ObjectName="ICAP_PROCESS_LIST", DealClassFullName = "TestFlowClient.ClientAddins.ApplicationAddins.OpicsAddins.OpicsDataAddins_ListViewForICAP"},
                            }
                        }
                    } },

                }
            },
            {"CaptureValue", new List<AddCheckClass> {
                new AddCheckClass {
                    Keyword = "CaptureValue",
                    SupportedPegAndItsChildren = new Dictionary<string, List<AddinsDataDealMgrClassAgent>> {
                        { "OPICS_ICAP",new List<AddinsDataDealMgrClassAgent>
                            {
                                new AddinsDataDealMgrClassAgent() { ObjectName="ICAP_INTERFACE_TABLE", DealClassFullName = "TestFlowClient.ClientAddins.ApplicationAddins.OpicsAddins.OpicsDataAddins_ListViewForICAP"},
                                new AddinsDataDealMgrClassAgent() { ObjectName="ICAP_PROCESS_LIST", DealClassFullName = "TestFlowClient.ClientAddins.ApplicationAddins.OpicsAddins.OpicsDataAddins_ListViewForICAP"},
                            }
                        }
                    } },

                }
            }
        };
        internal static bool checkObjectKeywordsSupported(string strKeyword, string strPegwind, string strObjName)
        {
            return FindAddinsBasedOnObjects(strKeyword, strPegwind, strObjName) != null;
        }

        private static AddinsDataDealMgrClassAgent FindAddinsBasedOnObjects(string strKeyword, string strPegwind, string strObjName)
        {
            AddinsDataDealMgrClassAgent objTargetMgrClass = null;
            foreach (string strKeyItm in supportedAddinsInfo.Keys)
            {
                if (string.Compare(strKeyItm, strKeyword, true) != 0) continue;
                List<AddCheckClass> lstCheckClass = supportedAddinsInfo[strKeyItm];
                foreach (AddCheckClass objItm in lstCheckClass)
                {
                    if (objItm == null) continue;
                    if ((objTargetMgrClass = objItm.IsObjectNeedsThisAddinsClass(strPegwind, strObjName)) == null) continue;
                    return objTargetMgrClass;
                }
            }

            return null;
        }

        internal static bool InvokeDataDealAfter(string strKeyword, string strPegwind, string strObjName, string strRc, string strDataSource1, ref string strDataTarget, ref string strError)
        {
            AddinsDataDealMgrClassAgent objTmp = FindAddinsBasedOnObjects(strKeyword, strPegwind, strObjName);
            if (objTmp == null)
            {
                strDataTarget = strDataSource1;
                return true;
            }
            try
            {
                bool isOk = objTmp.DealData(strKeyword, strPegwind, strObjName, strRc, strDataSource1, ref strDataTarget, ref strError);
                return isOk;
            }
            catch (Exception e)
            {
                strError = string.Format("Exception:[{0}] stackTrace:[{1}]", e.Message, e.StackTrace);
                return false;
            }
        }
    }

    internal class AddCheckClass
    {
        internal string Keyword { get; set; }
        protected Dictionary<string, List<AddinsDataDealMgrClassAgent>> supportedPegAndItsChildren = new Dictionary<string, List<AddinsDataDealMgrClassAgent>>();
        internal Dictionary<string, List<AddinsDataDealMgrClassAgent>> SupportedPegAndItsChildren
        {
            get { return supportedPegAndItsChildren; }
            set
            {
                supportedPegAndItsChildren = value;
            }
        }

        internal AddinsDataDealMgrClassAgent IsObjectNeedsThisAddinsClass(string strPeg, string strObject)
        {
            foreach (string strPegTmp in supportedPegAndItsChildren.Keys)
            {
                if (string.Compare(strPegTmp, strPeg, true) != 0) continue;
                foreach (AddinsDataDealMgrClassAgent strObjTmp in supportedPegAndItsChildren[strPegTmp])
                {
                    if (string.Compare(strObject, strObjTmp.ObjectName, true) == 0) return strObjTmp;
                }
            }
            return null;
        }

        //internal AddCheckClass(string strKeyword, Dictionary<string, List<string>> lstObjInfo)
        //{
        //    Keyword = strKeyword;
        //    this.supportedPegAndItsChildren = lstObjInfo;
        //}

    }

    internal class AddinsDataDealMgrClassAgent
    {
        internal string ObjectName;
        internal string DealClassFullName;
        MarsClientAddins_Base DealInstance = null;

        internal bool DealData(string strKeyword, string strPegwind, string strObjName, string strRc, string strDataSource1, ref string strDataTarget, ref string strError)
        {

            try
            {
                object oInst = Activator.CreateInstance(Type.GetType(DealClassFullName));
                if (DealInstance == null)
                {

                    if (!(oInst is MarsClientAddins_Base))
                    {
                        strError = string.Format("object [{0}] is not MarsClientAddins_Base", DealClassFullName);
                        strDataTarget = strDataSource1;
                        return false;
                    }

                }
                DealInstance = (MarsClientAddins_Base)oInst;
                bool isOk = false;
                strDataTarget = DealInstance.GetDataAddins(strKeyword, strObjName, strRc, strDataSource1, ref isOk, ref strError);
                return isOk;
            }
            catch (Exception e)
            {
                strDataTarget = strDataSource1;
                strError = string.Format("Exceptions:[{0}], stackTrace:[{1}]", e.Message, e.StackTrace);
                return false;
            }

        }
    }
}
