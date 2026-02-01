using Mars.MarsDataStructure.TestResult;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Route2NSEx.src.Marquis.systemUtil;
using Mars.ViewModel;

namespace Mars.basicDataStructure
{
    public class StoryboardRowDetailDataForMadel: StoryboardHistSummaryInfo, INotifyPropertyChanged
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(StoryboardRowDetailDataForMadel));
        public const string cnst_TestMode_BaseLine = "BaseLine";
        public const string cnst_TestMode_Compare = "Comparason";
        public const string cnst_TestMode_NA = "N/A";
        private static List<string> cnst_arrTestMode = new List<string>{ cnst_TestMode_Compare , cnst_TestMode_BaseLine , cnst_TestMode_NA };
        public static List<string> Cnst_arrTestMode {
            get{ return cnst_arrTestMode; }
        }
        #region Notifypropery 
        public event PropertyChangedEventHandler PropertyChanged;
        internal void RaisePropertyChanged(string prop)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        #endregion
        internal static ObservableCollection<StoryboardRowDetailDataForMadel> ConverFrom(List<StoryboardHistSummaryInfo> lstStoryHistInfo)
        {
            Logger.logBegin("ConverFrom", string.Format("Record count to Convert:[{0}]", lstStoryHistInfo == null ? 0 : lstStoryHistInfo.Count));
            try
            {
                List<StoryboardRowDetailDataForMadel> lstRslt = new List<StoryboardRowDetailDataForMadel>();
                if (lstStoryHistInfo == null) return null;
                lstStoryHistInfo.ForEach(itm =>
                {
                    lstRslt.Add(
                        new StoryboardRowDetailDataForMadel {
                            Hist_CreateDates = itm.Hist_CreateDates,
                            Hist_VersionNumber = itm.Hist_VersionNumber,
                            Test_ModeMark = itm.Test_ModeMark,
                            ResultDescription = itm.ResultDescription,
                            AliasName = itm.AliasName,
                            StoryboardDetailId = itm.StoryboardDetailId,
                            Hist_Id = itm.Hist_Id,
                            AssignedDatabaseRecord = itm.AssignedDatabaseRecord 
                        }
                        );
                }
                );

                return new ObservableCollection<StoryboardRowDetailDataForMadel>(lstRslt);
            }
            finally
            {
                Logger.logEnd("ConverFrom");
            }
        }

        #region Data Section
        private long hist_VersionNumber;
        public override long Hist_VersionNumber {
            get {
                return hist_VersionNumber;
            }
            set {
                hist_VersionNumber = value;
                RaisePropertyChanged("Hist_VersionNumber");
            }
        }

        private List<DateTime?> hist_CreateDates;
        public override List<DateTime?> Hist_CreateDates {
            get { return hist_CreateDates; }
            set {
                hist_CreateDates = value;
                RaisePropertyChanged("Hist_CreateDates ");
            } }
        private int test_ModeMark=-1;
        public override int Test_ModeMark { get { return test_ModeMark; }
            set { test_ModeMark = value;
                RaisePropertyChanged("Test_ModeMark");
            } }
        private string aliasName;
        public override string AliasName { get {
                return aliasName;
            } set { aliasName = value;
                RaisePropertyChanged("AliasName");
            }
        }

        private string resultDesc;
        public override string ResultDescription { get
            {
                return resultDesc;
            }
            set {
                resultDesc = value;
                RaisePropertyChanged("ResultDescription");
            } }
        private long storyboardDetailId;
        public override long StoryboardDetailId {
            get {
                return storyboardDetailId;
            } set {
                storyboardDetailId = value;
                RaisePropertyChanged("StoryboardDetailId");
            } }

        public string TestModeDisplayName
        {
            get
            {
                return
                  (test_ModeMark < 0 || test_ModeMark > 1) ?
                  cnst_arrTestMode[2] : cnst_arrTestMode[test_ModeMark];
            }

            set {
                if (string.IsNullOrEmpty(value))
                {
                    test_ModeMark = 2;
                    return;
                }
                int idx = cnst_arrTestMode.IndexOf(value);
                if (idx == -1) test_ModeMark = 2;
                test_ModeMark = idx;
                RaisePropertyChanged("Test_ModeMark");
                
            }
            
        }

        public DateTime? EndDateTime
        {
            get {
                if (hist_CreateDates == null) return null;
                for (int i=hist_CreateDates.Count-1;i>=0;i-- )
                {
                    if (hist_CreateDates[i] == null) continue;
                    return hist_CreateDates[i];
                }
                return null;
            } 
        }

        private long hist_Id;
        public override long Hist_Id
        {
            get
            {
                return hist_Id;
            }

            set
            {
                hist_Id = value;
            }
        }
        #endregion

        
    }



}
