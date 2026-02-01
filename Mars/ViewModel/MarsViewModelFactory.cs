using Route2NSEx.src.Marquis.systemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars.ViewModel
{
    public class MarsViewModelFactory
    {
        private static MLogger Logger = MLogger.GetLogger(typeof(MarsViewModelFactory));

        #region registed ViewModeName
        public const string CNST_VIEW_NAME_BASELINE = "BASE_LINE_DATA_EDITOR";

        #endregion //registed ViewModeName

        public static ViewModelBase GetViewModeDataContextByType(string ViewTypeName)
        {
            Logger.Info("GetViewModeDataContextByType",string.Format("Typeof View:[{0}]", ViewTypeName));
            switch (ViewTypeName)
            {
                case CNST_VIEW_NAME_BASELINE:
                    return new BaselineDataEditorViewModel();
            }
            Logger.Warnning("GetViewModeDataContextByType",string.Format("No such ViewTypeName for this methode [{0}]", ViewTypeName));
            return null;
        }
    }
}
