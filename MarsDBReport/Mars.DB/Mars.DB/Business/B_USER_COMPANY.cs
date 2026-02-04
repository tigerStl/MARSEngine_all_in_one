using Mars.DataLayer;
using Mars.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

namespace Mars.Business
{
    public class B_USER_COMPANY
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(B_USER_COMPANY));
        private const string CNST_SEQ_TESTER = "SEQ_TESTER_ID";
        public static List<V_USER_COMPANYDTO> GetUserList(string strDBIdx, ref bool isRight, ref string strError)
        {
            Logger.logBegin("GetUserList");
            Mars.Model.MarsEntities objMarsCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            try
            {
                var q = from user in objMarsCntx.V_USER_COMPANY
                        orderby user.COMPANY_ID, user.TESTER_LOGIN_NAME
                        select user;
                List<V_USER_COMPANYDTO> lstRslt = V_USER_COMPANYAssembler.ToDTOs(q);

                Logger.Info("GetUserList", string.Format("Get user count:[{0}]", lstRslt.Count));
                isRight = true;
                return lstRslt;
            }
            catch (Exception e)
            {
                Logger.Error("GetUserList", strError = string.Format("Exceptions:[{0}],StackTrace:[{1}]", e.Message, e.StackTrace), e);
                isRight = false;
                return null;
            }
        }

        public static bool IsLoginNameExists(string strDBIdx, string strLogName, ref string strError)
        {
            Logger.logBegin("IsLoginNameExists");
            Mars.Model.MarsEntities objMarsCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            try
            {
                strError = "";
                var q = (from u in objMarsCntx.V_USER_COMPANY
                         where u.TESTER_LOGIN_NAME == strLogName
                         select u).FirstOrDefault();
                if (q == null)
                    return false;
                else return true;
            }
            catch (Exception e)
            {
                Logger.Error("IsLoginNameExists", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
        }

        public static bool UpdateUser(string strDBIdx, V_USER_COMPANYDTO objUserInfo, string strPwdEncoded, ref string strError)
        {
            Logger.Info("UpdateUser", string.Format("Try to update user with id:[{0}], Account Login Name:[{1}]", objUserInfo.TESTER_ID, objUserInfo.TESTER_LOGIN_NAME));
            try
            {
                objUserInfo.TESTER_PWD = strPwdEncoded;
                T_TESTER_INFODTO objTester = ConvertToTesterInfo(objUserInfo);
                objTester.CREATE_TIME = DateTime.Now;
                Mars.Model.MarsEntities objMarsCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);

                using (TransactionScope scope = new TransactionScope())
                {
                    /// get Id
                    /// 
                    //objTester.TESTER_ID = BoHelper.GetIdBySeqName(CNST_SEQ_TESTER, objMarsCntx);
                    var objSrc = (from t in objMarsCntx.T_TESTER_INFO
                                  where t.TESTER_ID == objUserInfo.TESTER_ID
                                  select t).FirstOrDefault();
                    if (objSrc == null)
                    {
                        strError = string.Format("User Id :[{0}] with account name [{1}] doesn't exists already", objSrc.TESTER_ID, objSrc.TESTER_LOGIN_NAME);
                        return false;
                    }
                    T_TESTER_INFO objTmp = objTester.ToEntity();
                    objMarsCntx.Set<T_TESTER_INFO>();

                    objMarsCntx.T_TESTER_INFO.Attach(objSrc);
                    objSrc.AVAILABLE_MARK = objTmp.AVAILABLE_MARK;
                    objSrc.COMPANY_ID = objTmp.COMPANY_ID;
                    objSrc.CREATE_TIME = objTmp.CREATE_TIME;
                    objSrc.CREATOR_NAME = objTmp.CREATOR_NAME;
                    objSrc.TESTER_DESC = objTmp.TESTER_DESC;
                    objSrc.TESTER_ID = objTmp.TESTER_ID;
                    objSrc.TESTER_LOGIN_NAME = objTmp.TESTER_LOGIN_NAME;
                    objSrc.TESTER_MAIL = objTmp.TESTER_MAIL;
                    objSrc.TESTER_NAME_F = objTmp.TESTER_NAME_F;
                    objSrc.TESTER_NAME_LAST = objTmp.TESTER_NAME_LAST;
                    objSrc.TESTER_NAME_M = objTmp.TESTER_NAME_M;
                    objSrc.TESTER_NUMBER = objTmp.TESTER_NUMBER;
                    objSrc.TESTER_PWD = objTmp.TESTER_PWD;

                    objMarsCntx.SaveChanges();
                    scope.Complete();
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("UpdateUser", strError = string.Format("Exception when Update User:[{0}]\r\nStackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        public static bool DeleteUserAccount(string strDBIdx, V_USER_COMPANYDTO objUserInfo, ref string strError)
        {
            Logger.Info("UpdateUser", string.Format("Try to delete user with id:[{0}], Account Login Name:[{1}]", objUserInfo.TESTER_ID, objUserInfo.TESTER_LOGIN_NAME));
            try
            {
                Mars.Model.MarsEntities objMarsCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                using (TransactionScope scope = new TransactionScope())
                {
                    var q = (from u in objMarsCntx.T_TESTER_INFO
                             where u.TESTER_ID == objUserInfo.TESTER_ID
                             select u).FirstOrDefault();
                    if (q == null)
                    {
                        strError = string.Format("No such account id [{0}], perhaps it is removed already.", objUserInfo.TESTER_ID);
                        return true;
                    }
                    objMarsCntx.T_TESTER_INFO.Remove(q);
                    objMarsCntx.SaveChanges();
                    scope.Complete();
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("DeleteUserAccount", string.Format("Exception:[{0}]", e.Message));
                return false;
            }
        }

        public static bool CreateNewUser(string strDBIdx, V_USER_COMPANYDTO objUserInfoCmpInfo, string strPwdEncoded, ref string strError)
        {
            Logger.Info("CreateNewUser", string.Format("Try to create new user:[{0}]", objUserInfoCmpInfo.TESTER_LOGIN_NAME));
            try
            {
                objUserInfoCmpInfo.TESTER_PWD = strPwdEncoded;
                T_TESTER_INFODTO objTester = ConvertToTesterInfo(objUserInfoCmpInfo);

                Mars.Model.MarsEntities objMarsCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                using (TransactionScope scope = new TransactionScope())
                {
                    /// get Id
                    /// 
                    objTester.TESTER_ID = BoHelper.GetIdBySeqName(CNST_SEQ_TESTER, objMarsCntx);
                    objMarsCntx.T_TESTER_INFO.Add(objTester.ToEntity());
                    int iCount = objMarsCntx.SaveChanges();
                    //Logger.Info("Write "+" "+iCount,"");
                    scope.Complete();
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("CreateNewUser", strError = string.Format("Exception when Create New User:[{0}]\r\nStackTrace:[{1}]", e.Message, e.StackTrace), e);
                return false;
            }
        }

        internal static T_TESTER_INFODTO ConvertToTesterInfo(V_USER_COMPANYDTO objUserInfoCmpInfo)
        {
            if (objUserInfoCmpInfo == null) return null;
            T_TESTER_INFODTO objResult = new T_TESTER_INFODTO();
            objResult.TESTER_ID = objUserInfoCmpInfo.TESTER_ID;
            objResult.TESTER_DESC = objUserInfoCmpInfo.TESTER_DESC;
            objResult.TESTER_LOGIN_NAME = objUserInfoCmpInfo.TESTER_LOGIN_NAME;
            objResult.TESTER_MAIL = objUserInfoCmpInfo.TESTER_MAIL;
            objResult.TESTER_NAME_F = objUserInfoCmpInfo.TESTER_NAME_F;
            objResult.TESTER_NAME_LAST = objUserInfoCmpInfo.TESTER_NAME_LAST;
            objResult.TESTER_NAME_M = objUserInfoCmpInfo.TESTER_NAME_M;
            objResult.TESTER_NUMBER = objUserInfoCmpInfo.TESTER_NUMBER;
            objResult.TESTER_PWD = objUserInfoCmpInfo.TESTER_PWD;
            objResult.CREATE_TIME = DateTime.Now;
            objResult.COMPANY_ID = objUserInfoCmpInfo.COMPANY_ID;
            return objResult;
        }


    }
}
