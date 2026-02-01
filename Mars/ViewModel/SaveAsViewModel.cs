using Mars.Business;
using Mars.DataLayer;
using Mars.Dto;
using Mars.Model;
using Mars.Utility;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows;
using System.Windows.Input;

namespace Mars.ViewModel
{
    public class SaveAsViewModel : ViewModelBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(SaveAsViewModel));

        private string contextName;
        string callerName;
        string searchName;
        long refObjectId;
        private ICommand _saveCommand;
        private ICommand _clearCommand;
        MarsEntities marsEntities;
        public SaveAsViewModel(string caller, string strName, long lObjectId=-1)
        {
            callerName = caller;
            searchName = strName;
            ContextName = strName; //AF
            refObjectId = lObjectId;
            _saveCommand = new DelegateCommand(() => { SaveAs(); });
            _clearCommand = new DelegateCommand(() => { Clear(); });
        }

        private Visibility isExtendsOptionRequired = Visibility.Collapsed;
        public Visibility IsExtendsOptionRequired
        {
            get
            {
                return isExtendsOptionRequired;
            }
            set
            {
                isExtendsOptionRequired = value;
                RaisePropertyChanged("IsExtendsOptionRequired");
            }
        }
   
        private List<string> availableOptions = null;
        public List<string> AvailableOptions
        {
            get { return availableOptions; }
            set { availableOptions = value;RaisePropertyChanged("AvailableOptions"); }
        }
        private string optionHint;
        public string OptionHint
        {
            get { return optionHint; }
            set { optionHint = value;RaisePropertyChanged("OptionHint"); }
        }

        private string selectedOption;
        public string SelectedOption
        {
            get { return selectedOption; }
            set { selectedOption = value;RaisePropertyChanged("SelectedOption"); }
        }
        public string ContextName
        {
            get
            {
                return contextName;
            }
            set
            {
                contextName = value;
                RaisePropertyChanged("ContextName");
            }
        }

        public ICommand SaveCommand
        {
            get
            {
                return _saveCommand;
            }

            set
            { }

        }

        public ICommand ClearCommand
        {
            get
            {
                return _clearCommand;
            }

            set
            { 
               
            }

        }

        public List<string> validationErrors = new List<string>();

        static readonly string[] ValidatedProperties =
        {
                "SaveAs Name"
        };

        public bool IsValid
        {
            get
            {
                foreach (string property in ValidatedProperties)
                {
                    if (GetValidationError(property) != null)
                        validationErrors.Add(GetValidationError(property));
                }
                if (validationErrors.Count > 0)
                {
                    return false;
                }
                return true;
            }
        }

        private string GetValidationError(string propertyName)
        {
            string error = null;

            switch (propertyName)
            {
                case "SaveAs Name":
                    error = this.ValidateName();
                    break;
                default:
                    error = null;
                    throw new Exception("Unexpected property being validated on Service");
            }

            return error;
        }

        string ValidateName()
        {
            if (IsStringMissing(this.ContextName))
            {
                return "SaveAs Name";
            }
            return null;
        }

        static bool IsStringMissing(string value)
        {
            return
                String.IsNullOrEmpty(value) ||
                value.Trim() == String.Empty;
        }


        public bool SaveAs()
        {
            if (!IsValid)
            {
                StringBuilder sbError = new StringBuilder();
                sbError.Append("Please enter valid :");

                foreach (string error in validationErrors)
                {
                    sbError.Append(error);
                    sbError.Append(" : ");
                }
                MessageBox.Show(sbError.ToString(), "SaveAs" + callerName, MessageBoxButton.OK, MessageBoxImage.Error);
                validationErrors.Clear();
                return false;
             
            }
            
            switch(callerName)
            {
                case "Project":
                    if (SaveAsProject())
                    {
                        MarsTreeView.GetMarsTree(MarsMainWindow.CurrentDatabaseIdx);
                    }
                    break;
                case "Test suite":
                    SaveAsTestSuite();
                    break;
                case "Test case":
                    SaveAsTestCase();
                    break;
                case "Data Sheet":
                    SaveAsDataSheet();
                    break;
                case "Shared Data Sheet":
                    SaveAsSharedDataSheet();
                    break;

                case "Storyboard":
                    SaveAsStoryboard();
                    break;


                default:
                    break;

            }
            return false;
        }

        private void SaveAsSharedDataSheet()
        {
            VMCollCash.currentVMColl.SaveSharedDataSheetAs(ContextName, "");
        }

        private void SaveAsDataSheet()
        {
            string strError = "";
            bool isOk = VMCollCash.currentVMColl.SaveDataSheetAs(ContextName,ref strError);
            if (!isOk)
            {
                HintByMessageBox(string.Format("Error ocurrs:\r\n{0}",strError), "ERROR");
            }
            else
            {
                HintByMessageBox(string.Format("Data set [{0}] is saved ", contextName));
            }
        }


        public void Clear()
        {
            ContextName = "";            
        }


        private bool SaveAsStoryboard()
        {

            return true;
        }

        private bool SaveAsProject()
        {


            //marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            T_TEST_PROJECTDTO bProject = new T_TEST_PROJECTDTO();
            B_TEST_PROJECT bTestProject = new B_TEST_PROJECT();
            B_REL_APP_PROJ bRelAppProj = new B_REL_APP_PROJ();
            B_REL_APP_TESTSUITE bRelAppTestSuite = new B_REL_APP_TESTSUITE();
            B_REL_TEST_SUIT_PROJECT bTsProj = new B_REL_TEST_SUIT_PROJECT();
            WindowsIdentity ident = WindowsIdentity.GetCurrent();

            MarsTransactionMgr objTrans = new MarsTransactionMgr(MarsMainWindow.CurrentDatabaseIdx, true);
            try
            {
                using (var scope = new TransactionScope())
                {

                    //Check if it already exists
                    if (!bTestProject.ProjectExists(MarsMainWindow.CurrentDatabaseIdx, ContextName))
                    {

                        T_TEST_PROJECT project = null;
                        if (refObjectId == -1)
                            project = (from p in objTrans.CurrentDBContext.T_TEST_PROJECT
                                       where p.PROJECT_NAME == searchName
                                       select p).FirstOrDefault();
                        else
                            project = (from p in objTrans.CurrentDBContext.T_TEST_PROJECT
                                       where p.PROJECT_ID == refObjectId
                                       select p).FirstOrDefault();
                        var projectId = bTestProject.getProjectId(MarsMainWindow.CurrentDatabaseIdx, objTrans.CurrentDBContext);
                        bProject.PROJECT_ID = projectId;
                        bProject.PROJECT_NAME = ContextName;
                        bProject.PROJECT_DESCRIPTION = project.PROJECT_DESCRIPTION;
                        bProject.STATUS = project.STATUS;
                        bProject.CREATOR = ident.Name.ToString();
                        bProject.CREATE_DATE = DateTime.Now;

                        objTrans.CurrentDBContext.T_TEST_PROJECT.Add(T_TEST_PROJECTAssembler.ToEntity(bProject));

                        var relAppProject = (from a in objTrans.CurrentDBContext.REL_APP_PROJ
                                             where a.PROJECT_ID == project.PROJECT_ID
                                             select a);
                        foreach (var a in relAppProject)
                        {
                            REL_APP_PROJDTO relAppProjDto = new REL_APP_PROJDTO();
                            relAppProjDto.PROJECT_ID = projectId;
                            relAppProjDto.RELATIONSHIP_ID = bRelAppProj.getRelAppProjId(MarsMainWindow.CurrentDatabaseIdx, objTrans.CurrentDBContext);
                            relAppProjDto.APPLICATION_ID = a.APPLICATION_ID;
                            objTrans.CurrentDBContext.REL_APP_PROJ.Add(REL_APP_PROJAssembler.ToEntity(relAppProjDto));
                        }

                        var relTestSuiteProject = (from r in objTrans.CurrentDBContext.REL_TEST_SUIT_PROJECT
                                                   where r.PROJECT_ID == project.PROJECT_ID
                                                   select r);
                        foreach (var r in relTestSuiteProject)
                        {
                            REL_TEST_SUIT_PROJECTDTO bRelTestSuiteProject = new REL_TEST_SUIT_PROJECTDTO();
                            bRelTestSuiteProject.PROJECT_ID = projectId;
                            bRelTestSuiteProject.RELATIONSHIP_ID = bTsProj.getRelTestSuiteAppId(MarsMainWindow.CurrentDatabaseIdx, objTrans.CurrentDBContext);
                            bRelTestSuiteProject.TEST_SUITE_ID = r.TEST_SUITE_ID;
                            objTrans.CurrentDBContext.REL_TEST_SUIT_PROJECT.Add(REL_TEST_SUIT_PROJECTAssembler.ToEntity(bRelTestSuiteProject));
                        }

                        
                        try
                        {
                            if (objTrans.CurrentDBContext.SaveChanges() > 0)
                            {
                                scope.Complete();
                                
                                MessageBox.Show("Project successfully saved as", "Project SaveAs", MessageBoxButton.OK, MessageBoxImage.Information);
                                Clear();
                                return true;
                            }
                            else
                            {
                                marsEntities = null;
                                MessageBox.Show("Error saving project", "Project SaveAs", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return false;
                            }

                        }
                        catch (Exception ex)
                        {
                            marsEntities = null;
                            MessageBox.Show(ex.InnerException.ToString(), "Project Delete", MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }

                    }
                    else
                    {
                        MessageBox.Show("Project already exists");
                        return false;
                    }
                }
                
            }
            catch (Exception e)
            {
                string strError = "";
                Logger.Error("SaveAsProject",strError= string.Format("Exception:[{0}],stackTrace:\r\n{1}",e.Message,e.StackTrace),e);
                HintByMessageBox(strError, "Error");
                return false;
            }
        }

        private bool SaveAsTestSuite()
        {

            marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB: MarsMainWindow.CurrentDatabaseIdx);
            T_TEST_SUITEDTO testSuite = new T_TEST_SUITEDTO();
            B_TEST_SUITE bTestSuite = new B_TEST_SUITE();            
            B_REL_APP_TESTSUITE bRelAppTestSuite = new B_REL_APP_TESTSUITE();
            B_REL_TEST_CASE_TEST_SUITE bRelTestCaseTestSuite = new B_REL_TEST_CASE_TEST_SUITE();
            //Check if it already exists
            if (bTestSuite.TestSuiteExists(MarsMainWindow.CurrentDatabaseIdx, ContextName))
            {
                if (MessageBox.Show(string.Format("Test Suite Name:[{0}], alreday exists. \r\nDo you want to create a test suite with the same name?",ContextName),"Hint",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)
                {
                    return false;
                }
            }

            //if (!bTestSuite.TestSuiteExists(ContextName))
            {
                long testSuiteId = bTestSuite.getTestSuiteId(MarsMainWindow.CurrentDatabaseIdx);
                testSuite.TEST_SUITE_ID = testSuiteId;
                testSuite.TEST_SUITE_NAME = ContextName;
                var testsuite = (from t in marsEntities.T_TEST_SUITE
                                     //where t.TEST_SUITE_NAME == searchName
                                 where
                                    t.TEST_SUITE_ID==refObjectId
                                 select t).FirstOrDefault();

                var relAppTestSuite = (from a in marsEntities.REL_APP_TESTSUITE
                                       where a.TEST_SUITE_ID == testsuite.TEST_SUITE_ID
                                       select a);
                foreach (var a in relAppTestSuite)
                {
                    REL_APP_TESTSUITEDTO relAppTestSuiteDto = new REL_APP_TESTSUITEDTO();
                    relAppTestSuiteDto.TEST_SUITE_ID = testSuiteId;
                    relAppTestSuiteDto.RELATIONSHIP_ID = bRelAppTestSuite.getRelTestSuiteAppId(MarsMainWindow.CurrentDatabaseIdx);
                    relAppTestSuiteDto.APPLICATION_ID = a.APPLICATION_ID;
                    marsEntities.REL_APP_TESTSUITE.Add(REL_APP_TESTSUITEAssembler.ToEntity(relAppTestSuiteDto));
                }

                var relTestCaseTestSuite = (from r in marsEntities.REL_TEST_CASE_TEST_SUITE
                                            where r.TEST_SUITE_ID == testsuite.TEST_SUITE_ID
                                            select r);
                foreach (var r in relTestCaseTestSuite)
                {
                    REL_TEST_CASE_TEST_SUITEDTO bRelTestCaseTestSuiteDTo = new REL_TEST_CASE_TEST_SUITEDTO();
                    bRelTestCaseTestSuiteDTo.TEST_SUITE_ID = testSuiteId;
                    bRelTestCaseTestSuiteDTo.RELATIONSHIP_ID = bRelTestCaseTestSuite.getRelTestCasteTestSuite(MarsMainWindow.CurrentDatabaseIdx);
                    bRelTestCaseTestSuiteDTo.TEST_CASE_ID = r.TEST_CASE_ID;
                    marsEntities.REL_TEST_CASE_TEST_SUITE.Add(REL_TEST_CASE_TEST_SUITEAssembler.ToEntity(bRelTestCaseTestSuiteDTo));
                }

                marsEntities.T_TEST_SUITE.Add(T_TEST_SUITEAssembler.ToEntity(testSuite));
                try
                {
                    if (marsEntities.SaveChanges() > 0)
                    {
                        MarsTreeView.GetMarsTree(MarsMainWindow.CurrentDatabaseIdx);
                        MessageBox.Show("Test suite successfully saved as", "Test suite SaveAs", MessageBoxButton.OK, MessageBoxImage.Information);
                        Clear();
                        return true;
                    }
                    else
                    {
                        marsEntities = null;
                        MessageBox.Show("Error saving test suite", "Test suite SaveAs", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }

                }
                catch (Exception ex)
                {
                    marsEntities = null;
                    MessageBox.Show(ex.InnerException.ToString(), "Test suite saveas", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            //else
            //{
            //    MessageBox.Show("Test suite already exists");
            //    return false;
            //}
            
        }


        

        private bool SaveAsTestCase()
        {

            Logger.Info("SaveAsTestCase",string.Format("Try to save testcase as :[{0}] from Test case id:[{1}] with option:[{2}]",this.ContextName,this.refObjectId, this.selectedOption));
            //note:-1, 0 then only test test is created 
            int iSaveOption = string.IsNullOrEmpty(selectedOption) ? -1 : selectedOption.IndexOf(this.selectedOption);
            string strError = "";
            B_TEST_CASE boTS = new B_TEST_CASE();
            if (!boTS.SaveTestCase(MarsMainWindow.CurrentDatabaseIdx, this.refObjectId, this.contextName, iSaveOption, ref strError))
            {
                HintByMessageBox(string.Format("Can't save Testcase [{0}] to [{1}] with Error Message:\r\n[{2}]", refObjectId, contextName, strError), "Error");
                return false;
            }
            //需要update app-test case的cache


            HintByMessageBox(string.Format("Test case [{0}] is created!", contextName));
            return true;

            #region old codes, moved to data layer
            //marsEntities = Mars.DataLayer.BoHelper.GetMarsEntitiesInstance(strCurrentDB:strDBIdx);
            ////marsEntities.Database.Connection.Open();
            //T_TEST_CASE_SUMMARYDTO newTestCase = new T_TEST_CASE_SUMMARYDTO();
            //B_TEST_CASE bTestCase = new B_TEST_CASE();
            //B_TEST_STEPS bTestStep = new B_TEST_STEPS();
            //B_REL_APP_TESTCASE bRelAppTestCase = new B_REL_APP_TESTCASE();
            //if (!bTestCase.TestCaseExists(ContextName))
            //{

            //    var oldTestCase = (from t in marsEntities.T_TEST_CASE_SUMMARY
            //                           //where t.TEST_CASE_NAME == searchName
            //                       where t.TEST_CASE_ID == refObjectId
            //                    select t).FirstOrDefault();
 
            //    long testCaseId = bTestCase.getTestCaseId();
            //    newTestCase.TEST_CASE_ID = testCaseId;
            //    newTestCase.TEST_CASE_NAME = ContextName;
            //    newTestCase.TEST_STEP_DESCRIPTION = oldTestCase.TEST_STEP_DESCRIPTION;

            //    LastTestCase = newTestCase.TEST_CASE_ID;

            //    // create REL_TC_DATA_SUMMARY
            //    var relDataSetIds = (from r in marsEntities.REL_TC_DATA_SUMMARY
            //                         where r.TEST_CASE_ID == oldTestCase.TEST_CASE_ID
            //                         select r.DATA_SUMMARY_ID);

            //    foreach (var relDataSetId in relDataSetIds)
            //    {
            //        BoHelper.CreateRelTCDataSummary((long)relDataSetId, newTestCase.TEST_CASE_ID);
            //        LastDataSet = (long)relDataSetId;
            //    }

            //    // create REL_TEST_CASE_TEST_SUITE
            //    var relTestCaseTestSuteIds = (from r in marsEntities.REL_TEST_CASE_TEST_SUITE
            //                                  where r.TEST_CASE_ID == oldTestCase.TEST_CASE_ID
            //                                  select r.TEST_SUITE_ID).ToList();

            //    DataTable dtr = DataTableUtil.ToDataTable(relTestCaseTestSuteIds);

            //    foreach (var relTestCaseTestSuteId in relTestCaseTestSuteIds)
            //    {
            //        B_REL_TEST_CASE_TEST_SUITE bRelTestCaseTestSuite = new B_REL_TEST_CASE_TEST_SUITE();
            //        bRelTestCaseTestSuite.TEST_SUITE_ID = relTestCaseTestSuteId;
            //        bRelTestCaseTestSuite.TEST_CASE_ID = newTestCase.TEST_CASE_ID;
            //        bRelTestCaseTestSuite.RELATIONSHIP_ID = bRelTestCaseTestSuite.getRelTestCasteTestSuite();
            //        marsEntities.REL_TEST_CASE_TEST_SUITE.Add(REL_TEST_CASE_TEST_SUITEAssembler.ToEntity(bRelTestCaseTestSuite));
            //    }

            //    // Create REL_APP_TEST_CASE rows
            //    var relAppTestCase = (from a in marsEntities.REL_APP_TESTCASE
            //                          where a.TEST_CASE_ID == oldTestCase.TEST_CASE_ID
            //                          select a);

            //    foreach (var a in relAppTestCase)
            //    {
            //        REL_APP_TESTCASEDTO relAppTestCaseDto = new REL_APP_TESTCASEDTO();
            //        relAppTestCaseDto.TEST_CASE_ID = testCaseId;
            //        relAppTestCaseDto.RELATIONSHIP_ID = bRelAppTestCase.getRelTestCaseAppId();
            //        relAppTestCaseDto.APPLICATION_ID = a.APPLICATION_ID;
            //        marsEntities.REL_APP_TESTCASE.Add(REL_APP_TESTCASEAssembler.ToEntity(relAppTestCaseDto));
            //    }

            //    // Create T_TEST_STEPS rows
            //    var testCaseTestStep = (from r in marsEntities.T_TEST_STEPS
            //                            where r.TEST_CASE_ID == oldTestCase.TEST_CASE_ID
            //                            select r);

            //    foreach (var r in testCaseTestStep)
            //    {
            //        T_TEST_STEPSDTO bTestCaseTestStepsDTo = new T_TEST_STEPSDTO();
            //        bTestCaseTestStepsDTo.STEPS_ID = BoHelper.GetTestStepsId();
            //        bTestCaseTestStepsDTo.TEST_CASE_ID = testCaseId;
            //        bTestCaseTestStepsDTo.KEY_WORD_ID = r.KEY_WORD_ID;
            //        bTestCaseTestStepsDTo.RUN_ORDER = r.RUN_ORDER;
            //        bTestCaseTestStepsDTo.OBJECT_ID = r.OBJECT_ID;
            //        bTestCaseTestStepsDTo.COLUMN_ROW_SETTING = r.COLUMN_ROW_SETTING;
            //        bTestCaseTestStepsDTo.VALUE_SETTING = r.VALUE_SETTING;
            //        bTestCaseTestStepsDTo.COMMENT = r.COMMENT;
            //        bTestCaseTestStepsDTo.IS_RUNNABLE = r.IS_RUNNABLE;
            //        marsEntities.T_TEST_STEPS.Add(T_TEST_STEPSAssembler.ToEntity(bTestCaseTestStepsDTo));
                   
            //        AddTestDataSetting(bTestCaseTestStepsDTo.STEPS_ID, r.STEPS_ID, bTestStep);
                   
            //    }
                
            //    // Create T_TEST_CASE_SUMMARY
            //    marsEntities.T_TEST_CASE_SUMMARY.Add(T_TEST_CASE_SUMMARYAssembler.ToEntity(newTestCase));
            //    try
            //    {
                    
            //        if (marsEntities.Database.Connection.State != ConnectionState.Open)
            //            marsEntities.Database.Connection.Open();
            //        if (marsEntities.SaveChanges() > 0)
            //        {
            //            MarsTreeView.GetMarsTree();
            //            MessageBox.Show("Test case successfully saved as", "Test case SaveAs", MessageBoxButton.OK, MessageBoxImage.Information);
            //            Clear();
            //            MarsDBGlobe_Cache.UpdateAppTestCaseCache(); //AF added this
            //            return true;
            //        }
            //        else
            //        {
            //            marsEntities = null;
            //            MessageBox.Show("Error saving test case", "Test case SaveAs", MessageBoxButton.OK, MessageBoxImage.Warning);
            //            return false;
            //        }

            //    }
            //    catch (Exception ex)
            //    {
            //        marsEntities = null;
            //        MessageBox.Show(ex.InnerException.ToString(), "Test case saveas", MessageBoxButton.OK, MessageBoxImage.Error);
            //        return false;
            //    }
            //}
            //else
            //{
            //    MessageBox.Show("Test case already exists");
            //    return false;
            //}
            #endregion
        }


        private void AddTestDataSetting(long newStepId, long oldStepId, B_TEST_STEPS bTestStep)
        {
           
            var testDataSetting = (from r in marsEntities.TEST_DATA_SETTING
                                   where r.STEPS_ID == oldStepId
                                    select r).ToList();
            if (testDataSetting.Count != 0)
            {
                DataTable dt = DataTableUtil.ToDataTable(testDataSetting);
            
           

                foreach (var r in testDataSetting)
                {
                    TEST_DATA_SETTINGDTO btestDataSettingDTo = new TEST_DATA_SETTINGDTO();
                    btestDataSettingDTo.DATA_SETTING_ID = BoHelper.GetDataSettingId(MarsMainWindow.CurrentDatabaseIdx);
                    btestDataSettingDTo.LOOP_ID = r.LOOP_ID;
                    btestDataSettingDTo.STEPS_ID = newStepId;
                    btestDataSettingDTo.DATA_VALUE = r.DATA_VALUE;
                    btestDataSettingDTo.DESCRIPTION = r.DESCRIPTION;
                    btestDataSettingDTo.VALUE_OR_OBJECT = r.VALUE_OR_OBJECT;

                    btestDataSettingDTo.DATA_SUMMARY_ID = r.DATA_SUMMARY_ID;
                    btestDataSettingDTo.POOL_ID = r.POOL_ID;
                
                   // marsEntities.T_TEST_STEPS.Add(T_TEST_STEPSAssembler.ToEntity(bTestCaseTestStepsDTo));
                    marsEntities.TEST_DATA_SETTING.Add(TEST_DATA_SETTINGAssembler.ToEntity(btestDataSettingDTo));
                }

            }

        }

        public static long LastTestCase { get; set; }

        public static long LastDataSet { get; set; }
    }
}
