using MarsSpyTool.subToolWindows.testStepEditor;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarsSpyTool.subToolWindows.viewModal
{
    public class MarsHintWindowsModal: INotifyPropertyChanged
    {
        private string hintMessageHint;
        public string HintMessageHint
        {
            get => hintMessageHint;
            set
            {
                if (hintMessageHint == value) return;
                hintMessageHint = value;
                OnPropertyChanged(nameof(HintMessageHint));
            }
        }

        public ObservableCollection<MarsSpyApplication> applicationsToBeConfirmed;
        public ObservableCollection<MarsSpyApplication> ApplicationsToBeConfirmed
        {
            get { return applicationsToBeConfirmed; }
            set
            {
                if (applicationsToBeConfirmed == value) return;
                applicationsToBeConfirmed = value;
                OnPropertyChanged(nameof(ApplicationsToBeConfirmed));
            }
        }

        //private MarsSpyApplication selectedApplication;
        public MarsSpyApplication SelectedApplication
        {
            get => MarsTestAPPDBInfo.CurrentApplicationInfo;
            set
            {
                if (MarsTestAPPDBInfo.CurrentApplicationInfo == value) return;
                MarsTestAPPDBInfo.CurrentApplicationInfo = value;
                OnPropertyChanged(nameof(SelectedApplication));
            }
        }

        public void CreateApplicationsFromList(List<MarsSpyApplication> lstApps)
        {
            ApplicationsToBeConfirmed = new ObservableCollection<MarsSpyApplication>();
            if (lstApps == null) return;
            foreach(var itm in lstApps)
            {
                ApplicationsToBeConfirmed.Add(itm);
            }
            if (ApplicationsToBeConfirmed.Count <= 0)
            {
                return;
            }
            this.SelectedApplication = ApplicationsToBeConfirmed.FirstOrDefault();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        

    }
}
