using Mars.DataLayer;
using Mars.Model;
using Mars.Utility;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.ViewModel
{
    class DashboardViewModel : ViewModelBase
    {
        ObservableCollection<DashboardData> _dashboardDataColl = new ObservableCollection<DashboardData>();

        public ObservableCollection<DashboardData> DashboardDataColl
        {
            get { return _dashboardDataColl; }
            set { _dashboardDataColl = value; }
        }

        DashboardData _selectedDashboardData;
        private string _projectName;

        public DashboardData SelectedDashboardData
        {
            get { return _selectedDashboardData; }
            set { _selectedDashboardData = value; }
        }

        public DashboardViewModel(long projectId, string projectName)
        {
            _projectName = projectName;
            LoadDashboardData(projectId);
            Title = "Dashboard for Project:" + projectName;
            //ScaleUnit = (DashboardDataColl[0].MaxCount + (DashboardDataColl[0].MaxCount / 10)) / 9;
          
        }

        /*
        private void LoadDashboardDataTest()
        {
            DashboardData dd = new DashboardData("SB1", 1, "PASS", 0, "Feb 10 2016", "AF", 2, 3, 4);
            DashboardDataColl.Add(dd);

            dd = new DashboardData("SB2", 2, "PASS", 0, "Feb 10 2016", "AF", 3, 3, 6);
            DashboardDataColl.Add(dd);

            dd = new DashboardData("SB3", 3, "PASS", 0, "Feb 10 2016", "AF", 5, 3, 4);
            DashboardDataColl.Add(dd);

            dd = new DashboardData("SB4", 4, "PASS", 0, "Feb 10 2016", "AF", 1, 1, 25);
            DashboardDataColl.Add(dd);

            dd = new DashboardData("SB5", 4, "PASS", 0, "Feb 10 2016", "AF", 1, 1, 25);
            DashboardDataColl.Add(dd);
            dd = new DashboardData("SB6", 4, "PASS", 0, "Feb 10 2016", "AF", 1, 1, 25);
            DashboardDataColl.Add(dd);
        }
        */
        private static MLogger Logger = MLogger.GetLogger(typeof(DashboardViewModel));
        private void LoadDashboardData(long projectId)
        {
            //LoadDashboardDataTest();
            MarsEntities marsEntities = BoHelper.GetMarsEntitiesInstance(strCurrentDB: MarsMainWindow.CurrentDatabaseIdx);
            
            var storyBoards = (from c in marsEntities.V_STORYBOARD_TEST_FULLVISION
                               where c.PROJECT_ID == projectId 
                               //&& c.STORYBOARD_ID == 1825
                               select new
                               {
                                   c.PROJECT_ID,
                                   c.STORYBOARD_ID,
                                   c.STORYBOARD_NAME,
                                   c.STORYBOARD_DETAIL_ID,
                                   c.TEST_CASE_ID,
                                   c.TEST_CASE_END_TIME
                               });

            var storyBoardsList = storyBoards.ToList();
            Logger.Info("LoadDashboardData",string.Format("count:[{0}]", storyBoardsList.Count));

            var results = (from c in marsEntities.T_PROJ_TEST_RESULT
                           select new
                           {
                               c.TEST_CASE_ID,
                               c.STORYBOARD_DETAIL_ID,
                               c.TEST_RESULT,
                               c.LATEST_TEST_MARK_ID
                           });

            var resultsList = results.ToList();
            Logger.Info("LoadDashboardData", string.Format("resultsList count:[{0}]", resultsList.Count));

            var resultQuery =

            from a in storyBoardsList
            join b in resultsList

            on  ("" + a.TEST_CASE_ID + "_" + a.STORYBOARD_DETAIL_ID) equals 
                ("" + b.TEST_CASE_ID + "_" + b.STORYBOARD_DETAIL_ID) into joinedList
            
            from jl in joinedList.DefaultIfEmpty()
            select new
            {
                a.PROJECT_ID,
                a.STORYBOARD_ID,
                a.STORYBOARD_NAME,
                a.STORYBOARD_DETAIL_ID,
                a.TEST_CASE_ID,
                TEST_RESULT = jl == null ? (short?) null : jl.TEST_RESULT,
                
                a.TEST_CASE_END_TIME,

                LATEST_TEST_MARK_ID = jl == null ? (long?) null : jl.LATEST_TEST_MARK_ID,

//TEST_CASE_ID = jl == null ? (long?)null : jl.TEST_CASE_ID
                
            };
           

            var resultQueryList = resultQuery.ToList();
            Logger.Info("LoadDashboardData", string.Format("resultQueryList count:[{0}]", resultQueryList.Count));

            DataTable dt1 = DataTableUtil.ToDataTable(resultQueryList.ToList());
            //var storyBoardIdList = storyBoardsList.GroupBy(test => test.STORYBOARD_ID).Select(grp => grp.First().STORYBOARD_ID).ToList();

            var storyBoardIdList = ( from s in storyBoardsList
                                     orderby s.STORYBOARD_NAME
                                     group s by s.STORYBOARD_ID into newGroup
                                     select newGroup.Key).ToList();
            

            foreach (long storyBoardId in storyBoardIdList)
            {
                var resultsData = ( from r in resultQueryList
                                    where r.STORYBOARD_ID ==  storyBoardId  
                                    select r);

                if (resultsData==null) 
                    continue ;

                var resultsDataList = resultsData.ToList();
                DataTable dt3 = DataTableUtil.ToDataTable(resultsDataList.ToList());

                // latest mark
                long? maxLATEST_TEST_MARK_ID = (resultsDataList.Max(x => x.LATEST_TEST_MARK_ID));

                // total testcase count

                var ttt = resultsDataList.GroupBy(tc => tc.TEST_CASE_ID).Select(group => group.First());
                DataTable dt2 = DataTableUtil.ToDataTable(ttt.ToList());

                //int totalTestCaseCount = resultsDataList.ToList().Count ;
                int totalTestCaseCount = storyBoards.ToList().Count;
                //int totalTestCaseCount = resultsDataList.GroupBy(tc => tc.TEST_CASE_ID).Select(group => group.First()).Count();

                DataTable dt = DataTableUtil.ToDataTable(resultsData.ToList());

                /*
                var latestResult = resultsData.Where(p=>p.TEST_RESULT==1).OrderByDescending(p=>p.LATEST_TEST_MARK_ID).FirstOrDefault();
                var lsttmp = resultsData.Where(p => p.TEST_RESULT == 1);
                foreach(var itm in lsttmp)
                {

                }
                if (latestResult != null)
                    Console.WriteLine("");
                */


                int notProcessedDataCount = (from c in resultsDataList
                                       where c.TEST_RESULT == 0 
                                       select c).Count();


                int passedDataCount = (from c in resultsDataList
                                       where c.TEST_RESULT == 1 && c.LATEST_TEST_MARK_ID == maxLATEST_TEST_MARK_ID
                                       select c).Count();


                int failedDataCount = (from c in resultsDataList
                                       where c.TEST_RESULT == 2 && c.LATEST_TEST_MARK_ID == maxLATEST_TEST_MARK_ID
                                       select c).Count();

                notProcessedDataCount = totalTestCaseCount - (passedDataCount + failedDataCount);

            
                string sbName = (from c in storyBoardsList
                                 where c.STORYBOARD_ID == storyBoardId
                                 select c.STORYBOARD_NAME).First();

                var endTimeStamp = (from d in resultsData 
                                    select (DateTime?)d.TEST_CASE_END_TIME).Max(); 

                string status = "FAILED";
                if (failedDataCount == 0 && passedDataCount == 0)
                    status = "NOT PROCESSED";
                else if (failedDataCount == 0)
                    status = "PASSED";

                string lastDate = GetDateString(endTimeStamp);
                string lastTime = GetTimeString(endTimeStamp);
                //DashboardData dd = new DashboardData("SB1", 1, "PASS", 0, "Feb 10 2016", "AF", 2, 3, 4);
                DashboardData dd = new DashboardData(
                    sbName, 
                    storyBoardId, 
                    status, 

                    lastDate,
                    lastTime, 
                    "",
                    totalTestCaseCount,
                    passedDataCount, 
                    failedDataCount, 
                    notProcessedDataCount);
                DashboardDataColl.Add(dd);

            }
        
        }

        private string GetTimeString(object endTimeStamp)
        {
            if (endTimeStamp == null)
                return " ";

            else
            {
                DateTime dt = (DateTime)endTimeStamp;
                return dt.ToString("yyyy MMM dd");
            }
               
        }

        private string GetDateString(object endTimeStamp)
        {
            if (endTimeStamp == null)
                return " ";

            else
            {
                DateTime dt = (DateTime)endTimeStamp;
                return dt.ToString("HH:mm:ss");
            }
        }

        string _title;

        public string Title
        {
            get 
            { 
                return _title; 
            }
            set 
            { 
                _title = value;
            }
        }

        //public string Title { get; set; }
        private int margin1 = 0;
        public int Margin1 { get{return margin1 ;}
            set { margin1 = value; RaisePropertyChanged("Margin1"); }
        }

        public int ScaleUnit { get; set; }
    }


    public class DashboardData
    {

        int maxSize = 440;
        int total;
        public DashboardData()
        {

        }

        public DashboardData(string storyboardName,
                            long storyboardId, 
                            string testCaseStatus, 
                            string lastRunDate,
                            string lastRunTime, 
                            string userId,
                            int totalTestCaseCount,
                            int passedCount, 
                            int failedCount, 
                            int inProcessCount)
        {
            StoryboardName = storyboardName;
            StoryboardId = storyboardId;
            TestCaseStatus = testCaseStatus;
            LastRunDate = lastRunDate;
            LastRunTime = lastRunTime;
            UserId = userId;
            TotalTestCaseCount = totalTestCaseCount;
            PassedCount = passedCount;
            FailedCount = failedCount;
            InProcessCount = inProcessCount;

            total = passedCount + failedCount + inProcessCount +1;
            
            
            _failedTag = "" + FailedCount;
            _passedTag = "" + PassedCount;
            _inprocessTag = "" + InProcessCount;

            FailedTestCaseCount = FailedCount;
        }
        public string StoryboardName { get; set; }
        public long StoryboardId { get; set; }
        public string TestCaseStatus { get; set; }
        public long FailedTestCaseCount { get; set; }
        public string UserId { get; set; }

        public string LastRunDate { get; set; }
        public string GraphData { get; set; }

        private int _inProcessHeight;

        public int InProcessHeight
        {
            get 
            { 
                 return maxSize * InProcessCount / TotalTestCaseCount + 5;
            }
            set { _inProcessHeight = value; }
        }

        private int _passedHeight;

        public int PassedHeight
        {
            get 
            {
                if (TotalTestCaseCount == 0)
                    TotalTestCaseCount = 1;
  
                return maxSize * PassedCount / TotalTestCaseCount + 5;
            }
            set { _passedHeight = value; }
        }

        private int _failedHeight;

        public int FailedHeight
        {
            get 
            { 
                //return _failedHeight; 
                return maxSize * FailedCount / TotalTestCaseCount + 5;
            }
            set { _failedHeight = value; }
        }

        int _failedCount;

        public int FailedCount
        {
            get { return _failedCount; }
            set { _failedCount = value; }
        }
        int _passedCount;

        public int PassedCount
        {
            get { return _passedCount; }
            set { _passedCount = value; }
        }
        int _inProcessCount;

        public int InProcessCount
        {
            get { return _inProcessCount; }
            set { _inProcessCount = value; }
        }

        string _inprocessTag;

        public string InprocessTag
        {
            get { return _inprocessTag; }
            set { _inprocessTag = value; }
        }
        string _passedTag;

        public string PassedTag
        {
            get { return _passedTag; }
            set { _passedTag = value; }
        }
        string _failedTag;

        public string FailedTag
        {
            get { return _failedTag; }
            set { _failedTag = value; }
        }



        public int MaxCount { get; set; }

        public string LastRunTime { get; set; }

        public int TotalTestCaseCount { get; set; }
    }
}
