using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.Model;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Security.Principal;
using Mars.Business;
using Mars.Dto;

using System.IO;
//using Microsoft.Practices.Prism.Commands;
using System.Data;
using Mars.Helpers;
using System.Windows.Forms;
using Mars.DataLayer;
using Mars.Utility;
using System.Windows.Threading;
using System.Threading;
using Route2NSEx.src.Marquis.systemUtil;
using System.Windows.Data;
using Mars.ViewModel.objectManagement;
using System.Diagnostics;
using Prism.Commands;
#if _NOQTP
using Mars.InjectorAgent;
using Mars.AutoTestingDriver.injector;
using Mars.AutoTestingDriver.interProcess;
#endif

namespace Mars.ViewModel
{
    public delegate void OnObjectListChangeEvent(int iMode, int iInsertPos = -1, object oData=null);

    public class ObjectDatabaseViewModel : ViewModelBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(ObjectDatabaseViewModel));
        private ICommand _saveCommand;
        private ICommand _clearCommand;
        private ICommand _loadFileCommand;


        MarsEntities marsEntities;
        string _name;
        string _internalName;
        string _description;
        ObservableCollection<B_REGISTERED_APPS> _registerdApplication = new ObservableCollection<B_REGISTERED_APPS>();
        List<B_GUI_COMPONENT_TYPE_DIC> _typeList = new List<B_GUI_COMPONENT_TYPE_DIC>();
        ObservableCollection<string> _pegwindow = new ObservableCollection<string>();
        private string _pegwindowStatus;
        private string _applicationStatus;
        private string _excelFileName;

        private bool isStartedToLoad;
        public bool IsStartedToLoad
        {
            get { return isStartedToLoad; }
            set { isStartedToLoad = value;RaisePropertyChanged("IsStartedToLoad"); }
        }


        private B_GUI_COMPONENT_TYPE_DIC _typeStatus;
        B_REGISTERED_APPS _selectedApplication;
        ObservableCollection<B_REGISTED_OBJECT> _registerdObject = new ObservableCollection<B_REGISTED_OBJECT>();
        ObservableCollection<B_REGISTED_OBJECT> _RegisterdObjects4BatchConvert = new ObservableCollection<B_REGISTED_OBJECT>();
        public ObservableCollection<B_REGISTED_OBJECT> RegisterdObjects4BatchConvert
        {
            get
            {
                return _RegisterdObjects4BatchConvert;
            }
            set
            {
                _RegisterdObjects4BatchConvert = value;
                RaisePropertyChanged("RegisterdObjects4BatchConvert");
            }
        }
        public string ExcelFileName
        {
            get { return _excelFileName; }
            set
            {
                _excelFileName = value;
            }
        }

        private B_REGISTED_OBJECT selectedObject;
        public B_REGISTED_OBJECT SelectedObject
        {
            get { return selectedObject; }
            set
            {
                selectedObject = value;
                
                RaisePropertyChanged("SelectedObject");
                SelectedObjectCopy =(value==null? null :value.getShallowColone());
                ///创建sql datasource信息
                /// 
                CreateObjectDataSourceInfo();
            }
        }

        private B_REGISTED_OBJECT _SelectedObjects4BatchOP;
        public B_REGISTED_OBJECT SelectedObjects4BatchOP
        {
            get { return _SelectedObjects4BatchOP; }
            set
            {
                _SelectedObjects4BatchOP = value;
                RaisePropertyChanged("SelectedObjects4BatchOP");                
            }
        }

        private B_REGISTED_OBJECT selectedObjectCopy;
        public B_REGISTED_OBJECT SelectedObjectCopy
        {
            get { return selectedObjectCopy==null?selectedObjectCopy=new B_REGISTED_OBJECT(): selectedObjectCopy; }
            set
            {
                selectedObjectCopy = value;
                RaisePropertyChanged("SelectedObjectCopy");
            }
        }

        private string uploadLog;
        public string UploadLog
        {
            get { return uploadLog; }
            set {
                uploadLog = value;
                RaisePropertyChanged("UploadLog");
            }
        }
        private ObservableCollection<string> _errorList = new ObservableCollection<string>();
        public ObservableCollection<string> errorList
        {
            get { return _errorList; }
            set { _errorList = value;RaisePropertyChanged("errorList"); }
        }


        private DelegateCommand<object> _exportToExcelCommand;
        string objectdatabaseControlStatus;
        long applicationId;
        string _enumType;

        public DelegateCommand ConvertObjectsToOtherApplication
        {
            get
            {
                return new DelegateCommand(new Action(()=> {
                    //
                    if ((SelectedApplication==null)||(TargetSelectedApplication==null))
                    {
                        HintByMessageBox("Please select both Source Application and target application first.", "Hint");
                        return;
                    }
                    if (SelectedApplication.APPLICATION_ID==TargetSelectedApplication.APPLICATION_ID)
                    {
                        HintByMessageBox("Source Application and Target application should be different.", "Hint");
                        return;
                    }
                    if (RegisterdObjects4BatchConvert.Count == 0)
                    {
                        if (!QuestionByMessageBox(string.Format("Do you want System to copy all objects from application [{0}] to [{1}]? \r\nNote:\r\nAll Test Cases with those objects will be applied to new objects too.",
                            string.Format("{0}.{1}", SelectedApplication.APP_SHORT_NAME, SelectedApplication.VERSION),
                            string.Format("{0}.{1}", TargetSelectedApplication.APP_SHORT_NAME, TargetSelectedApplication.VERSION)), "Hint"))
                        {
                            return;
                        }

                        //Dispatcher.CurrentDispatcher.Invoke(()=>new Action(()=> {
                        string strError = "";
                        if (B_REGISTED_OBJECT.CopyObjectsFromAppliationToApplication(
                            MarsMainWindow.CurrentDatabaseIdx,
                            SelectedApplication.APPLICATION_ID, TargetSelectedApplication.APPLICATION_ID, ref strError))
                        {
                            HintByMessageBox(string.Format("Copy objects from [{0}.{1}] to [{2}.{3}] finished!\r\n{4}",
                                SelectedApplication.APP_SHORT_NAME, SelectedApplication.VERSION,
                                TargetSelectedApplication.APP_SHORT_NAME, TargetSelectedApplication.VERSION,
                                strError));
                        }
                        else
                        {
                            HintByMessageBox(string.Format("Copy objects from [{0}.{1}] to [{2}.{3}] Failed with error \r\n{4}!",
                                SelectedApplication.APP_SHORT_NAME, SelectedApplication.VERSION,
                                TargetSelectedApplication.APP_SHORT_NAME, TargetSelectedApplication.VERSION,
                                strError));
                        }
                    }
                    else
                    {
                        if (!QuestionByMessageBox(string.Format("Do you want System to copy objects in 'Batch Convert List' from application [{0}] to [{1}]? \r\nNote:\r\nTotal objects are [{2}]",
                            string.Format("{0}.{1}", SelectedApplication.APP_SHORT_NAME, SelectedApplication.VERSION),
                            string.Format("{0}.{1}", TargetSelectedApplication.APP_SHORT_NAME, TargetSelectedApplication.VERSION),
                            RegisterdObjects4BatchConvert.Count), "Hint"))
                        {
                            return;
                        }
                        //Dispatcher.CurrentDispatcher.Invoke(()=>new Action(()=> {
                        string strError = "";
                        if (B_REGISTED_OBJECT.CopyObjectsFromAppliationToApplication(
                            MarsMainWindow.CurrentDatabaseIdx,
                            RegisterdObjects4BatchConvert, SelectedApplication.APPLICATION_ID, TargetSelectedApplication.APPLICATION_ID, ref strError))
                        {
                            HintByMessageBox(string.Format("Copy objects from [{0}.{1}] to [{2}.{3}] finished!\r\n{4}",
                                SelectedApplication.APP_SHORT_NAME, SelectedApplication.VERSION,
                                TargetSelectedApplication.APP_SHORT_NAME, TargetSelectedApplication.VERSION,
                                strError));
                        }
                        else
                        {
                            HintByMessageBox(string.Format("Copy objects from [{0}.{1}] to [{2}.{3}] Failed with error \r\n{4}!",
                                SelectedApplication.APP_SHORT_NAME, SelectedApplication.VERSION,
                                TargetSelectedApplication.APP_SHORT_NAME, TargetSelectedApplication.VERSION,
                                strError));
                        }
                    }
                    //}));
                }));
            }
        }

        public ICommand HighlightSelectedObjectButtonClick
        {
            get
            {
                return new DelegateCommand(() => {
                    if (SelectedObject == null)
                    {
                        HintByMessageBox("Select an object first before you want to highlight an object.");
                        return;
                    }

                    //判断 是否有指定的application开启
                    Process targetProcessId = null;
                    string strProcessName = "";
                    bool isOk = TargetApplicationIsRunning(strProcessName = SelectedApplication == null ? "" : SelectedApplication.PROCESS_IDENTIFIER, ref targetProcessId);
                    if (!isOk)
                    {
                        HintByMessageBox(string.Format("Can't find process :[{0}]", strProcessName));
                        return;
                    }


#if _NOQTP
                    string strError = "";
                    //判断是否需要注射
                    MarsGuiInjectorAgent objInjectorAgnt;
                    if (!(objInjectorAgnt = new MarsGuiInjectorAgent()).IsInjected(targetProcessId.Id, targetProcessId.ProcessName, ref strError, ref isOk))
                    {
                        //inject to target application
                        objInjectorAgnt.InjectToTargetProcess(targetProcessId.Id, ref strError, ref isOk);
                        if (!isOk)
                        {
                            HintByMessageBox(strError);
                            Logger.Error("HighlightSelectedObjectButtonClick", strError);
                            return;
                        }
                        objInjectorAgnt.IsInjected(targetProcessId.Id, targetProcessId.ProcessName, ref strError, ref isOk);
                        if (!isOk)
                        {
                            HintByMessageBox(strError);
                            Logger.Error("HighlightSelectedObjectButtonClick", strError);
                            return;
                        }
                    }
                    //已经成功注射，构建test的消息 传递到msmq中
                    //获得pegwindow消息
                    string strPegInfo = selectedObject.OBJECT_TYPE;
                    B_REGISTED_OBJECT pegObj = null;
                    if (string.Compare(strPegInfo, selectedObject.OBJECT_HAPPY_NAME) == 0)
                    {
                        pegObj = selectedObject;
                    }
                    else
                        pegObj = _registerdObject.Where(p => (string.Compare(p.OBJECT_HAPPY_NAME, p.OBJECT_TYPE) == 0) && (string.Compare(p.OBJECT_HAPPY_NAME, strPegInfo) == 0))
                                        .FirstOrDefault();
                    if (pegObj == null)
                    {
                        HintByMessageBox(string.Format("Cant find parent window[{0}] for [{1}]", strPegInfo, selectedObject.OBJECT_HAPPY_NAME));
                        return;
                    }
                    MARSDealResult dealResult = new MARSDealResult();
                    Dictionary<string, string> pegDic = new Dictionary<string, string>(),
                        objDic = new Dictionary<string, string>();
                    isOk = ObjectInfoAnlyst.AlystObjectQuickAccessToPegAndObj(pegObj.QUICK_ACCESS,
                        selectedObject.QUICK_ACCESS, ref pegDic, ref objDic, ref strError);
                    string strObjTyp = selectedObject.T_GUI_COMPONENT_TYPE_DIC == null ? null : selectedObject.T_GUI_COMPONENT_TYPE_DIC.TYPE_NAME;
                    if (string.IsNullOrEmpty(strObjTyp))
                    {
                        strObjTyp=B_GUI_COMPONENT_TYPE_DIC.GetObjectTypeById(
                            MarsMainWindow.CurrentDatabaseIdx,
                            selectedObject.TYPE_ID,ref isOk, ref strError);
                    }
                    string strPeg = pegObj == null ? "" : pegObj.OBJECT_HAPPY_NAME;
                    string strObj = selectedObject == null ? "" : selectedObject.OBJECT_HAPPY_NAME;
                    InjectorMessageAgent.DealWithKeyword_GUIOp("HIGHLIGHT", -1, pegDic, objDic, "", "", strObjTyp,
                        "", strPeg, strObj, ref strError, ref dealResult);
#endif

                });
            }
        }

        private bool TargetApplicationIsRunning(string strProcessName, ref Process targetProcessId)
        {
            try
            {
                Process[] arrP = Process.GetProcessesByName(strProcessName);
                var currentP = Process.GetCurrentProcess().SessionId;
                var px = arrP.Where(p => p.SessionId == currentP).FirstOrDefault();
                if (arrP == null)
                    return false;                
                
                targetProcessId = arrP[0];

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
            
        }

        public ObjectDatabaseViewModel(string strDBIdx)
        {
            marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            _saveCommand = new DelegateCommand(() => { Save(); });
            _clearCommand = new DelegateCommand(() => { Clear(); });
            _loadFileCommand = new DelegateCommand(() => { LoadFile(); });
            _exportToExcelCommand = new DelegateCommand<object>(this.ExportToExcel);
            objectListChangeHandler += objectListChangeImplement;
            GetApplication();
            GetControlTypeList(strDBIdx);
            GetParentList();
        }

        private string _TargetInputApplicationTxt;
        public string TargetInputApplicationTxt
        {
            get
            {
                if (TargetSelectedApplication != null)
                {
                    return _TargetInputApplicationTxt=string.Format("{0} V[{1}]", TargetSelectedApplication.APP_SHORT_NAME, TargetSelectedApplication.VERSION);
                }
                return "";
            }
            set
            {
                _TargetInputApplicationTxt = value;                
            }
        }
    
        /// <summary>
        /// For Search and 
        /// </summary>
        /// 
        private string _InputApplicationTxt;
        public string InputApplicationTxt
        {
            get
            {
                if (SelectedApplication != null)
                {
                    return string.Format("{0} V[{1}]", SelectedApplication.APP_SHORT_NAME, SelectedApplication.VERSION);
                }
                return "";
            }
            set
            {
                _InputApplicationTxt = value;
            }
        }

        public ICommand AddToBatchConvert
        {
            get
            {
                return new DelegateCommand(()=> {
                    if (this.selectedObject==null)
                    {
                        ViewModelBase.HintByMessageBox("Select one object First.");
                        return;
                    }
                    if (RegisterdObjects4BatchConvert==null)
                    {
                        RegisterdObjects4BatchConvert = new ObservableCollection<B_REGISTED_OBJECT>();
                    }
                    if (RegisterdObjects4BatchConvert.IndexOf(this.selectedObject)!=-1)
                    {
                        ViewModelBase.HintByMessageBox(string.Format("{0} aleady exists in List.", this.selectedObject.OBJECT_HAPPY_NAME ));
                        return;
                    }
                    RegisterdObjects4BatchConvert.Add(this.SelectedObject);
                });
            }
        }

        public ICommand RemoveFromBatchConvertListCommand
        {
            get {
                return new DelegateCommand(()=> {
                    if (SelectedObjects4BatchOP==null)
                    {
                        ViewModelBase.HintByMessageBox("Select one object First.");
                        return;
                    }
                    RegisterdObjects4BatchConvert.Remove(SelectedObjects4BatchOP);
                });
            }
        }

        public ICommand ClearConvertObjectsList
        {
            get
            {
                return new DelegateCommand(()=> {
                    if (RegisterdObjects4BatchConvert != null)
                        RegisterdObjects4BatchConvert.Clear();
                });
            }
        }

        public DelegateCommand<object> ExportToExcelCommand
        {
            get
            {
                return _exportToExcelCommand;
            }

            set
            {
                _exportToExcelCommand = value;
            }
        }

        public OnObjectListChangeEvent objectListChangeHandler = null;
        public void objectListChangeImplement(int iMode, int iInsertPos = -1, object oData=null)
        {
            if (iMode == 1)//update
            {
                if (oData == null) return;
                this.SelectedObject = (B_REGISTED_OBJECT)oData;
                if (this.RegisterdObject != null)
                {
                    ///应该不是空
                    /// 
                    B_REGISTED_OBJECT targetObj = this.RegisterdObject.Where(p => p.OBJECT_ID == this.selectedObject.OBJECT_ID).FirstOrDefault();
                    if (targetObj!=null)
                    {
                        this.SelectedObject.CopyDataToBobjWithoutKey(targetObj);                        
                    }
                }
                RaisePropertyChanged("RegisterdObject");
                RaisePropertyChanged("SelectedObject");
                return;
            }
            if (iMode ==2 || iMode==3)
            {
                //增加数据 
                if (oData == null) return;
                if (!(oData is B_REGISTED_OBJECT)) return;
                if (_registerdObject==null)
                {
                    _registerdObject = new ObservableCollection<B_REGISTED_OBJECT>(new List<B_REGISTED_OBJECT>() {(B_REGISTED_OBJECT)oData });
                }
                else
                {
                    if ((_registerdObject.Count>=iInsertPos)&&(iInsertPos>0))
                    {
                        _registerdObject.Insert(iInsertPos, (B_REGISTED_OBJECT)oData);
                    }
                    else
                    {
                        _registerdObject.Add((B_REGISTED_OBJECT)oData);
                    }
                }
                RaisePropertyChanged("RegisterdObject");
                return;
            }

            
        }

        public ObservableCollection<B_REGISTED_OBJECT> RegisterdObject
        {
            get
            {
                return _registerdObject;
            }
            set
            {
                //_registerdObject = value;
                if (value != null)
                    _registerdObject = new ObservableCollection<B_REGISTED_OBJECT>(value.OrderBy(x => x.OBJECT_HAPPY_NAME));
                else
                    _registerdObject = value;

               
                RaisePropertyChanged("RegisterdObject");
            }
        }

        public string searchString = "SWAP";

        public  bool UserFilter(object item)
        {
            if (String.IsNullOrEmpty(searchString))
                return true;
            else
                if (((item as B_REGISTED_OBJECT).OBJECT_HAPPY_NAME == null)) return false;
                else
                    return ((item as B_REGISTED_OBJECT).OBJECT_HAPPY_NAME.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private B_REGISTERED_APPS _TargetSelectedApplication;
        public B_REGISTERED_APPS TargetSelectedApplication
        {
            get
            {
                return _TargetSelectedApplication;
            }
            set
            {
                _TargetSelectedApplication = value;                
                RaisePropertyChanged("TargetSelectedApplication");
            }
        }

        public B_REGISTERED_APPS SelectedApplication
        {
            get
            {
                return _selectedApplication;
            }
            set
            {
                _selectedApplication = value;
                applicationId = value.APPLICATION_ID;
                RaisePropertyChanged("SelectedApplication");
                App.Current.Dispatcher.Invoke(() => ObjectList(value.APPLICATION_ID));
            }
        }

        public ICommand LoadFileCommand
        {
            get { return _loadFileCommand; }
            set { _loadFileCommand = value; }
        }

        public ICommand SaveCommand
        {
            get
            {
                return _saveCommand;
            }

            set
            { }

        }

        public ICommand ClearCommand
        {
            get
            {
                return _clearCommand;
            }

            set
            { }

        }

        public ObservableCollection<B_REGISTERED_APPS> RegisterdApplication
        {
            get
            {
                return _registerdApplication;
            }
            set
            {
                _registerdApplication = value;
                RaisePropertyChanged("RegisterdApplication");
            }
        }

        public List<B_GUI_COMPONENT_TYPE_DIC> TypeList
        {
            get
            {
                return _typeList;
            }
            set
            {
                _typeList = value;
                RaisePropertyChanged("TypeList");
            }
        }

        public ObservableCollection<string> Pegwindow
        {
            get
            {
                return _pegwindow;
            }
            set
            {
                _pegwindow = value;
                RaisePropertyChanged("Pegwindow");
            }
        }

        public string ObjectdatabaseControlStatus
        {
            get
            {
                return objectdatabaseControlStatus;
            }
            set
            {
                objectdatabaseControlStatus = value;
                RaisePropertyChanged("ObjectdatabaseControlStatus");
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                RaisePropertyChanged("Name");
            }
        }

        public string EnumType
        {
            get
            {
                return _enumType;
            }
            set
            {
                _enumType = value;
                RaisePropertyChanged("EnumType");
            }
        }

        public string InternalName
        {
            get
            {
                return _internalName;
            }
            set
            {
                _internalName = value;
                RaisePropertyChanged("InternalName");
            }
        }

        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                _description = value;
                RaisePropertyChanged("Description");
            }
        }

        public string PegwindowStatus
        {
            get
            {
                return _pegwindowStatus;
            }
            set
            {
                _pegwindowStatus = value;
                RaisePropertyChanged("PegwindowStatus");
            }
        }

        public string ApplicationStatus
        {
            get
            {
                return _applicationStatus;
            }
            set
            {
                _applicationStatus = value;
                RaisePropertyChanged("ApplicationStatus");
            }
        }

        public B_GUI_COMPONENT_TYPE_DIC TypeStatus
        {
            get
            {
                return _typeStatus;
            }
            set
            {
                _typeStatus = value;
                RaisePropertyChanged("TypeStatus");
            }
        }

        public List<string> validationErrors = new List<string>();

        static readonly string[] ValidatedProperties =
        {
                "Name",
                "TypeList",
                "InternalName",
                "RegisterdApplication",
                "PegWindow",
                "Description"                
        };

        public bool IsValid
        {
            get
            {
                foreach (string property in ValidatedProperties)
                {
                    if (GetValidationError(property) != null)
                        validationErrors.Add(GetValidationError(property));
                }
                if (validationErrors.Count > 0)
                {
                    return false;
                }
                return true;
            }
        }

        private string GetValidationError(string propertyName)
        {
            string error = null;

            switch (propertyName)
            {
                case "Name":
                    error = this.ValidateName();
                    break;
                case "TypeList":
                    error = this.ValidateTypeList();
                    break;
                case "InternalName":
                    error = this.ValidateInternalName();
                    break;
                case "RegisterdApplication":
                    error = this.ValidateRegisterdApplication();
                    break;
                case "PegWindow":
                    error = this.ValidatePegWindow();
                    break;
                case "Description":
                    error = this.ValidateDescription();
                    break;
                default:
                    error = null;
                    throw new Exception("Unexpected property being validated on Service");
            }

            return error;
        }

        string ValidateName()
        {
            if (IsStringMissing(this.Name))
            {
                return "Name";
            }
            return null;
        }

        string ValidateTypeList()
        {
            if (this._typeStatus == null || IsStringMissing(this._typeStatus.TYPE_NAME))
            {
                return "Type";
            }
            return null;
        }

        string ValidateInternalName()
        {
            if (IsStringMissing(this.InternalName))
            {
                return "Internal Name";
            }
            return null;
        }

        string ValidateRegisterdApplication()
        {
            if (this._selectedApplication == null || IsStringMissing(this._selectedApplication.APP_SHORT_NAME))
            {
                return "Application";
            }
            return null;
        }

        string ValidatePegWindow()
        {
            if (IsStringMissing(this._pegwindowStatus))
            {
                return "Pegwindow";
            }
            return null;
        }

        string ValidateDescription()
        {
            if (IsStringMissing(this.Description))
            {
                return "Description";
            }
            return null;
        }

        static bool IsStringMissing(string value)
        {
            return
                String.IsNullOrEmpty(value) ||
                value.Trim() == String.Empty;
        }

        public bool Save()
        {
            if (!IsValid)
            {
                StringBuilder sbError = new StringBuilder();
                sbError.Append("Please enter valid :");

                foreach (string error in validationErrors)
                {
                    sbError.Append(error);
                    sbError.Append(" : ");
                }
                //System.Windows.MessageBox.Show(sbError.ToString(), "Object Add", MessageBoxButton.OK, MessageBoxImage.Error);
                ObjectdatabaseControlStatus = sbError.ToString();
                validationErrors.Clear();
                return false;
            }

            marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:MarsMainWindow.CurrentDatabaseIdx);
            B_REGISTED_OBJECT bRegObj = new B_REGISTED_OBJECT();
            T_REGISTED_OBJECTDTO tRegObj = new T_REGISTED_OBJECTDTO();
            try
            {
                if (!bRegObj.ObjectExists(
                    MarsMainWindow.CurrentDatabaseIdx,
                    Name, PegwindowStatus, applicationId))
                {
                    tRegObj.OBJECT_ID = bRegObj.GetObjectId(MarsMainWindow.CurrentDatabaseIdx);
#if !v_16AndUp
                    tRegObj.OBJECT_HAPPY_NAME = Name;
#endif
                    tRegObj.APPLICATION_ID = applicationId;
                    tRegObj.TYPE_ID = _typeStatus.TYPE_ID;
                    tRegObj.QUICK_ACCESS = InternalName;
                    tRegObj.OBJECT_TYPE = Pegwindow[Convert.ToInt32(PegwindowStatus)].ToString();
                    tRegObj.COMMENT = Description;
                    tRegObj.ENUM_TYPE = _enumType;
                    marsEntities.T_REGISTED_OBJECT.Add(T_REGISTED_OBJECTAssembler.ToEntity(tRegObj));
                    if (marsEntities.SaveChanges() > 0)
                    {
                        //System.Windows.MessageBox.Show("Object saved successfully", "Object Add", MessageBoxButton.OK, MessageBoxImage.Information);
                        ObjectdatabaseControlStatus = "Object saved successfully";
                        Clear();
                        return true;
                    }
                    else
                    {
                        marsEntities = null;
                        //System.Windows.MessageBox.Show("Error saving object", "Object Add", MessageBoxButton.OK, MessageBoxImage.Warning);
                        ObjectdatabaseControlStatus = "Error saving object";
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("Save", string.Format("Exception:[{0}] trace:[{1}]", e.Message,e.StackTrace),e);
            }

            return false;
        }

        public void Clear()
        {
            Name = "";
            InternalName = "";
            Description = "";
            _enumType = "";
            _selectedApplication = null;
            _typeStatus = null;
        }

        public void LoadFile()
        {

            new Thread(new ThreadStart(delegate() { LoadFileWorker(); })).Start();
            //Dispatcher.CurrentDispatcher.BeginInvoke(new Action(delegate()
            //    { LoadFileWorker(); }
            //    ),DispatcherPriority.Background);
        }
        public void LoadFileWorker()
        {
            Logger.logBegin("LoadFileWorker");
            try {
                this.IsStartedToLoad = true;

                DataSet ds = ExcelUtil.WorkbookToDataSet(ExcelFileName, "OBJ");
                try
                {
                    _errorList = new ObservableCollection<string>();
                }
                catch (Exception e)
                {
                    Logger.Error("LoadFileWorker", e.Message, e);
                }

                marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:MarsMainWindow.CurrentDatabaseIdx);


                string objectPegWindow = " ";
                int  iColIdx = 0;
                int iCnt = 0;
                foreach (DataTable dt in ds.Tables)
                {
                    Logger.Info("LoadFileWorker",string.Format("object sheet name:[{0}]",dt==null?"N/A-null":dt.TableName));
                    if (dt.Columns.Count == 0) continue;
                    iColIdx = dt.Columns.IndexOf("Object");
                    iColIdx = iColIdx < 0 ? 0 : iColIdx;
                    List<string> objectNames = new List<string>();
                    try
                    {
                        objectPegWindow = dt.TableName.Substring(0, dt.TableName.LastIndexOf("_OD"));
                    }
                    catch (Exception e)
                    {
                        Logger.Error("LoadFileWorker",UploadLog = string.Format("Wrong format of sheetName:[{0}], exception:[{1}]", dt.TableName, e.Message), e);
                        try
                        {
                            App.Current.Dispatcher.Invoke((Action)delegate () { _errorList.Add(UploadLog); });
                            RaisePropertyChanged("errorList");
                        }
                        catch (Exception ee)
                        {
                            Logger.Error("LoadFileWorker",string.Format("\t{0} Exception:[{1}]", DateTime.Now, ee.Message), ee);

                        }
                        continue;
                    }

                    foreach (DataRow row in dt.Rows)
                    {
                        // this is to ake sure same object name is not loaded twice

                        //string name = row["Object"].ToString();
                        string name = row[iColIdx].ToString();
                        UploadLog = string.Format("table:[{1}] Object:[{0}] ", name, dt.TableName);
                        if (objectNames.Contains(name))
                        {
                            Logger.Info("LoadFileWorker",UploadLog = string.Format("table:[{1}] Scipping [{0}] because it already exists", name, dt.TableName));
                            try
                            {
                                App.Current.Dispatcher.Invoke((Action)delegate () { _errorList.Add(UploadLog); });
                                RaisePropertyChanged("errorList");
                            }
                            catch (Exception e)
                            {
                                Logger.Error("LoadFileWorker",string.Format("\t{0} Exception:[{1}]", DateTime.Now, e.Message), e);

                            }
                            continue;
                        }
                        else
                            objectNames.Add(name);

                        //if (row["Expand Information"].ToString().Equals("Pegwindow") || row["Expand Information"].ToString().Equals("Mainwindow"))
                        //    objectPegWindow = row[iColIdx].ToString(); 

                        //objectPegWindow = row["Object"].ToString();

                        
                        SaveObjectToDb(row[iColIdx].ToString(),
                                        row["Identify"].ToString(),
                                        row["Expand Information"].ToString(),
                                        row["Comment"].ToString(),
                                        objectPegWindow);

                        if (iCnt++ % 100 == 0)
                            Thread.Sleep(50);
                    }

                    //int saveNum = marsEntities.SaveChanges();
                    //Logger.Info("LoadFileWorker", UploadLog = "saveNum = " + saveNum);
                    //if (saveNum > 0)
                    //{
                        //Thread.Sleep(500);
                        //System.Windows.MessageBox.Show("Object saved successfully", "Object Add", MessageBoxButton.OK, MessageBoxImage.Information);
                        //ObjectdatabaseControlStatus = "Object saved successfully";
                    //}
                    //else
                    //{
                    //    marsEntities = null;
                        //System.Windows.MessageBox.Show("Error saving object", "Object Add", MessageBoxButton.OK, MessageBoxImage.Warning);
                        //ObjectdatabaseControlStatus = "Error saving object";

                    //}

                    MarsDBGlobe_Cache.UpdateObjectsCache();
                }
                Logger.Info("LoadFileWorker", UploadLog = "Loading File finished");
            }catch(Exception e)
            {
                Logger.Error("LoadFileWorker",string.Format("Exception:[{0}] \r\nTrace:[{1}]",e.Message,e.StackTrace),e);

            }
            finally
            {
                this.IsStartedToLoad = false;
            }
        
        }

        private bool SaveObjectToDb(string objectField, string identifyField, string expandInfoField, string commentField, string objectPegWindow)
        {

            Logger.logBegin("SaveObjectToDb","objectField=" + objectField +
                            " identifyField=" + identifyField +
                            " expandInfoField=" + expandInfoField +
                            " commentField=" + commentField +
                            " objectPegWindow=" + objectPegWindow);
#if v_16AndUp
            B_REGISTED_OBJECT bRegObj = new B_REGISTED_OBJECT();
            string strError = "";
            bool isOk = bRegObj.UpdateOrCreateObject(
                MarsMainWindow.CurrentDatabaseIdx,
                objectPegWindow,objectField,applicationId, identifyField, GetTypeId(expandInfoField), commentField, ref strError);
            if (!isOk)
            {
                Logger.Error("SaveObjectToDb", string.Format("B_REGISTED_OBJECT.UpdateOrCreateObject return false, with Error :[{0}]",strError));
            }
            return isOk;
#else
            marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            B_REGISTED_OBJECT bRegObj = new B_REGISTED_OBJECT();
            T_REGISTED_OBJECTDTO tRegObj = new T_REGISTED_OBJECTDTO();
            if (!bRegObj.ObjectExists(objectField, objectPegWindow, applicationId))
            {
                Console.WriteLine("INSERTING!");
                tRegObj.OBJECT_ID = bRegObj.GetObjectId();
                tRegObj.OBJECT_HAPPY_NAME = objectField;
                tRegObj.APPLICATION_ID = applicationId;
                tRegObj.TYPE_ID = GetTypeId(expandInfoField);

                if (tRegObj.TYPE_ID == 0)
                    return false;

                tRegObj.QUICK_ACCESS = identifyField;
                tRegObj.OBJECT_TYPE = objectPegWindow;
                tRegObj.COMMENT = commentField;
                // ENUM_TYPE is not available in spreadsheet -- do not handle it here
                // if (expandInfoField.Equals("Pegwindow") == false)
                //    tRegObj.ENUM_TYPE = expandInfoField;
                marsEntities.T_REGISTED_OBJECT.Add(T_REGISTED_OBJECTAssembler.ToEntity(tRegObj));
            }
            else
            {
                Console.WriteLine("UPDATING!");
#if v_16AndUp
                B_REGISTED_OBJECT regObj = bRegObj.GetObject(objectField, objectPegWindow, applicationId);
#else
                T_REGISTED_OBJECT regObj = bRegObj.GetObject(objectField, objectPegWindow, applicationId);
#endif
                marsEntities.T_REGISTED_OBJECT.Attach(regObj);
                regObj.OBJECT_HAPPY_NAME = objectField;
                regObj.APPLICATION_ID = applicationId;
                regObj.TYPE_ID = GetTypeId(expandInfoField);

                if (regObj.TYPE_ID == 0)
                    return false;

                regObj.QUICK_ACCESS = identifyField;
                regObj.OBJECT_TYPE = objectPegWindow;
                regObj.COMMENT = commentField;

                // ENUM_TYPE is not available in spreadsheet -- do not handle it here
                // if (expandInfoField.Equals("Pegwindow") == false)
                //    tRegObj.ENUM_TYPE = expandInfoField;
              
            }
#if v_16AndUp
#else
            marsEntities.SaveChanges();
#endif
            return true;
#endif
        }

        private long GetTypeId(string expandInfoField)
        {
            var id = (from t in TypeList
                      where t.TYPE_NAME.ToLower() == expandInfoField.ToLower()
                      select t.TYPE_ID).FirstOrDefault();

            return id;
        }



        public void GetApplication()
        {
            try
            {
                RegisterdApplication = new ObservableCollection<B_REGISTERED_APPS>();
                var applications = (from c in marsEntities.T_REGISTERED_APPS
                                    orderby c.APP_SHORT_NAME
                                    select c);

                foreach (T_REGISTERED_APPS regApps in applications)
                {
                    B_REGISTERED_APPS newRegApps = new B_REGISTERED_APPS();
                    newRegApps.APPLICATION_ID = regApps.APPLICATION_ID;
                    newRegApps.APP_SHORT_NAME = regApps.APP_SHORT_NAME;
                    newRegApps.PROCESS_IDENTIFIER = regApps.PROCESS_IDENTIFIER;
                    newRegApps.RECORD_CREATE_DATE = regApps.RECORD_CREATE_DATE;
                    newRegApps.VERSION = regApps.VERSION;
                    RegisterdApplication.Add(newRegApps);
                }

            }
            catch (Exception ex)
            {
                Logger.Error("GetApplication",string.Format("exception:[{0}]",ex.Message),ex);                
            }
        }

        public void GetControlTypeList(string strDBIdx)
        {
            B_GUI_COMPONENT_TYPE_DIC objControlType = new B_GUI_COMPONENT_TYPE_DIC();
            TypeList = objControlType.GetTypeList(strDBIdx);
        }

        public void GetParentList()
        {
            B_REGISTED_OBJECT objRegObject = new B_REGISTED_OBJECT();
            List<string> lstTmp = objRegObject.GetReistedObjectsParent(MarsMainWindow.CurrentDatabaseIdx);
            _pegwindow.Clear();

            foreach (string itmParent in lstTmp)
            {
                _pegwindow.Add(itmParent);
            }
            RaisePropertyChanged("Pegwindow");
        }

        public void ObjectList(long applicationId)
        {
            B_REGISTED_OBJECT objRegObject = new B_REGISTED_OBJECT();
            List<B_REGISTED_OBJECT> regObject = objRegObject.GetReistedObjects(MarsMainWindow.CurrentDatabaseIdx, applicationId);
            RegisterdObject.Clear();
            /*
            foreach (B_REGISTED_OBJECT rObject in regObject)
            {
                RegisterdObject.Add(rObject);
            }
            */
            RegisterdObject = new ObservableCollection<B_REGISTED_OBJECT>(regObject);
        }

        //////////////////
        public void CreateCSVFile(DataTable dt, string strFilePath)
        {
            StreamWriter sw = new StreamWriter(strFilePath, false);

            int iColCount = dt.Columns.Count;
            for (int i = 0; i < iColCount; i++)
            {
                sw.Write(dt.Columns[i]);
                if (i < iColCount - 1)
                {
                    sw.Write(",");
                }
            }
            sw.Write(sw.NewLine);

            foreach (DataRow dr in dt.Rows)
            {
                for (int i = 0; i < iColCount; i++)
                {
                    if (!Convert.IsDBNull(dr[i]))
                    {
                        sw.Write(dr[i].ToString());
                    }
                    if (i < iColCount - 1)
                    {
                        sw.Write(",");
                    }
                }
                sw.Write(sw.NewLine);
            }
            sw.Close();
        }

        private void ExportToExcel(object listView)
        {
            try
            {
                if (listView != null)
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "CSV|*.CSV|Excel|*.XLS";
                    saveFileDialog.Title = "Save an excel File";
                    saveFileDialog.ShowDialog();
                    string strFilePath = "";
                    if (saveFileDialog.FileName != "")
                    {
                        strFilePath = saveFileDialog.FileName;
                        StreamWriter sw = new StreamWriter(strFilePath, false);
                        sw.Write("OBJECT_HAPPY_NAME,QUICK_ACCESS,TYPE_ID,OBJECT_TYPE,COMMENT");
                        sw.Write(sw.NewLine);
                        foreach (B_REGISTED_OBJECT objRegObj in (System.Collections.ObjectModel.ObservableCollection<Mars.Business.B_REGISTED_OBJECT>)(listView))
                        {
                            sw.Write(objRegObj.OBJECT_HAPPY_NAME + "," + objRegObj.QUICK_ACCESS + "," + objRegObj.TYPE_ID + "," + objRegObj.OBJECT_TYPE + "," + objRegObj.COMMENT);
                            sw.Write(sw.NewLine);
                        }
                        sw.Close();
                        //System.Windows.MessageBox.Show("Export completed. Please open your file from " + strFilePath.ToString());
                        objectdatabaseControlStatus = "Export completed. Please open your file from " + strFilePath.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ExportToExcel" , ex.Message,ex);
            }
        }

#region Datasource part
        private ObjectDataSourceViewModel currentObjectDataSource; 
        public ObjectDataSourceViewModel CurrentObjectDataSource
        {
            get
            {
                return currentObjectDataSource;
            }

            set
            {
                currentObjectDataSource = value;

                RaisePropertyChanged("CurrentObjectDataSource");
            }
        }
        /// <summary>
        /// 创建Datasource的信息，依据selectedObject
        /// 算法：
        /// 1，从数据库中获得存贮的xml文件，blob字段
        /// </summary>
        private void CreateObjectDataSourceInfo()
        {
            Logger.logBegin("CreateObjectDataSourceInfo",string.Format("From selected object:[id-{0}-{1}]", this.selectedObject==null?-1:this.selectedObject.OBJECT_ID
                ,this.selectedObject==null?"N/A":this.selectedObject.OBJECT_HAPPY_NAME));
            ///
            if (this.selectedObject == null) return;
            /// 从database 获得xml的byte数据
            /// 
            T_REGISTED_OBJECTDTO objTarget = B_REGISTED_OBJECT.GetObjectByIdFromDB(MarsMainWindow.CurrentDatabaseIdx, this.selectedObject.OBJECT_ID);
            ///load byte[] to xml file
            /// 
            CurrentObjectDataSource = new ObjectDataSourceViewModel(objTarget.OBJ_DATA_SRC,this.selectedObject.OBJECT_HAPPY_NAME, this.selectedObject.OBJECT_ID);                     

        }
#endregion //Datasource part
    }
}
