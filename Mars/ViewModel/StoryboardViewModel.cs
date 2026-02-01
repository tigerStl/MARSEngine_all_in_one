using Mars.Business;
using Mars.DataLayer;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows;
using System.Windows.Input;

namespace Mars.ViewModel
{
    class StoryboardViewModel : ViewModelBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(StoryboardViewModel));

        private long _projectId;

        private ICommand _saveCommand;

        public ICommand SaveCommand
        {
            get { return _saveCommand; }
            set {  }
        }


        private string _storyboardName;

        public string StoryboardName
        {
            get { return _storyboardName; }
            set { _storyboardName = value; }
        }

        private string _storyboardDescription;

        public string StoryboardDescription
        {
            get { return _storyboardDescription; }
            set { _storyboardDescription = value; }
        }

        public StoryboardViewModel(string storyboardName)
        {
            // TODO: Complete member initialization
            _storyboardName = storyboardName;
        }

        public StoryboardViewModel(long projectId)
        {
            _projectId = projectId;
            _saveCommand = new DelegateCommand(() => { SaveStoryboard(); });
        }

        public bool SaveStoryboard()
        {
            Logger.logBegin("SaveStoryboard",string.Format("Storyboard:[{0}] desc:[{1}]",_storyboardName, StoryboardDescription));
            string strError = "";
            try
            {
                
                MarsTransactionMgr objTrans = new MarsTransactionMgr(MarsMainWindow.CurrentDatabaseIdx, true);
                using (var scope = new TransactionScope())
                {
                    B_STORYBOARD_SUMMARY summary = new B_STORYBOARD_SUMMARY();
                    summary.STORYBOARD_ID = BoHelper.GetTestStepsId(objTrans.CurrentDBContext);
                    summary.STORYBOARD_NAME = _storyboardName;
                    summary.ASSIGNED_PROJECT_ID = _projectId;
                    summary.DESCRIPTION = _storyboardDescription;
                    //BoHelper.SaveStoryboardSummary(summary);
                    //BoHelper.SaveChanges();
                    BoHelper objBo = new BoHelper();
                    objBo.SaveStoryboardSummaryByInst(MarsMainWindow.CurrentDatabaseIdx, summary, objTrans.CurrentDBContext);
                    int iCnt = objTrans.CurrentDBContext.SaveChanges();
                    //objBo.SaveChangesByInst();

                    scope.Complete();
                    
                    Logger.Info("SaveStoryboard", strError=string.Format("Save Sucessfully. Totally [{0}] record(s) is/are updated/inserted.", iCnt));
                    HintByMessageBox(strError,"Hint");
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.Error("SaveStoryboard",strError=string.Format("Exception:[{0}] stackTrace:{1}", e.Message,e.StackTrace));
                HintByMessageBox(strError, "Error");
                return false;
            }finally
            {
                Logger.logEnd("SaveStoryboard");
            }
        }

        public long CreateStoryboard()
        {
            //Console.WriteLine("storyboard " + StoryboardName + " description " + StoryboardDescription);
            B_STORYBOARD_SUMMARY summary = new B_STORYBOARD_SUMMARY();
            BoHelper objBo = new BoHelper();

            summary.STORYBOARD_ID = objBo.getTestStepsId(MarsMainWindow.CurrentDatabaseIdx);
            summary.STORYBOARD_NAME = _storyboardName;
            summary.ASSIGNED_PROJECT_ID = _projectId;
            summary.DESCRIPTION = _storyboardDescription;

            
            objBo.SaveStoryboardSummaryByInst(MarsMainWindow.CurrentDatabaseIdx, summary);
            objBo.SaveChangesByInst(MarsMainWindow.CurrentDatabaseIdx);
            //BoHelper.SaveStoryboardSummary(summary);
            //BoHelper.SaveChanges();
            return summary.STORYBOARD_ID;
        }
       
    }
}
