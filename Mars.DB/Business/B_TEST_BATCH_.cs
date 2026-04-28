using Mars.message.DataLayer;
using Mars.message.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Transactions;

namespace Mars.message.Business
{
    public class B_TEST_BATCH : T_TEST_BATCHDTO, INotifyPropertyChanged
    {
        private static MLogger Logger = MLogger.GetLogger("B_TEST_BATCH");
        #region default members
        public override Int64 BAT_ID
        {
            get
            {
                return base.BAT_ID;
            }
            set
            {
                base.BAT_ID = value;
                RaisePropertyChanged("BAT_ID");
            }
        }



        public override String BAT_NAME
        {
            get
            {
                return base.BAT_NAME;
            }
            set
            {
                base.BAT_NAME = value;
                RaisePropertyChanged("BAT_NAME");
            }

        }

        public override String BAT_DESC
        {
            get
            {
                return base.BAT_DESC;
            }
            set
            {
                base.BAT_DESC = value;
                RaisePropertyChanged("BAT_DESC");
            }
        }
        #endregion

        #region expand features

        private B_TEST_PROJECT assignedProject;
        public string PROJECT_NAME
        {
            get
            {
                return assignedProject == null ? "" : assignedProject.PROJECT_NAME;
            }
            set
            {
                if (assignedProject == null)
                    assignedProject = new B_TEST_PROJECT();
                assignedProject.PROJECT_NAME = value;
                RaisePropertyChanged("PROJECT_NAME");
            }
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        internal void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }

        }

        public ObservableCollection<B_TEST_PROJECT> getProjectList(string strDBIdx)
        {
            return B_TEST_PROJECT.getCachedProjects(strDBIdx);

        }



        private ObservableCollection<B_TEST_BATCH_DETAIL> _StoryBoardList;
        public ObservableCollection<B_TEST_BATCH_DETAIL> StoryBoardList
        {
            get
            {
                return _StoryBoardList;
            }
            set
            {

                _StoryBoardList = value == null ? new ObservableCollection<B_TEST_BATCH_DETAIL>() : value;
                RaisePropertyChanged("StoryBoardList");
            }
        }

        private B_TEST_BATCH_DETAIL selectedStoryboard;
        public B_TEST_BATCH_DETAIL SelectedStoryboard
        {
            get
            {
                return selectedStoryboard;
            }
            set
            {
                selectedStoryboard = value;
                RaisePropertyChanged("SelectedStoryboard");
            }
        }

        public static ObservableCollection<B_TEST_BATCH> LoadAllBatches(string strDBIdx, ref bool isOk, ref string strError)
        {
            Logger.logBegin("LoaddAllBatches");
            try
            {
                ObservableCollection<B_TEST_BATCH> result = new ObservableCollection<B_TEST_BATCH>();
                MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var batList = (
                                from b in dbCntx.T_TEST_BATCH
                                join dtl in dbCntx.T_TEST_BATCH_DETAIL on b.BAT_ID equals dtl.BAT_ID into b_dtl
                                from x in b_dtl.DefaultIfEmpty()
                                    //from strybrd in dbCntx.T_STORYBOARD_SUMMARY
                                    //from proj in dbCntx.T_TEST_PROJECT
                                    //where
                                    //    strybrd.STORYBOARD_ID==x.STORYOBARD_ID
                                    //&&  proj.PROJECT_ID==strybrd.ASSIGNED_PROJECT_ID
                                select new
                                {
                                    master = b,
                                    detail = x
                                }
                              );
                B_TEST_BATCH tmpObj;
                var dic = batList.GroupBy(p => p.master)
                    .ToDictionary(p => p.Key, q => q.Select(x => x.detail));
                foreach (var itm in dic.Keys)
                {
                    if (itm == null) continue;
                    result.Add(tmpObj = FromEntity(itm));

                    //tmpObj.StoryBoardList = new ObservableCollection<B_TEST_BATCH_DETAIL>(dic[itm].ToDTOs());
                    //for (int i= tmpObj.StoryBoardList.Count-1;i>=0;i--)
                    //{
                    //    if (tmpObj.StoryBoardList[i] == null) tmpObj.StoryBoardList.RemoveAt(i);
                    //}
                }
                return result;
            }
            catch (Exception e)
            {
                Logger.Error("LoaddAllBatches", strError = string.Format("Exception:{0}, stackTrace:\r\n{1}", e.Message, e.StackTrace));
                isOk = false;
                return null;
            }
            finally
            {
                Logger.logEnd("LoaddAllBatches");
            }
        }



        public bool LoadStoryBoardList(string strDBIdx, ref string strError)
        {
            if (BAT_ID == -1) return true;

            List<B_TEST_BATCH_DETAIL> lstBatchDtl = new List<B_TEST_BATCH_DETAIL>();
            try
            {
                MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                var b = (from p in dbCntx.T_TEST_BATCH_DETAIL
                         from s in dbCntx.T_STORYBOARD_SUMMARY
                         from s_dtl in dbCntx.T_PROJ_TC_MGR
                         from proj in dbCntx.T_TEST_PROJECT
                         where p.BAT_ID == BAT_ID
                         && p.STORYOBARD_ID == s.STORYBOARD_ID
                         && s_dtl.STORYBOARD_ID == s.STORYBOARD_ID
                         && proj.PROJECT_ID == s_dtl.PROJECT_ID
                         orderby p.RUN_ORDER, s_dtl.RUN_ORDER
                         select new
                         {
                             batch_dtl = p, // 1
                             storyboard = s, //1
                             storyboard_dtl = s_dtl, //n
                             project = proj //n
                         })

                        ;
                var bl = b.ToList();
                bool isNew = false;
                for (int i = 0; i < bl.Count; i++)
                {
                    var itm = bl[i];
                    if (itm == null) continue;
                    T_TEST_BATCH_DETAILDTO batchDtlTmp = itm.batch_dtl.ToDTO();
                    if (batchDtlTmp == null) continue;
                    B_TEST_BATCH_DETAIL batchDtl = FromDTO(strDBIdx, batchDtlTmp);
                    if (batchDtl == null) continue;
                    batchDtl.AssignedProject = B_TEST_PROJECT.ConverFromDTO(itm.project == null ? null : itm.project.ToDTO());
                    batchDtl.SelectedStoryboard = B_STORYBOARD_SUMMARY.FromDTO(itm.storyboard == null ? null : itm.storyboard.ToDTO());

                    B_TEST_BATCH_DETAIL inList = lstBatchDtl.Where(p => p.BAT_DTL_ID == batchDtl.BAT_DTL_ID).FirstOrDefault();
                    isNew = false;
                    if (inList == null)
                    {
                        inList = batchDtl;
                        isNew = true;
                    }
                    if (isNew)
                    {
                        inList.AssignedStoryboard = B_STORYBOARD_SUMMARY.FromDTO(itm.storyboard.ToDTO());
                        inList.AssignedStoryboardDetailList.Add(B_PROJ_TC_MGR.FromDTO(itm.storyboard_dtl.ToDTO()));
                        inList.AssignedProject = B_TEST_PROJECT.ConverFromDTO(itm.project.ToDTO());
                        inList.CollapseOrExpand = "+";
                        lstBatchDtl.Add(inList);
                    }

                }

                StoryBoardList = new ObservableCollection<B_TEST_BATCH_DETAIL>(lstBatchDtl);
                return true;
            }
            catch (Exception)
            {

                throw;
            }
        }

        private static B_TEST_BATCH_DETAIL FromDTO(string strDBIdx, T_TEST_BATCH_DETAILDTO batDtlDto)
        {
            if (batDtlDto == null) return null;
            B_TEST_BATCH_DETAIL rslt = new B_TEST_BATCH_DETAIL(strDBIdx);
            rslt.BAT_DTL_ID = batDtlDto.BAT_DTL_ID;
            rslt.BAT_ID = batDtlDto.BAT_ID;
            rslt.CREATE_TIME = batDtlDto.CREATE_TIME;
            rslt.DTL_DESC = batDtlDto.DTL_DESC;
            rslt.RUN_ORDER = batDtlDto.RUN_ORDER;

            return rslt;
        }



        public T_TEST_BATCH_DETAILDTO CreateBatchByStoryboardId(string strDBIdx, long storyboardId, ref bool isOk, ref string strError)
        {
            Logger.logBegin("CreateBatchByStoryboardId", string.Format("storyboardId:[{0}]", storyboardId));
            MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            try
            {
                var bat = (from q in dbCntx.T_STORYBOARD_SUMMARY
                           where q.STORYBOARD_ID == storyboardId
                           select q)
                          .FirstOrDefault();
                if (bat == null)
                {
                    isOk = false;
                    strError = string.Format("storyboard id is :[{0}]", storyboardId);
                    return null;
                }
                T_TEST_BATCH_DETAILDTO objBat = new T_TEST_BATCH_DETAILDTO();
                objBat.STORYOBARD_ID = storyboardId;

                throw new NotImplementedException();
            }
            catch (Exception)
            {

                throw;
            }
        }

        internal static B_TEST_BATCH FromEntity(T_TEST_BATCH entity)
        {
            if (entity == null) return null;
            B_TEST_BATCH result = new B_TEST_BATCH()
            {
                BAT_ID = entity.BAT_ID,
                BAT_NAME = entity.BAT_NAME,
                BAT_DESC = entity.BAT_DESC,
                CREATE_TIME = entity.CREATE_TIME
            };

            return result;

        }

        private const string ID_SEQ = "T_KEYWORD_SEQ"; //合用keyword的
        public static bool CreateNew(string strDBIdx, B_TEST_BATCH objNewBatch, ref string strError)
        {
            Logger.logBegin("CreateNew");
            try
            {

                if (objNewBatch == null)
                {
                    strError = "Object is null.";
                    return false;
                }
                using (var scope = new TransactionScope())
                {
                    MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);

                    objNewBatch.BAT_ID = BoHelper.GetIdBySeqName(ID_SEQ, dbCntx);
                    objNewBatch.CREATE_TIME = DateTime.Now;
                    dbCntx.Set<T_TEST_BATCH>();
                    dbCntx.T_TEST_BATCH.Add(objNewBatch.ToEntity());

                    if (objNewBatch.StoryBoardList != null)
                    {

                    }
                    dbCntx.SaveChanges();
                    scope.Complete();
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error("CreateNew", strError = string.Format("Exception when create new Batch Record:\r\n[{0}]", e.Message), e);
                return false;
            }
            finally
            {

            }

        }
    }


}
