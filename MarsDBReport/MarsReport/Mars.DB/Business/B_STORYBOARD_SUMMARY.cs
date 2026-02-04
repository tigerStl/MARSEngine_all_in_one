using Mars.DataLayer;
using Mars.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;

namespace Mars.Business
{
    public class B_STORYBOARD_SUMMARY : T_STORYBOARD_SUMMARYDTO, INotifyPropertyChanged
    {

        public override Int64 STORYBOARD_ID
        {
            get
            {
                return base.STORYBOARD_ID;
            }
            set
            {
                base.STORYBOARD_ID = value;
                RaisePropertyChanged("STORYBOARD_ID");
            }
        }


        public override Nullable<DateTime> CREATE_TIME
        {
            get
            {
                return base.CREATE_TIME;
            }
            set
            {
                base.CREATE_TIME = value;
                RaisePropertyChanged("CREATE_TIME");
            }
        }


        public override Nullable<Decimal> LATEST_VERISON
        {
            get
            {
                return base.LATEST_VERISON;
            }

            set
            {
                base.LATEST_VERISON = value;
                RaisePropertyChanged("LATEST_VERISON");
            }
        }


        public override String DESCRIPTION
        {
            get
            {
                return base.DESCRIPTION;
            }
            set
            {
                base.DESCRIPTION = value;
                RaisePropertyChanged("DESCRIPTION");
            }
        }


        public override Nullable<Int64> CREATER
        {
            get
            {
                return base.CREATER;
            }
            set
            {
                base.CREATER = value;
                RaisePropertyChanged("CREATER");
            }
        }


        public override Nullable<Int64> ASSIGNED_PROJECT_ID
        {
            get
            {
                return base.ASSIGNED_PROJECT_ID;
            }
            set
            {
                base.ASSIGNED_PROJECT_ID = value;
                RaisePropertyChanged("ASSIGNED_PROJECT_ID");
            }
        }


        public override String STORYBOARD_NAME
        {
            get
            {
                return base.STORYBOARD_NAME;
            }
            set
            {
                base.STORYBOARD_NAME = value;
                RaisePropertyChanged("STORYBOARD_NAME");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        internal void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }

        }


        private static MLogger Logger = MLogger.GetLogger(typeof(B_STORYBOARD_SUMMARY));
        public static B_STORYBOARD_SUMMARY ToBo(T_STORYBOARD_SUMMARY entity)
        {
            if (entity == null) return null;

            var bo = new B_STORYBOARD_SUMMARY();

            bo.STORYBOARD_ID = entity.STORYBOARD_ID;
            bo.CREATE_TIME = entity.CREATE_TIME;
            bo.LATEST_VERISON = entity.LATEST_VERISON;
            bo.DESCRIPTION = entity.DESCRIPTION;
            bo.CREATER = entity.CREATER;
            bo.ASSIGNED_PROJECT_ID = entity.ASSIGNED_PROJECT_ID;
            bo.STORYBOARD_NAME = entity.STORYBOARD_NAME;


            return bo;
        }

        public static B_STORYBOARD_SUMMARY GetStoryBoardInfoById(string strDBIdx, long storyboardId)
        {
            MarsEntities objEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            var query = from stb in objEntities.T_STORYBOARD_SUMMARY
                        where stb.STORYBOARD_ID == storyboardId
                        select stb;
            T_STORYBOARD_SUMMARY objSum = query.FirstOrDefault();
            if (objSum == null)
                return null;
            return B_STORYBOARD_SUMMARY.ToBo(objSum);
        }

        public List<T_STORYBOARD_SUMMARYDTO> GetStoryboardSumByProjId(string strDBIdx, long lProjId, ref bool isOk, ref string strErrorOrHint)
        {
            Logger.logBegin("GetStoryboardSumByProjId", string.Format("Project Id:[{0}]", lProjId));
            try
            {
                MarsEntities objDBCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var p = from q in objDBCntx.T_STORYBOARD_SUMMARY
                        where q.ASSIGNED_PROJECT_ID == lProjId
                        select q;
                isOk = true;

                if (p == null) return null;
                return p.ToDTOs().ToList();
            }
            catch (Exception e)
            {
                Logger.Error("GetStoryboardSumByProjId", strErrorOrHint = string.Format("Exception:[{0}]", e.Message), e);
                return null;
            }
            finally
            {
                Logger.logEnd("GetStoryboardSumByProjId");
            }
        }

        public bool Insert2DB(MarsEntities currentDBContext, ref string strError)
        {
            Logger.logBegin("Insert2DB");
            try
            {
                this.STORYBOARD_ID = BoHelper.GetTestStepsId(currentDBContext);
                currentDBContext.Set<T_STORYBOARD_SUMMARY>();
                currentDBContext.T_STORYBOARD_SUMMARY.Add(this.ToEntity());
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("Insert2DB", strError = string.Format("Exception:[{0}]", e.Message), e);
                return false;
            }
            finally
            {
                Logger.logEnd("Logger.");
            }
        }

        internal static B_STORYBOARD_SUMMARY FromDTO(T_STORYBOARD_SUMMARYDTO srcObj)
        {
            if (srcObj == null) return null;
            B_STORYBOARD_SUMMARY objResult = new B_STORYBOARD_SUMMARY();
            objResult.ASSIGNED_PROJECT_ID = srcObj.ASSIGNED_PROJECT_ID;
            objResult.CREATER = srcObj.CREATER;
            objResult.CREATE_TIME = srcObj.CREATE_TIME;
            objResult.DESCRIPTION = srcObj.DESCRIPTION;
            objResult.LATEST_VERISON = srcObj.LATEST_VERISON;
            objResult.STORYBOARD_ID = srcObj.STORYBOARD_ID;
            objResult.STORYBOARD_NAME = srcObj.STORYBOARD_NAME;

            return objResult;
        }

        private static Dictionary<long, List<B_STORYBOARD_SUMMARY>> CachedProjectId_StoryboardList = new Dictionary<long, List<B_STORYBOARD_SUMMARY>>();

        internal static ObservableCollection<B_STORYBOARD_SUMMARY> GetStoryboardListByProjectId(string strDBIdx, long lProjId)
        {
            Logger.logBegin("GetStoryboardListByProjectId", string.Format("Project Id:[{0}]", lProjId));
            if (CachedProjectId_StoryboardList.ContainsKey(lProjId))
                return new ObservableCollection<B_STORYBOARD_SUMMARY>(CachedProjectId_StoryboardList[lProjId]);

            try
            {
                B_STORYBOARD_SUMMARY storyboards = new B_STORYBOARD_SUMMARY();
                bool isOk = false;
                string strError = "";
                var lstStoryboard = storyboards.GetStoryboardSumByProjId(strDBIdx,lProjId, ref isOk, ref strError);
                List<B_STORYBOARD_SUMMARY> lstObj = new List<B_STORYBOARD_SUMMARY>();
                foreach (var itm in lstStoryboard)
                {
                    if (itm == null) continue;
                    lstObj.Add(B_STORYBOARD_SUMMARY.FromDTO(itm));
                }
                return new ObservableCollection<B_STORYBOARD_SUMMARY>(lstObj);
            }
            catch (Exception e)
            {

                Logger.Error("GetStoryboardListByProjectId", e.Message, e);
                return null;
            }
            finally
            {
                Logger.logEnd("GetStoryboardListByProjectId");
            }
        }

        public static bool CheckOrCreateDefaultStoryboardByCnn(DbConnection cnn, ref string strError)
        {
            string strSqlSearch = "SELECT STORYBOARD_ID FROM T_STORYBOARD_SUMMARY WHERE STORYBOARD_ID=-1 ";
            try
            {
                using (DbCommand dbCmmd = cnn.CreateCommand())
                {
                    dbCmmd.CommandText = strSqlSearch;
                    if (dbCmmd.ExecuteScalar() == null)
                    {
                        string strSqlInsertDefault = @"INSERT INTO T_STORYBOARD_SUMMARY (STORYBOARD_ID, CREATE_TIME, LATEST_VERISON, DESCRIPTION, CREATER, ASSIGNED_PROJECT_ID,STORYBOARD_NAME)" +
                                                      "VALUES(-1, SYSDATE, 0, 'DEFAULT CONTAINER FOR DELETED ITEMS',-1,-1, 'MARS DELETED ITEM CONTAINER')";
                        dbCmmd.CommandText = strSqlInsertDefault;
                        int iCnt = dbCmmd.ExecuteNonQuery();
                        if (iCnt != 1)
                        {
                            Logger.Error("CheckOrCreateDefaultStoryboardByCnn", strError = string.Format("can't insert default T_STORYBOARD_SUMMARY with return count:[{0}]", iCnt));
                            return false;
                        }
                        return true;
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.Error("CheckOrCreateDefaultStoryboardByCnn", strError = e.Message, e);
                return false;
            }
        }
    }
}
