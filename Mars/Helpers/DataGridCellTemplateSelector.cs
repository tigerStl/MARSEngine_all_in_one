using Mars.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Mars.Helpers
{
    public class DataGridCellTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DateTemplate { get;
            set; }
        public DataTemplate ComboBoxTemplate {
            get;
            set; }
        public DataTemplate StringTemplate {
            get;
            set;
        }

        public DataTemplate FillPasswordTemplate {
            get;
            set;
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is TestStepViewModel)
            {
                var itm = item as TestStepViewModel;
                if ((itm.SelectedKeyword != null) && (string.Compare("FillPassword", itm.SelectedKeyword.KeywordName, true) == 0))
                    return FillPasswordTemplate;
                //if (((item as TestCaseEditViewModel).DataSetDataType == "DateTime"))
                //    return DateTemplate;

                //else if ((item as TestCaseEditViewModel).DataSetDataType == "ComboBox")
                //    return ComboBoxTemplate;

                //else
                return StringTemplate;
            }
            return base.SelectTemplate(item, container); 
        }

    }

    public class DataGridCellEditTemplateSelector : DataTemplateSelector
    {
        public DataTemplate EditingDateTemplate1 { get; set; }
        public DataTemplate EditingComboBoxTemplate1 { get; set; }
        public DataTemplate FillPasswordTemplate { get; set; }
        public DataTemplate EditingStringTemplate1 { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is TestStepViewModel)
            {
                var itm = item as TestStepViewModel;
                if (((item as TestStepViewModel).DataSetDataType == "DateTime"))
                    return EditingDateTemplate1;

                else if ((item as TestStepViewModel).DataSetDataType == "ComboBox")
                    return EditingComboBoxTemplate1;
                else if ((itm.SelectedKeyword != null) && (string.Compare("FillPassword", itm.SelectedKeyword.KeywordName, true) == 0))
                    return FillPasswordTemplate;
                else
                    return EditingStringTemplate1;
            }
            return base.SelectTemplate(item, container);
        }

    }
}
