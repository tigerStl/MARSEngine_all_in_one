using Mars.ViewModel;
using System;
using System.Windows.Input;

namespace Mars.Views.objectManagement
{
    internal class DataPopup4ObjDefinitionDataModal
    {
        public DataPopup4ObjDefinitionDataModal()
        {
        }

        internal DelegateCommandWithParam DatabaseTypeClick = new DelegateCommandWithParam(o => {
            if (o == null) return;
            string strSubCmmd = o.ToString().ToUpper();
            switch(strSubCmmd)
            {
                case "ORACLE":
                    break;
                case "SQL":
                    break;
            }
        });
        
    }
}