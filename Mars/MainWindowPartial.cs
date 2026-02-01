
using Mars.Delegate;
using MarsTestFrame.systemUtil;
using Route2NSEx.src.Marquis.systemUtil;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System;

namespace Mars
{
    /// <summary>
    /// This is a partial class for Mainwindow
    /// Author: tiger
    /// Date  : 20160119
    /// </summary>
    public partial class MainWindow : RibbonWindow, INotifyPropertyChanged
    {
        //private static MLogger Logger = MLogger.GetLogger(typeof(MainWindow));
        public static readonly  DependencyProperty CurrentStoryBoardIDProperty =
            DependencyProperty.Register("CurrentActiveStoryBoardID", typeof(long?), 
                typeof(MainWindow), null);            
        public long? CurrentActiveStoryBoardID
        {
            get {
                return (long?)GetValue(CurrentStoryBoardIDProperty); //currentActiveStoryBoardID;
            }
            set {
                SetValue(CurrentStoryBoardIDProperty, value);
                //if (value!=currentActiveStoryBoardID)
                //{
                //    currentActiveStoryBoardID = value;
                //    RaisePropertyChanged("CurrentActiveStoryBoardID");
                //}

            }
        }
        

        private void UpdateTestInfoByStoryBoardProjectId(long projectId, long  storyboardId)
        {
            //Logger.logBegin("UpdateTestInfoByStoryBoardProjectId");
            /// enable ribbon 
            /// 
            EnableTestRibbon();
            //CurrentActiveStoryBoardID = storyboardId;
            /// get data from DB
            /// 

        }

        private void EnableTestRibbon()
        {
            HashSet<E_DISPLAY_TESTRIBBON> hsHiddenTest = new HashSet<E_DISPLAY_TESTRIBBON>();
            hsHiddenTest.Add(E_DISPLAY_TESTRIBBON.E_ENABLE_STORYBOARD_TEST);
            hsHiddenTest.Add(E_DISPLAY_TESTRIBBON.E_ENABLE_TESTCASE_TEST);
            OnMainRibbon_ProjectTestChangeImpl(hsHiddenTest);
        }

        public void OnMainRibbon_ProjectTestChangeImpl(HashSet<E_DISPLAY_TESTRIBBON> displayId)
        {
            if (displayId == null) return;
            if (displayId.Contains(E_DISPLAY_TESTRIBBON.E_HIDDEN_ALL))
            {
                /// hidden all test group
                /// 
                this.ProjectTestGroup.Visibility = Visibility.Hidden;
                return;
            }
            if (this.ProjectTestGroup.Visibility != Visibility.Visible)
            {
                this.ProjectTestGroup.Visibility = Visibility.Visible;
            }
            if ((displayId.Contains(E_DISPLAY_TESTRIBBON.E_ENABLE_STORYBOARD_TEST)))
            {
                this.ComboxTargetApplication.Visibility = Visibility.Visible;
                this.ribbonBtnTestCurrentStoryboard.Visibility = Visibility.Visible;
                this.ribbonBtnTestCurrentTC.Visibility = Visibility.Hidden;
            }
            if ((displayId.Contains(E_DISPLAY_TESTRIBBON.E_ENABLE_TESTCASE_TEST)))
            {
                this.ComboxTargetApplication.Visibility = Visibility.Visible;
                this.ribbonBtnTestCurrentStoryboard.Visibility = Visibility.Visible;
                this.ribbonBtnTestCurrentTC.Visibility = Visibility.Visible;
            }

        }

        public int OnRequireRibbonCurrentTestApplicationImpl(ref string errorMessage, ref E_ERROR_CODE_TEST_FRAMEWORK errorCode)
        {
            if (!this.ComboxTargetApplication.IsVisible)
            {
                errorCode = E_ERROR_CODE_TEST_FRAMEWORK.E_ERROR_NOT_ENABLED_APPLICATION_SELECTLIST;
                errorMessage = Mars.Properties.Resources.E_ERROR_NOT_ENABLED_APPLICATION_SELECTLIST;
                return int.MinValue;
            }
            if ((this.ComboxTargetApplication.Items == null) || (this.ComboxTargetApplication.Items.Count == 0))
            {
                errorCode = E_ERROR_CODE_TEST_FRAMEWORK.E_ERROR_NO_APPLICATIONS;
                errorMessage = Mars.Properties.Resources.E_ERROR_NO_APPLICATIONS;
                return int.MinValue;
            }
            if (this.ComboxTargetApplication.SelectionBoxItem == null)
            {
                errorCode = E_ERROR_CODE_TEST_FRAMEWORK.E_ERROR_NO_APPLICATION_IS_SELECTED;
                errorMessage = Mars.Properties.Resources.E_ERROR_NO_APPLICATION_IS_SELECTED;
                return int.MinValue;
            }
            if (!(this.ComboxTargetApplication.SelectionBoxItem is MarsKeyValues<int, string>))
            {
                errorCode = E_ERROR_CODE_TEST_FRAMEWORK.E_ERROR_WRONG_SELECTED_ITEM_TYPE_PARA_2;
                errorMessage = string.Format(Mars.Properties.Resources.E_ERROR_WRONG_SELECTED_ITEM_TYPE_PARA_2, "MarsKeyValues<int, string>", this.ComboxTargetApplication.SelectionBoxItem.GetType().ToString());
                return int.MinValue;
            }
            return ((MarsKeyValues<int, string>)(this.ComboxTargetApplication.SelectionBoxItem)).MKey;
        }

        

        
    }
}