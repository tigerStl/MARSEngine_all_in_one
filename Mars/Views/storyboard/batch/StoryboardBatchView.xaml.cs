using Mars.Helpers;
using Mars.ViewModel;
using Mars.ViewModel.storyboard.batch;
using Mars.Views.baseView;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mars.Views.storyboard.batch
{
    /// <summary>
    /// Interaction logic for StoryboardBatch.xaml
    /// </summary>
    public partial class StoryboardBatch : MarsBaseViewControl
    {
        private static MLogger logger = MLogger.GetLogger(typeof(StoryboardBatch));
        public StoryboardBatch()
        {
            InitializeComponent();
            Title = "Storyboard Batch Management";
            DataContext = new StoryboardBatchModel();
        }

        private void DataGrid_DragEnter(object sender, DragEventArgs e)
        {
            logger.logBegin("DataGrid_DragEnter");
            Point pt = Mouse.GetPosition(BatchMgrGrid);
            try
            {
                object oData = e.Data.GetData("int");
                if (oData==null)
                {
                    return;
                }
                if (!(oData is long))
                {
                    return;
                }

                long storyboardId;
                if (!long.TryParse(oData.ToString(), out storyboardId))
                    return;

                string strError = "";

                HitTestResult hit = VisualTreeHelper.HitTest(BatchMgrGrid, pt);
                if (!(hit.VisualHit is System.Windows.FrameworkElement)) return;
                DataGridRow targetRow = GridViewSort.GetAncestor<DataGridRow>(hit.VisualHit);
                if (targetRow == null)
                {
                    //new row
                    StoryboardBatchModel stryBrdMdl = this.DataContext as StoryboardBatchModel;
                    if (stryBrdMdl == null) return;
                    if (stryBrdMdl.InsertNew(MarsMainWindow.CurrentDatabaseIdx, storyboardId,0,ref strError))
                    {
                        logger.Error("DataGrid_DragEnter",strError);
                        return;
                    }
                }
                else
                {

                }                
            }
            catch (Exception ex)
            {
                
            }
        }

        private void DataGrid_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            logger.logBegin("DataGrid_DataContextChanged", string.Format("dbContext:[{0}]", e.NewValue));
            return;
        }

        
        private void DataGrid_Drop(object sender, DragEventArgs e)
        {
            logger.logBegin("DataGrid_Drop");
            System.Drawing.Point crrntPt = System.Windows.Forms.Cursor.Position;
            Point pt = new Point(crrntPt.X, crrntPt.Y);

            if (e.Source == null) return;
            if (! (e.Source is DataGrid)) return;
            object oData = e.Data.GetData("int");
            if (oData == null) return;
            if (!(oData is long)) return;
            long storyBoardId = (long)oData;
            //获得当前的row count
            DataGrid grd = e.Source as DataGrid;
            object targetItm = null;
            bool isOk = false;
            string strError = "";
            Mars.Business.B_TEST_BATCH_DETAIL objNewToBeInserted = new Business.B_TEST_BATCH_DETAIL(MarsMainWindow.CurrentDatabaseIdx);
            if (grd.HasItems)
            {
                bool isIncreaseRunOrder = false;
                for(int i = grd.Items.Count-1; i>=0;i--)
                {
                    var itm = grd.Items[i];
                    if (itm == null) continue;

                    if ((itm as Mars.Business.B_TEST_BATCH_DETAIL) == null) continue;
                    DataGridRow oneRow = grd.ItemContainerGenerator.ContainerFromItem(itm) as DataGridRow;
                    Point ptNew = oneRow.PointFromScreen(pt);
                    
                    if (oneRow == null) continue;
                    int iTargetPos = -1;

                    if (oneRow.InputHitTest(ptNew) !=null)
                    {
                        iTargetPos = oneRow.GetIndex();
                        logger.Info("DataGrid_Drop", string.Format("in a row:[{0}]", iTargetPos));
                        //
                        targetItm = itm;
                        Mars.Business.B_TEST_BATCH_DETAIL objBatchDetailFromStoryBoardId = Mars.Business.B_TEST_BATCH_DETAIL.FromStoryboardId(MarsMainWindow.CurrentDatabaseIdx, storyBoardId,ref isOk, ref strError);
                        if (!isOk)
                        {
                            ViewModelBase.HintByMessageBox(strError);
                            return;
                        }
                        objBatchDetailFromStoryBoardId.BAT_ID = (this.DataContext as StoryboardBatchModel).CurrentBatch.BAT_ID;
                        objBatchDetailFromStoryBoardId.RUN_ORDER = iTargetPos+1;

                        isIncreaseRunOrder = true;
                        (this.DataContext as StoryboardBatchModel).CurrentBatch.StoryBoardList.Insert(iTargetPos, objBatchDetailFromStoryBoardId);                       
                    }
                    else
                    {
                        //if (isIncreaseRunOrder) {
                        //    ((Mars.Business.B_TEST_BATCH_DETAIL)itm).RUN_ORDER += 1;
                        //}
                    }
                    if (isIncreaseRunOrder)
                    {
                        (this.DataContext as StoryboardBatchModel).RefreshCurrentBatchRunOrder();
                    }
                }

            }
            //add new
            IInputElement oRow = grd.InputHitTest(pt);
            
        }

        private void DataGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //DependencyObject dep = (DependencyObject)e.OriginalSource;

            //// iteratively traverse the visual tree
            //while ((dep != null) &&
            //        !(dep is DataGridCell))
            //{
            //    dep = VisualTreeHelper.GetParent(dep);
            //}

            //if (dep == null)
            //    return;

            //if (dep is DataGridCell)
            //{
            //    DataGridCell cell = dep as DataGridCell;

            //    // navigate further up the tree
            //    while ((dep != null) && !(dep is DataGridRow))
            //    {
            //        dep = VisualTreeHelper.GetParent(dep);
            //    }

            //    LastActiveRow = dep as DataGridRow;

            //    //!!!!!!!!!!!!!* (Look below) !!!!!!!!!!!!!!!!!

            //}
        }
    }
}
