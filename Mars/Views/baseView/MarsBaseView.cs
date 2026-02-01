//using Microsoft.Windows.Controls.Primitives;
using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Mars.Views.baseView
{
    public class MarsBaseViewControl : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(MarsBaseViewControl), null);
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value);
                RaisePropertyChanged("Title");
            }
        }
        public void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class MarsBaseGridViewControl: MarsBaseViewControl
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsBaseGridViewControl));
        internal DataGridCell GetCellFromRowAndCellOrd(DataGridRow oneRow, int iColIdx, ref bool isOk, ref string strError)
        {
            Logger.logBegin("GetCellFromRowAndCellOrd",string.Format("ColIdx:[{0}]", iColIdx));
            try
            {
                if (oneRow == null)
                {
                    isOk = true;
                    Logger.Warnning("GetCellFromRowAndCellOrd","Row object is null");
                    return null;
                }
                System.Windows.Controls.Primitives.DataGridCellsPresenter cellsRepresenter = GetVisualChild<System.Windows.Controls.Primitives.DataGridCellsPresenter>(oneRow);
                //DataGridCell cellPrsnt = GetVisualChild<DataGridCell>(oneRow);
                if (cellsRepresenter != null)
                {
                    if (cellsRepresenter.ItemContainerGenerator.Items.Count <= iColIdx)
                    {
                        isOk = false;
                        Logger.Error("GetCellFromRowAndCellOrd",strError=string.Format("Only [{0}] columns exist, but require the [{1}] ", cellsRepresenter.ItemContainerGenerator.Items.Count, iColIdx));
                        return null;
                    }
                    DataGridCell cellPrsnt = (DataGridCell)cellsRepresenter.ItemContainerGenerator.ContainerFromIndex(iColIdx); 
                    isOk = true;
                    return cellPrsnt;
                    //DataGridCell resultCell = (DataGridCell)cellPrsnt.ItemContainerGenerator.ContainerFromIndex(iColIdx);
                    //isOk = true;
                    //return resultCell;
                }
                isOk = false;
                Logger.Error("GetCellFromRowAndCellOrd", strError = string.Format("no cell find from cell index:[{0}]", iColIdx));
                return null;
            }
            finally
            {
                Logger.logEnd("GetCellFromRowAndCellOrd");
            }
        }

        public static T GetVisualChild<T>(Visual parent) where T :Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i<numVisuals;i++)
            {
                DependencyObject v = VisualTreeHelper.GetChild(parent,i);
                child = v as T;
                if (child ==null)
                {
                    child = GetVisualChild<T>((Visual)v);                    
                }
                if (child!=null)
                {
                    break;
                }
            }
            return child;
        }
    }
}
