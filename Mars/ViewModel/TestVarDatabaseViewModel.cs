using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.Model;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Security.Principal;
using Mars.Business;
using Mars.Dto;

using System.IO;
//using Microsoft.Practices.Prism.Commands;
using Prism.Commands;
using System.Data;
using Mars.Helpers;
using System.Windows.Forms;
using Mars.DataLayer;
using Mars.Utility;
using System.Windows.Threading;
using System.Threading;
using Route2NSEx.src.Marquis.systemUtil;
using System.Windows.Data;

namespace Mars.ViewModel
{
    public class TestVarDatabaseViewModel : ViewModelBase
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(TestVarDatabaseViewModel));

        public string nameSearchString = "";
        public string typeSearchString = "";
        ObservableCollection<B_SYSTEM_LOOKUP> _systemLookup = new ObservableCollection<B_SYSTEM_LOOKUP>();

        public ObservableCollection<B_SYSTEM_LOOKUP> SystemLookup
        {
            get
            {
                return _systemLookup;
            }

            set
            {
                _systemLookup = value;
                RaisePropertyChanged("SystemLookup");
            }
        }



        private B_SYSTEM_LOOKUP selectedVar;
        public B_SYSTEM_LOOKUP SelectedVar
        {
            get { return selectedVar; }
            set
            {
                selectedVar = value;
                RaisePropertyChanged("SelectedVar");
                //RaisePropertyChanged();
            }
        }

        public bool UserFilterByName(object item)
        {
            if (String.IsNullOrEmpty(nameSearchString))
                return true;
            else
                if (((item as B_SYSTEM_LOOKUP).FIELD_NAME == null)) return false;
            else
                return ((item as B_SYSTEM_LOOKUP).FIELD_NAME.IndexOf(nameSearchString, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public bool UserFilterByType(object item)
        {
            if (String.IsNullOrEmpty(typeSearchString))
                return true;
            else
                if (((item as B_SYSTEM_LOOKUP).TABLE_NAME == null)) return false;
            else
                return ((item as B_SYSTEM_LOOKUP).TABLE_NAME.IndexOf(typeSearchString, StringComparison.OrdinalIgnoreCase) >= 0);
        }


        public bool UserFilterByNameAndType(object item)
        {
            if (String.IsNullOrEmpty(typeSearchString) && String.IsNullOrEmpty(nameSearchString))
                return true;
            else
                if ((item as B_SYSTEM_LOOKUP).TABLE_NAME == null || (item as B_SYSTEM_LOOKUP).FIELD_NAME == null) return false;
            else
            {
                if (String.IsNullOrEmpty(typeSearchString))
                    return ((item as B_SYSTEM_LOOKUP).FIELD_NAME.IndexOf(nameSearchString, StringComparison.OrdinalIgnoreCase) >= 0);
                else if (String.IsNullOrEmpty(nameSearchString))
                    return ((item as B_SYSTEM_LOOKUP).TABLE_NAME.IndexOf(typeSearchString, StringComparison.OrdinalIgnoreCase) >= 0);
                else
                    if (
                    ((item as B_SYSTEM_LOOKUP).FIELD_NAME.IndexOf(nameSearchString, StringComparison.OrdinalIgnoreCase) >= 0) &&
                    ((item as B_SYSTEM_LOOKUP).TABLE_NAME.IndexOf(typeSearchString, StringComparison.OrdinalIgnoreCase) >= 0)
                    )
                    return true;
                else
                    return false;

            }
               
        }



        public TestVarDatabaseViewModel()
        {
            //B_SYSTEM_LOOKUP sysLookup = new B_SYSTEM_LOOKUP();

            //List<B_SYSTEM_LOOKUP> sysLookupList = (from p in sysLookup.GetSystemLookup()
            //                                      where p.TABLE_NAME.Equals("GLOBAL_VAR") || p.TABLE_NAME.Equals("MODAL_VAR")
            //                                      select p).ToList();
            //SystemLookup.Clear();
            //SystemLookup = new ObservableCollection<B_SYSTEM_LOOKUP>(sysLookupList);

            InitLookupList();

        }

        private static readonly IList<string> MANAGED_VARTYPE = new ReadOnlyCollection<string>(new List<string>() { "GLOBAL_VAR", "MODAL_VAR" , "LOCAL_VAR" , "LOOP_VAR" });
        public void InitLookupList()
        {
            B_SYSTEM_LOOKUP sysLookup = new B_SYSTEM_LOOKUP();

            List<B_SYSTEM_LOOKUP> sysLookupList = sysLookup.GetSystemLookup(MarsMainWindow.CurrentDatabaseIdx).Where(p => MANAGED_VARTYPE.Contains(p.TABLE_NAME)).ToList() ;                                                   
            SystemLookup.Clear();
            if ((sysLookupList==null) ||(sysLookupList.Count==0))
            {
                sysLookupList = new List<B_SYSTEM_LOOKUP>();
                sysLookupList.Add(B_SYSTEM_LOOKUP.createDefault());
            }
            SystemLookup = new ObservableCollection<B_SYSTEM_LOOKUP>(sysLookupList);
            if (SystemLookup.Count>=1)
            {
                SelectedVar = SystemLookup[0];
            }
        }

    }
}
