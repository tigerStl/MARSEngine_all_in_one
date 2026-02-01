using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars.auto.LoadDataFromDB.auto.basedatastructure.MarsCfg;
using Mars.ViewModel;
using System.Collections.ObjectModel;
using System.Windows.Input;   

namespace Mars.Dialog.autoLoadData
{
    class AutoLoadDataParameterSettingModal:ViewModelBase
    {
        private ObservableCollection<MarsObjDataSrcMappingParameter> _ParametersNeedSet;
        public ObservableCollection<MarsObjDataSrcMappingParameter> ParametersNeedSet
        {
            get { return _ParametersNeedSet; }
            internal set {
                _ParametersNeedSet = value;
                RaisePropertyChanged("ParametersNeedSet");
                if ((_ParametersNeedSet!=null)&&(_ParametersNeedSet.Count>0))
                {
                    currentParameterObject = _ParametersNeedSet[0];
                }
            }
        }

        private MarsObjDataSrcMappingParameter _currentParameterObject;
        public MarsObjDataSrcMappingParameter currentParameterObject
        {
            get { return _currentParameterObject; }
            set
            {
                _currentParameterObject = value;
                RaisePropertyChanged("currentParameterObject");
            }
        }

        public bool OkBtnClickCommand(ref string strError)
        {
            for (int i=0;i<(_ParametersNeedSet==null?-1:_ParametersNeedSet.Count);i++)
            {
                MarsObjDataSrcMappingParameter oPCur = _ParametersNeedSet[i];
                if (!(oPCur==null? true :oPCur.CheckParaValueSetting()))
                {
                    strError = string.Format("Parameter type and values aren't match. [Require/Return:{0}/{1}]", oPCur.SourceType,oPCur.ParameterValue);
                    return false;
                }
            }
            return true;
        }

        private string _DatasetNamePattern;
        public string DatasetNamePattern
        {
            get
            {
                return _DatasetNamePattern;
            }
            set
            {
                _DatasetNamePattern = value;
                RaisePropertyChanged("DatasetNamePattern");
            }
        }
    }
}
