using Mars.Business;
using Mars.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Mars.Dialog
{
    class AssignApplicationToTestCaseModel: ViewModelBase
    {

        private ObservableCollection<DataCntx_Reg_Apps> allMarsApps=null;
        public ObservableCollection<DataCntx_Reg_Apps> AllMarsApplications
        {
            get
            {
                return allMarsApps;
            }
            set
            {
                allMarsApps = value;
                RaisePropertyChanged("AllMarsApplications");
            }
        }

        private DataCntx_Reg_Apps selectedApp;
        public DataCntx_Reg_Apps SelectedMarsApplictions
        {
            get
            {
                return selectedApp;
            }
            set
            {
                selectedApp = value;
                RaisePropertyChanged("SelectedMarsApplictions");
            }
        }

        private long projectId;
        public long ProjectId
        {
            get
            {
                return projectId;
            }
            set
            {
                projectId = value;
                RaisePropertyChanged("ProjectId");
            }
        }
        private System.Windows.Window selfView = null;

        private string projectName;
        public string ProjectName
        {
            get { return projectName; }
            set { projectName = value;
                RaisePropertyChanged("ProjectName");
            }
        }

        public AssignApplicationToTestCaseModel(long projId,string projName, System.Windows.Window cntrl)
        {
            selfView = cntrl;
            ProjectId = projId;
            ProjectName = projName;
            LoadAllApplications();
        }

        private void LoadAllApplications()
        {
            var lstApp = B_REGISTERED_APPS.GetCacheApps(MarsMainWindow.CurrentDatabaseIdx);
            this.AllMarsApplications = new ObservableCollection<DataCntx_Reg_Apps>(lstApp.FromBDTOs());
        }

        public AssignApplicationToTestCaseModel()
        {

        }

        public DelegateCommand AssingAppToTCCommand
        {
            get => new DelegateCommand(()=> {
                if (ProjectId < 0)
                {
                    HintByMessageBox("Select Project First!");
                    return;
                }
                if (SelectedMarsApplictions==null)
                {
                    HintByMessageBox("Select an application from Combobox!");
                    return;
                }
                bool isOk = false;
                string strError = "";
                int iCnt = DataCntx_Reg_Apps.AssignProjectWithAppId(MarsMainWindow.CurrentDatabaseIdx, 
                    ProjectId, SelectedMarsApplictions.APPLICATION_ID, ref isOk , ref strError);
                if (!isOk)
                {
                    HintByMessageBox(string.Format("Can't assign application to all test cases of this project. \r\n{0}", strError));
                    return;
                }
                
                //this.CloseWindow();
                HintByMessageBox(string.Format("Updated [{0}] records!", iCnt));
                selfView.Close();
            });
        }

        public DelegateCommand CloseCommand
        {
            get => new DelegateCommand(() => {
                //this.CloseWindow();
                selfView.Close();
            });
        }
    }
}
