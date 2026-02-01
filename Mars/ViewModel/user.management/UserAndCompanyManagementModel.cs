using Mars.Business;
using Mars.Dto;
using Mars.Utility;
//using Microsoft.Practices.Prism.Commands;
using Prism.Commands;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mars.ViewModel.user.management
{
    internal class MOBJ_V_USER_COMPANYDTO : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        V_USER_COMPANYDTO mobjDataCmpany = null;
        public V_USER_COMPANYDTO GetAssignedEntity()
        {
            return mobjDataCmpany;
        }

        public Decimal TESTER_ID {
            get
            {
                return mobjDataCmpany == null ? -1 : mobjDataCmpany.TESTER_ID;
            }
            set
            {
                if (mobjDataCmpany == null)
                    mobjDataCmpany = new V_USER_COMPANYDTO();
                    
                mobjDataCmpany.TESTER_ID = value;
                RaisePropertyChanged("TESTER_ID");
            }

        }

        
        public String TESTER_NAME_LAST {
            get
            {
                return mobjDataCmpany == null ?"" : mobjDataCmpany.TESTER_NAME_LAST;
            }
            set
            {
                if (mobjDataCmpany == null) mobjDataCmpany = new V_USER_COMPANYDTO(); 
                mobjDataCmpany.TESTER_NAME_LAST = value;
                RaisePropertyChanged("TESTER_NAME_LAST");
            }
        }

        
        public String TESTER_NAME_M {
            get
            {
                return mobjDataCmpany == null ? "" : mobjDataCmpany.TESTER_NAME_M;
            }
            set
            {
                if (mobjDataCmpany == null) mobjDataCmpany = new V_USER_COMPANYDTO();
                mobjDataCmpany.TESTER_NAME_M = value;
                RaisePropertyChanged("TESTER_NAME_M");
            }
        }

        
        public String TESTER_NAME_F {
            get
            {
                return mobjDataCmpany == null ? "" : mobjDataCmpany.TESTER_NAME_F;
            }
            set
            {
                if (mobjDataCmpany == null) mobjDataCmpany = new V_USER_COMPANYDTO();
                mobjDataCmpany.TESTER_NAME_F = value;
                RaisePropertyChanged("TESTER_NAME_F");
            }
        }

        
        public String TESTER_LOGIN_NAME {
            get
            {
                return mobjDataCmpany == null ? "" : mobjDataCmpany.TESTER_LOGIN_NAME;
            }
            set
            {
                if (mobjDataCmpany == null) mobjDataCmpany = new V_USER_COMPANYDTO();
                mobjDataCmpany.TESTER_LOGIN_NAME = value;
                RaisePropertyChanged("TESTER_LOGIN_NAME");
            }
        }

        
        public String TESTER_PWD {
            get
            {
                return mobjDataCmpany == null ? "" : mobjDataCmpany.TESTER_PWD;
            }
            set
            {
                if (mobjDataCmpany == null) mobjDataCmpany = new V_USER_COMPANYDTO();
                mobjDataCmpany.TESTER_PWD = value;
                RaisePropertyChanged("TESTER_PWD");
            }
        }

        
        public Nullable<Decimal> AVAILABLE_MARK {
            get
            {
                return mobjDataCmpany == null ? -1 : mobjDataCmpany.AVAILABLE_MARK;
            }
            set
            {
                if (mobjDataCmpany == null) mobjDataCmpany = new V_USER_COMPANYDTO();
                mobjDataCmpany.AVAILABLE_MARK = value;
                RaisePropertyChanged("AVAILABLE_MARK");
            }
        }

        
        public String TESTER_MAIL {
            get
            {
                return mobjDataCmpany == null ? "" : mobjDataCmpany.TESTER_MAIL;
            }
            set
            {
                if (mobjDataCmpany == null) mobjDataCmpany = new V_USER_COMPANYDTO();
                mobjDataCmpany.TESTER_MAIL = value;
                RaisePropertyChanged("TESTER_MAIL");
            }
        }

        
        public String TESTER_NUMBER {
            get
            {
                return mobjDataCmpany == null ? "" : mobjDataCmpany.TESTER_NUMBER;
            }
            set
            {
                if (mobjDataCmpany == null) mobjDataCmpany = new V_USER_COMPANYDTO();
                mobjDataCmpany.TESTER_NUMBER = value;
                RaisePropertyChanged("TESTER_NUMBER");
            }
        }

        
        public Decimal COMPANY_ID {
            get
            {
                return mobjDataCmpany == null ? -1 : mobjDataCmpany.COMPANY_ID;
            }
            set
            {
                if (mobjDataCmpany == null) mobjDataCmpany = new V_USER_COMPANYDTO();
                mobjDataCmpany.COMPANY_ID = value;
                RaisePropertyChanged("COMPANY_ID");
            }
        }

        
        public String TESTER_DESC {
            get
            {
                return mobjDataCmpany == null ? "" : mobjDataCmpany.TESTER_DESC;
            }
            set
            {
                if (mobjDataCmpany == null) mobjDataCmpany = new V_USER_COMPANYDTO();
                mobjDataCmpany.TESTER_DESC = value;
                RaisePropertyChanged("TESTER_DESC");
            }
        }

        
        public String COMPANY_NAME {
            get
            {
                return mobjDataCmpany == null ? "" : mobjDataCmpany.COMPANY_NAME;
            }
            set
            {
                if (mobjDataCmpany == null) mobjDataCmpany = new V_USER_COMPANYDTO();
                mobjDataCmpany.COMPANY_NAME = value;
                RaisePropertyChanged("COMPANY_NAME");
            }
        }

        
        public String COMPANY_DESC {
            get
            {
                return mobjDataCmpany == null ? "" : mobjDataCmpany.COMPANY_DESC;
            }
            set
            {
                if (mobjDataCmpany == null) mobjDataCmpany = new V_USER_COMPANYDTO();
                mobjDataCmpany.COMPANY_DESC = value;
                RaisePropertyChanged("COMPANY_DESC");
            }
        }

        
        public String COMPANY_ADDRESS {
            get
            {
                return mobjDataCmpany == null ? "" : mobjDataCmpany.COMPANY_ADDRESS;
            }
            set
            {
                if (mobjDataCmpany == null) mobjDataCmpany = new V_USER_COMPANYDTO();
                mobjDataCmpany.COMPANY_ADDRESS = value;
                RaisePropertyChanged("COMPANY_ADDRESS");
            }
        }

        
        public String COMPANY_NUMBER {
            get
            {
                return mobjDataCmpany == null ? "" : mobjDataCmpany.COMPANY_NUMBER;
            }
            set
            {
                if (mobjDataCmpany == null) mobjDataCmpany = new V_USER_COMPANYDTO();
                mobjDataCmpany.COMPANY_NUMBER = value;
                RaisePropertyChanged("COMPANY_NUMBER");
            }
        }

        
        public Nullable<Decimal> COMPANY_AVAILABLE_MARK {
            get
            {
                return mobjDataCmpany == null ? -1 : mobjDataCmpany.COMPANY_AVAILABLE_MARK;
            }
            set
            {
                if (mobjDataCmpany == null) mobjDataCmpany = new V_USER_COMPANYDTO();
                mobjDataCmpany.COMPANY_AVAILABLE_MARK = value;
                RaisePropertyChanged("COMPANY_AVAILABLE_MARK");
            }
        }

      
        internal void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        internal static MOBJ_V_USER_COMPANYDTO ConvertFromDTO(V_USER_COMPANYDTO objDto)
        {
            if (objDto == null) return null;
            MOBJ_V_USER_COMPANYDTO objRslt = new MOBJ_V_USER_COMPANYDTO();
            objRslt.mobjDataCmpany = objDto;
            return objRslt;
        }
    }

    internal class UserAndCompanyManagementModel:ViewModelBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(UserAndCompanyManagementModel));
        private MOBJ_V_USER_COMPANYDTO currentUser;
        public MOBJ_V_USER_COMPANYDTO CurrentUser
        {
            get
            {
                return currentUser;
            }
            set
            {
                currentUser = value;
                RaisePropertyChanged("CurrentUser");
                RaisePropertyChanged("CanUserNameBeEdited");
            }
        }
        private ObservableCollection<MOBJ_V_USER_COMPANYDTO> userList;
        public ObservableCollection<MOBJ_V_USER_COMPANYDTO> UsersList
        {
            get
            {
                return userList;
            }
            set
            {
                userList = value;
                RaisePropertyChanged("UserList");
                companyList = new ObservableCollection<KeyValuePair<long, string>>();
                
                if (userList != null) 
                {
                    var q = (from cc in userList
                            select new { cc.COMPANY_ID, cc.COMPANY_NAME }).Distinct();

                                    
                    foreach (var itm in q.OrderBy(p=>p.COMPANY_NAME))
                    {
                        companyList.Add(new KeyValuePair<long, string>((long)itm.COMPANY_ID,itm.COMPANY_NAME));                         
                    }
                }
                RaisePropertyChanged("CompanyList");
                                 
            }
        }

        ObservableCollection<KeyValuePair<long,string>> companyList = null;
        public ObservableCollection<KeyValuePair<long, string>> CompanyList
        {
            get
            {
                return companyList;
            }
        }

        public bool CanUserNameBeEdited
        {
            get
            {
                return currentUser == null ? true : (string.Compare(currentUser.TESTER_LOGIN_NAME, "ADMIN",true)!=0);
            }
            
        }

        public UserAndCompanyManagementModel()
        {
            /// get all user list from view
            /// 
            InitAllUserInfo();
            InitCmmd();
        }
        private void InitCmmd()
        {
            saveUserInformation = new DelegateCommand<object>(saveUserInfo2DB);
            delUserInformation = new DelegateCommand(DelUserInfoFromDB);
            clearUserInformation = new DelegateCommand(clearCurrentUserInfo);
        }
        private void clearCurrentUserInfo()
        {
            Logger.Info("clearCurrentUserInfo","try to clear data");
            this.CurrentUser = new MOBJ_V_USER_COMPANYDTO();
            this.CurrentUser.TESTER_ID = -1;
            this.CurrentUser.COMPANY_ID = 1;
            this.currentUser.COMPANY_NAME = userList == null ? "" : (userList.Count > 0 ? userList[0].COMPANY_NAME : "");
        }
        private void DelUserInfoFromDB()
        {
            Logger.Info("DelUserInfoFromDB",string.Format("try to delete user info:[{0}], user id:[{1}]", 
                currentUser==null?"": currentUser.TESTER_LOGIN_NAME,
                currentUser == null ? "" : currentUser.TESTER_ID.ToString()));
            if (currentUser == null)
            {
                HintByMessageBox("No user account information, \r\nplease select user Information from List first.","Hint");
                return;
            }
            if (!QuestionByMessageBox(string.Format("Do you want to delete this user account information?\r\nUser Login Name:[{0}]", currentUser.TESTER_LOGIN_NAME), "Hint"))
                return;
            string strError = "";
            if (B_USER_COMPANY.DeleteUserAccount(MarsMainWindow.CurrentDatabaseIdx, currentUser.GetAssignedEntity(),ref strError))
            {
                HintByMessageBox(string.Format("Delete user [{0}] successfully.",currentUser.TESTER_LOGIN_NAME),"Hint");
                this.userList.Remove(currentUser);
                return;
            }
            else
            {
                HintByMessageBox(string.Format("Delete user [{0}] successfully.", currentUser.TESTER_LOGIN_NAME), "Hint");
                return;
            }
        }

        private void saveUserInfo2DB(object pwds)
        {
            object[] arrPwds = (object[])pwds;
            if (arrPwds.Length!=2)
            {
                HintByMessageBox("System error. \r\n Please Call Marquis.","Warning");
                return;
            }
            string strError = "";
            try
            {
                string strPwd1 = MarsUtilities.ConvertToUnsecureString((arrPwds[0] as PasswordBox).SecurePassword);
                string strPwd2 = MarsUtilities.ConvertToUnsecureString((arrPwds[1] as PasswordBox).SecurePassword);
                if (string.IsNullOrEmpty(strPwd1)||string.IsNullOrEmpty(strPwd2))
                {
                    HintByMessageBox("Please set password!", "Hint");
                    return;
                }
                if (string.Compare(strPwd1,strPwd2)!=0)
                {
                    HintByMessageBox("Passwords are not the same!", "Hint");
                    return;
                }
                /// check account login name is null or empty
                /// 
                if (string.IsNullOrEmpty(currentUser.TESTER_LOGIN_NAME))
                {
                    HintByMessageBox("User Login name should not be empty!", "Error");
                    return;
                }
                bool isNewUser = false;
                #region checkuser Name
                if (isNewUser = B_USER_COMPANY.IsLoginNameExists(MarsMainWindow.CurrentDatabaseIdx, this.currentUser.TESTER_LOGIN_NAME,ref strError))
                {
                    if (!QuestionByMessageBox(
                        string.Format("Account Name :[{0}] exists. \r\nDo you want to update?",this.currentUser.TESTER_LOGIN_NAME),
                        "Hint"
                        ))
                    {
                        return;
                    }
                }
                else
                {
                    if (strError.Length==0)
                    {
                        //HintByMessageBox(string.Format("User Account [{0}] exists.", this.currentUser.TESTER_LOGIN_NAME),"Hint");
                    }
                    else
                    {
                        HintByMessageBox(string.Format("Exception when Query Database: [{0}] ", strError), "Hint");
                        return;
                    }
                   
                }
                #endregion //Checkuser Name
                string strPwdEncoded = Mars.Securities.MarsEncodePwd.EncodeString(strPwd1);
                if (!isNewUser)
                {
                    if (companyList==null)
                    {
                        if (userList.Count > 0)
                            currentUser.COMPANY_ID = userList[0].COMPANY_ID;
                        else
                            currentUser.COMPANY_ID = 1;//default ;
                    }
                    else
                        currentUser.COMPANY_ID = companyList[0].Key;

                    if (B_USER_COMPANY.CreateNewUser(MarsMainWindow.CurrentDatabaseIdx, this.currentUser.GetAssignedEntity(), strPwdEncoded,ref strError))
                    {
                        HintByMessageBox("User Information is created.","Hint");
                        userList.Add(currentUser);
                        RaisePropertyChanged("UserList");
                    }
                    else
                    {
                        HintByMessageBox(string.Format("Can't save user inforamtion with Error:[{0}]",strError),"Hint");
                    }
                }else
                {
                    /// update user information
                    /// 
                    if (B_USER_COMPANY.UpdateUser(MarsMainWindow.CurrentDatabaseIdx, this.currentUser.GetAssignedEntity(), strPwdEncoded, ref strError))
                    {
                        HintByMessageBox("User Information is updated.", "Hint");
                        RaisePropertyChanged("UserList");
                    }
                    else
                    {
                        HintByMessageBox(string.Format("Can't update user inforamtion with Error:[{0}]", strError), "Hint");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("saveUserInfo2DB", strError = string.Format("Exception:[{0}]\r\n[{1}]", e.Message,e.StackTrace),e);
                HintByMessageBox(strError, "Exception");
            }
        }
        private void InitAllUserInfo()
        {
            Logger.logBegin("InitAllUserInfo");
            string strError = "";
            bool isRight = false;
            List<V_USER_COMPANYDTO> lstUser = B_USER_COMPANY.GetUserList(MarsMainWindow.CurrentDatabaseIdx, ref isRight,ref strError);
            userList = new ObservableCollection<MOBJ_V_USER_COMPANYDTO>();
            if (!isRight)
            {
                Logger.Error("InitAllUserInfo", string.Format("no user Information is find with error:[{0}]", strError));
                
                return;
            }
            foreach(var u  in lstUser)
            {
                userList.Add(MOBJ_V_USER_COMPANYDTO.ConvertFromDTO(u));
            }
            clearCurrentUserInfo();

        }

        private ICommand saveUserInformation;
        private ICommand delUserInformation;
        private ICommand clearUserInformation;

        public ICommand SaveUserInformation
        {
            get { return saveUserInformation; }
        }
        public ICommand DelUserInformation
        {
            get { return delUserInformation; }
        }
        public ICommand ClearUserInformation
        {
            get { return clearUserInformation; }
        }
    }
}
