using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Mars.Views.gridBase
{
    public class MarsTigerGridBase: System.Windows.Controls.DataGrid
    {
        public DataGridRow GetEditingRow()
        {
            int sIndex = this.SelectedIndex;
            if (sIndex >= 0)
            {
                var selected = (DataGridRow)this.ItemContainerGenerator.ContainerFromIndex(sIndex);
                if (selected == null) return null;
                if (selected.IsEditing) return selected;
            }

            for (int i = 0; i < this.Items.Count; i++)
            {
                if (i == sIndex) continue;
                var itm = (DataGridRow)this.ItemContainerGenerator.ContainerFromIndex(i);
                if (itm == null) continue;
                if (itm.IsEditing) return itm;
            }
            return null;
        }
        public bool IsEditing()
        {
            return this.GetEditingRow() != null;
        }
    }
}
