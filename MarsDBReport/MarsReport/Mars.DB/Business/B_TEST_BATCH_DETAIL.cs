using Mars.DataLayer;
using Mars.Dto;
using Mars.Model;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Mars.Business
{
    public class B_TEST_BATCH_DETAIL : T_TEST_BATCH_DETAILDTO, INotifyPropertyChanged
    {

        private static MLogger Logger = MLogger.GetLogger(typeof(MLogger));

        public event PropertyChangedEventHandler PropertyChanged;

        private string currentDBIdx = null;
        public B_TEST_BATCH_DETAIL(string strDBIdx)
        {
            currentDBIdx = strDBIdx;
        }

        internal void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }

        }


        #region basic members
        public override Int64 BAT_DTL_ID
        {
            get
            {
                return base.BAT_DTL_ID;
            }
            set
            {
                base.BAT_DTL_ID = value;
                RaisePropertyChanged("BAT_DTL_ID");
            }
        }

        public override long BAT_ID
        {
            get => base.BAT_ID;
            set
            {
                base.BAT_ID = value;
                RaisePropertyChanged("BAT_ID");
            }
        }

        public override string DTL_DESC
        {
            get => base.DTL_DESC;
            set
            {
                base.DTL_DESC = value;
                RaisePropertyChanged("DTL_DESC");
            }
        }

        public override long RUN_ORDER
        {
            get => base.RUN_ORDER;
            set
            {
                base.RUN_ORDER = value;
                RaisePropertyChanged("RUN_ORDER");
            }
        }

        public override long STORYOBARD_ID
        {
            get => base.STORYOBARD_ID;
            set
            {
                base.STORYOBARD_ID = value;
                RaisePropertyChanged("STORYOBARD_ID");
            }
        }

        public override long T_TEST_BATCH_BAT_ID
        {
            get => base.T_TEST_BATCH_BAT_ID;
            set
            {
                base.T_TEST_BATCH_BAT_ID = value;
                RaisePropertyChanged("T_TEST_BATCH_BAT_ID");
            }
        }
        #endregion

        private string _CollapseOrExpand = "-";
        public string CollapseOrExpand
        {
            get
            {
                return _CollapseOrExpand;
            }
            set
            {
                _CollapseOrExpand = value;
                RaisePropertyChanged("CollapseOrExpand");
            }
        }

        public ObservableCollection<B_TEST_PROJECT> getAllProjects(string strDBIdx)
        {
                return B_TEST_PROJECT.getCachedProjects(strDBIdx);
            
        }

        private B_TEST_PROJECT assignedProject;
        public B_TEST_PROJECT AssignedProject
        {

            get => assignedProject;
            set
            {
                assignedProject = value;
                if (assignedProject != null)
                {
                    try
                    {
                        MarsEntities dbCntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB: currentDBIdx);
                        var story = (from s in dbCntx.T_STORYBOARD_SUMMARY
                                     from p in dbCntx.T_TEST_PROJECT
                                     where s.ASSIGNED_PROJECT_ID == assignedProject.PROJECT_ID
                                     && p.PROJECT_ID == s.ASSIGNED_PROJECT_ID
                                     orderby p.PROJECT_NAME
                                     select s)
                                    .ToList();
                        ObservableCollection<B_STORYBOARD_SUMMARY> tmpassignedStoryBoard = new ObservableCollection<B_STORYBOARD_SUMMARY>();
                        foreach (var itm in story)
                        {
                            if (itm == null) continue;
                            B_STORYBOARD_SUMMARY tmp = new B_STORYBOARD_SUMMARY();
                            tmp.ASSIGNED_PROJECT_ID = itm.ASSIGNED_PROJECT_ID;
                            tmp.CREATER = itm.CREATER;
                            tmp.DESCRIPTION = itm.DESCRIPTION;
                            tmp.LATEST_VERISON = itm.LATEST_VERISON;
                            tmp.STORYBOARD_ID = itm.STORYBOARD_ID;
                            tmp.STORYBOARD_NAME = itm.STORYBOARD_NAME;
                            if (tmpassignedStoryBoard == null)
                                tmpassignedStoryBoard = new ObservableCollection<B_STORYBOARD_SUMMARY>();
                            tmpassignedStoryBoard.Add(tmp);
                        }
                        AssignedStoryBoardForProj = tmpassignedStoryBoard;
                    }
                    catch (Exception e)
                    {
                        Logger.Error("AssignedProject", string.Format("Exception:[{0}]", e.Message), e);
                        AssignedStoryBoardForProj = null;
                    }
                }
                RaisePropertyChanged("AssignedProject");
            }
        }

        public static B_TEST_BATCH_DETAIL FromStoryboardId(string strDBIdx, long storyBoardId, ref bool isOk, ref string strError)
        {
            Logger.logBegin("FromStoryboardId", string.Format("storyboardId [{0}]", storyBoardId));
            try
            {
                var dbcntx = BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
                if (dbcntx == null)
                {
                    strError = "Db context is null";
                    isOk = false;
                    return null;
                }
                var o = (from p in dbcntx.T_TEST_PROJECT
                         from stry in dbcntx.T_STORYBOARD_SUMMARY
                         where stry.STORYBOARD_ID == storyBoardId
                         && stry.ASSIGNED_PROJECT_ID == p.PROJECT_ID
                         select new
                         {
                             proj = p,
                             storyboard = stry
                         }).FirstOrDefault();
                if (o == null)
                {
                    isOk = false;
                    strError = "Can't find Storyboard info and its Project info by storyboardId:" + storyBoardId;
                    return null;
                }
                if ((o.proj == null) || (o.storyboard == null))
                {
                    isOk = false;
                    strError = "Project info or storybaord Info is null when mars get data for storyboard id: " + storyBoardId;
                    return null;
                }
                B_TEST_BATCH_DETAIL objResult = new B_TEST_BATCH_DETAIL(strDBIdx);
                objResult.AssignedProject = B_TEST_PROJECT.ConverFromDTO(o.proj.ToDTO());
                objResult.AssignedStoryboard = B_STORYBOARD_SUMMARY.FromDTO(o.storyboard.ToDTO());
                objResult.SelectedStoryboard = B_STORYBOARD_SUMMARY.FromDTO(o.storyboard.ToDTO());
                objResult.AssignedStoryBoardForProj = B_STORYBOARD_SUMMARY.GetStoryboardListByProjectId(strDBIdx,o.proj.PROJECT_ID);
                objResult.BAT_DTL_ID = -1;
                objResult.BAT_ID = -1;
                objResult.CREATE_TIME = DateTime.Now;
                objResult.DTL_DESC = o.storyboard.DESCRIPTION;
                objResult.RUN_ORDER = -1;

                isOk = true;

                return objResult;
            }
            catch (Exception e)
            {
                Logger.Error("FromStoryboardId", strError = string.Format("Exception:[{0}]", e.Message), e);
                isOk = false;
                return null;
            }
        }

        private ObservableCollection<B_STORYBOARD_SUMMARY> assignedStoryBoardForProj;
        public ObservableCollection<B_STORYBOARD_SUMMARY> AssignedStoryBoardForProj
        {
            get => assignedStoryBoardForProj;
            set
            {
                assignedStoryBoardForProj = value;

                RaisePropertyChanged("AssignedStoryBoardForProj");
            }
        }

        private B_STORYBOARD_SUMMARY selectedStoryboard;
        public B_STORYBOARD_SUMMARY SelectedStoryboard
        {
            get => selectedStoryboard;
            set
            {
                selectedStoryboard = value;
                RaisePropertyChanged("SelectedStoryboard");
            }
        }



        public string PROJECT_NAME
        {
            get
            {
                if (assignedProject == null) return "";
                return assignedProject.PROJECT_NAME;
            }
        }

        private ObservableCollection<B_PROJ_TC_MGR> _AssignedStoryboardDetailList = new ObservableCollection<B_PROJ_TC_MGR>();
        public ObservableCollection<B_PROJ_TC_MGR> AssignedStoryboardDetailList
        {
            get => _AssignedStoryboardDetailList;
            set
            {
                _AssignedStoryboardDetailList = value;
                RaisePropertyChanged("AssignedStoryboardList");
            }
        }

        private B_STORYBOARD_SUMMARY assignedStoryboard;
        public B_STORYBOARD_SUMMARY AssignedStoryboard
        {
            get => assignedStoryboard;
            set
            {
                assignedStoryboard = value;
                RaisePropertyChanged("AssignedStoryboard");
            }
        }

        private string _desription;
        public string BatchDetailDesc
        {
            get => _desription;
            set
            {
                _desription = value;
                RaisePropertyChanged("BatchDetailDesc");
            }
        }

    }
}
