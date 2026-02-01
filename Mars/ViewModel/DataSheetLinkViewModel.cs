using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.Dto;
using Mars.Model;
using System.Windows.Input;
using System.Security.Principal;
using System.Windows;
using Mars.Business;
using System.Collections.ObjectModel;
using Mars.DataLayer;
using Route2NSEx.src.Marquis.systemUtil;

namespace Mars.ViewModel
{
    public class DataSheetLinkViewModel : ViewModelBase, IDataSheetLinkViewModel
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(DataSheetLinkViewModel));
        string testCaseName;
        //long testCaseId;
        
        private ICommand _saveCommand;
        //private ICommand _clearCommand;
        MarsEntities marsEntities;

        long _projectId;
        long _testSuiteId;
        long _testCaseId;

        public ObservableCollection<B_LINKED_DATA_SHEET> LinkedDataSheet { get; set; }

        public DataSheetLinkViewModel(long projectId, long testSuiteId, long testCaseId)
        {
            _projectId = projectId;
            _testSuiteId = testSuiteId;
            _testCaseId = testCaseId;

            _saveCommand = new DelegateCommand(() => { SaveDataSheetLink(); });
                       
            LinkedDataSheet = new ObservableCollection<B_LINKED_DATA_SHEET>(BoHelper.GetLinkedDataSheet(MarsMainWindow.CurrentDatabaseIdx, 
                projectId, testSuiteId, testCaseId));
        }

        public long TestCaseId
        {
            get
            {
                return _testCaseId;
            }
            set
            {
                _testCaseId = value;
                RaisePropertyChanged("TestCaseId");
            }
        }

        public string TestCaseName
        {
            get
            {
                return testCaseName;
            }
            set
            {
                testCaseName = value;
                RaisePropertyChanged("TestCaseName");
            }
        }

        

        public ICommand SaveCommand
        {
            get
            {
                return _saveCommand;
            }

            set
            {

            }

        }

     /// <summary>
     /// 该过程存在问题。暂时没有明白这个过程的逻辑。为使编译通过，无事务处理
     /// mark：需要重写过程
     /// </summary>
     /// <returns></returns>
        public bool SaveDataSheetLink()
        {
            Logger.logBegin("SaveDataSheetLink");
            try
            {

                marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:MarsMainWindow.CurrentDatabaseIdx);
                List<REL_TC_DATA_SUMMARY> relList = BoHelper.GetRelTcDataSummary(MarsMainWindow.CurrentDatabaseIdx);

                foreach (B_LINKED_DATA_SHEET linkedDataSheet in LinkedDataSheet)
                {
                    // IsSelected but not in the database -- add rel record
                    if (linkedDataSheet.IsSelected && (relList.FirstOrDefault(
                                                        x => x.TEST_CASE_ID == _testCaseId &&
                                                             x.DATA_SUMMARY_ID == linkedDataSheet.Id
                                                        ) == null))
                    {
                        VMCollCash.currentVMColl.LinkDataSet(MarsMainWindow.CurrentDatabaseIdx, linkedDataSheet.Id);
                    }

                    // is not selected but is in database -- delete it
                    else if (!linkedDataSheet.IsSelected && (relList.FirstOrDefault(
                                                        x => x.TEST_CASE_ID == _testCaseId &&
                                                             x.DATA_SUMMARY_ID == linkedDataSheet.Id
                                                        ) != null))
                    {
                        long id = relList.FirstOrDefault(x => x.TEST_CASE_ID == _testCaseId &&
                                                             x.DATA_SUMMARY_ID == linkedDataSheet.Id).ID;

                        VMCollCash.currentVMColl.UnLinkDataSet(MarsMainWindow.CurrentDatabaseIdx, linkedDataSheet.Id);
                        //BoHelper.RemoveRelTCDataSummary(id, linkedDataSheet.Id, _testCaseId);
                    }
                }
                return true;
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {

            }
        }

    }
}
