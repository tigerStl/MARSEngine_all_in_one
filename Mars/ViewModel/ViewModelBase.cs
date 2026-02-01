using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.ComponentModel;
using Route2NSEx.src.Marquis.systemUtil;

namespace Mars.ViewModel
{
    public class ViewModelBase : INotifyPropertyChanged
    {

        private static MLogger Logger = MLogger.GetLogger(typeof(ViewModelBase));
        //basic ViewModelBase
        internal void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        internal void OnPropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        //Extra Stuff, shows why a base ViewModel is useful
        bool? _CloseWindowFlag;
        public bool? CloseWindowFlag
        {
            get { return _CloseWindowFlag; }
            set
            {
                _CloseWindowFlag = value;
                RaisePropertyChanged("CloseWindowFlag");
            }
        }

        public virtual void CloseWindow(bool? result = true)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                CloseWindowFlag = CloseWindowFlag == null 
                    ? true 
                    : !CloseWindowFlag;
            }));
        }

        public object AssignedGuiObj;

        public static void HintByMessageBox(string strInfo, string strHint="Message")
        {
            Dispatcher.CurrentDispatcher.Invoke(
                () =>
                {
                    try
                    {
                        if (!App.Current.MainWindow.IsActive)
                            App.Current.MainWindow.Activate();
                    }
                    catch (Exception e)
                    {
                        Logger.Warnning("HintByMessageBox", string.Format("Exception:[{0}]", e.Message));
                    }
                    System.Windows.Forms.MessageBox.Show(strInfo, strHint,
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Information);

                }
                );
        }

        public static bool QuestionByMessageBox(string strInfo, string strHint )
        {
            return System.Windows.Forms.MessageBox.Show(strInfo, strHint, System.Windows.Forms.MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.OK;
        }

        public static System.Windows.Forms.DialogResult QuestionsByMessageBox(string strInfo, string strHint, System.Windows.Forms.MessageBoxButtons btns)
        {
            return System.Windows.Forms.MessageBox.Show(strInfo, strHint, btns);
        }   

        protected string OpenFileBrowserAndReturnFileFile(string strFilter)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = strFilter;

            if (dlg.ShowDialog()??false)
            {
                return dlg.FileName;
            }
            else return null;
        }
        
    }

    


    public class ComboBoxItemString
    {
        public string ValueString { get; set; }
    }

}
