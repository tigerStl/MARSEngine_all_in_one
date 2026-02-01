using Mars.Dto;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.ViewModel.BaselineDataEditorViewModelSub
{
    public class BaseLineData_ChildItem : INotifyPropertyChanged
    {
        
        #region Notifypropery 
        public event PropertyChangedEventHandler PropertyChanged;
        internal void RaisePropertyChanged(string prop)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        #endregion

        private static MLogger Logger = MLogger.GetLogger(typeof(BaseLineData_ChildItem));

        protected T_BASELINE_DATA_SUMMARYDTO assignedBaselineDataSummary;
        
        protected Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>> assingendBaselineDetails;
        protected List<T_BASELINE_DATA_DETAILDTO> tobeDeletedItems = new List<T_BASELINE_DATA_DETAILDTO>();
        

        public Dictionary<T_BASELINE_DATA_SUMMARYDTO, Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>>> extendObject 
            = new Dictionary<T_BASELINE_DATA_SUMMARYDTO, Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>>>();
        #region /// Record Data to delete
        protected T_BASELINE_DATA_SUMMARYDTO toDelBaselineDataSummary;
        protected Dictionary<T_BASELINE_DATA_SUMMARYDTO, Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>>> toDelExtendObject = new Dictionary<T_BASELINE_DATA_SUMMARYDTO, Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>>>();


        public List<T_BASELINE_DATA_SUMMARYDTO> ErrorSummaryInfo = new List<T_BASELINE_DATA_SUMMARYDTO>();
        #endregion

        public List<T_BASELINE_DATA_DETAILDTO> ExtendObjectsTobeDel
        {
            get
            {
                List<T_BASELINE_DATA_DETAILDTO> lstRslt = new List<T_BASELINE_DATA_DETAILDTO>();
                if (toDelExtendObject==null)
                {
                    return lstRslt;
                }
                foreach(T_BASELINE_DATA_SUMMARYDTO objSumm in toDelExtendObject.Keys)
                {
                    Dictionary < short ?, List<T_BASELINE_DATA_DETAILDTO>> dicLoopRslt = toDelExtendObject[objSumm];
                    foreach (short? s in dicLoopRslt.Keys)
                    {
                        lstRslt.AddRange(dicLoopRslt[s]);
                    }
                }
                return lstRslt;
            }
        }

        public bool ContentChangedFromOutside(string strContent, int iLoop)
        {
            Logger.Info("ContentChangedFromOutside", string.Format("Content to update:[{0}] iLoop:[{1}]", strContent, iLoop));
            //AddOrUpdateObject(iLoop, strContent,)
            string[] arrData = strContent.Split(new string[] { "\r\n" , "\n", "\r" }, StringSplitOptions.None);

            #region for the first object
            /// the first is for assignedBaselineDataSummary
            /// 
            List<T_BASELINE_DATA_DETAILDTO> tmpLst = null;
            if (!assingendBaselineDetails.ContainsKey((short)iLoop))
            {
                assingendBaselineDetails.Add((short)iLoop, new List<T_BASELINE_DATA_DETAILDTO>());
            }
            tmpLst = assingendBaselineDetails[(short)iLoop];
            T_BASELINE_DATA_DETAILDTO objDefaultDto = null;
            if (tmpLst.Count > 0)
            {
                objDefaultDto = tmpLst[0];
            }
            else
            {
                objDefaultDto = new T_BASELINE_DATA_DETAILDTO();
                tmpLst.Add(objDefaultDto);
                objDefaultDto.LOOP_ID = (Int16)iLoop;
            }

            objDefaultDto.DATA_VALUE = arrData[0];
            if (objDefaultDto.DATA_BASE_OBJ_ID<=0)
            {
                objDefaultDto.DATA_BASE_OBJ_ID = assignedBaselineDataSummary.DATA_BASE_OBJ_ID;
            }
            #endregion //for the first object

            #region the rest objects
            if (extendObject == null)
                extendObject = new Dictionary<T_BASELINE_DATA_SUMMARYDTO, Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>>>();

            /// notic: 
            /// it is not necessary to update exist object, just remove the loop objects and recreat a new one? otherwise, untouched objects should be removed from 
            /// list.
            /// 
            List<string> lstSubObjectNames = new List<string>();
            for (int i=1;i<arrData.Length;i++)
            {
                lstSubObjectNames.Add(string.Format("{0}_{1}", assignedBaselineDataSummary.OBJECT_HAPPY_NAME, i));
            }

            List<T_BASELINE_DATA_SUMMARYDTO> keySubTobeDel = new List<T_BASELINE_DATA_SUMMARYDTO>();
            List<T_BASELINE_DATA_SUMMARYDTO> keyToBedelted = extendObject.Keys.Where(p => !lstSubObjectNames.Contains(p.OBJECT_HAPPY_NAME)).ToList();
            
            foreach(T_BASELINE_DATA_SUMMARYDTO itm in keyToBedelted)
            {
                extendObject.Remove(itm);
                /// put to Error List for deleting
                ErrorSummaryInfo.Add(itm);
            }

            for (int i = 1; i < arrData.Length; i++)
            {
                string strObjectName = string.Format("{0}_{1}", assignedBaselineDataSummary.OBJECT_HAPPY_NAME, i);
                T_BASELINE_DATA_SUMMARYDTO getObjFromDtl = extendObject.Keys.FirstOrDefault(p => p.OBJECT_HAPPY_NAME.CompareTo(strObjectName) == 0);
                if (getObjFromDtl == null)
                {
                    getObjFromDtl = new T_BASELINE_DATA_SUMMARYDTO();
                    getObjFromDtl.OBJECT_HAPPY_NAME = strObjectName;
                    getObjFromDtl.DATA_SUMMARY_ID = assignedBaselineDataSummary.DATA_SUMMARY_ID;
                    extendObject.Add(getObjFromDtl, new Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>>());
                }
                Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>> lstDtl = extendObject[getObjFromDtl];
                if (lstDtl == null)
                    extendObject[getObjFromDtl] = lstDtl = new Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>>();

                List<T_BASELINE_DATA_DETAILDTO> lstDtlRest = null;
                if (!lstDtl.ContainsKey((short)iLoop))
                {
                    lstDtlRest = new List<T_BASELINE_DATA_DETAILDTO>();
                    lstDtl.Add((short)iLoop, lstDtlRest);
                }
                else
                {
                    lstDtlRest = lstDtl[(short)iLoop];
                }
                T_BASELINE_DATA_DETAILDTO objDtoRest = lstDtlRest.Count > 0 ? lstDtlRest[0] : new T_BASELINE_DATA_DETAILDTO();
                objDtoRest.DATA_VALUE = arrData[i];
                //if (objDtoRest.LOOP_ID == null )
                //{
                objDtoRest.LOOP_ID = (short)iLoop;
                //}
                objDtoRest.DATA_BASE_OBJ_ID = getObjFromDtl == null ? -1 : getObjFromDtl.DATA_BASE_OBJ_ID;
                if (lstDtlRest.Count <= 0)
                {
                    lstDtlRest.Add(objDtoRest);
                }
                else
                {
                    lstDtlRest[0] = objDtoRest;
                }
                                
                //if (objDtoRest.DATA_BASE_OBJ_ID== assignedBaselineDataSummary==null?)
            }
            #endregion //the rest object


            return true;
        }
        public int getLoopCount()
        {
            return assingendBaselineDetails.Keys.Count;
        }

        public T_BASELINE_DATA_SUMMARYDTO AssignedBaselineDataSummary
        {
            get
            {
                return assignedBaselineDataSummary;
            }
            set
            {
                assignedBaselineDataSummary = value;
            }
        }

        public Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>> AssingendBaselineDetailsList
        {
            get
            {
                return assingendBaselineDetails;
            }
            set
            {
                this.assingendBaselineDetails = value;
            }
        }

        public bool AdjustLoopCount(int iTargetLoop,ref string strError)
        {
            Logger.logBegin("AdjustLoopCount");
            /// delete major object's loops
            /// 
            if (iTargetLoop<1)
            {
                strError = string.Format("Dataset should be kept at least 1 set, but Loop count is :[{0}]",iTargetLoop);
                return false;
            }
            List<short?> lstKeyLoop = assingendBaselineDetails.Keys.ToList();
            foreach (short? sKey in lstKeyLoop)
            {
                if (sKey == null) continue;
                if (sKey>=(iTargetLoop))
                {
                    ///delte 
                    /// 
                    assingendBaselineDetails.Remove(sKey);
                }
            }

            /// delete all extends objects
            /// 
            foreach (T_BASELINE_DATA_SUMMARYDTO objSumExt in extendObject.Keys.ToList())
            {
                lstKeyLoop = extendObject[objSumExt].Keys.ToList();
                foreach (short? sKey in lstKeyLoop)
                {
                    if (sKey == null) continue;
                    if (sKey >= (iTargetLoop))
                    {
                        ///delte 
                        /// 
                        extendObject[objSumExt].Remove(sKey);
                    }
                    extendObject.Remove(objSumExt);
                }
            }
            return true;
        }

        /// <summary>
        /// Ord Id will be re-adjusted outside
        /// </summary>
        /// <param name="iLoopId">Loop id sets to tell how many test loop</param>
        /// <param name="strContent"></param>
        /// <param name="strObjectName"></param>
        /// <param name="strError"></param>
        /// <returns></returns>
        public bool AddOrUpdateObject(int iLoopId, string strContent, string strObjectName, ref string strError, long lDataSummaryId=-int.MaxValue)
        {
            Logger.Info("AddOrUpdateObject", string.Format("Trying to update/add :[{0}] with values:[{1}]", iLoopId, strContent));

            /// steps :
            /// 1, check whether the summary object is exist
            ///     1.1 if the object name is not the same, then update mode ,otherwise, new object mode

            if (assignedBaselineDataSummary == null)
            {
                assignedBaselineDataSummary = new T_BASELINE_DATA_SUMMARYDTO();
                if (lDataSummaryId!=-int.MaxValue)
                {
                    assignedBaselineDataSummary.DATA_SUMMARY_ID = (Int16)lDataSummaryId;
                }
            }
            if (string.Compare(assignedBaselineDataSummary.OBJECT_HAPPY_NAME ?? "", strObjectName) != 0)
            {
                assignedBaselineDataSummary.OBJECT_HAPPY_NAME = strObjectName;
            }

            string[] arrValues = strContent.Split(new string[] { "\r\n","\r","\n" }, StringSplitOptions.None);
            Logger.Info("AddOrUpdateObject", string.Format("sub objects to create:[{0}]", arrValues.Length));
            List<T_BASELINE_DATA_DETAILDTO> lstTmp = new List<T_BASELINE_DATA_DETAILDTO>();
            if (assingendBaselineDetails == null)
                assingendBaselineDetails = new Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>>();
            if (this.assingendBaselineDetails.Keys.Contains((short)iLoopId))
            {
                this.tobeDeletedItems.AddRange(this.assingendBaselineDetails[(short)iLoopId]);
            }
            /// the first one is for assingendBaselineDetails
            /// 
            string itmValue = arrValues[0];
            #region create a object for Details
            T_BASELINE_DATA_DETAILDTO objItm = new T_BASELINE_DATA_DETAILDTO();
            objItm.LOOP_ID = (Int16)iLoopId;
            objItm.DATA_VALUE = itmValue;
            lstTmp.Add(objItm);
            #endregion
            this.assingendBaselineDetails[(short)iLoopId] = lstTmp;
            //this.assingendBaselineDetails.Add((short)iLoopId, lstTmp);

            /// create the rest objects
            /// 
            #region the rest objects
            //toDelExtendObject = this.extendObject;
            toDelExtendObject = new Dictionary<T_BASELINE_DATA_SUMMARYDTO, Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>>>();
            
            List<T_BASELINE_DATA_SUMMARYDTO> lstKeyOfExt = extendObject.Keys.OrderBy(p=>p.OBJECT_HAPPY_NAME).ToList();
            if (lstKeyOfExt.Count > arrValues.Length-1)
            {
                for (int i=arrValues.Length;i<lstKeyOfExt.Count;i++)
                {
                    toDelExtendObject.Add(lstKeyOfExt[i], extendObject[lstKeyOfExt[i]]);
                }
            }

            for (int i=1;i<arrValues.Length;i++)
            {
                #region create extends object
                if ((i-1)< lstKeyOfExt.Count)
                {
                    /// change the detail objects
                    /// 
                    Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>> dtlObjects = extendObject[lstKeyOfExt[i - 1]];
                    if (dtlObjects==null)
                    {
                        dtlObjects = new Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>>();
                        extendObject.Add(lstKeyOfExt[i - 1], dtlObjects);
                    }
                    List<T_BASELINE_DATA_DETAILDTO> dtlLstObjs = null;
                    if (dtlObjects.ContainsKey((short)iLoopId))
                    {
                        dtlLstObjs = dtlObjects[(short)iLoopId];
                    }
                    else
                    {
                        dtlLstObjs = new List<T_BASELINE_DATA_DETAILDTO>();
                    }
                    T_BASELINE_DATA_DETAILDTO objCurrDtlDto = null;
                    if (dtlLstObjs.Count<1)
                    {
                        dtlLstObjs.Add(objCurrDtlDto = new T_BASELINE_DATA_DETAILDTO());
                        objCurrDtlDto.DATA_BASE_OBJ_ID = lstKeyOfExt[i - 1].DATA_BASE_OBJ_ID;
                    }
                    objCurrDtlDto = dtlLstObjs[0];
                    objCurrDtlDto.DATA_VALUE = arrValues[i];

                    /// reuse the old objects
                    /// 
                    lstKeyOfExt[i - 1].OBJECT_HAPPY_NAME = string.Format("{0}_{1}", strObjectName, i);
                }
                else
                {
                    #region create all extend objects
                    /// new objects, 
                    /// steps:
                    /// 1, create a T_BASELINE_DATA_SUMMARY, set key to -1;
                    /// 2, create a T_BASELINE_DATA_DETAIL, set key to -1;
                    /// 3, add T_BASELINE_DATA_DETAIL to dictionary
                    /// 4, add T_BASELINE_DATA_SUMMARY to  extendObject
                    /// 
                    /** 1, create a T_BASELINE_DATA_SUMMARY, set key to -1; **/
                    T_BASELINE_DATA_SUMMARYDTO objSum = new T_BASELINE_DATA_SUMMARYDTO();
                    objSum.DATA_BASE_OBJ_ID = -1;
                    objSum.DATA_BASE_OBJ_PARENT_ID = assignedBaselineDataSummary.DATA_BASE_OBJ_ID;
                    objSum.DATA_SUMMARY_ID = assignedBaselineDataSummary.DATA_SUMMARY_ID;
                    objSum.OBJECT_HAPPY_NAME = string.Format("{0}_{1}",strObjectName, i);

                    /***2, create a T_BASELINE_DATA_DETAIL, set key to -1; ***/
                    List<T_BASELINE_DATA_DETAILDTO> lsttmpDtl = new List<T_BASELINE_DATA_DETAILDTO>();
                    T_BASELINE_DATA_DETAILDTO objDtlTmp = new T_BASELINE_DATA_DETAILDTO();
                    objDtlTmp.DATA_BASE_OBJ_ID = -1;
                    objDtlTmp.DATA_VALUE = arrValues[i];
                    objDtlTmp.DETAIL_ID = -1;
                    lsttmpDtl.Add(objDtlTmp);
                    /*** 3, add T_BASELINE_DATA_DETAIL to dictionary ***/
                    Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>> dicTmpSum = new Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>>();
                    dicTmpSum.Add((short)iLoopId,lsttmpDtl);

                    /*** 4, add T_BASELINE_DATA_SUMMARY to extendObject ***/
                    extendObject.Add(objSum, dicTmpSum);

                    #endregion create all extend objects

                }
                #endregion
            }
            #endregion //the rest objects
            
            return true;
            //if ()
        }


        public string ObjectHappyName
        {
            get
            {
                return assignedBaselineDataSummary == null ? "" : assignedBaselineDataSummary.OBJECT_HAPPY_NAME;
            }
            set
            {
                if (assignedBaselineDataSummary == null)
                {
                    assignedBaselineDataSummary = new T_BASELINE_DATA_SUMMARYDTO();
                }
                if (assignedBaselineDataSummary.OBJECT_HAPPY_NAME != value)
                {
                    assignedBaselineDataSummary.OBJECT_HAPPY_NAME = value;
                    // the extended objects name should change as well
                    int iIdx = 0;
                    foreach (var itm in extendObject.Keys.ToList().OrderBy(p => p.OBJECT_HAPPY_NAME))
                    {
                        changeExtendObjectsName(itm, value, iIdx);
                        iIdx++;
                    }
                }
                RaisePropertyChanged("ObjectHappyName");
            }
        }

        private void changeExtendObjectsName(T_BASELINE_DATA_SUMMARYDTO objToBechange, string strNewNamePrefix, int iIdx_Adjust)
        {
            if (objToBechange == null) return;
            Logger.Info("changeExtendObjectsName", string.Format("Try to change object name from [{0}] with new prefix:[{1}]", objToBechange.OBJECT_HAPPY_NAME, strNewNamePrefix));
            int iPos = objToBechange.OBJECT_HAPPY_NAME == null ? -1 : objToBechange.OBJECT_HAPPY_NAME.LastIndexOf("_");
            if (iPos == -1)
            {
                objToBechange.OBJECT_HAPPY_NAME = string.Format("{0}_{1}", strNewNamePrefix, iIdx_Adjust);
                return;
            }

            objToBechange.OBJECT_HAPPY_NAME = string.Format("{0}{1}", strNewNamePrefix, objToBechange.OBJECT_HAPPY_NAME.Substring(iPos));
        }


        internal string getValuesByLoopId(int iLoop)
        {
            string strResult = "";
            if (assingendBaselineDetails.ContainsKey((short)iLoop))
            {
                foreach (var itm in assingendBaselineDetails[(short)iLoop])
                {
                    /// should only one item 
                    /// 
                    strResult = itm.DATA_VALUE ?? "";
                }
            }
            else
                strResult = "";

            /// formater:Dictionary<T_BASELINE_DATA_SUMMARYDTO, Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>>>
            /// attach extends objects

            List<T_BASELINE_DATA_SUMMARYDTO> lstExtKeys = extendObject.Keys.OrderBy(p => p.OBJECT_HAPPY_NAME).ToList();
            for (int i=0;i<lstExtKeys.Count;i++)
            {
                var itmSummary = lstExtKeys[i];
                string strTmp = "";
                if (extendObject[itmSummary].ContainsKey((short)iLoop))
                {
                    foreach (var itmDtl in extendObject[itmSummary][(short)iLoop])
                    {
                        strTmp = itmDtl.DATA_VALUE;
                    }
                    strResult = string.Format("{0}\r\n{1}", strResult, strTmp);
                }              
                
            }

            return strResult;
        }

        internal void assignNewIdForMajorObj(long baseLineDataSummaryId)
        {
            assignedBaselineDataSummary.DATA_BASE_OBJ_ID = baseLineDataSummaryId;
            assignedBaselineDataSummary.DATA_BASE_OBJ_PARENT_ID = null;

            foreach(short? sLoop in assingendBaselineDetails.Keys)
            {
                List<T_BASELINE_DATA_DETAILDTO> lstDtl = assingendBaselineDetails[sLoop];
                if (lstDtl == null) continue;
                foreach (T_BASELINE_DATA_DETAILDTO itm in lstDtl)
                {
                    itm.DATA_BASE_OBJ_ID = baseLineDataSummaryId;
                }
            }

            #region update all sub auto generated items
            foreach(T_BASELINE_DATA_SUMMARYDTO itm in extendObject.Keys)
            {
                itm.DATA_BASE_OBJ_PARENT_ID = baseLineDataSummaryId;
            }
            #endregion update all sub auto generated items
        }

        internal bool updateAutoGenObjId(long lAutoGenedSummaryId, T_BASELINE_DATA_SUMMARYDTO detailItm, ref string strError)
        {
            Logger.Info("updateDetailId",string.Format("try to create a new auto generate objs [{0}],object Name:[{1}]", lAutoGenedSummaryId, detailItm.OBJECT_HAPPY_NAME));
            if (detailItm==null)
            {
                Logger.Warnning("updateAutoGenObjId", "object is null, ignored");
                return true;
            }

            Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>> dicDetails = this.extendObject[detailItm];
            detailItm.DATA_BASE_OBJ_ID = lAutoGenedSummaryId;
            /// update all detail objects
            /// 
            foreach (short? sLoop  in dicDetails.Keys)
            {
                List<T_BASELINE_DATA_DETAILDTO> lstDetail = dicDetails[sLoop];
                if (lstDetail == null) continue;
                if (lstDetail.Count > 0)
                    lstDetail[0].DATA_BASE_OBJ_ID = lAutoGenedSummaryId;
            }
            return true;
        }

        internal void CreateADefault(long dataSummaryId)
        {
            Logger.logBegin("CreateADefault");
            string strError = "";
            AddOrUpdateObject(0, "Please Change", "New Baseline Object", ref strError, dataSummaryId);
        }
    }

    internal class DataBaseSetting2Del
    {

    }
}
