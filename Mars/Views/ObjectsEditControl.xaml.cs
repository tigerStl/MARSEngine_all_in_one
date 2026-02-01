using Mars.ViewModel;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Mars.Business;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Mars.Dialog;
using Mars.Views.baseView;

namespace Mars.Views
{
    /// <summary>
    /// Interaction logic for ObjectDatabaseAddControl.xaml
    /// </summary>
    public partial class ObjectsEditControl : MarsBaseViewControl
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(ObjectsEditControl));
        public ObjectsEditControl()
        {
            InitializeComponent();
            InitData();
            //this.DataContext = new ObjectDatabaseViewModel();
            Title = "Object Editor";
        }

        private ObservableCollection<B_REGISTERED_APPS> applicationList;
        public ObservableCollection<B_REGISTERED_APPS> RegisterdApplication
        {
            get { return applicationList; }
        }

        private ObservableCollection<B_GUI_COMPONENT_TYPE_DIC> objectTypeList;
        public ObservableCollection<B_GUI_COMPONENT_TYPE_DIC> TypeList
        {
            get { return objectTypeList; }
            set {
                objectTypeList = value;                
            }
        }
        private ObservableCollection<B_REGISTED_OBJECT> pegwindows;
        public ObservableCollection<B_REGISTED_OBJECT> Pegwindows
        {
            get { return pegwindows; }
            set {
                pegwindows = value;
                RaisePropertyChanged("Pegwindows");
            }
        }

        public OnObjectListChangeEvent ObjectListIsChangedHandle = null;

        //
        private List<string> testEnums;
        public List<string> TestEnumList
        {
            get { return testEnums; }
            set
            {
                testEnums = value;
                RaisePropertyChanged("TestEnumList");
            }
        }

        //


        //public event PropertyChangedEventHandler PropertyChanged;
        //internal void RaisePropertyChanged(string prop)
        //{
        //    if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        //}

        private void InitData()
        {
            applicationList = B_REGISTERED_APPS.GetCacheApps(MarsMainWindow.CurrentDatabaseIdx);
            cbxApplication.ItemsSource = RegisterdApplication;

            objectTypeList = B_GUI_COMPONENT_TYPE_DIC.GetObjectTypeListEx(MarsMainWindow.CurrentDatabaseIdx);
            cbxType.ItemsSource = objectTypeList;
            if (DataContext is B_REGISTED_OBJECT)
            {
                pegwindows = B_REGISTED_OBJECT.GetPegwindowByAppId(MarsMainWindow.CurrentDatabaseIdx, ((B_REGISTED_OBJECT)this.DataContext).APPLICATION_ID);
                RaisePropertyChanged("Pegwindows");
            }

            testEnums = B_TEST_ENUM.GetDistinctEnumList(MarsMainWindow.CurrentDatabaseIdx);
         //   cbxEnumType.ItemsSource = this.testEnums;
           
            
            // todo cbxType.ItemsSource = objectTypeList;
            
        }

        private void ValidationError(object sender, ValidationErrorEventArgs e)
        {
            if (e.Action == ValidationErrorEventAction.Added)
            {
                ((Control)sender).ToolTip = e.Error.ErrorContent.ToString();
            }
            else
            {
                ((Control)sender).ToolTip = "";
            }
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Logger.logBegin("UserControl_DataContextChanged");
            if (!(e.NewValue is Mars.Business.B_REGISTED_OBJECT)) return;
            if (e.NewValue == null) return;
            DealWithNewDataContextChange((Mars.Business.B_REGISTED_OBJECT)e.NewValue);
        }

        

        private void DealWithNewDataContextChange(B_REGISTED_OBJECT objCurrentObj)
        {
            Logger.Info("DealWithNewDataContextChange",string.Format("ObjectName:[{0}]",objCurrentObj==null?"":objCurrentObj.OBJECT_HAPPY_NAME));
            if (DataContext is B_REGISTED_OBJECT)
            {
                pegwindows = B_REGISTED_OBJECT.GetPegwindowByAppId(MarsMainWindow.CurrentDatabaseIdx, ((B_REGISTED_OBJECT)this.DataContext).APPLICATION_ID);
                RaisePropertyChanged("Pegwindows");
                cbxPegwindow.ItemsSource = Pegwindows;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {

            Logger.Info("Button_Click",string.Format("button:[{0}]",sender.ToString()));
            this.txtError.Text = ""; 
            if (!(sender is Button)) return;
            string strError = "";
            bool isValidate = false;
            if (this.DataContext==null)
            {
                ObjectDatabaseViewModel.HintByMessageBox("No object is assigned.", "ERROR");
                return;
            }
            isValidate = CheckDataSettingValidate(ref strError);
            switch ((string)((Button)sender).Tag)
            {
                case "DELETE":
                    string strObjName = "";
                    if (System.Windows.Forms.DialogResult.Yes!=  ViewModelBase.QuestionsByMessageBox(string.Format("Delete object [{0}] for [{1}] ?",
                        strObjName=((B_REGISTED_OBJECT)this.DataContext).OBJECT_HAPPY_NAME,
                        ((B_REGISTED_OBJECT)this.DataContext).APPLICATION_ID),
                        "Question",
                        System.Windows.Forms.MessageBoxButtons.YesNo))
                    {
                        return;
                    }
                    
                    if (!B_REGISTED_OBJECT.DeleteObject(MarsMainWindow.CurrentDatabaseIdx, ((B_REGISTED_OBJECT)this.DataContext).OBJECT_ID, ((B_REGISTED_OBJECT)this.DataContext).APPLICATION_ID, ref strError))
                    {
                        ViewModelBase.HintByMessageBox(string.Format("Can't delete object [{0}-for application {1}], with Errors:\r\n{2}", strObjName,
                            ((B_REGISTED_OBJECT)this.DataContext).APPLICATION_ID,
                            strError));

                    }
                    else
                    {
                        ViewModelBase.HintByMessageBox(string.Format(" Have object [{0}-for application {1}] deleted!", strObjName,
                            ((B_REGISTED_OBJECT)this.DataContext).APPLICATION_ID)) ;
                        if (this.ObjectListIsChangedHandle!=null)
                        {
                            //this.ObjectListIsChangedHandle()
                        }
                    }
                    return;

                case "INSERT":
                    
                    //B_REGISTED_OBJECT.insertNewObject();
                    if (isValidate)
                    {
                        isValidate = B_REGISTED_OBJECT.InsertObjectWithTransaction(MarsMainWindow.CurrentDatabaseIdx, (B_REGISTED_OBJECT)this.DataContext,ref strError);
                        if (isValidate && this.ObjectListIsChangedHandle!=null)
                        {
                            this.ObjectListIsChangedHandle(3, -1, this.DataContext);
                        }
                    }
                    break;
                case "UPDATE":
                    if (isValidate)
                    {
                        isValidate = B_REGISTED_OBJECT.UpdateObject(MarsMainWindow.CurrentDatabaseIdx, (B_REGISTED_OBJECT)this.DataContext,ref strError);
                        if (isValidate && this.ObjectListIsChangedHandle!=null)
                        {
                            this.ObjectListIsChangedHandle(1,-1, (B_REGISTED_OBJECT)this.DataContext);
                        }
                    }
                    break;
                case "INSERT_PEG":
                    if (isValidate)
                    {
                        isValidate = B_REGISTED_OBJECT.InsertObject(MarsMainWindow.CurrentDatabaseIdx, (B_REGISTED_OBJECT)this.DataContext, ref strError,true);
                        string strCmp = ((B_REGISTED_OBJECT)this.DataContext).OBJECT_HAPPY_NAME;
                        strCmp = string.IsNullOrEmpty(strCmp) ? "" : strCmp;
                        if (isValidate)
                        {
                            //改变item source
                            ObservableCollection<B_REGISTED_OBJECT> lstOfItmsource = (ObservableCollection<B_REGISTED_OBJECT>)cbxPegwindow.ItemsSource;
                            int idx = 0;
                            //将新的对象插入到该链表中
                            if (lstOfItmsource!=null)
                            {
                                if (lstOfItmsource.Count <= 1)
                                    lstOfItmsource.Add(((B_REGISTED_OBJECT)this.DataContext));
                                else {
                                    //if (((B_REGISTED_OBJECT)this.DataContext).OBJECT_ID > 0)
                                    //lstOfItmsource.Add(((B_REGISTED_OBJECT)this.DataContext));
                                    
                                    string strCurrentHpn = lstOfItmsource[idx].OBJECT_HAPPY_NAME,
                                        strNxtHpn = lstOfItmsource[idx + 1].OBJECT_HAPPY_NAME;
                                    strCurrentHpn = string.IsNullOrEmpty(strCurrentHpn) ? "" : strCurrentHpn;
                                    strNxtHpn = string.IsNullOrEmpty(strNxtHpn) ? "" : strNxtHpn;
                                    while (idx < (lstOfItmsource.Count - 1))
                                    {
                                        lstOfItmsource.OrderBy(p => p.OBJECT_HAPPY_NAME);
                                        if (strNxtHpn.CompareTo(strCmp)>=0 && strCurrentHpn.CompareTo(strCmp)<=0)
                                        {
                                            lstOfItmsource.Insert(idx, (B_REGISTED_OBJECT)this.DataContext) ;
                                            break;
                                        }
                                       
                                        strCurrentHpn = strNxtHpn ;
                                        strNxtHpn = lstOfItmsource[idx + 1].OBJECT_HAPPY_NAME;
                                        idx++;
                                    }
                                    if (idx==lstOfItmsource.Count-1)
                                    {
                                        lstOfItmsource.Add((B_REGISTED_OBJECT)this.DataContext);
                                    }

                                }
                                if (this.ObjectListIsChangedHandle!=null)
                                    this.ObjectListIsChangedHandle(2, idx, this.DataContext);
                            }
                        }
                    }
                    break;
                default:
                    strError = "None support Action.";
                    isValidate = false;
                    break;
            }
            if (!isValidate)
            {
                this.txtError.Text = strError;
                ObjectDatabaseViewModel.HintByMessageBox(
                    ((string)((Button)sender).Tag).StartsWith("INSERT") ? string.Format("Object [{0}] is NOT Inserted. error:[{1}]", ((B_REGISTED_OBJECT)this.DataContext).OBJECT_HAPPY_NAME,strError) :
                    string.Format("Object [{0}] is NOT updated.", ((B_REGISTED_OBJECT)this.DataContext).OBJECT_HAPPY_NAME),strError);
            }
            else {
                this.txtError.Text = "";
                ObjectDatabaseViewModel.HintByMessageBox(
                    ((string)((Button)sender).Tag).StartsWith("INSERT") ? string.Format("Object [{0}] is Inserted.", ((B_REGISTED_OBJECT)this.DataContext).OBJECT_HAPPY_NAME) :
                    string.Format("Object [{0}] is updated.", ((B_REGISTED_OBJECT)this.DataContext).OBJECT_HAPPY_NAME));
                    
            }
        }

        private bool CheckDataSettingValidate(ref string strError)
        {
            if (!(this.DataContext is B_REGISTED_OBJECT))
            {
                Logger.Error("CheckDataSettingValidate", strError = "DataContext is not B_REGISTED_OBJECT");
                return false;
            }
            B_REGISTED_OBJECT objToOp = (B_REGISTED_OBJECT)this.DataContext;
            if (objToOp == null)
            {
                Logger.Error("CheckDataSettingValidate",strError = "DataContext is null");
                return false;
            }

            if (string.IsNullOrWhiteSpace(objToOp.OBJECT_HAPPY_NAME) || (string.IsNullOrWhiteSpace(objToOp.QUICK_ACCESS)))
            {
                Logger.Error("CheckDataSettingValidate",strError = string.Format("HPPAYNAME:[{0}] OR RECOGNIZE:[{1}] IS NULL/EMPTY",objToOp.OBJECT_HAPPY_NAME,objToOp.QUICK_ACCESS));
                return false;
            }


            return true;
        }

        private void txtEnumType_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Console.WriteLine("CTRL F");

               // FilteredListDialog dlg = new FilteredListDialog(testEnums, txtEnumType);
                FilteredListDialog dlg = FilteredListDialog.GetInstance(testEnums, txtEnumType);

              // FilterDialogControl dlg = new FilterDialogControl();
                dlg.Show();
            }
        }
    }


}
