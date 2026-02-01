using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.ViewModel.BaseData
{
    public delegate bool AfterContentChangeEvent(string strCurrrentContentValue,int iLoopId);

    public interface IUserNamePassword
    {
        System.Security.SecureString GetPassword();
    }

    public class MarsDataSettingTabsBase : INotifyPropertyChanged
    {
        protected string strHeader;
        public string Header
        {
            get { return strHeader; }
            set
            {
                if (strHeader != value)
                {
                    strHeader = value;
                    RaisePropertyChanged("Header");
                }
            }
        }

        public AfterContentChangeEvent afterContentChangeHander = null;

        protected string strContent;
        public string Content
        {
            get { return strContent; }
            set
            {
                if (strContent != value)
                {
                    strContent = value;
                    RaisePropertyChanged("Content");
                    if (afterContentChangeHander != null)
                        afterContentChangeHander(strContent,iId);
                }
            }
        }

        private int iId = -1;
        public int Id
        {
            get
            {
                return iId;
            }
            set
            {
                if (iId != value)
                {
                    iId = value;
                    RaisePropertyChanged("Id");
                }
            }
        }

        #region Notifypropery 
        public event PropertyChangedEventHandler PropertyChanged;
        internal void RaisePropertyChanged(string prop)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        #endregion
    }
}
