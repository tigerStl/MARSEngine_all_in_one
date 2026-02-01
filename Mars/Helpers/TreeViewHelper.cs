using Mars.ViewModel;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Mars.Helpers
{
    public static class TreeViewHelper
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TreeViewHelper));

        public static TreeViewItem FindStoryboardTreeViewItem(TreeViewItem projectItem, long storyboardId)
        {
            Logger.logBegin("FindStoryboardTreeViewItem", string.Format("StoryboardId:[{0}] by project Node", storyboardId));
            if (projectItem == null) return null;
            ObservableCollection<MarsFolderTreeView> lstProjectFolds = ((projectItem.Header as MarsProjectTreeView).TEST_FOLDER);
            if (lstProjectFolds == null) return null;

            projectItem.IsExpanded = true;
            projectItem.UpdateLayout();
            int iProjectNodes = VisualTreeHelper.GetChildrenCount(projectItem);
            
            for (int i=0;i<iProjectNodes;i++)
            {
                TreeViewItem c = VisualTreeHelper.GetChild(projectItem, i) as TreeViewItem;
                if (c == null) continue;                
            }
            //MarsStoryboardTreeView targetStoryboard = 
            foreach (var itmTmp in lstProjectFolds)
            {
                if (itmTmp == null) continue;
                if (string.Compare(itmTmp.FolderName, "Storyboards",true)==0)
                {
                    foreach (var storyItm in itmTmp.TREE_ITEM)
                    {
                        //if ((storyItm as MarsStoryboardTreeView) null) continue;
                        //if (storyItm )
                    }
                }
            }

            return null;
        }

        public static TreeViewItem FindStoryboardTreeViewItem(TreeView treeView, long storyboardId)
        {
            Logger.logBegin("FindStoryboardTreeViewItem", string.Format("StoryboardId:[{0}]", storyboardId));
            TreeViewItem treeViewItem = null;

            var myItems = FindTreeViewItems(treeView,true);
            Logger.Info("FindStoryboardTreeViewItem", string.Format("find:[{0}]", myItems.Count));

            foreach (TreeViewItem item in myItems)
            {
                if (item.DataContext.GetType().ToString().Contains("MarsStoryboardTreeView"))
                {
                    MarsStoryboardTreeView tView = (MarsStoryboardTreeView)item.DataContext;
                    if (tView.StoryboardId == storyboardId)
                    {
                        item.IsExpanded = true;
                        List<Control> lstParnt = new List<Control>();
                        GetAllParents(item, lstParnt);
                        Logger.Info("FindStoryboardTreeViewItem", string.Format("find parent:[{0}]", lstParnt));
                        for(int i=0;i < lstParnt.Count ; i++)
                        {
                            var tmpParent = lstParnt[i];
                            TreeViewItem tmpTreeItm = tmpParent as TreeViewItem;
                            tmpTreeItm.IsExpanded = true;
                            tmpParent.UpdateLayout();
                        }

                        treeViewItem = item;
                        break;
                    }
                }
             }
            Logger.logEnd("FindStoryboardTreeViewItem");
            return treeViewItem;
        }

        private static void GetAllParents(Control itm, List<Control> prnt)
        {
            if (itm == null) return;
            if (itm.Parent!=null)
            {
                prnt.Add(itm.Parent as Control);
                GetAllParents(itm.Parent as Control, prnt);
            }
        }

        public static TreeViewItem FindProjectViewItem(TreeView treeView, long projectId)
        {
            Logger.logBegin("FindProjectViewItem",string.Format("Project:[{0}]", projectId));
            TreeViewItem treeViewItem = null;

            Logger.Info("FindProjectViewItem", "begin FindTreeViewItems ");
            var myItems = FindTreeViewItems(treeView,false);
            Logger.Info("FindProjectViewItem", "end FindTreeViewItems ");
            List<Control> lstParnt = new List<Control>();
            Logger.Info("FindProjectViewItem",string.Format("find:[{0}]", myItems.Count));

            foreach (TreeViewItem item in myItems)
            {
                
                if (item == null) continue;                

                if (item.DataContext.GetType().ToString().Contains("MarsProjectTreeView"))
                {
                    MarsProjectTreeView tView = (MarsProjectTreeView)item.DataContext;
                    if (tView.ProjectId == projectId)
                    {
                        item.IsExpanded = true;
                        item.UpdateLayout();

                        //lstParnt.Clear();
                        //GetAllParents(item, lstParnt);
                        //Logger.Info("FindStoryboardTreeViewItem", string.Format("find parent:[{0}]", lstParnt));
                        //for (int i = 0; i < lstParnt.Count; i++)
                        //{
                        //    var tmpParent = lstParnt[i];
                        //    TreeViewItem tmpTreeItm = tmpParent as TreeViewItem;
                        //    tmpTreeItm.IsExpanded = true;
                        //    tmpParent.UpdateLayout();
                        //}
                        //treeViewItem = item;
                        treeViewItem = item;
                        break;
                    }
                }
            }
            Logger.logEnd("FindProjectViewItem");
            return treeViewItem;
        }

        public static TreeViewItem FindProjectViewItem(TreeViewItem treeView, long projectId)
        {
            Logger.logBegin("FindProjectViewItem", string.Format("by Item,Project:[{0}]", projectId));
            TreeViewItem treeViewItem = null;

            var myItems = FindTreeViewItems(treeView);

            foreach (TreeViewItem item in myItems)
            {
                if (item.DataContext.GetType().ToString().Contains("MarsProjectTreeView"))
                {
                    MarsProjectTreeView tView = (MarsProjectTreeView)item.DataContext;
                    if (tView.ProjectId == projectId)
                    {
                        treeViewItem = item;
                        break;
                    }
                }
            }
            Logger.logEnd("FindProjectViewItem");
            return treeViewItem;
        }


        //int iTargetLevel=-1, int iCurrentLevel=-1,
        public static List<TreeViewItem> FindTreeViewItems(this Visual @this,  bool isUpdateLayout=true)
        {
            //Logger.logBegin("FindTreeViewItems");
            if (@this == null)
                return null;

            var result = new List<TreeViewItem>();

            var frameworkElement = @this as FrameworkElement;
            if (frameworkElement != null)
            {
                //Console.WriteLine(frameworkElement);
                //frameworkElement.ApplyTemplate();
            }

            Visual child = null;
            for (int i = 0, count = VisualTreeHelper.GetChildrenCount(@this); i < count; i++)
            {
                child = VisualTreeHelper.GetChild(@this, i) as Visual;

                var treeViewItem = child as TreeViewItem;

                bool expStatus = false;

                if (treeViewItem != null)
                {
                    expStatus = treeViewItem.IsExpanded;
                    result.Add(treeViewItem);
                    //////////////////////
                    if (!treeViewItem.IsExpanded)
                    {
                        if (isUpdateLayout)
                        {
                            treeViewItem.IsExpanded = true;

                            treeViewItem.UpdateLayout();
                        }
                    }
                    //////////////////////////
                }

                //if (iCurrentLevel + 1 >= iTargetLevel) return result;

                foreach (var childTreeViewItem in FindTreeViewItems(child, isUpdateLayout))
                {
                    result.Add(childTreeViewItem);
                }

                if (treeViewItem != null)
                {
                    if (isUpdateLayout)
                    {
                        treeViewItem.IsExpanded = expStatus;

                        treeViewItem.UpdateLayout();
                    }
                    
                }
            }
            //Logger.logEnd("FindTreeViewItems");

            return result;
        }

        public static ItemsControl GetSelectedTreeViewItemParent(TreeViewItem item)
        {
            Logger.logBegin("GetSelectedTreeViewItemParent");
            if (item == null)
                return null;

            DependencyObject parent = VisualTreeHelper.GetParent(item);
            while (!(parent is TreeViewItem || parent is TreeView))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            Logger.logEnd("GetSelectedTreeViewItemParent");
            return parent as ItemsControl;
        }


        internal static TreeViewItem FindTestSuiteTreeViewItem(TreeViewItem tv, long testSuiteId)
        {
            Logger.logBegin("FindTestSuiteTreeViewItem");
            TreeViewItem treeViewItem = null;

            var myItems = FindTreeViewItems(tv);

            foreach (TreeViewItem item in myItems)
            {
                if (item.DataContext.GetType().ToString().Contains("MarsTestSuiteTreeView"))
                {
                    MarsTestSuiteTreeView tView = (MarsTestSuiteTreeView)item.DataContext;
                    if (tView.TestSuiteId == testSuiteId)
                    {
                        treeViewItem = item;
                        break;
                    }
                }
            }
            Logger.logEnd("FindTestSuiteTreeViewItem");
            return treeViewItem;
        }

        internal static TreeViewItem FindTestCaseTreeViewItem(TreeViewItem tv, long testCaseId)
        {
            Logger.logBegin("FindTestCaseTreeViewItem");
            TreeViewItem treeViewItem = null;

            var myItems = FindTreeViewItems(tv);

            foreach (TreeViewItem item in myItems)
            {
                if (item.DataContext.GetType().ToString().Contains("MarsTestCaseTreeView"))
                {
                    MarsTestCaseTreeView tView = (MarsTestCaseTreeView)item.DataContext;
                    if (tView.TestCaseId == testCaseId)
                    {
                        treeViewItem = item;
                        break;
                    }
                }
            }
            Logger.logEnd("FindTestCaseTreeViewItem");
            return treeViewItem;
        }

        internal static TreeViewItem FindTestCaseTreeViewItem(TreeView tv, long testCaseId)
        {
            Logger.logBegin("FindTestCaseTreeViewItem");
            TreeViewItem treeViewItem = null;

            var myItems = FindTreeViewItems(tv);

            foreach (TreeViewItem item in myItems)
            {
                if (item.DataContext.GetType().ToString().Contains("MarsTestCaseTreeView"))
                {
                    MarsTestCaseTreeView tView = (MarsTestCaseTreeView)item.DataContext;
                    if (tView.TestCaseId == testCaseId)
                    {
                        treeViewItem = item;
                        break;
                    }
                }
            }
            Logger.logEnd("FindTestCaseTreeViewItem");
            return treeViewItem;
        }

        internal static TreeViewItem FindSetTreeViewItem(TreeViewItem tv, long dataSetId)
        {
            Logger.logBegin("FindTestCaseTreeViewItem");
            throw new NotImplementedException();
        }

        internal static TreeViewItem FindTestcaseNode(TreeView tv, long lProjId, long lTSId, long lTCId)
        {
            Logger.logBegin("FindTestcaseNode");
            var myItems = FindTreeViewItems(tv);
            //string strType = "";
            foreach (TreeViewItem item in myItems)
            {
                //strType += (item.DataContext.GetType().ToString() + "\r\n");
                if (item.DataContext.GetType().ToString().Contains("MarsTestCaseTreeView"))
                {
                    MarsTestCaseTreeView tView = (MarsTestCaseTreeView)item.DataContext;
                    if (tView.ProjectId == lProjId &&
                        tView.TestSuiteId == lTSId &&
                        tView.TestCaseId == lTCId )
                    {
                        return item;
                    }
                }
            }
            Logger.logEnd("FindTestcaseNode");
            return null;
        }

        internal static TreeViewItem FindDataSheetTreeViewItem(TreeView tv, long projectId , long testSuiteId, long testCaseId, long dataSheetId)
        {
            Logger.logBegin("FindDataSheetTreeViewItem");
            TreeViewItem treeViewItem = null;

            var myItems = FindTreeViewItems(tv);

            foreach (TreeViewItem item in myItems)
            {
                if (item.DataContext.GetType().ToString().Contains("MarsDataSheetTreeView"))
                {
                    MarsDataSheetTreeView tView = (MarsDataSheetTreeView)item.DataContext;
                    if (tView.ProjectId == projectId &&
                        tView.TestSuiteId == testSuiteId &&
                        tView.TestCaseId == testCaseId &&
                        tView.DataSheetId == dataSheetId)
                    {
                        treeViewItem = item;
                        break;
                    }
                }
            }
            Logger.logEnd("FindDataSheetTreeViewItem");
            return treeViewItem;
        }

        public static DependencyObject FindParent<T>(DependencyObject obj)
        {
            if (obj == null) return null;
            DependencyObject oP=obj;
            while ((oP = VisualTreeHelper.GetParent(oP))!=null)
            {
                if (oP is T) return oP;
            }
            return null;
        }

        public static T FindChild<T>(DependencyObject parent, string childName)
        where T : DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        internal static TreeViewItem FindDashboardTreeViewItem(TreeView tvMars, long projectId)
        {
            TreeViewItem treeViewItem = null;

            var myItems = FindTreeViewItems(tvMars);

            foreach (TreeViewItem item in myItems)
            {
                if (item.DataContext.GetType().ToString().Contains("MarsFolderTreeView"))
                {
                    MarsFolderTreeView tView = (MarsFolderTreeView)item.DataContext;
                    if (tView.ProjectId == projectId)
                    {
                        treeViewItem = item;
                        break;
                    }
                }
            }

            return treeViewItem;
        }
    }



}
