using Mars.Business;
using Mars.Dialog;
using Mars.Dto;
using Mars.ViewModel.BaseData;
using Mars.ViewModel.BaselineDataEditorViewModelSub;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Mars.ViewModel
{
    public class BaselineDataEditorViewModel: ViewModelBase
    {
        public MarsSearchDialogDataContext<T_TEST_DATA_SUMMARYDTO> CurrentSearchInformation = new MarsSearchDialogDataContext<T_TEST_DATA_SUMMARYDTO>();

        #region command for finder
        private  bool SearchBaseLineDataSets()
        {
            Logger.logBegin("SearchBaseLineDataSets");
            /// get Baseline data set from database
            /// 
            B_T_TEST_DATA_SUMMARYDTO objDataSummary = new B_T_TEST_DATA_SUMMARYDTO();
            List<T_TEST_DATA_SUMMARYDTO> lstDataSummary = objDataSummary.getBaselineDataSetsBy(MarsMainWindow.CurrentDatabaseIdx, CurrentSearchInformation.SearchKey);
            CurrentSearchInformation.listResult = lstDataSummary;
            return false;
        }
        #endregion //command for finder

        private static MLogger Logger = MLogger.GetLogger(typeof(BaselineDataEditorViewModel));

        private ObservableCollection<MarsDataSettingTabsBase> _lstDataSetLoops;
        public ObservableCollection<MarsDataSettingTabsBase> _DataSetLoops
        {
            get { return _lstDataSetLoops; }
            set
            {
                if (_lstDataSetLoops!=value)
                {
                    _lstDataSetLoops = value;
                    RaisePropertyChanged("_DataSetLoops");
                }
            }
        }

        private int _activedTabIdx;
        public int activedTabIdx
        {
            get
            {
                if (_lstDataSetLoops == null) return _activedTabIdx =- 1;
                if (_lstDataSetLoops.Count <= 0) return _activedTabIdx =- 1;
                return _activedTabIdx;
            }
            set
            {
                if (value < 0 || value >= _lstDataSetLoops.Count) return;

                if (_activedTabIdx!=value)
                {
                    _activedTabIdx = value;
                }
                RaisePropertyChanged("activedTabIdx");
            }
        }


        private T_TEST_DATA_SUMMARYDTO currentDataSummaryInfo=null;
        #region unused code
        //private ObservableCollection<TabItem> _lstTabItems;
        //public ObservableCollection<TabItem> TabItems
        //{
        //    get { return _lstTabItems; }
        //    set {
        //        if (_lstTabItems!=value)
        //        {
        //            _lstTabItems = value;
        //            RaisePropertyChanged("TabItems");
        //        }
        //    }
        //}
        #endregion //unused code
        public int LoopCount
        {
            get {
                //return loopCount = _lstTabItems.Count;
                return _lstDataSetLoops.Count;
            }
            set
            {
                if (value <= 0)
                    value = 1;
                
                //if (value > _lstTabItems.Count)
                //{
                //    while(_lstTabItems.Count<value)
                //    {
                //        _lstTabItems.Add(new TabItem() { Header = string.Format("Loop_{0}", _lstTabItems.Count+1) });
                //    }
                //    //LoopCount = value;
                    
                //    //loopCount = _lstTabItems.Count;
                //    RaisePropertyChanged("LoopCount");
                //    return;
                //}
                //else
                //{
                //    while(_lstTabItems.Count>value)
                //    {
                //        _lstTabItems.RemoveAt(_lstTabItems.Count-1);
                //    }
                    
                //    //loopCount = _lstTabItems.Count;
                //    RaisePropertyChanged("LoopCount");
                //    return;
                //}
                if (value> _lstDataSetLoops.Count)
                {
                    while(_lstDataSetLoops.Count<value)
                    {
                        MarsDataSettingTabsBase objNewTab = null;
                        _lstDataSetLoops.Add(objNewTab=new MarsDataSettingTabsBase() { Header = string.Format("Loop_{0}", _lstDataSetLoops.Count + 1),Id= _lstDataSetLoops.Count });
                        objNewTab.afterContentChangeHander += this._currentObject.ContentChangedFromOutside;
                    }

                    RaisePropertyChanged("LoopCount");
                    RaisePropertyChanged("_DataSetLoops");
                    activedTabIdx = _lstDataSetLoops.Count > 0 ? _lstDataSetLoops.Count-1 : -1;
                }
                else
                {
                    while(_lstDataSetLoops.Count>value)
                    {
                        _lstDataSetLoops[_lstDataSetLoops.Count - 1].afterContentChangeHander = null;
                        _lstDataSetLoops.RemoveAt(_lstDataSetLoops.Count - 1);
                    }
                    RaisePropertyChanged("LoopCount");
                    RaisePropertyChanged("_DataSetLoops");
                    activedTabIdx = _lstDataSetLoops.Count > 0 ? 0 : -1;

                    int idx = this._definedObjects.IndexOf(this._currentObject);
                    if (idx>0)
                    {
                        string strError = "";
                        this._definedObjects[idx].AdjustLoopCount(LoopCount, ref strError);
                        RaisePropertyChanged("definedObjects");
                    }

                }
            }
        }

        #region //conver data to target
        public ObservableCollection<SearchableCommonResult4ListView<T_TEST_DATA_SUMMARYDTO>> ConvertDataSetSummary2DisplayForFinderImpl<T>(List<T_TEST_DATA_SUMMARYDTO> listResult)
        {
            Logger.Info("ConvertDataSetSummary2DisplayForFinderImpl", "Delegate provided for datacontext");
            ObservableCollection<SearchableCommonResult4ListView<T_TEST_DATA_SUMMARYDTO>> lstResult = new ObservableCollection<SearchableCommonResult4ListView<T_TEST_DATA_SUMMARYDTO>>();
            foreach(var itm in listResult)
            {
                SearchableCommonResult4ListView<T_TEST_DATA_SUMMARYDTO> objItm = new SearchableCommonResult4ListView<T_TEST_DATA_SUMMARYDTO>();
                if (!(itm is T_TEST_DATA_SUMMARYDTO))
                {
                    continue;
                }
                
                objItm.Description = ((T_TEST_DATA_SUMMARYDTO)itm).DESCRIPTION_INFO;
                objItm.Name = ((T_TEST_DATA_SUMMARYDTO)itm).ALIAS_NAME;
                objItm.objectAttached = itm;
                //objItm.Description = itm.
                lstResult.Add(objItm);
            }
            return lstResult;
        }

        public bool AfterOkButtonClickImpl<T>(SearchableCommonResult4ListView<T> objectSelectedItem)
        {
            Logger.logBegin("AfterOkButtonClickImpl");
            if (!(objectSelectedItem.objectAttached is T_TEST_DATA_SUMMARYDTO))
            {
                Logger.Error("AfterOkButtonClickImpl", string.Format("Only T_TEST_DATA_SUMMARYDTO is support for this modal, but type is :[{0}]", objectSelectedItem.objectAttached.GetType()));
                return false;
            }
            
            T_TEST_DATA_SUMMARYDTO objDto =(T_TEST_DATA_SUMMARYDTO)Convert.ChangeType(objectSelectedItem.objectAttached, typeof(T_TEST_DATA_SUMMARYDTO));
            if (objDto == null) return false;

            currentDataSummaryInfo = objDto;
            oSearchKey = objDto.ALIAS_NAME;
            RaisePropertyChanged("DataSetDescription");

            /// popup all sub objects for list
            /// 
            B_T_BASELINE_DATA_SUMMARY objBaselineDataSum = new B_T_BASELINE_DATA_SUMMARY();
            Dictionary<T_BASELINE_DATA_SUMMARYDTO,Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>>> hashBaseLineData=objBaselineDataSum.getBaseLineDataAndDetails(MarsMainWindow.CurrentDatabaseIdx, objDto.DATA_SUMMARY_ID);

            /// set to list view binding _definedObjects
            /// 
            ConvertDataFromDB2ListCollection(hashBaseLineData);
            currentObject = ((definedObjects != null) && (definedObjects.Count > 0)) ? definedObjects[0] : null;

            /// change _lstDataSetLoops
            
            return true;
        }

        private void ConvertDataFromDB2ListCollection(Dictionary<T_BASELINE_DATA_SUMMARYDTO, Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>>> dicDataFromDB)
        {
            Logger.logBegin("ConvertDataFromDB2ListCollection");
            if (dicDataFromDB==null)
            {
                Logger.Warnning("ConvertDataFromDB2ListCollection", "Object parameters is null");
                _definedObjects = null;
                return;
            }

            List<T_BASELINE_DATA_SUMMARYDTO> objKeys = dicDataFromDB.Keys.OrderBy(p=>p.OBJECT_HAPPY_NAME).ToList();
            //var q = from sum in objKeys
            //        from s1 in objKeys
            //        where ((sum.DATA_BASE_OBJ_PARENT_ID == -1) || (sum.DATA_BASE_OBJ_PARENT_ID == null))
            //        && s1.DATA_BASE_OBJ_PARENT_ID == sum.DATA_BASE_OBJ_ID
            //        select new
            //        {
            //            p = sum,
            //            d = s1
            //        };
            var q= from sum_1 in objKeys                    // 
                    join s1 in objKeys
                    on sum_1.DATA_BASE_OBJ_ID equals s1.DATA_BASE_OBJ_PARENT_ID   into w
                    where ((sum_1.DATA_BASE_OBJ_PARENT_ID == -1) || (sum_1.DATA_BASE_OBJ_PARENT_ID == null))
                    from w1 in w.DefaultIfEmpty()
                    select new
                    {
                        p = sum_1,
                        d = w1
                    };

            ObservableCollection <BaseLineData_ChildItem>  tmpDefinedObjects = new ObservableCollection<BaseLineData_ChildItem>();
            Dictionary < T_BASELINE_DATA_SUMMARYDTO,List<T_BASELINE_DATA_SUMMARYDTO>> dicSumAndItsExt = q.GroupBy(x => x.p, x => x.d).ToDictionary(z => z.Key, z => z.ToList());
            foreach (var itm in dicSumAndItsExt.Keys)
            {
                BaseLineData_ChildItem objItm = new BaseLineData_ChildItem();
                objItm.AssignedBaselineDataSummary = itm;
                objItm.AssingendBaselineDetailsList = dicDataFromDB[itm];
                
                Dictionary<T_BASELINE_DATA_SUMMARYDTO, Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>>> extndObjs = new Dictionary<T_BASELINE_DATA_SUMMARYDTO, Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>>>();
                foreach (var subItm in dicSumAndItsExt[itm])
                {
                    if (subItm == null)
                        continue;
                    extndObjs.Add(subItm, dicDataFromDB[subItm]);
                }
                objItm.extendObject = extndObjs;
                tmpDefinedObjects.Add(objItm);
            }
            definedObjects = tmpDefinedObjects;
            
        }

        private void afterCurrentObjectChange_Loops()
        {
            /// Steps:
            /// 1, clear tabs, but keep on
            /// 2, get loop count
            /// 3, 
            int iLoopCnt = _currentObject.getLoopCount();
            _lstDataSetLoops.Clear();
            MarsDataSettingTabsBase objTab = null;
            if (iLoopCnt == 0)
            {
                
                _lstDataSetLoops.Add(objTab =new MarsDataSettingTabsBase() { Id=0 });
                objTab.Header = string.Format("Loop_{0}", _lstDataSetLoops.Count + 1);
            }
            else
            {
                for (int i = 0; i < iLoopCnt; i++)
                {
                    _lstDataSetLoops.Add(objTab = new MarsDataSettingTabsBase() { Header = string.Format("Loop_{0}", _lstDataSetLoops.Count + 1),                       
                        Id=i,                        
                    });
                    objTab.Content = _currentObject.getValuesByLoopId(i);
                    _lstDataSetLoops[_lstDataSetLoops.Count - 1].afterContentChangeHander += _currentObject.ContentChangedFromOutside;
                }
            }
            RaisePropertyChanged("LoopCount");
            RaisePropertyChanged("_DataSetLoops");

            activedTabIdx = _lstDataSetLoops == null ? -1 : _lstDataSetLoops.Count > 0 ? 0 : -1;
        }
        #endregion

        #region one object
        private ObservableCollection<BaseLineData_ChildItem> _definedObjects=new ObservableCollection<BaseLineData_ChildItem>();

        public ObservableCollection<BaseLineData_ChildItem> definedObjects
        {
            get
            {
                return _definedObjects;
            }

            set
            {
                if (_definedObjects!=value)
                {
                    _definedObjects = value;
                    RaisePropertyChanged("definedObjects");
                }
            }
        }
        private BaseLineData_ChildItem _currentObject;
        public BaseLineData_ChildItem currentObject
        {
            get {
                return _currentObject;
            }
            set
            {
                if (_currentObject!=value)
                {
                    _currentObject = value;

                    RaisePropertyChanged("currentObject");
                    RaisePropertyChanged("currentObjectHappyName");

                    /// change details tabs 
                    /// 
                    if (_currentObject != null)
                        afterCurrentObjectChange_Loops();
                }
            }
        }
        public string currentObjectHappyName
        {
            get
            {
                return _currentObject == null ? "" : _currentObject.ObjectHappyName;
            }
            set
            {
                if (_currentObject==null)
                {
                    _currentObject = new BaseLineData_ChildItem();
                    
                }
                
                if (_currentObject.ObjectHappyName!=value)
                {
                    _currentObject.ObjectHappyName = value;
                    RaisePropertyChanged("currentObjectHappyName");
                    RaisePropertyChanged("currentObject");
                    RaisePropertyChanged("definedObjects");
                    
                }

                //if (_definedObjects.Count==0)
                //{
                //    _definedObjects.Add(_currentObject);
                //    RaisePropertyChanged("definedObjects");
                //}
            }
        }
        #endregion //one object

        public BaselineDataEditorViewModel()
        {
            initializeData();
            
        }



        private void initializeData()
        {
            CurrentSearchInformation.Hint = "Baseline Dataset Searching:";
            CurrentSearchInformation.SearchKey = "Input Search Key here...";

            _lstDataSetLoops = new ObservableCollection<MarsDataSettingTabsBase>();
            _lstDataSetLoops.Add(
                new MarsDataSettingTabsBase() {
                    Header="Loop_1",
                }
                );

            //_lstTabItems = new ObservableCollection<TabItem>();
            //_lstTabItems.Add(
            //    new TabItem() { Header="Loop_1"}
            //    );
            //DockPanel objPnl = null;
            //_lstTabItems[0].Content = objPnl = new DockPanel() { Background= new SolidColorBrush(System.Windows.Media.Color.FromRgb(255,0,0)) } ;
            //objPnl.Children.Add(new DataGrid());

            // for finder
            CurrentSearchInformation.onSearchButtonClick = new DelegateCommand(() => SearchBaseLineDataSets());            
            CurrentSearchInformation.convertData2Disp += ConvertDataSetSummary2DisplayForFinderImpl<T_TEST_DATA_SUMMARYDTO>;
            CurrentSearchInformation.afterOkButtonClickHandler += AfterOkButtonClickImpl<T_TEST_DATA_SUMMARYDTO>;
        }

        public string oSearchKey
        {
            get
            {
                return CurrentSearchInformation.SearchKey;
            }
            set
            {
                CurrentSearchInformation.SearchKey = value;
                RaisePropertyChanged("oSearchKey");
            }
        }
        
        public string DataSetDescription
        {
            get
            {
                return currentDataSummaryInfo==null?"": currentDataSummaryInfo.DESCRIPTION_INFO;
            }

            set
            {
                if (currentDataSummaryInfo==null)
                {
                    currentDataSummaryInfo = new T_TEST_DATA_SUMMARYDTO();
                }
                if (currentDataSummaryInfo.DESCRIPTION_INFO!=value)
                {
                    currentDataSummaryInfo.DESCRIPTION_INFO = value;
                    RaisePropertyChanged("DataSetDescription");
                }
            }
        }

        private ICommand onSearchButtonClick;
        public ICommand OnSearchButtonClick
        {
            get {
                if (onSearchButtonClick==null)
                {
                    onSearchButtonClick=new DelegateCommand(()=> { SearchButtonClickImpl(); }) ;
                    
                }
                return onSearchButtonClick;
            }
        }

        private ICommand onDataSetNumberChange=null;
        public ICommand OnDataSetNumberChange
        {
            get
            {
                if (onDataSetNumberChange==null)
                {
                    onDataSetNumberChange = new DelegateCommand(()=> { onDatasetNumberChangeImpl(); });
                }
                return onDataSetNumberChange;
            }
        }

        private bool SearchButtonClickImpl()
        {
            bool isOk = MarsDialogHelper.GetCommonFinderForModal(CurrentSearchInformation);
            return isOk;
        }

        private bool onDatasetNumberChangeImpl()
        {
            Logger.logBegin("onDatasetNumberChangeImpl");
            ///steps:
            /// 1, select the current object            
            /// 2, Delete Loops
            /// 
            int iIdx = this.definedObjects.IndexOf(_currentObject);
            if (_currentObject == null)
            {
                HintByMessageBox("The currentObject is note exists, please select one from the left list", "Hint");
                return false;
            }
            
            string strError = "";
            bool isAdjustOk = this._currentObject.AdjustLoopCount(LoopCount,ref strError);
            if (!isAdjustOk)
            {
                HintByMessageBox(string.Format("System can't adjust Loop, because:\r\n{0}", strError), "Error");
            }
            
            return isAdjustOk;
        }

        #region Command
        public ICommand SaveDataSet
        {
            get
            {
                return new DelegateCommand(()=>saveDataSetImpl()) ;
            }
        }

        public ICommand SaveUpdateObject
        {
            get
            {
                return new DelegateCommand(() => SaveUpdateObjectImpl());
            }
        }
        public ICommand DeleteObject
        {
            get
            {
                return new DelegateCommand(() => DeleteObjectImpl());
            }
        }

        public ICommand CreateObject
        {
            get { return new DelegateCommand(()=>CreateObjectImpl()); }
        }

        private bool saveDataSetImpl()
        {
            Logger.logBegin("saveDataSetImpl");

            return false;
        }
        private bool SaveUpdateObjectImpl()
        {
            Logger.logBegin("SaveUpdateObjectImpl");
            /// steps:
            /// 1, make sure that the data is not empty, and unique
            /// 2, covert to T_BASELINE_DATA_DETAILDTO and T_BASELINE_DATA_SUMMARYDTO
            /// 3, add to list 
            #region step 1:make sure that the data is not empty, and unique
            /// step 1:
            /// 
            bool isDataEmpty = ValidateDataSetting();
            if (!isDataEmpty)
            {
                isDataEmpty = QuestionByMessageBox(string.Format("At least one of the data is empty, \r\nare you sure to save?"), "Hint");
            }
            if (!isDataEmpty) return false;

            bool isUnique = CheckObjectNameIsUnique();
            if (!isUnique)
            {
                HintByMessageBox(string.Format("The object [{0}] is already exists", this._currentObject==null?"":this._currentObject.ObjectHappyName), "Error");
                return false;
            }
            #endregion

            #region step 2:covert to T_BASELINE_DATA_DETAILDTO and T_BASELINE_DATA_SUMMARYDTO
            /// if multiple rows exist inside of the content, 
            /// then, new sub object should be exits
            /// 
            ConvertSetting2Dtos();
            #endregion

            #region step 3:save to data base
            bool isOk = SaveToDatabase();
            #endregion // save to database
            return false;
        }

        
        private bool SaveToDatabase()
        {
            Logger.logBegin("SaveToDatabase");
            B_T_TEST_DATA_SUMMARYDTO objDataMgr = new B_T_TEST_DATA_SUMMARYDTO();
            string strError = ""; 
            try
            {
                using (var trans = objDataMgr.InitTransaction(MarsMainWindow.CurrentDatabaseIdx))
                {
                    /// firstly, delete BASELINE_DATA_DETAIL
                    /// 
                    #region delete error object 
                    foreach (var lstObjsItm in this.definedObjects)
                    {
                        objDataMgr.DeleteObject_BASELINE_DATA_DETAIL(lstObjsItm.ErrorSummaryInfo, ref strError);
                    }

                    #endregion

                    bool isDeleted = false;
                    #region delete auto generated sub objects
                    isDeleted = objDataMgr.DeleteBaseDetailByDataSetId(this.currentDataSummaryInfo.DATA_SUMMARY_ID,ref strError);
                    if (!isDeleted)
                    {
                        string strErrorTmp = "";
                        objDataMgr.RollbackTransactoin(ref strErrorTmp);
                        HintByMessageBox(string.Format("Can't delete detail objects with Errors:\r\n{0}", strError), "Warnning");
                        return false;
                    }
                    //delte base summary objects by dataset id
                    isDeleted = objDataMgr.DeleteBaseSummaryByDataSetId(this.currentDataSummaryInfo.DATA_SUMMARY_ID, ref strError);
                    if (!isDeleted)
                    {
                        string strErrorTmp = "";
                        objDataMgr.RollbackTransactoin(ref strErrorTmp);
                        HintByMessageBox(string.Format("Can't delete sumamry objects with Errors:\r\n{0}", strError), "Warnning");
                        return false;
                    }
                    /*
                    foreach (var lstObjsItm in this.definedObjects)
                    {
                        //isDeleted = objDataMgr.DeleteObject_BASELINE_DATA_DETAIL(lstObjsItm.extendObject.Keys.ToList(), ref strError);
                        //if (!isDeleted)
                        //{
                        //    string strErrorTmp = "";
                        //    objDataMgr.RollbackTransactoin(ref strErrorTmp);
                        //    HintByMessageBox(string.Format("Can't delete Extend objects with Errors:\r\n{0}", strError), "Warnning");
                        //    return false;
                        //}
                        isDeleted = objDataMgr.DeleteObject_BASELINE_DATA_DETAIL_byBaseObject(lstObjsItm.AssignedBaselineDataSummary.DATA_BASE_OBJ_ID, ref strError);
                        if (!isDeleted)
                        {
                            string strErrorTmp = "";
                            objDataMgr.RollbackTransactoin(ref strErrorTmp);
                            HintByMessageBox(string.Format("Can't delete Extend objects with Errors:\r\n{0}", strError), "Warnning");
                            return false;
                        }
                        /// delete major one
                        /// 
                        isDeleted = objDataMgr.DeleteObject_BASELINE_DATA_DETAIL(lstObjsItm.AssignedBaselineDataSummary, ref strError);
                        if (!isDeleted)
                        {
                            string strErrorTmp = "";
                            objDataMgr.RollbackTransactoin(ref strErrorTmp);
                            HintByMessageBox(string.Format("Can't delete Major objects with Errors:\r\n{0}", strError), "Warnning");
                            return false;
                        }
                    }
                    #endregion

                    #region delete main 
                    foreach (var lstObjsItm in this.definedObjects)
                    {
                        //isDeleted = objDataMgr.DeleteObject_BASELINE_DATA_DETAIL(lstObjsItm.AssignedBaselineDataSummary, ref strError);
                        //if (!isDeleted)
                        //{
                        //    string strErrorTmp = "";
                        //    objDataMgr.RollbackTransactoin(ref strErrorTmp);
                        //    HintByMessageBox(string.Format("Can't delete Marjor objects with Errors:\r\n{0}", strError), "Warnning");
                        //    return false;
                        //}
                        #region delete auto generated objects by parent id
                        isDeleted = objDataMgr.Deleteobject_BASELINE_DATA_SUMMARY_AUTOGEN(lstObjsItm.AssignedBaselineDataSummary, ref strError);
                        if (!isDeleted)
                        {
                            string strErrorTmp = "";
                            objDataMgr.RollbackTransactoin(ref strErrorTmp);
                            HintByMessageBox(string.Format("Can't delete auto-gen Marjor objects with Errors:\r\n{0}", strError), "Warnning");
                            return false;
                        }
                        #endregion
                        #region delete marjor object 
                        /// it will re-create again
                        /// 
                        isDeleted = objDataMgr.Deleteobject_BASELINE_DATA_SUMMARY(lstObjsItm.AssignedBaselineDataSummary, ref strError);
                        if (!isDeleted)
                        {
                            string strErrorTmp = "";
                            objDataMgr.RollbackTransactoin(ref strErrorTmp);
                            HintByMessageBox(string.Format("Can't delete auto-gen Marjor objects with Errors:\r\n{0}", strError), "Warnning");
                            return false;
                        }
                        #endregion //delete marjor object 

                    }
                    */
                    #endregion

                    long summaryId = -1;
                    #region  build major objects
                    bool isUpdate = false, isCreatedExtendObj = false, isCreatedDetailObj=false;
                    foreach (var lstObjsItm in this.definedObjects)
                    {
                        bool isCreateANew = false;
                        if (lstObjsItm.AssignedBaselineDataSummary.DATA_BASE_OBJ_ID <= 0)
                        {
                            ///get a new ID and set all others subs
                            /// 
                            summaryId = objDataMgr.GetNewIdForBaseLineData();
                            isCreateANew = true;
                        }
                        else
                        {
                            summaryId = lstObjsItm.AssignedBaselineDataSummary.DATA_BASE_OBJ_ID;
                            isCreateANew = true;//because all objects are deleted
                        }
                        lstObjsItm.assignNewIdForMajorObj(summaryId);
                        isUpdate = objDataMgr.updateOrCreateBaseLineSummaryObj(isCreateANew, lstObjsItm.AssignedBaselineDataSummary, ref strError);
                        if (!isUpdate)
                        {
                            string strErrorTmp = "";
                            objDataMgr.RollbackTransactoin(ref strErrorTmp);
                            HintByMessageBox(string.Format("Can't delete Marjor objects with Errors:\r\n{0}", strError), "Warnning");
                            return false;
                        }

                        ///need to insert or update its children
                        foreach (short? sLoop in lstObjsItm.AssingendBaselineDetailsList.Keys)
                        {
                            List<T_BASELINE_DATA_DETAILDTO> lstDtlItms = lstObjsItm.AssingendBaselineDetailsList[sLoop];
                            if (lstDtlItms == null || lstDtlItms.Count <= 0) continue;
                            isCreateANew = lstDtlItms[0].DETAIL_ID <= 0;
                            lstDtlItms[0].DETAIL_ID = isCreateANew ? objDataMgr.GetNewIdForBaseLineData() : lstDtlItms[0].DETAIL_ID;

                            isCreatedDetailObj = objDataMgr.updateOrCreateDetailObject(true, lstDtlItms[0], ref strError);
                            if (!isCreatedDetailObj)
                            {
                                string strETmp = strError;
                                Logger.Error("SaveToDatabase", string.Format("Error,when call updateOrCreateDetailObject:\r\n\t[{0}]", strError));
                                isCreatedDetailObj = objDataMgr.RollbackTransactoin(ref strError);
                                if (!isCreatedDetailObj)
                                {
                                    strETmp += "\r\n\tCan't Rollback, please restart the application.";
                                }
                                HintByMessageBox(strETmp, "Error");
                                return false;
                            }
                        }
                    }
                    #endregion

                    #region build Extends objects
                    /// important:
                    /// for auto generated objects, if DATA_BASE_OBJ_ID is less than 0, than means a new object is created, otherwise, an exists object
                    /// 
                    long iExtendSummaryId=-1;
                    
                    foreach (var objItm in this.definedObjects)
                    {
                        if (objItm.AssignedBaselineDataSummary.DATA_BASE_OBJ_ID <= 0)
                        {
                            Logger.Warnning("SaveToDatabase", string.Format("AssignedBaselineDataSummary is ignored as ID is less than 0:[{0}], object Name:[{1}]", objItm.AssignedBaselineDataSummary.DATA_BASE_OBJ_ID, objItm.AssignedBaselineDataSummary.OBJECT_HAPPY_NAME));
                            continue;
                        }
                        if (objItm.extendObject == null) continue;

                        foreach (var detailItm in objItm.extendObject.Keys)
                        {
                            if (detailItm == null) continue;
                            bool isCreateANew = false;
                            /// all objects should create as new 
                            /// 
                            if (detailItm.DATA_BASE_OBJ_ID <= 0)
                            {
                                iExtendSummaryId = objDataMgr.GetNewIdForBaseLineData();
                                isCreateANew = true;
                            }
                            else
                            {
                                iExtendSummaryId = detailItm.DATA_BASE_OBJ_ID;
                                isCreateANew = true;
                            }
                            isCreatedExtendObj = objItm.updateAutoGenObjId(iExtendSummaryId, detailItm, ref strError);
                            #region update or create Extends objects
                            isCreatedExtendObj = objDataMgr.updateOrCreateExtendBaseLineSummaryObj(isCreateANew, detailItm, ref strError);
                            if (!isCreatedExtendObj)
                            {
                                string strETmp = strError;
                                Logger.Error("SaveToDatabase", string.Format("Error when create or update extends objects:\r\n[{0}]",strError));
                                isCreatedDetailObj = objDataMgr.RollbackTransactoin(ref strError);
                                if (!isCreatedDetailObj)
                                {
                                    strETmp += "\r\n\tCan't Rollback, please restart the application.";
                                }
                                HintByMessageBox(strETmp, "Error");
                                return false;
                            }
                            #endregion

                            #region update detail objects
                            Dictionary<short?, List<T_BASELINE_DATA_DETAILDTO>> dicDtlWithLoop = objItm.extendObject[detailItm];
                            foreach (short? sLoop in dicDtlWithLoop.Keys)
                            {
                                List<T_BASELINE_DATA_DETAILDTO> lstDtlItms = dicDtlWithLoop[sLoop];
                                if (lstDtlItms == null || lstDtlItms.Count <= 0) continue;
                                isCreateANew = true;
                                lstDtlItms[0].DETAIL_ID = isCreateANew? objDataMgr.GetNewIdForBaseLineData(): lstDtlItms[0].DETAIL_ID;

                                isCreatedDetailObj = objDataMgr.updateOrCreateDetailObject(isCreateANew, lstDtlItms[0], ref strError);
                                if (!isCreatedDetailObj)
                                {
                                    string strETmp = strError;
                                    Logger.Error("SaveToDatabase", string.Format("Error,when call updateOrCreateDetailObject:\r\n\t[{0}]", strError));
                                    isCreatedDetailObj = objDataMgr.RollbackTransactoin(ref strError);
                                    if (!isCreatedDetailObj)
                                    {
                                        strETmp += "\r\n\tCan't Rollback, please restart the application.";
                                    }
                                    HintByMessageBox(strETmp, "Error");
                                    return false;
                                }
                            }
                            
                            #endregion
                        }
                    }
                    #endregion

                    bool isOk = objDataMgr.CommitTransction(ref strError);
                    if (!isOk)
                    {
                        Logger.Error("SaveToDatabase", string.Format("CommitTransction caused error:[{0}]", strError));
                        return false;
                    }
                    //Logger.Error("SaveToDatabase", string.Format("Can't save to database, and data rolled back. \r\nError:[{0}]", strError));
                    return isOk;
                }
            }
            catch (Exception e)
            {
                Logger.Error("SaveToDatabase",string.Format("Can't save to database, \r\nexceptions:[{0}]",e.Message),e);
                return false;
            }
            
        }


        private bool CreateObjectImpl()
        {
            Logger.logBegin("CreateObjectImpl");
            /// added a default objct with name to definedObjects
            /// 
            if (_definedObjects == null)
            {
                _definedObjects = new ObservableCollection<BaseLineData_ChildItem>();
            }
            BaseLineData_ChildItem objTmpItm = null;
            _definedObjects.Add(objTmpItm = new BaseLineData_ChildItem());
            objTmpItm.CreateADefault(this.currentDataSummaryInfo.DATA_SUMMARY_ID);
            RaisePropertyChanged("definedObjects");

            /// set the index is the last one
            /// 
            currentObject = _definedObjects == null ? null : _definedObjects.Count <= 0 ? null : _definedObjects[_definedObjects.Count - 1];
            return true;
        }

        private bool DeleteObjectImpl()
        {
            Logger.logBegin("DeleteObjectImpl");
            if (this._currentObject== null)
            {
                HintByMessageBox("Please select a object to be deleted.", "Hint");
                return false;
            }
            string strObjName = this._currentObject.ObjectHappyName;
            if (!QuestionByMessageBox(string.Format("Do you want to delete [{0}] and all data?", strObjName), "Mars Hint"))
                return false;

            int idx = this._definedObjects.IndexOf(this._currentObject);
            if (idx<0)
            {
                string strError = "";
                HintByMessageBox(strError=string.Format("Can't find Object from [{0}], unknow error, please restart system.", strObjName), "Warning");
                Logger.Error("DeleteObjectImpl",strError);
                return false;
            }
            this._definedObjects.Remove(this._currentObject);
            RaisePropertyChanged("definedObjects");

            if (this._definedObjects.Count > 0)
                this.currentObject = this._definedObjects[0];
            else
                this.currentObject = null;
            

            return false;
        }
        #endregion //command

        private bool ValidateDataSetting()
        {
            Logger.logBegin("ValidateDataSetting");
            foreach (var itm in this._lstDataSetLoops)
            {
                if (string.IsNullOrEmpty(itm.Content))
                    return false;
            }
            return true;
        }

        private void ConvertSetting2Dtos()
        {
            Logger.logBegin("ConvertSetting2Dtos");
            try
            {
                /// get loops number
                /// 
                string strObjectName = currentObjectHappyName;
                for (int i =0;i<this._lstDataSetLoops.Count;i++)
                {
                    //bool isUpdateOk = this._currentObject.AddOrUpdateObject(i, this._lstDataSetLoops[i].Content, strObjectName,ref strError);
                }
            }
            finally
            {
                Logger.logEnd("ConvertSetting2Dtos");
            }
        }

        private bool CheckObjectNameIsUnique()
        {
            Logger.logBegin("CheckObjectNameIsUnique");
            try
            {
                string strCurrentObjectName = this._currentObject.ObjectHappyName;
                return this._definedObjects.FirstOrDefault(p => p.ObjectHappyName == strCurrentObjectName) != null;
            }
            finally
            {
                Logger.logEnd("CheckObjectNameIsUnique");
            }
            
        }
    }

    internal class MarsDataContext4BaseLineDataEditor
    {
        
    }

    
}
