using Mars.Business;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.ViewModel.storyboard.batch
{
    

    public class StoryboardBatchModel:ViewModelBase
    {
        private static MLogger logger = MLogger.GetLogger(typeof(MLogger));
       
        public StoryboardBatchModel()
        {
            LoadAllBatchs();
        }
        #region commands
        public DelegateCommand InsertCommand
        {
            get
            {
                return new DelegateCommand(() => {
                    string strError = "";
                    if (B_TEST_BATCH.CreateNew(MarsMainWindow.CurrentDatabaseIdx, _currentBatch,ref strError))
                    {
                        HintByMessageBox(string.Format("Create an new batch:[{0}] successfully", _currentBatch.BAT_NAME));
                    }
                    else
                    {
                        HintByMessageBox(string.Format("Create an new batch:[{0}] failed with error:\r\n[{1}]", _currentBatch.BAT_NAME,strError));
                    }
                });
            }
        }

        public DelegateCommand DeleteCommand
        {
            get
            {
                return new DelegateCommand(()=> { });
            }
        }

        public DelegateCommand SaveCommand
        {
            get
            {
                return new DelegateCommand(()=> {
                    if (CurrentBatch==null)
                    {
                        HintByMessageBox("Please select 0");
                    }
                });
            }
        }

        

        public DelegateCommand ExecuteCommand
        {
            get
            {
                return new DelegateCommand(()=> {
                });
            }
        }
        public DelegateCommand CleanCommand
        {
            get
            {
                return new DelegateCommand(() => {
                });
            }
        }
        #endregion //commands

        #region basic Data
        


        private ObservableCollection<B_TEST_BATCH> _allStoryBatches;
        public ObservableCollection<B_TEST_BATCH> AllStoryBatches
        {
            get
            {
                return _allStoryBatches;
            }
            set
            {
                _allStoryBatches = value;
                RaisePropertyChanged("AllStoryBatches");
            }
        }

        internal bool InsertNew(string strDBIdx, long storyboardId, int idx, ref string strError)
        {
            logger.logBegin("InsertNew",string.Format("storyboardId:[{0}] idx:[{1}]", storyboardId, idx));

            B_STORYBOARD_SUMMARY objStoryboard = B_STORYBOARD_SUMMARY.GetStoryBoardInfoById(strDBIdx, storyboardId);

            bool isOk = false;            
            B_TEST_BATCH bat = new B_TEST_BATCH();
            //B_TEST_BATCH newBat = bat.GetBatchByStoryboardId(storyboardId, ref isOk, ref strError);

            return false;
        }

        private B_TEST_BATCH _currentBatch=new B_TEST_BATCH();
        public B_TEST_BATCH CurrentBatch
        {
            get
            {
                return _currentBatch;
            }
            set
            {
                _currentBatch = value;
                
                RaisePropertyChanged("CurrentBatch");
                string strError = "";
                if (_currentBatch != null)
                {
                    if (!_currentBatch.LoadStoryBoardList(MarsMainWindow.CurrentDatabaseIdx, ref strError))
                    {
                        HintByMessageBox(string.Format("Mars Can't load the storyboard list for batch [{0}], Error:\r\n{1}", _currentBatch.BAT_NAME, strError));
                        return;
                    }
                }

            }
        }

        #endregion

        #region methods
        private void LoadAllBatchs()
        {
            string strError = "";
            bool isOk = false;
            AllStoryBatches = B_TEST_BATCH.LoadAllBatches(MarsMainWindow.CurrentDatabaseIdx, ref isOk, ref strError);
        }

        internal void RefreshCurrentBatchRunOrder()
        {
            if (CurrentBatch == null) return;
            for (int i=0;i<CurrentBatch.StoryBoardList.Count;i++)
            {
                if (CurrentBatch.StoryBoardList[i] == null) continue;
                if (CurrentBatch.StoryBoardList[i].RUN_ORDER!=i+1)
                {
                    CurrentBatch.StoryBoardList[i].RUN_ORDER = i + 1;
                }
            }
        }
        #endregion
    }
}
