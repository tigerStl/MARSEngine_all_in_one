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
    /// Interaction logic for TestVarEditControl.xaml
    /// </summary>
    public partial class TestVarEditControl : MarsBaseViewControl, INotifyPropertyChanged
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestVarEditControl));
        public TestVarEditControl()
        {
            InitializeComponent();
            InitData();
            
            Title = "Test Variable Editor";
            //cbxVarType.ItemsSource = new ObservableCollection<string>() { "GLOBAL_VAR", "MODAL_VAR" };
        }

        private void InitData()
        {
            
        }

        //public event PropertyChangedEventHandler PropertyChanged;
        //internal void RaisePropertyChanged(string prop)
        //{
        //    if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        //}


        //private void DealWithNewDataContextChange(B_REGISTED_OBJECT objCurrentObj)
        //{
        //    Logger.Info("DealWithNewDataContextChange", string.Format("ObjectName:[{0}]", objCurrentObj == null ? "" : objCurrentObj.OBJECT_HAPPY_NAME));
        //    if (DataContext is B_REGISTED_OBJECT)
        //    {
        //        pegwindows = B_REGISTED_OBJECT.GetPegwindowByAppId(((B_REGISTED_OBJECT)this.DataContext).APPLICATION_ID);
        //        RaisePropertyChanged("Pegwindows");
        //        cbxPegwindow.ItemsSource = Pegwindows;
        //    }

        //}

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                Logger.logBegin("UserControl_DataContextChanged");
                B_SYSTEM_LOOKUP bo = null;
                
                if (e.NewValue == null)
                {
                    bo = new B_SYSTEM_LOOKUP();
                }
                else
                {
                    bo = (B_SYSTEM_LOOKUP)e.NewValue;
                }
                //if (!(e.NewValue is Mars.Business.B_SYSTEM_LOOKUP))
                //    return;

                cbxVarType.ItemsSource = new ObservableCollection<string>() { "GLOBAL_VAR", "MODAL_VAR" };

                
                //cbxBse.ItemsSource = null;
                if (bo.TABLE_NAME.Contains("GLOBAL_VAR"))
                {
                    //cbxVarType.SelectedIndex = 0;
                    cbxBse.IsEnabled = true;
                }
                else
                {
                    if (string.Compare("MODAL_VAR", bo.TABLE_NAME??"",true)==0)
                    {
                        cbxBse.ItemsSource = new ObservableCollection<string>() {"BASELINE", "COMPARE" };
                    }
                    //cbxVarType.SelectedValue = "MODAL_VAR";
                    //cbxVarType.SelectedIndex = 1;
                    cbxBse.IsEnabled = true;
                }

                //if (bo.STATUS == 1)
                //    //cbxBse.SelectedValue = "BASELINE";
                //    cbxBse.SelectedIndex = 0;
                //else
                //    //cbxBse.SelectedValue = "COMPARE";
                //    cbxBse.SelectedIndex = 1;
            }
            catch (Exception ex)
            {
                Logger.logBegin("Exception " + ex.ToString());
            }
        }

        private bool CheckDataSettingValidate(ref string strError)
        {
            if (!(this.DataContext is B_SYSTEM_LOOKUP))
            {
                Logger.Error("CheckDataSettingValidate", strError = "DataContext is not B_REGISTED_OBJECT");
                return false;
            }
            B_SYSTEM_LOOKUP objToOp = (B_SYSTEM_LOOKUP)this.DataContext;
            if (objToOp == null)
            {
                Logger.Error("CheckDataSettingValidate", strError = "DataContext is null");
                return false;
            }

            if (string.IsNullOrWhiteSpace(objToOp.DISPLAY_NAME) || 
                string.IsNullOrWhiteSpace(objToOp.FIELD_NAME) || 
                string.IsNullOrWhiteSpace(objToOp.TABLE_NAME))
            {
                Logger.Error("CheckDataSettingValidate", strError = string.Format("HPPAYNAME:[{0}] OR RECOGNIZE:[{1}] IS NULL/EMPTY", objToOp.DISPLAY_NAME, objToOp.FIELD_NAME));
                return false;
            }


            return true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            Logger.Info("Button_Click", string.Format("button:[{0}]", sender.ToString()));
            this.txtError.Text = "";
            if (!(sender is Button)) return;
            string strError = "";
            bool isValidate = false;
            B_SYSTEM_LOOKUP systemLookup = (B_SYSTEM_LOOKUP)this.DataContext;
            //if (systemLookup==null)
            //{
            //    ViewModelBase.HintByMessageBox(string.Format("no variable is selected. "));
            //    return;
            //}
            string strCmmd = (string)((Button)sender).Tag;
            if (string.Compare(strCmmd, "delete",true) != 0)
            {
                isValidate = CheckDataSettingValidate(ref strError);
                if (!isValidate)
                {
                    ViewModelBase.HintByMessageBox(string.Format("Can't [{0}] with Error:[{1}]", (string)((Button)sender).Tag, strError));
                    return;
                }
            }

            switch ((string)((Button)sender).Tag)
            {
                case "INSERT":

                    //B_REGISTED_OBJECT.insertNewObject();
                    if (isValidate)
                    {
                        systemLookup.VALUE = 1;
                        isValidate = systemLookup.InsertSelf(Mars.MarsMainWindow.CurrentDatabaseIdx,ref strError);
                        if (isValidate)
                            ViewModelBase.HintByMessageBox(string.Format("INSERT variable:s[{0}] Success!", systemLookup.FIELD_NAME));
                        else
                            ViewModelBase.HintByMessageBox(string.Format("INSERT variable:s[{0}] Failed, with Error:[{1}]"
                                , systemLookup.FIELD_NAME
                                , strError));
                    }
                    break;
                case "UPDATE":
                    if (isValidate)
                    {
                        isValidate = systemLookup.updateSelf(systemLookup, ref strError, strDBIdx: Mars.MarsMainWindow.CurrentDatabaseIdx);
                        if (isValidate)
                            ViewModelBase.HintByMessageBox(string.Format("Update variable:s[{0}] Success!", systemLookup.FIELD_NAME)) ;
                        else
                            ViewModelBase.HintByMessageBox(string.Format("Update variable:s[{0}] Failed, with Error:[{1}]"
                                , systemLookup.FIELD_NAME
                                , strError));
                    }
                    break;

                case "DELETE":                    
                        isValidate = systemLookup.deleteSelf(ref strError, Mars.MarsMainWindow.CurrentDatabaseIdx);
                        if (isValidate)
                            ViewModelBase.HintByMessageBox(string.Format("DELETE variable:s[{0}] Success!", systemLookup.FIELD_NAME));
                        else
                            ViewModelBase.HintByMessageBox(string.Format("DELETE variable:s[{0}] Failed, with Error:[{1}]"
                                , systemLookup.FIELD_NAME
                                , strError));
                    
                    break;
                default:
                    strError = "Action is not supported.";
                    isValidate = false;
                    break;
            }
            if (!isValidate)
            {
                this.txtError.Text = strError;
            }
            else
            {
                this.txtError.Text = "";
                //RaisePropertyChanged("SelectedVar");
                RaisePropertyChanged("SystemLookup");
            }
        }

        private void cbxVarType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DataContext == null) return;
            
            B_SYSTEM_LOOKUP systemLookup = (B_SYSTEM_LOOKUP)this.DataContext;

            //systemLookup.TABLE_NAME = cbxVarType.SelectedValue.ToString();
            if (string.Compare(systemLookup.TABLE_NAME, "modal_var", true) == 0)
            {
                cbxBse.IsEnabled = true;
                cbxBse.ItemsSource = new ObservableCollection<string>() { "BASELINE", "COMPARE" };
            }
            else
            {
                cbxBse.IsEnabled = false;
                cbxBse.ItemsSource = null;
            }
            
        }

        private void cbxBse_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            B_SYSTEM_LOOKUP systemLookup = (B_SYSTEM_LOOKUP)this.DataContext;
            
            if (cbxBse.SelectedValue == null) return;
            if (systemLookup == null) return;

            if (string.Compare(systemLookup.TABLE_NAME, "modal_var", true) != 0) return;

            if (cbxBse.SelectedValue.ToString().Contains("BASELINE"))
            {
                systemLookup.VALUE = 1;
                systemLookup.STATUS = 1;
            }
            else
            {
                systemLookup.VALUE = 2;
                systemLookup.STATUS = 2;
            }
        }

        private void cbxVarType_LostFocus(object sender, RoutedEventArgs e)
        {
            //RaisePropertyChanged("SelectedVar");
            //RaisePropertyChanged("SystemLookup");
        }
    }
}
    